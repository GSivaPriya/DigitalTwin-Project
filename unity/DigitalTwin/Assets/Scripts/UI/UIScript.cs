using UnityEngine;
using TMPro;

public class UIScript : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI motorRunning;
    [SerializeField] TextMeshProUGUI boxCount;
    [SerializeField] MQTTSubscriber mqttSubscriber;


    // Update is called once per frame
    void Update()
    {
        if(mqttSubscriber.CurrentStatus!=null)
        {
            motorRunning.text="Motor Running: "+mqttSubscriber.CurrentStatus.machineRunning.ToString();
            boxCount.text = "Box Count: "+mqttSubscriber.CurrentStatus.boxCount.ToString();
        }
    }
}
