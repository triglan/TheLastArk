using UnityEngine;
using UnityEngine.UI;

public class CharacterView : MonoBehaviour
{
    private const string StandingIllustrationObjectName = "Standing Illust";

    public bool isStandingIllust = false;

    [Header("수동 연결")]
    public Image displayImage;
    public BarUI barUI;
    [SerializeField] private GameObject enemyTargetMarkerPrefab;
    private GameObject enemyTargetMarker;

    private void Awake()
    {
        EnsureLocalDisplayImage();
        if (enemyTargetMarker != null) enemyTargetMarker.SetActive(false);
    }

    private void Reset()
    {
        EnsureLocalDisplayImage();
    }

    private void OnValidate()
    {
        EnsureLocalDisplayImage();
    }

    // 이제 데이터를 생성하지 않고, 받은 데이터를 그리기만 합니다.
    public void UpdateVisual(CharacterStatus status)
    {
        EnsureLocalDisplayImage();

        if (displayImage != null && status.origin != null)
        {
            displayImage.sprite = (status.origin.isEnemy || isStandingIllust)
                ? status.origin.standingSprite
                : status.origin.portraitSprite;
        }

        if (barUI != null && status.origin != null)
        {
            // 강화 배율이 적용된 최종 체력과 정신력을 바에 표시합니다.
            barUI.UpdateAllBars(status.currentHp, status.FinalMaxHp,
                               status.currentMental, status.FinalMaxMental);
        }
    }

    public void SetEnemyTargeted(bool targeted)
    {
        if (targeted && enemyTargetMarker == null)
            CreateDefaultEnemyTargetMarker();

        if (enemyTargetMarker != null)
            enemyTargetMarker.SetActive(targeted);
    }

    [ContextMenu("Debug: Toggle Enemy Target Marker")]
    private void DebugToggleEnemyTargetMarker()
    {
        SetEnemyTargeted(enemyTargetMarker == null || !enemyTargetMarker.activeSelf);
    }

    private void CreateDefaultEnemyTargetMarker()
    {
        Transform parent = transform.Find("TargetPoint");
        if (parent == null) parent = transform;

        if (enemyTargetMarkerPrefab != null)
        {
            enemyTargetMarker = Instantiate(enemyTargetMarkerPrefab, parent, false);
            enemyTargetMarker.SetActive(false);
            return;
        }

        enemyTargetMarker = new GameObject("Enemy Target Marker", typeof(RectTransform), typeof(Image));
        enemyTargetMarker.transform.SetParent(parent, false);

        RectTransform rect = enemyTargetMarker.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(48f, 48f);
        rect.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image image = enemyTargetMarker.GetComponent<Image>();
        image.color = new Color(1f, 0.1f, 0.1f, 0.9f);
        image.raycastTarget = false;
        enemyTargetMarker.SetActive(false);
    }

    private void EnsureLocalDisplayImage()
    {
        if (displayImage != null && displayImage.transform.IsChildOf(transform)) return;

        Transform standingIllustration = transform.Find(StandingIllustrationObjectName);
        if (standingIllustration != null)
            displayImage = standingIllustration.GetComponent<Image>();

        if (displayImage == null)
            Debug.LogError($"[{name}] CharacterView could not find its local '{StandingIllustrationObjectName}' Image.", this);
    }
}
