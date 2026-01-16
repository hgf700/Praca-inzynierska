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


        void Pr()
        {
            File.WriteAllText(za,"")
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "python";
            process.StartInfo.Arguments = "pred.py";
            process.Start();
            process.WaitForExit();
            string harm = File.ReadAllText("");

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
        static string GetResultFileName()
        {
            string dir = "../../../wyniki";
            Directory.CreateDirectory(dir);
            string name = Path.Combine(dir, $"genetic_Result.csv");
            return name;
        }

        private void ProcessCsvWithNN(CancellationToken token)
        {
            var inputPath = GetResultFileName();      // Plik wejściowy z AG
            var outputPath = GetModelResultFileName(); // Nowy plik z wynikami NN

            if (!File.Exists(inputPath))
            {
                MessageBox.Show("Plik wejściowy AG nie istnieje!");
                return;
            }

            var lines = File.ReadAllLines(inputPath).ToList();
            if (lines.Count <= 1) return;

            string header = lines[0];
            var dataRows = lines.Skip(1).ToList();

            using (Py.GIL())
            {
                dynamic tf = Py.Import("tensorflow");
                dynamic np = Py.Import("numpy");
                dynamic model = tf.keras.models.load_model(GetModel());

                StringBuilder sb = new StringBuilder();
                // Dodajemy nową kolumnę do nagłówka
                sb.AppendLine($"{header.TrimEnd()},prediction");

                foreach (var line in dataRows)
                {
                    if (token.IsCancellationRequested) break;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        // Parsowanie danych wejściowych
                        var values = line.Split(',').Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();

                        // Przygotowanie danych (np. pierwsze 5 kolumn)
                        var inputData = values.Take(5).ToArray();

                        // Predykcja
                        dynamic npInput = np.array(inputData).reshape(1, -1);
                        dynamic pred = model.predict(npInput, verbose: 0);
                        double prediction = pred[0][0].As<double>();

                        // Zapisanie starej linii + nowa kolumna
                        sb.AppendLine($"{line.TrimEnd()},{prediction.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}");
                    }
                    catch (Exception ex)
                    {
                        // Opcjonalnie: logowanie błędu parsowania konkretnej linii
                        continue;
                    }
                }

                // Zapis do NOWEGO pliku
                File.WriteAllText(outputPath, sb.ToString());
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            cts = new CancellationTokenSource();
            var ag = new GeneticAlgorithm();

            try
            {
                // 1. Uruchom AG (zapisuje dane do CSV bez predykcji)
                await Task.Run(() => ag.Run(cts.Token));

                MessageBox.Show("AG zakończył generowanie danych. Teraz sieć neuronowa dokona oceny...");

                // 2. Uruchom procesowanie CSV przez NN
                await Task.Run(() => ProcessCsvWithNN(cts.Token));

                MessageBox.Show("Proces zakończony! Wyniki dodane do pliku CSV.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}");
            }
        }
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();
            inputQueue.CompleteAdding();
        }
    }
}
