using UnityEngine;

public class PhotoelectricSensor : MonoBehaviour
{
    //[SerializeField] MQTTPublisher mqttPublisher;
    LineRenderer lineRenderer;
    RaycastHit hit;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lineRenderer= GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        lineRenderer.SetPosition(0,transform.position);
        
        // if(Physics.Raycast(transform.position, transform.forward, out hit, 6f))
        // {
        //     if(hit.collider.CompareTag("Box"))
        //     {
        //         Debug.Log("Box detected");
        //         //mqttPublisher.PublishCommands("factory/conveyor/01/commands/start",)
        //         lineRenderer.SetPosition(1,hit.point);
        //     }
        //     else
        //     {
        //         Vector3 maxEndPoint = transform.position+(transform.forward*6f);
        //         lineRenderer.SetPosition(1,maxEndPoint);
        //     }

        if (Physics.Raycast(transform.position, transform.forward, out hit, 6f))
{
    Debug.Log("Hit: " + hit.collider.gameObject.name +
              " | Tag: " + hit.collider.gameObject.tag);

    if (hit.collider.CompareTag("Box"))
    {
        Debug.Log("Box detected");
    }
}
        }
}