using UnityEngine;
using System.Collections;

public class infopointAnimation : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}

	float tCycle;

	void Update(){
			transform.Rotate(0,180*Time.deltaTime,0);
	}

}
