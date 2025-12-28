using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;  // potrzebne do Parallel.For
using System.Collections.Generic;

namespace GeneticScheduling
{
    class Program
    {
        static Random rand = new Random();

        const int populationSize = 500;
        const int generations = 100;
        public static int totallyNewChildren = 5;
        public static double mutationRate = 0.1;
        public static bool variableMutationRate = true;
        public static int mutationCount = 0;
        const int EliteCount = 5;

        static int numEmployees;
        static int numActualDays;
        const int numShiftsPerDay = 3;
        static int numTimeSlots;

        static int[] requiredWorkersPerShiftDisplay;
        static int[] requiredWorkersPerShiftNumeric;
        static int[,] employeePreferences;

        public static class FitnessConstants
        {
            public const int workersPerDayPenalty = 50;
            public const int EmployeePreferenceMultiplier = 10;
        }

        static Program()
        {
            string fileName = "grafik_30d_3s_11emp";
            string file = @$"..\..\..\..\msi1\Dane_csv\{fileName}.csv";

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
                {
                    if (int.TryParse(values[j].Trim(), out int val))
                        reqValues.Add(val);
                }
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

            int numEmployees = lines.Length - 3;
            int numShifts = prefCols.Count;
            int[,] preferences = new int[numEmployees, numShifts];

            for (int row = 3; row < lines.Length; row++)
            {
                string[] values = lines[row].Split(',');
                int empIndex = row - 3;

                for (int c = 0; c < numShifts; c++)
                {
                    int colIndex = prefCols[c];
                    if (colIndex < values.Length && int.TryParse(values[colIndex].Trim(), out int val))
                        preferences[empIndex, c] = val;
                }
            }
            return preferences;
        }

