using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ConvNetSharp;
using ConvNetSharp.Layers;
using ConvNetSharp.Training;

struct Experience
{
    public double[] s0;
    public int a0;
    public float r0;
    public double[] s1;
    public int a1;

    public Experience(double[] s0, int a0, float r0, double[] s1, int a1)
    {
        this.s0 = s0;
        this.a0 = a0;
        this.r0 = r0;
        this.s1 = s1;
        this.a1 = a1;
    }
};

public class Learning : MonoBehaviour
{


    private int numActions = 5;
    private int numStates = 5;
    private int hiddenNeurons = 100;
    private float gamma = 0.7f;
    private float epsilon = 0.2f;
    private float clamp = 2.0f;
    private int experienceAddEvery = 5;
    private int experienceSize = 5000;
    private int learningStepsPerIteration = 5;

    private Net net;

    private SgdTrainer trainer;

    private Experience[] exp;
    private int expi;
    private int expn;
    private int t;
    private float r0;
    private double[] s0;
    private double[] s1;
    private int a0;
    private int a1;

	// Use this for initialization
	void Start () {

        exp = new Experience[experienceSize];
        expi = 0;
        expn = 0;
        t = 0;
        r0 = -99f;

        // species a 2-layer neural network with one hidden layer of 20 neurons
        net = new Net();

        // input layer declares size of input. here: 2-D data
        // ConvNetSharp works on 3-Dimensional volumes (width, height, depth), but if you're not dealing with images
        // then the first two dimensions (width, height) will always be kept at size 1
        net.AddLayer(new InputLayer(1, 1, numStates));

        // declare 20 neurons, followed by ReLU (rectified linear unit non-linearity)
        net.AddLayer(new FullyConnLayer(hiddenNeurons, Activation.Relu));

        net.AddLayer(new FullyConnLayer(hiddenNeurons, Activation.Relu));

        // declare the linear classifier on top of the previous hidden layer
        net.AddLayer(new RegressionLayer(numActions));

        double[] weights = { 0.3, -0.5 , 0.1, 0.9, 0.6 };



        // forward a random data point through the network
        var x = new Volume(weights);

        var prob = net.Forward(x);

        // prob is a Volume. Volumes have a property Weights that stores the raw data, and WeightGradients that stores gradients
        Debug.Log("probability that x is class 0: " + prob.Weights[0]); // prints e.g. 0.50101

        trainer = new SgdTrainer(net) { LearningRate = 0.01, L2Decay = 0.001, Momentum = 0.0, BatchSize = 1 };
        
        //trainer.Train(x, 0); // train the network, specifying that x is class zero

        Volume prob2 = net.Forward(x);

        Debug.Log("probability that x is class 0: " + prob2.Weights[0]);
        // now prints 0.50374, slightly higher than previous 0.50101: the networks
        // weights have been adjusted by the Trainer to give a higher probability to
        // the class we trained the network with (zero)

    }

    int count = 0;

    

    // Update is called once per frame
    void Update () {


        double[] state0 = { 0.3, -0.5, 0.9, 0.1, 0.1 };

        double[] state1 = { 0.2, -0.1, 0.5, 0.9, 0.9 };

        int action0 = 2;

        int action1 = 3;

        float reward = 1;

        //double tderror = learnFromTuple(state0, action0, reward, state1, action1);

        //Debug.Log(tderror);

        //Debug.Log(act(state0));

        

        if(count < 200)
            printArrayDouble(forwardQ(state0));


        if (count == 0)
            act(state0);
        else if (count == 1)
            act(state1);

        if(count < 2)
        learn(1f);

        /*   double[] weights = { 0.3, -0.5 };

           var x = new Volume(weights);

           if (count < 900)
               printArrayDouble(forwardQ(weights));

           double backward = 0.5;

           int index = 2;


           //printArrayDouble(backward);

           trainer.Train(new Volume(weights), backward, index);

       */

        count++;

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

    private int maxi(double[] w) {
        // argmax of array w
        double maxv = w[0];
        int maxix = 0;
        for (int i = 1, n = w.Length; i<n; i++) {
            double v = w[i];
            if (v > maxv) {
            maxix = i;
            maxv = v;
            }
            }
            return maxix;
     }
   

    double[] forwardQ(double[] s)
    {
        Volume result = net.Forward(new Volume(s));

        return result.Weights;
    }


    private int act(double[] s)
    {
        int a;

        if (Random.Range(0f, 1f) < epsilon)
        {
            a = Random.Range(0,numActions);
        }
        else
        {
            // greedy wrt Q function
            double[] amat = forwardQ(s);
            a = maxi(amat); // returns index of argmax action
        }
        // shift state memory
        s0 = s1;
        a0 = a1;
        s1 = s;
        a1 = a;
        return a;
    }


    private void learn(float r1)
    {
        // perform an update on Q function
        if (r0 != -99f)
        {
            // learn from this tuple to get a sense of how "surprising" it is to the agent
            double tderror = learnFromTuple(s0, a0, r0, s1, a1);

            // decide if we should keep this experience in the replay
            if (t % experienceAddEvery == 0 || r0 != 0)
            {

                exp[expi] = new Experience(s0, a0, r0, s1, a1);
                expi += 1;
                if (expn < experienceSize)
                {
                    expn += 1;
                }
                if (expi > experienceSize)
                {
                    expi = 0;
                } // roll over when we run out
            }
            t += 1;

            Debug.Log("length: " + expn);
            // sample some additional experience from replay memory and learn from it
            for (int k = 0; k < learningStepsPerIteration; k++)
            {
                Experience experience = exp[Random.Range(0,expn)];
                learnFromTuple(experience.s0, experience.a0, experience.r0, experience.s1, experience.a1);
            }
        }
        r0 = r1; // store for next update
    }



    double learnFromTuple(double[] s0, int a0, float r0, double[] s1, int a1)
    {
        // want: Q(s,a) = r + gamma * max_a' Q(s',a')
        // compute the target Q value
        double[] tmat = forwardQ(s1);
        double qmax = r0 + gamma * tmat[maxi(tmat)];
        // now predict
        double[] pred = forwardQ(s0);

        Debug.Log("s1:");
        printArrayDouble(tmat);

        Debug.Log("qmax: " + qmax);

        Debug.Log("s2:");
        printArrayDouble(pred);

        double tderror = pred[a0] - qmax;

        Debug.Log("tderror: " + tderror + " a0: " + a0);

        if (Mathf.Abs((float)tderror) > clamp)
        { // huber loss to robustify
            if (tderror > clamp)
            {
                tderror = clamp;
            }
            if (tderror < -clamp)
            {
                tderror = -clamp;
            }
        }

        trainer.Train(new Volume(s0),tderror,a0);

        return tderror;

    }

}
