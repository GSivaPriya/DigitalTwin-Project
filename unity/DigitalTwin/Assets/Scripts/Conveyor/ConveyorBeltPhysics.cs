using UnityEngine;
using System.Collections.Generic;

public class ConveyorBeltPhysics : MonoBehaviour
{
    MQTTSubscriber mqttSubscriber;
    public float ConveyorSpeed{get; private set;}=0.55f;
    public float ConveyorMaxSpeed{get; private set;}= 1.33f;

    HashSet<Rigidbody> boxesOnConveyor = new HashSet<Rigidbody>();
    public float ConveyorLoad {get ; private set;}

    void Start()
    {
        mqttSubscriber = GameObject.FindFirstObjectByType<MQTTSubscriber>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is create

    private void OnCollisionEnter(Collision other) 
    {
       if(other.gameObject.CompareTag("Box"))
        {
            boxesOnConveyor.Add(other.rigidbody);
            ConveyorLoad+=other.rigidbody.mass;
        }
    }

    private void OnCollisionExit(Collision other) 
    {
        if(other.gameObject.CompareTag("Box"))
        {
            boxesOnConveyor.Remove(other.rigidbody);
            ConveyorLoad-=other.rigidbody.mass;
        }
    }

    private void FixedUpdate() 
    {
        if(mqttSubscriber==null || mqttSubscriber.CurrentStatus==null)
        return;

        if(!mqttSubscriber.CurrentStatus.machineRunning)
        return;

        boxesOnConveyor.RemoveWhere(rb => rb == null);

            //int direction = mqttSubscriber.CurrentStatus.machineDirection;
            foreach(Rigidbody rb in boxesOnConveyor)
            {
                rb.WakeUp();
                rb.linearVelocity=new Vector3(rb.linearVelocity.x,rb.linearVelocity.y, 
                ConveyorSpeed); //*direction removed 
            }

    }
}
