using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class raycast : MonoBehaviour {

    public double[] stateVector;

    private int rayAngle = 12;

    private infopointDynamics infopointDynamicsScript;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        stateVector = getRaycast(12, 5);
		
	}


    public double[] getRaycast(int maxDistance, int variables)
    {

        int numberOfRays = (int)360 / rayAngle;

        GameObject[] arrayOfHits;

        arrayOfHits = new GameObject[numberOfRays];

        double[] stateSpace = new double[numberOfRays * variables];

        int numberMatrix = numberOfRays * variables;

        //Debug.Log(stateSpace.Length);

        for (int i = 0; i < stateSpace.Length; i++)
        {

            stateSpace[i] = 0f;

        }

        for (int i = 0; i < numberOfRays; i++)
        {

            stateSpace[i * variables] = 1f;
            stateSpace[i * variables + 1] = 1f;
            stateSpace[i * variables + 2] = 1f;

        }


        RaycastHit hit;

        int index = 0;

        while (index < numberOfRays)
        {

            //cast a ray in the current direction.
            if (Physics.Raycast(transform.position, transform.forward, out hit, maxDistance))
            {
                Debug.DrawLine(transform.position, hit.point, Color.cyan);
                //Distance To the wall
                //distanceToWalls [index] = Vector3.Distance (transform.position, hit.point);

                arrayOfHits[index] = hit.transform.gameObject;

            }
            else
            {

                arrayOfHits[index] = null;
            }

            transform.Rotate(0, rayAngle, 0);

            index += 1;

        }


        int count = 0;


        foreach (var item in arrayOfHits)
        {

            if (item != null)
            {

                double itemDistance = Vector3.Distance(transform.position, item.transform.position);

                if (item.tag == "info")
                {
                    infopointDynamicsScript = item.GetComponent<infopointDynamics>();

                    if (infopointDynamicsScript.disposition == infopointDynamics.possibleDispositions.Good)
                    {

                        stateSpace[variables * count] = itemDistance / maxDistance;

                    }
                    else
                    {

                        stateSpace[variables * count + 1] = itemDistance / maxDistance;

                    }

                    stateSpace[variables * count + 3] = item.GetComponent<Rigidbody>().velocity.x / 5;

                    stateSpace[variables * count + 4] = item.GetComponent<Rigidbody>().velocity.z / 5;

                }
                else if (item.tag == "wall")
                {
                    stateSpace[variables * count + 2] = itemDistance / maxDistance;

                }


                // Debug.Log ("----------   " +item.transform.tag + " " + count + " " + distance);
            }

            count++;
        }


        //Debug.Log ("next");


        //printArrayDouble(stateSpace);

        //stateSpace[numberMatrix] = gameObject.GetComponent<Rigidbody>().velocity.x / 10;

        //stateSpace[numberMatrix + 1] = gameObject.GetComponent<Rigidbody>().velocity.z / 10;


        return stateSpace;

    }
}
