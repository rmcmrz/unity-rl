using UnityEngine;
using System.Collections.Generic;
using Apex.WorldGeometry;
using System.Linq;
using Apex.Units;
using System.IO;
using Apex.Steering.Behaviours;
using System.Collections;

public class movingFood : MonoBehaviour
{

    private IGrid mainGrid;

    private Object foodSource;

    private float rate = 0.2f;

    private List<Cell> foodCells;

    public enum terrainType { medium, good, bad };

    public terrainType selectedTerrain;

    private int range = 100;

    private infopointDynamics infopointScript;

    public float foodRatio = 10f;

    private int totalCells;

    private List<Cell> cellList, cellsClose;

    private IUnitFacade _unit;

    public int score;

    private int totalFood = 70;

    public int foodCount = 0;

    public int badfoodCount = 0;

    public int goodfoodCount = 0;

    private int generatedFood = 0;

	private static string FILE_NAME = "C:\\Users\\Proprietario\\Documents\\result\\";

    GameObject[] agents;

    List<double>[] evolved = new List<double>[6];

    private Object singleAgent;

    List<Vector3> positions;



    // Use this for initialization
    void Start()
    {

        singleAgent = Resources.Load("Prefabs/agent");

        positions = new List<Vector3>();

        Debug.Log(evolved.Length);

        agents = GameObject.FindGameObjectsWithTag("agent");

        foodSource = Resources.Load("Prefabs/food");

        for (int i = 0; i < agents.Length; i++)
        {
            positions.Add(agents[i].gameObject.transform.position);
        }

        Vector3 position = Vector3.zero;

        position.y = 0.9f;

        for (int i = 0; i < totalFood; i++)
        {
            position.x = Random.Range(-range, range);
            position.z = Random.Range(-range, range);

            GameObject temp = Instantiate(foodSource, position, Quaternion.identity) as GameObject;

            temp.layer = 8;
            temp.tag = "info";

            if (i % 2 == 0)
            {
                infopointScript = temp.GetComponent<infopointDynamics>();

                infopointScript.disposition = infopointDynamics.possibleDispositions.Bad;

                badfoodCount++;

            }
            else
            {

                goodfoodCount++;

            }


            foodCount = i;

        }



        InvokeRepeating("updateFood", 1f, 1f);


        loadEvolved();

        /*

        AgentBehaviourRemote AgentBehaviourScript = agents[0].GetComponent<AgentBehaviourRemote>();

        printListDouble(AgentBehaviourScript.downloadNet());

        

        List<double> first = new List<double>(new double[] { 0.5, 1, 5, 9, 0.1 });

        List<double> second = new List<double>(new double[] { 8, 3, 6, 0.9, 0.8 });

        List<double> child = mating(first, second);

        List<double> child2 = mutation(first);

        Debug.Log("child");

        printListDouble(child);

        Debug.Log("mutation");

        printListDouble(child2);

        */

    }

    // Update is called once per frame
    void Update()
    {

    }

    void updateFood()
    {

        //Debug.Log(foodCount + " " + totalFood);

        if (foodCount < totalFood)
        {
            

            Vector3 position = Vector3.zero;

            position.x = Random.Range(-range, range);
            position.z = Random.Range(-range, range);

            position.y = 0.9f;

            GameObject temp = Instantiate(foodSource, position, Quaternion.identity) as GameObject;

            temp.layer = 8;
            temp.tag = "info";

            if (goodfoodCount >= badfoodCount)
            {
                infopointScript = temp.GetComponent<infopointDynamics>();

                infopointScript.disposition = infopointDynamics.possibleDispositions.Bad;

                badfoodCount++;

            }
            else
            {

                goodfoodCount++;
                
            }

            foodCount++;
            generatedFood++;

        }

    }







    public static void writeData(string buffer,string fn) 
    {
        StreamWriter sr = new StreamWriter(FILE_NAME + fn + ".txt", false);
        sr.WriteLine (buffer);
        sr.Close();
    }

    public static void writeDataAppend(string buffer, string fn)
    {
        StreamWriter sr = new StreamWriter(FILE_NAME + fn + ".txt", true);
        sr.WriteLine(buffer);
        sr.Close();
    }


    public static string readData(string fn)
    {

        StreamReader sr;
        try
        {

            sr = new StreamReader(FILE_NAME + fn + ".txt");

        }
        catch (System.Exception)
        {
            return "error";
            throw;
        }
        

        if (sr != null)
        {
            string buffer = sr.ReadLine();
            sr.Close();

            return buffer;
        }
        else
        {

            return "error";
        }

       
    }


