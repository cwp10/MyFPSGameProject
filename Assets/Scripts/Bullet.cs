using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour {

    private float speed = 2500.0f;

	// Use this for initialization
	void Start () {
        Rigidbody rigidbody = GetComponent<Rigidbody>();
        rigidbody.AddRelativeForce(Vector3.forward * speed);
        Destroy(this.gameObject, 0.1f);
    }

    void OnTriggerEnter(Collider other) {
        Destroy(this.gameObject);
    }
}
