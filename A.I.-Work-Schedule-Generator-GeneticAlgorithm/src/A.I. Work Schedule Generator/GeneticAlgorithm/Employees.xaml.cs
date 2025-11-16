using System.Windows;
using System.Windows.Controls;
using GeneticAlgorithm.ViewModel;

namespace GeneticAlgorithm
{
    /// <summary>
    /// Interaction logic for Employees.xaml
    /// </summary>
    public partial class Employees : Window
    {
        public Employees()
        {
            InitializeComponent();

            Loaded += (s, e) => Window_SizeChanged(null, null);

            var viewModel = new EmployeeViewModel();
            DataContext = viewModel;

            viewModel.LoadEmployeeData();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (EmployeeListView.View is GridView gridView)
            {
                // Adjust width for padding and scrollbar
                double totalWidth = EmployeeListView.ActualWidth - 35;

                if (gridView.Columns.Count == 5)
                {
                    gridView.Columns[0].Width = totalWidth * 0.1; // ID
                    gridView.Columns[1].Width = totalWidth * 0.15; // Name
                    gridView.Columns[2].Width = totalWidth * 0.15; // Last name
                    gridView.Columns[3].Width = totalWidth * 0.4; // Email
                    gridView.Columns[4].Width = totalWidth * 0.2; // Phone number
                }
            }
        }
    }
}