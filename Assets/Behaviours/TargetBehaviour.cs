using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetBehaviour : MonoBehaviour
{
    public GameObject destination;       // the final destination of the drone
    public GameObject user;
    public float speed = 2;              // the (constant) speed at which the target moves to the destination
    public float waypointMargin = 0.1f;  // the maximum distance at which a waypoint is considered 'reached'
    public float toleranceAngle = 90;
    public float defaultDistance = 4;
    public float stabilizationTime = 1;

    private List<Vector3> path;          // sequence of waypoints to follow to get to the destination
    private int nextWaypoint = 1;        // index of the next waypoint in the path

	private GUIBehaviour gui;
    private Vector3 userDirection0;
    private Vector3 userDirection;
    private float userAngle;
    private float userDistance;

    // Start is called before the first frame update
    void Start()
    {
    	// Get GUI script
    	gui = GameObject.Find("GUI").GetComponent<GUIBehaviour>();

        // Find shortest path from current position to destination, using navNodes as waypoints
        path = Pathfinding.GetShortestPath(gameObject, destination);

        // Display the path
        if (path == null || path.Count == 0)
        {
            Debug.Log("Failed to find a valid path");
        }
        else
        {
            // Draw a line along the path
            // for (int i = 0; i < path.Count - 1; i++)
            // {
            //     Debug.DrawLine(path[i], path[i+1], Color.green, Mathf.Infinity);
            // }
        }

        userDirection0 = user.transform.position - transform.position;
        userDirection0.y = 0;  // project onto the horizontal plane
    }

    // FixedUpdate is called once per physics update
    void FixedUpdate()
    {
        // Calculate user angle and distance
        userDirection = user.transform.position - transform.position;
        userDirection.y = 0;  // project onto the horizontal plane
        userAngle = Vector3.Angle(userDirection, userDirection0);
        userDistance = userDirection.magnitude;

        gui.setVar("User angle", userAngle);
        Debug.DrawRay(transform.position, userDirection0, Color.white);
        Debug.DrawRay(transform.position, userDirection, Color.red);
        
        // Don't try to move if there is no path
        if (path == null || nextWaypoint >= path.Count) return;

        Vector3 nextWaypointDir = path[nextWaypoint] - transform.position;
        float nextWaypointDist = nextWaypointDir.magnitude;
        

        if (nextWaypointDist > 0 &&
        	userDistance < 3f/4f * defaultDistance / Mathf.Cos(Mathf.Deg2Rad * userAngle) * stabilizationTime)
        {
            Debug.Log("User too close");
            float moveDist = Mathf.Min(defaultDistance - userDistance, nextWaypointDist);  // make sure we don't move past the waypoint
            transform.position += moveDist * nextWaypointDir.normalized;
            userDirection0 = userDirection;
        }
        if (userAngle > toleranceAngle / 2f * stabilizationTime)
        {
            Debug.Log("User went sideways");
            userDirection0 = userDirection;
        }
        if (userDistance > 3f/2f * defaultDistance / Mathf.Cos(Mathf.Deg2Rad * userAngle) * stabilizationTime)
        {
            Debug.Log("User too far");
            transform.position += (userDistance - defaultDistance) * userDirection.normalized;
            userDirection0 = userDirection;
        }

        // If we have reached the current waypoint, select the next node in the path (unless this is the last one)
        if (nextWaypointDist <= waypointMargin && nextWaypoint < path.Count - 1)
        {
            Debug.Log("Reached waypoint");
            nextWaypoint++;
        }
    }
}