    public void statistics()
    {

        Debug.Log("agents " + agents.Length );


        AgentBehaviourRemote AgentBehaviourScript = agents[0].GetComponent<AgentBehaviourRemote>();

        //List<double> weights = AgentBehaviourScript.downloadNet();

        //writeData(listToCsv(weights), "AgentWeights");



        //**** begin export stats scores

        string rewardString = "";


        List<int> sum = new List<int>();


        string c = ",";

        char comma = c[0];


        string temp2 = agents[0].GetComponent<AgentBehaviourRemote>().rewardString.TrimEnd();

        temp2 = temp2.Substring(0, temp2.Length - 1);

        string[] tempArray2 = temp2.Split(comma);

        for (int i = 0; i < tempArray2.Length; i++)
        {
            sum.Add(0);
        }

        double replayEntropySum = 0f;

        double replayCuriositySum = 0f;

        double rewardSum = 0f;


        List<double> fitnessList = new List<double>();



        foreach (GameObject agent in agents)

        {


        AgentBehaviourScript = agent.GetComponent<AgentBehaviourRemote>();


        string temp = AgentBehaviourScript.rewardString.TrimEnd();

        temp = temp.Substring(0, temp.Length - 1);

            string[] tempArray = temp.Split(comma);

            List<int> integerList = new List<int>();

            for (int i = 0; i < tempArray.Length; i++)
            {
                int item;

                int.TryParse(tempArray[i], out item);

                integerList.Add(item);
            }

            for (int i = 0; i < sum.Count; i++)
            {

                sum[i] = sum[i] + integerList[i];

            }

            fitnessList.Add(AgentBehaviourScript.getFitness());


            Debug.Log(agent.name + " fitness " + AgentBehaviourScript.getFitness());

        }

        double[] result = new double[tempArray2.Length];

        for (int i = 0; i < sum.Count; i++)
        {

            result[i] = (double) sum.ElementAt(i) / agents.Length;

        }

        rewardString = listToCsv(result.ToList());

        List<int> BestIndexes = new List<int>();


        int index = fitnessList.IndexOf(fitnessList.Max());

        BestIndexes.Add(index);

        fitnessList[index] = -999999f;

        index = fitnessList.IndexOf(fitnessList.Max());

        BestIndexes.Add(index);

        fitnessList[index] = -999999f;

        index = fitnessList.IndexOf(fitnessList.Max());

        BestIndexes.Add(index);

        fitnessList[index] = -999999f;


        List<double> firstCandidate = new List<double>();

        List<double> secondCandidate = new List<double>();

        List<double> thirdCandidate = new List<double>();

        for (int i = 0; i < BestIndexes.Count; i++)
        {
            AgentBehaviourScript = agents[BestIndexes[i]].GetComponent<AgentBehaviourRemote>();

            Debug.Log(i + " " + agents[BestIndexes[i]].name + " " + AgentBehaviourScript.getFitness());

            if(i == 0)
            {
                firstCandidate = AgentBehaviourScript.downloadNet();
            }

            if (i == 1)
            {
                secondCandidate = AgentBehaviourScript.downloadNet();
            }

            if (i == 2)
            {
                thirdCandidate = AgentBehaviourScript.downloadNet();
            }


            replayEntropySum += AgentBehaviourScript.getReplayEntropy();

            replayCuriositySum += AgentBehaviourScript.getReplayCuriosity();

			rewardSum += AgentBehaviourScript.getFitness();


        }

        string replayEntropy = (replayEntropySum / 3).ToString();

        string replayCuriosity = (replayCuriositySum / 3).ToString();

        string replayReward = (rewardSum / 3).ToString();



        writeDataAppend((replayEntropy + ",").TrimEnd(), "agents.Entropy");

        writeDataAppend((replayCuriosity + ",").TrimEnd(), "agents.Curiosity");

        writeDataAppend((replayReward + ",").TrimEnd(), "agents.Reward");

		double replayCuriositySumAll = 0f;

		double rewardSumAll = 0f;

		foreach (var a in agents) {

			AgentBehaviourScript = a.GetComponent<AgentBehaviourRemote>();

			replayCuriositySumAll += AgentBehaviourScript.getReplayCuriosity();

			rewardSumAll += AgentBehaviourScript.getFitness();
		}


		writeDataAppend((replayCuriositySumAll / agents.Length + ",").TrimEnd(), "agents.CuriosityAll");

		writeDataAppend((rewardSumAll / agents.Length  + ",").TrimEnd(), "agents.RewardAll");


        for (int i = 0; i < agents.Length; i++)
        {

            DestroyImmediate(agents[i]);

        }

        for (int i = 0; i < agents.Length; i++)
        {

            GameObject temp = Instantiate(singleAgent, positions[i], Quaternion.identity) as GameObject;

            temp.name = "agent" + i.ToString();

            temp.tag = "agent";

            agents[i] = temp;

        }

        //evolving

        evolved[0] = mating(firstCandidate, secondCandidate);

        evolved[1] = mating(secondCandidate, firstCandidate);

        evolved[2] = mating(firstCandidate, thirdCandidate);

        evolved[3] = mating(secondCandidate, thirdCandidate);

        evolved[4] = mutation(thirdCandidate);

        evolved[5] = mutation(firstCandidate);



        StartCoroutine(updateAgents());

        /*

        Debug.Log("first candidate");

        printListDouble(firstCandidate);

        Debug.Log("second candidate");

        printListDouble(secondCandidate);

        Debug.Log("evolved 0");

        printListDouble(evolved[0]);

        Debug.Log("evolved 1");

        printListDouble(evolved[1]);

        Debug.Log("evolved 2");

        printListDouble(evolved[2]);

        Debug.Log("evolved 3");

        printListDouble(evolved[3]);

        */


        saveEvolved();


    }

