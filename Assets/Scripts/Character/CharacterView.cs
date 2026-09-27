using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Collections.Generic;
using TheLastArk.UI;

public class CharacterView : MonoBehaviour
{
    private const string StandingIllustrationObjectName = "Standing Illust";

    public bool isStandingIllust = false;

    [Header("수동 연결")]
    public Image displayImage;
    public BarUI barUI;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject enemyTargetMarkerPrefab;
    private GameObject enemyTargetMarker;
    private RectTransform defenseRoot;
    private TextMeshProUGUI armorText;
    private TextMeshProUGUI magicResistText;
    private RectTransform statusIconRoot;
    private readonly List<Image> statusIcons = new List<Image>();

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
            Sprite sprite = (status.origin.isEnemy || isStandingIllust)
                ? status.origin.standingSprite
                : status.origin.portraitSprite;
            if (sprite != null) displayImage.sprite = sprite;
        }

        if (barUI != null && status.origin != null)
        {
            // 강화 배율이 적용된 최종 체력과 정신력을 바에 표시합니다.
            barUI.UpdateAllBars(status.currentHp, status.FinalMaxHp,
                               status.currentMental, status.FinalMaxMental);
            UpdateDefense(status);
        }

        UpdateStatusEffects(status);
    }

    private void UpdateStatusEffects(CharacterStatus status)
    {
        EnsureStatusText();
        if (statusText == null) return;

        var builder = new StringBuilder();
        if (status?.activeStatusEffects != null)
        {
            foreach (ActiveStatusEffect effect in status.activeStatusEffects)
            {
                if (effect == null || !effect.IsActive) continue;
                if (builder.Length > 0) builder.AppendLine();
                builder.Append(effect.GetDisplayText());
            }
        }

        UpdateStatusIcons(status);
        statusText.text = builder.ToString();
        statusText.gameObject.SetActive(builder.Length > 0);
    }

    private void UpdateStatusIcons(CharacterStatus status)
    {
        EnsureStatusIconRoot();
        if (statusIconRoot == null) return;

        var sprites = new List<Sprite>();
        if (status?.activeStatusEffects != null)
            foreach (ActiveStatusEffect effect in status.activeStatusEffects)
                if (effect != null && effect.IsActive && effect.displayIcon != null) sprites.Add(effect.displayIcon);

        while (statusIcons.Count < sprites.Count)
        {
            GameObject icon = new GameObject("Status Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            icon.transform.SetParent(statusIconRoot, false);
            Image image = icon.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(26f, 26f);
            statusIcons.Add(image);
        }

        int rows = Mathf.CeilToInt(sprites.Count / 5f);
        float defenseOffset = status?.origin != null && status.origin.isEnemy ? 34f : 0f;
        RectTransform sourceRect = barUI.mentalText.rectTransform;
        statusIconRoot.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -24f - defenseOffset);
        statusIconRoot.sizeDelta = new Vector2(180f, rows * 30f);
        for (int i = 0; i < statusIcons.Count; i++)
        {
            bool active = i < sprites.Count;
            statusIcons[i].gameObject.SetActive(active);
            if (!active) continue;
            int row = i / 5;
            int column = i % 5;
            int columnsInRow = Mathf.Min(5, sprites.Count - row * 5);
            statusIcons[i].sprite = sprites[i];
            statusIcons[i].rectTransform.anchoredPosition = new Vector2((column - (columnsInRow - 1) * 0.5f) * 30f, -row * 30f);
        }

        statusIconRoot.gameObject.SetActive(sprites.Count > 0);
        statusText.rectTransform.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -24f - defenseOffset - rows * 30f);
    }

    private void UpdateDefense(CharacterStatus status)
    {
        bool visible = status?.origin != null && status.origin.isEnemy;
        if (!visible)
        {
            if (defenseRoot != null) defenseRoot.gameObject.SetActive(false);
            return;
        }

        EnsureDefenseUI();
        if (defenseRoot == null) return;

        defenseRoot.gameObject.SetActive(true);
        armorText.text = Mathf.Max(0f, status.FinalArmor).ToString("0.#");
        magicResistText.text = Mathf.Max(0f, status.FinalMagicResist).ToString("0.#");
    }

    private void EnsureDefenseUI()
    {
        if (defenseRoot != null || barUI == null || barUI.hpSlider == null) return;

        GameObject root = new GameObject("Defense Stats", typeof(RectTransform));
        root.transform.SetParent(barUI.hpSlider.transform.parent, false);
        defenseRoot = root.GetComponent<RectTransform>();
        defenseRoot.anchorMin = defenseRoot.anchorMax = defenseRoot.pivot = new Vector2(0.5f, 0.5f);
        defenseRoot.anchoredPosition = barUI.hpSlider.GetComponent<RectTransform>().anchoredPosition + new Vector2(0f, -44f);
        defenseRoot.sizeDelta = new Vector2(72f, 30f);

        armorText = CreateDefenseSlot("Armor", -18f, IconLibrary.FindArmorIcon());
        magicResistText = CreateDefenseSlot("Magic Resist", 18f, IconLibrary.FindMagicResistIcon());
    }

    private TextMeshProUGUI CreateDefenseSlot(string name, float x, Sprite sprite)
    {
        GameObject slot = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        slot.transform.SetParent(defenseRoot, false);
        RectTransform slotRect = slot.GetComponent<RectTransform>();
        slotRect.anchorMin = slotRect.anchorMax = slotRect.pivot = new Vector2(0.5f, 0.5f);
        slotRect.anchoredPosition = new Vector2(x, 0f);
        slotRect.sizeDelta = new Vector2(28f, 28f);

        UnityEngine.UI.Image image = slot.GetComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        GameObject label = new GameObject("Value", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(slot.transform, false);
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.font = barUI.hpText != null ? barUI.hpText.font : null;
        text.fontSize = 13f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.BottomRight;
        text.color = Color.white;
        text.outlineColor = Color.black;
        text.outlineWidth = 0.2f;
        text.raycastTarget = false;
        TMPFontManager.ApplyFont(text);

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return text;
    }

    private void EnsureStatusText()
    {
        if (statusText != null || barUI == null || barUI.mentalText == null) return;

        GameObject statusObject = new GameObject("StatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        statusObject.transform.SetParent(barUI.mentalText.transform.parent, false);
        statusText = statusObject.GetComponent<TextMeshProUGUI>();
        statusText.font = barUI.mentalText.font;
        statusText.fontSize = 14f;
        statusText.alignment = TextAlignmentOptions.Top;
        statusText.color = Color.white;
        statusText.raycastTarget = false;
        statusText.enableWordWrapping = false;
        TMPFontManager.ApplyFont(statusText);

        RectTransform sourceRect = barUI.mentalText.rectTransform;
        RectTransform rect = statusText.rectTransform;
        rect.anchorMin = sourceRect.anchorMin;
        rect.anchorMax = sourceRect.anchorMax;
        rect.pivot = sourceRect.pivot;
        rect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -24f);
        rect.sizeDelta = new Vector2(Mathf.Max(220f, sourceRect.sizeDelta.x), 90f);
        statusObject.SetActive(false);
    }

    private void EnsureStatusIconRoot()
    {
        if (statusIconRoot != null || barUI == null || barUI.mentalText == null) return;

        GameObject root = new GameObject("StatusIcons", typeof(RectTransform));
        root.transform.SetParent(barUI.mentalText.transform.parent, false);
        statusIconRoot = root.GetComponent<RectTransform>();
        RectTransform sourceRect = barUI.mentalText.rectTransform;
        statusIconRoot.anchorMin = sourceRect.anchorMin;
        statusIconRoot.anchorMax = sourceRect.anchorMax;
        statusIconRoot.pivot = new Vector2(0.5f, 1f);
        statusIconRoot.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -24f);
        root.SetActive(false);
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
