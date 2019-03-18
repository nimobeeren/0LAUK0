using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    // The GameObject that represents this node
    public GameObject obj;

    // All nodes that can be reached from this one
    public List<Node> neighbors;

    // The previous node in the path
    public Node cameFrom = null;

    // The cost of getting from the start to this node
    public float gScore = Mathf.Infinity;

    // The cost of getting from the start to the goal while passing by this node
    // This value is partly known, partly heuristic
    public float fScore = Mathf.Infinity;

    // A shorthand for obj.transform.position
    public Vector3 Position
    {
        get { return obj.transform.position; }
        set { obj.transform.position = value; }
    }

    public Node(GameObject obj)
    {
        this.obj = obj;
        neighbors = new List<Node>();
    }

    public static float Distance(Node v1, Node v2)
    {
        return Vector3.Distance(v1.Position, v2.Position);
    }
}

public class TargetBehaviour : MonoBehaviour
{
	public GameObject destination;  // the final destination of the drone
	public float speed;				// the (constant) speed at which the target moves to the destination
	public float waypointMargin;	// the maximum distance at which a waypoint is considered 'reached'

	private bool flying = false;	// whether the drone is currently flying
	private Rigidbody rb;			// the target's rigidbody
    private List<Node> navNodes;    // waypoints used for pathfinding
    private Node nextNode;          // the node that the target is currently moving towards
    private List<Node> path;		// the calculated path from the starting point to the destination

    List<Node> ReconstructPath(Node current)
    {
        List<Node> path = new List<Node>();
        path.Add(current);
        while (current.cameFrom != null)
        {
            current = current.cameFrom;
            path.Add(current);
        }
        return path;
    }

    List<Node> AStar(Node start, Node goal)
    {
        // The set of nodes already evaluated
        List<Node> closedSet = new List<Node>();  // TODO: use HashSet

        // The set of currently discovered nodes that are not evaluated yet
        List<Node> openSet = new List<Node>();
        openSet.Add(start);

        // The cost of going from start to start is zero
        start.gScore = 0;

        // For the first node, the cost to get to the goal is estimated using the heuristic
        start.fScore = Node.Distance(start, goal);

        while (openSet.Count > 0)
        {
            // Find the node with lowest fScore
            Node current = null;
            float maxF = Mathf.Infinity;
            foreach (Node v in openSet)
            {
                if (current == null || v.fScore < maxF)
                {
                    current = v;
                    maxF = v.fScore;
                }
            }

            if (current == goal)
            {
                return ReconstructPath(current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Node neighbor in current.neighbors)
            {
                if (closedSet.Contains(neighbor)) continue;  // ignore neighbors that have already been evaluated

                // The distance from start to a neighbor
                float tentativeGScore = current.gScore + Node.Distance(current, neighbor);

                if (!openSet.Contains(neighbor))  // discover a new node
                {
                    openSet.Add(neighbor);
                }
                else if (tentativeGScore >= neighbor.gScore)
                {
                    continue;
                }

                // This path is the best yet, record it!
                neighbor.cameFrom = current;
                neighbor.gScore = tentativeGScore;
                neighbor.fScore = neighbor.gScore = Node.Distance(neighbor, goal);
            }
        }

        // We failed to find a path from start to goal
        return null;
    }

    // Start is called before the first frame update
    void Start()
    {
    	// Get our own Rigidbody component
    	rb = GetComponent<Rigidbody>();

        // Get NavNodes
        navNodes = new List<Node>();
        GameObject[] nodeObjs = GameObject.FindGameObjectsWithTag("NavNode");
        foreach (GameObject obj in nodeObjs)
        {
            navNodes.Add(new Node(obj));
        }

        // Create nodes for itself and destination
        Node start = new Node(gameObject);
        Node goal = new Node(destination);
        navNodes.Add(start);
        navNodes.Add(goal);

        // For each node, find its neighbors
        // Two nodes are neighbors iff they have line of sight to eachother
        foreach (Node v in navNodes)
        {
            foreach (Node u in navNodes)
            {
                if (v == u) continue;
                Vector3 origin = v.Position;
                Vector3 direction = u.Position - v.Position;
                float dist = Node.Distance(v, u);
                int layerMask = ~(1 << 9);  // we can see through objects in Debug layer (9)
                if (!Physics.Raycast(origin, direction, dist, layerMask))
                {
                    // Nodes have line of sight to eachother
                    v.neighbors.Add(u);
                    u.neighbors.Add(v);
                    Debug.DrawRay(origin, direction.normalized * dist, Color.green, Mathf.Infinity);
                }
            }
        }

        // Find the optimal path and show it
        path = AStar(start, goal);
        if (path == null)
        {
        	Debug.Log("Failed to find a valid path");
        }
        else
        {
        	// Draw a line along the path
        	Debug.Log(path[0].Position);
	        for (int i = 0; i < path.Count - 1; i++)
	        {
	        	Debug.Log(path[i+1].Position);
	        	Node u = path[i];
	        	Node v = path[i+1];
	        	Vector3 origin = v.Position;
	        	Vector3 direction = u.Position - v.Position;
	        	float dist = Vector3.Distance(u.Position, v.Position);
	        	Debug.DrawRay(origin, direction.normalized * dist, Color.red, Mathf.Infinity);
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
    	// Don't try to move if there is no path
    	if (path == null || path.Count == 0) return;

    	// Only move when drone is flying
    	if (!flying) return;

    	// If we have reached the current waypoint, select the next node in the path
    	float distToWaypoint = Vector3.Distance(transform.position, path[0].Position);  // FIXME: after removing once, the next waypoint stays the same, so it removes twice
    	Debug.Log(distToWaypoint);
    	if (distToWaypoint <= waypointMargin)
    	{
    		Debug.Log("removing");
    		path.RemoveAt(0);  // TODO: this is expensive, use a better datastructure
    		Debug.Log("next waypoint: " + path[0].Position.ToString());
    	}

    	// Move towards the next waypoint
    	Vector3 direction = path[0].Position - transform.position;
    	rb.velocity = direction.normalized * speed;
    }
}
