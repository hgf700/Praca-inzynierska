using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace msi_GA.GA
{
    public class SaveManagment
    {
        private readonly string _saveDirectory;
        private readonly string _manifestFilePath;

        public string SaveDirectory => _saveDirectory;

        public SaveManagment(string saveDirectory)
        {
            // Jeśli ścieżka jest względna, to bazujemy na katalogu projektu (znajdując folder "Saved" powyżej)
            if (!Path.IsPathRooted(saveDirectory))
            {
                var baseDir = AppContext.BaseDirectory; // np. bin\Debug\net8.0

                // Przeszukujemy katalogi nadrzędne aż znajdziemy folder zawierający "Saved"
                string projectRoot = FindProjectRootContainingFolder(baseDir, "Saved");
                if (string.IsNullOrEmpty(projectRoot))
                    throw new DirectoryNotFoundException("Nie znaleziono katalogu projektu zawierającego folder 'Saved'.");

                _saveDirectory = Path.Combine(projectRoot, saveDirectory);
            }
            else
            {
                _saveDirectory = saveDirectory;
            }

            _manifestFilePath = Path.Combine(_saveDirectory, "Manifest", "manifest.json");
        }

        private string FindProjectRootContainingFolder(string startDirectory, string folderName)
        {
            var dir = new DirectoryInfo(startDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, folderName)))
            {
                dir = dir.Parent;
            }
            return dir?.FullName;
        }

        public void CheckCreateFolder(int n)
        {
            string basePath = Path.Combine(_saveDirectory, "Saves");
            string folderName = $"{n}Save";
            string fullPath = Path.Combine(basePath, folderName);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }

        public void CopyWorkersFile(int generationNumber, string sourceFilePath, string destinationFolderPath)
        {
            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("Plik pracowników nie istnieje", sourceFilePath);
            }

            if (!Directory.Exists(destinationFolderPath))
            {
                Directory.CreateDirectory(destinationFolderPath);
            }

            string destinationFile = Path.Combine(destinationFolderPath, $"generation_{generationNumber}_workers.json");

            try
            {
                File.Copy(sourceFilePath, destinationFile, true);
            }
            catch (IOException ex)
            {
                throw new IOException($"Błąd podczas kopiowania pliku: {ex.Message}", ex);
            }
        }

        public void SaveGeneration(Generation generation)
        {
            CheckCreateFolder(generation.number);

            string saveFolderPath = Path.Combine(_saveDirectory, "Saves", $"{generation.number}Save");
            string saveFilePath = Path.Combine(saveFolderPath, $"{generation.number}_Generation_{generation.seriesnumber}.json");

            CopyWorkersFile(generation.number, generation.filepathWorkers, saveFolderPath);

            var dataToSave = new
            {
                GenerationInfo = new
                {
                    generation.number,
                    generation.seriesnumber,
                    generation.ReprodcutionRate,
                    generation.MutationRate,
                    generation.BestParentsKept,
                    generation.GenerationLength,
                },
                ListOfGens = generation._generations.Select(g => new
                {
                    Schedule = g.Schedule,
                    Fitness = g.Fitness,
                    HoursScore = g.HoursScore,
                    ShiftBreakScore = g.ShiftBreakScore,
                    WorkerDispersionScore = g.WorkerDispersionScore,
                    MaxShiftScore = g.MaxShiftScore,
                    TypeOfWorkerPerShift = g.TypeOfWorkerPerShift,
                    Preferences = g.PreferencesScore,
                    Days = g.Days,
                }).ToList(),
            };

            string json = JsonConvert.SerializeObject(dataToSave, Formatting.Indented);
            File.WriteAllText(saveFilePath, json);
        }

        public Generation LoadGeneration(int generationNumber, int seriesNumber)
        {
            string saveFolderPath = Path.Combine(_saveDirectory, "Saves", $"{generationNumber}Save");
            string saveFilePath = Path.Combine(saveFolderPath, $"{generationNumber}_Generation_{seriesNumber}.json");
            string workerFilePath = Path.Combine(saveFolderPath, $"generation_{generationNumber}_workers.json");

            if (!File.Exists(saveFilePath))
                throw new FileNotFoundException($"Generation file not found at {saveFilePath}");

            string json = File.ReadAllText(saveFilePath);
            var data = JsonConvert.DeserializeAnonymousType(json, new
            {
                GenerationInfo = new
                {
                    number = 0,
                    seriesnumber = 0,
                    ElitistRate = 0,
                    MutationRate = 0.0,
                    BestParentsKept = 0,
                    GenerationLength = 0,
                },
                ListOfGens = new[] { new
                {
                    Schedule = new int[][] { new int[] { 0 } },
                    Fitness = 0.0,
                    HoursScore = 0.0,
                    ShiftBreakScore = 0.0,
                    WorkerDispersionScore = 0.0,
                    MaxShiftScore = 0.0,
                    TypeOfWorkerPerShift = 0.0,
                    PreferencesScore = 0.0,
                    Days = 0,
                } }.ToList(),
                Statistics = new
                {
                    MaxFitness = 0,
                    MaxEachWorkerTypePerShift = 0,
                    MaxFitnessPerWorker = 0.0,
                    MaxHoursScore = 0,
                    MaxShiftBreakScore = 0,
                    Mean = 0.0,
                    Variation = 0.0
                }
            });

            if (data == null)
                throw new JsonException("Failed to deserialize generation data. The file may be empty or invalid.");

            Generation generation = new Generation()
            {
                number = data.GenerationInfo.number,
                seriesnumber = data.GenerationInfo.seriesnumber,
                ReprodcutionRate = data.GenerationInfo.ElitistRate,
                MutationRate = data.GenerationInfo.MutationRate,
                BestParentsKept = data.GenerationInfo.BestParentsKept,
                GenerationLength = data.GenerationInfo.GenerationLength,
                _generations = data.ListOfGens.Select(g => new Gen()
                {
                    Schedule = null,
                    Fitness = g.Fitness,
                    HoursScore = g.HoursScore,
                    ShiftBreakScore = g.ShiftBreakScore,
                    WorkerDispersionScore = g.WorkerDispersionScore,
                    MaxShiftScore = g.MaxShiftScore,
                    TypeOfWorkerPerShift = g.TypeOfWorkerPerShift,
                    PreferencesScore = g.PreferencesScore,
                    Days = g.Days,
                }).ToList(),
            };

            generation.LoadWorkersFromJson(workerFilePath);

            int workers = generation.workers;
            int days = generation._generations.Count > 0 ? generation._generations[0].Days : 0;

            int[][] InitializeSchedule(int w, int d)
            {
                int[][] schedule = new int[w][];
                for (int i = 0; i < w; i++)
                {
                    schedule[i] = new int[3 * d];
                }
                return schedule;
            }

            int[][] DeepCopySchedule(int[][] source)
            {
                if (source == null) return null;
                int[][] copy = new int[source.Length][];
                for (int i = 0; i < source.Length; i++)
                {
                    copy[i] = new int[source[i].Length];
                    Array.Copy(source[i], copy[i], source[i].Length);
                }
                return copy;
            }

            for (int i = 0; i < generation._generations.Count; i++)
            {
                generation._generations[i].Schedule = InitializeSchedule(workers, days);
                if (data.ListOfGens.Count > i && data.ListOfGens[i].Schedule != null)
                {
                    generation._generations[i].Schedule = DeepCopySchedule(data.ListOfGens[i].Schedule);
                }
            }

            if (generation._generations.Count > 0)
            {
                generation.Schedule = DeepCopySchedule(generation._generations[0].Schedule);
            }

            return generation;
        }

        public void CreateEmptyManifest()
        {
            var manifest = new
            {
                LastCreatedFileId = 0,
            };

            string manifestFolder = Path.GetDirectoryName(_manifestFilePath);
            if (!Directory.Exists(manifestFolder))
            {
                Directory.CreateDirectory(manifestFolder);
            }

            string json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
            File.WriteAllText(_manifestFilePath, json);
        }

        public void UpdateManifest(int number)
        {
            try
            {
                if (!File.Exists(_manifestFilePath))
                {
                    CreateEmptyManifest();
                }

                string json = File.ReadAllText(_manifestFilePath);
                var manifest = JsonConvert.DeserializeObject<ManifestModel>(json);
                if (manifest != null)
                {
                    manifest.LastCreatedFileId = number;
                    string updatedJson = JsonConvert.SerializeObject(manifest, Formatting.Indented);
                    File.WriteAllText(_manifestFilePath, updatedJson);
                }
                else
                {
                    throw new InvalidOperationException("Manifest file was empty or invalid.");
                }
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse manifest file", ex);
            }
            catch (IOException ex)
            {
                throw new IOException("Failed to write to manifest file", ex);
            }
        }

        public int ReadManifest()
        {
            if (!File.Exists(_manifestFilePath))
            {
                CreateEmptyManifest();
            }

            try
            {
                string json = File.ReadAllText(_manifestFilePath);
                var manifest = JsonConvert.DeserializeObject<ManifestModel>(json);
                return manifest?.LastCreatedFileId ?? 0;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse manifest file", ex);
            }
        }
    }

    public class ManifestModel
    {
        public int LastCreatedFileId { get; set; }
    }
}
