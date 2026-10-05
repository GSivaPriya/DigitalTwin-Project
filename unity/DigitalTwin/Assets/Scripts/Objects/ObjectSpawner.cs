using System;
using System.Numerics;
using Unity.Mathematics;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [SerializeField] Transform spawnPosition;
    [SerializeField] GameObject objectToSpawn;
    [SerializeField] float time;
    [SerializeField] float repeatRate;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InvokeRepeating("SpawnObject",time,repeatRate);
    }

    void SpawnObject()
    {
        Instantiate(objectToSpawn, spawnPosition.position, quaternion.identity);
    }
}
