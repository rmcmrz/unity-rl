using UnityEngine;
using System.Collections;

public class testAgent : MonoBehaviour {

	int rayAngle;

	// Use this for initialization
	void Start () {

		rayAngle = 6;
	
	}
	
	// Update is called once per frame
	void Update () {

		getRaycast (50);
	
	}



	public void getRaycast(int maxDistance)
	{

		int numberOfRays = (int)360/rayAngle;

		GameObject[] arrayOfHits;

		arrayOfHits = new GameObject [numberOfRays];

		RaycastHit hit;

		int index = 0;

		while (index < numberOfRays) {

			//cast a ray in the current direction.
			if (Physics.Raycast (transform.position, transform.forward, out hit, maxDistance)) {
				Debug.DrawLine (transform.position, hit.point, Color.cyan);
				//Distance To the wall
				//distanceToWalls [index] = Vector3.Distance (transform.position, hit.point);

				arrayOfHits [index] = hit.transform.gameObject;

			} else {

				arrayOfHits [index] = null;
			}

			transform.Rotate(0,rayAngle,0);

			index += 1;

		}





		foreach (var item in arrayOfHits) {

			if (item!= null) {

				//Debug.Log ("----------   " +item.transform.tag);
			}
		}

		//Debug.Log ("------");




	}
}
