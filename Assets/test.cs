using UnityEngine;
using Accord.Neuro;
using Accord.Neuro.ActivationFunctions;
using Accord.Neuro.Learning;
using Accord.Neuro.Networks;
using Accord.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AForge.Neuro.Learning;
using System.IO;

public class test : MonoBehaviour {

	// Use this for initialization
	void Start () {


        // Setup the deep belief network and initialize with random weights.
        DeepBeliefNetwork network = new DeepBeliefNetwork(20, 10, 10);
        new GaussianWeights(network, 0.1).Randomize();
        network.UpdateVisibleWeights();

        Debug.Log("update");

        Debug.Log(network.Layers.Count());


    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
