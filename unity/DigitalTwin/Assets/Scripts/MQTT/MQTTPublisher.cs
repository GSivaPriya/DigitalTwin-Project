
using UnityEngine;
using MQTTnet;
using System;
using System.Globalization;
using System.Threading.Tasks;

public class MQTTPublisher : MonoBehaviour
{
    [SerializeField] MQTTClient mqttClient;

    public async Task PublishCommands(string topic, object value)
    {
        if (mqttClient == null ||
            mqttClient.Client == null ||
            !mqttClient.Client.IsConnected)
        {
            Debug.LogWarning("MQTT client is not connected.");
            return;
        }

        string payload = value switch
        {
            bool b => b.ToString().ToLowerInvariant(),
            float f => f.ToString(CultureInfo.InvariantCulture),
            double d => d.ToString(CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build();

        await mqttClient.Client.PublishAsync(message);
    }
}
