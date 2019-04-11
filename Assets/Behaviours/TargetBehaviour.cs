using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetBehaviour : MonoBehaviour
{
    /* Configurable parameters */
    public GameObject destination;       // the final destination of the drone
    public GameObject user;              // the user that is following the drone
    public GameObject drone;             // the drone itself
    public float waypointMargin = 0.1f;  // the maximum distance at which a waypoint is considered 'reached'
    public float toleranceAngle = 45;    // maximum angle at which the user can be to the drone before it adjusts sideways
    public float defaultDistance = 4;    // distance to the user that the drone should try to keep
    public float stabilizationTime = 1;  // currently ununsed
    public bool useTracking = false;     // whether to use tracking to determinine user position
    public bool dontMove = false;        // whether to stay stationary (for testing e.g. tracking)

    /* Pathfinding variables */
    private List<Vector3> path;          // sequence of waypoints to follow to get to the destination
    private int nextWaypoint = 1;        // index of the next waypoint in the path

    /* Tolerance zone variables */
    private Mesh zoneMesh;               // mesh of tolerance zone visualization
    private Vector3 userPos0;      // last significant direction of the user from the drone

    /* Tracking variables */
    private Tracking tracking;           // used to get a bounding box of the user
    private GameObject droneCam;         // camera placed on the drone, used for tracking the user
    private float userHeight;            // real height of the user

    /* Debug variables */
    private GUIBehaviour gui;            // object used to display debug information

    // Updates the tolerance zone mesh using the user direction and zone parameters
    void UpdateToleranceZone()
    {
        zoneMesh.Clear();

        Vector3 U = userPos0.normalized;
        Vector3 UT = new Vector3(U.z, 0, -U.x);  // orthogonal to U

        zoneMesh.vertices = new Vector3[] {
            3f/4f * defaultDistance * (U + Mathf.Tan(toleranceAngle/2) * UT),
            3f/4f * defaultDistance * (U - Mathf.Tan(toleranceAngle/2) * UT),
            3f/2f * defaultDistance * (U + Mathf.Tan(toleranceAngle/2) * UT),
            3f/2f * defaultDistance * (U - Mathf.Tan(toleranceAngle/2) * UT)
        };

        zoneMesh.triangles = new int[] {0, 1, 2, 1, 3, 2};
    }

    // Gets the relative movement 
    Vector3 GetMovement()
    {
        // Get position of user relative to target
        Vector3 userPos;
        if (useTracking && tracking != null)
        {
            // Get position using bounding box produced by tracking algorithm
            Vector3 droneToUser = tracking.GetObjectPosition(droneCam, userHeight);
            if (droneToUser.magnitude == 0)
            {
                // We don't know where the user is, so don't move
                return Vector3.zero;
            }

            userPos = drone.transform.position + droneToUser;

            // Draw a line for debugging
            Debug.DrawLine(drone.transform.position, userPos, Color.red);
        }
        else
        {
            // Get position by accessing the GameObject's transform (cheating)
            userPos = user.transform.position - transform.position;
            userPos.y = 0;  // project onto the horizontal plane
        }

        // Get distance and angle of user to target
        float userDist = userPos.magnitude;
        float userAngle = Vector3.Angle(userPos, userPos0);

        // Get position and distance of next waypoint
        Vector3 nextWaypointPos = path[nextWaypoint] - transform.position;
        float nextWaypointDist = nextWaypointPos.magnitude;

        // Output debug information
        if (gui)
        {
            gui.SetVar("User-target angle", userAngle);
            gui.SetVar("User-target distance", userDist);
            gui.SetTargetInfo(transform.position, userPos, userPos0);
        }

        // Determine relative target movement
        if (nextWaypointDist > 0 &&
            userDist < 3f/4f * defaultDistance / Mathf.Cos(Mathf.Deg2Rad * userAngle) * stabilizationTime)
        {
            Debug.Log("User too close");
            float moveDist = Mathf.Min(defaultDistance - userDist, nextWaypointDist);  // make sure we don't move past the waypoint
            return moveDist * nextWaypointPos.normalized;
        }
        if (userAngle > toleranceAngle / 2f * stabilizationTime)
        {
            Debug.Log("User went sideways");

            Vector3 waypointToUserDir = user.transform.position - path[nextWaypoint];
            float beta = Mathf.Deg2Rad * Vector3.SignedAngle(userPos, waypointToUserDir, Vector3.up);
            float delta = Mathf.PI/2 - beta;
            float moveDist = userDist * Mathf.Sin(beta);
            Vector3 move = moveDist * userPos.normalized;  // vector with correct magnitude but in direction of user

            // Rotate move vector in correct direction (perpendicular to line between user and next waypoint)
            move = new Vector3(
                move.x * Mathf.Cos(delta) - move.z * Mathf.Sin(delta),
                0,
                move.x * Mathf.Sin(delta) + move.z * Mathf.Cos(delta)
            );

            // Set new significant user position
            userPos0 = user.transform.position + move - transform.position;
            userPos0.y = 0;  // project onto horizontal plane

            // Point drone camera towards user
            if (!dontMove)
            {
                droneCam.transform.forward = userPos0;
            }
            
            return move;
        }
        if (userDist > 3f/2f * defaultDistance / Mathf.Cos(Mathf.Deg2Rad * userAngle) * stabilizationTime)
        {
            Debug.Log("User too far");
            return (userDist - defaultDistance) * userPos.normalized;
        }

        return Vector3.zero;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Get GUI script
        gui = GameObject.Find("GUI").GetComponent<GUIBehaviour>();

        // Get tolerance zone mesh
        GameObject zoneObj = transform.Find("ToleranceZone").gameObject;
        if (zoneObj)
        {
            zoneMesh = zoneObj.GetComponent<MeshFilter>().mesh;            
        }

        // Find shortest path from current position to destination, using navNodes as waypoints
        path = Pathfinding.GetShortestPath(gameObject, destination);
        gui.SetPathfindingInfo(Pathfinding.nodes, path);

        // Check if path is valid
        if (path == null || path.Count == 0)
        {
            Debug.Log("Failed to find a valid path");
        }

        // Set initial tolerance zone position
        userPos0 = user.transform.position - transform.position;
        userPos0.y = 0;  // project onto the horizontal plane
        UpdateToleranceZone();

        // Initialize tracking
        if (useTracking)
        {
            tracking = new Tracking();
            droneCam = GameObject.Find("DroneCamera");
            userHeight = user.GetComponent<Renderer>().bounds.size.y;
        }
    }

    // FixedUpdate is called once per physics update
    void FixedUpdate()
    {
        // Don't try to move if there is no path
        if (path == null || nextWaypoint >= path.Count) return;
        
        // Adjust target position when user steps out of tolerance zone
        Vector3 movement = GetMovement();
        if (movement.magnitude > 0 && !dontMove)
        {
            transform.position += movement;
            UpdateToleranceZone();
        }

        // If we have reached the current waypoint, select the next node in the path (unless this is the last one)
        float nextWaypointDist = Vector3.Distance(path[nextWaypoint], transform.position);
        if (nextWaypointDist <= waypointMargin && nextWaypoint < path.Count - 1)
        {
            Debug.Log("Reached waypoint");
            nextWaypoint++;
        }
    }

    // Called when the object is destroyed or the game exits
    void OnDestroy()
    {
        if (tracking != null)
        {
            tracking.Stop();
        }
    }
}
