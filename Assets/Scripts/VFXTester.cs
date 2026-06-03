using UnityEngine;

public class VFXTester : MonoBehaviour
{
    [Header("Test Settings")]
    [Tooltip("Name of the VFX to spawn (must match a name in VFXManager).")]
    public string vfxName = "Explosion"; 
    
    [Tooltip("Key to press to spawn the VFX.")]
    public KeyCode triggerKey = KeyCode.Space;
    
    [Tooltip("If true, spawn at mouse position on click.")]
    public bool spawnOnMouseClick = true;

    private void Update()
    {
        // 1. Spawn by Key Press (at center or random position)
        if (Input.GetKeyDown(triggerKey))
        {
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnVFX(vfxName, transform.position, Quaternion.identity);
                Debug.Log($"[VFXTester] Spawned {vfxName} at {transform.position}");
            }
        }

        // 2. Spawn by Mouse Click
        if (spawnOnMouseClick && Input.GetMouseButtonDown(0))
        {
            SpawnAtMousePosition();
        }
    }

    private void SpawnAtMousePosition()
    {
        if (VFXManager.Instance == null) return;

        // Convert mouse position to world point
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            VFXManager.Instance.SpawnVFX(vfxName, hit.point + Vector3.up * 0.5f, Quaternion.identity);
        }
        else
        {
            // If no ground, just spawn at a fixed distance from camera
            Vector3 point = ray.GetPoint(10.0f);
            VFXManager.Instance.SpawnVFX(vfxName, point, Quaternion.identity);
        }
    }
}
