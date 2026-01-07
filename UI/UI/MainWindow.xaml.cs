using AG;
using System.Collections.Concurrent;
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
using static System.Runtime.InteropServices.JavaScript.JSType;
using Tensorflow;
using Tensorflow.Keras.Engine;
using static Tensorflow.Binding;

namespace UI;

public partial class MainWindow : Window
{
    BlockingCollection<float[]> inputQueue = new();
    BlockingCollection<float[]> outputQueue = new();
    CancellationTokenSource cts;
    Task nnWorkerTask;

    public MainWindow()
    {
        InitializeComponent();
    }

    //private Task StartNNWorker(CancellationToken token)
    //{
    //    return Task.Run(() =>
    //    {
    //        var model = LoadModel();

    //        try
    //        {
    //            foreach (var input in inputQueue.GetConsumingEnumerable(token))
    //            {
    //                var prediction = model.Predict(input);
    //                outputQueue.Add(prediction, token);
    //            }
    //        }
    //        catch (OperationCanceledException) { }
    //    }, token);
    //}

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        cts = new CancellationTokenSource();

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
    }
    //private Model LoadModel()
    //{
    //    // Ładowanie modelu Keras w TensorFlow.NET
    //    var model = tf.keras.models.load_model("saved_model/model.keras");
    //    return model;
    //}
    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        cts?.Cancel();
        inputQueue.CompleteAdding();
    }
    
}