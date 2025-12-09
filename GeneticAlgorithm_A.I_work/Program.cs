using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace GeneticAlgorithm
{
    public static class FitnessConstants
    {
        public const int workersPerDayPenalty = 10;
        public const int EmployeePreferenceMultiplier = 5;
    }

    public class Program
    {
        private Random random = new Random();
        private int populationSize = 100;
        private int generations = 100;
        private double mutationRate = 0.05;

        private static int LowClient = 50;
        private static int HighClient = 200;

        private static int numEmployees;
        private static int numActualDays;
        private const int numShiftsPerDay = 3;
        private static int numTimeSlots;

        private static int[] requiredWorkersPerShiftDisplay;
        private static int[] requiredWorkersPerShiftNumeric;
        private static int[,] employeePreferences;

        static Program()
        {
            string csv = "grafik_7d_3s_10emp.csv";
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Data", csv);

            if (!File.Exists(file))
            {
                Console.WriteLine($"Plik nie istnieje: {file}");
                return;
            }

            requiredWorkersPerShiftDisplay = ReadRequirementsFromFile(file);
            employeePreferences = ReadPreferencesFromFile(file);

            numEmployees = employeePreferences.GetLength(0);
            numActualDays = requiredWorkersPerShiftDisplay.Length / numShiftsPerDay;
            numTimeSlots = numActualDays * numShiftsPerDay;

            requiredWorkersPerShiftNumeric = (int[])requiredWorkersPerShiftDisplay.Clone();
        }

        private static int[] ReadRequirementsFromFile(string fileName)
        {
            string[] lines = File.ReadAllLines(fileName);
            string[] headers = lines[0].Split(',');
            string[] values = lines[1].Split(',');

            List<int> reqValues = new List<int>();
            for (int j = 0; j < headers.Length; j++)
            {
                if (headers[j].StartsWith("req") && j < values.Length)
                    reqValues.Add(int.Parse(values[j]));
            }

            return reqValues.ToArray();
        }

        private static int[,] ReadPreferencesFromFile(string fileName)
        {
            string[] lines = File.ReadAllLines(fileName);
            string[] headers = lines[2].Split(',');

            List<int> prefCols = new List<int>();
            for (int i = 0; i < headers.Length; i++)
                if (headers[i].StartsWith("pref"))
                    prefCols.Add(i);

            int numEmp = lines.Length - 3;
            int numShifts = prefCols.Count;
            int[,] preferences = new int[numEmp, numShifts];

            for (int row = 3; row < lines.Length; row++)
            {
                string[] values = lines[row].Split(',');
                int empIndex = row - 3;
                for (int c = 0; c < numShifts; c++)
                    if (prefCols[c] < values.Length)
                        preferences[empIndex, c] = int.Parse(values[prefCols[c]]);
            }
            return preferences;
        }
        static int UnsolvedWorkerRequirmnts(int[,] schedule)
        {
            int workersPenalty = CalculateWorkersPenalty(schedule);
            return workersPenalty;
        }

        static int UnsolvedFirmRequirmnts(int[,] schedule)
        {
            int preferenceBonus = CalculatePreferenceBonus(schedule);
            int pref = preferenceBonus / 10;
            return pref;
        }
        private int[,] GenerateSchedule()
        {
            int[,] schedule = new int[numEmployees, numTimeSlots];
            for (int i = 0; i < numEmployees; i++)
                for (int j = 0; j < numTimeSlots; j++)
                    schedule[i, j] = random.Next(2);
            return schedule;
        }

        static int[] CalculateEmployeeFitness(int[,] schedule, int employeeIndex)
        {
            int numPref = employeePreferences.GetLength(1);
            int[] bonus = new int[numPref];

            for (int j = 0; j < numPref; j++)
            {
                if (schedule[employeeIndex, j] == employeePreferences[employeeIndex, j])
                    bonus[j] = FitnessConstants.EmployeePreferenceMultiplier;
                else
                    bonus[j] = 0;
            }

            return bonus;
        }

        static int CalculateWorkersPenalty(int[,] schedule)
        {
            int penalty = 0;
            for (int j = 0; j < numTimeSlots; j++)
            {
                int actualWorkers = 0;
                for (int i = 0; i < numEmployees; i++)
                    actualWorkers += schedule[i, j];
                penalty += FitnessConstants.workersPerDayPenalty * Math.Abs(requiredWorkersPerShiftNumeric[j] - actualWorkers);
            }
            return penalty;
        }

        static int CalculatePreferenceBonus(int[,] schedule)
        {
            int bonus = 0;
            for (int i = 0; i < numEmployees; i++)
                for (int j = 0; j < employeePreferences.GetLength(1); j++)
                    if (schedule[i, j] == employeePreferences[i, j])
                        bonus += FitnessConstants.EmployeePreferenceMultiplier;
            return bonus;
        }

        private int Fitness(int[,] schedule, List<int> clientCounts)
        {
            int baseFitness = 0;
            for (int j = 0; j < numTimeSlots; j++)
            {
                int sum = 0;
                for (int i = 0; i < numEmployees; i++)
                    sum += schedule[i, j];
                int ideal = Math.Max(1, clientCounts[j] / 20);
                baseFitness += Math.Abs(sum - ideal);
            }
            int workerPenalty = CalculateWorkersPenalty(schedule);
            int preferenceBonus = CalculatePreferenceBonus(schedule);
            return baseFitness + workerPenalty - preferenceBonus;
        }

        private int[,] Crossover(int[,] parent1, int[,] parent2)
        {
            int[,] child = new int[numEmployees, numTimeSlots];
            for (int i = 0; i < numEmployees; i++)
            {
                int split = random.Next(numTimeSlots);
                for (int j = 0; j < split; j++)
                    child[i, j] = parent1[i, j];
                for (int j = split; j < numTimeSlots; j++)
                    child[i, j] = parent2[i, j];
            }
            return child;
        }

        private void Mutate(int[,] individual)
        {
            for (int i = 0; i < numEmployees; i++)
                for (int j = 0; j < numTimeSlots; j++)
                    if (random.NextDouble() < mutationRate)
                        individual[i, j] = random.Next(2);
        }

        private int[,] Select(List<int[,]> population, List<int> clientCounts)
        {
            var tournament = population.OrderBy(x => random.Next()).Take(5).ToList();
            return tournament.OrderBy(x => Fitness(x, clientCounts)).First();
        }

        public int[,] Optimize(List<int> clientCounts, string logFile, string resultFile)
        {
            List<int[,]> population = new List<int[,]>();
            for (int i = 0; i < populationSize; i++)
                population.Add(GenerateSchedule());

            // Writer 1 - logi, pozostaje bez zmian
            using StreamWriter writer = new StreamWriter(logFile, false, new UTF8Encoding(true));
            writer.WriteLine("Generation;BestFitness;AverageFitness;MutationRate;MutationCount");

            for (int gen = 0; gen < generations; gen++)
            {
                int mutationCount = 0;

                population = population.OrderBy(ind => Fitness(ind, clientCounts)).ToList();
                int bestFitness = Fitness(population[0], clientCounts);
                double avgFitness = population.Average(ind => Fitness(ind, clientCounts));

                List<int[,]> newPopulation = new List<int[,]>();
                while (newPopulation.Count < populationSize)
                {
                    var parent1 = Select(population, clientCounts);
                    var parent2 = Select(population, clientCounts);
                    var child = Crossover(parent1, parent2);
                    Mutate(child);
                    newPopulation.Add(child);
                }
                population = newPopulation;

                writer.WriteLine($"{gen + 1};{bestFitness};{Math.Round(avgFitness, 2).ToString().Replace('.', ',')};{mutationRate.ToString().Replace('.', ',')};{mutationCount}");
            }

            int[,] finalSchedule = population[0];

            // Writer 2 - poprawione wyświetlanie i zapis CSV
            using StreamWriter writer2 = new StreamWriter(resultFile, false, new UTF8Encoding(true));
            string header = GetShiftHeaders(",");
            string[] headers = header.Split(',');

            writer2.Write("Id,");
            foreach (var h in headers) writer2.Write($"P_{h},");
            foreach (var h in headers) writer2.Write($"FR_{h},");
            foreach (var h in headers) writer2.Write($"S_{h},");
            writer2.Write("worker_fitness,mismatchWorkerRequirments,mismatchFirmRequirmments\n");

            for (int i = 0; i < numEmployees; i++)
            {
                writer2.Write($"{i},");
                // Preferences
                for (int j = 0; j < numTimeSlots; j++)
                    writer2.Write($"{(j < employeePreferences.GetLength(1) ? employeePreferences[i, j] : 0)},");
                // Requirements
                for (int j = 0; j < numTimeSlots; j++)
                    writer2.Write($"{(j < requiredWorkersPerShiftDisplay.Length ? requiredWorkersPerShiftDisplay[j] : 0)},");
                // Schedule
                for (int j = 0; j < numTimeSlots; j++)
                    writer2.Write($"{(j < finalSchedule.GetLength(1) ? finalSchedule[i, j] : 0)}{(j < numTimeSlots - 1 ? "," : "")}");
                writer2.Write(",");

                // Fitness i penalty
                int fitness = CalculateEmployeeFitness(finalSchedule, i).Sum();
                int unsolvedWorker = UnsolvedWorkerRequirmnts(finalSchedule);
                int unsolvedFirm = UnsolvedFirmRequirmnts(finalSchedule);

                writer2.Write($"{fitness},{unsolvedWorker},{unsolvedFirm}\n");

                Console.WriteLine($"Pracownik {i}: Fitness={fitness}, UnsolvedWorker={unsolvedWorker}, UnsolvedFirm={unsolvedFirm}");
            }

            return finalSchedule;
        }

        static string GetShiftHeaders(string delimiter)
        {
            StringBuilder sb = new StringBuilder();
            for (int d = 1; d <= numActualDays; d++)
                for (int s = 1; s <= numShiftsPerDay; s++)
                    sb.Append($"D{d}S{s}{(d == numActualDays && s == numShiftsPerDay ? "" : delimiter)}");
            return sb.ToString();
        }

        static string GetLogFileName()
        {
            string logDirectory = "../../../logi";
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);
            return Path.Combine(logDirectory, $"genetic_log.csv");
        }

        static string GetResultFileName()
        {
            string logDirectory = "../../../wyniki";
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            int logNumber = 1;
            string logFileName;
            do
            {
                logFileName = Path.Combine(logDirectory, $"genetic_Result{logNumber}.csv");
                logNumber++;
            } while (File.Exists(logFileName));

            return logFileName;
        }

        static void Main(string[] args)
        {
            Program scheduler = new Program();
            Random randomClient = new Random();

            List<int> clientCounts = new List<int>();
            for (int i = 0; i < numShiftsPerDay * numActualDays; i++)
                clientCounts.Add(randomClient.Next(LowClient, HighClient));

            string logFileName = GetLogFileName();
            string resultFile = GetResultFileName();

            int[,] bestSchedule = scheduler.Optimize(clientCounts, logFileName, resultFile);

            Console.WriteLine("Najlepszy harmonogram:");
            for (int i = 0; i < numEmployees; i++)
            {
                for (int j = 0; j < numTimeSlots; j++)
                    Console.Write(bestSchedule[i, j] + " ");
                Console.WriteLine();
            }

            Console.WriteLine($"Log zapisany w: {logFileName}");
            Console.WriteLine($"Wyniki zapisane w: {resultFile}");
            Console.ReadKey();
        }
    }
}
