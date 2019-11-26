using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Apex.Messages;
using Apex.Services;
using Apex.Steering.Components;
using Apex.Units;
using Apex.WorldGeometry;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class testAngleNew : MonoBehaviour {

    private bool useNeuralNetworks = true;

    private int range, interactRange;

    public int numberOfDirections;

    private GameObject[] directionsState;

    private double[] directionsVector;

    public double[,] directionsMatrix;

    private double[,] stateMatrix;

    public double[] stateMatrixFeatures;

    public double[] onedimensionalFeatures;

    public double[] previousfeaturesVector;

    public double[] weightsVector;

    private float step;

    private int numberOfAgents;

    private int numberOfAgentse;

    private int numberOfAgentsi;

    private int numberOfAgentsc;

    private int numberOfAgentss;

    private int numberofGoodInfopoints;

    private int numberofBadInfopoints;

    private int vectorSize;

    private int matrixLength;

    private int matrixSize;

    private float consumeReward;

    private float consumeRewardNegative;

    private float socialReward;

    private float visitedReward;

    private float refuseReward;

    public float totalScore;

    private infopointDynamics infopointDynamicsScript;



    public float state = 0.5f;



    bool TestRange(float numberToCheck, double bottom, double top)
    {
        //Debug.Log(numberToCheck + " " + bottom + " " + top);
        return (numberToCheck > bottom && numberToCheck <= top);
    }


    float SignedAngleBetween2(Vector3 a, Vector3 b)
    {

        float angle = Mathf.Atan2(b.z - a.z, b.x - a.x) / Mathf.PI;

        //Debug.Log ("a: " + angle);

        return angle;
    }


    float SignedAngleBetween(Vector3 a, Vector3 b)
    {

        float rotationAngle = transform.rotation.eulerAngles.y;

        float angle = ((Mathf.Atan2(b.z - a.z, b.x - a.x) / Mathf.PI) + (rotationAngle * Mathf.PI / 180) / Mathf.PI);

        if (angle > 1)
        {

            angle -= 2f;

        }


        //Debug.Log("a: " + angle + " rotation: " + rotationAngle * Mathf.PI / 180 + " - " + Mathf.Atan2(b.z - a.z, b.x - a.x));


        return angle;
    }

    double[] getState(Vector3 position)
    {

        double[] featuresVector = new double[vectorSize];

        numberOfAgents = 0;

        numberofGoodInfopoints = 0;

        numberofBadInfopoints = 0;

        double distance = 0f;

        numberOfAgentse = 0;

        numberOfAgentsi = 0;

        numberOfAgentsc = 0;

        numberOfAgentss = 0;


        //Debug.Log ("range: " + range);

        directionsState = new GameObject[numberOfDirections];

        Collider[] cols = Physics.OverlapSphere(position, range);
        

        foreach (var colider in cols)
        {

            //Debug.Log(colider.tag);

            if (colider.tag == "agent")
            {
                /*

                float itemDistance = Vector3.Distance(transform.position, colider.transform.position);

                if (itemDistance > 0.1f)
                {

                    var AgentScript = colider.GetComponent<simulationAgentEncapsulator>();

                    if (AgentScript.assignedPersonality == simulationAgentEncapsulator.personalityType.Extroverted)
                    {

                        numberOfAgentse++;

                    }
                    else
                    {

                        numberOfAgentsi++;
                    }

                    if (AgentScript.assignedPersonalitySecond == simulationAgentEncapsulator.personalityTypeSecond.Cooperative)
                    {

                        numberOfAgentsc++;

                    }
                    else
                    {

                        numberOfAgentss++;
                    }


                }


            */

            }
            else if (colider.tag == "info")
            {

                infopointDynamicsScript = colider.GetComponent<infopointDynamics>();

                if (infopointDynamicsScript.disposition == infopointDynamics.possibleDispositions.Good)
                {

                    numberofGoodInfopoints++;

                }
                else
                {

                    numberofBadInfopoints++;

                }

            }


            distance = Vector3.Distance(position, colider.transform.position);

            if (distance > 0.2f && distance < range)
            {

                //Debug.Log(colider.tag + " " + distance);

                for (int i = 0; i < directionsMatrix.GetLength(0); i++)
                {

                    if (TestRange(SignedAngleBetween(position, colider.transform.position), directionsMatrix[i, 0], directionsMatrix[i, 1]))
                    {


                       if(colider.tag == "info") 
                       Debug.Log ("f: " + i + " " + colider.tag + " distance: " + distance);


                        if (directionsState[i] != null)
                        {



                            if (distance < Vector3.Distance(transform.position, directionsState[i].transform.position))
                            {


                                //Debug.Log (distance + " ---- " + Vector3.Distance (position, directionsState [i].transform.position));

                                directionsState[i] = colider.transform.gameObject;

                            }

                        }
                        else
                        {

                            directionsState[i] = colider.transform.gameObject;


                        }

                    }

                }







                //Debug.Log (distance);

                //SignedAngleBetween (transform.position, colider.transform.position);


            }

        }


        int c = 0;

        foreach (var item in directionsState)
        {

            //Debug.Log (item);

            if (item != null)
            {

                float itemDistance = Vector3.Distance(position, item.transform.position);

                if (item.tag == "agent")
                {

                    /*

                    var AgentScript = item.GetComponent<simulationAgentEncapsulator>();

                    if (AgentScript.assignedPersonality == simulationAgentEncapsulator.personalityType.Extroverted) {

                        featuresVector [matrixLength * c + 2] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

                    } else {

                        featuresVector [matrixLength * c + 3] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

                    }

                    if (AgentScript.assignedPersonalitySecond == simulationAgentEncapsulator.personalityTypeSecond.Cooperative) {

                        featuresVector [matrixLength * c + 4] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

                    } else {

                        featuresVector [matrixLength * c + 5] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

                    }

                    */

                }
                else if (item.tag == "info")
                {

                    //Debug.Log("info found");

                    infopointDynamicsScript = item.GetComponent<infopointDynamics>();

                    if (infopointDynamicsScript.disposition == infopointDynamics.possibleDispositions.Good)
                    {

                        featuresVector[matrixLength * c] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

                    }
                    else
                    {

                        featuresVector[matrixLength * c + 1] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

                    }

                }


            }


            c++;
        }

        var divideNormalize = 10;


        /*
        featuresVector [matrixSize] = (float) numberOfAgentse / divideNormalize;

        featuresVector [matrixSize + 1] = (float) numberOfAgentsi / divideNormalize;

        featuresVector [matrixSize + 2] = (float) numberOfAgentse / divideNormalize;

        featuresVector [matrixSize + 3] = (float) numberOfAgentsi / divideNormalize;




        featuresVector [matrixSize + 4] = (float) numberofGoodInfopoints / divideNormalize;


        featuresVector [matrixSize + 5] = (float) numberofBadInfopoints / divideNormalize;

        */


        /*

        if (agentEncapsulatorScript.assignedPersonality == simulationAgentEncapsulator.personalityType.Extroverted) {

            featuresVector [matrixSize + 3] = 1f;

        } else {

            featuresVector [matrixSize + 3] = 0.1f;

        }

        if (agentEncapsulatorScript.currentMood == simulationAgentEncapsulator.agentsMood.Positive) {

            featuresVector [matrixSize + 4] = 1f;

        } else if (agentEncapsulatorScript.currentMood == simulationAgentEncapsulator.agentsMood.Neutral) {


            featuresVector [matrixSize + 4] = 0.5f;

        } else {


            featuresVector [matrixSize + 4] = 0.1f;

        }

*/



        //Debug.Log (q);

        
        List<double> featuresFront = new List<double>();

        featuresFront = featuresVector.ToList().GetRange(4,24);

        printArrayDouble(featuresFront.ToArray());

        //printArrayDouble(featuresVector);

        return featuresVector;

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

    // Use this for initialization
    void Start () {

        useNeuralNetworks = true;

        range = 10;

        interactRange = 10;

        consumeReward = 1f;

        socialReward = consumeReward / 2;

        visitedReward = (float)-consumeReward / 3;

        refuseReward = (float)-socialReward / 3;

        consumeRewardNegative = -consumeReward;


        numberOfDirections = 6;

        directionsState = new GameObject[numberOfDirections];

        directionsVector = new double[numberOfDirections + 2];

        directionsMatrix = new double[numberOfDirections, 2];

        step = (float)1 / (numberOfDirections / 2);

        Debug.Log ("step: " + step);



        /* rows of matrix - attributes that are perceived for each direction
         * 
         * 0 - distance of the detected object - 0-1
         * 
         * 1 - type of the detected object 1 - agent , 0 - info
         * 
         * 2 - extraverted agent - 1, infopoint - 0
         * 
         * 3 - introverted agent - 1 , infopoint - 0
         * 
         * 
         * 0 - good infopoints close  - 0 - 1
         * 
         * 1 - bad infopoints close - 0 -1
         * 
         * 2 - extraverted agents close - 0 - 1
         * 
         * 3 - intraverted agents close - 0 - 1
         * 
         * 4 - cooperative agents close - 0 - 1
         * 
         * 5 - suspicious agents close - 0 - 1
         * 
         */

        stateMatrixFeatures = new double[2];



        /* additional features
         * 
         * 0 - total number of extraverted agents detected
         * 
         * 1 - total number of intraverted agents detected
         * 
         * 2 - total number of cooperative agents detected
         * 
         * 3 - total number of suspicious agents detected
         * 
         * 4 - total number of good infopoints detected
         * 
         * 5 - total number of bad infopoints detected
         * 
         * 6 - detecting agents personality type - 0 - introverted, 1 -extroverted
         * 
         * 7 - detecting agent mood, positive 1, neutral 0.5, negative 0
         * 
         */


        onedimensionalFeatures = new double[0];


        vectorSize = numberOfDirections * stateMatrixFeatures.Length + onedimensionalFeatures.Length + 1;  // + bias

        if (useNeuralNetworks)
        {

            vectorSize--; //no need for bias

        }

        previousfeaturesVector = new double[vectorSize];

        weightsVector = new double[vectorSize];

        matrixLength = stateMatrixFeatures.Length;

        matrixSize = numberOfDirections * matrixLength;


        int counter;

        for (counter = 0; counter < weightsVector.Length; counter++)
        {

            weightsVector[counter] = 0f;

        }



        //Debug.Log (TestRange (-0.5f, -0.5f, -0.25f ));


        for (int i = 0; i < numberOfDirections + 2; i++)
        {



            if (i <= numberOfDirections / 2)
            {

                //Debug.Log ("number " + i * step);

                directionsVector[i] = i * step;

            }
            else
            {

                //Debug.Log ("number2 " + (i * step - ( 2 + step )));

                directionsVector[i] = i * step - (2 + step);

            }


            //Debug.Log (i);

        }

        for (int i = 0; i < directionsMatrix.GetLength(0); i++)
        {

            if (i < numberOfDirections / 2)
            {
                directionsMatrix[i, 0] = directionsVector[i];

                directionsMatrix[i, 1] = directionsVector[i + 1];
            }
            else
            {

                directionsMatrix[i, 0] = directionsVector[i + 1];

                directionsMatrix[i, 1] = directionsVector[i + 2];

            }


            for (int j = 0; j < directionsMatrix.GetLength(1); j++)
            {
                double s = directionsMatrix[i, j];

                Debug.Log (i + "-"+ j + " " + s);
            }
        }

    }

    int checkValue(float value)
    {
        for (int i = 0; i < directionsMatrix.GetLength(0); i++)
        {

            if (TestRange(value, directionsMatrix[i, 0], directionsMatrix[i, 1]))
                return i;
        }

        if(value < 1)
        {
            return numberOfDirections;
        }
        else if(value > 1)
        {
            return numberOfDirections + 1;
        }

        return -1;
    }
	
	// Update is called once per frame
	void Update () {

        Debug.Log(checkValue(state));
        //previousfeaturesVector = getState(transform.position);
	
	}
}
