using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserBehaviour : MonoBehaviour
{
	public float speed = 2;
	public float rotationSpeed = 100;

	private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
    	// Move forwards/backwards and strafe
        rb.velocity = speed * Input.GetAxis("Vertical") * transform.forward;
        rb.velocity += speed * Input.GetAxis("Horizontal") * transform.right;

        // Rotate the user with Q and E
        float rotation = 0;
        if (Input.GetKey(KeyCode.Q)) rotation = -1;
        else if (Input.GetKey(KeyCode.E)) rotation = 1;
        transform.Rotate(new Vector3(0, rotation, 0) * Time.deltaTime * rotationSpeed, Space.World);
    }
}
