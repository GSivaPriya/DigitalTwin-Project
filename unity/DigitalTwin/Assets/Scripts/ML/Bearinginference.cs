using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Bearinginference : MonoBehaviour
{
    private string bearingID;
    private int windowSize = 30;
    private float anomalyThreshold = 0.1042751f;
    MQTTSubscriber mqttSubscriber;
    [SerializeField] GameObject anomalyDetectedPanel;
    [SerializeField] TextMeshProUGUI anomalyDetectedText; 

    Queue<float[]> samples = new Queue<float[]>();



    void Start()
    {
        bearingID = GetComponent<BearingSimulation>().fullerID;
        mqttSubscriber = FindFirstObjectByType<MQTTSubscriber>();
        if (mqttSubscriber != null)
        {
            mqttSubscriber.OnScadaSnapshotReceived += ReceiveSnapShot;
        }
        anomalyDetectedPanel.SetActive(false);
    }
    void OnDestroy()
    {
        MQTTSubscriber mqttSubscriber = FindFirstObjectByType<MQTTSubscriber>();
        if (mqttSubscriber != null)
        {
            mqttSubscriber.OnScadaSnapshotReceived -= ReceiveSnapShot;
        }
    }

    public void ReceiveSnapShot(ScadaSnapshot snapshot)
    {
        Debug.Log("In Receive Snapshot");
        
        if(mqttSubscriber==null || mqttSubscriber.CurrentStatus==null)
        return;

        if(!mqttSubscriber.CurrentStatus.machineRunning)
        {
            samples.Clear();
            anomalyDetectedPanel.SetActive(false);
            return;
        }

        if(snapshot==null || snapshot.bearings==null || snapshot.conveyor==null)
        return;

        Bearing bearing = snapshot.bearings.GetBearingById(bearingID);

        if(bearing==null)
        return;

        float[] values = new float[]
        {
            snapshot.conveyor.commandedSpeed,
            snapshot.conveyor.conveyorLoad,
            snapshot.conveyor.motorCurrent,
            bearing.temperature,
            bearing.vibration
        };
        Debug.Log(
    $"Raw temperature: {bearing.temperature}, " +
    $"Raw vibration: {bearing.vibration}"
);

        Debug.Log("collecting samples");

        samples.Enqueue(values);

        if(samples.Count>windowSize)
        {
            samples.Dequeue();
        }

        if (samples.Count == windowSize)
        {
            float[] input = new float[windowSize * 5];
            int index = 0;

            foreach (float[] sample in samples)
            {
                foreach (float value in sample)
                {
                    input[index] = value;
                    index++;
                }
            }
           

            float mse = AnomalyModel.Instance.Predict(input);

            if (mse >= anomalyThreshold)
            {
                Debug.Log($"{bearingID} | Anomaly detected! MSE: {mse}");
                anomalyDetectedPanel.SetActive(true);
                anomalyDetectedText.text = $"Anomaly detected in {bearingID}!\nMSE: {mse}";
            }
            else
            {
                Debug.Log($"{bearingID} | No anomaly detected. MSE: {mse}");
                anomalyDetectedPanel.SetActive(false);
            }
                
            
        }


    }

    // Bearing FindBearing(Bearings bearings)
    // {
    //     Bearing[] all = new Bearing[]
    //     {
    //         bearings.roller_1_bearing_1,
    //         bearings.roller_1_bearing_2,
    //         bearings.roller_2_bearing_1,
    //         bearings.roller_2_bearing_2,
    //     };

    //     foreach (Bearing b in all)
    //     {
    //         if(b.bearingId==bearingID)
    //         {
    //             return b;
    //         }
    //     }
    //     return null;
    // }
}
