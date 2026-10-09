using UnityEngine;

public class ObjectDestroy : MonoBehaviour
{
    private void OnTriggerEnter(Collider other) {
        Debug.Log("TriggerENTERED");
        if(other.gameObject.CompareTag("EndPoint"))
        {
            Destroy(gameObject);
        }
    }
}
