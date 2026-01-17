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
        }



        void ProcessCsvWithNN(CancellationToken token)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                //FileName=@"..\python_predict\venv\Scripts\python.exe",
                //WorkingDirectory = @"..\python_predict",

                FileName = @"C:\Users\USER098\Documents\GitHub\genekordos\UI\UI\python_predict\venv\Scripts\python.exe",
                Arguments = "pred.py",
                WorkingDirectory = @"C:\Users\USER098\Documents\GitHub\genekordos\UI\UI\python_predict",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new System.Diagnostics.Process();
            process.StartInfo = psi;

            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            MessageBox.Show($"STDOUT:\n{output}\n\nSTDERR:\n{error}");

            if (process.ExitCode != 0)
            {
                throw new Exception($"Python error: {error}");
            }
        }


        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            cts = new CancellationTokenSource();
            var ag = new GeneticAlgorithm();

            try
            {
                // 1. AG generuje CSV
                await Task.Run(() => ag.Run(cts.Token));

                MessageBox.Show("AG zakończył generowanie danych. Teraz sieć neuronowa dokona oceny...");

                // 2. Python robi predykcję
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
