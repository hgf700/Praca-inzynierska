using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using msi_GA.Task_handling;
using System.IO;
using System.Text.Json;
using msi_GA.GA;
using System.Collections;
using System.Security;
using System.Numerics;
using System.ComponentModel.Design;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using Accord.Genetic;
using Tensorflow.Operations.Activation;
using NeuralNetwork1;

namespace msi_GA.GA
{

    public class Generation : Gen
    {
        public static List<Worker> Workers = new List<Worker> { };
        public int workers = 0;
        public List<Gen> _generations = new List<Gen>();
        private object randLock = new object();
        public List<Generation> inferiorPopulations = new List<Generation>();


        // generation options-------------------------------------------------------------
        public int GenerationLength = 24;
        public int ParentsCount = 2;
        public int ReprodcutionRate = 0;
        public double ReproductionRateDouble = 0.0;

        public int BestParentsKept = 2;
        public double MutationRate = 0.1;
        public int CrossoverOption = 1;
        public int HillSteps = 1000; // liczba kroków w hill climbing
        public bool Preferences = false;
        public bool EtylistRateEnabled = true;

        public bool ChangingMutationEbaled = true;
        public bool TargetedMutationEnabled = true;
        public bool IgnoreMutationRate = false;
        public bool useNeuralMutation = false; // Use neural network for mutation guidance
        public int MaxTry = 10;

        public bool BetterHoursMutationEnabled = true;

        public int EtylistChangeInterval = 20;
        public int MutationChangeInterval = 20;
        // statistics --------------------------------------------------------------------

        public double Variation = 0.0;
        public double Mean = 0.0;
        public double StandardDeviation = 0.0;
        public double PreviousBestFitness = 0.0;
        public double MaxFitness = 0.0;
        public int StagnationCounter = 0;
        public int MaxStagnation = 50;
        public int HoursScoreMean = 0;


        public int FasterMutationMultiplyPoint = 2;

        //Save Managment ---------------------------------------------------------------
        public int number = 0;
        public int seriesnumber = 0;
        public string filepathWorkers = "";
        private readonly SaveManagment _saveManagment;

        // Neural Network --------------------------------------------------------------
        private NeuralNetwork _mutationNetwork;
        public double hourWeight = 0.35; // Default weight for hours

        public int OrginalReproductionRate = 0;
        public int RR_before = 0;
        public double MR_before = 0;

