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
    private EnemyThornChargesUI thornCharges;
    private RectTransform illustrationRect;
    private Vector3 illustrationBaseScale;
    private Vector2 illustrationBasePosition;
    private bool illustrationBasePreserveAspect;
    private float presentationScale = 1f;
    private CharacterData illustrationData;
    private int enemyStatusRows;
    private readonly List<TextMeshProUGUI> statusCounts = new List<TextMeshProUGUI>();
    private readonly List<Image> statusCountBackgrounds = new List<Image>();

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
            if (status.origin.closedEyesSprite != null && GetComponent<EnemyAI>()?.RuntimeState.Get("eyes_closed") == 1)
                sprite = status.origin.closedEyesSprite;
            if (sprite != null) displayImage.sprite = sprite;
            if (status.origin.isEnemy || illustrationRect != null) ApplyIllustrationLayout(status.origin);
        }

        if (barUI != null && status.origin != null)
        {
            // 강화 배율이 적용된 최종 체력과 정신력을 바에 표시합니다.
            barUI.UpdateAllBars(status.currentHp, status.FinalMaxHp,
                               status.currentMental, status.FinalMaxMental);
            UpdateDefense(status);
        }

        UpdateStatusEffects(status);
        int thorns = status.GetStatus(EffectType.Reflect)?.remainingCharges ?? 0;
        if (thorns > 0 && thornCharges == null)
        {
            var root = new GameObject("Remaining Thorns", typeof(RectTransform), typeof(CanvasRenderer));
            root.transform.SetParent(transform, false);
            thornCharges = root.AddComponent<EnemyThornChargesUI>();
            thornCharges.raycastTarget = false;
            thornCharges.rectTransform.anchorMin = thornCharges.rectTransform.anchorMax = new Vector2(0.5f, 0.85f);
            thornCharges.rectTransform.sizeDelta = new Vector2(100f, 20f);
        }
        if (thornCharges != null) thornCharges.SetCount(thorns);
        if (status.origin != null && status.origin.isEnemy) RefreshEnemyHUDLayout();
    }

    public void ApplyIllustrationLayout(CharacterData data)
    {
        if (displayImage == null) return;
        illustrationData = data;
        RectTransform rect = displayImage.rectTransform;
        if (illustrationRect != rect)
        {
            illustrationRect = rect;
            illustrationBaseScale = rect.localScale;
            illustrationBasePosition = rect.anchoredPosition;
            illustrationBasePreserveAspect = displayImage.preserveAspect;
        }
        displayImage.preserveAspect = data != null && data.isEnemy || illustrationBasePreserveAspect;
        Vector2 size = TheLastArk.Character.EnemyIllustrationLayout.FittedSize(rect.rect.size, displayImage.sprite, displayImage.preserveAspect);
        float scale = TheLastArk.Character.EnemyIllustrationLayout.ScaleFor(data, size, illustrationBaseScale) * presentationScale;
        rect.localScale = Vector3.Scale(illustrationBaseScale, new Vector3(scale, scale, 1f));
        rect.anchoredPosition = illustrationBasePosition + TheLastArk.Character.EnemyIllustrationLayout.PositionOffset(data, size, rect.rect.size, rect.pivot, illustrationBaseScale, presentationScale);
        if (data != null && data.isEnemy) RefreshEnemyHUDLayout();
    }

    public void SetPresentationScale(float scale)
    {
        presentationScale = Mathf.Clamp(scale, 0.1f, 1.12f);
        if (illustrationData != null) ApplyIllustrationLayout(illustrationData);
    }

    public Rect IllustrationBounds
    {
        get
        {
            if (displayImage == null) return Rect.zero;
            var rect = displayImage.rectTransform;
            Vector2 size = TheLastArk.Character.EnemyIllustrationLayout.FittedSize(rect.rect.size, displayImage.sprite, displayImage.preserveAspect);
            size = Vector2.Scale(size, rect.localScale);
            return new Rect((Vector2)rect.localPosition - Vector2.Scale(size, rect.pivot), size);
        }
    }

    private float EnhancementHeight => GetComponent<EnemyEnhancementUI>()?.LayoutHeight ?? 0f;
    public float EnemyHUDHeight => 92f + Mathf.Max(0, enemyStatusRows - 1) * 32f + EnhancementHeight;
    public float IllustrationBaseline => illustrationRect != null
        ? illustrationBasePosition.y - illustrationRect.rect.height * illustrationRect.pivot.y * illustrationBaseScale.y : -352f;

    public void RefreshEnemyHUDLayout()
    {
        if (illustrationData == null || !illustrationData.isEnemy || barUI == null || barUI.hpSlider == null) return;
        Rect bounds = IllustrationBounds;
        var barRoot = barUI.hpSlider.transform.parent as RectTransform;
        if (barRoot == null || barRoot == transform) return;
        barRoot.anchorMin = barRoot.anchorMax = new Vector2(0.5f, 0.5f);
        barRoot.localScale = Vector3.one;
        barRoot.anchoredPosition = new Vector2(bounds.center.x, bounds.yMax + 56f + Mathf.Max(0, enemyStatusRows - 1) * 32f + EnhancementHeight);
        GetComponent<EnemyEnhancementUI>()?.SetPosition(new Vector2(bounds.center.x, bounds.yMax + 20f));
        var hpRect = barUI.hpSlider.GetComponent<RectTransform>();
        var mentalRect = barUI.mentalSlider.GetComponent<RectTransform>();
        SetCompactBarHeight(hpRect);
        SetCompactBarHeight(mentalRect);
        mentalRect.anchoredPosition = new Vector2(0f, -18f);
        if (defenseRoot != null)
        {
            defenseRoot.anchoredPosition = new Vector2(enemyStatusRows > 0 ? -54f : 0f, -38f);
            defenseRoot.localScale = Vector3.one * 0.9f;
        }
        if (statusIconRoot != null)
        {
            statusIconRoot.pivot = new Vector2(0.5f, 1f);
            statusIconRoot.anchoredPosition = new Vector2(50f, -38f);
        }
        GetComponent<EnemyIntentUI>()?.SetPosition(barRoot.anchoredPosition + new Vector2(0f, 22f));
        GetComponent<EnemyNameHoverUI>()?.SetLabelPosition(new Vector2(bounds.center.x, bounds.yMin - 28f));
        if (thornCharges != null)
        {
            thornCharges.rectTransform.anchorMin = thornCharges.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            thornCharges.rectTransform.anchoredPosition = new Vector2(bounds.center.x, bounds.yMin + 12f);
        }
    }

    private static void SetCompactBarHeight(RectTransform bar)
    {
        bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 28f);
        // The prefab's fixed border otherwise overlaps the adjacent compact bar.
        var border = bar.Find("Border") as RectTransform;
        if (border != null) border.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 16f);
    }

    private void UpdateStatusEffects(CharacterStatus status)
    {
        if (status?.origin != null && status.origin.isEnemy)
        {
            UpdateEnemyStatusIcons(status);
            if (statusText != null) statusText.gameObject.SetActive(false);
            return;
        }
        EnsureStatusText();
        if (statusText == null) return;

        var builder = new StringBuilder();
        if (status?.panic != null && status.panic.IsActive)
        {
            builder.Append("<color=#FF7777>패닉 · ").Append(TheLastArk.Battle.PanicState.Name(status.panic.Type)).Append("</color>");
        }
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

    private void UpdateEnemyStatusIcons(CharacterStatus status)
    {
        if (barUI == null || barUI.hpSlider == null) return;
        EnsureStatusIconRoot();
        if (statusIconRoot == null) return;
        statusIconRoot.SetParent(barUI.hpSlider.transform.parent, false);
        statusIconRoot.anchorMin = statusIconRoot.anchorMax = new Vector2(0.5f, 0.5f);
        var active = status.activeStatusEffects.FindAll(effect => effect != null && effect.IsActive);
        while (statusIcons.Count < active.Count)
        {
            var root = new GameObject("Status Icon", typeof(RectTransform), typeof(Image), typeof(BattleHoverTooltip));
            root.transform.SetParent(statusIconRoot, false);
            var icon = root.GetComponent<Image>(); icon.preserveAspect = true; icon.rectTransform.sizeDelta = new Vector2(30f, 30f);
            statusIcons.Add(icon);
            var bubble = new GameObject("Count Background", typeof(RectTransform), typeof(Image)); bubble.transform.SetParent(root.transform, false);
            var bubbleImage = bubble.GetComponent<Image>(); bubbleImage.color = new Color(0.04f, 0.06f, 0.1f, 0.95f); bubbleImage.raycastTarget = false;
            bubbleImage.rectTransform.anchorMin = bubbleImage.rectTransform.anchorMax = new Vector2(1f, 0f);
            bubbleImage.rectTransform.sizeDelta = new Vector2(18f, 18f);
            statusCountBackgrounds.Add(bubbleImage);
            var count = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI)); count.transform.SetParent(root.transform, false);
            var label = count.GetComponent<TextMeshProUGUI>(); label.fontSize = 15f; label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(1f, 0f);
            label.rectTransform.sizeDelta = new Vector2(24f, 20f);
            TMPFontManager.ApplyFont(label); statusCounts.Add(label);
        }
        enemyStatusRows = Mathf.CeilToInt(active.Count / 3f);
        statusIconRoot.sizeDelta = new Vector2(180f, enemyStatusRows * 32f);
        for (int i = 0; i < statusIcons.Count; i++)
        {
            statusIcons[i].gameObject.SetActive(i < active.Count);
            if (i >= active.Count) continue;
            var effect = active[i]; var icon = statusIcons[i];
            icon.sprite = effect.displayIcon != null ? effect.displayIcon : IconLibrary.FindIcon(effect.effectType);
            icon.color = icon.sprite != null ? Color.white : new Color(0.2f, 0.25f, 0.34f);
            int columns = Mathf.Min(3, active.Count - i / 3 * 3);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            icon.rectTransform.anchoredPosition = new Vector2((i % 3 - (columns - 1) * 0.5f) * 32f, -i / 3 * 32f);
            string count = effect.durationType == StatusDurationType.Turns ? effect.remainingTurns.ToString()
                : effect.durationType == StatusDurationType.Charges ? effect.remainingCharges.ToString()
                : effect.damagePerTurn > 0f ? effect.damagePerTurn.ToString("0.#") : "";
            statusCounts[i].text = icon.sprite != null ? count : ActiveStatusEffect.GetDisplayName(effect.effectType) + count;
            statusCountBackgrounds[i].gameObject.SetActive(icon.sprite != null && count.Length > 0);
            statusCounts[i].rectTransform.anchorMin = statusCounts[i].rectTransform.anchorMax = icon.sprite != null ? new Vector2(1f, 0f) : new Vector2(0.5f, 0.5f);
            statusCounts[i].rectTransform.sizeDelta = icon.sprite != null ? new Vector2(24f, 20f) : new Vector2(32f, 30f);
            statusCounts[i].fontSize = icon.sprite != null ? 15f : 11f;
            icon.GetComponent<BattleHoverTooltip>().SetContent(effect.GetDisplayText());
        }
        statusIconRoot.gameObject.SetActive(active.Count > 0);
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
        rect.pivot = new Vector2(sourceRect.pivot.x, 1f);
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
