
using UnityEngine;

public class TemperatureModel
{
    private float bearingTemperatureC;

    private readonly float baseTemperatureRiseC;
    private readonly float maxLoadTemperatureRiseC;
    private readonly float frictionTemperatureRiseC;
    private readonly float thermalTimeConstant;
    private readonly float noiseStandardDeviationC;

    public TemperatureModel(
        float initialTemperatureC,
        float baseTemperatureRiseC = 10f,
        float maxLoadTemperatureRiseC = 15f,
        float frictionTemperatureRiseC = 10f,
        float thermalTimeConstant = 300f,
        float noiseStandardDeviationC = 0.3f)
    {
        bearingTemperatureC = initialTemperatureC;
        this.baseTemperatureRiseC = baseTemperatureRiseC;
        this.maxLoadTemperatureRiseC = maxLoadTemperatureRiseC;
        this.frictionTemperatureRiseC = frictionTemperatureRiseC;
        this.thermalTimeConstant = thermalTimeConstant;
        this.noiseStandardDeviationC = noiseStandardDeviationC;
    }

    public float CalculateTemperature(
        float ambientTemperatureC,
        float loadRatio,
        bool isRunning,
        float resistance,
        float deltaTime)
    {
        float equilibriumTemperatureC = ambientTemperatureC;

        if (isRunning)
        {
            equilibriumTemperatureC += baseTemperatureRiseC
                + maxLoadTemperatureRiseC * loadRatio
                + frictionTemperatureRiseC * resistance;
        }

        float timeConstant = Mathf.Max(thermalTimeConstant, 0.01f);

        float response = 1f - Mathf.Exp(
            -Mathf.Max(deltaTime, 0f) / timeConstant);

        bearingTemperatureC +=
            (equilibriumTemperatureC - bearingTemperatureC) * response;

        float noise = RandomGaussian()
            * noiseStandardDeviationC
            * Mathf.Sqrt(Mathf.Max(deltaTime, 0f));

        bearingTemperatureC += noise;

        return bearingTemperatureC;
    }

    private float RandomGaussian()
    {
        float u1 = Mathf.Max(Random.value, 0.000001f);
        float u2 = Random.value;

        return Mathf.Sqrt(-2f * Mathf.Log(u1))
            * Mathf.Cos(2f * Mathf.PI * u2);
    }
}
