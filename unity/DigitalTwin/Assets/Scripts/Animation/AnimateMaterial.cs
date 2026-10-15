using UnityEngine;

public class AnimateMaterial : MonoBehaviour
{
    private int materialIndex = 7;
    private Material targetMaterial;
    private Vector2 currentOffset = Vector2.zero;
    private float scrollSpeed = 0.5f; 
    private float conveyorReferenceSpeed=0.45f;
    MQTTSubscriber mqttSubscriber;
    ConveyorBeltPhysics conveyor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        mqttSubscriber = GameObject.FindFirstObjectByType<MQTTSubscriber>();
        conveyor = GetComponent<ConveyorBeltPhysics>();


        targetMaterial= meshRenderer.materials[materialIndex];
    }

    // Update is called once per frame
    void Update()
    {
        MaterialAnimation();
    }

    void MaterialAnimation()
    {
        if(mqttSubscriber==null || mqttSubscriber.CurrentStatus==null)
        return;

        if(!mqttSubscriber.CurrentStatus.machineRunning)
        return;

        float scale = conveyor.ConveyorSpeed/conveyorReferenceSpeed;

        currentOffset.y += scrollSpeed* scale* Time.deltaTime;
        targetMaterial.SetTextureOffset("_BaseMap", currentOffset);
        
    }

}

