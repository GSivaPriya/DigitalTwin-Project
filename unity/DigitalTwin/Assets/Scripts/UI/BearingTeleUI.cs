using UnityEngine;
using TMPro;

public class BearingTeleUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI bearingIDText;
    [SerializeField] TextMeshProUGUI bearingTempText;
    [SerializeField] TextMeshProUGUI bearingVibrationText;
    public void GetBearingTelemetry(string bearingID, float temperature, float vibration)
    {
        bearingIDText.text= $"Bearing ID: {bearingID}";
        bearingTempText.text= "Temperature C: " + temperature.ToString("F2");
        bearingVibrationText.text="Vibration mm/s: "+ vibration.ToString("F2");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
