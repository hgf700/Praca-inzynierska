using GeneticAlgorithm.ExternalClasses;
using GeneticAlgorithm.Model;
using GeneticAlgorithm.NVVM;
using System.Collections.ObjectModel;

namespace GeneticAlgorithm.ViewModel
{
    public class WorkSchedulePredictionViewModel : ViewModelBase
    {
        private ObservableCollection<WorkSchedulePredictionModel> _workSchedulesPredictions = new();
        public ObservableCollection<WorkSchedulePredictionModel> WorkSchedulesPredictions
        {
            get => _workSchedulesPredictions;
            set
            {
                _workSchedulesPredictions = value;
                OnPropertyChanged(nameof(WorkSchedulesPredictions));
            }
        }

        // Add new day after 3 days: Monday 3 times -> Tuesday 3 times -> Wednesday 3 times e.t.c.
        public string AddNewDay(string date, int addedDays)
        {
            DateTime parsedDate = DateTime.Parse(date);
            DateTime nextDay = parsedDate.AddDays(addedDays);
            return nextDay.ToString("yyyy-MM-dd");
        }

        // Based on work schedules data, create optimal work schedules predictions
        public void LoadWorkSchedulePredictionData(ObservableCollection<WorkScheduleModel> workSchedules)
        {
            int addedDays = 1;

            // Generating new days of the week and DD/MM/YY
            for (int i = 1; i <= workSchedules.Count; i++)
            {
                if (i == 1)
                {
                    addedDays = 2;
                }

                string nextDay = AddNewDay(workSchedules[workSchedules.Count - 1].Date.ToString("yyyy-MM-dd"), addedDays);
                string changeId = workSchedules[i - 1].ChangeId;

                var prediction = new WorkSchedulePredictionModel
                {
                    Date = nextDay,
                    ChangeId = changeId!
                };

                WorkSchedulesPredictions.Add(prediction);

                if (i % 3 == 0)
                {
                    addedDays += 1;
                }
            }

            var clientCounts = workSchedules.Select(ws => int.Parse(ws.ClientCounter!)).ToList();
            var daysOfWeek = workSchedules.Select(ws => ws.DayOfWeek).ToList();

            /*
                Using genetic algorithm reference method for optimization
            */
            var geneticScheduler = new GeneticScheduler();
            var optimizedEmployeeCounts = geneticScheduler.Optimize(clientCounts, daysOfWeek);

            for (int i = 0; i < WorkSchedulesPredictions.Count; i++)
            {
                WorkSchedulesPredictions[i].EmployeeIdCount = optimizedEmployeeCounts[i];
            }

            if (workSchedules.Count == 0)
                throw new Exception("No data inside work schedules predictions");

            // Another loop over the work schedules predictions for analizing and catching employees for changes based on employee counter of each change
            var employeePreferences = new EmployeePreferencesViewModel();
            employeePreferences.LoadEmployeePreferencesData();

            if (!employeePreferences.EmployeePreferences.Any())
                throw new Exception("No data inside work employees preferences");

            // Employees and their availability
            List<string> employees = new();
            List<int> employeesUsed = new();

            // Availability per day based on employee preferences in json file => EmployeePreferences.json
            Dictionary<string, List<string>> availabilityPerDay = new()
            {
                { "Monday", new List<string>() },
                { "Tuesday", new List<string>() },
                { "Wednesday", new List<string>() },
                { "Thursday", new List<string>() },
                { "Friday", new List<string>() },
                { "Saturday", new List<string>() }
            };

            // Adding employees and their availability
            foreach (var pref in employeePreferences.EmployeePreferences)
            {
                employees.Add(pref.Employee);
                employeesUsed.Add(0);
                availabilityPerDay["Monday"].Add(pref.Monday);
                availabilityPerDay["Tuesday"].Add(pref.Tuesday);
                availabilityPerDay["Wednesday"].Add(pref.Wednesday);
                availabilityPerDay["Thursday"].Add(pref.Thursday);
                availabilityPerDay["Friday"].Add(pref.Friday);
                availabilityPerDay["Saturday"].Add(pref.Saturday);
            }

            // Adding employees to work schedule prediction for next week
            foreach (var prediction in WorkSchedulesPredictions)
            {
                var availability = availabilityPerDay[prediction.DayOfWeek];
                int sum = 0; // sum of employees used for each day

                // Sorting employees by availability and random index
                var sortedIndices = employeesUsed
                    .Select((count, index) => new { count, index })
                    .OrderBy(x => x.count)
                    .ThenBy(x => Guid.NewGuid())
                    .ToList();

                // Adding employees to prediction based on availability and employee counter
                foreach (var x in sortedIndices)
                {
                    int i = x.index;

                    // If sum of employees used is greater than employee counter
                    if (sum >= prediction.EmployeeIdCount)
                        break;

                    // Optimal solution each employee can be used maximum 5 times
                    if (employeesUsed[i] >= 5)
                        continue;

                    // If employee is available => add to prediction
                    if (availability[i] == prediction.ChangeId)
                    {
                        prediction.Employees.Add(employees[i]);
                        employeesUsed[i]++;
                        sum++;
                    }
                }
            }
        }
    }
}
