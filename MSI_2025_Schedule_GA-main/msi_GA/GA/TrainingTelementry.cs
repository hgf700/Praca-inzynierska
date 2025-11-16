using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNetwork1
{

    public class TrainingTelemetry
    {
        public int Iteration { get; set; }
        public double[][,] Weights { get; set; }
        public double[][] Bias { get; set; }
        public double[] Error { get; set; }

    }
}
