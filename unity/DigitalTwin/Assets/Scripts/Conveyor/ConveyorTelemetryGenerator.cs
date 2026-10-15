using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorTelemetryGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject roller1;
    [SerializeField] private GameObject roller2;

    [Header("Conveyor settings")]
    [SerializeField] private string conveyorID = "CONV_01";
    [SerializeField] private float ambientTemperature = 25f;
    [Min(0.1f)] [SerializeField] private float samplingRate = 1f;
    [Min(1f)] [SerializeField] private float conveyorMaxLoad = 125f;

    private ConveyorBeltPhysics conveyor;
    private MQTTSubscriber mqttSubscriber;
    private MQTTPublisher mqttPublisher;
    private CurrentModel currentModel;
    //private BearingTelemetryData telemetryData;
    private BearingSimulation[] bearings;

    private float conveyorLoad;
    private float normalizedSpeed;
    private bool isConveyorRunning;
    private float motorCurrent;
    private int sampleCount;

    public int SampleCount => sampleCount;
    public float SamplingRate => samplingRate;
    public float ConveyorLoadKg => conveyorLoad;
    public float MotorCurrentA => motorCurrent;

    private void Start()
    {
        conveyor = GetComponent<ConveyorBeltPhysics>();
        mqttSubscriber = FindFirstObjectByType<MQTTSubscriber>();
        mqttPublisher = FindFirstObjectByType<MQTTPublisher>();

        var list = new List<BearingSimulation>();
        if (roller1 != null) list.AddRange(roller1.GetComponentsInChildren<BearingSimulation>(true));
        if (roller2 != null) list.AddRange(roller2.GetComponentsInChildren<BearingSimulation>(true));
        bearings = list.ToArray();

        currentModel = new CurrentModel();
        // telemetryData = new BearingTelemetryData
        // {
        //     conveyorId = conveyorID
        // };
        

        if (conveyor == null)
            Debug.LogError("ConveyorTelemetryGenerator requires ConveyorBeltPhysics on the same GameObject.", this);
        if (bearings.Length != 4)
            Debug.LogWarning($"Expected four BearingSimulation components, found {bearings.Length}. Check roller references.", this);

        StartCoroutine(GenerateTelemetry());
    }

    public void SetSamplingInterval(float seconds) => samplingRate = Mathf.Max(0.1f, seconds);
    public void SetMaximumLoad(float kg) => conveyorMaxLoad = Mathf.Max(1f, kg);

    private IEnumerator GenerateTelemetry()
    {
        while (true)
        {
            yield return new WaitForSeconds(Mathf.Max(samplingRate, 0.1f));
            if (conveyor == null) continue;

            conveyorLoad = conveyor.ConveyorLoad;
            isConveyorRunning = mqttSubscriber != null &&
                mqttSubscriber.CurrentStatus != null &&
                mqttSubscriber.CurrentStatus.machineRunning;

            normalizedSpeed = conveyor.ConveyorMaxSpeed > 0f
                ? Mathf.Clamp01(Mathf.Abs(conveyor.ConveyorSpeed) / conveyor.ConveyorMaxSpeed)
                : 0f;

            float loadRatio = Mathf.Clamp01(conveyorLoad / Mathf.Max(1f, conveyorMaxLoad));
            foreach (BearingSimulation bearing in bearings)
            {
                if (bearing == null) continue;
                bearing.SimulateStep(ambientTemperature, loadRatio, normalizedSpeed,
                                     isConveyorRunning, samplingRate);
            }

            motorCurrent = currentModel.CalculateCurrent(
                conveyorLoad, conveyorMaxLoad, normalizedSpeed, isConveyorRunning);

            PublishTelemetry();

            sampleCount++;

        }
    }

    void PublishTelemetry()
    {
        _ = mqttPublisher.PublishCommands("factory/conveyor/01/telemetry/speed",conveyor.ConveyorSpeed);
        _ = mqttPublisher.PublishCommands("factory/conveyor/01/telemetry/load",conveyorLoad);
        _ = mqttPublisher.PublishCommands("factory/conveyor/01/telemetry/motorCurrent",motorCurrent);
    }
}
