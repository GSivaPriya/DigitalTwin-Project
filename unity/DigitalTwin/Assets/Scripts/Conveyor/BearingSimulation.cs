using MQTTnet;
using UnityEngine;

/// <summary>
/// Simulates observable bearing temperature and vibration. The hidden condition
/// and lubrication variables only influence the sensor models; they are not
/// sensor readings, PLC inputs, or intended ML features. Wear parameters are
/// illustrative and are not a validated bearing-life model.
/// </summary>
public class BearingSimulation : MonoBehaviour
{
    [Header("Identification")]
    [SerializeField] private string bearingID = "BEARING-1";
    RollerIdentity rollerID;
    public string BearingID => bearingID;

    public void FullBearingID()
    {
        rollerID = GetComponentInParent<RollerIdentity>();
    }

    [Header("Hidden internal state")]
    [Range(0f, 1f)] [SerializeField] private float initialHiddenCondition = 1f;
    [Range(0f, 1f)] [SerializeField] private float initialLubricationState = 1f;
    [Tooltip("Illustrative simulated degradation per operating hour; not a real wear estimate.")]
    [Min(0f)] [SerializeField] private float hiddenDegradationPerHour = 0.0005f;
    [Min(0f)] [SerializeField] private float hiddenLubricationLossPerHour = 0.002f;

    [Header("Temperature")]
    [SerializeField] private float initialTemperatureC = 25f;

    private TemperatureModel temperatureModel;
    private VibrationModel vibrationModel;
    private float hiddenCondition;
    private float hiddenLubrication;
    private float hiddenRelativeResistance;

    public float TemperatureC { get; private set; }
    public float VibrationMmS { get; private set; }

    private MQTTPublisher mqttPublisher;

    private void Awake()
    {
        hiddenCondition = initialHiddenCondition;
        hiddenLubrication = initialLubricationState;
        temperatureModel = new TemperatureModel(initialTemperatureC);
        vibrationModel = new VibrationModel();
        TemperatureC = initialTemperatureC;

        FullBearingID();
    }
    void Start()
    {
        mqttPublisher = GameObject.FindFirstObjectByType<MQTTPublisher>();
    }

    public void SimulateStep(float ambientTemperatureC, float loadRatio,
        float normalizedSpeed, bool isRunning, float deltaTime)
    {
        loadRatio = Mathf.Clamp01(loadRatio);
        normalizedSpeed = Mathf.Clamp01(normalizedSpeed);
        deltaTime = Mathf.Max(0f, deltaTime);

        if (isRunning)
        {
            float temperatureFactor = 1f + Mathf.Max(0f, TemperatureC - 60f) / 40f;
            float lubricationFactor = 1f + 3f * (1f - hiddenLubrication);
            float degradation = hiddenDegradationPerHour * (0.2f + loadRatio) *
                normalizedSpeed * lubricationFactor * temperatureFactor * deltaTime / 3600f;
            hiddenCondition = Mathf.Clamp01(hiddenCondition - degradation);

            float lubricationLoss = hiddenLubricationLossPerHour *
                (0.2f + loadRatio) * normalizedSpeed * deltaTime / 3600f;
            hiddenLubrication = Mathf.Clamp01(hiddenLubrication - lubricationLoss);
        }

        hiddenRelativeResistance = Mathf.Clamp01(
            0.6f * (1f - hiddenCondition) + 0.4f * (1f - hiddenLubrication));

        TemperatureC = temperatureModel.CalculateTemperature(
            ambientTemperatureC, loadRatio, isRunning, hiddenRelativeResistance, deltaTime);

        VibrationMmS = vibrationModel.CalculateVibration(
            loadRatio, normalizedSpeed, isRunning, hiddenCondition, hiddenLubrication);

        PublishTelemetry();
    }

    void PublishTelemetry()
    {
        _= mqttPublisher.PublishCommands($"factory/conveyor/01/telemetry/{rollerID.RollerID}/{bearingID}/temperature",TemperatureC);
        _= mqttPublisher.PublishCommands($"factory/conveyor/01/telemetry/{rollerID.RollerID}/{bearingID}/vibration",VibrationMmS);
    }

    // Internal model coupling only; do not publish as a sensor or ML feature.
    public float GetHiddenRelativeResistanceForCurrentModel() => hiddenRelativeResistance;
}
