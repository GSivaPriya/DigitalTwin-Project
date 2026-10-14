using UnityEngine;

/// <summary>
/// Simplified simulated motor-current relationship.
/// The model depends on operating load and speed, not the hidden bearing state.
/// Values are illustrative and require calibration for a particular motor/conveyor.
/// </summary>
public class CurrentModel
{
    private readonly float baselineCurrentA;
    private readonly float ratedCurrentA;
    private readonly float speedContributionA;
    private readonly float noiseStandardDeviationA;

    public CurrentModel(float baselineCurrentA = 0.5f, float ratedCurrentA = 2f,
        float speedContributionA = 0.3f, float noiseStandardDeviationA = 0.05f)
    {
        this.baselineCurrentA = baselineCurrentA;
        this.ratedCurrentA = ratedCurrentA;
        this.speedContributionA = speedContributionA;
        this.noiseStandardDeviationA = noiseStandardDeviationA;
    }

    public float CalculateCurrent(float conveyorLoad, float conveyorMaxLoad,
        float normalizedSpeed, bool isRunning)
    {
        if (!isRunning) return 0f;

        float loadRatio = conveyorMaxLoad > 0f
            ? Mathf.Clamp01(conveyorLoad / conveyorMaxLoad) : 0f;
        normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

        float mean = baselineCurrentA
            + (ratedCurrentA - baselineCurrentA) * loadRatio
            + speedContributionA * normalizedSpeed;

        float u1 = Mathf.Max(Random.value, 0.000001f);
        float u2 = Random.value;
        float z = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
        return Mathf.Max(0f, mean + z * noiseStandardDeviationA);
    }
}
