using UnityEngine;

public class ObjectDriver : MonoBehaviour
{
    [SerializeField] float moveSpeed=5f;

    // Update is called once per frame
    void Update()
    {
        MoveObject();
    }

    void MoveObject()
    {
        transform.position += new Vector3(0f,0f,moveSpeed)*Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("EndPoint"))
        {
            Destroy(gameObject);
        }
    }
}
