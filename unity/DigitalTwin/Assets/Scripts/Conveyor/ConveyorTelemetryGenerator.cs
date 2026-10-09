using System.Collections;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class ConveyorTelemetryGenerator : MonoBehaviour
{
    ConveyorBeltPhysics conveyor;
    MQTTSubscriber mqttSubscriber;
    TemperatureModel tempModel;
    VibrationModel vibModel;
    CurrentModel currentModel;

    [SerializeField] string conveyorID;
    [SerializeField] private float ambientTemperature = 25f;
    [SerializeField] private readonly float initialTempC = 25f;
    [SerializeField] private float samplingRate = 1f;
    private float normalizedSpeed;
    private float conveyorLoad;
    private readonly float conveyorMaxLoad=80.0f;
    private bool isConveyorRunning;

    private float bearingTemperature;
    private float bearingVibration;
    private float motorCurrent;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        conveyor = GetComponent<ConveyorBeltPhysics>();

        mqttSubscriber = GameObject.FindFirstObjectByType<MQTTSubscriber>();

        tempModel = new TemperatureModel(initialTempC);
        vibModel = new VibrationModel();
        currentModel = new CurrentModel();

        StartCoroutine(GenerateTelemetry());
    }

    // Update is called once per frame
    void Update()
    {
        if(mqttSubscriber==null || mqttSubscriber.CurrentStatus==null)
        return;

        if(!mqttSubscriber.CurrentStatus.machineRunning)
        return;
            conveyorLoad = conveyor.ConveyorLoad;
            isConveyorRunning = mqttSubscriber.CurrentStatus.machineRunning;
            normalizedSpeed = Mathf.Clamp01(Mathf.Abs(conveyor.ConveyorSpeed)/conveyor.ConveyorMaxSpeed);   

    }

    IEnumerator GenerateTelemetry()
    {
        while(true)
        {
            yield return new WaitForSeconds(samplingRate);
        
            bearingTemperature = tempModel.CalculateTemperature(ambientTemperature,
            conveyorLoad,
            conveyorMaxLoad,
            isConveyorRunning,
            samplingRate);

            bearingVibration = vibModel.CalculateVibration(conveyorLoad,
            conveyorMaxLoad,
            normalizedSpeed,
            isConveyorRunning);

            motorCurrent = currentModel.CalculateCurrent(conveyorLoad,
            conveyorMaxLoad,
            isConveyorRunning);

            Debug.Log($"ConveyorID: {conveyorID}, ConveyorLoad: {conveyorLoad} Temp: {bearingTemperature}, vib: {bearingVibration}, current: {motorCurrent}");
        }
        
        
    }
}
