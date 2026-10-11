
using System.Collections;
using UnityEngine;

public class ConveyorTelemetryGenerator : MonoBehaviour
{
    private ConveyorBeltPhysics conveyor;
    private MQTTSubscriber mqttSubscriber;
    private CurrentModel currentModel;

    private BearingSimulation[] bearings;
    [SerializeField] GameObject roller1;
    [SerializeField] GameObject roller2;

    [Header("Conveyor Settings")]
    [SerializeField] private string conveyorID = "CONV_01";
    [SerializeField] private float ambientTemperature = 25f;
    [SerializeField] private float samplingRate = 1f;
    [SerializeField] private float conveyorMaxLoad = 80f;

    private float conveyorLoad;
    private float normalizedSpeed;
    private bool isConveyorRunning;
    private float motorCurrent;

    private void Start()
    {
        conveyor = GetComponent<ConveyorBeltPhysics>();
        mqttSubscriber = FindFirstObjectByType<MQTTSubscriber>();

        // Find all four BearingSimulation components under this conveyor.
        //bearings = GetComponentsInChildren<BearingSimulation>(true);
        //bearings = GameObject.FindObjectsByType<BearingSimulation>(FindObjectsSortMode.None);

        var bearingList = new System.Collections.Generic.List<BearingSimulation>();

        if (roller1 != null)
            bearingList.AddRange(
            roller1.GetComponentsInChildren<BearingSimulation>(true));

        if (roller2 != null)
            bearingList.AddRange(
            roller2.GetComponentsInChildren<BearingSimulation>(true));

            bearings = bearingList.ToArray();

            Debug.Log($"Found {bearings.Length} bearing simulations.");


        currentModel = new CurrentModel();

        if (conveyor == null)
            Debug.LogError("ConveyorBeltPhysics is missing.", this);

        if (bearings.Length != 4)
        {
            Debug.LogWarning(
                $"Expected 4 bearing simulations, found {bearings.Length}.",
                this);
        }

        StartCoroutine(GenerateTelemetry());
    }

    private IEnumerator GenerateTelemetry()
    {
        while (true)
        {
            yield return new WaitForSeconds(
                Mathf.Max(samplingRate, 0.01f));

            if (conveyor == null)
                continue;

            // Refresh operating conditions at each sample.
            conveyorLoad = conveyor.ConveyorLoad;

            isConveyorRunning =
                mqttSubscriber != null
                && mqttSubscriber.CurrentStatus != null
                && mqttSubscriber.CurrentStatus.machineRunning;

            normalizedSpeed = conveyor.ConveyorMaxSpeed > 0f
                ? Mathf.Clamp01(
                    Mathf.Abs(conveyor.ConveyorSpeed)
                    / conveyor.ConveyorMaxSpeed)
                : 0f;

            float loadRatio = conveyorMaxLoad > 0f
                ? Mathf.Clamp01(conveyorLoad / conveyorMaxLoad)
                : 0f;

            float totalResistance = 0f;
            int activeBearingCount = 0;

            foreach (BearingSimulation bearing in bearings)
            {
                if (bearing == null)
                    continue;

                bearing.SimulateStep(
                    ambientTemperature,
                    loadRatio,
                    normalizedSpeed,
                    isConveyorRunning,
                    samplingRate);

                totalResistance += bearing.Resistance;
                activeBearingCount++;
            }

            float averageResistance = activeBearingCount > 0
                ? totalResistance / activeBearingCount
                : 0f;

            motorCurrent = currentModel.CalculateCurrent(
                conveyorLoad,
                conveyorMaxLoad,
                isConveyorRunning,
                averageResistance);

            Debug.Log(
                $"Conveyor: {conveyorID}, " +
                $"Load: {conveyorLoad:F2} kg, " +
                $"Running: {isConveyorRunning}, " +
                $"Motor current: {motorCurrent:F2} A");

            foreach (BearingSimulation bearing in bearings)
            {
                if (bearing == null)
                    continue;

                Debug.Log(
                    $"{bearing.name} | " +
                    $"Health: {bearing.Health:P1} | " +
                    $"Lubrication: {bearing.Lubrication:P1} | " +
                    $"Temp: {bearing.TemperatureC:F2} °C | " +
                    $"Vibration: {bearing.VibrationMmS:F2} mm/s | " +
                    $"Resistance: {bearing.Resistance:F3}");
            }
        }
    }
}
