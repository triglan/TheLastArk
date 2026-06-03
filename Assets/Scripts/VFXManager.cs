using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [System.Serializable]
    public class VFXData
    {
        public string name;
        public GameObject prefab;
    }

    [Header("VFX Settings")]
    [Tooltip("Register your VFX prefabs here with a unique name.")]
    public List<VFXData> vfxList = new List<VFXData>();

    private Dictionary<string, GameObject> vfxDictionary = new Dictionary<string, GameObject>();

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keep it across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize dictionary for fast lookup
        foreach (var vfx in vfxList)
        {
            if (vfx.prefab != null && !string.IsNullOrEmpty(vfx.name))
            {
                if (!vfxDictionary.ContainsKey(vfx.name))
                {
                    vfxDictionary.Add(vfx.name, vfx.prefab);
                }
                else
                {
                    Debug.LogWarning($"[VFXManager] Duplicate VFX name found: {vfx.name}");
                }
            }
        }
    }

    /// <summary>
    /// Spawns a VFX at the given position and rotation.
    /// </summary>
    /// <param name="vfxName">Name of the VFX registered in the list.</param>
    /// <param name="position">World position to spawn.</param>
    /// <param name="rotation">Rotation of the VFX.</param>
    public void SpawnVFX(string vfxName, Vector3 position, Quaternion rotation)
    {
        if (vfxDictionary.TryGetValue(vfxName, out GameObject prefab))
        {
            Instantiate(prefab, position, rotation);
        }
        else
        {
            Debug.LogWarning($"[VFXManager] VFX not found: {vfxName}");
        }
    }

    /// <summary>
    /// Helper method to spawn VFX with default rotation.
    /// </summary>
    public void SpawnVFX(string vfxName, Vector3 position)
    {
        SpawnVFX(vfxName, position, Quaternion.identity);
    }
}
