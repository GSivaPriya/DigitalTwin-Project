using System;
using System.Collections;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [SerializeField] MQTTSubscriber mqttSubscriber;
    [SerializeField] Transform spawnPosition;
    [SerializeField] GameObject objectToSpawn;
    [SerializeField] float time;
    [SerializeField] float repeatRate;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SpawnObjects());
    }

    void SpawnObject()
    {
        GameObject spawnedObject= Instantiate(objectToSpawn, spawnPosition.position, Quaternion.identity);
        spawnedObject.gameObject.GetComponent<Rigidbody>().mass = UnityEngine.Random.Range(4.8f,5.2f);
        // spawnedObject.GetComponent<ObjectDriver>().direction=mqttSubscriber.CurrentStatus.machineDirection;
    }

    IEnumerator SpawnObjects()
    {
        while(true)
        {
            yield return new WaitUntil(() => 
            mqttSubscriber.CurrentStatus!=null&&
            mqttSubscriber.CurrentStatus.machineRunning &&
            mqttSubscriber.CurrentStatus.machineDirection!=0);

            SpawnObject();

            yield return new WaitForSeconds(repeatRate);

        }

    }
}
