using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestinationBehaviour : MonoBehaviour
{
    void OnDrawGizmos()
    {
    	Gizmos.color = Color.green;
    	Gizmos.DrawSphere(transform.position, 0.15f);
    }
}
