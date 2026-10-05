using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using UnityEngine;

public class MQTTSubscriber : MonoBehaviour
{
    [SerializeField] MQTTClient mqttClient;

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
                .WithTopic("factory/conveyor/01/status")
                .Build()
        );

        Debug.Log("Subscribed to conveyor status.");
    }

    private Task OnMessageReceived(
        MqttApplicationMessageReceivedEventArgs e)
    {
        string topic = e.ApplicationMessage.Topic;

        string payload = Encoding.UTF8.GetString(
            e.ApplicationMessage.PayloadSegment
        );

        Debug.Log($"MQTT Received: {topic} = {payload}");

        return Task.CompletedTask;
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
