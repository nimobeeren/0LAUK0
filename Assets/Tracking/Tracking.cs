using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Tracking
{
	private string script = "./Assets/Tracking/tracking.py";
	private Process process;
	private Rect lastBbox;

	// Starts a new python process to run the tracking script
    public Tracking()
    {
    	ProcessStartInfo psi = new ProcessStartInfo();
    	psi.FileName = "python3";
    	psi.Arguments = script;
    	psi.UseShellExecute = false;
    	psi.RedirectStandardOutput = true;

    	UnityEngine.Debug.Log("Starting python process");
    	process = new Process();
    	process.OutputDataReceived += (sender, args) => UpdateBbox(args.Data);
    	process.Exited += (sender, args) => UnityEngine.Debug.LogError("Tracker has exited");
    	process.StartInfo = psi;
		process.Start();
		process.BeginOutputReadLine();
    }

    // Parses the tracker output and updates own state
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

    // Simply kills the python process
    public void Stop()
    {
    	if (process != null && !process.HasExited)
    	{
    		process.Kill();
    	}
    }

    public float GetObjectDistance(Camera droneCam, float objectHeight)
    {
    	if (!droneCam.usePhysicalProperties)
    	{
    		UnityEngine.Debug.LogError("Drone camera must have a rectilinear lens for distance computation");
    		return -1;
    	}

    	float focalLength = droneCam.focalLength;
    	float sensorHeight = droneCam.sensorSize.y;
    	float bboxHeight = lastBbox.height;

    	return focalLength / sensorHeight * objectHeight / bboxHeight;
    }
}
