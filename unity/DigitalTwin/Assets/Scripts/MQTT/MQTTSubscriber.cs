using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using UnityEngine;
using System.Collections.Concurrent;

public class MQTTSubscriber : MonoBehaviour
{
    [SerializeField] MQTTClient mqttClient;

    private const string StatusTopic = "factory/conveyor/01/status";
    private const string CommandedSpeedTopic = "factory/conveyor/01/commandedSpeed";
    private const string ScadaSnapShot = "factory/conveyor/01/scada_snapshot";

    [System.Serializable]
    public class MachineStatus
    {
        public bool machineRunning;
        public int boxCount;
    }

    public MachineStatus CurrentStatus{get;private set;}
    public ScadaSnapshot snapShot {get; private set;}
    public float CommandedConveyorSpeed;

    public Action<float> OnCommandedSpeedChange;
    private readonly ConcurrentQueue<ScadaSnapshot> snapshotQueue
    = new ConcurrentQueue<ScadaSnapshot>();
    public Action<ScadaSnapshot> OnScadaSnapshotReceived;

    private async void Start()
    {
        // Wait until MQTTClient has connected
        while (mqttClient.Client == null || !mqttClient.Client.IsConnected)
        {
            await Task.Delay(100);
        }

        // Register message handler
        mqttClient.Client.ApplicationMessageReceivedAsync += OnMessageReceived;

        // Subscribe to PLC status
        await mqttClient.Client.SubscribeAsync(
            new MqttTopicFilterBuilder()
                .WithTopic(StatusTopic)
                .Build()
        );

        Debug.Log("Subscribed to conveyor status.");

        await mqttClient.Client.SubscribeAsync(
            new MqttTopicFilterBuilder()
            .WithTopic(CommandedSpeedTopic)
            .Build());

        await mqttClient.Client.SubscribeAsync(
            new MqttTopicFilterBuilder()
            .WithTopic(ScadaSnapShot)
            .Build());
    }

    private Task OnMessageReceived(
        MqttApplicationMessageReceivedEventArgs e)
    {
        string topic = e.ApplicationMessage.Topic;
        string payload = Encoding.UTF8.GetString(
            e.ApplicationMessage.PayloadSegment
        );


        if(topic==StatusTopic)
        {
            CurrentStatus = JsonUtility.FromJson<MachineStatus>(payload);
            
        }
        else if(topic==CommandedSpeedTopic)
        {
            if (float.TryParse(
                    payload,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float speed))
            {
                CommandedConveyorSpeed = speed;
                Debug.Log("Subscriber received commanded speed");
                OnCommandedSpeedChange?.Invoke(CommandedConveyorSpeed);
            } 
        }
        else if (topic==ScadaSnapShot)
        {
            snapShot = JsonUtility.FromJson<ScadaSnapshot>(payload);
            Debug.Log("scada_snapshot!");
            if (snapShot != null)
            snapshotQueue.Enqueue(snapShot);
            
        }
        return Task.CompletedTask;
        
    }
    void Update()
    {
        while (snapshotQueue.TryDequeue(out ScadaSnapshot snapshot))
        {
            Debug.Log("scada_snapshot!");
            OnScadaSnapshotReceived?.Invoke(snapshot);
        }
    }

    private void OnDestroy()
    {
        if (mqttClient != null &&
            mqttClient.Client != null)
        {
            mqttClient.Client.ApplicationMessageReceivedAsync -= OnMessageReceived;
        }
    }

}
