using GeneticAlgorithm.ViewModel;


namespace UnitTests
{
    public class UnitTest1
    {
        [Fact]
        public void JsonData_Exists()
        {
            string fileName1 = "../../../../GeneticAlgorithm/Data/Employee.json";
            string jsonString1 = File.ReadAllText(fileName1);
            Assert.False(string.IsNullOrEmpty(jsonString1));

            string fileName2 = "../../../../GeneticAlgorithm/Data/WorkSchedule.json";
            string jsonString2 = File.ReadAllText(fileName2);
            Assert.False(string.IsNullOrEmpty(jsonString2));
        }

        [Fact]
        public void ViewModelsCollections_AreEmpty()
        {
            var viewModel1 = new WorkSchedulePredictionViewModel();
            var viewModel2 = new WorkScheduleViewModel();
            var viewModel3 = new EmployeePreferencesViewModel();
            var viewModel4 = new EmployeeViewModel();

            Assert.Empty(viewModel1.WorkSchedulesPredictions);
            Assert.Empty(viewModel2.WorkSchedules);
            Assert.Empty(viewModel3.EmployeePreferences);
            Assert.Empty(viewModel4.Employees);
        }
    }
}