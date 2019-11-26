using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Apex.Common;
using Apex.WorldGeometry;
using System.IO;
using Apex.Steering.Components;
using Apex.Steering.Behaviours;
using UnityEditor.Audio;
using UnityEngine.SceneManagement;




public class Generator : MonoBehaviour
{

	private static string FILE_NAME = "C:\\Users\\one\\Documents\\stats";

    public GameObject agent;
    public int numberOfAgents;
    public int min, max;
	private Vector3 center;
	private IGrid mainGrid;
	private GridComponent mainGridComponent;
	private FlatCell tempCell;
	private static Object prefab;
	private static Object prefabInfo;

	private int currentGridBoundsMax;
	private int currentGridBoundsMin;
	private static int spawnTreshold = 500;

    private AgentBehaviour AgentBehaviourScript;

    private simulationAgentEncapsulator AgentEncapsulatorScript;

	private infopointDynamics infopointScript;

    private int numberOfDirections, directionalFeatures, onedimensionalFeatures;

	public int score;

	public enum terrainType { medium, good , bad };

	public terrainType selectedTerrain;

    public bool useNeuralNetworks;

    void OnGUI()
    {

        if (GUI.Button(new Rect(10, 70, 50, 30), "Stats"))
            statistics();

    }

    void statistics()
    {

        GameObject a = GameObject.FindGameObjectWithTag("agent");

        AgentBehaviourScript = a.GetComponent<AgentBehaviour>();

        numberOfDirections = AgentBehaviourScript.numberOfDirections;

        directionalFeatures = AgentBehaviourScript.stateMatrixFeatures.Length;

        onedimensionalFeatures = AgentBehaviourScript.onedimensionalFeatures.Length;

        GameObject[] agents = GameObject.FindGameObjectsWithTag("agent");

        Debug.Log("agents " + agents.Length + " directional " + numberOfDirections * directionalFeatures + " onedimensional " + onedimensionalFeatures );

        //Time.timeScale = 0;

        //string buffer = "visited_info,unvisited_info,extraverted_agents,introverted_agents,total_agents,total_visited_info,total_unvisited_info,type" + System.Environment.NewLine;

        string buffer = "vi,uvi,ea,ia,ca,sa,tae,tai,tca,tsa,tvi,tuvi,good,bad,score,type" + System.Environment.NewLine;

        foreach (GameObject agent in agents)
        {
            AgentBehaviourScript = agent.GetComponent<AgentBehaviour>();

            AgentEncapsulatorScript = agent.GetComponent<simulationAgentEncapsulator>();

            double[] currentVector = AgentBehaviourScript.weightsVector;

			List<GameObject> infopointsVisited = AgentBehaviourScript.infopointsVisited;


			float totalScore = AgentBehaviourScript.totalScore;

			int goodInfo = 0;

			int badInfo = 0;

			foreach (var infopoint in infopointsVisited) {

				infopointScript = infopoint.GetComponent<infopointDynamics> ();

				if (infopointScript.disposition == infopointDynamics.possibleDispositions.Good) {

					goodInfo++;

				} else {

					badInfo++;

				}

			}

            string personality = "";

            if(AgentEncapsulatorScript.assignedPersonality == simulationAgentEncapsulator.personalityType.Extroverted)
            {

                personality = "E";

            }
            else
            {

                personality = "I";

            }

			if (AgentEncapsulatorScript.assignedPersonalitySecond == simulationAgentEncapsulator.personalityTypeSecond.Cooperative) {

				personality += "C";

			} else {
				
				personality += "S";

			}

            printArrayDouble(currentVector);

            double bias = currentVector[currentVector.Length - 1];

            Debug.Log(bias);

            for (int i = 0; i < currentVector.Length; i++)
            {

                currentVector[i] = currentVector[i] - bias;

            }

            printArrayDouble(currentVector);

            List<double> features = new List<double>();

            for (int i = 0; i < directionalFeatures; i++)
            {

                //List<double> f = new List<double>();

                double featureSum = 0f;


                for (int j = 0; j < numberOfDirections; j++)
                {

                    //Debug.Log(currentVector[j * directionalFeatures + i]);

                    featureSum += currentVector[j * directionalFeatures + i];

                    //f.Add(currentVector[j * directionalFeatures + i]);

                    //f.Add(j * directionalFeatures + i);

                }

                features.Add(featureSum / numberOfDirections);

            }


            for (int i = 0; i < onedimensionalFeatures; i++)
            {

                features.Add(currentVector[numberOfDirections * directionalFeatures + i]);

            }


			features.Add (goodInfo);

			features.Add (badInfo);

			features.Add (totalScore);


            //Debug.Log(agent.name);

            string featureBuffer = "";

            int count = 0;

            foreach(var feature in features)
            {

                count++;

                if(count<features.Count)
                featureBuffer += feature + ",";
                else
                featureBuffer += feature;

            }

         // if(!featureBuffer.Contains("0,0,0,0,0,0,0"))
            buffer += featureBuffer + "," + personality + System.Environment.NewLine;

            

        }


        writeData(buffer);


    }

