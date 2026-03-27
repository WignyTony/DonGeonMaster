using System;
using System.Collections.Generic;
using UnityEngine;

namespace DonGeonMaster.MapGeneration
{
    [Serializable]
    public class GenerationResult
    {
        public GenerationStatus status = GenerationStatus.Echec;
        public int seed;
        public float generationTimeMs;
        public string timestamp;

        public int totalObjectsPlaced;
        public int roomCount;
        public int corridorCount;
        public int walkableCellCount;
        public int wallCellCount;
        public int waterCellCount;

        public Vector2Int spawnCell;
        public Vector2Int exitCell;
        public float spawnToExitDistance;

        public Dictionary<string, int> objectsPerCategory = new();
        public List<ValidationEntry> validationEntries = new();
        public List<string> pipelineSteps = new();

        public int errorCount;
        public int warningCount;
        public int infoCount;

        public string summaryText;

        public void AddPipelineStep(string step)
        {
            pipelineSteps.Add($"[{DateTime.Now:HH:mm:ss.fff}] {step}");
        }

        public void CountValidation()
        {
            errorCount = 0;
            warningCount = 0;
            infoCount = 0;
            foreach (var entry in validationEntries)
            {
                switch (entry.severity)
                {
                    case ValidationSeverity.Erreur: errorCount++; break;
                    case ValidationSeverity.Warning: warningCount++; break;
                    case ValidationSeverity.Info: infoCount++; break;
                }
            }

            if (errorCount > 0) status = GenerationStatus.Echec;
            else if (warningCount > 0) status = GenerationStatus.SuccesAvecWarnings;
            else status = GenerationStatus.Succes;
        }

        public string BuildSummary()
        {
            summaryText = $"=== Génération {timestamp} ===\n" +
                          $"Seed: {seed}\n" +
                          $"Statut: {status}\n" +
                          $"Temps: {generationTimeMs:F1}ms\n" +
                          $"Salles: {roomCount} | Couloirs: {corridorCount}\n" +
                          $"Objets placés: {totalObjectsPlaced}\n" +
                          $"Cellules marchables: {walkableCellCount}\n" +
                          $"Erreurs: {errorCount} | Warnings: {warningCount}\n" +
                          $"Spawn: ({spawnCell.x},{spawnCell.y}) → Sortie: ({exitCell.x},{exitCell.y})\n" +
                          $"Distance spawn-sortie: {spawnToExitDistance:F1}";
            return summaryText;
        }
    }

    [Serializable]
    public class ValidationEntry
    {
        public ValidationSeverity severity;
        public string ruleName;
        public string message;
        public Vector2Int? cell;

        public ValidationEntry(ValidationSeverity severity, string ruleName, string message, Vector2Int? cell = null)
        {
            this.severity = severity;
            this.ruleName = ruleName;
            this.message = message;
            this.cell = cell;
        }

        public override string ToString()
        {
            string prefix = severity switch
            {
                ValidationSeverity.Erreur => "[ERREUR]",
                ValidationSeverity.Warning => "[WARN]",
                _ => "[INFO]"
            };
            string cellStr = cell.HasValue ? $" @({cell.Value.x},{cell.Value.y})" : "";
            return $"{prefix} {ruleName}: {message}{cellStr}";
        }
    }

    [Serializable]
    public class GenerationMetrics
    {
        public int totalGenerations;
        public int successes;
        public int warnings;
        public int failures;
        public float avgGenerationTimeMs;
        public float minGenerationTimeMs = float.MaxValue;
        public float maxGenerationTimeMs;
        public List<int> failedSeeds = new();
        public List<int> warningSeeds = new();

        public void Record(GenerationResult result)
        {
            totalGenerations++;
            float totalTime = avgGenerationTimeMs * (totalGenerations - 1) + result.generationTimeMs;
            avgGenerationTimeMs = totalTime / totalGenerations;

            if (result.generationTimeMs < minGenerationTimeMs) minGenerationTimeMs = result.generationTimeMs;
            if (result.generationTimeMs > maxGenerationTimeMs) maxGenerationTimeMs = result.generationTimeMs;

            switch (result.status)
            {
                case GenerationStatus.Succes: successes++; break;
                case GenerationStatus.SuccesAvecWarnings:
                    warnings++;
                    warningSeeds.Add(result.seed);
                    break;
                case GenerationStatus.Echec:
                    failures++;
                    failedSeeds.Add(result.seed);
                    break;
            }
        }

        public string BuildReport()
        {
            return $"=== Rapport Batch ===\n" +
                   $"Total: {totalGenerations}\n" +
                   $"Succès: {successes} | Warnings: {warnings} | Échecs: {failures}\n" +
                   $"Temps moyen: {avgGenerationTimeMs:F1}ms (min: {minGenerationTimeMs:F1}, max: {maxGenerationTimeMs:F1})\n" +
                   $"Seeds échouées: [{string.Join(", ", failedSeeds)}]\n" +
                   $"Seeds warnings: [{string.Join(", ", warningSeeds)}]";
        }
    }
}
