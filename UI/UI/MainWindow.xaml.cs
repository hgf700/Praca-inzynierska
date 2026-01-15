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

            // Inicjalny nagłówek (tylko raz)
            File.WriteAllText(modelResult, "day,shift,preference,requirements,singleWorkerFitness,prediction\n");

            return Task.Run(() =>
            {
                dynamic model;
                using (Py.GIL())
                {
                    dynamic tf = Py.Import("tensorflow");
                    model = tf.keras.models.load_model(GetModel());
                }

                while (!token.IsCancellationRequested)
                {
                    float[] input;
                    try
                    {
                        input = inputQueue.Take(token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    double result;
                    using (Py.GIL())
                    {
                        dynamic np = Py.Import("numpy");
                        dynamic npInput = np.array(input).reshape(1, -1);
                        dynamic pred = model.predict(npInput);
                        result = pred[0][0].As<double>();
                    }

                    // Zapis do CSV – dzień i zmiana jako przykładowe kolumny (dostosuj jeśli masz inne dane)
                    string csvLine = $"{input[0]},{input[1]},{input[2]},{input[3]},{input[4]},{result:F6}";
                    File.AppendAllText(modelResult, csvLine + Environment.NewLine);

                    // Opcjonalnie – zwróć wynik do kolejki wyjściowej (jeśli AG ma go potrzebować)
                    outputQueue.Add(new float[] { (float)result });
                }
            }, token);
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            cts = new CancellationTokenSource();

            // Uruchom worker NN – teraz będzie czekał na dane
            var workerTask = StartNNWorker(cts.Token);

            // Dodaj dane do kolejki wejściowej (np. z AG lub ręcznie)
            inputQueue.Add(new float[] { 1, 2, 3, 4, 5 });
            inputQueue.Add(new float[] { 2, 3, 4, 5, 6 });
            inputQueue.Add(new float[] { 3, 4, 5, 6, 7 });

            var ag = new GeneticAlgorithm();

            try
            {
                var result = await Task.Run(() => ag.Run(inputQueue, outputQueue, cts.Token));
                MessageBox.Show($"Best fitness: {result.BestFitness}\nAG completed\nNN results saved to CSV");
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Algorytm zatrzymany");
            }

            // Zakończ kolejkę wejściową – worker NN wyjdzie z pętli
            inputQueue.CompleteAdding();

            // Poczekaj na zakończenie worker NN
            await workerTask;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();
            inputQueue.CompleteAdding();
        }
    }
}
