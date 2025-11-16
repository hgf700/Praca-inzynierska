using System.Text.Json.Serialization;
using GeneticAlgorithm.ExternalClasses;

namespace GeneticAlgorithm.Model
{
    public class WorkScheduleModel
    {
        private DateTime _date;

        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime Date
        {
            get => _date;
            set => _date = value;
        }

        public string FormattedDate => Date.ToString("yyyy-MM-dd");

        public string DayOfWeek => Date.DayOfWeek.ToString();
        public string? ChangeId { get; set; }
        public string[]? Employees { get; set; }
        public string? ClientCounter { get; set; }
        public int EmployeeCounter => Employees?.Length ?? 0;
    }
}
