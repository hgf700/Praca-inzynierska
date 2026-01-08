using AG;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tensorflow;
using Tensorflow.Keras.Engine;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Tensorflow.Binding;

namespace UI;

public partial class MainWindow : Window
{
    BlockingCollection<float[]> inputQueue = new();
    BlockingCollection<float[]> outputQueue = new();
    CancellationTokenSource cts;
    Task nnWorkerTask;
    bool nnRunning = false;

    public MainWindow()
    {
        InitializeComponent();
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
        string name = System.IO.Path.Combine(dir, $"model.onnx");
        return name;
    }

    private Task StartNNWorker(CancellationToken token)
    {
        return Task.Run(() =>
        {
            var model = GetModel();
            using var session = new InferenceSession(model);

            var modelResult = GetModelResultFileName();

            using var writer = new StreamWriter(modelResult, false, Encoding.UTF8);

            // 🔹 Nagłówek CSV
            writer.WriteLine(
                "day,shift,worker_id,preference,requirements,singleWorkerFitness,prediction"
            );

            try
            {
                foreach (var input in inputQueue.GetConsumingEnumerable(token))
                {
                    // 1️⃣ Tensor [1,6]
                    var tensor = new DenseTensor<float>(
                        input,
                        new[] { 1, input.Length }
                    );

                    var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input", tensor)
                };

                    // 2️⃣ Predykcja
                    using var results = session.Run(inputs);
                    float prediction = results.First().AsTensor<float>()[0];

                    // 3️⃣ Zapis do CSV
                    string csvLine =
                        $"{input[0]},{input[1]},{input[2]},{input[3]},{input[4]},{input[5]},{prediction}";

                    writer.WriteLine(csvLine);
                    writer.Flush(); // ⬅️ ważne przy długim działaniu

                    // 4️⃣ Opcjonalnie do kolejki
                    outputQueue.Add(new[] { prediction }, token);
                }
            }
            catch (OperationCanceledException)
            {
                // graceful shutdown
            }
        }, token);
    }


    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {

        if (nnRunning)
            return;

        nnRunning = true;

        cts = new CancellationTokenSource();

        // 🔥 start NN worker
        nnWorkerTask = StartNNWorker(cts.Token);

        var ag = new GeneticAlgorithm();

        try
        {
            var result = await Task.Run(() => ag.Run(cts.Token));
            MessageBox.Show($"Best fitness: {result.BestFitness} AG completed");
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("Alghorytm stopped");
        }
        finally
        {
            nnRunning = false;
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        cts?.Cancel();
        inputQueue.CompleteAdding();
    }
}

