using AG;
using Python.Runtime;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Windows;

namespace UI
{
    public partial class MainWindow : Window
    {
        BlockingCollection<float[]> inputQueue = new();
        BlockingCollection<float[]> outputQueue = new();
        CancellationTokenSource cts;

        public MainWindow()
        {
            InitializeComponent();
            Runtime.PythonDLL = @"C:\Users\USER098\AppData\Local\Programs\Python\Python311\python311.dll";
            PythonEngine.Initialize();
        }

        static string GetModelResultFileName()
        {
            string dir = "../../../wyniki_modelu";
            Directory.CreateDirectory(dir);
            string name = System.IO.Path.Combine(dir, $"model_Result.csv");
            return name;
        }

        static string GetModel()
        {
            string dir = "../../../saved_model";
            Directory.CreateDirectory(dir);
            string name = System.IO.Path.Combine(dir, $"model.keras");
            return name;
        }

        private Task StartNNWorker(CancellationToken token)
        {
            var modelResult = GetModelResultFileName();

            return Task.Run(() =>
            {
                File.WriteAllText(modelResult, "day,shift,preference,requirements,singleWorkerFitness,prediction\n");

                dynamic model;
                using (Py.GIL())
                {
                    dynamic tf = Py.Import("tensorflow");
                    model = tf.keras.models.load_model(GetModel());
                }

                while (!token.IsCancellationRequested)
                {
                    float[] input;
                    try { input = inputQueue.Take(token); }
                    catch (OperationCanceledException) { break; }

                    double result;
                    using (Py.GIL())
                    {
                        dynamic np = Py.Import("numpy");
                        dynamic npInput = np.array(input).reshape(1, -1);
                        dynamic pred = model.predict(npInput);
                        result = pred[0][0].As<double>();
                    }

                    string csvLine = $"{input[0]},{input[1]},{input[2]},{input[3]},{input[4]},{result}";

                    File.AppendAllText(modelResult, csvLine + Environment.NewLine);
                    outputQueue.Add(new float[] { (float)result });
                }
            }, token);
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {

            cts = new CancellationTokenSource();

            var workerTask = StartNNWorker(cts.Token);

            inputQueue.Add(new float[] { 1, 2, 3, 4, 5 });
            inputQueue.Add(new float[] { 2, 3, 4, 5, 6 });
            inputQueue.Add(new float[] { 3, 4, 5, 6, 7 });

            var ag = new GeneticAlgorithm();

            try
            {
                var result = await Task.Run(() => ag.Run(inputQueue, outputQueue, cts.Token));
                MessageBox.Show($"Best fitness: {result.BestFitness} AG completed");
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Alghorytm stopped");
            }
            
            cts.Cancel();
            await workerTask;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();
            inputQueue.CompleteAdding();
        }
    }
}
