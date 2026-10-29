using UnityEngine;
using UnityEngine.Pool;

public class ObjectPooling : MonoBehaviour
{
    [SerializeField] GameObject objToPool;
    [SerializeField] Transform spawnPoint;
    public ObjectPool<GameObject> pool;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        pool = new ObjectPool<GameObject>(
            OnCreateObj,
            OnGetObj,
            OnReleaseObj,
            OnDestroyObj,
            true,
            20,
            50 
        );
    }

    GameObject OnCreateObj()
    {
        GameObject gameObject = Instantiate(objToPool,spawnPoint.transform.position,Quaternion.identity);
        return gameObject;
    }

    void OnDestroyObj(GameObject obj)
    {
        Destroy(obj);
    }

    void OnGetObj(GameObject obj)
    {
        obj.SetActive(true);
    }

    void OnReleaseObj(GameObject obj)
    {
        obj.SetActive(false);
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        rb.linearVelocity=Vector3.zero;
        rb.angularVelocity=Vector3.zero;
        rb.mass=0f;
        obj.transform.position=spawnPoint.transform.position;
        obj.transform.rotation=Quaternion.identity;
    }
}
