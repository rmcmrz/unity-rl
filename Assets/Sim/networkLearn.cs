using UnityEngine;
using System.Collections;
using SocketIO;
using Apex.Steering.Behaviours;
using System.Collections.Generic;

public class networkLearn : MonoBehaviour {

    private SocketIOComponent socket;

    private GameObject[] learningAgents;

    private List<AgentBehaviourRemote> learningAgentsReferences;

    private bool connected = false;

    double[] stateVector;

    private bool sent = false;

    private void getNextAction(SocketIOEvent e)
    {
        //stateVector = agentBehaviourScript.getStateVector();

        //Dictionary<string, string> data = new Dictionary<string, string>();

        //data["state"] = arrayDoubletoString(stateVector);

        //socket.Emit("getAction", new JSONObject(data));

        foreach (var reference in learningAgentsReferences)
        {

            string id = e.data["id"].ToString();

            if (reference.name.Substring(5).Equals(id.Substring(1, id.Length - 2)))
            {

                stateVector = reference.getStateVector();

                Dictionary<string, string> data = new Dictionary<string, string>();

                data["state"] = arrayDoubletoString(stateVector);

                data["id"] = id.Substring(1, id.Length - 2);

                socket.Emit("getAction", new JSONObject(data));

                //Debug.Log("next action agent " + id);

            }

        }

    }

    public void sendReward(float reward, string id)
    {

        Dictionary<string, string> data = new Dictionary<string, string>();

        data["reward"] = reward.ToString();

        data["id"] = id;

        socket.Emit("sendReward", new JSONObject(data));

        //Debug.Log("agent " + id + " sent reward: " + reward);

    }


    public void ServerOpen(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Open received: " + e.name + ": " + e.data);

        connected = true;

    }

    public void ServerError(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Error received: " + e.name + " : " + e.data);

        connected = false;

    }

    public void ServerClose(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Close received: " + e.name + " : " + e.data);

        connected = false;
    }

    void doAction(SocketIOEvent e)
    {

        foreach (var reference in learningAgentsReferences)
        {

            string id = e.data["id"].ToString();

            //Debug.Log("agent: " + id.Substring(1, id.Length - 2) + " doing " + e.data["action"].ToString());

           //Debug.Log(reference.name.Substring(5) + " " + e.data["id"]);

            if(reference.name.Substring(5).Equals(id.Substring(1,id.Length - 2)))
            {

                reference.doAction(int.Parse(e.data["action"].ToString()));

               //Debug.Log("agent " + e.data["id"] + "doing " + e.data["action"]);

            }

        }

        //Debug.Log("sucess");
        //Debug.Log(e.data["action"]);

    }

    void loginUnsuccess(SocketIOEvent e)
    {
        Debug.Log("login error");
    }

    // Use this for initialization
    void Start () {

        connected = false;

        sent = false;

        DontDestroyOnLoad(gameObject);

        learningAgents = GameObject.FindGameObjectsWithTag("agent");

        learningAgentsReferences = new List<AgentBehaviourRemote>();

        foreach (var item in learningAgents)
        {

            learningAgentsReferences.Add(item.GetComponent<AgentBehaviourRemote>());

        }


        foreach (var item in learningAgentsReferences)
        {

            Debug.Log(item.name.Substring(5));

        }

        GameObject go = GameObject.Find("SocketIO");
        socket = go.GetComponent<SocketIOComponent>();


        socket.On("open", ServerOpen);
        socket.On("error", ServerError);
        socket.On("close", ServerClose);
        socket.On("doAction", doAction);
        socket.On("getNextAction", getNextAction);

    }

    void Awake()
    {


    }
	
	// Update is called once per frame
	void Update () {

        while (connected&&!sent)
        {

            for (int i = 0; i < learningAgentsReferences.Count; i++)
            {

                stateVector = learningAgentsReferences[i].getStateVector();

                string id = learningAgentsReferences[i].name.Substring(5);

                Dictionary<string, string> data = new Dictionary<string, string>();

                data["state"] = arrayDoubletoString(stateVector);

                data["id"] = id;

                socket.Emit("getAction", new JSONObject(data));

            }

            sent = true;

        }

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

    private string arrayDoubletoString(double[] array)

    {

        string row = "";

        foreach (var item in array)
        {

            row += System.Math.Round((decimal)item, 6).ToString() + ",";

        }

        return row.Substring(0,row.Length - 1);

    }


}
