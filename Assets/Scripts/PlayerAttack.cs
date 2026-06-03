using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Two-step attack: press BaseAttack button to enter targeting mode,
/// then click an enemy to deal damage.
/// Requires a Camera with Physics2DRaycaster and enemies with Collider2D + EnemyHealth.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("Damage dealt per basic attack.")]
    public int attackDamage = 10;

    [Tooltip("Name of the VFX to spawn on hit (must match VFXManager list).")]
    public string vfxName = "Explosion";

    [Tooltip("Offset from the target position for VFX.")]
    public Vector3 vfxOffset = Vector3.up;

    [Header("Targeting Feedback")]
    [Tooltip("Color of the BaseAttack button while in targeting mode.")]
    public Color targetingColor = new Color(1f, 0.6f, 0.2f, 1f); // orange tint

    [Tooltip("Tag used to identify enemy GameObjects.")]
    public string enemyTag = "Enemy";

    // ── Internal state ──────────────────────────────────
    private bool isTargeting = false;
    private Image buttonImage;
    private Color originalColor;
    private Camera mainCamera;

    private void Awake()
    {
        // Cache the button's Image component for color feedback
        buttonImage = GetComponent<Image>();
        if (buttonImage != null)
        {
            originalColor = buttonImage.color;
        }

        mainCamera = Camera.main;

        // Auto-wire button OnClick → OnBasicAttack
        // This replaces any existing Inspector-assigned OnClick handlers at runtime
        var button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnBasicAttack);
        }
    }

    // ─────────────────────────────────────────────────────
    // Called from the UI Button's OnClick event
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Toggle targeting mode on/off.
    /// </summary>
    public void OnBasicAttack()
    {
        SetTargetingMode(!isTargeting);
    }

    // ─────────────────────────────────────────────────────
    // Targeting loop
    // ─────────────────────────────────────────────────────

    private void Update()
    {
        if (!isTargeting) return;

        // Cancel targeting with right-click or Escape
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            SetTargetingMode(false);
            return;
        }

        // Wait for left-click
        if (Input.GetMouseButtonDown(0))
        {
            TryAttackAtMousePosition();
        }
    }

    // ─────────────────────────────────────────────────────
    // Core logic
    // ─────────────────────────────────────────────────────

    private void TryAttackAtMousePosition()
    {
        if (mainCamera == null) return;

        Vector2 worldPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

        if (hit.collider != null && hit.collider.CompareTag(enemyTag))
        {
            // Found a valid enemy target
            EnemyHealth enemy = hit.collider.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                // Deal damage
                enemy.TakeDamage(attackDamage);

                // Spawn VFX at hit position
                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.SpawnVFX(vfxName, hit.collider.transform.position + vfxOffset);
                }

                Debug.Log($"[PlayerAttack] Attacked {hit.collider.gameObject.name} for {attackDamage} damage!");
            }

            // Exit targeting mode after a successful attack
            SetTargetingMode(false);
        }
        // If clicked on empty space or non-enemy, stay in targeting mode
    }

    private void SetTargetingMode(bool active)
    {
        isTargeting = active;

        // Visual feedback on the button
        if (buttonImage != null)
        {
            buttonImage.color = active ? targetingColor : originalColor;
        }

        Debug.Log(active
            ? "[PlayerAttack] Targeting mode ON — click an enemy to attack!"
            : "[PlayerAttack] Targeting mode OFF.");
    }
}