    // **** end export data score

    string listToCsv(List<double> list)

    {

        string s = "";

        int count = 1;
        int size = list.Count;

        foreach (var item in list)
        {

            if (count == size)
            {
                s += item.ToString();
            }
            else
            {
                s += item.ToString() + ",";
            }

            count++;
        }

        return s;

    }

    string listIntToCsv(List<int> list)

    {

        string s = "";

        int count = 1;
        int size = list.Count;

        foreach (var item in list)
        {

            if (count == size)
            {
                s += item.ToString();
            }
            else
            {
                s += item.ToString() + ",";
            }

            count++;
        }

        return s;

    }

    /*
    public void loading()
    {

        AgentBehaviourRemote AgentBehaviourScript = agents[0].GetComponent<AgentBehaviourRemote>();


        string weights = readData("AgentWeights");

        string[] weightArray;

        string c = ",";

        char separator = c.ToCharArray()[0];

        List<double> weightList = new List<double>();

        if(weights != "error")
        {

            weightArray = weights.Split(separator);

            foreach (var weight in weightArray)
            {
                double item;

                double.TryParse(weight, out item);

                weightList.Add(item);
            }

            AgentBehaviourScript.loadNet(weightList);

            Debug.Log("loaded weights " + weightList.Count + " " + weightList.ElementAt(1));


        }
        else
        {

            Debug.Log("cannot load weights");
        }
    }

    */

    public void loading()
    {


    }

    private List<double> mating(List<double> first,List<double> second)
    {
        List<double> child = new List<double>();

        for (int i = 0; i < first.Count; i++)
        {

            if (Random.Range(0f, 1f) < 0.5f)
            {
                child.Add(first[i]);
            }
            else
            {
                child.Add(second[i]);
            }

        }

        return child;

    }


    private List<double> mutation(List<double> candidate)
    {

        List<double> child = new List<double>();

        double amount = 0.05f;

        for (int i = 0; i < candidate.Count; i++)
        {


            if (Random.Range(0f, 1f) < 0.55f)
            {

                if (Random.Range(0f, 1f) < 0.5f)
                {
                    child.Add(candidate[i] + amount);
                }
                else
                {

                    child.Add(candidate[i] + ( amount * -1f ));

                }

            }
            else
            {
                child.Add(candidate[i]);
            }

        }

        return child;

    }

    private void printListDouble(List<double> list)
    {

        string buffer = "";

        foreach (var item in list)
        {
            buffer += item + ",";
        }

        Debug.Log(buffer);


    }

    private void saveEvolved()
    {

        for (int i = 0; i < evolved.Length; i++)
        {

            writeData(listToCsv(evolved[i]), "network.agent" + i.ToString());

        }


    }

    private void loadEvolved()
    {

        string c = ",";

        char separator = c.ToCharArray()[0];

        for (int i = 0; i < agents.Length; i++)
        {

            AgentBehaviourRemote AgentBehaviourScript = agents[i].GetComponent<AgentBehaviourRemote>();


            string weights = readData("network.agent" + i.ToString());

            string[] weightArray;

            List<double> weightList = new List<double>();

            if (weights != "error")
            {

                weightArray = weights.Split(separator);

                foreach (var weight in weightArray)
                {
                    double item;

                    double.TryParse(weight, out item);

                    weightList.Add(item);
                }

                AgentBehaviourScript.loadNet(weightList);

                Debug.Log("loaded weights " + " " + agents[i].name + " " + weightList.Count + " " + weightList.ElementAt(1));


            }
            else
            {

                Debug.Log("cannot load weights");
            }


        }



    }

    IEnumerator updateAgents()
    {

        yield return new WaitForEndOfFrame();

        agents = GameObject.FindGameObjectsWithTag("agent");

        Debug.Log("updating total " + agents.Length);

        //update



        for (int i = 0; i < agents.Length; i++)
        {
            AgentBehaviourRemote AgentBehaviourScript = agents[i].GetComponent<AgentBehaviourRemote>();

            //AgentBehaviourScript.resetNet();

            //AgentBehaviourScript.totalScore = 0f;

            //AgentBehaviourScript.totalScoreHalf = 0f;

            //AgentBehaviourScript.moveCount = 0;

            AgentBehaviourScript.loadNet(evolved[i]);

        }

        
    }




}