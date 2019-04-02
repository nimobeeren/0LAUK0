using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetBehaviour : MonoBehaviour
{
    /* Configurable parameters */
    public GameObject destination;       // the final destination of the drone
    public GameObject user;
    public float waypointMargin = 0.1f;  // the maximum distance at which a waypoint is considered 'reached'
    public float toleranceAngle = 45;    // maximum angle at which the user can be to the drone before it adjusts sideways
    public float defaultDistance = 4;    // distance to the user that the drone should try to keep
    public float stabilizationTime = 1;  // currently ununsed

    /* Pathfinding variables */
    private List<Vector3> path;          // sequence of waypoints to follow to get to the destination
    private int nextWaypoint = 1;        // index of the next waypoint in the path

    /* Tolerance zone variables */
    private Vector3 userDirection0;      // last significant direction of the user from the drone

    /* Debug variables */
    private GUIBehaviour gui;            // object used to display debug information

    // Start is called before the first frame update
    void Start()
    {
        // Get GUI script
        gui = GameObject.Find("GUI").GetComponent<GUIBehaviour>();

        // Find shortest path from current position to destination, using navNodes as waypoints
        path = Pathfinding.GetShortestPath(gameObject, destination);
        gui.SetPathfindingInfo(Pathfinding.nodes, path);

        // Check if path is valid
        if (path == null || path.Count == 0)
        {
            Debug.Log("Failed to find a valid path");
        }

        userDirection0 = user.transform.position - transform.position;
        userDirection0.y = 0;  // project onto the horizontal plane
    }

    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.15f);
    }

    // FixedUpdate is called once per physics update
    void FixedUpdate()
    {
        // Calculate user angle and distance
        Vector3 userDirection = user.transform.position - transform.position;
        userDirection.y = 0;  // project onto the horizontal plane
        float userAngle = Vector3.Angle(userDirection, userDirection0);
        float userDistance = userDirection.magnitude;

        // Output debug information
        if (gui)
        {
            gui.SetVar("User angle", userAngle);
            gui.SetVar("User distance", userDistance);
            gui.SetTargetInfo(transform.position, userDirection, userDirection0);
        }
        
        // Don't try to move if there is no path
        if (path == null || nextWaypoint >= path.Count) return;

        // Calculate distance and direction of next waypoint
        Vector3 nextWaypointDir = path[nextWaypoint] - transform.position;
        float nextWaypointDist = nextWaypointDir.magnitude;
        
        // Adjust target position when user steps out of tolerance zone
        if (nextWaypointDist > 0 &&
            userDistance < 3f/4f * defaultDistance / Mathf.Cos(Mathf.Deg2Rad * userAngle) * stabilizationTime)
        {
            Debug.Log("User too close");
            float moveDist = Mathf.Min(defaultDistance - userDistance, nextWaypointDist);  // make sure we don't move past the waypoint

            transform.position += moveDist * nextWaypointDir.normalized;
            userDirection0 = user.transform.position - transform.position;
            userDirection0.y = 0;  // project onto horizontal plane
        }
        if (userAngle > toleranceAngle / 2f * stabilizationTime)
        {
            Debug.Log("User went sideways");

            Vector3 waypointUserDir = user.transform.position - path[nextWaypoint];
            float beta = Mathf.Deg2Rad * Vector3.SignedAngle(userDirection, waypointUserDir, Vector3.up);
            float delta = Mathf.PI/2 - beta;
            float moveDist = userDistance * Mathf.Sin(beta);
            Vector3 move = moveDist * userDirection.normalized;  // vector with correct magnitude but in direction of user
            move = new Vector3(move.x * Mathf.Cos(delta) - move.z * Mathf.Sin(delta), 0, move.x * Mathf.Sin(delta) + move.z * Mathf.Cos(delta));  // rotated vector

            transform.position += move;
            userDirection0 = user.transform.position - transform.position;
            userDirection0.y = 0;  // project onto horizontal plane
        }
        if (userDistance > 3f/2f * defaultDistance / Mathf.Cos(Mathf.Deg2Rad * userAngle) * stabilizationTime)
        {
            Debug.Log("User too far");
            transform.position += (userDistance - defaultDistance) * userDirection.normalized;
            userDirection0 = user.transform.position - transform.position;
            userDirection0.y = 0;  // project onto horizontal plane
        }

        // If we have reached the current waypoint, select the next node in the path (unless this is the last one)
        if (nextWaypointDist <= waypointMargin && nextWaypoint < path.Count - 1)
        {
            Debug.Log("Reached waypoint");
            nextWaypoint++;
        }
    }
}
