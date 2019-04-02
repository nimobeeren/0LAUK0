using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUIBehaviour : MonoBehaviour
{
    /* Visual parameters for inspector */
	public int xPos = 10;
	public int yPos = 10;
	public int width = 200;
	public int lineHeight = 20;
    public int margin = 5;

    /* Data structure to hold key,value pairs used for debugging */
	private Dictionary<string, string> vars;

    /* Debug variables for pathfinding */
    private List<Node> nodes;
    private List<Vector3> path;

    /* Debug variables for target */
    private Vector3 targetPos;
    private Vector3 userDirection;
    private Vector3 userDirection0;

    /* Which debug information to show */
    private bool showTarget = false;
    private bool showPathfinding = false;
    private bool showInspector = false;

    void Start()
    {
        vars = new Dictionary<string, string>();
    }

    void Update()
    {
        // Read input to toggle showing of various debug information
        if (Input.GetKeyDown(KeyCode.F9))
        {
            showPathfinding = !showPathfinding;
        }
        if (Input.GetKeyDown(KeyCode.F11))
        {
            showTarget = !showTarget;
        }
        if (Input.GetKeyDown(KeyCode.F12))
        {
            showInspector = !showInspector;
        }

        if (showPathfinding)
        {
            if (nodes != null && nodes.Count > 0)
            {
                foreach (Node v in nodes)
                {
                    foreach (Node u in v.neighbors)
                    {
                        Debug.DrawLine(v.position, u.position, Color.yellow);
                    }
                }
            }
            if (path != null)
            {
                // Draw a line along the path
                for (int i = 0; i < path.Count - 1; i++)
                {
                    Debug.DrawLine(path[i], path[i+1], Color.green);
                }
            }
        }

        // Draw visual debug information for target
        if (showTarget)
        {
            Debug.DrawRay(targetPos, userDirection, Color.red);
            Debug.DrawRay(targetPos, userDirection0, Color.white);
        }
    }

    void OnGUI()
    {
        if (!showInspector) return;
        
        // Draw box
        GUI.Box(new Rect(xPos, yPos, width + 2 * margin, (vars.Count + 1) * lineHeight + 2 * margin), "Inspector");

        // Draw variables as text
        int y = yPos + margin + lineHeight;
        foreach (string key in vars.Keys)
        {
            GUI.Label(new Rect(xPos + margin, y, width, lineHeight), key + ": " + vars[key]);
            y += lineHeight;
        }
    }

    public void SetPathfindingInfo(List<Node> nodes, List<Vector3> path)
    {
        this.nodes = nodes;
        this.path = path;
    }

    public void SetTargetInfo(Vector3 targetPos, Vector3 userDirection, Vector3 userDirection0)
    {
        this.targetPos = targetPos;
        this.userDirection = userDirection;
        this.userDirection0 = userDirection0;
    }

    public void SetVar(string key, string value)
    {
    	vars[key] = value;
    }

	public void SetVar(string key, float value)
	{
		SetVar(key, value.ToString());
	}
}
