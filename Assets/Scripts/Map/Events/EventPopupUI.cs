using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TheLastArk.Map.Events;

/// <summary>
/// 맵 씬 위에 팝업 창으로 이벤트를 표시하는 컴포넌트.
/// MapManager에서 호출되어 동적으로 생성됩니다.
///
/// 레이아웃:
///   - 뒤: 반투명 어두운 오버레이 (맵이 비침)
///   - 중간: 이벤트 창 패널 (화면 중앙, 약간 작게)
///     - 상단 60%: 이벤트 이미지
///     - 이미지 위 하단: 검은 그라데이션
///     - 하단: 제목 + 설명 + 선택지
/// </summary>
public class EventPopupUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // 색상 상수
    // ─────────────────────────────────────────────

    private static readonly Color OVERLAY_COLOR = new Color(0, 0, 0, 0.6f);
    private static readonly Color PANEL_BG = new Color(0.08f, 0.08f, 0.12f, 0.97f);
    private static readonly Color IMAGE_PLACEHOLDER = new Color(0.15f, 0.15f, 0.22f);
    private static readonly Color TITLE_COLOR = new Color(1f, 0.85f, 0.2f);
    private static readonly Color DESC_COLOR = new Color(0.88f, 0.88f, 0.92f);
    private static readonly Color DIVIDER_COLOR = new Color(1f, 0.85f, 0.2f, 0.35f);
    private static readonly Color BTN_NORMAL = new Color(0.14f, 0.16f, 0.24f, 0.9f);
    private static readonly Color BTN_HOVER = new Color(0.22f, 0.26f, 0.38f, 0.95f);
    private static readonly Color BTN_TEXT = new Color(0.95f, 0.95f, 0.95f);
    private static readonly Color RESULT_SUCCESS = new Color(0.4f, 0.9f, 0.5f);
    private static readonly Color RESULT_FAIL = new Color(0.9f, 0.4f, 0.4f);
    private static readonly Color RESULT_NEUTRAL = new Color(0.85f, 0.85f, 0.9f);

    // ─────────────────────────────────────────────
    // 내부 참조
    // ─────────────────────────────────────────────

    private TMPro.TMP_FontAsset mainFont;
    private GameEventData currentEvent;
    private System.Action onClose;                // 팝업 닫힐 때 콜백

    // UI
    private Canvas popupCanvas;
    private Image backgroundImage;                // 이벤트 이미지
    private TMPro.TextMeshProUGUI titleText;
    private TMPro.TextMeshProUGUI descriptionText;
    private GameObject optionsContainer;
    private List<Button> optionButtons = new List<Button>();

    // 결과
    private GameObject eventContentArea;
    private GameObject resultContentArea;
    private TMPro.TextMeshProUGUI resultText;

    // ─────────────────────────────────────────────
    // 공개 API
    // ─────────────────────────────────────────────

    /// <summary>
    /// 이벤트 팝업을 생성하고 표시합니다.
    /// </summary>
    /// <param name="eventData">표시할 이벤트 데이터</param>
    /// <param name="closeCallback">팝업 닫힐 때 호출될 콜백</param>
    public static EventPopupUI Show(GameEventData eventData, System.Action closeCallback = null)
    {
        GameObject popupObj = new GameObject("EventPopup");
        EventPopupUI popup = popupObj.AddComponent<EventPopupUI>();
        popup.currentEvent = eventData;
        popup.onClose = closeCallback;
        popup.Initialize();
        return popup;
    }

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────

    private void Initialize()
    {
        mainFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Main_Fonts");
        BuildUI();
        ShowEvent();
    }

    private void BuildUI()
    {
        // ═══════════════════════════════════════════
        // 최상위 Canvas (기존 UI 위에 렌더링)
        // ═══════════════════════════════════════════
        popupCanvas = gameObject.AddComponent<Canvas>();
        popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        popupCanvas.sortingOrder = 100;  // 맵 UI보다 위에 표시

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // ═══════════════════════════════════════════
        // 레이어 1: 반투명 어두운 오버레이 (맵이 비침)
        // ═══════════════════════════════════════════
        GameObject overlayObj = new GameObject("DarkOverlay");
        overlayObj.transform.SetParent(transform, false);
        Image overlayImg = overlayObj.AddComponent<Image>();
        overlayImg.color = OVERLAY_COLOR;
        overlayImg.raycastTarget = true;  // 뒤쪽 클릭 차단
        SetFullScreen(overlayObj.GetComponent<RectTransform>());

        // ═══════════════════════════════════════════
        // 레이어 2: 이벤트 창 패널 (중앙)
        // ═══════════════════════════════════════════
        GameObject panelObj = new GameObject("EventPanel");
        panelObj.transform.SetParent(transform, false);
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0); // 투명 (이미지가 전체를 덮으므로)

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        // 앵커 기반 크기 (화면의 약 90%)
        panelRect.anchorMin = new Vector2(0.05f, 0.06f);
        panelRect.anchorMax = new Vector2(0.95f, 0.94f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // ─── 이미지 영역 (패널 전체) ───
        GameObject imageArea = new GameObject("ImageArea");
        imageArea.transform.SetParent(panelObj.transform, false);
        RectTransform imgAreaRect = imageArea.AddComponent<RectTransform>();
        imgAreaRect.anchorMin = Vector2.zero;
        imgAreaRect.anchorMax = Vector2.one;
        imgAreaRect.offsetMin = Vector2.zero;
        imgAreaRect.offsetMax = Vector2.zero;

        // 마스크 (이미지가 영역을 넘어가면 잘라냄)
        Image areaBg = imageArea.AddComponent<Image>();
        areaBg.color = IMAGE_PLACEHOLDER;
        areaBg.raycastTarget = false;
        RectMask2D mask = imageArea.AddComponent<RectMask2D>();

        // 이벤트 이미지 (자식으로 배치, 비율 유지하면서 영역 꽉 채움)
        GameObject imgObj = new GameObject("EventImage");
        imgObj.transform.SetParent(imageArea.transform, false);
        backgroundImage = imgObj.AddComponent<Image>();
        backgroundImage.color = IMAGE_PLACEHOLDER;
        backgroundImage.preserveAspect = true;
        backgroundImage.raycastTarget = false;
        RectTransform imgRect = imgObj.GetComponent<RectTransform>();
        imgRect.anchorMin = new Vector2(0.5f, 0.5f);
        imgRect.anchorMax = new Vector2(0.5f, 0.5f);
        imgRect.pivot = new Vector2(0.5f, 0.5f);

        // ─── 하단 검은 그라데이션 (패널 위에 직접 배치) ───
        CreateGradientOverlay(panelObj.transform);

        // ─── 텍스트+선택지 영역 (패널 하단) ───
        BuildContentArea(panelObj.transform);

        // ─── 결과 영역 (초기 숨김) ───
        BuildResultArea(panelObj.transform);
    }

    /// <summary>
    /// 패널 하단에 검은 그라데이션 오버레이.
    /// 아래쪽은 진한 검정, 위로 갈수록 투명하게 사라집니다.
    /// </summary>
    private void CreateGradientOverlay(Transform parent)
    {
        GameObject gradObj = new GameObject("GradientOverlay");
        gradObj.transform.SetParent(parent, false);
        Image gradImg = gradObj.AddComponent<Image>();
        gradImg.raycastTarget = false;

        // 그라데이션 텍스처 (하단 불투명 검정 → 상단 완전 투명)
        int h = 256;
        Texture2D tex = new Texture2D(1, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / (h - 1);
            // 비선형 커브: 아래쪽이 더 진하게 유지되다가 위로 갈수록 빠르게 사라짐
            float alpha = 1f - Mathf.Pow(t, 0.55f);
            tex.SetPixel(0, y, new Color(0, 0, 0, alpha * 0.93f));
        }
        tex.Apply();
        gradImg.sprite = Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f));
        gradImg.type = Image.Type.Sliced;

        // 패널 하단 65% 차지
        RectTransform gRect = gradObj.GetComponent<RectTransform>();
        gRect.anchorMin = new Vector2(0, 0);
        gRect.anchorMax = new Vector2(1, 0.65f);
        gRect.offsetMin = Vector2.zero;
        gRect.offsetMax = Vector2.zero;
    }

    private void BuildContentArea(Transform panelParent)
    {
        eventContentArea = new GameObject("EventContent");
        eventContentArea.transform.SetParent(panelParent, false);
        RectTransform cRect = eventContentArea.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 0);
        cRect.anchorMax = new Vector2(1, 0.45f);
        cRect.offsetMin = new Vector2(35, 20);
        cRect.offsetMax = new Vector2(-35, 0);

        VerticalLayoutGroup vlg = eventContentArea.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.childAlignment = TextAnchor.LowerCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // 제목
        titleText = CreateText(eventContentArea.transform, "Title", "", 32, TITLE_COLOR);
        titleText.fontStyle = TMPro.FontStyles.Bold;
        titleText.alignment = TMPro.TextAlignmentOptions.Bottom;
        SetPreferredHeight(titleText.gameObject, 45);

        // 설명
        descriptionText = CreateText(eventContentArea.transform, "Desc", "", 20, DESC_COLOR);
        descriptionText.alignment = TMPro.TextAlignmentOptions.Top;
        SetPreferredHeight(descriptionText.gameObject, 65);

        // 선택지 컨테이너 (2x2 그리드 배열)
        optionsContainer = new GameObject("Options");
        optionsContainer.transform.SetParent(eventContentArea.transform, false);
        optionsContainer.AddComponent<RectTransform>();
        GridLayoutGroup oGrid = optionsContainer.AddComponent<GridLayoutGroup>();
        oGrid.cellSize = new Vector2(810, 48);   // 반반 차지하도록 폭 넓게 설정
        oGrid.spacing = new Vector2(15, 10);
        oGrid.childAlignment = TextAnchor.UpperCenter;
        oGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        oGrid.constraintCount = 2; // 최대 열 2개

        LayoutElement oLE = optionsContainer.AddComponent<LayoutElement>();
        oLE.preferredHeight = 110;
        oLE.flexibleWidth = 1;
    }

    private void BuildResultArea(Transform panelParent)
    {
        resultContentArea = new GameObject("ResultContent");
        resultContentArea.transform.SetParent(panelParent, false);
        RectTransform rRect = resultContentArea.AddComponent<RectTransform>();
        rRect.anchorMin = new Vector2(0, 0);
        rRect.anchorMax = new Vector2(1, 0.45f);
        rRect.offsetMin = new Vector2(35, 20);
        rRect.offsetMax = new Vector2(-35, 0);

        VerticalLayoutGroup vlg = resultContentArea.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.childAlignment = TextAnchor.LowerCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // 결과 제목
        TMPro.TextMeshProUGUI rTitle = CreateText(resultContentArea.transform, "RTitle", "결과", 30, TITLE_COLOR);
        rTitle.fontStyle = TMPro.FontStyles.Bold;
        rTitle.alignment = TMPro.TextAlignmentOptions.Bottom;
        SetPreferredHeight(rTitle.gameObject, 45);

        CreateDivider(resultContentArea.transform);

        // 결과 텍스트
        resultText = CreateText(resultContentArea.transform, "RText", "", 21, RESULT_NEUTRAL);
        resultText.alignment = TMPro.TextAlignmentOptions.Top;
        SetPreferredHeight(resultText.gameObject, 110);

        // 돌아가기 버튼 (하단 고정)
        GameObject returnBtnObj = new GameObject("ReturnBtnContainer");
        returnBtnObj.transform.SetParent(resultContentArea.transform, false);
        returnBtnObj.AddComponent<RectTransform>();
        HorizontalLayoutGroup hlg = returnBtnObj.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        LayoutElement rLE = returnBtnObj.AddComponent<LayoutElement>();
        rLE.preferredHeight = 50;

        Button returnBtn = CreateOptionButton(returnBtnObj.transform, "계속하기", OnClosePopup, 48);
        LayoutElement btnLE = returnBtn.gameObject.AddComponent<LayoutElement>();
        btnLE.preferredWidth = 400; // 버튼 가로 크기 제한

        resultContentArea.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // 이벤트 표시
    // ─────────────────────────────────────────────

    private void ShowEvent()
    {
        titleText.text = currentEvent.eventTitle;
        descriptionText.text = currentEvent.eventDescription;

        // 이벤트 이미지
        if (currentEvent.eventImage != null)
        {
            backgroundImage.sprite = currentEvent.eventImage;
            backgroundImage.color = Color.white;
            // 1프레임 후에 커버 스케일 적용 (레이아웃 빌드 대기)
            StartCoroutine(ApplyCoverScale());
        }

        // 선택지
        ClearOptions();
        for (int i = 0; i < currentEvent.options.Count; i++)
        {
            EventOption option = currentEvent.options[i];
            int idx = i;

            string text = $"{i + 1}.  {option.optionText}";
            if (option.requirementType != EventRequirementType.None)
            {
                string req = option.requirementType == EventRequirementType.RequireGold
                    ? $"(골드 {option.requirementValue} 필요)"
                    : $"(HP {option.requirementValue} 필요)";
                text += $"  <size=15><color=#888>{req}</color></size>";
            }

            Button btn = CreateOptionButton(optionsContainer.transform, text, () => OnOptionSelected(idx), 38);
            optionButtons.Add(btn);
        }
    }

    // ─────────────────────────────────────────────
    // 선택 / 결과
    // ─────────────────────────────────────────────

    private void OnOptionSelected(int index)
    {
        if (index < 0 || index >= currentEvent.options.Count) return;

        EventOption option = currentEvent.options[index];
        EventOutcome outcome = EventManager.Instance.ResolveOutcome(option);
        EventManager.Instance.ApplyRewards(outcome);

        // 이벤트 내용 숨기고 결과 표시
        eventContentArea.SetActive(false);
        resultContentArea.SetActive(true);

        Color textColor = RESULT_NEUTRAL;
        if (outcome.rewards != null && outcome.rewards.Count > 0)
        {
            textColor = RESULT_SUCCESS; // 기본적으로 성공색
            foreach (var r in outcome.rewards)
            {
                if (r.rewardType == EventRewardType.TakeDamage ||
                    r.rewardType == EventRewardType.LoseGold ||
                    r.rewardType == EventRewardType.TakeMentalDamage ||
                    r.rewardType == EventRewardType.UpgradeNextBattles)
                {
                    // 부정적 보상이 하나라도 포함되어 있으면 붉은색
                    textColor = RESULT_FAIL;
                    break;
                }
            }
        }

        string msg = outcome.outcomeText;
        string summary = GetRewardSummary(outcome);
        if (!string.IsNullOrEmpty(summary))
            msg += $"\n\n{summary}";

        resultText.text = msg;
        resultText.color = textColor;
    }

    private void OnClosePopup()
    {
        onClose?.Invoke();
        Destroy(gameObject);
    }

    /// <summary>
    /// 레이아웃이 빌드된 후 이미지를 영역에 꽉 차게 스케일합니다.
    /// 비율을 유지하면서 영역을 완전히 덮도록 합니다 (CSS object-fit: cover).
    /// </summary>
    private IEnumerator ApplyCoverScale()
    {
        // 레이아웃 빌드 대기
        yield return null;
        yield return null;

        RectTransform imgRect = backgroundImage.GetComponent<RectTransform>();
        RectTransform areaRect = backgroundImage.transform.parent.GetComponent<RectTransform>();

        Sprite sprite = backgroundImage.sprite;
        if (sprite == null) yield break;

        float areaW = areaRect.rect.width;
        float areaH = areaRect.rect.height;
        float spriteW = sprite.rect.width;
        float spriteH = sprite.rect.height;

        if (areaW <= 0 || areaH <= 0 || spriteW <= 0 || spriteH <= 0) yield break;

        float areaAspect = areaW / areaH;
        float spriteAspect = spriteW / spriteH;

        float finalW, finalH;

        if (spriteAspect > areaAspect)
        {
            // 이미지가 영역보다 가로로 길다 → 높이 맞추고 가로 넘침 (잘려나감)
            finalH = areaH;
            finalW = areaH * spriteAspect;
        }
        else
        {
            // 이미지가 영역보다 세로로 길다 → 가로 맞추고 세로 넘침
            finalW = areaW;
            finalH = areaW / spriteAspect;
        }

        imgRect.sizeDelta = new Vector2(finalW, finalH);
    }

    private string GetRewardSummary(EventOutcome o)
    {
        if (o.rewards == null || o.rewards.Count == 0) return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var r in o.rewards)
        {
            switch (r.rewardType)
            {
                case EventRewardType.HealHP:           sb.AppendLine($"<color=#66EE77>♥ 모든 아군 HP +{r.rewardValue}</color>"); break;
                case EventRewardType.TakeDamage:       sb.AppendLine($"<color=#EE6666>♥ 모든 아군 HP -{r.rewardValue}</color>"); break;
                case EventRewardType.GainGold:         sb.AppendLine($"<color=#FFD700>● 골드 +{r.rewardValue}</color>"); break;
                case EventRewardType.LoseGold:         sb.AppendLine($"<color=#EE6666>● 골드 -{r.rewardValue}</color>"); break;
                case EventRewardType.GainRelic:        sb.AppendLine($"<color=#BB77FF>★ 유물 획득: {r.rewardDataID}</color>"); break;
                case EventRewardType.GainCard:         sb.AppendLine($"<color=#77BBFF>◆ 카드 획득: {r.rewardDataID}</color>"); break;
                case EventRewardType.GainConsumable:   sb.AppendLine($"<color=#77EEFF>■ 소모품 획득: {r.rewardDataID}</color>"); break;
                case EventRewardType.UpgradeTrainCar:  sb.AppendLine($"<color=#FFAA33>▲ 기차 칸 강화!</color>"); break;
                case EventRewardType.TakeMentalDamage: sb.AppendLine($"<color=#CC66EE>♦ 모든 아군 정신력 -{r.rewardValue}</color>"); break;
                case EventRewardType.UpgradeNextBattles:sb.AppendLine($"<color=#EE6666>⚔ 다음 {r.rewardValue}회 전투가 강적 전투로 대체!</color>"); break;
            }
        }
        return sb.ToString().TrimEnd();
    }

    // ─────────────────────────────────────────────
    // UI 유틸
    // ─────────────────────────────────────────────

    private TMPro.TextMeshProUGUI CreateText(Transform parent, string name, string content, float size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        TMPro.TextMeshProUGUI tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.richText = true;
        if (mainFont != null) tmp.font = mainFont;
        return tmp;
    }

    private void CreateDivider(Transform parent)
    {
        GameObject d = new GameObject("Divider");
        d.transform.SetParent(parent, false);
        d.AddComponent<RectTransform>();
        Image img = d.AddComponent<Image>();
        img.color = DIVIDER_COLOR;
        img.raycastTarget = false;
        LayoutElement le = d.AddComponent<LayoutElement>();
        le.preferredHeight = 2;
        le.flexibleWidth = 1;
    }

    private Button CreateOptionButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick, float height)
    {
        GameObject btnObj = new GameObject("Btn");
        btnObj.transform.SetParent(parent, false);
        btnObj.AddComponent<RectTransform>();

        Image bg = btnObj.AddComponent<Image>();
        bg.color = BTN_NORMAL;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = BTN_NORMAL;
        cb.highlightedColor = BTN_HOVER;
        cb.pressedColor = new Color(0.10f, 0.12f, 0.18f, 0.9f);
        cb.selectedColor = BTN_HOVER;
        btn.colors = cb;
        btn.targetGraphic = bg;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform tRect = txtObj.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = new Vector2(18, 4);
        tRect.offsetMax = new Vector2(-18, -4);

        TMPro.TextMeshProUGUI tmp = txtObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 19;
        tmp.color = BTN_TEXT;
        tmp.alignment = TMPro.TextAlignmentOptions.Midline; // 버튼 텍스트 중앙 정렬
        tmp.enableWordWrapping = true;
        tmp.richText = true;
        if (mainFont != null) tmp.font = mainFont;

        btn.onClick.AddListener(onClick);
        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleHeight = 0;  // 남은 공간에 의해 늘어나지 않도록 고정
        return btn;
    }

    private void SetPreferredHeight(GameObject obj, float h)
    {
        LayoutElement le = obj.GetComponent<LayoutElement>();
        if (le == null) le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = h;
    }

    private void SetFullScreen(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    private void ClearOptions()
    {
        foreach (var b in optionButtons)
            if (b != null) Destroy(b.gameObject);
        optionButtons.Clear();
    }
}
