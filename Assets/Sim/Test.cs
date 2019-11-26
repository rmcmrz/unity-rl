using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Qlearning.Entropy;

public class Test : MonoBehaviour {

    public double[] test,test2;

    public int[] histogram;

    public double entropy;

    private Entropy e;

	// Use this for initialization
	void Start () {

        e = new Entropy();

        test = new double[4] { 0.3f, 0f, 0f, 0f };

        test2 = new double[4] { 0.9f, 0f, 0f, 0f };

    }
	
	// Update is called once per frame
	void Update () {

        entropy = e.getEntropy(test, test2);
		
	}
}
