﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUIBehaviour : MonoBehaviour
{
	public int xPos = 10;
	public int yPos = 10;
	public int width = 200;
	public int lineHeight = 20;
    public int margin = 5;

	private Dictionary<string, string> vars;

    // Start is called before the first frame update
    void Start()
    {
        vars = new Dictionary<string, string>();
    }

    public void setVar(string key, string value)
    {
    	vars[key] = value;
    }

	public void setVar(string key, float value)
	{
		setVar(key, value.ToString());
	}

    void OnGUI()
    {
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
}