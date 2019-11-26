using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectBuilderScript : MonoBehaviour 
{


	private GameObject[] agents;


	public void BuildObject()
	{

		agents = GameObject.FindGameObjectsWithTag ("agent");

		Debug.Log (agents.Length);

	}
}