using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages enemy HP. Attach to each enemy GameObject.
/// When HP reaches 0 the GameObject is deactivated.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("HP Settings")]
    [Tooltip("Maximum hit points.")]
    public int maxHP = 100;

    [Tooltip("Current hit points (auto-set to maxHP on start).")]
    public int currentHP;

    [Header("Optional UI")]
    [Tooltip("Drag an HP bar Image (Filled type) here to show remaining HP.")]
    public Image hpBarFill;

    private void Start()
    {
        currentHP = maxHP;
        UpdateHPBar();
    }

    /// <summary>
    /// Reduce HP by the given amount. Deactivates the object when HP <= 0.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        currentHP -= amount;
        currentHP = Mathf.Max(currentHP, 0);

        Debug.Log($"[EnemyHealth] {gameObject.name} took {amount} damage! HP: {currentHP}/{maxHP}");

        UpdateHPBar();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void UpdateHPBar()
    {
        if (hpBarFill != null)
        {
            hpBarFill.fillAmount = (float)currentHP / maxHP;
        }
    }

    private void Die()
    {
        Debug.Log($"[EnemyHealth] {gameObject.name} has been defeated!");
        gameObject.SetActive(false);
    }
}
