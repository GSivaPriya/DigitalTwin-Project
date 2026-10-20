
using System;
using UnityEngine;
using Unity.InferenceEngine;


public class AnomalyModel : MonoBehaviour
{
    public static AnomalyModel Instance;

    [SerializeField] ModelAsset onnxModel;
    [SerializeField] TextAsset scalerParameters;
    [SerializeField] float anomalyThreshold = 0.499913f;

    Worker worker;
    float[] means;
    float[] scales;

    [Serializable]
    class ScalerConfig
    {
        public string[] features;
        public float[] means;
        public float[] scales;
    }

    void Awake()
    {
        Instance = this;

        ScalerConfig config =
            JsonUtility.FromJson<ScalerConfig>(
                scalerParameters.text);

        means = config.means;
        scales = config.scales;

        Model model = ModelLoader.Load(onnxModel);
        worker = new Worker(model, BackendType.CPU);

        Debug.Log("Sentis anomaly model loaded.");
    }

    // Input: 30 samples x 5 features = 150 raw values.
    public float Predict(float[] samples)
    {
        if (samples == null || samples.Length != 150)
        {
            Debug.LogError("Expected 150 values.");
            return -1f;
        }

        float[] normalized = new float[150];

        for (int t = 0; t < 30; t++)
        {
            for (int f = 0; f < 5; f++)
            {
                int i = t * 5 + f;

                normalized[i] =
                    (samples[i] - means[f]) / scales[f];
            }
        }

        Debug.Log(
    $"Normalized sample: " +
    $"Speed={normalized[145]:F3}, " +
    $"Load={normalized[146]:F3}, " +
    $"Current={normalized[147]:F3}, " +
    $"Temp={normalized[148]:F3}, " +
    $"Vibration={normalized[149]:F3}");

        using (var input = new Tensor<float>(
            new TensorShape(1, 30, 5), normalized))
        {
            worker.Schedule(input);

            var output = worker.PeekOutput() as Tensor<float>;

            using var readableOutput = output.ReadbackAndClone();
            float[] reconstructed = readableOutput.DownloadToArray();

            if (reconstructed.Length != 150)
            {
                Debug.LogError(
                    "Unexpected model output size: " +
                    reconstructed.Length);
                return -1f;
            }

            float mse = 0f;

            for (int i = 0; i < 150; i++)
            {
                float difference =
                    normalized[i] - reconstructed[i];

                mse += difference * difference;
            }

            mse /= 150f;

            // Debug.Log(
            //     $"MSE: {mse:F6} | Anomaly: " +
            //     (mse > anomalyThreshold));

            return mse;
        }
    }

    public bool IsAnomaly(float mse)
    {
        return mse >= 0f && mse > anomalyThreshold;
    }

    void OnDestroy()
    {
        worker?.Dispose();

        if (Instance == this)
            Instance = null;
    }
}