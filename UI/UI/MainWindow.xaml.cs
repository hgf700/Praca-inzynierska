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

namespace UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void OnClick1(object sender, RoutedEventArgs e)
        {
            btn1.IsEnabled = false;

            await Task.Run(() =>
            {
                //RunGeneticAlgorithm();
            });

            btn1.IsEnabled = true;
            MessageBox.Show("AG zakończony");
        }
    }
}