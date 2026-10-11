
using UnityEngine;

public class VibrationModel
{
    private readonly float baseVibration;
    private readonly float maxLoadContribution;
    private readonly float maxSpeedContribution;
    private readonly float degradationContribution;
    private readonly float lubricationContribution;
    private readonly float lognormalSpread;

    public VibrationModel(
        float baseVibration = 0.8f,
        float maxLoadContribution = 0.4f,
        float maxSpeedContribution = 0.3f,
        float degradationContribution = 2f,
        float lubricationContribution = 1f,
        float lognormalSpread = 0.15f)
    {
        this.baseVibration = baseVibration;
        this.maxLoadContribution = maxLoadContribution;
        this.maxSpeedContribution = maxSpeedContribution;
        this.degradationContribution = degradationContribution;
        this.lubricationContribution = lubricationContribution;
        this.lognormalSpread = lognormalSpread;
    }

    public float CalculateVibration(
        float loadRatio,
        float normalizedSpeed,
        bool isRunning,
        float health,
        float lubrication)
    {
        if (!isRunning)
            return 0f;

        loadRatio = Mathf.Clamp01(loadRatio);
        normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

        float meanVibration = baseVibration
            + maxLoadContribution * loadRatio
            + maxSpeedContribution * normalizedSpeed
            + degradationContribution * (1f - health)
            + lubricationContribution * (1f - lubrication);

        float u1 = Mathf.Max(Random.value, 0.000001f);
        float u2 = Random.value;

        float z = Mathf.Sqrt(-2f * Mathf.Log(u1))
            * Mathf.Cos(2f * Mathf.PI * u2);

        float sigma = lognormalSpread;

        return Mathf.Exp(
            Mathf.Log(meanVibration)
            - sigma * sigma / 2f
            + sigma * z);
    }
}
