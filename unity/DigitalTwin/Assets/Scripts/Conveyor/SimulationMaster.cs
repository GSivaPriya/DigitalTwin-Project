using System.Collections;
using UnityEngine;

/// <summary>
/// Coordinates normal-operation data-generation scenarios.
/// Start/Stop are MQTT commands to the PLC-controlled machine; this does not
/// stop the OpenPLC runtime service. Keep OpenPLC running during cooling.
/// </summary>
public class SimulationMaster : MonoBehaviour
{
    [System.Serializable]
    public class Scenario
    {
        public string scenarioName = "Normal operation";
        [Min(0.1f)] public float minimumBoxMassKg = 4.8f;
        [Min(0.1f)] public float maximumBoxMassKg = 5.2f;
        [Min(0.1f)] public float spawnIntervalSeconds = 3f;
        [Min(0f)] public float conveyorSpeed = 0.45f;
        [Min(1f)] public float runningDurationSeconds = 180f;
        [Min(0f)] public float drainTimeoutSeconds = 30f;
        [Min(0f)] public float coolingDurationSeconds = 180f;
    }

    [Header("References")]
    [SerializeField] private MQTTClient mqttClient;
    [SerializeField] private MQTTPublisher publisher;
    [SerializeField] private ObjectSpawner spawner;
    [SerializeField] private ConveyorBeltPhysics conveyor;
    [SerializeField] private ConveyorTelemetryGenerator telemetry;

    [Header("MQTT topics - must match Node-RED")]
    [SerializeField] private string startTopic = "factory/conveyor/01/commands/start";
    [SerializeField] private string stopTopic = "factory/conveyor/01/commands/stop";

    [Header("Sampling")]
    [Min(0.1f)] [SerializeField] private float samplingIntervalSeconds = 1f;
    [Tooltip("0 disables this limit. This is a Unity sample count, not a verified historian row count.")]
    [Min(0)] [SerializeField] private int targetSamples;
    [SerializeField] private bool stopWhenTargetReached = true;
    [Min(0f)] [SerializeField] private float commandSettleSeconds = 2f;
    [SerializeField] private bool startAutomatically;

    [Header("Normal scenarios")]
    [SerializeField] private Scenario[] scenarios =
    {
        new Scenario { scenarioName = "Light load - nominal speed", minimumBoxMassKg = 5.5f, maximumBoxMassKg = 6f, spawnIntervalSeconds = 3f, conveyorSpeed = 0.45f, runningDurationSeconds = 180f, drainTimeoutSeconds = 30f, coolingDurationSeconds = 180f },
        new Scenario { scenarioName = "Medium load - nominal speed", minimumBoxMassKg = 8.5f, maximumBoxMassKg = 9.5f, spawnIntervalSeconds = 2.5f, conveyorSpeed = 0.45f, runningDurationSeconds = 180f, drainTimeoutSeconds = 30f, coolingDurationSeconds = 180f },
        new Scenario { scenarioName = "Heavy load - nominal speed", minimumBoxMassKg = 12f, maximumBoxMassKg = 13f, spawnIntervalSeconds = 2f, conveyorSpeed = 0.45f, runningDurationSeconds = 180f, drainTimeoutSeconds = 30f, coolingDurationSeconds = 180f },
        new Scenario { scenarioName = "Medium load - higher speed", minimumBoxMassKg = 8.5f, maximumBoxMassKg = 9.5f, spawnIntervalSeconds = 2.5f, conveyorSpeed = 0.75f, runningDurationSeconds = 180f, drainTimeoutSeconds = 30f, coolingDurationSeconds = 180f }
    };

    private Coroutine sequence;
    private bool sequenceRunning;
    private int initialSampleCount;

    public bool IsRunning => sequenceRunning;

    private void Start()
    {
        if (telemetry != null) telemetry.SetSamplingInterval(samplingIntervalSeconds);
        if (startAutomatically) StartDataCollection();
    }

    public void StartDataCollection()
    {
        if (sequenceRunning) return;
        if (publisher == null || spawner == null || conveyor == null || telemetry == null ||
            scenarios == null || scenarios.Length == 0)
        {
            Debug.LogError("SimulationMaster: assign Publisher, Spawner, Conveyor, Telemetry and at least one scenario.", this);
            return;
        }

        sequence = StartCoroutine(RunScenarios());
    }

    public void StopDataCollection()
    {
        sequenceRunning = false;
        if (sequence != null) StopCoroutine(sequence);
        sequence = null;
        if (spawner != null) spawner.StopSpawning();
        _ = PublishMachineState(false);
    }

    private IEnumerator RunScenarios()
    {
        while (mqttClient.Client == null || !mqttClient.Client.IsConnected)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        sequenceRunning = true;
        initialSampleCount = telemetry.SampleCount;
        Debug.Log("SimulationMaster: starting normal-operation data collection.");

        foreach (Scenario scenario in scenarios)
        {
            if (!sequenceRunning) yield break;
            if (scenario == null) continue;

            Debug.Log($"SimulationMaster: {scenario.scenarioName}");

            spawner.Configure(scenario.minimumBoxMassKg, scenario.maximumBoxMassKg,
                              scenario.spawnIntervalSeconds);
            conveyor.SetSimulationSpeed(scenario.conveyorSpeed);
            spawner.StartSpawning();

            yield return PublishMachineState(true);
            if (commandSettleSeconds > 0f) yield return new WaitForSeconds(commandSettleSeconds);

            float elapsed = 0f;
            while (elapsed < scenario.runningDurationSeconds && sequenceRunning && !TargetReached())
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            if (!sequenceRunning) yield break;

            // Stop new boxes, allow existing boxes to clear, then stop the machine.
            spawner.StopSpawning();
            float drainElapsed = 0f;
            while (conveyor.ConveyorLoad > 0.1f &&
                   drainElapsed < scenario.drainTimeoutSeconds && sequenceRunning)
            {
                yield return null;
                drainElapsed += Time.deltaTime;
            }

            yield return PublishMachineState(false);

            // Telemetry keeps sampling while the thermal model cools toward ambient.
            if (commandSettleSeconds > 0f) yield return new WaitForSeconds(commandSettleSeconds);
            float coolingElapsed = 0f;
            while (coolingElapsed < scenario.coolingDurationSeconds &&
                   sequenceRunning && !TargetReached())
            {
                yield return null;
                coolingElapsed += Time.deltaTime;
            }

            if (TargetReached()) break;
        }

        spawner.StopSpawning();
        yield return PublishMachineState(false);
        sequenceRunning = false;
        sequence = null;
        Debug.Log($"SimulationMaster: finished. Unity samples: {telemetry.SampleCount - initialSampleCount}. Check historian row count independently.");
    }

    private bool TargetReached()
    {
        return stopWhenTargetReached && targetSamples > 0 &&
               telemetry.SampleCount - initialSampleCount >= targetSamples;
    }

    private IEnumerator PublishMachineState(bool run)
    {
        if (publisher == null) yield break;
        var startTask = publisher.PublishCommands(startTopic, run);
        var stopTask = publisher.PublishCommands(stopTopic, !run);
        while (!startTask.IsCompleted || !stopTask.IsCompleted) yield return null;
        if (startTask.IsFaulted) Debug.LogError($"Start MQTT publish failed: {startTask.Exception}");
        if (stopTask.IsFaulted) Debug.LogError($"Stop MQTT publish failed: {stopTask.Exception}");
    }
}
