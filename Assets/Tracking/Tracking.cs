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
    }

    // Simply kills the python process
    public void Stop()
    {
    	if (process != null && !process.HasExited)
    	{
    		process.Kill();
    	}
    }

    // Computes the distance between camera and object, using the bounding box
    public float GetObjectDistance(Camera camera, float objectHeight)
    {
    	if (lastBbox == null)
    	{
    		return 0;
    	}
    	if (!camera.usePhysicalProperties)
    	{
    		UnityEngine.Debug.LogError("Camera must have a rectilinear lens for distance computation");
    		return -1;
    	}

    	float focalLength = camera.focalLength;
    	float sensorHeight = camera.sensorSize.y;
    	float bboxHeight = lastBbox.height;

    	return focalLength / sensorHeight * objectHeight / bboxHeight;
    }

    // Returns a unit vector in the direction of the object, seen from the camera's position
    public Vector3 GetObjectDirection(Camera camera, Transform camTransform)
    {
    	// Get the horizontal angle of the object in the camera view
    	float fovDeg = camera.fieldOfView * camera.aspect;
    	float avgX = (2 * lastBbox.x + lastBbox.width) / 2;
    	float objAngle = Mathf.Deg2Rad * Mathf.Lerp(fovDeg / 2, -fovDeg / 2, avgX);

    	// Take the camera's forward vector and rotate it by the found angle
    	Vector3 forward = camTransform.forward;
    	forward.y = 0;  // project onto the horizontal plane
    	return new Vector3(
            forward.x * Mathf.Cos(objAngle) - forward.z * Mathf.Sin(objAngle),
            0,
            forward.x * Mathf.Sin(objAngle) + forward.z * Mathf.Cos(objAngle)
        );
    }

    // Gets the position of the object relative to the camera
    public Vector3 GetObjectPosition(GameObject cameraObj, float objectHeight)
    {
    	Camera camera = cameraObj.GetComponent<Camera>();
    	Transform camTransform = cameraObj.transform;

    	if (lastBbox.height == 0 && lastBbox.width == 0)
    	{
    		UnityEngine.Debug.LogWarning("Tracking not yet initalized or failed");
    		return Vector3.zero;
    	}

    	if (camera == null)
    	{
    		UnityEngine.Debug.LogError("GameObject must have a camera component to determine object position");
    		return Vector3.zero;
    	}

    	float dist = GetObjectDistance(camera, objectHeight);
    	Vector3 dir = GetObjectDirection(camera, camTransform);
    	return dist * dir;
    }
}
