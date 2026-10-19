
using UnityEngine;
using TMPro;

public class TelemetryStatusUI : MonoBehaviour
{
    private MQTTSubscriber mqttSubscriber;

    [SerializeField] private TextMeshProUGUI isMotorRunning;
    [SerializeField] private TextMeshProUGUI conveyorLoad;
    [SerializeField] private TextMeshProUGUI conveyorSpeed;
    [SerializeField] private TextMeshProUGUI motorCurrent;
    [SerializeField] private TextMeshProUGUI roller1bearing1Temp;
    [SerializeField] private TextMeshProUGUI roller1bearing2Temp;
    [SerializeField] private TextMeshProUGUI roller2bearing1Temp;
    [SerializeField] private TextMeshProUGUI roller2bearing2Temp;
    [SerializeField] private TextMeshProUGUI roller1bearing1Vib;
    [SerializeField] private TextMeshProUGUI roller1bearing2Vib;
    [SerializeField] private TextMeshProUGUI roller2bearing1Vib;
    [SerializeField] private TextMeshProUGUI roller2bearing2Vib;

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
        var bearings = mqttSubscriber.snapShot.bearings;

        isMotorRunning.text = $"Motor Running: {conveyor.isMotorRunning}";
        conveyorLoad.text = $"Conveyor Load: {conveyor.conveyorLoad}";
        conveyorSpeed.text = $"Conveyor Speed: {conveyor.commandedSpeed}";
        motorCurrent.text = $"Motor Current: {conveyor.motorCurrent}";

        roller1bearing1Temp.text="TemperatureC: "+ bearings.roller_1_bearing_1.temperature.ToString("F2");
        roller1bearing2Temp.text=$"TemperatureC: "+bearings.roller_1_bearing_2.temperature.ToString("F2");
        roller2bearing1Temp.text=$"TemperatureC: "+bearings.roller_2_bearing_1.temperature.ToString("F2");
        roller2bearing2Temp.text=$"TemperatureC: "+bearings.roller_2_bearing_2.temperature.ToString("F2");

        roller1bearing1Vib.text=$"Vibration-mm/s: "+bearings.roller_1_bearing_1.vibration.ToString("F2");
        roller1bearing2Vib.text=$"Vibration-mm/s: "+bearings.roller_1_bearing_2.vibration.ToString("F2");
        roller2bearing1Vib.text=$"Vibration-mm/s: "+bearings.roller_2_bearing_1.vibration.ToString("F2");
        roller2bearing2Vib.text=$"Vibration-mm/s: "+bearings.roller_2_bearing_2.vibration.ToString("F2");

    }
}
