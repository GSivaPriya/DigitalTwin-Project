using UnityEngine;
using System.Collections.Generic;

public class ConveyorBeltPhysics : MonoBehaviour
{
    MQTTSubscriber mqttSubscriber;
    MQTTPublisher mqttPublisher;
    public float ConveyorSpeed{get; private set;}=0.45f;
    public float ConveyorMaxSpeed{get; private set;}= 1.33f;

    HashSet<Rigidbody> boxesOnConveyor = new HashSet<Rigidbody>();
    public float ConveyorLoad {get ; private set;}


    void Start()
    {
        mqttSubscriber = FindFirstObjectByType<MQTTSubscriber>();
        mqttPublisher = FindFirstObjectByType<MQTTPublisher>();

        mqttSubscriber.OnCommandedSpeedChange += ReceiveCommandedSpeed;
    }
    void OnDestroy()
    {
        mqttSubscriber.OnCommandedSpeedChange -= ReceiveCommandedSpeed;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is create

    public void SetSimulationSpeed(float speed)
    {
        if(mqttPublisher==null)
        return;
        
        RequestChangeSpeed(speed);
        
    }


    void RequestChangeSpeed(float speed)
    {
        _=mqttPublisher.PublishCommands("factory/conveyor/01/commands/speed-setpoint",speed);
        Debug.Log($"Speed request sent with speed: {speed}");
    }

    void ReceiveCommandedSpeed(float speed)
    {
        ConveyorSpeed=Mathf.Clamp(speed, 0f, ConveyorMaxSpeed);
        Debug.Log($"Conveyor speed received: {ConveyorSpeed}");
    }


    private void OnCollisionEnter(Collision other) 
    {
       if(other.gameObject.CompareTag("Box"))
        {
            boxesOnConveyor.Add(other.rigidbody);
            ConveyorLoad+=other.rigidbody.mass;
            Debug.Log("box added);");
        }
    }

    private void OnCollisionExit(Collision other) 
    {
        if(other.gameObject.CompareTag("Box"))
        {
            boxesOnConveyor.Remove(other.rigidbody);
            ConveyorLoad-=other.rigidbody.mass;
            Debug.Log("box removed");
        }
    }

    private void FixedUpdate() 
    {
        if(mqttSubscriber==null || mqttSubscriber.CurrentStatus==null)
        return;

        if(!mqttSubscriber.CurrentStatus.machineRunning)
        return;

        boxesOnConveyor.RemoveWhere(rb => rb == null);

            foreach(Rigidbody rb in boxesOnConveyor)
            {
                rb.WakeUp();
                rb.linearVelocity=new Vector3(rb.linearVelocity.x,rb.linearVelocity.y, 
                ConveyorSpeed);
            }

    }
}
