using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBehaviour : MonoBehaviour
{
    void Update()
    {
    	// Quit application when escape is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
        	Application.Quit();
        }
    }
}
