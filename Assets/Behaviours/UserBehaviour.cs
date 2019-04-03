using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserBehaviour : MonoBehaviour
{
	public float speed = 2;
	public float rotationSpeed = 100;

	private CharacterController controller;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Rotate the user with Q and E
        float rotation = 0;
        if (Input.GetKey(KeyCode.Q)) rotation = -1;
        else if (Input.GetKey(KeyCode.E)) rotation = 1;
        transform.Rotate(new Vector3(0, rotation, 0) * Time.deltaTime * rotationSpeed, Space.World);

        // Move forward/backward/left/right
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        float forwardSpeed = speed * Input.GetAxis("Vertical");
        float sidewaysSpeed = speed * Input.GetAxis("Horizontal");
        controller.SimpleMove(Vector3.ClampMagnitude(forward * forwardSpeed + right * sidewaysSpeed, speed));
    }
}