    void Start()
    {
		prefab = Resources.Load ("Prefabs/simulationAgent");
		prefabInfo = Resources.Load ("Prefabs/InfoPoint");

		//System.Text.StringBuilder sb = new System.Text.StringBuilder();

		currentGridBoundsMax = 4000;

		currentGridBoundsMin = -4000;

		score = 0;

		instantiateGrid ();

		//spawnInfopoints(75);

        

		center = new Vector3 (0, 0, 0);

		FILE_NAME += selectedTerrain.ToString() + ".csv";


	/*	
		IEnumerator en = mainGrid.cellMatrix.items.GetEnumerator();

		while (en.MoveNext()) {

			tempCell = (FlatCell) en.Current;

			if(tempCell.IsWalkableToAll())
			sb.Append("0");
			else
			sb.Append("1");

		}

		writeData (sb.ToString());

	*/


    }

	void Update()
	{

		bool stats = true;

		if (score >= 3000) {

			if (stats) {

				Time.timeScale = 0;

				statistics ();

				stats = false;

				//SceneManager.LoadScene ("sim");

			}

			UnityEditor.EditorApplication.Beep ();

		}

	}

    void placeAgents()
    {
        simulationAgentEncapsulator mainAgentScript=null;



        for (int i = 0; i < numberOfAgents; i++)
        {
           //Instantiate(agent, GeneratedPosition(), Quaternion.identity);

            GameObject cloneAgent = Instantiate(prefab, GeneratedPosition(), Quaternion.identity) as GameObject;

			cloneAgent.tag = "agent";

			cloneAgent.name = "agent" + i;

			int quarterNum = (int) numberOfAgents / 4;

             mainAgentScript = cloneAgent.GetComponent<simulationAgentEncapsulator>();
			if (i >= 0 && i < quarterNum) {
				mainAgentScript.assignedPersonality = simulationAgentEncapsulator.personalityType.Introverted;
				mainAgentScript.assignedPersonalitySecond = simulationAgentEncapsulator.personalityTypeSecond.Cooperative;
			}

			if (i >= quarterNum && i < quarterNum*2) {
				mainAgentScript.assignedPersonality = simulationAgentEncapsulator.personalityType.Introverted;
				mainAgentScript.assignedPersonalitySecond = simulationAgentEncapsulator.personalityTypeSecond.Suspicious;
			}

			if (i >= quarterNum*2 && i < quarterNum*3) {
				mainAgentScript.assignedPersonality = simulationAgentEncapsulator.personalityType.Extroverted;
				mainAgentScript.assignedPersonalitySecond = simulationAgentEncapsulator.personalityTypeSecond.Cooperative;
			}

			if (i >= quarterNum*3 && i < quarterNum*4) {
				mainAgentScript.assignedPersonality = simulationAgentEncapsulator.personalityType.Extroverted;
				mainAgentScript.assignedPersonalitySecond = simulationAgentEncapsulator.personalityTypeSecond.Suspicious;
			}

        }

		Debug.Log ("Agents placed...");
    }
    Vector3 GeneratedPosition()
    {
        int x, y, z;
		x = UnityEngine.Random.Range(- 1500  , 1500 );
		y = 0;
		z = UnityEngine.Random.Range(- 1500  , 1500 );
        return new Vector3(x, y, z);
    }


