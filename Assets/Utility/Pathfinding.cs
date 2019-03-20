using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    // The position of the node in 3D space
    public Vector3 position;

    // All nodes that can be reached from this one
    public List<Node> neighbors;

    // The previous node in the path
    public Node cameFrom = null;

    // The cost of getting from the start to this node
    public float gScore = Mathf.Infinity;

    // The cost of getting from the start to the goal while passing by this node
    // This value is partly known, partly heuristic
    public float fScore = Mathf.Infinity;

    public Node(Vector3 pos)
    {
        position = pos;
        neighbors = new List<Node>();
    }

    /**
    Finds the distance between two nodes.
    */
    public static float Distance(Node v1, Node v2)
    {
        return Vector3.Distance(v1.position, v2.position);
    }
}

public static class Pathfinding
{
    /**
    Traces back the path from the starting node to the current node when A* is finished.
    */
    static List<Node> ReconstructPath(Node current)
    {
        List<Node> path = new List<Node>();
        path.Add(current);
        while (current.cameFrom != null)
        {
            current = current.cameFrom;
            path.Add(current);
        }
        path.Reverse();
        return path;
    }

    /**
    Finds the shortest path between two nodes in a graph defined a set of neighbor nodes for each node.
    */
    static List<Node> AStar(Node start, Node goal)
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

                float tentativeGScore = current.gScore + Node.Distance(current, neighbor); // the distance from start to a neighbor

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

    /**
    Finds out what nodes have line of sight to eachother, and sets their neighbor variables accordingly.
    */
    static void SetNeighbors(List<Node> nodes)
    {
        // For each node, find its neighbors
        // Two nodes are neighbors iff they have line of sight to eachother
        foreach (Node v in nodes)
        {
            foreach (Node u in nodes)
            {
                if (v == u) continue;
                Vector3 origin = v.position;
                Vector3 direction = u.position - v.position;
                float dist = Node.Distance(v, u);
                int layerMask = ~(1 << 9);  // we can see through objects in Debug layer (9)
                if (!Physics.Raycast(origin, direction, dist, layerMask))
                {
                    // Nodes have line of sight to eachother
                    v.neighbors.Add(u);
                    u.neighbors.Add(v);
                    Debug.DrawRay(origin, direction.normalized * dist, Color.yellow, Mathf.Infinity);
                }
            }
        }
    }

    /**
    Finds the shortest path between two gameobjects that is free of collisions, using NavNodes as waypoints.
    */
    public static List<Vector3> GetShortestPath(GameObject startObj, GameObject goalObj)
    {
        return GetShortestPath(startObj.transform.position, goalObj.transform.position);
    }

    /**
    Finds the shortest path between two points that is free of collisions, using NavNodes as waypoints.
    */
    public static List<Vector3> GetShortestPath(Vector3 start, Vector3 goal)
    {
        // Create list that will contain all nodes in the pathfinding graph
        List<Node> nodes = new List<Node>();
        
        // Create nodes for NavNodes
        GameObject[] nodeObjs = GameObject.FindGameObjectsWithTag("NavNode");
        foreach (GameObject obj in nodeObjs)
        {
            nodes.Add(new Node(obj.transform.position));
        }

        // Create nodes for start and goal
        Node startNode = new Node(start);
        Node goalNode = new Node(goal);
        nodes.Add(startNode);
        nodes.Add(goalNode);

        // Determine what nodes are neighbors based on line of sight
        SetNeighbors(nodes);

        // Find the optimal path
        List<Node> path = AStar(startNode, goalNode);

        // Return the path
        if (path == null)
        {
            return null;
        }
        else
        {
            // Return points in path
            List<Vector3> pathPoints = new List<Vector3>();
            foreach (Node v in path)
            {
                pathPoints.Add(v.position);
            }
            return pathPoints;
        }
    }
}
