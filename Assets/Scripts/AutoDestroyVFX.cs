using UnityEngine;

public class AutoDestroyVFX : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Time in seconds before the VFX object is destroyed.")]
    public float lifeTime = 2.0f;
    private System.Collections.Generic.List<Material> ownedMaterials;
    public void OwnMaterials(System.Collections.Generic.List<Material> materials) => ownedMaterials = materials;

    private void Start()
    {
        // Destroy this GameObject after lifeTime seconds
        Destroy(gameObject, lifeTime);
    }

    private void OnDestroy()
    {
        if (ownedMaterials == null) return;
        foreach (var material in ownedMaterials) if (material != null) Destroy(material);
    }
}
