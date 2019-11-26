using UnityEngine;
using System.Collections;
using Apex.Steering.Components;
using Apex.Steering.Behaviours;

//The main controller script that encapuslates all other behavior scripts

[System.Serializable]
public class simulationAgentEncapsulator : MonoBehaviour {

    public enum personalityType { Introverted, Extroverted };
    public personalityType assignedPersonality;

	public enum personalityTypeSecond { Suspicious, Cooperative };
	public personalityTypeSecond assignedPersonalitySecond;

	public enum agentsMood { Positive, Negative, Neutral };
	public agentsMood currentMood;

	public float socialEnergy;

	public float socialEnergyMax;

	public float socialEnergyTreshold;

	public float socialEnergyStepHigh;

	public float socialEnergyStepLow;

    private HumanoidSpeedComponent humanoidSpeedScript;

	private AgentBehaviour AgentBehaviourScript;

    // Use this for initialization
    void Start () {

    socialEnergy = 0.0f;

	socialEnergyMax = 30f;

	socialEnergyTreshold = - (socialEnergyMax - 5f);

    socialEnergyStepHigh = 5f;

	socialEnergyStepLow = socialEnergyStepHigh / 4;


    //set defaults on initialization

    currentMood = agentsMood.Neutral;

        //get other objects scripts for initialization of parameters/behavioral characteristics

        //humanoidSpeedScript = GetComponent<HumanoidSpeedComponent>();
		AgentBehaviourScript = GetComponent<AgentBehaviour>();


		//setting the defaults for personality type of an agent

		if (assignedPersonality == personalityType.Introverted) {

			//humanoidSpeedScript.maximumSpeed = 5000;
			//humanoidSpeedScript.walkSpeed = 50f;
			//AgentBehaviourScript.radiusWander = 150f;
			//AgentBehaviourScript.lingerForSeconds= 20f;

			//Debug.Log (assignedPersonality.ToString ());
		} else {
			//humanoidSpeedScript.maximumSpeed = 5000;
			//humanoidSpeedScript.walkSpeed = 50f;
			//AgentBehaviourScript.radiusWander = 150f;
			//AgentBehaviourScript.lingerForSeconds = 20f;

		}



    }
	
	// Update is called once per frame
	void Update () {

        


    }
	/*
	void communicateAction() {


		if (assignedPersonality == personalityType.Introverted) {

			socialEnergy -= socialEnergyDecrement;

		}


	}

*/




}
