using UnityEngine;

/// <summary>First-order thermal response with independent measurement noise.</summary>
public class TemperatureModel
{
    private float internalTemperatureC;
    private readonly float baseTemperatureRiseC;
    private readonly float maxLoadTemperatureRiseC;
    private readonly float frictionTemperatureRiseC;
    private readonly float thermalTimeConstant;
    private readonly float noiseStandardDeviationC;

    public TemperatureModel(float initialTemperatureC, float baseTemperatureRiseC = 10f,
        float maxLoadTemperatureRiseC = 15f, float frictionTemperatureRiseC = 10f,
        float thermalTimeConstant = 300f, float noiseStandardDeviationC = 0.3f)
    {
        internalTemperatureC = initialTemperatureC;
        this.baseTemperatureRiseC = baseTemperatureRiseC;
        this.maxLoadTemperatureRiseC = maxLoadTemperatureRiseC;
        this.frictionTemperatureRiseC = frictionTemperatureRiseC;
        this.thermalTimeConstant = thermalTimeConstant;
        this.noiseStandardDeviationC = noiseStandardDeviationC;
    }

    public float CalculateTemperature(float ambientTemperatureC, float loadRatio,
        bool isRunning, float resistance, float deltaTime)
    {
        float equilibrium = ambientTemperatureC;
        if (isRunning)
            equilibrium += baseTemperatureRiseC +
                maxLoadTemperatureRiseC * Mathf.Clamp01(loadRatio) +
                frictionTemperatureRiseC * Mathf.Clamp01(resistance);

        float tau = Mathf.Max(thermalTimeConstant, 0.01f);
        float response = 1f - Mathf.Exp(-Mathf.Max(deltaTime, 0f) / tau);
        internalTemperatureC += (equilibrium - internalTemperatureC) * response;

        // Independent sensor noise avoids the random-walk drift in the old model.
        return internalTemperatureC + RandomGaussian() * noiseStandardDeviationC;
    }

    private float RandomGaussian()
    {
        float u1 = Mathf.Max(Random.value, 0.000001f);
        float u2 = Random.value;
        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
    }
}
