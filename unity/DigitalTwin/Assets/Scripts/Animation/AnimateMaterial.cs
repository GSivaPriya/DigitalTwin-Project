using UnityEngine;

public class AnimateMaterial : MonoBehaviour
{
    private int materialIndex = 7;
    private Material targetMaterial;
    private Vector2 currentOffset = Vector2.zero;
    public Vector2 scrollSpeed = new Vector2(0.5f,0.0f);
    private int currentDirection;
    [SerializeField] MQTTSubscriber mqttSubscriber;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
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

        
            currentDirection=mqttSubscriber.CurrentStatus.machineDirection;
            currentOffset += scrollSpeed *currentDirection* Time.deltaTime;
            targetMaterial.SetTextureOffset("_BaseMap", currentOffset);
        
    }

}

