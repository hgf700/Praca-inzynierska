using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace GeneticAlgorithm
{
    public class Program
    {
        private Random random = new Random();
        private int maxEmployees = 4;
        private int populationSize = 100;
        private int generations = 100;
        private double mutationRate = 0.05;

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
            numActualDays = (int)(requiredWorkersPerShiftDisplay.Length / numShiftsPerDay);
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

        static string GetShiftHeaders(string delimiter)
        {
            StringBuilder sb = new StringBuilder();
            for (int d = 1; d <= numActualDays; d++)
            {
                for (int s = 1; s <= numShiftsPerDay; s++)
                {
                    sb.Append($"D{d}S{s}");
                    if (!(d == numActualDays && s == numShiftsPerDay))
                        sb.Append(delimiter);
                }
            }
            return sb.ToString();
        }

        public List<int> Optimize(List<int> clientCounts, string logFile)
        {
            List<List<int>> population = new List<List<int>>();
            for (int i = 0; i < populationSize; i++)
                population.Add(RandomIndividual(clientCounts.Count));

            using StreamWriter writer = new StreamWriter(logFile, false, new UTF8Encoding(true));

            // Nagłówek CSV
            writer.WriteLine("Generation;BestFitness;AverageFitness;MutationRate;MutationCount");

            double mutationRateCurrent = mutationRate;
            Random rnd = new Random();

            for (int gen = 0; gen < generations; gen++)
            {
                int mutationCount = 0;

                // Sortowanie populacji po fitness
                population = population.OrderBy(ind => Fitness(ind, clientCounts)).ToList();

                int bestFitness = Fitness(population[0], clientCounts);
                double avgFitness = population.Average(ind => Fitness(ind, clientCounts));

                // Tworzenie nowej populacji
                List<List<int>> newPopulation = new List<List<int>>();
                while (newPopulation.Count < populationSize)
                {
                    var parent1 = Select(population, clientCounts);
                    var parent2 = Select(population, clientCounts);

                    var child = Crossover(parent1, parent2);

                    // Mutacja
                    for (int i = 0; i < child.Count; i++)
                    {
                        if (rnd.NextDouble() < mutationRateCurrent)
                        {
                            child[i] = rnd.Next(1, maxEmployees + 1);
                            mutationCount++;
                        }
                    }

                    newPopulation.Add(child);
                }

                population = newPopulation;

                // Zapis do CSV
                writer.WriteLine($"{gen + 1};{bestFitness};{Math.Round(avgFitness, 2).ToString().Replace('.', ',')};{mutationRateCurrent.ToString().Replace('.', ',')};{mutationCount}");

                // Zapis preferencji, wymagań i harmonogramu
                string shiftHeaderString = GetShiftHeaders(";");

                // Preferences
                writer.WriteLine();
                writer.WriteLine("Preferences");
                writer.WriteLine($" ;{shiftHeaderString}");
                for (int i = 0; i < numEmployees; i++)
                {
                    writer.Write($"P{i + 1};");
                    for (int j = 0; j < numTimeSlots; j++)
                    {
                        if (j >= employeePreferences.GetLength(1)) break;
                        writer.Write(employeePreferences[i, j]);
                        if (j < numTimeSlots - 1) writer.Write(';');
                    }
                    writer.WriteLine();
                }

                // Requirements
                writer.WriteLine();
                writer.WriteLine("Requirements");
                writer.WriteLine($" ;{shiftHeaderString}");
                writer.Write("LP;");
                for (int j = 0; j < numTimeSlots; j++)
                {
                    if (j >= requiredWorkersPerShiftDisplay.Length) break;
                    writer.Write(requiredWorkersPerShiftDisplay[j]);
                    if (j < numTimeSlots - 1) writer.Write(';');
                }
                writer.WriteLine();

                // Schedule (z najlepszym osobnikiem)
                writer.WriteLine();
                writer.WriteLine("Schedule");
                writer.WriteLine($" ;{shiftHeaderString}");
                List<int> bestSchedule = population[0];
                for (int i = 0; i < numEmployees; i++)
                {
                    writer.Write($"P{i + 1};");
                    for (int j = 0; j < numTimeSlots; j++)
                    {
                        int index = i * numTimeSlots + j;
                        if (index >= bestSchedule.Count) break;
                        writer.Write(bestSchedule[index]);
                        if (j < numTimeSlots - 1) writer.Write(';');
                    }
                    writer.WriteLine();
                }
            }

            return population[0];
        }

        private List<int> RandomIndividual(int size) => Enumerable.Range(0, size).Select(_ => random.Next(1, maxEmployees + 1)).ToList();

        private int Fitness(List<int> individual, List<int> clientCounts)
        {
            int totalError = 0;
            for (int i = 0; i < individual.Count; i++)
            {
                int ideal = Math.Max(1, clientCounts[i] / 20);
                totalError += Math.Abs(individual[i] - ideal);
            }
            return totalError;
        }

        private List<int> Select(List<List<int>> population, List<int> clientCounts)
        {
            var tournament = population.OrderBy(x => random.Next()).Take(5).ToList();
            return tournament.OrderBy(x => Fitness(x, clientCounts)).First();
        }

        private List<int> Crossover(List<int> parent1, List<int> parent2)
        {
            int split = random.Next(parent1.Count);
            return parent1.Take(split).Concat(parent2.Skip(split)).ToList();
        }

        static string GetLogFileName()
        {
            string logDirectory = "../../../logi";
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            int logNumber = 1;
            string logFileName;
            do
            {
                logFileName = Path.Combine(logDirectory, $"genetic_log{logNumber}.csv");
                logNumber++;
            } while (File.Exists(logFileName));
            return logFileName;
        }

        static void Main(string[] args)
        {
            Program scheduler = new Program();
            Random randomClient = new Random();

            int numShifts = numShiftsPerDay * numActualDays;
            List<int> clientCounts = new List<int>();
            for (int i = 0; i < numShifts; i++)
                clientCounts.Add(randomClient.Next(10, 100));

            string logFileName = GetLogFileName();
            List<int> bestSchedule = scheduler.Optimize(clientCounts, logFileName);

            Console.WriteLine("Najlepszy harmonogram:");
            for (int i = 0; i < bestSchedule.Count; i++)
                Console.WriteLine($"Zmiana {i + 1}: {bestSchedule[i]} pracowników (klienci: {clientCounts[i]})");

            Console.WriteLine($"Log zapisany w: {logFileName}");
            Console.ReadKey();
        }
    }
}
