using UnityEngine;
using MQTTnet;
using System.Threading.Tasks;
using MQTTnet.Client;

public class MQTTPublisher : MonoBehaviour
{
    [SerializeField] MQTTClient mqttClient;


    public async Task PublishCommands(string topic, bool value)
    {
        
        if (mqttClient == null || !mqttClient.Client.IsConnected)
            {
                Debug.LogWarning("MQTT client is not connected.");
                return;
            }
        
        
        var message = new MqttApplicationMessageBuilder()
        .WithTopic(topic)
        .WithPayload(value.ToString().ToLower())
        .Build();

        await mqttClient.Client.PublishAsync(message);
    }
}
