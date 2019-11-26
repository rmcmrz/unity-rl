using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AForge.Math;

namespace Qlearning.Entropy
{

    public class Entropy
    {

        private int numberOfPartitions;
        private float[,] directionsMatrix;
        private float step;
        private float[] directionsVector;

        public Entropy()
        {
            numberOfPartitions = 6;

            directionsVector = new float[numberOfPartitions + 2];

            directionsMatrix = new float[numberOfPartitions, 2];

            step = (float)1 / (numberOfPartitions / 2);

            Debug.Log("step: " + step);


            for (var i = 0; i < numberOfPartitions + 2; i++)
            {



                if (i <= numberOfPartitions / 2)
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

                if (i < numberOfPartitions / 2)
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

                    Debug.Log(i + "-" + j + " " + s);
                }
            }


        }

        // Use this for initialization
        void Start()
        {


            // create histogram array with 2 values of equal probabilities
            int[] histogram1 = new int[2] { 3, 3 };
            // calculate entropy
            double entropy1 = Statistics.Entropy(histogram1);
            // output it (1.000)
            Debug.Log("entropy1 = " + entropy1.ToString("F3"));

            // create histogram array with 4 values of equal probabilities
            int[] histogram2 = new int[4] { 1, 1, 1, 1 };
            // calculate entropy
            double entropy2 = Statistics.Entropy(histogram2);
            // output it (2.000)
            Debug.Log("entropy2 = " + entropy2.ToString("F3"));

            // create histogram array with 4 values of different probabilities
            int[] histogram3 = new int[4] { 2, 2, 3, 4 };
            // calculate entropy
            double entropy3 = Statistics.Entropy(histogram3);
            // output it (1.846)
            Debug.Log("entropy3 = " + entropy3.ToString("F3"));


        }

        public double getEntropy(double[] s0, double[] s1)
        {

            double entropys0 = Statistics.Entropy(getHistogram(toFloat(s0)));

            double entropys1 = Statistics.Entropy(getHistogram(toFloat(s1)));

            //Debug.Log(entropys0 + " " + entropys1 + " - " + (entropys1 - entropys0) );

            //return Statistics.Entropy(getHistogram(getDifference(s0, s1)));

            return entropys1 - entropys0;
        }

        public double getEntropySingle(double[] s0)
        {

            double entropy = Statistics.Entropy(getHistogram(toFloat(s0)));

            return entropy;
        }

        public int[] getHistogram(float[] array)
        {
            int[] histogram = new int[numberOfPartitions + 2];

            for (int i = 0; i < array.Length; i++)
            {
                histogram[discreteValue(array[i])]++;
            }

            return histogram;

        }

        private float[] getDifference(double[] s0, double[] s1)
        {
            int length = s0.Length;
            float[] difference = new float[length];

            for (int i = 0; i < length; i++)
            {
                difference[i] = (float)(s0[i] - s1[i]);
            }

            return difference;

        }

        private float[] toFloat(double[] s)
        {
            int length = s.Length;
            float[] difference = new float[length];

            for (int i = 0; i < length; i++)
            {
                difference[i] = (float)(s[i]);
            }

            return difference;

        }

        bool TestRange(float numberToCheck, double bottom, double top)
        {
            //Debug.Log(numberToCheck + " " + bottom + " " + top);
            return (numberToCheck > bottom && numberToCheck <= top);
        }

        private int discreteValue(float value)
        {
            for (int i = 0; i < directionsMatrix.GetLength(0); i++)
            {

                if (TestRange(value, directionsMatrix[i, 0], directionsMatrix[i, 1]))
                    return i;
            }

            if (value < 1)
            {
                return numberOfPartitions;
            }
            else if (value > 1)
            {
                return numberOfPartitions + 1;
            }

            return -1;
        }

    }

}