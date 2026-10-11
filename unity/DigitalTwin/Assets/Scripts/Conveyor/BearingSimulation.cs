using UnityEngine;

public class BearingSimulation : MonoBehaviour
{
    [Header("Bearing Identification")]
    [SerializeField] private string bearingID = "BEARING-1";

    public string BearingID => bearingID;

    public string FullBearingID
    {
        get
        {
            RollerIdentity roller =
                GetComponentInParent<RollerIdentity>();

            if (roller == null)
                return bearingID;

            return $"{roller.RollerID}/{bearingID}";
        }
    }

    [Header("Initial Bearing Condition")]
    [Range(0f, 1f)]
    [SerializeField] private float initialHealth = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float initialLubrication = 1f;

    [Header("Illustrative Wear Rates")]
    [SerializeField] private float wearPerHour = 0.0005f;
    [SerializeField] private float lubricationLossPerHour = 0.002f;

    [Header("Model Settings")]
    [SerializeField] private float initialTemperatureC = 25f;

    private TemperatureModel temperatureModel;
    private VibrationModel vibrationModel;

    public float Health { get; private set; }
    public float Lubrication { get; private set; }
    public float TemperatureC { get; private set; }
    public float VibrationMmS { get; private set; }

    // Simplified relative resistance, from 0 to 1.
    public float Resistance { get; private set; }

    private bool initialized;

    private void Awake()
    {
        Health = initialHealth;
        Lubrication = initialLubrication;

        temperatureModel = new TemperatureModel(initialTemperatureC);
        vibrationModel = new VibrationModel();

        TemperatureC = initialTemperatureC;
        initialized = true;
    }

    public void SimulateStep(
        float ambientTemperatureC,
        float loadRatio,
        float normalizedSpeed,
        bool isRunning,
        float deltaTime)
    {
        if (!initialized)
            return;

        loadRatio = Mathf.Clamp01(loadRatio);
        normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

        // Update bearing condition using the temperature
        // from the previous sample.
        if (isRunning)
        {
            float temperatureFactor = 1f +
                Mathf.Max(0f, TemperatureC - 60f) / 40f;

            float lubricationFactor = 1f +
                3f * (1f - Lubrication);

            float wear = wearPerHour
                * (0.2f + loadRatio)
                * normalizedSpeed
                * lubricationFactor
                * temperatureFactor
                * deltaTime / 3600f;

            Health = Mathf.Clamp01(Health - wear);

            float lubricationLoss = lubricationLossPerHour
                * (0.2f + loadRatio)
                * normalizedSpeed
                * deltaTime / 3600f;

            Lubrication = Mathf.Clamp01(
                Lubrication - lubricationLoss);
        }

        // Calculate relative mechanical resistance.
        Resistance = Mathf.Clamp01(
            0.6f * (1f - Health) +
            0.4f * (1f - Lubrication));

        // Estimate bearing temperature.
        TemperatureC = temperatureModel.CalculateTemperature(
            ambientTemperatureC,
            loadRatio,
            isRunning,
            Resistance,
            deltaTime);

        // Estimate bearing vibration.
        VibrationMmS = vibrationModel.CalculateVibration(
            loadRatio,
            normalizedSpeed,
            isRunning,
            Health,
            Lubrication);
    }
}