using System;
using System.Collections.Generic;
using ConvNetSharp.Layers;

namespace ConvNetSharp.Training
{
    public class AdadeltaTrainer : TrainerBase
    {
        private readonly List<double[]> gsum = new List<double[]>(); // last iteration gradients (used for momentum calculations)
        private readonly List<double[]> xsum = new List<double[]>();

        public AdadeltaTrainer(Net net) : base(net)
        {
            this.Ro = 0.95;
            this.Eps = 1e-6;
        }

        public double L1Decay { get; set; }

        public double L2Decay { get; set; }

        public double L2DecayLoss { get; private set; }

        public double L1DecayLoss { get; private set; }

        public double Ro { get; set; }

        public double Eps { get; set; }

        protected override void TrainImplem()
        {
            this.K++;
            if (this.K % this.BatchSize == 0)
            {
                List<ParametersAndGradients> parametersAndGradients = this.Net.GetParametersAndGradients();

                // initialize lists for accumulators. Will only be done once on first iteration
                if (this.gsum.Count == 0)
                {
                    foreach (var t in parametersAndGradients)
                    {
                        this.gsum.Add(new double[t.Parameters.Length]);
                        this.xsum.Add(new double[t.Parameters.Length]);
                    }
                }

                // perform an update for all sets of weights
                for (var i = 0; i < parametersAndGradients.Count; i++)
                {
                    var parametersAndGradient = parametersAndGradients[i];
                    // param, gradient, other options in future (custom learning rate etc)
                    double[] parameters = parametersAndGradient.Parameters;
                    double[] gradients = parametersAndGradient.Gradients;

                    // learning rate for some parameters.
                    var l2DecayMul = parametersAndGradient.L2DecayMul ?? 1.0;
                    var l1DecayMul = parametersAndGradient.L1DecayMul ?? 1.0;
                    var l2Decay = this.L2Decay * l2DecayMul;
                    var l1Decay = this.L1Decay * l1DecayMul;

                    var plen = parameters.Length;
                    for (var j = 0; j < plen; j++)
                    {
                        this.L2DecayLoss += l2Decay * parameters[j] * parameters[j] / 2; // accumulate weight decay loss
                        this.L1DecayLoss += l1Decay * Math.Abs(parameters[j]);
                        var l1Grad = l1Decay * (parameters[j] > 0 ? 1 : -1);
                        var l2Grad = l2Decay * parameters[j];

                        var gij = (l2Grad + l1Grad + gradients[j]) / this.BatchSize; // raw batch gradient

                        double[] gsumi = null;
                        if (this.gsum.Count > 0)
                        {
                            gsumi = this.gsum[i];
                        }

                        double[] xsumi = null;
                        if (this.xsum.Count > 0)
                        {
                            xsumi = this.xsum[i];
                        }

                        gsumi[j] = this.Ro * gsumi[j] + (1 - this.Ro) * gij * gij;
                        var dx = -Math.Sqrt((xsumi[j] + this.Eps) / (gsumi[j] + this.Eps)) * gij;
                        xsumi[j] = this.Ro * xsumi[j] + (1 - this.Ro) * dx * dx; // yes, xsum lags behind gsum by 1.
                        parameters[j] += dx;

                        gradients[j] = 0.0; // zero out gradient so that we can begin accumulating anew
                    }
                }
            }

            // in future, TODO: have to completely redo the way loss is done around the network as currently 
            // loss is a bit of a hack. Ideally, user should specify arbitrary number of loss functions on any layer
            // and it should all be computed correctly and automatically. 
        }

        protected override void Backward(double y)
        {
            base.Backward(y);

            this.L2DecayLoss = 0.0;
            this.L1DecayLoss = 0.0;
        }

        protected override void Backward(double[] y)
        {
            base.Backward(y);

            this.L2DecayLoss = 0.0;
            this.L1DecayLoss = 0.0;
        }
    }
}