using UnityEngine;

public class ExitSensor : PhotoelectricSensor
{
    protected override void OnBoxDetected(bool value, MQTTPublisher publisher)
    {
        if (value)
        {
            _= publisher.PublishCommands("factory/conveyor/01/sensors/exit",value);

        }
        else
        {
            Debug.Log("Box cleared");
            _= publisher.PublishCommands("factory/conveyor/01/sensors/exit",value);
        }
    }
}
