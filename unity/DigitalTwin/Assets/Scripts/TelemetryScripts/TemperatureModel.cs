using UnityEngine;

public class TemperatureModel
{
    private float bearingTemperaturC;
    private readonly float baseTemperatureRiseC;
    private readonly float maxLoadTemperatureRiseC;
    private readonly float thermalTimeConstant;
    private readonly float noiseStandardDeviationC;
    public TemperatureModel(
        float initialTemperatureC,
        float baseTemperatureRiseC=10f,
        float maxLoadTemperatureRiseC = 15f,
        float thermalTimeConstant=300f,
        float noiseStandardDeviationC=0.3f)
    {
        bearingTemperaturC= initialTemperatureC;
        this.baseTemperatureRiseC = baseTemperatureRiseC;
        this.maxLoadTemperatureRiseC = maxLoadTemperatureRiseC;
        this. thermalTimeConstant = thermalTimeConstant;
        this.noiseStandardDeviationC = noiseStandardDeviationC;

    }

    public float CalculateTemperature (
        float ambientTemperatureC,
        float conveyorLoadKg,
        float maxLoadKg,
        bool isRunning,
        float deltaTime
    )
    {
        float loadRatio = maxLoadKg >0f ? Mathf.Clamp01(conveyorLoadKg/maxLoadKg) : 0f;
        float equilibriumTemperatureC = ambientTemperatureC;

        if(isRunning)
        {
            equilibriumTemperatureC += baseTemperatureRiseC + maxLoadTemperatureRiseC * loadRatio;
        }
        float timeConstant = Mathf.Max(thermalTimeConstant,0.01f);

        //Gradual heating or cooling toward equilibrium
        float response = 1f - Mathf.Exp(-Mathf.Max(deltaTime,0f)/timeConstant);

        bearingTemperaturC += (equilibriumTemperatureC - bearingTemperaturC) *response;

        //small measurement noise, scaled by the square root of elapsed time.

        float noise = RandomGaussian()*noiseStandardDeviationC*Mathf.Sqrt(Mathf.Max(deltaTime,0f));

        bearingTemperaturC += noise;
        return bearingTemperaturC;

    }

    private float RandomGaussian()
    {
        float u1 = Mathf.Max(Random.value, 0.000001f);
        float u2 = Random.value;

        return Mathf.Sqrt(-2f*Mathf.Log(u1))*Mathf.Cos(2f*Mathf.PI*u2);
    }
}
