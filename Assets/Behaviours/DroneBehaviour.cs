using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneBehaviour : MonoBehaviour
{
    // Parameters
    public GameObject target;  // the object the drone should always move towards
    public float maxSpeed;
    public float maxObstacleDistance;  // obstacles further away than this are ignored

    // Potential field parameters
	public float lambda1;
	public float lambda2;
	public float eta1;
	public float eta2;

    private bool flying;
	private Rigidbody rb;
	private Rigidbody targetRb;
	private GameObject[] obstacles;
	private Rigidbody[] obstacleRbs;
	private Collider[] obstacleColliders;

    void Start()
    {
        // Wait for input before flying
        flying = false;

    	// Get Rigidbody components for self and target
    	rb = GetComponent<Rigidbody>();
    	targetRb = target.GetComponent<Rigidbody>();

    	// Get obstacle GameObjects
        obstacles = GameObject.FindGameObjectsWithTag("Obstacle");

        // Get Rigidbody and Collider components for obstacles
        if (obstacles != null)
        {
	        obstacleRbs = new Rigidbody[obstacles.Length];
	        obstacleColliders = new Collider[obstacles.Length];
	        for (int i = 0; i < obstacles.Length; i++)
	        {
	        	obstacleRbs[i] = obstacles[i].GetComponent<Rigidbody>();
	        	obstacleColliders[i] = obstacles[i].GetComponentInChildren<Collider>();
	        }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            flying = true;
        }
    }

    // FixedUpdate is called once per phyiscs update
    void FixedUpdate()
    {
    	if (!flying) return;

    	// Get some useful values 
        Vector3 dronePos = transform.position;
        Vector3 droneVel = rb.velocity;
        Vector3 targetPos = target.transform.position;
        Vector3 targetVel = targetRb ? targetRb.velocity : Vector3.zero;
        
    	Vector3 attraction = lambda1 * (targetPos - dronePos) + lambda2 * (droneVel - targetVel);
    	Vector3 newVel = attraction;

    	if (obstacles != null)
    	{
		   	for (int i = 0; i < obstacles.Length; i++)
        	{
                if (!obstacleColliders[i]) {
                    // If this obstacle has no collider, we cannot find its distance, so just skip it
                    continue;
                }

        		Vector3 obstaclePos = obstacles[i].transform.position;
        		Vector3 obstacleVel = obstacleRbs[i].velocity;

        		// Calculate distance between center of drone and edge of obstacle
        		Vector3 pointOnObstacle = obstacleColliders[i].ClosestPointOnBounds(dronePos);
        		float dist = Vector3.Distance(dronePos, pointOnObstacle);

        		// If the obstacle is too far away, ignore it
        		if (dist > maxObstacleDistance && maxObstacleDistance >= 0)
        		{
        			continue;
        		}

        		Vector3 repulsion1 = -eta1 / Mathf.Pow(dist, 3) * Vector3.Normalize(obstaclePos - dronePos);
        		Vector3 repulsion2;
        		if (Vector3.Dot(obstacleVel - droneVel, obstaclePos - dronePos) < 0)
        		{
        			// Drone is moving towards obstacle
        			repulsion2 = -eta2 * (obstacleVel - droneVel);
        		}
        		else
        		{
        			// Drone is moving away from obstacle
        			repulsion2 = Vector3.zero;
        		}
		        newVel += repulsion1 + repulsion2;
        	}
    	}
        
        rb.velocity = Vector3.ClampMagnitude(newVel, maxSpeed);
    }
}
