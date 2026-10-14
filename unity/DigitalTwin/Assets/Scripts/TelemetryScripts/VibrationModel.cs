using UnityEngine;

/// <summary>Illustrative positive-valued vibration model, not a fault classifier.</summary>
public class VibrationModel
{
    private readonly float baseVibration;
    private readonly float maxLoadContribution;
    private readonly float maxSpeedContribution;
    private readonly float degradationContribution;
    private readonly float lubricationContribution;
    private readonly float lognormalSpread;

    public VibrationModel(float baseVibration = 0.8f, float maxLoadContribution = 0.4f,
        float maxSpeedContribution = 0.3f, float degradationContribution = 2f,
        float lubricationContribution = 1f, float lognormalSpread = 0.15f)
    {
        this.baseVibration = baseVibration;
        this.maxLoadContribution = maxLoadContribution;
        this.maxSpeedContribution = maxSpeedContribution;
        this.degradationContribution = degradationContribution;
        this.lubricationContribution = lubricationContribution;
        this.lognormalSpread = lognormalSpread;
    }

    public float CalculateVibration(float loadRatio, float normalizedSpeed,
        bool isRunning, float hiddenCondition, float hiddenLubrication)
    {
        if (!isRunning) return 0f;

        loadRatio = Mathf.Clamp01(loadRatio);
        normalizedSpeed = Mathf.Clamp01(normalizedSpeed);
        float mean = baseVibration + maxLoadContribution * loadRatio +
            maxSpeedContribution * normalizedSpeed +
            degradationContribution * (1f - Mathf.Clamp01(hiddenCondition)) +
            lubricationContribution * (1f - Mathf.Clamp01(hiddenLubrication));

        float u1 = Mathf.Max(Random.value, 0.000001f);
        float u2 = Random.value;
        float z = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
        float sigma = Mathf.Max(0f, lognormalSpread);
        return Mathf.Exp(Mathf.Log(Mathf.Max(mean, 0.0001f)) - sigma * sigma / 2f + sigma * z);
    }
}
