using UnityEngine;

public class AnimateMaterial : MonoBehaviour
{
    private int materialIndex = 7;
    public Vector2 scrollSpeed = new Vector2(0.5f,0.0f);
    private Material targetMaterial;
    private Vector2 currentOffset = Vector2.zero;
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
        currentOffset += scrollSpeed * Time.deltaTime;
        targetMaterial.SetTextureOffset("_BaseMap", currentOffset);
    }

}

