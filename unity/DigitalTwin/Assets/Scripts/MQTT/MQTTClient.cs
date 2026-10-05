using UnityEngine;
using MQTTnet;
using MQTTnet.Client;

public class MQTTClient : MonoBehaviour
{
    
    public IMqttClient Client {get; private set;}
    MqttClientOptions options;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   async void Start()
    {
        var factory = new MqttFactory();
        Client = factory.CreateMqttClient();
        options = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1",1883).Build();

        await Client.ConnectAsync(options);

    }

}
