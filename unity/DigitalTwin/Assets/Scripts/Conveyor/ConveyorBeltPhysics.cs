using UnityEngine;
using System.Collections.Generic;

public class ConveyorBeltPhysics : MonoBehaviour
{
    [SerializeField] MQTTSubscriber mqttSubscriber;
    [SerializeField] float conveyorSpeed=0.55f;
    HashSet<Rigidbody> boxesOnConveyor = new HashSet<Rigidbody>();

    void Start()
    {

    }
    // Start is called once before the first execution of Update after the MonoBehaviour is create

    private void OnCollisionEnter(Collision other) 
    {
       if(other.gameObject.CompareTag("Box"))
        {
            boxesOnConveyor.Add(other.rigidbody);
        }
    }

    private void OnCollisionExit(Collision other) 
    {
        if(other.gameObject.CompareTag("Box"))
        {
            boxesOnConveyor.Remove(other.rigidbody);
        }
    }

    private void FixedUpdate() 
    {
        if(mqttSubscriber==null || mqttSubscriber.CurrentStatus==null)
        return;

        if(!mqttSubscriber.CurrentStatus.machineRunning)
        return;

        boxesOnConveyor.RemoveWhere(rb => rb == null);

            int direction = mqttSubscriber.CurrentStatus.machineDirection;
            foreach(Rigidbody rb in boxesOnConveyor)
            {
                rb.WakeUp();
                rb.linearVelocity=new Vector3(rb.linearVelocity.x,rb.linearVelocity.y, conveyorSpeed*direction);
            }

    }
}
