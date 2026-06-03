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
        popupCanvas = gameObject.AddComponent<Canvas>();
        popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        popupCanvas.sortingOrder = 100;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // 레이어 1: 반투명 어두운 오버레이
        GameObject overlayObj = new GameObject("DarkOverlay");
        overlayObj.transform.SetParent(transform, false);
        Image overlayImg = overlayObj.AddComponent<Image>();
        overlayImg.color = OVERLAY_COLOR;
        overlayImg.raycastTarget = true;
        SetFullScreen(overlayObj.GetComponent<RectTransform>());

        // 레이어 2: 메인 패널 (화면 중앙 90%)
        GameObject panelObj = new GameObject("EventPanel");
        panelObj.transform.SetParent(transform, false);
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = PANEL_BG; // 배경색 설정
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0.05f);
        panelRect.anchorMax = new Vector2(0.95f, 0.95f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // ─── 좌측 50%: 이미지 영역 ───
        GameObject imageArea = new GameObject("ImageArea");
        imageArea.transform.SetParent(panelObj.transform, false);
        RectTransform imgAreaRect = imageArea.AddComponent<RectTransform>();
        imgAreaRect.anchorMin = new Vector2(0, 0);
        imgAreaRect.anchorMax = new Vector2(0.5f, 1);
        imgAreaRect.offsetMin = Vector2.zero;
        imgAreaRect.offsetMax = Vector2.zero;

        Image areaBg = imageArea.AddComponent<Image>();
        areaBg.color = IMAGE_PLACEHOLDER;
        areaBg.raycastTarget = false;
        imageArea.AddComponent<RectMask2D>();

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

        // ─── 이미지 우측 경계 그라데이션 (자연스러운 블렌딩) ───
        CreateGradientOverlay(panelObj.transform);

        // ─── 우측 50%: 텍스트 및 선택지 영역 ───
        BuildContentArea(panelObj.transform);
        BuildResultArea(panelObj.transform);
    }

    private void CreateGradientOverlay(Transform parent)
    {
        GameObject gradObj = new GameObject("GradientOverlay");
        gradObj.transform.SetParent(parent, false);
        Image gradImg = gradObj.AddComponent<Image>();
        gradImg.raycastTarget = false;

        // 가로 그라데이션 (왼쪽 투명 -> 오른쪽 불투명 PANEL_BG)
        int w = 256;
        Texture2D tex = new Texture2D(w, 1, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int x = 0; x < w; x++)
        {
            float t = (float)x / (w - 1);
            float alpha = Mathf.Pow(t, 2f);
            tex.SetPixel(x, 0, new Color(PANEL_BG.r, PANEL_BG.g, PANEL_BG.b, alpha));
        }
        tex.Apply();
        gradImg.sprite = Sprite.Create(tex, new Rect(0, 0, w, 1), new Vector2(0.5f, 0.5f));
        gradImg.type = Image.Type.Sliced;

        // 좌측 50% 영역의 오른쪽에 배치
        RectTransform gRect = gradObj.GetComponent<RectTransform>();
        gRect.anchorMin = new Vector2(0.3f, 0);
        gRect.anchorMax = new Vector2(0.5f, 1);
        gRect.offsetMin = Vector2.zero;
        gRect.offsetMax = Vector2.zero;
    }

    private void BuildContentArea(Transform panelParent)
    {
        eventContentArea = new GameObject("EventContent");
        eventContentArea.transform.SetParent(panelParent, false);
        RectTransform cRect = eventContentArea.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0.5f, 0);
        cRect.anchorMax = new Vector2(1, 1);
        cRect.offsetMin = new Vector2(40, 40);
        cRect.offsetMax = new Vector2(-40, -40);

        VerticalLayoutGroup vlg = eventContentArea.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        // 1. 타이틀은 상단에
        titleText = CreateText(eventContentArea.transform, "Title", "", 36, TITLE_COLOR);
        titleText.fontStyle = TMPro.FontStyles.Bold;
        titleText.alignment = TMPro.TextAlignmentOptions.TopLeft;
        SetPreferredHeight(titleText.gameObject, 50);

        CreateDivider(eventContentArea.transform);

        // 2. 이벤트 텍스트는 타이틀과 버튼 사이 (유연하게 공간 차지)
        descriptionText = CreateText(eventContentArea.transform, "Desc", "", 26, DESC_COLOR);
        descriptionText.alignment = TMPro.TextAlignmentOptions.TopLeft;
        descriptionText.lineSpacing = 60; // 결과창처럼 간격 늘리기
        LayoutElement descLE = descriptionText.gameObject.AddComponent<LayoutElement>();
        descLE.flexibleHeight = 1; // 남는 공간 채움

        // 3. 버튼은 하단에
        optionsContainer = new GameObject("Options");
        optionsContainer.transform.SetParent(eventContentArea.transform, false);
        optionsContainer.AddComponent<RectTransform>();
        VerticalLayoutGroup oVlg = optionsContainer.AddComponent<VerticalLayoutGroup>();
        oVlg.spacing = 10;
        oVlg.childAlignment = TextAnchor.LowerCenter;
        oVlg.childForceExpandWidth = true;
        oVlg.childForceExpandHeight = false;
        oVlg.childControlWidth = true;
        oVlg.childControlHeight = true;
        LayoutElement oLE = optionsContainer.AddComponent<LayoutElement>();
        // oLE.preferredHeight = 250; // 제거: 자식 크기에 맞춰 자동 계산
        oLE.flexibleHeight = 0;
    }

    private void BuildResultArea(Transform panelParent)
    {
        resultContentArea = new GameObject("ResultContent");
        resultContentArea.transform.SetParent(panelParent, false);
        RectTransform rRect = resultContentArea.AddComponent<RectTransform>();
        rRect.anchorMin = new Vector2(0.5f, 0);
        rRect.anchorMax = new Vector2(1, 1);
        rRect.offsetMin = new Vector2(40, 40);
        rRect.offsetMax = new Vector2(-40, -40);

        VerticalLayoutGroup vlg = resultContentArea.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        TMPro.TextMeshProUGUI rTitle = CreateText(resultContentArea.transform, "RTitle", "결과", 36, TITLE_COLOR);
        rTitle.fontStyle = TMPro.FontStyles.Bold;
        rTitle.alignment = TMPro.TextAlignmentOptions.TopLeft;
        SetPreferredHeight(rTitle.gameObject, 50);

        CreateDivider(resultContentArea.transform);

        resultText = CreateText(resultContentArea.transform, "RText", "", 24, RESULT_NEUTRAL);
        resultText.alignment = TMPro.TextAlignmentOptions.TopLeft;
        resultText.lineSpacing = 60;
        LayoutElement resLE = resultText.gameObject.AddComponent<LayoutElement>();
        resLE.flexibleHeight = 1;

        GameObject btnContainer = new GameObject("BtnContainer");
        btnContainer.transform.SetParent(resultContentArea.transform, false);
        VerticalLayoutGroup bVlg = btnContainer.AddComponent<VerticalLayoutGroup>();
        bVlg.childAlignment = TextAnchor.LowerCenter;
        bVlg.childControlHeight = true;
        bVlg.childControlWidth = true;
        bVlg.childForceExpandWidth = true;
        bVlg.childForceExpandHeight = false;
        
        CreateOptionButton(btnContainer.transform, "계속하기", OnClosePopup, 110);

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

            string rewardPreview = GetOptionRewardPreviewText(option);
            if (!string.IsNullOrEmpty(rewardPreview))
            {
                text += $"\n<size=18>{rewardPreview}</size>";
            }

            Button btn = CreateOptionButton(optionsContainer.transform, text, () => OnOptionSelected(idx), 110);
            optionButtons.Add(btn);
        }
    }

    private string GetOptionRewardPreviewText(EventOption option)
    {
        if (option.outcomes == null || option.outcomes.Count == 0) return "";
        
        List<string> goodRewards = new List<string>();
        List<string> badRewards = new List<string>();

        foreach (var outcome in option.outcomes)
        {
            if (outcome.rewards == null) continue;
            foreach (var r in outcome.rewards)
            {
                string text = "";
                bool isGood = true;
                switch (r.rewardType)
                {
                    case EventRewardType.GainGold: text = $"골드 +{r.rewardValue}"; isGood = true; break;
                    case EventRewardType.LoseGold: text = $"골드 -{r.rewardValue}"; isGood = false; break;
                    case EventRewardType.HealHP: text = $"HP 회복"; isGood = true; break;
                    case EventRewardType.TakeDamage: text = $"HP 감소"; isGood = false; break;
                    case EventRewardType.TakeMentalDamage: text = $"정신력 감소"; isGood = false; break;
                    case EventRewardType.GainCard: text = $"카드 획득"; isGood = true; break;
                    case EventRewardType.GainRelic: text = $"유물 획득"; isGood = true; break;
                    case EventRewardType.LoseRelic: text = $"유물 소실"; isGood = false; break;
                    case EventRewardType.GainConsumable: text = $"소모품 획득"; isGood = true; break;
                    case EventRewardType.UpgradeTrainCar: text = $"기차 강화"; isGood = true; break;
                    case EventRewardType.DamageTrainCar: text = $"기차 파손"; isGood = false; break;
                    case EventRewardType.UpgradeNextBattles: text = $"강적 조우"; isGood = false; break;
                    case EventRewardType.GainActionPoints: text = $"행동력 증가"; isGood = true; break;
                }
                
                if (!string.IsNullOrEmpty(text))
                {
                    if (isGood) { if (!goodRewards.Contains(text)) goodRewards.Add(text); }
                    else { if (!badRewards.Contains(text)) badRewards.Add(text); }
                }
            }
        }

        List<string> allRewards = new List<string>();
        foreach (var b in badRewards) allRewards.Add($"<color=#FF5555>{b}</color>");
        foreach (var g in goodRewards) allRewards.Add($"<color=#55CCFF>{g}</color>");

        if (allRewards.Count > 0)
        {
            return string.Join(", ", allRewards);
        }
        return "";
    }

    // ─────────────────────────────────────────────
    // 선택 / 결과
    // ─────────────────────────────────────────────

    private void OnOptionSelected(int index)
    {
        if (index < 0 || index >= currentEvent.options.Count) return;

        EventOption option = currentEvent.options[index];

        // 선택 비용 지불
        if (option.requirementType == EventRequirementType.RequireGold)
        {
            TheLastArk.Managers.ResourceManager.Instance.SpendGold(option.requirementValue);
        }
        else if (option.requirementType == EventRequirementType.RequireHP)
        {
            // TODO: HP 차감 로직 연동 (현재 ResourceManager엔 HP 관리가 없으므로, BattleCharacter 또는 TrainManager 등을 연동)
            // 우선 주석 처리하거나, 필요 시 나중에 구현합니다.
        }

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
                case EventRewardType.HealHP:           sb.Append($"<color=#66EE77>모든 아군 HP +{r.rewardValue}</color>\n"); break;
                case EventRewardType.TakeDamage:       sb.Append($"<color=#EE6666>모든 아군 HP -{r.rewardValue}</color>\n"); break;
                case EventRewardType.GainGold:         sb.Append($"<color=#FFD700>골드 +{r.rewardValue}</color>\n"); break;
                case EventRewardType.LoseGold:         sb.Append($"<color=#EE6666>골드 -{r.rewardValue}</color>\n"); break;
                case EventRewardType.GainRelic:        sb.Append($"<color=#BB77FF>유물 획득: {r.rewardDataID}</color>\n"); break;
                case EventRewardType.GainCard:         sb.Append($"<color=#77BBFF>카드 획득: {r.rewardDataID}</color>\n"); break;
                case EventRewardType.GainConsumable:   sb.Append($"<color=#77EEFF>소모품 획득: {r.rewardDataID}</color>\n"); break;
                case EventRewardType.UpgradeTrainCar:  sb.Append($"<color=#FFAA33>기차 칸 강화!</color>\n"); break;
                case EventRewardType.TakeMentalDamage: sb.Append($"<color=#CC66EE>모든 아군 정신력 -{r.rewardValue}</color>\n"); break;
                case EventRewardType.UpgradeNextBattles:sb.Append($"<color=#EE6666>다음 {r.rewardValue}회 전투가 강적 전투로 대체!</color>\n"); break;
                case EventRewardType.LoseRelic:        sb.Append($"<color=#EE6666>유물 소실: {r.rewardDataID}</color>\n"); break;
                case EventRewardType.DamageTrainCar:   sb.Append($"<color=#EE6666>기차 칸 파손!</color>\n"); break;
                case EventRewardType.GainActionPoints: sb.Append($"<color=#44DDFF>다음 {r.rewardValue}회 전투 행동력 +2</color>\n"); break;
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
        Sprite bgSprite = Resources.Load<Sprite>("Events/Eventbutton");
        if (bgSprite != null)
        {
            bg.sprite = bgSprite;
            bg.type = Image.Type.Sliced;
        }
        else
        {
            bg.color = BTN_NORMAL;
        }

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        cb.selectedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        if (bgSprite == null)
        {
            cb.normalColor = BTN_NORMAL;
            cb.highlightedColor = BTN_HOVER;
            cb.pressedColor = new Color(0.10f, 0.12f, 0.18f, 0.9f);
            cb.selectedColor = BTN_HOVER;
        }
        btn.colors = cb;
        btn.targetGraphic = bg;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform tRect = txtObj.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = new Vector2(80, 20);
        tRect.offsetMax = new Vector2(-40, -20);

        TMPro.TextMeshProUGUI tmp = txtObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 19;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 14;
        tmp.fontSizeMax = 19;
        tmp.color = BTN_TEXT;
        tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
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

#if UNITY_EDITOR
    public static void PreviewInEditor(GameEventData eventData)
    {
        var existing = GameObject.Find("EventPopup_Preview");
        if (existing != null) DestroyImmediate(existing);

        GameObject popupObj = new GameObject("EventPopup_Preview");
        EventPopupUI popup = popupObj.AddComponent<EventPopupUI>();
        popup.currentEvent = eventData;
        popup.Initialize();
    }
#endif
}
