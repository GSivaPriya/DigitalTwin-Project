using System;
using UnityEngine;

public class ObjectDestroy : MonoBehaviour
{
    [SerializeField] ObjectPooling objectPooling;
    // private void OnTriggerEnter(Collider other) {
    //     Debug.Log("TriggerENTERED");
    //     if(other.gameObject.CompareTag("EndPoint"))
    //     {
    //         Destroy(gameObject);
    //     }
    // }

    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Box"))
        {
            objectPooling.pool.Release(collision.gameObject);
        }
    }
}
