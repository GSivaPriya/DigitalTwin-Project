using System.Threading.Tasks;
using UnityEngine;

public class ConveyorControllerCommands : MonoBehaviour
{
    bool startCommand;
    bool stopCommand;
    [SerializeField] float speedSetPoint=0.45f;

    MQTTPublisher mqttPublisher;

    void Start()
    {
        mqttPublisher = GameObject.FindFirstObjectByType<MQTTPublisher>();
    }
    
    public void OnStartClicked()
    {
        _ = HandleStart();
    }

    async Task HandleStart()
    {
        startCommand=!startCommand;
        if(startCommand)
        {
            stopCommand=false;
        }

        PublishStates();
    }

    public void OnStopClicked()
    {
        _=HandleStop();
    }

    async Task HandleStop()
    {
        stopCommand=!stopCommand;
        if(stopCommand)
        {
            startCommand=false;
        }
        PublishStates();
    }

    async Task OnSpeedChange(float speed)
    {
        speedSetPoint=speed;
        PublishStates();
    }

    void PublishStates()
    {
        _ = mqttPublisher.PublishCommands("factory/conveyor/01/commands/start", startCommand);
        _ = mqttPublisher.PublishCommands("factory/conveyor/01/commands/stop", stopCommand);
        _ = mqttPublisher.PublishCommands("factory/conveyor/01/commands/speed-setpoint",speedSetPoint.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
    }
}
