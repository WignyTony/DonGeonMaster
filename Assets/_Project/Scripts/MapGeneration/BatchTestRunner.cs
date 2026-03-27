using System;
using System.Collections;
using UnityEngine;

namespace DonGeonMaster.MapGeneration
{
    public class BatchTestRunner : MonoBehaviour
    {
        public bool isRunning { get; private set; }
        public int currentIteration { get; private set; }
        public int totalIterations { get; private set; }
        public GenerationMetrics metrics { get; private set; }

        public event Action<int, int, GenerationResult> OnIterationComplete;
        public event Action<GenerationMetrics> OnBatchComplete;
        public event Action<string> OnStatusUpdate;

        MapGenConfig baseConfig;
        MapGenerator generator;
        GenerationValidator validator;
        bool cancelRequested;

        public void StartBatch(MapGenConfig config, int iterations)
        {
            if (isRunning)
            {
                Debug.LogWarning("[BatchTestRunner] Un batch est déjà en cours");
                return;
            }

            baseConfig = config.Clone();
            totalIterations = iterations;
            generator = new MapGenerator();
            validator = new GenerationValidator();
            metrics = new GenerationMetrics();
            cancelRequested = false;

            StartCoroutine(RunBatch());
        }

        public void Cancel()
        {
            cancelRequested = true;
            OnStatusUpdate?.Invoke("Annulation en cours...");
        }

        IEnumerator RunBatch()
        {
            isRunning = true;
            OnStatusUpdate?.Invoke($"Batch démarré: {totalIterations} itérations");

            for (currentIteration = 0; currentIteration < totalIterations; currentIteration++)
            {
                if (cancelRequested)
                {
                    OnStatusUpdate?.Invoke($"Batch annulé après {currentIteration} itérations");
                    break;
                }

                var iterConfig = baseConfig.Clone();
                iterConfig.useRandomSeed = true;
                iterConfig.lockSeed = false;

                var (map, result) = generator.Generate(iterConfig);

                if (iterConfig.validateAfterGeneration)
                    validator.Validate(map, iterConfig, result);

                metrics.Record(result);
                OnIterationComplete?.Invoke(currentIteration, totalIterations, result);

                if (currentIteration % 10 == 0)
                {
                    float pct = (float)currentIteration / totalIterations * 100f;
                    OnStatusUpdate?.Invoke(
                        $"Batch: {currentIteration}/{totalIterations} ({pct:F0}%) " +
                        $"S:{metrics.successes} W:{metrics.warnings} E:{metrics.failures}");
                }

                // Yield pour ne pas bloquer le thread principal
                if (currentIteration % 5 == 0)
                    yield return null;
            }

            // Écrire le rapport
            string reportPath = GenerationLogger.WriteBatchReport(metrics, baseConfig);
            OnStatusUpdate?.Invoke($"Batch terminé. Rapport: {reportPath}");
            OnBatchComplete?.Invoke(metrics);

            isRunning = false;
        }
    }
}
