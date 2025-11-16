using System.Windows;
using System.Windows.Controls;
using GeneticAlgorithm.ViewModel;

namespace GeneticAlgorithm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += (s, e) => Window_SizeChanged(null, null);

            StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata
            {
                DefaultValue = FindResource(typeof(Window))
            });

            var viewModel = new WorkScheduleViewModel();
            DataContext = viewModel;

            viewModel.LoadWorkScheduleData();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WorkScheduleListView.View is GridView gridView)
            {
                // Adjust width for padding and scrollbar
                double totalWidth = WorkScheduleListView.ActualWidth - 35;

                if (gridView.Columns.Count == 6)
                {
                    gridView.Columns[0].Width = totalWidth * 0.1; // Date
                    gridView.Columns[1].Width = totalWidth * 0.1; // Day
                    gridView.Columns[2].Width = totalWidth * 0.1; // Change ID
                    gridView.Columns[3].Width = totalWidth * 0.1; // Client Counter
                    gridView.Columns[4].Width = totalWidth * 0.5; // Employees
                    gridView.Columns[5].Width = totalWidth * 0.1; // Employees Counter
                }
            }
        }

        private void OpenNewTab(object sender, RoutedEventArgs e)
        {
            var newWindow = new Prediction();
            newWindow.Show();
        }

        private void showEmployees(object sender, RoutedEventArgs e)
        {
            var newWindow = new Employees();
            newWindow.Show();
        }

        private void showEmployeesPreferences(object sender, RoutedEventArgs e)
        {
            var newWindow = new EmployeesPreferences();
            newWindow.Show();
        }
    }
}