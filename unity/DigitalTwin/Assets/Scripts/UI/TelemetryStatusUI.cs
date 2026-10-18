
using UnityEngine;
using TMPro;

public class TelemetryStatusUI : MonoBehaviour
{
    private MQTTSubscriber mqttSubscriber;

    [SerializeField] private TextMeshProUGUI isMotorRunning;
    [SerializeField] private TextMeshProUGUI conveyorLoad;
    [SerializeField] private TextMeshProUGUI conveyorSpeed;
    [SerializeField] private TextMeshProUGUI motorCurrent;

    private void Start()
    {
        mqttSubscriber = FindFirstObjectByType<MQTTSubscriber>();

        if (mqttSubscriber == null)
        {
            Debug.LogError("MQTTSubscriber not found.");
        }
    }

    private void Update()
    {
        UpdateConveyorTelemetry();
    }

    private void UpdateConveyorTelemetry()
    {
        if (mqttSubscriber == null || mqttSubscriber.snapShot == null)
            return;

        if (mqttSubscriber.snapShot.conveyor == null)
            return;

        var conveyor = mqttSubscriber.snapShot.conveyor;

        isMotorRunning.text = $"Motor Running: {conveyor.isMotorRunning}";
        conveyorLoad.text = $"Conveyor Load: {conveyor.conveyorLoad}";
        conveyorSpeed.text = $"Conveyor Speed: {conveyor.commandedSpeed}";
        motorCurrent.text = $"Motor Current: {conveyor.motorCurrent}";
    }
}
