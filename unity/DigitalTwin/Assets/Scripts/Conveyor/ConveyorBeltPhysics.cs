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
        mqttSubscriber = GameObject.FindFirstObjectByType<MQTTSubscriber>();
        mqttPublisher =  GameObject.FindFirstObjectByType<MQTTPublisher>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is create

    public void SetSimulationSpeed(float speed)
    {
        RequestChangeSpeed(speed);
        float commandedSpeed = ReceiveCommandedSpeed();
        ConveyorSpeed = Mathf.Clamp(commandedSpeed, 0f, ConveyorMaxSpeed);
    }


    void RequestChangeSpeed(float speed)
    {
        _=mqttPublisher.PublishCommands("factory/conveyor/01/commands/speed-setpoint",speed);
    }

    float ReceiveCommandedSpeed()
    {
        float speed = mqttSubscriber.CommandedConveyorSpeed;
        return speed;
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
