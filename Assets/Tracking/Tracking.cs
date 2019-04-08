using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Tracking
{
	public Rect lastBbox;

    public Tracking()
    {
    	ProcessStartInfo psi = new ProcessStartInfo();
    	psi.FileName = "/usr/bin/python3";  // TODO: make platform independent
    	psi.Arguments = "/home/nimo/Development/Unity/0LAUK0/Assets/Tracking/tracking.py";  // TODO: use relative path
    	psi.UseShellExecute = false;
    	psi.RedirectStandardOutput = true;

    	UnityEngine.Debug.Log("Starting python process");
    	Process process = new Process();
    	process.OutputDataReceived += (sender, args) => UpdateBbox(args.Data);
    	process.StartInfo = psi;
		process.Start();
		process.BeginOutputReadLine();
    }

    void UpdateBbox(string trackerOutput)
    {
    	// Parse tracker output to create a Rect object
    	string[] result = trackerOutput.Split(new char[] {'(', ')', ','}, StringSplitOptions.RemoveEmptyEntries);
    	float x = float.Parse(result[0]);
    	float y = float.Parse(result[1]);
    	float w = float.Parse(result[2]);
    	float h = float.Parse(result[3]);
    	lastBbox = new Rect(x, y, w, h);

    	UnityEngine.Debug.Log(lastBbox);
    }
}