        //------------------------------------------------------------------------------------------------------- 
        private ThreadLocal<Random> threadLocalRandom = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));
        Random random = new Random();




        public Generation(bool IsConstantAware, string filePath, SaveManagment saveManagment)
      : base()
        {
            _saveManagment = saveManagment;
            filepathWorkers = filePath;
            LoadWorkersFromJson(filepathWorkers);
            workers = Workers.Count;




            // Initialize neural network (layers: input, hidden, output)



            _mutationNetwork = new NeuralNetwork(new[] { 6, 4, 2 })  // 8 inputs, 8 hidden, 2 output
            {
                Iterations = 500,
                Alpha = 5.5,
                L2_Regularization = true,
                Lambda = 0.003,
                Rnd = new Random(42)  // Fixed seed for reproducibility
            };

            GenerateFirstGeneration(IsConstantAware);
        }
        public Generation()
        {


        }



        public void GenerateFirstGeneration(bool IsConstantAware = false)
        {
            number = _saveManagment.ReadManifest() + 1;


            for (int i = 0; i < GenerationLength; i++)
            {
                _generations.Add(new Gen(IsConstantAware, workers));
            }
            ReprodcutionRate = GenerationLength;
            OrginalReproductionRate = ReprodcutionRate;
            ReproductionRateDouble = (double)ReprodcutionRate / GenerationLength;



            SortGenerations();
            CalculateVarianceAndMean();

            seriesnumber++;

            _saveManagment.UpdateManifest(number);


            MaxFitness = _generations[0].Fitness;

        }





        public void GenerateNextGeneration()
        {
            List<Gen> newGenerations = new List<Gen>();

            int ChildAmount = 0;

            PreviousBestFitness = _generations[0].Fitness;




            for (int i = BestParentsKept; i < GenerationLength; i = i + ParentsCount)
            {
                var children = ChooseCrossover(CrossoverOption);

                foreach (var child in children)
                {
                    if (ChildAmount <= GenerationLength)
                    {
                        newGenerations.Add(child);
                        ChildAmount++;
                    }
                    else break;
                }



            }
            foreach (var child in newGenerations)
            {

                Gen original = child.Clone();

                if (useNeuralMutation)
                {
                    RR_before = ReprodcutionRate;
                    MR_before = MutationRate;
                    NeuralGuidedMutation(child);
                }
                else if (HoursScoreMean < 0.8 && BetterHoursMutationEnabled)
                {
                    MutateForBetterHours(child);
                }
                else if (MaxFitness > 8.0 && MaxFitness < 0.95 && !child.ValidHardConstraints)
                {
                    MutateHardConstraints(child);


                }
                else if (MaxFitness > 0.95 && !child.ValidHardConstraints)
                {
                    ForceHardContraints(child);


                }
                else
                    Mutate(child, MutationRate);

                Recalcutate(child);
            }

            CopyGeneration(newGenerations);
            SortGenerations();
            CalculateVarianceAndMean();
            MaxFitness = _generations[0].Fitness;
            Stagnation();
            AdaptiveParametersUpdate();


        }

        public void TryPreferences(Gen individual)
        {
            // Sprawdź, czy są pracownicy i zmiany
            if (individual.Schedule == null || individual.Schedule.Length == 0 || individual.Schedule[0].Length == 0)
                return;

            // Wybierz losowego pracownika
            int workerIndex = random.Next(0, individual.Schedule.Length);

            // Pobierz preferencje tego pracownika
            int[] preferences = Workers[workerIndex].Preferences;

            // Znajdź wszystkie zmiany, gdzie:
            // 1. Pracownik obecnie nie pracuje (0), ale wolałby pracować (1 w preferencjach)
            // 2. Albo pracuje (1), ale wolałby nie pracować (0 w preferencjach)
            var possibleChanges = new List<(int shift, int current, int preferred)>();

            for (int shift = 0; shift < individual.Schedule[workerIndex].Length; shift++)
            {
                int current = individual.Schedule[workerIndex][shift];
                int preferred = preferences[shift];

                if (current != preferred)
                {
                    possibleChanges.Add((shift, current, preferred));
                }
            }

            // Jeśli znaleziono możliwe zmiany
            if (possibleChanges.Count > 0)
            {
                // Wybierz losową zmianę do wprowadzenia
                var change = possibleChanges[random.Next(0, possibleChanges.Count)];

                // Wprowadź zmianę
                individual.Schedule[workerIndex][change.shift] = change.preferred;


            }
        }
        public void ClibGeneration()
        {

            foreach (var gen in _generations)
            {
                HillClimbing(gen, HillSteps);
                Recalcutate(gen);
            }

            SortGenerations();

            CalculateVarianceAndMean();
            MaxFitness = _generations[0].Fitness;
            Stagnation();
            AdaptiveParametersUpdate();

        }
        // ----------------------------crossovers
        public List<Gen> MaskCrossoverTwoParents()
        {
            int parent1 = random.Next(0, ReprodcutionRate);
            int parent2 = random.Next(0, ReprodcutionRate);
            Gen child1 = new Gen(workers);
            Gen child2 = new Gen(workers);
            List<Gen> children = new List<Gen>();

            Parallel.For(0, workers, i =>
            {
                for (int j = 0; j < ShiftsAmount; j++)
                {
                    bool mask = GenerateMaskOnTheFly1();

                    if (mask)
                    {
                        child1.Schedule[i][j] = _generations[parent1].Schedule[i][j];
                        child2.Schedule[i][j] = _generations[parent2].Schedule[i][j];
                    }
                    else
                    {
                        child1.Schedule[i][j] = _generations[parent2].Schedule[i][j];
                        child2.Schedule[i][j] = _generations[parent1].Schedule[i][j];
                    }
                }
            });



            child1.Fitness = CalculateFitness(child1.Schedule);
            child2.Fitness = CalculateFitness(child2.Schedule);

            children.Add(child1);
            children.Add(child2);
            return children;
        }


        public List<Gen> MaskCrossoverThreeParents()
        {
            int parent1 = random.Next(0, ReprodcutionRate);
            int parent2 = random.Next(0, ReprodcutionRate);
            int parent3 = random.Next(0, ReprodcutionRate);
            Gen child1 = new Gen(workers);
            Gen child2 = new Gen(workers);
            Gen child3 = new Gen(workers);
            List<Gen> children = new List<Gen>();


            Parallel.For(0, workers, i =>
            {
                for (int j = 0; j < ShiftsAmount; j++)
                {
                    int mask = GenerateMaskOnTheFly2();

                    if (mask == 0)
                    {
                        child1.Schedule[i][j] = _generations[parent1].Schedule[i][j];
                        child2.Schedule[i][j] = _generations[parent2].Schedule[i][j];
                        child1.Schedule[i][j] = _generations[parent3].Schedule[i][j];
                    }
                    else if (mask == 1)
                    {
                        child1.Schedule[i][j] = _generations[parent2].Schedule[i][j];
                        child2.Schedule[i][j] = _generations[parent3].Schedule[i][j];
                        child3.Schedule[i][j] = _generations[parent1].Schedule[i][j];
                    }
                    else if (mask == 2)
                    {
                        child1.Schedule[i][j] = _generations[parent3].Schedule[i][j];
                        child2.Schedule[i][j] = _generations[parent1].Schedule[i][j];
                        child3.Schedule[i][j] = _generations[parent2].Schedule[i][j];
                    }
                }
            });

            children.Add(child1);
            children.Add(child2);
            children.Add(child3);
            return children;
        }


        public List<Gen> MaskCrossoverRandomParentsAmount()
        {
            List<int> parents = new List<int>();


            for (int i = 0; i < ParentsCount; i++)
            {
                int parent = random.Next(0, ReprodcutionRate);
                parents.Add(parent);


            }





            List<Gen> children = new List<Gen>();
            for (int i = 0; i < ParentsCount; i++)
            {
                children.Add(new Gen(workers));
            }



            Parallel.For(0, workers, workerIndex =>
            {
                for (int shiftIndex = 0; shiftIndex < ShiftsAmount; shiftIndex++)
                {
                    int mask = GenerateMaskOnTheFly(ParentsCount);


                    for (int childIndex = 0; childIndex < ParentsCount; childIndex++)
                    {

                        int parentIndex = (childIndex + mask) % ParentsCount;
                        children[childIndex].Schedule[workerIndex][shiftIndex] =
                            _generations[parents[parentIndex]].Schedule[workerIndex][shiftIndex];
                    }
                }
            });

            return children;




        }





        private List<Gen> SelectParents(int tournamentSize)
        {
            return _generations
                .OrderBy(x => random.Next())
                .Take(tournamentSize)
                .OrderByDescending(x => x.Fitness)
                .Take(2)
                .ToList();
        }
        public List<Gen> PMXCrossover(Gen parent1, Gen parent2)
        {
            var child1 = new Gen(workers);
            var child2 = new Gen(workers);


            int point1 = random.Next(0, ShiftsAmount - 1);
            int point2 = random.Next(point1 + 1, ShiftsAmount);


            for (int i = point1; i <= point2; i++)
            {
                for (int w = 0; w < workers; w++)
                {
                    child1.Schedule[w][i] = parent2.Schedule[w][i];
                    child2.Schedule[w][i] = parent1.Schedule[w][i];
                }
            }


            return new List<Gen> { child1, child2 };
        }








        public static double StdDev(IEnumerable<double> values)
        {
            double avg = values.Average();
            double sumSquares = values.Sum(v => Math.Pow(v - avg, 2));
            return Math.Sqrt(sumSquares / values.Count());
        }


        //------------------------ hill climbing


        public Gen HillStep(Gen current)
        {
            Gen neighbor = current.Clone();

            if (Preferences)
            {
                TryPreferences(neighbor);
            }
            else
                ForceHardContraints(neighbor);

            neighbor.Fitness = CalculateFitness(neighbor.Schedule);

            Console.WriteLine($"neiighbour: {neighbor.Fitness} , current:{current.Fitness} ");
            return neighbor.Fitness > current.Fitness ? neighbor : current;
        }

        public void HillClimbing(Gen initial, int maxSteps = 100)
        {
            Gen current = initial.Clone();
            current.Fitness = CalculateFitness(current.Schedule);

            for (int i = 0; i < maxSteps; i++)
            {
                Gen next = HillStep(current);



                current = next;
            }

            initial = current; // Zapisz najlepszy znaleziony gen
        }






        //------------------------------------------------




        public void ForceHardContraints(Gen individual)
        {
            int interation = 0;
            while (!individual.ValidHardConstraints && MaxTry > interation)
            {


                (int w, int day) = FindBadShift(individual.Schedule, 0);
                if (day >= 0 && IgnoreMutationRate == true ? true : random.NextDouble() < MutationRate)
                {
                    MutateDayShifts(individual, day, w);
                }



                (int breakDay, int worker) = FindNeededBreak(individual.Schedule, 0);
                if (breakDay >= 0 && worker >= 0)
                {
                    FixBreakBetweenShifts(individual, breakDay, worker);
                }
                Recalcutate(individual);
                interation++;
            }

        }
        public void MutateHardConstraints(Gen individual)
        {
            // 1. Napraw dzień z problemem ilości zmian
            (int w, int day) = FindBadShift(individual.Schedule, MaxFitness > 0.96 ? 0 : random.Next(0, individual.Schedule[0].Length));
            if (day >= 0 && w >= 0 && (IgnoreMutationRate || random.NextDouble() < MutationRate))
            {
                MutateDayShifts(individual, day, w);
            }

            // 2. Znajdź i popraw przerwę między zmianami
            (int breakDay, int worker) = FindNeededBreak(individual.Schedule, MaxFitness > 0.96 ? 0 : random.Next(0, individual.Schedule[0].Length));
            if (breakDay >= 0 && worker >= 0 && (IgnoreMutationRate || random.NextDouble() < MutationRate))
            {
                FixBreakBetweenShifts(individual, breakDay, worker);
            }

            // 3. Popraw nadmiar godzin pracownika
            int worstWorker = FindTheWorstHours(individual.Schedule, MaxFitness > 0.96 ? 0 : random.Next(0, individual.Schedule[0].Length));
            if (worstWorker >= 0 && (IgnoreMutationRate || random.NextDouble() < MutationRate))
            {
                ReduceWorkerHours(individual, worstWorker);
            }

            // 4. Popraw słabo obsadzoną zmianę
            int worstShiftDay = NoDispersionShift(individual.Schedule, MaxFitness > 0.96 ? 0 : random.Next(0, individual.Schedule[1].Length));
            if (worstShiftDay >= 0 && (IgnoreMutationRate || random.NextDouble() < MutationRate))
            {
                ImproveShiftDispersion(individual, worstShiftDay);
            }
        }


        private void MutateDayShifts(Gen individual, int day, int worker)
        {
            int max = day + 3;
            for (int i = day; i <= max; i++)
            {
                if (individual.Schedule[worker][i] == 1)
                {
                    individual.Schedule[worker][i] = 0;
                    break;
                }
            }

        }

        private void FixBreakBetweenShifts(Gen individual, int day, int worker)
        {
            // Usuń lub przesun zmianę, która łamie przerwę
            individual.Schedule[worker][day] = 0; // usuń zmianę danego dnia, żeby był odpoczynek



        }

        private void ReduceWorkerHours(Gen individual, int worker)
        {
            // Usuń jedną zmianę pracownika, która jest najmniej krytyczna
            int dayToRemove = FindLeastCriticalShift(individual.Schedule, worker);
            if (dayToRemove >= 0)
                individual.Schedule[worker][dayToRemove] = 0;
        }

        private void ImproveShiftDispersion(Gen individual, int day)
        {
            // Znajdź pracownika, który mógłby uzupełnić brakujący typ zmiany i przydziel go
            int workerToAdd = FindAvailableWorkerForShift(individual.Schedule, day);
            if (workerToAdd >= 0)
                individual.Schedule[workerToAdd][day] = 1; // dodaj zmianę
        }



        public void MutateForBetterHours(Gen individual)
        {
            const int minHours = 24;
            const int idealHours = 40;
            const int maxHours = 48;

            for (int w = 0; w < individual.Schedule.Length; w++)
            {
                int currentHours = CalculateWorkerHours(individual.Schedule, w);

                // Only mutate if hours are outside acceptable range
                if (currentHours < minHours || currentHours > maxHours)
                {
                    int shiftsToChange = Math.Abs(idealHours - currentHours) / 8;
                    bool needsMoreHours = currentHours < minHours;

                    // Get all possible shifts we could modify
                    var candidateShifts = Enumerable.Range(0, individual.Schedule[w].Length)
                        .Where(s => needsMoreHours
                            ? individual.Schedule[w][s] == 0  // If need more hours, look for empty shifts
                            : individual.Schedule[w][s] == 1) // If need fewer hours, look for filled shifts
                        .ToList();

                    // Randomize the order of candidate shifts
                    candidateShifts = candidateShifts.OrderBy(x => random.Next()).ToList();

                    // Apply changes to the necessary number of shifts
                    for (int i = 0; i < Math.Min(shiftsToChange, candidateShifts.Count); i++)
                    {
                        if (MutationRate > random.NextDouble())
                        {
                            // Set the shift to 1 if we need more hours, otherwise set it to 0
                            int shiftIndex = candidateShifts[i];
                            individual.Schedule[w][shiftIndex] = needsMoreHours ? 1 : 0;
                        }
                    }
                }
            }
        }

        private int CalculateWorkerHours(int[][] schedule, int workerIndex)
        {
            int hours = 0;
            for (int s = 0; s < schedule[workerIndex].Length; s++)
            {
                hours += schedule[workerIndex][s] * 8;
            }
            return hours;
        }



        public void WyswietlOpcje()
        {
            Console.WriteLine("------------------------------------------------------------------------");
            Console.WriteLine($"Pula rodzicow:: {ReprodcutionRate}");
            Console.WriteLine($"MutationRate : {MutationRate}");
            Console.WriteLine($"CrossoverOption : {CrossoverOption}");
            Console.WriteLine($"BestParentsKept : {BestParentsKept}");
            Console.WriteLine($"CrossoverOption : {CrossoverOption}");
            Console.WriteLine($"PularodzicowChangeInterval : {EtylistChangeInterval}");
            Console.WriteLine($"MutationChangeInterval : {MutationChangeInterval}");
            Console.WriteLine($"FasterMutationMultiplyPoint : {FasterMutationMultiplyPoint}");

            Console.WriteLine($"PularodzicowEnabled : {EtylistRateEnabled}");
            Console.WriteLine($"MutationRateEnabled : {ChangingMutationEbaled}");
            Console.WriteLine($"TargetetMutationEnabled: {TargetedMutationEnabled}");
            Console.WriteLine($"BetterHoursMutationEnabled: {BetterHoursMutationEnabled}");
            Console.WriteLine("------------------------------------------------------------------------");

        }

        public void LoadWorkersFromJson(string filePath)
        {


            if (!File.Exists(filePath))
                throw new FileNotFoundException("Plik z pracownikami nie istnieje");

            string json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            Workers = JsonSerializer.Deserialize<List<Worker>>(json, options);
            workers = Workers.Count;
        }

        public void CalculateVarianceAndMean(Generation generation)
        {
            double sum = 0;

            foreach (var gen in generation._generations)
            {
                sum += gen.Fitness;
            }

            Mean = sum / GenerationLength;

            foreach (var gen in generation._generations)
            {
                Variation += Math.Pow(gen.Fitness - Mean, 2);
            }

            StandardDeviation = Math.Sqrt(Variation / GenerationLength);

        }


        public void CopyGeneration(List<Gen> NewGeneration)
        {

            int j = 0;

            for (int i = BestParentsKept; i < GenerationLength; i++)
            {

                _generations[i] = NewGeneration[j];

                _generations[i].Fitness = CalculateFitness(_generations[i]._schedule);
                _generations[i].HoursScore = HoursMeet(_generations[i]._schedule);
                _generations[i].ShiftBreakScore = ShiftBreak(_generations[i]._schedule);
                _generations[i].WorkerDispersionScore = WorkerDispersionGeneral(_generations[i]._schedule);
                _generations[i].MaxShiftScore = MaxShifts(_generations[i]._schedule);
                _generations[i].TypeOfWorkerPerShift = EachWorkerTypePerShifts(_generations[i]._schedule);
                _generations[i].PreferencesScore = Preferences(_generations[i]._schedule);

                if (HoursScore >= 0.8 && ShiftBreakScore >= 0.9 && MaxShiftScore >= 0.9)
                {
                    _generations[i].ValidHardConstraints = true;
                }
                else _generations[i].ValidHardConstraints = false;

                j++;
            }
        }


        public void Recalcutate(Gen gen)
        {
            gen.Fitness = CalculateFitness(gen.Schedule);
            gen.HoursScore = HoursMeet(gen.Schedule);
            gen.ShiftBreakScore = ShiftBreak(gen.Schedule);
            gen.WorkerDispersionScore = WorkerDispersionGeneral(gen.Schedule);
            gen.MaxShiftScore = MaxShifts(gen.Schedule);
            gen.TypeOfWorkerPerShift = EachWorkerTypePerShifts(gen.Schedule);
            gen.PreferencesScore = Preferences(gen.Schedule);




            const double epsilon = 0.0001;
            bool hoursValid = gen.HoursScore >= (0.8 - epsilon);
            bool shiftBreakValid = gen.ShiftBreakScore >= (1.0 - epsilon);
            bool maxShiftValid = gen.MaxShiftScore >= (1.0 - epsilon);

            gen.ValidHardConstraints = hoursValid && shiftBreakValid && maxShiftValid;


        }


        public List<Gen> ChooseCrossover(int option)
        {
            if (option == 1)
            {
                ParentsCount = 2;
                return MaskCrossoverTwoParents();

            }
            else if (option == 2)
            {
                ParentsCount = 3;
                return MaskCrossoverThreeParents();
            }
            else if (option == 3)
            {
                ParentsCount = random.Next(2, ReprodcutionRate);
                return MaskCrossoverRandomParentsAmount();
            }
            else if (option == 4)
            {
                int parent1 = random.Next(0, ReprodcutionRate);
                int parent2 = random.Next(0, ReprodcutionRate);
                return PMXCrossover(_generations[parent1], _generations[parent2]);
            }
            else return null;



        }

        public void SortGenerations()
        {
            _generations = _generations.OrderByDescending(x => x.Fitness).ToList();

            CalculateVarianceAndMean(this);
        }

        public void PrintGenerations()
        {


            foreach (var gen in _generations)
            {
                for (int i = 0; i < gen.Schedule.Length; i++)
                {
                    if (i < 10) { Console.Write($"Worker  {i}: "); }
                    else
                        Console.Write($"Worker {i}: ");

                    for (int j = 0; j < gen.Schedule[i].Length; j++)
                    {
                        Console.Write($"{gen.Schedule[i][j]} ");
                        if ((j + 1) % 3 == 0) Console.Write($"|| ");
                    }
                    Console.WriteLine();
                }
                Console.WriteLine($"Fitness: {gen.Fitness} , HoursScore : {gen.HoursScore} , ShiftsBreakScore : {gen.ShiftBreakScore}, WorkerDispersion : {gen.WorkerDispersionScore}, Shift Score : {gen._maxShiftsScore}, eachworkerTypeperShift Score : {gen.TypeOfWorkerPerShift} , PreferencesScore :{gen.PreferencesScore}");
                Console.WriteLine($"HC: {gen.ValidHardConstraints}");

                int workerNumber = 1;
                Console.WriteLine("--------Preferences-----------------------------------------------");
                Console.ForegroundColor = ConsoleColor.Blue;

                foreach (var worker in Workers)
                {
                    if (workerNumber < 10) { Console.Write($"Worker  {workerNumber}: "); }
                    else
                        Console.Write($"Worker {workerNumber}: ");


                    for (int i = 0; i < worker.Preferences.Length; i++)
                    {
                        Console.Write($"{worker.Preferences[i]} ");


                        if ((i + 1) % 3 == 0)
                        {
                            Console.Write("|| ");
                        }
                    }
                    Console.WriteLine();
                    workerNumber++;
                }

                Console.ResetColor();


                Console.WriteLine("------------------------------------------------------------------------");
                Console.WriteLine($"Etylist Rate: {ReprodcutionRate}");
                Console.WriteLine($"Mutation Rate: {MutationRate}");
                Console.WriteLine($"Parents : {ParentsCount}");

                Console.WriteLine($"Mean: {Mean}");
                Console.WriteLine($"Standard Deviation: {StandardDeviation}");
                Console.WriteLine($"Variation: {Variation}");
                Console.WriteLine($"Previous Best Fitness: {PreviousBestFitness}");
                Console.WriteLine($"Max Fitness: {MaxFitness}");

                Console.WriteLine("------------------------------------------------------------------------");


            }

            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var gen in _generations)
            {
                Console.WriteLine($"Fitness: {gen.Fitness}, {gen.ValidHardConstraints}");


            }
            Console.ResetColor();
        }


        //------------------ NN----------------------------------
        private double[] CalculateGlobalFeatures(int[][] schedule)
        {
            return new double[]
            {

                  GetNormalizedFitness(schedule),  // Ogólna jakość harmonogramu
        ShiftBreak(schedule),            // Naruszenia zasad
        EachWorkerTypePerShifts(schedule), // Poprawność przypisań
        _generations.Average(ind => ind.Fitness), // Średnia fitness
        RR_before,  // Poprzedni ER (do śledzenia trendów)
        MR_before   // Poprzedni MR (do śledzenia trendów)
           
        };
        }



        public void TrainMutationNetwork(List<MutationRecord> successfulMutations)
        {

            var inputs = new List<double[]>();
            var outputs = new List<double[]>(); // <--- teraz 2-wymiarowe wyjście

            // Cache global features dla unikalnych osobników
            var globalCache = successfulMutations
                .Select(m => m.Original)
                .Distinct()
                .ToDictionary(
                    original => original,
                    original => CalculateGlobalFeatures(original.Schedule)
                );

            foreach (var mutation in successfulMutations)
            {
                var original = mutation.Original;

                // Pobierz globalne cechy
                if (!globalCache.ContainsKey(original)) continue;

                double[] input = globalCache[original];

                // Skopiuj dane do wejścia
                double[] inputCopy = new double[input.Length];
                Array.Copy(input, inputCopy, input.Length);
                inputs.Add(inputCopy);

                // Wyjścia:
                // - mutationRate = 1 jeśli mutacja poprawiła fitness, 0 w przeciwnym razie
                // - elitistRate = znormalizowany fitness po mutacji
                double mutationSuccess = mutation.Mutated.Fitness > original.Fitness ? 1.0 : 0.0;
                double elitistScore = Math.Clamp(mutation.Mutated.Fitness, 0, 1); // lub Normalize()

                outputs.Add(new double[] { mutationSuccess, elitistScore });
            }

            // Trenuj sieć
            _mutationNetwork.Train(inputs.ToArray(), outputs.ToArray());
        }


        private double CalculateFairnessScore(int[][] schedule)
        {
            var hours = Enumerable.Range(0, schedule.Length)
                .Select(w => CalculateWorkerHours(schedule, w))
                .ToArray();

            double avg = hours.Average();
            double stdDev = Math.Sqrt(hours.Select(h => Math.Pow(h - avg, 2)).Average());

            // Score from 0 (unfair) to 1 (perfectly fair)
            return Math.Exp(-stdDev / 20);
        }

        private double Sigmoid(double x)
        {
            return 1.0 / (1.0 + Math.Exp(-x));
        }

        private double Normalize(double value, double min, double max)
        {
            return Math.Clamp((value - min) / (max - min), 0, 1);
        }
        public void NeuralGuidedMutation(Gen individual)
        {

            // Pre-compute global features once per schedule
            double[] globalFeatures = CalculateGlobalFeatures(individual.Schedule);



            // Prepare input features (4 global + 4 worker-specific)
            double[] inputs = new double[6];

            // Global features (position 0-4)
            Array.Copy(globalFeatures, 0, inputs, 0, 5);

            // Worker-specific features (position 5-6)





            MR_before = MutationRate;
            double[] output = _mutationNetwork.Predict(globalFeatures);
            MutationRate = Sigmoid(output[0]);

            double value = Sigmoid(output[1]);
            Console.WriteLine(value);
            RR_before = ReprodcutionRate;
            ReprodcutionRate = (int)Math.Floor(OrginalReproductionRate * value);
            Console.WriteLine(ReprodcutionRate);
            if (ReprodcutionRate < 4) ReprodcutionRate = 4;


            for (int w = 0; w < workers; w++)
                for (int s = 0; s < individual.Schedule[0].Length; s++)
                {
                    if (random.NextDouble() < MutationRate)
                    {

                        individual.Schedule[w][s] = individual.Schedule[w][s] == 1 ? 0 : 1;
                    }



                }


            individual.Fitness = CalculateFitness(individual.Schedule);
            individual.HoursScore = HoursMeet(individual.Schedule);
            individual.ShiftBreakScore = ShiftBreak(individual.Schedule);
            individual.WorkerDispersionScore = WorkerDispersionGeneral(individual.Schedule);
            individual.MaxShiftScore = MaxShifts(individual.Schedule);
            individual.TypeOfWorkerPerShift = EachWorkerTypePerShifts(individual.Schedule);
            individual.PreferencesScore = Preferences(individual.Schedule);
        }



        public void CalculateVarianceAndMean()
        {
            if (_generations == null || _generations.Count == 0)
            {
                Variation = 0.0;
                Mean = 0.0;
                StandardDeviation = 0.0;
                return;
            }

            // Oblicz średnią
            Mean = _generations.Average(gen => gen.Fitness);

            // Oblicz wariancję
            double sumOfSquares = 0.0;
            foreach (var gen in _generations)
            {
                sumOfSquares += Math.Pow(gen.Fitness - Mean, 2);
            }
            Variation = sumOfSquares / _generations.Count;

            // Oblicz odchylenie standardowe
            StandardDeviation = Math.Sqrt(Variation);
        }

        public void AdaptiveParametersUpdate()
        {
            // Zakresy wartości
            const double minReproduction = 0.2;
            const double maxReproduction = 1;
            const double minMutation = 0.1;
            const double maxMutation = 0.8;

            double adaptFactor = CalculateAdaptationFactor();





            // Adaptacja reproduction rate
            if (EtylistRateEnabled)
            {
                double diversityFactor = Math.Max(0.1, 1 - StandardDeviation);
                double reproductionChange = 0.05 * diversityFactor * adaptFactor;


                // Jeśli jest postęp i różnorodność – zmniejsz reproduction 
                ReproductionRateDouble = Math.Max(minReproduction, ReproductionRateDouble - reproductionChange);


                ReprodcutionRate = (int)(ReproductionRateDouble * GenerationLength);
            }

            if (ChangingMutationEbaled)
            {
                // Automatyczne dostosowanie mutation rate jako odwrotność reproduction rate
                double normalizedRepro = (ReproductionRateDouble - minReproduction) / (maxReproduction - minReproduction);
                double inverted = 1.0 - normalizedRepro;
                MutationRate = minMutation + inverted * (maxMutation - minMutation);
            }

        }

        private void Stagnation()
        {
            if (MaxFitness > PreviousBestFitness)
            {
                StagnationCounter = 0; // reset bo mamy poprawę
            }
            else
            {
                StagnationCounter++;    // inkrementuj bo stagnacja trwa
            }

            if (StagnationCounter > MaxStagnation || StandardDeviation < 0.01)
            {
                MutationRate = Math.Min(MutationRate * 1.5, 0.2);
                ReproductionRateDouble = Math.Max(ReproductionRateDouble * 0.8, 1.0);
                ReprodcutionRate = (int)(ReproductionRateDouble * GenerationLength);
                StagnationCounter = 0; // reset, bo "przełamaliśmy" stagnację

            }
        }

        private double CalculateAdaptationFactor()
        {
            // Im większa generacja, tym wolniejsze zmiany
            double rawFactor = 1.0 / (1 + Math.Log(seriesnumber));
            double generationFactor = Math.Max(0.1, rawFactor); // Nigdy nie niższy niż 0.1

            // Jeśli fitness się poprawia, zmieniaj parametry wolniej
            double improvementFactor = 1.0;
            if (_generations.Count > 1)
            {
                double currentBest = _generations[0].Fitness;

                if (currentBest > PreviousBestFitness)
                {
                    improvementFactor = 1.0 / (1 + (currentBest - PreviousBestFitness));
                }
            }

            return generationFactor * improvementFactor;
        }

        public void CalculateHoursScoreMean()
        {
            if (_generations == null || _generations.Count == 0)
            {
                HoursScoreMean = 0;
                return;
            }
            double sum = 0.0;
            foreach (var gen in _generations)
            {
                sum += gen.HoursScore;
            }
            HoursScoreMean = (int)(sum / _generations.Count * 100);
        }

        //---------------------------------------------- Inferior

        public void ExportGenerationToCsv(Generation generation, string outputCsvPath)
        {
            if (generation == null || generation._generations == null || generation._generations.Count == 0)
            {
                throw new ArgumentException("Invalid generation data.");
            }

            using (var writer = new StreamWriter(outputCsvPath))
            {
                // Write CSV header
                writer.WriteLine("Index,Fitness,HoursScore,ShiftBreakScore,WorkerDispersionScore,MaxShiftScore,TypeOfWorkerPerShift,PreferencesScore,Days");

                for (int i = 0; i < generation._generations.Count; i++)
                {
                    var g = generation._generations[i];
                    writer.WriteLine($"{i},{g.Fitness},{g.HoursScore},{g.ShiftBreakScore},{g.WorkerDispersionScore},{g.MaxShiftScore},{g.TypeOfWorkerPerShift},{g.PreferencesScore},{g.Days}");
                }
            }
        }


    }


}