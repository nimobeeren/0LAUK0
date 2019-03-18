using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	IEnumerator ScreenshotLoop()
	{
		while (true)
		{
            /* OLD METHOD
			ScreenCapture.CaptureScreenshot(filename);
            */

            // create screenshot objects if needed
            if (renderTexture == null)
            {
                // creates off-screen render texture that can rendered into
                rect = new Rect(0, 0, captureWidth, captureHeight);
                renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
                screenShot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
            }
        
            // get main camera and manually render scene into rt
            Camera camera = this.GetComponent<Camera>(); // NOTE: added because there was no reference to camera in original script; must add this script to Camera
            camera.targetTexture = renderTexture;
            camera.Render();

            // read pixels will read from the currently active render texture so make our offscreen 
            // render texture active and then read the pixels
            RenderTexture.active = renderTexture;
            screenShot.ReadPixels(rect, 0, 0);

            // reset active camera texture and render texture
            camera.targetTexture = null;
            RenderTexture.active = null;

            // pull in our file header/data bytes for the specified image format (has to be done from main thread)
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
                // create a file header for ppm formatted file
                string headerStr = string.Format("P6\n{0} {1}\n255\n", rect.width, rect.height);
                fileHeader = System.Text.Encoding.ASCII.GetBytes(headerStr);
                fileData = screenShot.GetRawTextureData();
            }

            // create new thread to save the image to file (only operation that can be done in background)
            new System.Threading.Thread(() =>
            {
                // create file and write optional header with image bytes
                var f = System.IO.File.Create(filename + "." + format.ToString().ToLower());
                if (fileHeader != null) f.Write(fileHeader, 0, fileHeader.Length);
                f.Write(fileData, 0, fileData.Length);
                f.Close();
                // Debug.Log(string.Format("Wrote screenshot {0} of size {1}", filename, fileData.Length));
            }).Start();

            // Wait a bit before capturing the next frame
			yield return new WaitForSeconds(1/FPS);
		}
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
        	}
        	else
        	{
        		// Start capturing
	            capturing = true;
	            StartCoroutine("ScreenshotLoop");
	            Debug.Log("Started capture at " + FPS + " FPS");
        	}
        }
    }
}
