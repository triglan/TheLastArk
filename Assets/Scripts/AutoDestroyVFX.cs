using UnityEngine;

public class AutoDestroyVFX : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Time in seconds before the VFX object is destroyed.")]
    public float lifeTime = 2.0f;

    private void Start()
    {
        // Destroy this GameObject after lifeTime seconds
        Destroy(gameObject, lifeTime);
    }
}
