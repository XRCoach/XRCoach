using UnityEngine;
using Unity.Barracuda;
using Unity.Collections;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class InferenceRunner : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private NNModel modelAsset;

    [Header("Buffer Settings")]
    [SerializeField] private int windowSize = 100;
    [SerializeField] private int numFeatures = 10;

    private IWorker worker;
    private NativeArray<float> featureBuffer;
    private int bufferIndex = 0;

    private void Start()
    {
        if (modelAsset == null)
        {
            Debug.LogError("[InferenceRunner] NNModel manquant !");
            enabled = false;
            return;
        }

        var model = ModelLoader.Load(modelAsset);

        // Essaie ComputePrecompiled → sinon fallback CSharp
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
        // Si ça plante → décommente la ligne suivante :
        // worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpRef, model);

        featureBuffer = new NativeArray<float>(windowSize * numFeatures, Allocator.Persistent);

        var fusion = FindObjectOfType<SensorFusion>();
        if (fusion != null)
            fusion.OnFeaturesUpdated += OnNewFeatures;
        else
            Debug.LogError("[InferenceRunner] SensorFusion introuvable !");
    }

    private void OnNewFeatures(float[] features)
    {
        if (features == null || features.Length < numFeatures) return;

        int offset = bufferIndex * numFeatures;
        for (int i = 0; i < numFeatures; i++)
            featureBuffer[offset + i] = features[i];

        bufferIndex = (bufferIndex + 1) % windowSize;

        if (bufferIndex == 0)
            RunInference();
    }

    private void RunInference()
    {
        var sw = Stopwatch.StartNew();

        // CRÉATION DU TENSOR → .ToArray() obligatoire pour compatibilité
        float[] bufferCopy = featureBuffer.ToArray();
        using var inputTensor = new Tensor(1, windowSize, numFeatures, 1, bufferCopy);

        // EXÉCUTION COMPATIBLE AVEC TOUTES LES VERSIONS DE BARRACUDA
        worker.Execute(inputTensor);                    // ← Fonctionne partout
        // Si tu as une version récente, tu peux aussi faire :
        // worker.Schedule(inputTensor);
        // worker.FlushSchedule().Complete();

        using var outputTensor = worker.PeekOutput();

        // LECTURE SÉCURISÉE DES PROBABILITÉS (aucun ToReadOnlySpan)
        int numClasses = outputTensor.channels;
        float[] probs = new float[numClasses];
        for (int i = 0; i < numClasses; i++)
        {
            probs[i] = outputTensor[0, i]; // [batch, class]
        }

        // Classe prédite
        int predictedClass = 0;
        float maxProb = probs[0];
        for (int i = 1; i < numClasses; i++)
        {
            if (probs[i] > maxProb)
            {
                maxProb = probs[i];
                predictedClass = i;
            }
        }

        // Envoi au SessionManager
        var session = FindObjectOfType<SessionManager>();
        session?.OnPrediction(predictedClass, probs);

        float ms = sw.ElapsedTicks * 1000f / Stopwatch.Frequency;
        // Debug.Log($"[AI] {predictedClass} | {maxProb:P1} | {ms:F2} ms");
    }

    private void OnDestroy()
    {
        worker?.Dispose();
        if (featureBuffer.IsCreated)
            featureBuffer.Dispose();
    }
}