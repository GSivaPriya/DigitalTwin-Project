
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class SimulationMasterCheck : MonoBehaviour
{
    [Serializable]
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

    [Header("MQTT Topics")]
    [SerializeField] private string startTopic =
        "factory/conveyor/01/commands/start";

    [SerializeField] private string stopTopic =
        "factory/conveyor/01/commands/stop";

    [Header("Sampling")]
    [Min(0.1f)]
    [SerializeField] private float samplingIntervalSeconds = 0.25f;

    [Min(0)]
    [SerializeField] private int targetSamples;

    [SerializeField] private bool stopWhenTargetReached = true;

    [Min(0f)]
    [SerializeField] private float commandSettleSeconds = 2f;

    [SerializeField] private bool startAutomatically;

    [Header("Normal Scenarios")]
    [SerializeField] private Scenario[] scenarios =
    {
        // 1. Light load - nominal speed
    new Scenario
    {
        scenarioName = "Light load - nominal speed",
        minimumBoxMassKg = 5.5f,
        maximumBoxMassKg = 6f,
        spawnIntervalSeconds = 3f,
        conveyorSpeed = 0.45f,
        runningDurationSeconds = 3240f,
        drainTimeoutSeconds = 60f,
        coolingDurationSeconds = 300f
    },

    // 2. Medium load - nominal speed
    new Scenario
    {
        scenarioName = "Medium load - nominal speed",
        minimumBoxMassKg = 8.5f,
        maximumBoxMassKg = 9.5f,
        spawnIntervalSeconds = 2.5f,
        conveyorSpeed = 0.45f,
        runningDurationSeconds = 3240f,
        drainTimeoutSeconds = 60f,
        coolingDurationSeconds = 300f
    },

    // 3. Heavy load - nominal speed
    new Scenario
    {
        scenarioName = "Heavy load - nominal speed",
        minimumBoxMassKg = 12f,
        maximumBoxMassKg = 13f,
        spawnIntervalSeconds = 2f,
        conveyorSpeed = 0.45f,
        runningDurationSeconds = 3240f,
        drainTimeoutSeconds = 60f,
        coolingDurationSeconds = 300f
    },

    // 4. Medium load - higher speed
    new Scenario
    {
        scenarioName = "Medium load - higher speed",
        minimumBoxMassKg = 8.5f,
        maximumBoxMassKg = 9.5f,
        spawnIntervalSeconds = 2.5f,
        conveyorSpeed = 0.75f,
        runningDurationSeconds = 3240f,
        drainTimeoutSeconds = 60f,
        coolingDurationSeconds = 300f
    },

    // 5. Light load - higher throughput
    new Scenario
    {
        scenarioName = "Light load - higher throughput",
        minimumBoxMassKg = 5.5f,
        maximumBoxMassKg = 6f,
        spawnIntervalSeconds = 2f,
        conveyorSpeed = 0.45f,
        runningDurationSeconds = 3240f,
        drainTimeoutSeconds = 60f,
        coolingDurationSeconds = 300f
    },

    // 6. Medium load - slower throughput
    new Scenario
    {
        scenarioName = "Medium load - slower throughput",
        minimumBoxMassKg = 8.5f,
        maximumBoxMassKg = 9.5f,
        spawnIntervalSeconds = 3.5f,
        conveyorSpeed = 0.45f,
        runningDurationSeconds = 3240f,
        drainTimeoutSeconds = 60f,
        coolingDurationSeconds = 300f
    },

    // 7. Heavy load - higher speed
    new Scenario
    {
        scenarioName = "Heavy load - higher speed",
        minimumBoxMassKg = 12f,
        maximumBoxMassKg = 13f,
        spawnIntervalSeconds = 2f,
        conveyorSpeed = 0.75f,
        runningDurationSeconds = 3240f,
        drainTimeoutSeconds = 60f,
        coolingDurationSeconds = 300f
    }
    };

    private Coroutine sequence;
    private bool sequenceRunning;
    private int initialSampleCount;

    public bool IsRunning => sequenceRunning;

    private void Start()
    {
        // if (telemetry != null)
        //     telemetry.SetSamplingInterval(samplingIntervalSeconds);

        if (startAutomatically)
            StartDataCollection();
    }

    public void StartDataCollection()
    {
        Debug.Log("START BUTTON: StartDataCollection() was called.", this);

        
        if (sequenceRunning)
        {
            Debug.LogWarning("SimulationMaster: sequence is already running.", this);
            return;
        }

        if (mqttClient == null ||
            publisher == null ||
            spawner == null ||
            conveyor == null ||
            telemetry == null ||
            scenarios == null ||
            scenarios.Length == 0)
        {
            Debug.LogError(
                "SimulationMaster: a required reference is missing. " +
                "Check MQTTClient, Publisher, Spawner, Conveyor, Telemetry and Scenarios.",
                this);
            return;
        }

        if (sequence != null)
            StopCoroutine(sequence);

        if (telemetry != null)
        telemetry.SetSamplingInterval(samplingIntervalSeconds);

        sequence = StartCoroutine(RunScenarios());
        Debug.Log($"sequence coroutine start {sequence}");
    }

    public void StopDataCollection()
    {
        sequenceRunning = false;

        if (sequence != null)
            StopCoroutine(sequence);

        sequence = null;

        if (spawner != null)
            spawner.StopSpawning();

        if (publisher != null)
            StartCoroutine(PublishMachineState(false));
    }

    private IEnumerator RunScenarios()
    {
        Debug.Log("SimulationMaster: waiting for MQTT connection.", this);

        while (mqttClient == null ||
               mqttClient.Client == null ||
               !mqttClient.Client.IsConnected)
        {
            yield return new WaitForSeconds(0.1f);
        }

        sequenceRunning = true;
        initialSampleCount = telemetry.SampleCount;

        Debug.Log("SimulationMaster: MQTT connected; starting scenarios.", this);

        foreach (Scenario scenario in scenarios)
        {
            if (!sequenceRunning)
                yield break;

            if (scenario == null)
                continue;

            Debug.Log(
                $"SimulationMaster: starting scenario '{scenario.scenarioName}'.",
                this);

            spawner.Configure(
                scenario.minimumBoxMassKg,
                scenario.maximumBoxMassKg,
                scenario.spawnIntervalSeconds);

            conveyor.SetSimulationSpeed(scenario.conveyorSpeed);

            // Publish the PLC command before spawning boxes.
            yield return PublishMachineState(true);

            if (!sequenceRunning)
                yield break;

            Debug.Log(
                $"SimulationMaster: start/stop publish tasks completed for '{scenario.scenarioName}'.",
                this);

            if (commandSettleSeconds > 0f)
                yield return new WaitForSeconds(commandSettleSeconds);

            spawner.StartSpawning();

            Debug.Log("SimulationMaster: box spawning started.", this);

            float elapsed = 0f;

            while (elapsed < scenario.runningDurationSeconds &&
                   sequenceRunning &&
                   !TargetReached())
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!sequenceRunning)
                yield break;

            spawner.StopSpawning();

            Debug.Log("SimulationMaster: stopped spawning; draining conveyor.", this);

            float drainElapsed = 0f;

            while (conveyor.ConveyorLoad > 0.1f &&
                   drainElapsed < scenario.drainTimeoutSeconds &&
                   sequenceRunning)
            {
                drainElapsed += Time.deltaTime;
                yield return null;
            }

            yield return PublishMachineState(false);

            if (!sequenceRunning)
                yield break;

            Debug.Log("SimulationMaster: stop command publish tasks completed.", this);

            if (commandSettleSeconds > 0f)
                yield return new WaitForSeconds(commandSettleSeconds);

            float coolingElapsed = 0f;

            while (coolingElapsed < scenario.coolingDurationSeconds &&
                   sequenceRunning &&
                   !TargetReached())
            {
                coolingElapsed += Time.deltaTime;
                yield return null;
            }

            if (TargetReached())
                break;
        }

        spawner.StopSpawning();
        yield return PublishMachineState(false);

        sequenceRunning = false;
        sequence = null;

        Debug.Log(
            $"SimulationMaster: finished. Unity samples collected: " +
            $"{telemetry.SampleCount - initialSampleCount}.",
            this);

        Application.Quit();
    }

    private bool TargetReached()
    {
        return stopWhenTargetReached &&
               targetSamples > 0 &&
               telemetry.SampleCount - initialSampleCount >= targetSamples;
    }

    private IEnumerator PublishMachineState(bool run)
    {
        if (publisher == null)
        {
            Debug.LogError("SimulationMaster: MQTTPublisher is not assigned.", this);
            sequenceRunning = false;
            yield break;
        }

        Debug.Log(
            $"SimulationMaster: publishing start={run} to '{startTopic}' " +
            $"and stop={!run} to '{stopTopic}'.",
            this);

        Task startTask = publisher.PublishCommands(startTopic, run);
        Task stopTask = publisher.PublishCommands(stopTopic, !run);

        while (!startTask.IsCompleted || !stopTask.IsCompleted)
            yield return null;

        if (startTask.IsFaulted || stopTask.IsFaulted ||
            startTask.IsCanceled || stopTask.IsCanceled)
        {
            Debug.LogError(
                $"SimulationMaster: MQTT command publish failed. " +
                $"Start status: {startTask.Status}, " +
                $"Stop status: {stopTask.Status}. " +
                $"Start error: {startTask.Exception}; " +
                $"Stop error: {stopTask.Exception}",
                this);

            sequenceRunning = false;
            yield break;
        }

        Debug.Log(
            $"SimulationMaster: MQTT publish calls completed. Requested run={run}.",
            this);
    }
}