	public static void writeData(string buffer) 
	{
		if (File.Exists(FILE_NAME)) 
		{
            File.Delete(FILE_NAME);
		}
		StreamWriter sr = File.CreateText(FILE_NAME);
		sr.WriteLine (buffer);
		sr.Close();
	}


	void spawnInfopoints(int cubeNumber) {

		int range = 0;

		int quarter = Mathf.RoundToInt(cubeNumber / 4);

		if (selectedTerrain == terrainType.bad) {

			range = quarter;

		} else if (selectedTerrain == terrainType.medium) {

			range = quarter * 2;

		} else if (selectedTerrain == terrainType.good) {

			range = quarter * 3;
		}


		Debug.Log ("Generated " + range + " good out of " + cubeNumber);



		infopointDynamics infopointScript=null;


		Vector3 v3T = Vector3.zero;
		Vector3[] arv3 = new Vector3[cubeNumber];
		float fMinDist = 10.0f;   // Minimum distance they need to be apart
		int iMaxTries = 1000;    // Number of times to try to generate a position
		float fMinTry = Mathf.Infinity;
		
		int i;
		for (i = 0; i < cubeNumber; i++) {
			
			for (int j = 0; j < iMaxTries; j++) {
				v3T.x = Random.Range (currentGridBoundsMin, currentGridBoundsMax);
				v3T.z = Random.Range (currentGridBoundsMin, currentGridBoundsMax);
				v3T.y = 16;
				
				fMinTry = Mathf.Infinity; 
				for (int k = 0; k < i; k++)
					fMinTry = Mathf.Min ((v3T - arv3[k]).magnitude, fMinTry);
				
				if (fMinTry > fMinDist) // Far enough apart
					break;
			}
			if (fMinTry < fMinDist) {
				Debug.Log ("Generation failed -- only found " + i + " points");
				break;
			}
			arv3[i] = v3T;
		}
		
		for (int j = 0; j < i; j++) {

			GameObject go = Instantiate(prefabInfo, arv3[j], Quaternion.identity) as GameObject;
			go.layer = 8;
			go.tag = "info";
			go.name = "info" + j;


			infopointScript = go.GetComponent<infopointDynamics>();

			if (j <= range)
				infopointScript.disposition = infopointDynamics.possibleDispositions.Good;
			else
				infopointScript.disposition = infopointDynamics.possibleDispositions.Bad;
			

		}

		
	}

	void instantiateGrid()
	{

		mainGridComponent = GridManager.instance.GetGridComponent (center);



		if(mainGridComponent != null)
		{
			mainGridComponent.Initialize(1000, (Result) =>
			                             {
				
				afterInitialization();

			});
		}

	}

	void afterInitialization()
	{
		Debug.Log ("Grid is initalized, placing agents...");

		getBounds();

		spawnInfopoints (7000);

		placeAgents();

        Time.timeScale = 6f;



	}

	void getBounds()
	{

		mainGrid = GridManager.instance.GetGrid (center);
		
		
		currentGridBoundsMax = (int) mainGrid.bounds.max.x - spawnTreshold;
		currentGridBoundsMin = (int) mainGrid.bounds.min.x + spawnTreshold;

		Debug.Log(currentGridBoundsMax);
		Debug.Log(currentGridBoundsMin);

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