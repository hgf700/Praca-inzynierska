namespace GeneticAlgorithm.ExternalClasses
{
    public class GeneticScheduler
    {
        private Random random = new Random();
        private int maxEmployees = 4;
        private int populationSize = 100;
        private int generations = 100;
        private double mutationRate = 0.05;

        // Main optimization method to find the best optimal schedule
        public List<int> Optimize(List<int> clientCounts, List<string> daysOfWeek)
        {
            // Initialize the population
            List<List<int>> population = new List<List<int>>();
            for (int i = 0; i < populationSize; i++)
            {
                population.Add(RandomIndividual(clientCounts.Count));
            }
            // 4 3 5

            // 4       6 
            // Jan    
            // Paweł
            // Anna
            // Kamila
            // Evolve the population
            for (int gen = 0; gen < generations; gen++)
            {
                population = population.OrderBy(ind => Fitness(ind, clientCounts, daysOfWeek)).ToList();

                List<List<int>> newPopulation = new List<List<int>>();
                newPopulation.Add(population[0]);
                newPopulation.Add(population[1]);

                // Crossover and mutation
                while (newPopulation.Count < populationSize)
                {
                    var parent1 = Select(population, clientCounts, daysOfWeek);
                    var parent2 = Select(population, clientCounts, daysOfWeek);
                    var child = Crossover(parent1, parent2);
                    Mutate(child);
                    newPopulation.Add(child);
                }

                population = newPopulation;
            }

            // Return the best individual
            return population[0];
        }

        // Generate a random individual
        private List<int> RandomIndividual(int size)
        {
            return Enumerable.Range(0, size).Select(_ => random.Next(1, maxEmployees + 1)).ToList();
        }

        // Calculate the fitness of an individual
        private int Fitness(List<int> individual, List<int> clientCounts, List<string> daysOfWeek)
        {
            int totalError = 0;

            for (int i = 0; i < individual.Count; i++)
            {
                int clients = clientCounts[i];
                int idealEmployees = Math.Max(1, clients / 20);

                if (daysOfWeek[i] == "Saturday")
                {
                    idealEmployees = (int)(idealEmployees * 0.8); // Reduction by 20%
                    if (idealEmployees < 1) idealEmployees = 1;
                }

                totalError += Math.Abs(individual[i] - idealEmployees);
            }

            return totalError;
        }

        // Tournament selection
        private List<int> Select(List<List<int>> population, List<int> clientCounts, List<string> daysOfWeek)
        {
            var tournament = population.OrderBy(x => random.Next()).Take(5).ToList();
            return tournament.OrderBy(x => Fitness(x, clientCounts, daysOfWeek)).First();
        }

        // Crossover 50% chance
        private List<int> Crossover(List<int> parent1, List<int> parent2)
        {
            int split = random.Next(parent1.Count);
            return parent1.Take(split).Concat(parent2.Skip(split)).ToList();
        }

        // Child mutation 5% chance
        private void Mutate(List<int> individual)
        {
            for (int i = 0; i < individual.Count; i++)
            {
                if (random.NextDouble() < mutationRate)
                {
                    individual[i] = random.Next(1, maxEmployees + 1);
                }
            }
        }
    }
}
