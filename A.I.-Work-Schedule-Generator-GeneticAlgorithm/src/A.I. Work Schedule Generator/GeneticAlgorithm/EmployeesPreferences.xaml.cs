using System.Windows;
using System.Windows.Controls;
using GeneticAlgorithm.ViewModel;

namespace GeneticAlgorithm
{
    /// <summary>
    /// Interaction logic for EmployeesPreferences.xaml
    /// </summary>
    public partial class EmployeesPreferences : Window
    {
        public EmployeesPreferences()
        {
            InitializeComponent();

            Loaded += (s, e) => Window_SizeChanged(null, null);

            var viewModel = new EmployeePreferencesViewModel();
            DataContext = viewModel;

            viewModel.LoadEmployeePreferencesData();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (EmployeePrederencesListView.View is GridView gridView)
            {
                // Adjust width for padding and scrollbar
                double totalWidth = EmployeePrederencesListView.ActualWidth - 35;

                if (gridView.Columns.Count == 7)
                {
                    gridView.Columns[0].Width = totalWidth * 0.22; // Employee
                    gridView.Columns[1].Width = totalWidth * 0.13; // Monday
                    gridView.Columns[2].Width = totalWidth * 0.13; // Tuesday
                    gridView.Columns[3].Width = totalWidth * 0.13; // Wednesday
                    gridView.Columns[4].Width = totalWidth * 0.13; // Thursday
                    gridView.Columns[5].Width = totalWidth * 0.13; // Friday
                    gridView.Columns[6].Width = totalWidth * 0.13; // Saturday
                }
            }
        }
    }
}