        static void Main(string[] args)
        {
            int[][,] population = new int[populationSize][,];
            for (int i = 0; i < populationSize; i++)
                population[i] = GenerateSchedule();

            int[,] initpop = (int[,])population[0].Clone();

            string logFileName = GetLogFileName();
            string resultFileName = GetResultFileName();

            using (StreamWriter writer = new StreamWriter(logFileName, false, Encoding.UTF8))
            {
                writer.WriteLine($"Population size:;{populationSize}");
                writer.WriteLine($"Generations:;{generations}");
                writer.WriteLine($"Mutation rate (initial):;{mutationRate}");
                writer.WriteLine($"Elite count:;{EliteCount}");
                writer.WriteLine($"Totally new children:;{totallyNewChildren}");
                writer.WriteLine($"Variable mutation rate:;{variableMutationRate}");
                writer.WriteLine();
                writer.WriteLine("Generation;BestFitness;AverageFitness;MutationRate;MutationCount");

                double previousAverageFitness = 0.0;

                for (int gen = 0; gen < generations; gen++)
                {
                    // Oblicz fitness równolegle
                    double[] fitness = new double[populationSize];
                    Parallel.For(0, populationSize, i =>
                    {
                        fitness[i] = CalculateFitness(population[i]);
                    });

                    // Sortuj populację według fitness malejąco (najlepszy pierwszy)
                    var sortedPairs = population.Zip(fitness, (sched, fit) => new { sched, fit })
                                               .OrderByDescending(x => x.fit)
                                               .ToArray();

                    population = sortedPairs.Select(x => x.sched).ToArray();
                    double[] sortedFitness = sortedPairs.Select(x => x.fit).ToArray();

                    double bestFitness = sortedFitness[0];
                    double averageFitness = sortedFitness.Average();

                    // Nowa populacja
                    int[][,] newPopulation = new int[populationSize][,];

                    // Elita
                    for (int i = 0; i < EliteCount; i++)
                        newPopulation[i] = population[i];  // można dodać .Clone() jeśli chcesz

                    // Krzyżowanie i mutacja – równolegle
                    Parallel.For(EliteCount, populationSize - totallyNewChildren, i =>
                    {
                        int[,] parent1 = population[rand.Next(populationSize / 2)];
                        int[,] parent2 = population[rand.Next(populationSize / 2)];
                        int[,] child = Crossover(parent1, parent2);

                        if (rand.NextDouble() < mutationRate)
                        {
                            child = Mutate(child);
                            System.Threading.Interlocked.Increment(ref mutationCount);
                        }

                        newPopulation[i] = child;
                    });

                    // Całkowicie nowe osobniki
                    for (int i = populationSize - totallyNewChildren; i < populationSize; i++)
                        newPopulation[i] = GenerateSchedule();

                    // Adaptacyjna mutacja
                    if (gen > 0 && variableMutationRate)
                    {
                        if (averageFitness > previousAverageFitness)
                            mutationRate = Math.Min(1.0, mutationRate + 0.05);
                        else if (averageFitness < previousAverageFitness)
                            mutationRate = Math.Max(0.01, mutationRate - 0.05);
                    }

                    previousAverageFitness = averageFitness;
                    mutationRate = Math.Round(mutationRate, 3);

                    writer.WriteLine($"{gen + 1};{bestFitness};{averageFitness:F2};{mutationRate};{mutationCount}");
                    Console.WriteLine($"Gen {gen + 1} | Best: {bestFitness} | Avg: {averageFitness:F2} | MutRate: {mutationRate} | MutCount: {mutationCount}");

                    mutationCount = 0;
                    population = newPopulation;
                }

                int[,] finalSchedule = population[0];

                // Zapis logu (preferencje, wymagania, grafik, fitness) – bez zmian
                string shiftHeaderString = GetShiftHeaders(";");
                writer.WriteLine();
                writer.WriteLine("Preferences");
                writer.WriteLine($" ;{shiftHeaderString}");
                for (int i = 0; i < numEmployees; i++)
                {
                    writer.Write($"P{i + 1};");
                    for (int j = 0; j < numTimeSlots; j++)
                    {
                        writer.Write(employeePreferences[i, j]);
                        if (j < numTimeSlots - 1) writer.Write(";");
                    }
                    writer.WriteLine();
                }

                writer.WriteLine();
                writer.WriteLine("Requirements");
                writer.WriteLine($" ;{shiftHeaderString}");
                writer.Write("LP;");
                for (int j = 0; j < numTimeSlots; j++)
                {
                    writer.Write(requiredWorkersPerShiftDisplay[j]);
                    if (j < numTimeSlots - 1) writer.Write(";");
                }
                writer.WriteLine();

                writer.WriteLine();
                writer.WriteLine("Schedule");
                writer.WriteLine($" ;{shiftHeaderString}");
                for (int i = 0; i < numEmployees; i++)
                {
                    writer.Write($"P{i + 1};");
                    for (int j = 0; j < numTimeSlots; j++)
                    {
                        writer.Write(finalSchedule[i, j]);
                        if (j < numTimeSlots - 1) writer.Write(";");
                    }
                    writer.WriteLine();
                }

                writer.WriteLine();
                writer.WriteLine("FitnessForEachWorker");
                for (int i = 0; i < numEmployees; i++)
                {
                    int fit = CalculateEmployeeFitnessNorm(finalSchedule, i);
                    writer.WriteLine(fit);
                    Console.WriteLine($"P{i + 1}: {fit}");
                }
            }

            SaveResultsToCsv(resultFileName, population[0]);

            Console.WriteLine("\nInitial Schedule:");
            PrintScheduleToConsole(initpop);
            Console.WriteLine("\nFinal Schedule:");
            PrintScheduleToConsole(population[0]);
        }

        static void SaveResultsToCsv(string fileName, int[,] finalSchedule)
        {
            using (StreamWriter writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                writer.WriteLine("id,day,shift,worker_id,preference,requirements,assigned");

                int numDays = 30;
                int shiftsPerDay = 3;

                int id = 0;

                for (int day = 1; day <= numDays; day++)
                {
                    for (int shift = 1; shift <= shiftsPerDay; shift++)
                    {
                        int slot = (day - 1) * shiftsPerDay + (shift - 1);

                        for (int worker = 0; worker < numEmployees; worker++)
                        {
                            int preference = employeePreferences[worker, slot];
                            int requirement = requiredWorkersPerShiftDisplay[slot];
                            int assigned = finalSchedule[worker, slot];

                            writer.WriteLine(
                                $"{id},{day},{shift},{worker + 1},{preference},{requirement},{assigned}"
                            );

                            id++;
                        }
                    }
                }
            }
        }




        static string GetShiftHeaders(string delimiter)
        {
            StringBuilder sb = new StringBuilder();
            for (int d = 1; d <= numActualDays; d++)
                for (int s = 1; s <= numShiftsPerDay; s++)
                {
                    sb.Append($"D{d}S{s}");
                    if (!(d == numActualDays && s == numShiftsPerDay))
                        sb.Append(delimiter);
                }
            return sb.ToString();
        }

