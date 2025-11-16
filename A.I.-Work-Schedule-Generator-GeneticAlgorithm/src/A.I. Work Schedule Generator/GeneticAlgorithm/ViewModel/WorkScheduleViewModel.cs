using GeneticAlgorithm.Model;
using GeneticAlgorithm.NVVM;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace GeneticAlgorithm.ViewModel
{
    public class WorkScheduleViewModel : ViewModelBase
    {
        private ObservableCollection<WorkScheduleModel> _workSchedules = new();
        public ObservableCollection<WorkScheduleModel> WorkSchedules
        {
            get => _workSchedules;
            set
            {
                _workSchedules = value;
                OnPropertyChanged(nameof(WorkSchedules));
            }
        }

        // Load work schedule data from a JSON file => WorkSchedule.json
        public void LoadWorkScheduleData()
        {
            const string fileName = "../../../Data/WorkSchedule.json";

            try
            {
                string jsonString = File.ReadAllText(fileName);

                var schedules = JsonSerializer.Deserialize<List<WorkScheduleModel>>(jsonString);

                if (schedules is not null)
                    WorkSchedules = new ObservableCollection<WorkScheduleModel>(schedules);
            }
            catch (Exception ex) when (ex is FileNotFoundException or JsonException)
            {
                throw new ApplicationException($"Failed to load work schedule data from {fileName}", ex);
            }

            if (WorkSchedules.Count == 0)
            {
                throw new Exception("No work schedule data found.");
            }
        }
    }
}
