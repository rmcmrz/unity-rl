using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class infopointDynamics : MonoBehaviour {


	public int capacity,maxCapacity;

	public enum possibleDispositions { Good , Bad };

	public possibleDispositions disposition;

	public bool isGoal = false;

	private Renderer rend;

	private placeFood generatorScript;

	List<string> agentsConsumed;

	// Use this for initialization
	void Start () {

		capacity = 5;

		maxCapacity = capacity;


		rend = GetComponent<Renderer>();
	

        if(disposition == possibleDispositions.Good)
		rend.material.color = Color.HSVToRGB (0.55f, 1, 0.8f);
        else
        rend.material.color = Color.HSVToRGB(0.1f, 1, 0.8f);


        agentsConsumed = new List<string>();

		GameObject camera = GameObject.Find ("ExampleGround");

		generatorScript = camera.GetComponent<placeFood>();


	}
	
	// Update is called once per frame
	void Update () {
	
	}



	public string eatFood(string idOfAgent)
	{

		if (agentsConsumed.Contains (idOfAgent)) {

			return "consumed";

		} else {

			agentsConsumed.Add (idOfAgent);

		}

		if (capacity != 0) {

			capacity -= 1;

		} else {
			
			return "consumed";

		}

		generatorScript.score += 1;

        if(disposition == possibleDispositions.Good)
		rend.material.color = Color.HSVToRGB (0.55f, (float) capacity / maxCapacity, 0.8f);
        else
        rend.material.color = Color.HSVToRGB(0.1f, (float)capacity / maxCapacity, 0.8f);

        if (disposition == possibleDispositions.Good) {
			
			return "positive";
		}
		else
			return "negative";

	}
		

}