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
    private bool allyMarkerSpaceReserved;
    private const float TargetBadgeSpace = 36f;
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
    private const int VisibleStatusLimit = 4;
    private readonly List<TextMeshProUGUI> statusCounts = new List<TextMeshProUGUI>();
    private readonly List<Image> statusCountBackgrounds = new List<Image>();
    private Image panicIcon;
    private PanicIconGraphic panicSymbol;
    private BattleHoverTooltip panicTooltip;
    private Image primalManaIcon;
    private TextMeshProUGUI primalManaText;

    private bool UpdatePrimalManaIcon(CharacterStatus status)
    {
        bool active = status.UsesPrimalMana || status.UsesCapacitor;
        if (!active && primalManaIcon == null) return false;
        if (primalManaIcon == null)
        {
            var root = new GameObject("Primal Mana", typeof(RectTransform), typeof(Image), typeof(BattleHoverTooltip));
            root.transform.SetParent(statusIconRoot, false);
            primalManaIcon = root.GetComponent<Image>();
            primalManaIcon.color = new Color(.04f, .19f, .27f, .98f);
            primalManaIcon.rectTransform.sizeDelta = Vector2.one * 34f;
            primalManaIcon.rectTransform.anchorMin = primalManaIcon.rectTransform.anchorMax = Vector2.one * .5f;
            var label = new GameObject("Mana Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(root.transform, false);
            primalManaText = label.GetComponent<TextMeshProUGUI>();
            primalManaText.fontSize = 12f;
            primalManaText.fontStyle = FontStyles.Bold;
            primalManaText.alignment = TextAlignmentOptions.Center;
            primalManaText.color = new Color(.45f, .87f, 1f);
            primalManaText.raycastTarget = false;
            primalManaText.rectTransform.anchorMin = Vector2.zero;
            primalManaText.rectTransform.anchorMax = Vector2.one;
            primalManaText.rectTransform.offsetMin = primalManaText.rectTransform.offsetMax = Vector2.zero;
            TMPFontManager.ApplyFont(primalManaText);
        }
        primalManaIcon.gameObject.SetActive(active);
        if (!active) return false;
        primalManaText.color = status.UsesCapacitor ? new Color(1f, .83f, .35f) : new Color(.45f, .87f, 1f);
        primalManaText.text = status.UsesCapacitor ? "축전\n" + status.CapacitorCharge : "마력\n" + status.PrimalMana;
        primalManaIcon.GetComponent<BattleHoverTooltip>().SetContent(status.UsesCapacitor
            ? $"축전 {status.CapacitorCharge} / {status.MaxCapacitor}\n자기 턴 시작 시 +{status.GetPassiveEntry(PassiveTrait.IronCapacitor)?.value:0}\n{(status.IsTraitAwakened ? "자신을 포함한 아군이" : "자신이")} 보호막으로 한 번에 5 이상 흡수하면 +1\n전투마다 초기화"
            : $"원초 마력 {status.PrimalMana} / {status.PrimalManaCapacity}\n주사위 사용 후 효과 눈금만큼 마력 획득 · 전투 종료 후에도 유지\n20 이상: 마술의 땅 전체 공격 / 30 이상: 근원 사용 가능");
        return true;
    }

    private void Awake()
    {
        EnsureLocalDisplayImage();
        EnsureAllyMarkerSpace();
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
        if (status.origin != null && !status.origin.isEnemy) EnsureAllyMarkerSpace();

        if (displayImage != null && status.origin != null)
        {
            Sprite sprite = (status.origin.isEnemy || isStandingIllust)
                ? status.origin.standingSprite
                : status.origin.PortraitSprite;
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
    // Selection artwork may overlap the illustration; it must not enlarge the formation's HUD envelope.
    private const float EnemyHeaderOffset = 56f;
    public float EnemyHUDHeight => EnemyHeaderOffset + 44f;
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
        barRoot.anchoredPosition = new Vector2(bounds.center.x, bounds.yMax + EnemyHeaderOffset);
        GetComponent<EnemyEnhancementUI>()?.SetPosition(new Vector2(bounds.center.x, bounds.yMin - 64f));
        var hpRect = barUI.hpSlider.GetComponent<RectTransform>();
        var mentalRect = barUI.mentalSlider.GetComponent<RectTransform>();
        SetCompactBarHeight(hpRect);
        SetCompactBarHeight(mentalRect);
        mentalRect.anchoredPosition = new Vector2(0f, -18f);
        if (defenseRoot != null)
        {
            defenseRoot.anchoredPosition = new Vector2(0f, -40f);
            defenseRoot.localScale = Vector3.one * 0.9f;
        }
        if (statusIconRoot != null)
        {
            statusIconRoot.SetParent(transform, false);
            statusIconRoot.anchorMin = statusIconRoot.anchorMax = statusIconRoot.pivot = Vector2.one * 0.5f;
            statusIconRoot.anchoredPosition = new Vector2(bounds.center.x, bounds.yMin - 26f);
        }
        GetComponent<EnemyIntentUI>()?.SetPosition(barRoot.anchoredPosition + new Vector2(0f, 32f));
        GetComponent<EnemyNameHoverUI>()?.SetLabelPosition(new Vector2(bounds.center.x, bounds.yMin + 20f));
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
        if (statusText != null) statusText.gameObject.SetActive(false);
        if (status?.origin != null && status.origin.isEnemy)
        {
            UpdateEnemyStatusIcons(status);
            return;
        }
        UpdateStatusIcons(status);
    }

    private bool UpdatePanicIcon(CharacterStatus status)
    {
        bool active = status?.panic != null && status.panic.IsActive;
        if (!active && panicIcon == null) return false;
        if (panicIcon == null)
        {
            var root=new GameObject("Panic Icon",typeof(RectTransform),typeof(Image),typeof(BattleHoverTooltip),typeof(PanicIconPulse));
            root.transform.SetParent(statusIconRoot,false);
            panicIcon=root.GetComponent<Image>();panicIcon.color=new Color(.25f,.055f,.09f,.98f);
            panicIcon.rectTransform.sizeDelta=Vector2.one*34f;
            panicIcon.rectTransform.anchorMin=panicIcon.rectTransform.anchorMax=Vector2.one*.5f;
            panicTooltip=root.GetComponent<BattleHoverTooltip>();
            var symbol=new GameObject("Symbol",typeof(RectTransform),typeof(PanicIconGraphic));symbol.transform.SetParent(root.transform,false);
            panicSymbol=symbol.GetComponent<PanicIconGraphic>();panicSymbol.color=new Color(1,.65f,.61f);panicSymbol.raycastTarget=false;
            panicSymbol.rectTransform.anchorMin=Vector2.zero;panicSymbol.rectTransform.anchorMax=Vector2.one;
            panicSymbol.rectTransform.offsetMin=Vector2.one*3;panicSymbol.rectTransform.offsetMax=Vector2.one*-3;
        }
        panicIcon.gameObject.SetActive(active);
        if (!active) return false;
        panicSymbol.SetType(status.panic.Type);
        panicTooltip.SetContent("패닉 · "+TheLastArk.Battle.PanicState.Name(status.panic.Type)+"\n"+TheLastArk.Battle.PanicState.Description(status.panic.Type)+"\n정신력을 회복하면 해제");
        return true;
    }

    private void UpdateEnemyStatusIcons(CharacterStatus status) => DrawStatusIcons(status);

    public void PulsePanicIcon()
    {
        if (panicIcon != null && panicIcon.gameObject.activeInHierarchy)
            panicIcon.GetComponent<PanicIconPulse>()?.Play();
    }

    private static int StatusPriority(EffectType type)
        => type == EffectType.Stun ? 0 : type == EffectType.Blockade ? 1 : 2;

    private void DrawStatusIcons(CharacterStatus status)
    {
        EnsureStatusIconRoot();
        if (statusIconRoot == null || status == null) return;
        var summaries = status.GetStatusSummaries();
        summaries.Sort((a, b) =>
        {
            int priority = StatusPriority(a.type).CompareTo(StatusPriority(b.type));
            return priority != 0 ? priority : a.type.CompareTo(b.type);
        });
        bool panicking=UpdatePanicIcon(status);
        bool mana = UpdatePrimalManaIcon(status);
        int reserved = (panicking ? 1 : 0) + (mana ? 1 : 0);
        int limit=VisibleStatusLimit-reserved;
        bool overflow = summaries.Count > limit;
        int count = Mathf.Min(summaries.Count, limit) + (overflow ? 1 : 0);
        int total=count+reserved;
        if (panicking) panicIcon.rectTransform.anchoredPosition=new Vector2(-(total-1)*.5f*42f,0);
        if (mana) primalManaIcon.rectTransform.anchoredPosition = new Vector2(((panicking ? 1 : 0) - (total - 1) * .5f) * 42f, 0);
        while (statusIcons.Count < count)
        {
            var root = new GameObject("Status Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(BattleHoverTooltip));
            root.transform.SetParent(statusIconRoot, false);
            var icon = root.GetComponent<UnityEngine.UI.Image>();
            icon.preserveAspect = true;
            icon.rectTransform.sizeDelta = new Vector2(34f, 34f);
            statusIcons.Add(icon);
            var bubble = new GameObject("Count Background", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            bubble.transform.SetParent(root.transform, false);
            var bubbleImage = bubble.GetComponent<UnityEngine.UI.Image>();
            bubbleImage.color = new Color(0.04f, 0.06f, 0.1f, 0.95f);
            bubbleImage.raycastTarget = false;
            bubbleImage.rectTransform.anchorMin = bubbleImage.rectTransform.anchorMax = new Vector2(1f, 0f);
            bubbleImage.rectTransform.sizeDelta = new Vector2(18f, 18f);
            statusCountBackgrounds.Add(bubbleImage);
            var textObject = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(root.transform, false);
            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 15f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 15f;
            TMPFontManager.ApplyFont(label);
            statusCounts.Add(label);
        }
        statusIconRoot.sizeDelta = new Vector2(210f, 36f);
        for (int i = 0; i < statusIcons.Count; i++)
        {
            var icon = statusIcons[i];
            icon.gameObject.SetActive(i < count);
            if (i >= count) continue;
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = Vector2.one * 0.5f;
            icon.rectTransform.anchoredPosition = new Vector2((i + reserved - (total - 1) * 0.5f) * 42f, 0f);
            bool more = overflow && i == limit;
            var effect = more ? null : status.GetStatus(summaries[i].type);
            icon.sprite = effect == null ? null : effect.displayIcon != null ? effect.displayIcon : IconLibrary.FindIcon(effect.effectType);
            icon.color = icon.sprite != null ? Color.white : new Color(0.08f, 0.14f, 0.21f, 0.96f);
            string text;
            string detail;
            if (more)
            {
                text = "+" + (summaries.Count - limit);
                var details = new StringBuilder();
                for (int j = limit; j < summaries.Count; j++)
                {
                    if (details.Length > 0) details.AppendLine();
                    details.Append(summaries[j].breakdown);
                }
                detail = details.ToString();
            }
            else
            {
                float value = status.GetStatusValue(effect.effectType);
                text = value > 0f ? Mathf.Floor(value + 1e-4f).ToString("0")
                    : effect.durationType == StatusDurationType.Turns ? effect.remainingTurns.ToString()
                    : effect.durationType == StatusDurationType.Charges || effect.effectType == EffectType.Evasion ? effect.remainingCharges.ToString() : "";
                if (icon.sprite == null) text = ActiveStatusEffect.GetDisplayName(effect.effectType) + text;
                detail = summaries[i].breakdown;
            }
            statusCounts[i].text = text;
            bool badge = icon.sprite != null;
            statusCounts[i].rectTransform.anchorMin = statusCounts[i].rectTransform.anchorMax = badge ? new Vector2(1f, 0f) : Vector2.one * 0.5f;
            statusCounts[i].rectTransform.sizeDelta = badge ? new Vector2(24f, 20f) : new Vector2(34f, 34f);
            statusCountBackgrounds[i].gameObject.SetActive(badge && text.Length > 0);
            icon.GetComponent<BattleHoverTooltip>().SetContent(detail);
        }
        statusIconRoot.gameObject.SetActive(total > 0);
    }

    private void UpdateStatusIcons(CharacterStatus status)
    {
        DrawStatusIcons(status);
        if (statusIconRoot == null || barUI == null || barUI.mentalSlider == null) return;
        var mental = barUI.mentalSlider.GetComponent<RectTransform>();
        statusIconRoot.SetParent(mental.parent, false);
        statusIconRoot.anchorMin = mental.anchorMin;
        statusIconRoot.anchorMax = mental.anchorMax;
        statusIconRoot.pivot = Vector2.one * 0.5f;
        statusIconRoot.localScale = Vector3.one * 0.68f;
        statusIconRoot.anchoredPosition = mental.anchoredPosition + new Vector2(0f, -29f);
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
        defenseRoot.sizeDelta = new Vector2(144f, 30f);

        armorText = CreateDefenseSlot("Armor", -40f, IconLibrary.FindArmorIcon());
        magicResistText = CreateDefenseSlot("Magic Resist", 40f, IconLibrary.FindMagicResistIcon());
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
        text.fontSize = 18f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.BottomRight;
        text.color = Color.white;
        text.outlineColor = Color.black;
        text.outlineWidth = 0.2f;
        text.raycastTarget = false;
        TMPFontManager.ApplyFont(text);

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = textRect.anchorMax = new Vector2(1f, 0.5f);
        textRect.anchoredPosition = new Vector2(18f, 0f);
        textRect.sizeDelta = new Vector2(32f, 28f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        image.raycastTarget = true;
        slot.AddComponent<BattleHoverTooltip>().SetContent(name == "Armor" ? "방어력" : "마법 저항력");
        return text;
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
        if (targeted) EnsureAllyMarkerSpace();
        if (targeted && enemyTargetMarker == null)
            CreateDefaultEnemyTargetMarker();

        if (enemyTargetMarker != null)
        {
            if (!targeted) SetEnemyTargetHighlighted(false);
            enemyTargetMarker.SetActive(targeted);
        }
    }

    public void SetEnemyTargetHighlighted(bool highlighted)
    {
        if (enemyTargetMarker != null)
            enemyTargetMarker.transform.localScale = Vector3.one * (highlighted ? 1.15f : 1f);
    }

    private void EnsureAllyMarkerSpace()
    {
        if (allyMarkerSpaceReserved || barUI == null || barUI.hpSlider == null || barUI.mentalSlider == null
            || GetComponent<EnemyBattleCharacter>() != null) return;
        allyMarkerSpaceReserved = true;
        ReserveBadgeSpace(barUI.hpSlider.GetComponent<RectTransform>());
        ReserveBadgeSpace(barUI.mentalSlider.GetComponent<RectTransform>());
    }

    private static void ReserveBadgeSpace(RectTransform bar)
    {
        // Keep the right edge fixed, leaving a stable icon slot inside the existing HUD area.
        float reserved = Mathf.Min(TargetBadgeSpace, Mathf.Max(0f, bar.rect.width - 100f));
        bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bar.rect.width - reserved);
        bar.anchoredPosition += new Vector2(reserved * (1f - bar.pivot.x), 0f);
        var border = bar.Find("Border") as RectTransform;
        if (border != null)
        {
            border.anchorMin = new Vector2(0f, border.anchorMin.y);
            border.anchorMax = new Vector2(1f, border.anchorMax.y);
            border.sizeDelta = new Vector2(2f, border.sizeDelta.y);
        }
    }

    [ContextMenu("Debug: Toggle Enemy Target Marker")]
    private void DebugToggleEnemyTargetMarker()
    {
        SetEnemyTargeted(enemyTargetMarker == null || !enemyTargetMarker.activeSelf);
    }

    private void CreateDefaultEnemyTargetMarker()
    {
        // Enemy intent is a HUD warning, not a second player-selection arrow.
        // Anchor it to the health bar so it cannot cover the portrait's face.
        Transform parent = barUI != null && barUI.hpSlider != null
            ? barUI.hpSlider.transform : transform;

        if (enemyTargetMarkerPrefab != null)
        {
            enemyTargetMarker = Instantiate(enemyTargetMarkerPrefab, parent, false);
        }
        else
        {
            enemyTargetMarker = new GameObject("Enemy Target Marker", typeof(RectTransform), typeof(Image));
            enemyTargetMarker.transform.SetParent(parent, false);
            Image image = enemyTargetMarker.GetComponent<Image>();
            image.sprite = Resources.Load<Sprite>("UI/BattleMarkers/EnemyThreat");
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = true;
        }

        RectTransform rect = enemyTargetMarker.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, .5f);
            rect.pivot = new Vector2(1f, .5f);
            rect.anchoredPosition = new Vector2(-4f, 0f);
            rect.sizeDelta = new Vector2(32f, 32f);
        }
        var tooltip = enemyTargetMarker.GetComponent<BattleHoverTooltip>();
        if (tooltip == null) tooltip = enemyTargetMarker.AddComponent<BattleHoverTooltip>();
        tooltip.SetContent("적의 대상\n적이 다음 행동의 대상으로 지정한 아군입니다.\n적의 의도 아이콘에서 행동 내용을 확인할 수 있습니다.");
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