        static string GetLogFileName()
        {
            string dir = "../../../LOGI_ALGORYTMU";
            Directory.CreateDirectory(dir);
            int num = 1;
            string name;
            do { name = Path.Combine(dir, $"genetic_Logs{num}.csv"); num++; }
            while (File.Exists(name));
            return name;
        }

        static string GetResultFileName()
        {
            string dir = "../../../wyniki";
            Directory.CreateDirectory(dir);
            int num = 1;
            string name;
            do { name = Path.Combine(dir, $"genetic_Result{num}.csv"); num++; }
            while (File.Exists(name));
            return name;
        }

        static int[,] GenerateSchedule()
        {
            int[,] s = new int[numEmployees, numTimeSlots];
            for (int i = 0; i < numEmployees; i++)
                for (int j = 0; j < numTimeSlots; j++)
                    s[i, j] = rand.Next(2);
            return s;
        }

        static int CalculateFitness(int[,] schedule) => CalculatePreferenceBonus(schedule) - CalculateWorkersPenalty(schedule);

        static int CalculateWorkersPenalty(int[,] schedule)
        {
            int penalty = 0;
            for (int j = 0; j < numTimeSlots; j++)
            {
                int actual = 0;
                for (int i = 0; i < numEmployees; i++) actual += schedule[i, j];
                int req = requiredWorkersPerShiftNumeric[j];
                penalty += FitnessConstants.workersPerDayPenalty * Math.Abs(req - actual);
            }
            return penalty;
        }

        static int CalculatePreferenceBonus(int[,] schedule)
        {
            int bonus = 0;
            for (int i = 0; i < numEmployees; i++)
                for (int j = 0; j < numTimeSlots; j++)
                    if (schedule[i, j] == employeePreferences[i, j])
                        bonus += FitnessConstants.EmployeePreferenceMultiplier;
            return bonus;
        }

        static int CalculateEmployeeFitnessNorm(int[,] schedule, int emp)
        {
            int bonus = 0;
            for (int j = 0; j < numTimeSlots; j++)
                if (schedule[emp, j] == employeePreferences[emp, j])
                    bonus += FitnessConstants.EmployeePreferenceMultiplier;
            return bonus;
        }

        static int[,] Crossover(int[,] p1, int[,] p2)
        {
            int[,] child = new int[numEmployees, numTimeSlots];
            if (rand.Next(2) == 0)
            {
                int point = rand.Next(1, numTimeSlots);
                for (int i = 0; i < numEmployees; i++)
                    for (int j = 0; j < numTimeSlots; j++)
                        child[i, j] = j < point ? p1[i, j] : p2[i, j];
            }
            else
            {
                for (int i = 0; i < numEmployees; i++)
                    for (int j = 0; j < numTimeSlots; j++)
                        child[i, j] = rand.NextDouble() < 0.5 ? p1[i, j] : p2[i, j];
            }
            return child;
        }

        static int[,] Mutate(int[,] schedule)
        {
            int[,] mutated = (int[,])schedule.Clone();
            if (rand.NextDouble() < 0.5)
            {
                int emp = rand.Next(numEmployees);
                int slot = rand.Next(numTimeSlots);
                mutated[emp, slot] = 1 - mutated[emp, slot];
            }
            else
            {
                int emp = rand.Next(numEmployees);
                int s1 = rand.Next(numTimeSlots);
                int s2 = rand.Next(numTimeSlots);
                if (s1 != s2)
                {
                    int temp = mutated[emp, s1];
                    mutated[emp, s1] = mutated[emp, s2];
                    mutated[emp, s2] = temp;
                }
            }
            return mutated;
        }

        static void PrintScheduleToConsole(int[,] schedule)
        {
            Console.Write("  ");
            for (int d = 1; d <= numActualDays; d++)
                for (int s = 1; s <= numShiftsPerDay; s++)
                    Console.Write($"D{d}S{s}\t");
            Console.WriteLine();

            for (int i = 0; i < numEmployees; i++)
            {
                Console.Write($"P{i + 1}\t");
                for (int j = 0; j < numTimeSlots; j++)
                    Console.Write(schedule[i, j] + "\t");
                Console.WriteLine();
            }
        }
    }
}