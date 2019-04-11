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

    /* Pathfinding variables */
    private List<Vector3> path;          // sequence of waypoints to follow to get to the destination
    private int nextWaypoint = 1;        // index of the next waypoint in the path

    /* Tolerance zone variables */
    private Mesh zoneMesh;               // mesh of tolerance zone visualization
    private Vector3 userDir0;      // last significant direction of the user from the drone

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

        Vector3 U = userDir0.normalized;
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
        // Get user-target direction and distance
        Vector3 realUserDir = user.transform.position - transform.position;
        realUserDir.y = 0;  // project onto the horizontal plane
        float realUserAngle = Vector3.Angle(realUserDir, userDir0);
        float realUserDist = realUserDir.magnitude;

        // Estimate user position using tracking
        Vector3 estUserPosition = drone.transform.position + tracking.GetObjectPosition(droneCam, userHeight);
        Debug.DrawLine(drone.transform.position, estUserPosition, Color.red);

        // Get drone direction and distance
        Vector3 droneDir = drone.transform.position - transform.position;
        droneDir.y = 0;  // project onto the horizontal plane
        float droneDist = droneDir.magnitude;

        // Get distance and direction of next waypoint
        Vector3 nextWaypointDir = path[nextWaypoint] - transform.position;
        float nextWaypointDist = nextWaypointDir.magnitude;

        // Output debug information
        if (gui)
        {
            gui.SetVar("Real user-target angle", realUserAngle);
            gui.SetVar("Real user-target distance", realUserDist);
            gui.SetVar("Estimated user distance", estUserPosition.magnitude);
            gui.SetTargetInfo(transform.position, realUserDir, userDir0);
        }

        // Determine relative target movement
        if (nextWaypointDist > 0 &&
            realUserDist < 3f/4f * defaultDistance / Mathf.Cos(Mathf.Deg2Rad * realUserAngle) * stabilizationTime)
        {
            Debug.Log("User too close");
            float moveDist = Mathf.Min(defaultDistance - realUserDist, nextWaypointDist);  // make sure we don't move past the waypoint
            return moveDist * nextWaypointDir.normalized;
        }
        if (realUserAngle > toleranceAngle / 2f * stabilizationTime)
        {
            Debug.Log("User went sideways");

            Vector3 waypointUserDir = user.transform.position - path[nextWaypoint];
            float beta = Mathf.Deg2Rad * Vector3.SignedAngle(realUserDir, waypointUserDir, Vector3.up);
            float delta = Mathf.PI/2 - beta;
            float moveDist = realUserDist * Mathf.Sin(beta);
            Vector3 move = moveDist * realUserDir.normalized;  // vector with correct magnitude but in direction of user

            // Rotate move vector in correct direction (perpendicular to line between user and next waypoint)
            move = new Vector3(
                move.x * Mathf.Cos(delta) - move.z * Mathf.Sin(delta),
                0,
                move.x * Mathf.Sin(delta) + move.z * Mathf.Cos(delta)
            );

            // Set new significant user direction
            userDir0 = user.transform.position + move - transform.position;
            userDir0.y = 0;  // project onto horizontal plane
            
            return move;
        }
        if (realUserDist > 3f/2f * defaultDistance / Mathf.Cos(Mathf.Deg2Rad * realUserAngle) * stabilizationTime)
        {
            Debug.Log("User too far");
            return (realUserDist - defaultDistance) * realUserDir.normalized;
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
        userDir0 = user.transform.position - transform.position;
        userDir0.y = 0;  // project onto the horizontal plane
        UpdateToleranceZone();

        // Initialize tracking
        tracking = new Tracking();
        droneCam = GameObject.Find("DroneCamera");
        userHeight = user.GetComponent<Renderer>().bounds.size.y;
    }

    // FixedUpdate is called once per physics update
    void FixedUpdate()
    {
        // Don't try to move if there is no path
        if (path == null || nextWaypoint >= path.Count) return;
        
        // Adjust target position when user steps out of tolerance zone
        Vector3 movement = GetMovement();
        if (movement.magnitude > 0)
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
        tracking.Stop();
    }
}
