using UnityEngine;

public class CurrentModel
{
    private readonly float baselineCurrentA;
    private readonly float ratedCurrentA;
    private readonly float noiseStandardDeviationA;

    public CurrentModel(
        float baselineCurrentA = 0.5f,
        float ratedCurrentA = 2.0f,
        float noiseStandardDeviationA = 0.05f)
    {
        this.baselineCurrentA = baselineCurrentA;
        this.ratedCurrentA = ratedCurrentA;
        this.noiseStandardDeviationA = noiseStandardDeviationA;
    }

    public float CalculateCurrent(
        float conveyorLoad,
        float conveyorMaxLoad,
        bool isRunning)
    {
        if (!isRunning)
            return 0f;

        float loadRatio = conveyorMaxLoad > 0f
            ? Mathf.Clamp01(conveyorLoad / conveyorMaxLoad)
            : 0f;

        float meanCurrent = baselineCurrentA
            + (ratedCurrentA - baselineCurrentA) * loadRatio;

        float noise = Random.Range(-1f, 1f)
            * noiseStandardDeviationA * Mathf.Sqrt(3f);

        return Mathf.Max(0f, meanCurrent + noise);
    }
}
