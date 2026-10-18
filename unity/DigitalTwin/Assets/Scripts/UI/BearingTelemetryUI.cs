using UnityEngine;
using TMPro;

public class BearingTelemetryUI : MonoBehaviour
{
    MQTTSubscriber mqttSubscriber;
    Bearings bearings;
    Bearing bearing;
    
    string bearingID;

    [SerializeField] BearingSimulation bsimulation;
    [SerializeField] TextMeshProUGUI temperatureText;
    [SerializeField] TextMeshProUGUI VibrationText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
void Start()
{
    mqttSubscriber = FindFirstObjectByType<MQTTSubscriber>();

    bsimulation = GetComponent<BearingSimulation>();

    if (mqttSubscriber == null)
    {
        Debug.LogError("MQTTSubscriber not found.", this);
        return;
    }

    if (bsimulation == null)
    {
        Debug.LogError("BearingSimulation not found on this GameObject.", this);
        return;
    }

    bearingID = bsimulation.fullerID;
    Debug.Log("Bearing ID: " + bearingID, this);
}


    private Bearing GetBearing(Bearings bearings)
    {
        switch(bearingID)
        {
            case "ROLLER-1BEARING-1":
            return bearings.roller_1_bearing_1;
            
            case "ROLLER-1BEARING-2":
            return bearings.roller_1_bearing_2;

            case "ROLLER-2BEARING-1":
            return bearings.roller_2_bearing_1;

            case "ROLLER-2BEARING-2":
            return bearings.roller_2_bearing_2;

            default:
            return null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateBearingTelemetry();
    }

    
void UpdateBearingTelemetry()
{
    if (mqttSubscriber == null || mqttSubscriber.snapShot == null)
        return;

    bearings = mqttSubscriber.snapShot.bearings;

    if (bearings == null)
        return;

    bearing = GetBearing(bearings);

    if (bearing == null)
        return;

    if (temperatureText == null || VibrationText == null)
    {
        Debug.LogError("Temperature or vibration text is not assigned.", this);
        return;
    }

    temperatureText.text = $"Bearing Temperature: {bearing.temperature} °C";
    VibrationText.text = $"Bearing Vibration: {bearing.vibration} mm/s";
}

}
