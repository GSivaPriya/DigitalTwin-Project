using System.Threading.Tasks;
using UnityEngine;

public class ConveyorControllerCommands : MonoBehaviour
{
    bool startCommand;
    bool stopCommand;
    // bool forwardCommand;
    // bool reverseCommand;

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

    // public void OnForwardClicked()
    // {
    //     _ = HandleForward();
    // }
    // async Task HandleForward()
    // {
    //     forwardCommand=!forwardCommand;
    //     if(forwardCommand)
    //     {
    //         reverseCommand=false;
    //     }
    //     PublishStates();
    // }
    // public void OnReverseClicked()
    // {
    //     _ = HandleReverse();
    // }

    // async Task HandleReverse()
    // {
    //     reverseCommand=!reverseCommand;
    //     if(reverseCommand)
    //     {
    //         forwardCommand=false;
    //     }
    //     PublishStates();
    // }

    void PublishStates()
    {
        _ = mqttPublisher.PublishCommands("factory/conveyor/01/commands/start", startCommand);
        _ = mqttPublisher.PublishCommands("factory/conveyor/01/commands/stop", stopCommand);
        // _ = mqttPublisher.PublishCommands("factory/conveyor/01/commands/forward", forwardCommand);
        // _ = mqttPublisher.PublishCommands("factory/conveyor/01/commands/reverse", reverseCommand);
    }
}
