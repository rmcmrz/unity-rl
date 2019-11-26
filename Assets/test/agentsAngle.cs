using UnityEngine;
using System.Collections;

public class agentsAngle : MonoBehaviour {

	private int range,numberOfDirections;

	private GameObject[] directionsState;

	private float[] directionsVector;

	private float[,] directionsMatrix;

	private float step;

	private int rayAngle = 9;

    infopointDynamics infopointDynamicsScript;



    public double[] getRaycast(int maxDistance, int variables)
	{

		int numberOfRays = (int)360/rayAngle;

		GameObject[] arrayOfHits;

		arrayOfHits = new GameObject [numberOfRays];

        double[] stateSpace = new double[numberOfRays * variables];

        for (int i = 0; i < stateSpace.Length; i++)
        {

            stateSpace[i] = 0f;

        }
        

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


        int count = 0;


		foreach (var item in arrayOfHits) {

            if (item != null) {

                double itemDistance = Vector3.Distance(transform.position, item.transform.position);

                if (item.tag == "info")
                {
                    infopointDynamicsScript = item.GetComponent<infopointDynamics>();

                    if (infopointDynamicsScript.disposition == infopointDynamics.possibleDispositions.Good)
                    {

                        stateSpace[variables * count] = 1 - (itemDistance - 1f) / (maxDistance - 1f);

                    }
                    else
                    {

                        stateSpace[variables * count + 1] = 1 - (itemDistance - 1f) / (maxDistance - 1f);

                    }

                    stateSpace[variables * count + 3] = item.GetComponent<Rigidbody>().velocity.x / 10;

                    stateSpace[variables * count + 4] = item.GetComponent<Rigidbody>().velocity.z / 10;

                }
                else if (item.tag == "wall")
                {
                    stateSpace[variables * count + 2] = 1 - (itemDistance - 1f) / (maxDistance - 1f);

                }


               // Debug.Log ("----------   " +item.transform.tag + " " + count + " " + distance);
			}

            count++;
		}

        //Debug.Log ("next");


        //printArrayDouble(stateSpace);


        return stateSpace;

	}


	bool TestRange (float numberToCheck, float bottom, float top)
	{
		return (numberToCheck > bottom && numberToCheck <= top);
	}



	float  SignedAngleBetween(Vector3 a, Vector3 b){

		float rotationAngle = transform.rotation.eulerAngles.y;
			
		float angle = ( (Mathf.Atan2 (b.z - a.z, b.x - a.x) / Mathf.PI ) + (rotationAngle * Mathf.PI / 180)/ Mathf.PI);

		if (angle > 1) {

			angle -= 2f;

		}


		Debug.Log ("a: " + angle + " rotation: " + rotationAngle * Mathf.PI / 180 + " - " + Mathf.Atan2(b.z-a.z, b.x-a.x));


		return angle;
	}

	void getState(int numberOfDirections,int range)
	{

		float distance = 0f;

		directionsState = new GameObject[numberOfDirections];

		Collider[] cols = Physics.OverlapSphere(transform.position, range);
		int q = cols.Length;

		foreach (var colider in cols) {

			distance = Vector3.Distance (transform.position, colider.transform.position);

			if (distance > 0.2f && distance < range) {


				for (int i = 0; i < directionsMatrix.GetLength (0); i++) {

					if (TestRange (SignedAngleBetween (transform.position, colider.transform.position), directionsMatrix [i, 0], directionsMatrix [i, 1])) {


						Debug.Log ("f: " + i);

						if (directionsState [i] != null) {



							if (distance < Vector3.Distance (transform.position, directionsState [i].transform.position)) {


								Debug.Log (distance + " ---- " + Vector3.Distance (transform.position, directionsState [i].transform.position));

								directionsState [i] = colider.transform.gameObject;

							}

						} else {

							directionsState [i] = colider.transform.gameObject;

						}

					}


				}



				//Debug.Log (distance);

				//SignedAngleBetween (transform.position, colider.transform.position);
			

			}

		}

		//Debug.Log (q);

	}


	// Use this for initialization
	void Start () {

		range = 45;

		numberOfDirections = 8;

		directionsState = new GameObject[numberOfDirections];

		directionsVector = new float[numberOfDirections + 2];

		directionsMatrix = new float[numberOfDirections,2];

		step = (float) 1 / ( numberOfDirections / 2 ) ;

		Debug.Log ("step: " + step);


		//Debug.Log (TestRange (-0.5f, -0.5f, -0.25f ));


		for (int i = 0; i < numberOfDirections + 2; i++) {



			if (i <= numberOfDirections / 2) {

				//Debug.Log ("number " + i * step);

				directionsVector [i] = i * step;

			} else {

				//Debug.Log ("number2 " + (i * step - ( 2 + step )));

				directionsVector [i] = i * step - (2 + step);

			}


			//Debug.Log (i);

		}
			


		for (int i = 0; i < directionsMatrix.GetLength(0); i++)
		{

			if (i < numberOfDirections / 2) {
				directionsMatrix [i, 0] = directionsVector [i];

				directionsMatrix [i, 1] = directionsVector [i + 1];
			} else {

				directionsMatrix [i, 0] = directionsVector [i + 1];

				directionsMatrix [i, 1] = directionsVector [i + 2];

			}


			for (int j = 0; j < directionsMatrix.GetLength(1); j++)
			{
				float s = directionsMatrix[i, j];

				Debug.Log (i + "-"+ j + " " + s);
			}
		}


		foreach (float item in directionsVector) {

			//Debug.Log (item);
	
		}



	}
	// Update is called once per frame
	void Update () {

        getRaycast (10,5);

        //getState(16, 10);

	}

    private void printArrayDouble(double[] array)

    {

        string row = "";

        foreach (var item in array)
        {

            row += "[" + System.Math.Round((decimal)item, 2).ToString() + "]";

        }



        Debug.Log(row);

    }
}