using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetBehaviour : MonoBehaviour
{
    public GameObject destination;       // the final destination of the drone
    public float speed = 2;              // the (constant) speed at which the target moves to the destination
    public float waypointMargin = 0.1f;  // the maximum distance at which a waypoint is considered 'reached'

    private bool flying = false;         // whether the drone is currently flying
    private Rigidbody rb;                // the target's rigidbody
    private List<Vector3> path;          // sequence of waypoints to follow to get to the destination

    // Start is called before the first frame update
    void Start()
    {
        // Get our own Rigidbody component
        rb = GetComponent<Rigidbody>();

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
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 origin = path[i];
                Vector3 direction = path[i+1] - path[i];
                float dist = Vector3.Distance(path[i], path[i+1]);
                Debug.DrawRay(origin, direction.normalized * dist, Color.green, Mathf.Infinity);
            }

            // Remove the first node from the path (which is itself)
            // Then the first node in the path will be the next waypoint
            path.RemoveAt(0);  // TODO: this is expensive, use a better datastructure
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            flying = true;
        }
    }

    // FixedUpdate is called once per physics update
    void FixedUpdate()
    {
        // Only move when drone is flying
        if (!flying) return;

        // Don't try to move if there is no path
        if (path == null || path.Count == 0) return;

        // If we have reached the current waypoint, select the next node in the path
        float distToWaypoint = Vector3.Distance(transform.position, path[0]);  // FIXME: after removing once, the next waypoint stays the same, so it removes twice
        if (distToWaypoint <= waypointMargin)
        {
            Debug.Log("Reached waypoint");
            path.RemoveAt(0);  // TODO: this is expensive, use a better datastructure
        }

        // Move towards the next waypoint
        if (path.Count > 0)
        {
            Vector3 direction = path[0] - transform.position;
            rb.velocity = direction.normalized * speed;
        }
        else
        {
            rb.velocity = Vector3.zero;
        }
    }
}
