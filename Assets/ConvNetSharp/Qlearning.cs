using UnityEngine;
using ConvNetSharp.Layers;
using ConvNetSharp.Training;
using Qlearning.Entropy;
using System.Linq;
using Stats.Quartiles;
using System.Collections.Generic;

namespace ConvNetSharp
{

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

    public class QLearning
    {


        private int numActions = 5;
        private int numStates = 152;
        private int hiddenNeurons = 100;
        private float gamma = 0.75f;
        public float epsilon = 0.2f;
        private float clamp = 2.0f;
        private int experienceAddEvery = 6;
        private int experienceSize = 3500;
        private int learningStepsPerIteration = 5;

        private float alpha = 2.5f;

        private Net net;

        private Net netClassify;

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
        private Entropy e;
        private Experience agentexp;

        private double entropyAverage = 0;

        private int entropyNum = 0;

        private Quartiles q;

        private static string FILE_NAME = "C:\\Users\\one\\Documents\\agents\\";

        // Use this for initialization
        public QLearning()
        {

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
            net.AddLayer(new FullyConnLayer(hiddenNeurons - 10, Activation.Relu));

            //snet.AddLayer(new FullyConnLayer(hiddenNeurons/4, Activation.Relu));

            // declare the linear classifier on top of the previous hidden layer
            net.AddLayer(new RegressionLayer(numActions));

            Debug.Log("Network initialized");


            // species a 2-layer neural network with one hidden layer of 20 neurons
            netClassify = new Net();

            // input layer declares size of input. here: 2-D data
            // ConvNetSharp works on 3-Dimensional volumes (width, height, depth), but if you're not dealing with images
            // then the first two dimensions (width, height) will always be kept at size 1
            netClassify.AddLayer(new InputLayer(1, 1, 2));

            // declare 20 neurons, followed by ReLU (rectified linear unit non-linearity)
            netClassify.AddLayer(new FullyConnLayer(4, Activation.Relu));

            //snet.AddLayer(new FullyConnLayer(hiddenNeurons/4, Activation.Relu));

            // declare the linear classifier on top of the previous hidden layer
            netClassify.AddLayer(new SoftmaxLayer(2));

            Debug.Log("Network Classify initialized");

            /*
            List<double> list = new List<double>();

            list = netToList(net);

            outputList(list, "agent1");


            ListToNet(net, list);

            List<double> list2 = new List<double>();

            list2 = netToList(net);

            list2[1] = 0.5f;

            outputList(list2, "agent2");

            */



            //double[] weights = { 0.3, -0.5, 0.1, 0.9, 0.6 };



            // forward a random data point through the network
            //var x = new Volume(weights);

            //var prob = net.Forward(x);

            // prob is a Volume. Volumes have a property Weights that stores the raw data, and WeightGradients that stores gradients
            //Debug.Log("probability that x is class 0: " + prob.Weights[0]); // prints e.g. 0.50101

            trainer = new SgdTrainer(net) { LearningRate = 0.01, L2Decay = 0.001, Momentum = 0.0, BatchSize = 5 };

            //trainer.Train(x, 0); // train the network, specifying that x is class zero

            // Volume prob2 = net.Forward(x);

            //Debug.Log("probability that x is class 0: " + prob2.Weights[0]);
            // now prints 0.50374, slightly higher than previous 0.50101: the networks
            // weights have been adjusted by the Trainer to give a higher probability to
            // the class we trained the network with (zero)

            e = new Entropy();

            q = new Quartiles();

            double[] arr = new double[8] { 5, 6, 7, 2, 1, 8, 4, 3 };

            double[] ascOrderedArray = (from i in arr orderby i ascending select i).ToArray();

            Debug.Log(q.umidmean(ascOrderedArray));

            Debug.Log(q.lmidmean(ascOrderedArray));

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

        private int maxi(double[] w)
        {
            // argmax of array w
            double maxv = w[0];
            int maxix = 0;
            for (int i = 1, n = w.Length; i < n; i++)
            {
                double v = w[i];
                if (v > maxv)
                {
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


        public int act(double[] s)
        {
            //Debug.Log("length" + s.Length);

            int a;

            if (Random.Range(0f, 1f) < epsilon)
            {
                a = Random.Range(0, numActions);
            }
            else
            {
                // greedy wrt Q function
                double[] amat = forwardQ(s);
                a = maxi(amat); // returns index of argmax action]
                //printArrayDouble(amat);
            }
            // shift state memory
            s0 = s1;
            a0 = a1;
            s1 = s;
            a1 = a;
            return a;
        }


        public void learn(float r1,bool food)
        {
            // perform an update on Q function
            if (r0 != -99f)
            {


                // learn from this tuple to get a sense of how "surprising" it is to the agent
                double tderror = learnFromTuple(s0, a0, r0, s1, a1);

                entropyNum++;


                /*

                entropyAverage = entropyAverage + (1 / (double)entropyNum) * (entropy - entropyAverage);


                double entropyDifference = entropy - entropyAverage;

                if (entropyDifference < 0)
                    entropyDifference = 0;

                */

                /*

                if( entropyDifference * alpha > 0.5f )
                Debug.Log( entropyDifference * alpha );

                


                if (entropyNum >= 500)
                {

                    entropyAverage = entropyNum = 0;
                }

                */

                double[] state = new double[2];

                state[0] = e.getEntropySingle(s0);

                state[1] = e.getEntropySingle(s1);

                /* printArrayDouble(s0);

                printArrayDouble(state);

                Debug.Log(s0.Length + " " + state.Length);

                */

                double resultClass = 1f;

                //bool sample = true;

                if (t > 2)
                {

                Volume resultClassify = netClassify.Forward(new Volume(state));

                resultClass = resultClassify.Weights[0];

                //sample = resultClassify.Weights[0] <= resultClassify.Weights[1];

                }

                //Debug.Log(resultClass);
                
                // decide if we should keep this experience in the replay
				//if (food || Random.Range(0f, 1f) < resultClass)
                //if (entropy > 0.7 || r0 != 0 || t == 0)
                //if(sample)
				if(t % experienceAddEvery == 0)
                {

                    exp[expi] = new Experience(s0, a0, r0, s1, a1);
                    expi += 1;
                    if (expn < experienceSize)
                    {
                        expn += 1;
                    }
                    if (expi >= experienceSize)
                    {
                        expi = 0;
                    } // roll over when we run out
                }
                

                

                t += 1;

                //Debug.Log("length: " + exp.Length);
                // sample some additional experience from replay memory and learn from it

                int k = 0;

                while (k < learningStepsPerIteration)
                {
                    Experience experience = exp[Random.Range(0, expn)];

                    double[] s = new double[2];

                    s[0] = e.getEntropySingle(experience.s0);

                    s[1] = e.getEntropySingle(experience.s1);

                    Volume resultClassify = netClassify.Forward(new Volume(s));

                    double result = 1f;

                    result = resultClassify.Weights[0];

                    if (Random.Range(0f, 1f) < result)
                    {
                        learnFromTuple(experience.s0, experience.a0, experience.r0, experience.s1, experience.a1);

                        k++;
                    }
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

            //double tderror = pred[a0] - qmax;

            double tderror = qmax - pred[a0];

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

			trainer.Train (new Volume (s0), tderror, a0);

            return tderror;

        }

        public static void writeData(string buffer, string fn)
        {
            System.IO.StreamWriter sr = new System.IO.StreamWriter(FILE_NAME + fn + ".txt", true);
            sr.WriteLine(buffer);
            sr.Close();
        }

        void outputList(List<double> list,string name)

        {

            string s = "";

            foreach (var item in list)
            {

                s += item.ToString() + ",";

            }

            writeData(s, name);


        }

        List<double> netToList(Net network)
        {

            List<double> weightsArray = new List<double>();

            foreach (var layer in network.Layers)
            {

                List<ParametersAndGradients> g = layer.GetParametersAndGradients();


                foreach (var weights in g)
                {

                    foreach (var weight in weights.Parameters)
                    {

                        weightsArray.Add(weight);
                    }


                }
            }

            return weightsArray;
        }

        void ListToNet(Net network, List<double> weightsArray)
        {

            int count = 0;

            foreach (var layer in network.Layers)
            {

                List<ParametersAndGradients> g = layer.GetParametersAndGradients();


                foreach (var weights in g)
                {

                    for(int i=0;i<weights.Parameters.Length;i++)
                    {
                        weights.Parameters[i] = weightsArray.ElementAt(count);

                        count++;

                    }


                }
            }
        }

        public void loadNet(List<double> weightsArray)
        {

            ListToNet(netClassify, weightsArray);

        }

        public List<double> downloadNet()
        {

            return netToList(netClassify);
        }

        public double getReplayEntropy()
        {

            double sum = 0;

            int length = expn;

            int count = 0;

            for (int i = 0; i < length; i++)
            {
                if(exp[i].r0 == 0)
                    {
                    sum += e.getEntropySingle(exp[i].s0);
                    count++;
                    }
            }

            return sum / count;

        }


        public double getReplayCuriosity()
        {

            double sum = 0;

            int length = expn;

            int count = 0;


            for (int i = 0; i < length; i++)
            {

                if (exp[i].r0 == 0)
                {
                    sum += e.getEntropy(exp[i].s0, exp[i].s1);
                    count++;
                }
            }

            return sum / count;

        }

    }

}
