using System;
using UnityEngine;

public class PhotoelectricSensor : MonoBehaviour
{
    LineRenderer lineRenderer;
    RaycastHit hit;
    bool boxDetected;
    [SerializeField] float maxDistance=6f;
    [SerializeField] Material material;
    [SerializeField] MQTTPublisher mqttPublisher;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lineRenderer= GetComponent<LineRenderer>();
        lineRenderer.material=material;
    }

    void Update()
    {
        bool detected = Physics.Raycast(transform.position, transform.forward, out hit, maxDistance) &&
        hit.collider.CompareTag("Box");

        if (detected != boxDetected)
        {
            boxDetected = detected;
            OnBoxDetected(boxDetected, mqttPublisher);
        }

        lineRenderer.SetPosition(0, transform.position);

        if (detected)
        {
            lineRenderer.SetPosition(1, hit.point);
        }
        else
        {
            lineRenderer.SetPosition(1, transform.position + transform.forward * maxDistance);
        }
    }

    protected virtual void OnBoxDetected(bool value, MQTTPublisher publisher)
    {}

}