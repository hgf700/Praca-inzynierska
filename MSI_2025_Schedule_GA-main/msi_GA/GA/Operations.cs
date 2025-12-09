using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reflection.Metadata;
using System.Xml;

namespace msi_GA.GA
{
    public abstract class Operations
    {
        // Normalized weights (sum to 1.0)
        private const double HOURS_WEIGHT = 0.25;
        private const double SHIFT_BREAK_WEIGHT = 0.28;
        private const double PREFERENCES_WEIGHT = 0.03;
        private const double OCCUPATION_WEIGHT = 0.11;
        private const double DISPERSION_WEIGHT = 0.05;
        private const double MAX_SHIFTS_WEIGHT = 0.28;

        Random random = new Random();

        private ThreadLocal<Random> threadLocalRandom = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));


        private object randLock = new object();




        protected const int OccupationMatchedScore = 3;

        public double CalculateFitness(int[][] schedule)
        {
            // Use normalized scores exclusively
            return HoursMeet(schedule) * HOURS_WEIGHT +
                   MaxShifts(schedule) * MAX_SHIFTS_WEIGHT +
                   ShiftBreak(schedule) * SHIFT_BREAK_WEIGHT +
                   WorkerDispersionGeneral(schedule) * DISPERSION_WEIGHT +
                   EachWorkerTypePerShifts(schedule) * OCCUPATION_WEIGHT +
                   Preferences(schedule) * PREFERENCES_WEIGHT;
        }

        public double GetNormalizedFitness(int[][] schedule)
        {

            double maxScore = MaxFitness(schedule);

            double fitnessScore = CalculateFitness(schedule);

            return fitnessScore / maxScore;
        }

        public double MaxFitness(int[][] schedule)
        {
            return (schedule.Length * HOURS_WEIGHT) // HOURS
                          + (schedule.Length * (schedule.Length / 3) * MAX_SHIFTS_WEIGHT) // MAX_SHIFTS
                          + ((schedule.Length * ((schedule[0].Length - 1) / 3)) * SHIFT_BREAK_WEIGHT)  // SHIFT_BREAK
                          + (schedule[0].Length * DISPERSION_WEIGHT) // DISPERSION
                          + (schedule[0].Length * OCCUPATION_WEIGHT) // OCCUPATION
                         + (schedule.Length * schedule.Length * PREFERENCES_WEIGHT); // PREFERENCES



        }



        // 0 - intern , 1 - junior, 2 - mid, 3 - senior





        // Example: Evaluate "overwork danger" for a worker

        public int FindMostChangedWorker(Gen original, Gen mutated)
        {
            int maxChanges = 0;
            int workerIndex = 0;

            for (int w = 0; w < original.Schedule.Length; w++)
            {
                int changes = 0;
                for (int s = 0; s < original.Schedule[w].Length; s++)
                {
                    if (original.Schedule[w][s] != mutated.Schedule[w][s]) changes++;
                }

                if (changes > maxChanges)
                {
                    maxChanges = changes;
                    workerIndex = w;
                }
            }

            return workerIndex;
        }


        public int FindMostChangedShift(Gen original, Gen mutated)
        {

            int maxChanges = 0;
            int ShiftIndex = 0;

            for (int s = 0; s < original.Schedule[0].Length; s++)
            {
                int changes = 0;
                for (int w = 0; w < original.Schedule.Length; w++)
                {
                    if (original.Schedule[w][s] != mutated.Schedule[w][s]) changes++;
                }
                if (changes > maxChanges)
                {
                    maxChanges = changes;
                    ShiftIndex = s;
                }
            }

            return ShiftIndex;
        }
        public float CalculateShiftDensity(int[][] schedule, int worker)
        {
            // Calculate how concentrated shifts are
            int consecutive = 0;
            int maxConsecutive = 0;

            for (int s = 0; s < schedule[worker].Length; s++)
            {
                if (schedule[worker][s] == 1)
                {
                    consecutive++;
                    maxConsecutive = Math.Max(maxConsecutive, consecutive);
                }
                else
                {
                    consecutive = 0;
                }
            }

            return (float)maxConsecutive / schedule[worker].Length;
        }


        //----------------------- fitness score segment---------------------------------//
        protected virtual double Preferences(int[][] schedule)
        {
            int sum = 0;

            for (int i = 0; i < schedule.Length; i++)
            {

                for (int k = 0; k < Generation.Workers[i].Preferences.Length; k++)
                {
                    if (Generation.Workers[i].Preferences[k] == schedule[i][k])
                    {
                        sum++;
                    }
                }


            }


            return sum * 1.0 / (schedule.Length * schedule[0].Length);
        }


        protected virtual int PreferencesPerWorker(int[][] schedule, int i)
        {
            int sum = 0;
            for (int k = 0; k < Generation.Workers[i].Preferences.Length; k++)
            {
                if (Generation.Workers[i].Preferences[k] == schedule[i][k])
                {
                    sum++;
                }
            }
            return sum;
        }

        protected virtual double EachWorkerTypePerShifts(int[][] schedule)
        {
            int points = 0;

            


                for (int i = 0; i < schedule[0].Length; i++)
                {
                    for (int j = 0; j < schedule.Length; j++)
                    {
                        int sum = 0;
                        if (schedule[j][i] == 1)
                        {

                            

                                if (Generation.Workers[j].Experience != 0)
                                {
                                    sum++;

                                }
                                else
                                {
                                    if (FindExperiencedPeer(schedule, j, i))
                                    {
                                        sum++;

                                    }

                                }


                            



                        }
                        if (sum != 0)
                        {
                            points++;
                            break; ;
                        }
                    }


                }
            }




        


        protected virtual bool FindExperiencedPeer(int[][] schedule, int i, int j)
        {
            for (int k = 0; k < schedule.Length; k++)
            {
                if (schedule[k][j] == 1 && Generation.Workers[k].Experience > Generation.Workers[i].Experience)
                {
                    return true;
                }
            }

            return false;
        }


        protected virtual double HoursMeet(int[][] schedule)
        {
            double sum = 0;
            for (int i = 0; i < schedule.Length; i++)
            {
                int hours = CalculateWorkerHours(schedule, i);
                sum += hours switch
                {
                    < 24 => 0.2,
                    40 => 1.0,
                    > 48 => 0.1,
                    _ => 0.8
                };
            }
            return sum / schedule.Length; // Already normalized
        }

        public int CalculateWorkerHours(int[][] schedule, int i)
        {
            int hours = 0;
            for (int j = 0; j < schedule[i].Length; j++)
            {
                if (schedule[i][j] != 0)
                {
                    hours += 8;
                }
            }
            return hours;
        }



        protected virtual double MaxShifts(int[][] schedule)
        {

            int sum = 0;
            int daysMatched = 0;
            for (int i = 0; i < schedule.Length; i++)
            {
                for (int j = 0; j < schedule[i].Length; j = j + 3)
                {


                    for (int k = j; k < j + 3; k++)
                    {
                        if (schedule[i][k] == 1) sum++;
                    }


                    if (sum < 2) daysMatched++;
                    sum = 0;
                }
            }


            return daysMatched * 1.0 / (schedule.Length * (schedule[0].Length / 3));

        }


        protected virtual double ShiftBreak(int[][] schedule)
        {
            int sum = 0;


            for (int i = 0; i < schedule.Length; i++)
            {
                for (int j = 2; j < schedule[i].Length - 3; j = j + 3)
                {

                    if (schedule[i][j] != 1 || schedule[i][j + 1] != 1)
                    {

                        sum++;

                    }
                }

            }

            return sum * 1.0 / (schedule.Length * ((schedule[0].Length - 1) / 3));

        }

        // chcemy zeby byl ktos 24/7 wiec tak naprawde musimy sprawdzic kazda zmiane czy ktos jest 
        protected virtual double WorkerDispersionGeneral(int[][] schedule)
        {

            int sum = 0;



            for (int i = 0; i < schedule[0].Length; i++) // teraz sprawdzamy kazdy dzien
            {
                for (int j = 0; j < schedule.Length; j++) // sprawdzamy czy jest jakis pracownik na tej zmianie
                {
                    if (schedule[j][i] == 1)
                    {
                        sum++;
                        break;

                    }

                }

            }

            return sum * (1.0 / schedule[0].Length); // Normalized to [0, 1]
        }





        public double CalculateMaxPreferences(int[][] schedule)
        {
            return PREFERENCES_WEIGHT * schedule.Length * schedule[0].Length;
        }







        //-----------------------------------------------------

        public double CalculatePreferenceMathedScore(int[][] schedule, int i)
        {
            double sum = 0;
            for (int k = 0; k < Generation.Workers[i].Preferences.Length; k++)
            {
                if (Generation.Workers[i].Preferences[k] == schedule[i][k])
                {
                    sum++;
                }
            }
            return sum;
        }




        //-------------------------------------------------------------
        public double CalculateMaxEachWorkerTypePerShifts(int[][] schedule)
        {

            return schedule[0].Length * OccupationMatchedScore;
        }
        public double ShiftBreaksPerWorker(int[][] schedule, int i)
        {
            double sum = 0.0;


            for (int j = 2; j < schedule[i].Length - 3; j = j + 3)
            {
                if (schedule[i][j] == 1 && schedule[i][j + 1] == 1)
                {
                    sum++;
                }
            }


            return sum * 1.0 / ((schedule[0].Length / 3) - 1);
        }



        //------------- normalization-----------------------

        public double NormalizeHoursScore(int[][] schedule)
        {
            double total = 0;
            for (int i = 0; i < schedule.Length; i++)
            {
                int hours = CalculateWorkerHours(schedule, i);
                if (hours < 24) total += 0.2;
                else if (hours == 40) total += 1.0;
                else if (hours > 48) total += 0.1;
                else total += 0.8; // 24-48 range
            }
            return total / schedule.Length;
        }


        public double ApplyExponentialPenalty(double score)
        {
            // More severe penalty for more violations
            return Math.Pow(score, 3);
        }

        //--------------------------------------------



        public void CheckDay(int[][] schedule)
        {



        }
        public int NoDispersionShift(int[][] schedule, int value)
        {
            

            


                for (int i = value; i < schedule[1].Length; i++)
                {
                    for (int j = 0; j < schedule[0].Length; j++)
                    {
                        int sum = 0;
                        if (schedule[j][i] == 1)
                        {

                            

                                if (Generation.Workers[j].Experience != 0)
                                {
                                    sum++;

                                }
                                else
                                {
                                    if (FindExperiencedPeer(schedule, j, i))
                                    {
                                        sum++;

                                    }

                                }


                            


                        }
                        if (sum > 0.9999)
                        {
                            return j;
                        }
                    }


                


            }
            return -1;

        }




        public int FindTheWorstHours(int[][] schedule, int startWorker)
        {
            int worstWorker = -1;

            for (int i = startWorker; i < schedule.Length; i++)
            {
                int hours = CalculateWorkerHours(schedule, i);
                if (hours > 48 || hours < 24)
                {
                    worstWorker = i;
                    break; 
                }
            }

            return worstWorker;
        }

        public (int, int) FindNeededBreak(int[][] schedule, int startWorker)
        {
            for (int i = startWorker; i < schedule.Length; i++)
            {
                for (int j = 2; j < schedule[i].Length - 3; j += 3)
                {
                    if (schedule[i][j] == 1 && schedule[i][j + 1] == 1)
                    {
                        return (j, i); // Zwracaj (day, worker)
                    }
                }
            }

            return (-1, -1);
        }

        public (int worker, int day) FindBadShift(int[][] schedule, int startWorker)
        {
            for (int i = startWorker; i < schedule.Length; i++)
            {
                for (int j = 0; j < schedule[i].Length; j += 3)
                {
                    int shifts = 0;
                    for (int k = j; k < j + 3 && k < schedule[i].Length; k++)
                    {
                        if (schedule[i][k] == 1) shifts++;
                    }

                    if (shifts < 2)
                    {
                        return (i, j);
                    }
                }
            }

            return (-1, -1);
        }




        private Random _random = new Random();

        // Zwraca losowego pracownika, który ma zmianę w podanym dniu
        public int RandomWorkerOnDay(int[][] schedule, int day)
        {
            var workersWithShift = new List<int>();

            for (int worker = 0; worker < schedule[0].Length; worker++)
            {
                if (schedule[worker][day] != 0)  // 0 = brak zmiany
                {
                    workersWithShift.Add(worker);
                }
            }

            if (workersWithShift.Count == 0)
                return -1; // brak pracownika z przypisaną zmianą tego dnia

            int index = _random.Next(workersWithShift.Count);
            return workersWithShift[index];
        }

        // Zwraca losowy dzień różny od excludeDay
        public int RandomDifferentDay(int excludeDay, int totalDays)
        {
            if (totalDays <= 1)
                return -1;

            int newDay;
            do
            {
                newDay = _random.Next(totalDays);
            } while (newDay == excludeDay);

            return newDay;
        }

        // Znajduje pierwszy dzień, w którym pracownik ma przypisaną zmianę (kandydata do usunięcia)
        public int FindLeastCriticalShift(int[][] schedule, int worker)
        {
           
            for (int day = 0; day < day+3; day++)
            {
                if (schedule[worker][day] != 0)
                {
                    return day;
                }
            }
            return -1; // brak zmian do usunięcia
        }

        // Znajduje pierwszego dostępnego pracownika, który nie ma przypisanej zmiany tego dnia
        public int FindAvailableWorkerForShift(int[][] schedule, int day)
        {
            for (int worker = 0; worker < schedule.Length; worker++)
            {
                if (schedule[worker][day] == 0)
                {
                    
                    return worker;
                }
            }
            return -1; // brak dostępnego pracownika
        }

        //--------- mutatiion and crossover 

        public int GenerateMaskOnTheFly(int parentsAmount)
        {
            return random.Next(0, parentsAmount);

        }

        public int GenerateMaskOnTheFly2()
        {
            lock (randLock)
            {
                return random.Next(0, 2);
            }
        }

        public void Mutation(List<Gen> generation, double MutationRate)
        {

            foreach (Gen gen in generation)
            {

                for (int i = 0; i < gen.Schedule.Length; i++)
                {
                    for (int j = 0; j < gen.Schedule[i].Length; j++)
                    {
                        if (random.NextDouble() < MutationRate)
                        {
                            gen.Schedule[i][j] = threadLocalRandom.Value.Next(0, 2);


                        }
                    }
                }
            }



        }


        public void Mutate(Gen gen, double MutationRate)
        {
            for (int i = 0; i < gen.Schedule.Length; i++)
            {
                for (int j = 0; j < gen.Schedule[i].Length; j++)
                {
                    if (random.NextDouble() < MutationRate)
                    {
                        gen.Schedule[i][j] = threadLocalRandom.Value.Next(0, 2);
                    }
                }
            }
        }

        public void MutationForEach(List<Gen> generation, double mutationRate)
        {
            Parallel.ForEach(generation, gen =>
            {
                var newSchedule = gen.Schedule.Select(worker => worker.ToArray()).ToArray();

                for (int w = 0; w < newSchedule.Length; w++)
                {
                    for (int s = 0; s < newSchedule[w].Length; s++)
                    {
                        if (random.NextDouble() < mutationRate)
                        {
                            newSchedule[w][s] = newSchedule[w][s] == 1 ? 0 : 1;
                        }
                    }
                }

                gen.Schedule = newSchedule;

            });
        }







        public void MutateWorker(int[][] schedule, int WorstWorker, double NewMutationRate)
        {
            for (int j = 0; j < schedule[WorstWorker].Length; j++)
            {
                if (random.NextDouble() < NewMutationRate)
                {
                    schedule[WorstWorker][j] = random.Next(0, 2);
                }
            }
        }






        public int CheckDay(int[][] schedule, int Worker, int day)
        {

            int sum = 0;
            for (int i = day; i < day + 3; i++)
            {
                if (schedule[Worker][i] == 1)
                    sum++;
            }

            return sum;


        }



  

        public bool GenerateMaskOnTheFly1()
        {
            lock (randLock)
            {
                return random.Next(0, 2) == 1;
            }
        }

  








    }


}