using System;

namespace NeuralNetwork1
{
    public class NeuralNetwork
    {
        private Neuron[][] Neurons;
        private double[][,] Weights;

        public int Iterations { get; set; } = 5000;
        public bool L2_Regularization { get; set; } = true;
        public double Lambda { get; set; } = 0.01;
        public double Alpha { get; set; } = 5.5;

        private int LastLayer;
        public Random Rnd { get; set; } = new Random();

        public Action<TrainingTelemetry> Monitor { get; set; }

        double AlphaMutation = 8.0;   // Smaller learning rate for slow 0→1 output
        double AlphaEtylist = 14.0;    // Larger learning rate for faster 1→0 output

        public NeuralNetwork(int[] layers)
        {
            LastLayer = layers.Length - 1;

            Neurons = new Neuron[layers.Length][];
            for (int l = 0; l < layers.Length; l++)
            {
                Neurons[l] = new Neuron[layers[l]];
                for (int n = 0; n < layers[l]; n++)
                    Neurons[l][n] = new Neuron();
            }

            InitializeWeights();
            InitializeBiases();
        }

        private void InitializeWeights()
        {
            int layers = Neurons.GetLength(0);
            Weights = new double[layers - 1][,];
            for (int l = 0; l < layers - 1; l++)
            {
                Weights[l] = new double[Neurons[l].Length, Neurons[l + 1].Length];
                for (int n = 0; n < Neurons[l].Length; n++)
                {
                    for (int j = 0; j < Neurons[l + 1].Length; j++)
                        Weights[l][n, j] = WeightFunction(l + 1);
                }
            }
        }

        private double WeightFunction(int layer)
        {
            var fanIn = (layer > 0) ? Neurons[layer - 1].Length : 0;
            var fanOut = Neurons[layer].Length;
            var b = Math.Sqrt(6) / Math.Sqrt(fanIn + fanOut);
            return Rnd.NextDouble() * 2 * b - b;
        }

        private void InitializeBiases()
        {
            int layers = Neurons.GetLength(0);
            for (int l = 1; l < layers; l++)
            {
                for (int n = 0; n < Neurons[l].Length; n++)
                {
                    Neurons[l][n].bias = 0.0;
                }
            }

            // First output starts near 0 (slow to reach 1)
            Neurons[LastLayer][0].bias = -6.0;  // sigmoid(-6) ≈ 0.0025

            // Second output starts near 1 (faster to reach 0)
            Neurons[LastLayer][1].bias = 6.0;   // sigmoid(6) ≈ 0.9975
        }

        public void Train(double[][] input, double[][] y)
        {
            InitializeWeights();
            InitializeBiases();

            int iteration = Iterations;
            var cost = new double[Neurons[LastLayer].Length];

            while (iteration-- > 0)
            {
                for (int i = 0; i < input.GetLength(0); i++)
                {
                    var output = Predict(input[i]);

                    // Error for each output neuron
                    for (int n = 0; n < Neurons[LastLayer].Length; n++)
                    {
                        cost[n] = Neurons[LastLayer][n].output - y[i][n];
                        Neurons[LastLayer][n].error = cost[n] * SigmoidPrime(Neurons[LastLayer][n].input + Neurons[LastLayer][n].bias);
                    }

                    BackPropagate();

                    // Update biases for hidden layers
                    for (int l = 1; l < LastLayer; l++)
                    {
                        for (int n = 0; n < Neurons[l].Length; n++)
                        {
                            Neurons[l][n].bias -= Alpha * Neurons[l][n].error;
                        }
                    }

                    // Update output layer biases with different learning rates
                    Neurons[LastLayer][0].bias -= AlphaMutation * Neurons[LastLayer][0].error; // Slow 0→1
                    Neurons[LastLayer][1].bias -= AlphaEtylist * Neurons[LastLayer][1].error; // Faster 1→0

                    // Update weights for hidden layers
                    for (int l = 0; l < LastLayer - 1; l++)
                    {
                        for (int j = 0; j < Neurons[l].Length; j++)
                        {
                            for (int k = 0; k < Neurons[l + 1].Length; k++)
                            {
                                Weights[l][j, k] -= Alpha * Neurons[l][j].output * Neurons[l + 1][k].error;
                                if (L2_Regularization)
                                    Weights[l][j, k] -= Lambda * Weights[l][j, k];
                            }
                        }
                    }

                    // Update weights to output layer with different learning rates
                    int lOutput = LastLayer - 1;
                    for (int j = 0; j < Neurons[lOutput].Length; j++)
                    {
                        Weights[lOutput][j, 0] -= AlphaMutation * Neurons[lOutput][j].output * Neurons[LastLayer][0].error;
                        Weights[lOutput][j, 1] -= AlphaEtylist * Neurons[lOutput][j].output * Neurons[LastLayer][1].error;

                        if (L2_Regularization)
                        {
                            Weights[lOutput][j, 0] -= Lambda * Weights[lOutput][j, 0];
                            Weights[lOutput][j, 1] -= Lambda * Weights[lOutput][j, 1];
                        }
                    }
                }

                if (Monitor != null)
                {
                    var bias = new double[Neurons.Length][];
                    for (int i = 0; i < bias.Length; i++)
                    {
                        bias[i] = new double[Neurons[i].Length];
                        for (int j = 0; j < bias[i].Length; j++)
                            bias[i][j] = Neurons[i][j].bias;
                    }

                    var telemetry = new TrainingTelemetry()
                    {
                        Iteration = iteration,
                        Weights = Weights,
                        Bias = bias,
                        Error = cost
                    };
                    Monitor(telemetry);
                }
            }
        }
        public double[] Predict(double[] input)
        {
            if (Weights == null)
                throw new InvalidOperationException("Network must be trained before prediction");



            for (int d = 0; d < Neurons[0].Length; d++)
                Neurons[0][d].output = input[d];

            for (int l = 1; l < Neurons.GetLength(0); l++)
            {
                for (int n = 0; n < Neurons[l].Length; n++)
                {
                    double sum = 0;
                    for (int j = 0; j < Neurons[l - 1].Length; j++)
                        sum += Neurons[l - 1][j].output * Weights[l - 1][j, n];

                    Neurons[l][n].input = sum;
                    Neurons[l][n].output = Sigmoid(sum + Neurons[l][n].bias);
                }
            }

            var outputlayer = Neurons.GetLength(0) - 1;
            var output = new double[Neurons[outputlayer].Length];
            for (int n = 0; n < output.Length; n++)
                output[n] = Neurons[outputlayer][n].output;

            return output;
        }

        public void BackPropagate()
        {
            for (int l = LastLayer - 1; l > 0; l--)
            {
                for (int n = 0; n < Neurons[l].Length; n++)
                {
                    double sum = 0.0;
                    for (int m = 0; m < Neurons[l + 1].Length; m++)
                    {
                        sum += Weights[l][n, m] * Neurons[l + 1][m].error;
                    }

                    Neurons[l][n].error = sum * SigmoidPrime(Neurons[l][n].input + Neurons[l][n].bias);
                }
            }
        }

        private static double Sigmoid(double x)
        {
            return 1.0 / (1.0 + Math.Exp(-x));
        }

        private static double SigmoidPrime(double x)
        {
            return Sigmoid(x) * (1.0 - Sigmoid(x));
        }

        private static double Prime(double z)
        {
            return z * (1.0 - z);
        }
    }


}
