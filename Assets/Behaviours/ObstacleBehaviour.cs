using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleBehaviour : MonoBehaviour
{
	public float speed;
	public float period;

	private Rigidbody rb;
	private Vector3 direction;

	IEnumerator Bounce()
	{
		while (true)
		{
			rb.velocity = speed * direction;
			yield return new WaitForSeconds(period / 2);
			direction *= -1;
		}
		
	}

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // direction = transform.InverseTransformDirection(Vector3.right);
        direction = transform.right.normalized;
        // StartCoroutine("Bounce");
    }

    void FixedUpdate()
    {
    	rb.velocity = speed * Mathf.Sin(2 * Mathf.PI / period * Time.time) * direction;
    }
}
