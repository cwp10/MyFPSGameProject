using UnityEngine;
using System.Collections;

public class SunControl : MonoBehaviour {

    private Transform myTrans;
    private float rotX;
    // Use this for initialization
    void Start () {
        myTrans = this.GetComponent<Transform>();
        rotX = -100.0f;
    }
	
	// Update is called once per frame
	void Update () {
        rotX += 1f * Time.deltaTime;
        myTrans.rotation = Quaternion.Euler(rotX, myTrans.rotation.y, myTrans.rotation.z);
    }
}
