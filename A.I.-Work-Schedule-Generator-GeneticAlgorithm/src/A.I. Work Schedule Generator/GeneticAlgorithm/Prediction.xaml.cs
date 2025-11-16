using System.Windows;
using System.Windows.Controls;
using GeneticAlgorithm.ViewModel;

namespace GeneticAlgorithm
{
    /// <summary>
    /// Interaction logic for Prediction.xaml
    /// </summary>
    public partial class Prediction : Window
    {
        public Prediction()
        {
            InitializeComponent();

            Loaded += (s, e) => Window_SizeChanged(null, null);

            var previousModel = new WorkScheduleViewModel();
            previousModel.LoadWorkScheduleData();

            var viewModel = new WorkSchedulePredictionViewModel();
            DataContext = viewModel;

            viewModel.LoadWorkSchedulePredictionData(previousModel.WorkSchedules);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WorkScheduleListView.View is GridView gridView)
            {
                // Adjust width for padding and scrollbar
                double totalWidth = WorkScheduleListView.ActualWidth - 35;

                if (gridView.Columns.Count == 5)
                {
                    gridView.Columns[0].Width = totalWidth * 0.15; // Date
                    gridView.Columns[1].Width = totalWidth * 0.15; // Day
                    gridView.Columns[2].Width = totalWidth * 0.10; // Change ID
                    gridView.Columns[3].Width = totalWidth * 0.50; // Employees
                    gridView.Columns[4].Width = totalWidth * 0.10; // Employees Counter
                }
            }
        }
    }
}
