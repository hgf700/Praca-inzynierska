using GeneticAlgorithm.Model;
using GeneticAlgorithm.NVVM;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace GeneticAlgorithm.ViewModel
{
    public class EmployeePreferencesViewModel : ViewModelBase
    {
        private ObservableCollection<EmployeePreferencesModel> _employeePreferences = new();
        public ObservableCollection<EmployeePreferencesModel> EmployeePreferences
        {
            get => _employeePreferences;
            set
            {
                _employeePreferences = value;
                OnPropertyChanged(nameof(EmployeePreferences));
            }
        }

        // Load employee preferences data from a JSON file => EmployeePreferences.json
        public void LoadEmployeePreferencesData()
        {
            const string fileName = "../../../Data/EmployeePreferences.json";

            try
            {
                string jsonString = File.ReadAllText(fileName);

                var employeePreferences = JsonSerializer.Deserialize<List<EmployeePreferencesModel>>(jsonString);

                if (employeePreferences is not null)
                    EmployeePreferences = new ObservableCollection<EmployeePreferencesModel>(employeePreferences);
            }
            catch (Exception ex) when (ex is FileNotFoundException or JsonException)
            {
                throw new ApplicationException($"Failed to load employee preferences data from {fileName}", ex);
            }

            if (EmployeePreferences.Count == 0)
            {
                throw new Exception("No employee preferences data found.");
            }
        }
    }
}
