// Wczytanie wymagań z pliku CSV
requiredWorkersPerShiftDisplay = ReadRequirementsFromFile(file);

// Ustawienie wymiarów problemu
numEmployees = employeePreferences.GetLength(0);
numActualDays = requiredWorkersPerShiftDisplay.Length / numShiftsPerDay;

// Generacja początkowej populacji harmonogramów
int[][,] population = new int[populationSize][,];
for (int i = 0; i < populationSize; i++)
    population[i] = GenerateSchedule();

// Pętla ewolucyjna
for (int gen = 0; gen < generations; gen++)
{
    // Obliczanie fitness wszystkich harmonogramów
    Parallel.For(0, populationSize, i => fitness[i] = CalculateFitness(population[i]));

    // Sortowanie populacji wg fitness rosnąco
    population = SortByFitness(population, fitness);

    // Tworzenie nowej populacji
    int[][,] newPopulation = new int[populationSize][,];

    // Elita (najlepsi przechodzą bez zmian)
    for (int i = 0; i < EliteCount; i++)
        newPopulation[i] = population[i];

    // Krzyżowanie i mutacja reszty populacji
    Parallel.For(EliteCount, populationSize - totallyNewChildren, i =>
    {
        int[,] child = Crossover(parent1, parent2);
        if (Random() < mutationRate) child = Mutate(child);
        newPopulation[i] = child;
    });

    // Dodanie kilku całkowicie nowych harmonogramów
    for (int i = populationSize - totallyNewChildren; i < populationSize; i++)
        newPopulation[i] = GenerateSchedule();

    population = newPopulation;
}

// Zapis najlepszego harmonogramu i wyników do pliku CSV
SaveResultsToCsv(population[0], finalWorkerPenalty, finalBestFitness);