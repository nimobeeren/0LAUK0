using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavNodeBehaviour : MonoBehaviour
{
    void OnDrawGizmos()
    {
    	Gizmos.color = Color.magenta;
    	Gizmos.DrawCube(transform.position, 0.15f * new Vector3(1, 1, 1));
    }
}
