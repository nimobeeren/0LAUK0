using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script will save individual images of active scene in any resolution and of a specific image format
// including raw, jpg, png, and ppm.  Raw and PPM are the fastest image formats for saving.
//
// You can compile these images into a video using ffmpeg:
// ffmpeg -i screen_3840x2160_%d.ppm -y test.avi
//
// Based on a script by toddmoore (http://answers.unity.com/answers/1296574/view.html)

public class CaptureBehaviour : MonoBehaviour
{
	public string filename;
	public KeyCode captureKey;
    public int captureWidth = 1920;
    public int captureHeight = 1080;
    public enum Format { RAW, JPG, PNG, PPM };
    public Format format = Format.PPM; // configure with raw, jpg, png, or ppm (simple raw format)
    public float FPS;

	private bool capturing = false;
    private Rect rect;
    private RenderTexture renderTexture;
    private Texture2D screenShot;
    private string oldName;
    private string newName;

	IEnumerator ScreenshotLoop()
	{
		while (true)
		{
            /* OLD METHOD
			ScreenCapture.CaptureScreenshot(filename);
            */

            // Create screenshot objects if needed
            if (renderTexture == null)
            {
                // Creates off-screen render texture that can rendered into
                rect = new Rect(0, 0, captureWidth, captureHeight);
                renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
                screenShot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
            }
        
            // Get main camera and manually render scene into rt
            Camera camera = this.GetComponent<Camera>(); // NOTE: added because there was no reference to camera in original script; must add this script to Camera
            camera.targetTexture = renderTexture;
            camera.Render();

            // Read pixels will read from the currently active render texture so make our offscreen 
            // Render texture active and then read the pixels
            RenderTexture.active = renderTexture;
            screenShot.ReadPixels(rect, 0, 0);

            // Reset active camera texture and render texture
            camera.targetTexture = null;
            RenderTexture.active = null;

            // Pull in our file header/data bytes for the specified image format (has to be done from main thread)
            byte[] fileHeader = null;
            byte[] fileData = null;
            if (format == Format.RAW)
            {
                fileData = screenShot.GetRawTextureData();
            }
            else if (format == Format.PNG)
            {
                fileData = screenShot.EncodeToPNG();
            }
            else if (format == Format.JPG)
            {
                fileData = screenShot.EncodeToJPG();
            }
            else // ppm
            {
                // Create a file header for ppm formatted file
                string headerStr = string.Format("P6\n{0} {1}\n255\n", rect.width, rect.height);
                fileHeader = System.Text.Encoding.ASCII.GetBytes(headerStr);
                fileData = screenShot.GetRawTextureData();
            }

            // create new thread to save the image to file (only operation that can be done in background)
            new System.Threading.Thread(() =>
            {
                // Create file and write optional header with image bytes
                var f = System.IO.File.Create(newName);
                if (fileHeader != null) f.Write(fileHeader, 0, fileHeader.Length);
                f.Write(fileData, 0, fileData.Length);
                f.Close();
                // Debug.Log(string.Format("Wrote screenshot {0} of size {1}", filename, fileData.Length));

                // Replace old image by new one
                System.IO.File.Replace(newName, oldName, null);
            }).Start();

            // Wait a bit before capturing the next frame
			yield return new WaitForSeconds(1/FPS);
		}
	}

    void Start()
    {
        // Define two filenames
        oldName = filename + "." + format.ToString().ToLower();      // file to use for tracking
        newName = filename + "-new." + format.ToString().ToLower();  // file to write to
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(captureKey))
        {
        	if (FPS <= 0)
        	{
        		// Take a single screenshot
        		ScreenCapture.CaptureScreenshot(filename + ".png");
        	}
        	else if (capturing)
        	{
        		// Stop capturing
        		capturing = false;
        		StopCoroutine("ScreenshotLoop");
        		Debug.Log("Stopped capture");

                // Delete the files
                System.IO.File.Delete(oldName);
                System.IO.File.Delete(newName);
        	}
        	else
        	{
                // Create the files
                System.IO.File.Create(oldName).Close();
                System.IO.File.Create(newName).Close();
                
                // Start capturing
	            capturing = true;
	            StartCoroutine("ScreenshotLoop");
	            Debug.Log("Started capture at " + FPS + " FPS");
        	}
        }
    }
}
