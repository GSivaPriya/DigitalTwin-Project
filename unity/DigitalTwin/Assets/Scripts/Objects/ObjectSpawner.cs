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
    [SerializeField] float minimumMassKg = 4.8f;
    [SerializeField] float maximumMassKg = 5.2f;

    private bool spawningEnabled;
    public bool SpawningEnabled => spawningEnabled;

    public void Configure(float minMassKg, float maxMassKg, float intervalSeconds)
    {
        minimumMassKg = Mathf.Max(0.1f, minMassKg);
        maximumMassKg = Mathf.Max(minimumMassKg, maxMassKg);
        repeatRate = Mathf.Max(0.1f, intervalSeconds);
    }

    public void StartSpawning() => spawningEnabled = true;
    public void StopSpawning() => spawningEnabled = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            bool plcRunning = mqttSubscriber != null &&
                mqttSubscriber.CurrentStatus != null &&
                mqttSubscriber.CurrentStatus.machineRunning;

            if (spawningEnabled && plcRunning)
            {
                SpawnObject();
                yield return new WaitForSeconds(Mathf.Max(0.1f, repeatRate));
            }
            else yield return null;
        }
    }
    void SpawnObject()
    {
        GameObject spawnedObject= Instantiate(objectToSpawn, spawnPosition.position, Quaternion.identity);
        spawnedObject.gameObject.GetComponent<Rigidbody>().mass = UnityEngine.Random.Range(4.8f,5.2f);
    }

    private void OnDisable() => spawningEnabled = false;

    // IEnumerator SpawnObjects()
    // {
    //     while(true)
    //     {
    //         yield return new WaitUntil(() => 
    //         mqttSubscriber.CurrentStatus!=null&&
    //         mqttSubscriber.CurrentStatus.machineRunning);

    //         SpawnObject();

    //         yield return new WaitForSeconds(repeatRate);

    //     }

    // }
}
