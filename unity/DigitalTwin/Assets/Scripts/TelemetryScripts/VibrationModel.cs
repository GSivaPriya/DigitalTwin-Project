using UnityEngine;

public class VibrationModel
{
    private readonly float baseVibration;
    private readonly float maxLoadContribution;
    private readonly float maxSpeedContribution;
    private readonly float lognormalSpread;

    public VibrationModel(
        float baseVibration = 0.8f,
        float maxLoadContribution = 0.4f,
        float maxSpeedContribution = 0.3f,
        float lognormalSpread = 0.15f)
    {
        this.baseVibration = baseVibration;
        this.maxLoadContribution = maxLoadContribution;
        this.maxSpeedContribution = maxSpeedContribution;
        this.lognormalSpread = lognormalSpread;
    }

    public float CalculateVibration(
        float conveyorLoadKg,
        float maxLoadKg,
        float normalizedSpeed,
        bool isRunning)
    {
        if (!isRunning)
            return 0f;

        float loadRatio = maxLoadKg > 0f
            ? Mathf.Clamp01(conveyorLoadKg / maxLoadKg)
            : 0f;

        float speedRatio = Mathf.Clamp01(normalizedSpeed);

        // Expected vibration level in mm/s RMS.
        float meanVibration = baseVibration
            + maxLoadContribution * loadRatio
            + maxSpeedContribution * speedRatio;

        // Generate a standard normal random value.
        float u1 = Mathf.Max(Random.value, 0.000001f);
        float u2 = Random.value;

        float z = Mathf.Sqrt(-2f * Mathf.Log(u1))
            * Mathf.Cos(2f * Mathf.PI * u2);

        // Lognormal sample with the specified expected mean.
        float sigma = lognormalSpread;

        float vibration = Mathf.Exp(
            Mathf.Log(meanVibration)
            - (sigma * sigma / 2f)
            + sigma * z
        );

        return vibration;
    }
}
