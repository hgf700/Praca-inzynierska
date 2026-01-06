using AG;
using System.Text;
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

namespace UI;

public partial class MainWindow : Window
{
    CancellationTokenSource cts;
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        cts = new CancellationTokenSource();

        var ag = new GeneticAlgorithm();

        try
        {
            var result = await Task.Run(() => ag.Run(cts.Token));
            MessageBox.Show($"Best fitness: {result.BestFitness}");
            MessageBox.Show("AG zakończony");
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("Algorytm przerwany");
        }
    }
    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        cts?.Cancel();
    }
}