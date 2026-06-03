using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TheLastArk.Map.Events;

/// <summary>
/// EventScene에 배치되는 컴포넌트.
/// 씬 로드 시 자동으로 이벤트 UI를 구성하고 플레이어 선택을 처리합니다.
///
/// 레이아웃:
///   - 뒤: 이벤트 이미지 (화면 전체)
///   - 중간: 하단에서 올라오는 검은 그라데이션 오버레이
///   - 앞: 제목 + 설명 + 선택지 (하단 정렬)
/// </summary>
public class SetupEventScene : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // 색상 & 스타일 상수
    // ─────────────────────────────────────────────

    private static readonly Color BG_COLOR = new Color(0.06f, 0.06f, 0.1f, 1f);
    private static readonly Color TITLE_COLOR = new Color(1f, 0.85f, 0.2f);       // 금색
    private static readonly Color DESC_COLOR = new Color(0.9f, 0.9f, 0.93f);
    private static readonly Color BTN_NORMAL = new Color(0.12f, 0.14f, 0.22f, 0.85f);
    private static readonly Color BTN_HOVER = new Color(0.20f, 0.24f, 0.35f, 0.9f);
    private static readonly Color BTN_TEXT_COLOR = new Color(0.95f, 0.95f, 0.95f);
    private static readonly Color RESULT_SUCCESS = new Color(0.4f, 0.9f, 0.5f);
    private static readonly Color RESULT_FAIL = new Color(0.9f, 0.4f, 0.4f);
    private static readonly Color RESULT_NEUTRAL = new Color(0.85f, 0.85f, 0.9f);

    // ─────────────────────────────────────────────
    // 내부 참조
    // ─────────────────────────────────────────────

    private Canvas canvas;
    private TMPro.TMP_FontAsset mainFont;
    private GameEventData currentEvent;

    // UI 요소
    private Image backgroundImage;         // 전체 화면 이벤트 이미지
    private Image gradientOverlay;         // 하단→상단 검은 그라데이션
    private TMPro.TextMeshProUGUI titleText;
    private TMPro.TextMeshProUGUI descriptionText;
    private GameObject optionsContainer;
    private List<Button> optionButtons = new List<Button>();

    // 결과 UI
    private GameObject resultPanel;
    private TMPro.TextMeshProUGUI resultText;
    private Button returnButton;

    // 이벤트/결과 하단 컨테이너
    private GameObject eventBottomPanel;

    void Start()
    {
        mainFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Main_Fonts");

        currentEvent = EventManager.Instance?.CurrentEvent;
        if (currentEvent == null)
        {
            Debug.LogError("[SetupEventScene] 현재 이벤트가 없습니다! 맵으로 돌아갑니다.");
            SceneManager.LoadScene("MapScene");
            return;
        }

        BuildUI();
        ShowEvent();
    }

    // ─────────────────────────────────────────────
    // UI 구성
    // ─────────────────────────────────────────────

    private void BuildUI()
    {
        // Camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BG_COLOR;

        // EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Canvas
        GameObject canvasObj = new GameObject("EventCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // ═══════════════════════════════════════════
        // 레이어 1: 좌측 50% 배경 이미지 영역
        // ═══════════════════════════════════════════
        GameObject imageArea = new GameObject("ImageArea");
        imageArea.transform.SetParent(canvasObj.transform, false);
        RectTransform imgAreaRect = imageArea.AddComponent<RectTransform>();
        imgAreaRect.anchorMin = new Vector2(0, 0);
        imgAreaRect.anchorMax = new Vector2(0.5f, 1);
        imgAreaRect.offsetMin = Vector2.zero;
        imgAreaRect.offsetMax = Vector2.zero;

        Image areaBg = imageArea.AddComponent<Image>();
        areaBg.color = BG_COLOR;
        areaBg.raycastTarget = false;
        imageArea.AddComponent<RectMask2D>();

        GameObject bgObj = new GameObject("BackgroundImage");
        bgObj.transform.SetParent(imageArea.transform, false);
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = new Color(0.15f, 0.15f, 0.2f);
        backgroundImage.preserveAspect = false;
        backgroundImage.raycastTarget = false;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // ═══════════════════════════════════════════
        // 레이어 2: 우측 콘텐츠 (텍스트/버튼) 영역용 패널 및 그라데이션
        // ═══════════════════════════════════════════
        CreateGradientOverlay(canvasObj.transform);

        // ═══════════════════════════════════════════
        // 레이어 3: 우측 콘텐츠 
        // ═══════════════════════════════════════════
        BuildEventBottomPanel(canvasObj.transform);
        BuildResultPanel(canvasObj.transform);
    }

    private void CreateGradientOverlay(Transform parent)
    {
        // 텍스트 영역용 우측 50% 반투명 배경
        GameObject rightBgObj = new GameObject("RightBackground");
        rightBgObj.transform.SetParent(parent, false);
        RectTransform rbRect = rightBgObj.AddComponent<RectTransform>();
        rbRect.anchorMin = new Vector2(0.5f, 0);
        rbRect.anchorMax = new Vector2(1, 1);
        rbRect.offsetMin = Vector2.zero;
        rbRect.offsetMax = Vector2.zero;
        Image rbImg = rightBgObj.AddComponent<Image>();
        rbImg.color = BG_COLOR;

        // 경계선 그라데이션 (좌측 이미지와 우측 텍스트 사이 자연스러운 전환)
        GameObject overlayObj = new GameObject("GradientOverlay");
        overlayObj.transform.SetParent(parent, false);
        gradientOverlay = overlayObj.AddComponent<Image>();
        gradientOverlay.raycastTarget = false;

        int w = 256;
        Texture2D gradTex = new Texture2D(w, 1, TextureFormat.RGBA32, false);
        gradTex.wrapMode = TextureWrapMode.Clamp;

        for (int x = 0; x < w; x++)
        {
            float t = (float)x / (w - 1);
            float alpha = Mathf.Pow(t, 2f);
            gradTex.SetPixel(x, 0, new Color(BG_COLOR.r, BG_COLOR.g, BG_COLOR.b, alpha));
        }
        gradTex.Apply();

        Sprite gradSprite = Sprite.Create(gradTex, new Rect(0, 0, w, 1), new Vector2(0.5f, 0.5f));
        gradientOverlay.sprite = gradSprite;
        gradientOverlay.type = Image.Type.Sliced;

        RectTransform oRect = overlayObj.GetComponent<RectTransform>();
        oRect.anchorMin = new Vector2(0.3f, 0);
        oRect.anchorMax = new Vector2(0.5f, 1);
        oRect.offsetMin = Vector2.zero;
        oRect.offsetMax = Vector2.zero;
    }

    private void BuildEventBottomPanel(Transform parent)
    {
        eventBottomPanel = new GameObject("EventBottomPanel");
        eventBottomPanel.transform.SetParent(parent, false);
        RectTransform bpRect = eventBottomPanel.AddComponent<RectTransform>();

        // 화면 우측 50% 영역
        bpRect.anchorMin = new Vector2(0.5f, 0);
        bpRect.anchorMax = new Vector2(1, 1);
        bpRect.offsetMin = new Vector2(60, 60);
        bpRect.offsetMax = new Vector2(-60, -60);

        VerticalLayoutGroup vlg = eventBottomPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        // ─── 제목 ───
        titleText = CreateText(eventBottomPanel.transform, "Title", "", 40, TITLE_COLOR);
        titleText.fontStyle = TMPro.FontStyles.Bold;
        titleText.alignment = TMPro.TextAlignmentOptions.TopLeft;
        SetPreferredHeight(titleText.gameObject, 60);

        // ─── 구분선 ───
        CreateDivider(eventBottomPanel.transform, new Color(1f, 0.85f, 0.2f, 0.4f));

        // ─── 설명 텍스트 ───
        descriptionText = CreateText(eventBottomPanel.transform, "Description", "", 26, DESC_COLOR);
        descriptionText.alignment = TMPro.TextAlignmentOptions.TopLeft;
        descriptionText.lineSpacing = 40; // 간격 증가
        LayoutElement descLE = descriptionText.gameObject.AddComponent<LayoutElement>();
        descLE.flexibleHeight = 1;

        // ─── 선택지 컨테이너 ───
        optionsContainer = new GameObject("OptionsContainer");
        optionsContainer.transform.SetParent(eventBottomPanel.transform, false);
        optionsContainer.AddComponent<RectTransform>();
        VerticalLayoutGroup optVlg = optionsContainer.AddComponent<VerticalLayoutGroup>();
        optVlg.spacing = 15;
        optVlg.childAlignment = TextAnchor.LowerCenter;
        optVlg.childForceExpandWidth = true;
        optVlg.childForceExpandHeight = false;
        optVlg.childControlWidth = true;
        optVlg.childControlHeight = false;
        LayoutElement optLE = optionsContainer.AddComponent<LayoutElement>();
        optLE.preferredHeight = 300;
        optLE.flexibleHeight = 0;
    }

    private void BuildResultPanel(Transform parent)
    {
        resultPanel = new GameObject("ResultPanel");
        resultPanel.transform.SetParent(parent, false);
        RectTransform rRect = resultPanel.AddComponent<RectTransform>();
        rRect.anchorMin = new Vector2(0.5f, 0);
        rRect.anchorMax = new Vector2(1, 1);
        rRect.offsetMin = new Vector2(60, 60);
        rRect.offsetMax = new Vector2(-60, -60);

        VerticalLayoutGroup vlg = resultPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        // 결과 제목
        TMPro.TextMeshProUGUI resultTitle = CreateText(resultPanel.transform, "ResultTitle", "결과", 40, TITLE_COLOR);
        resultTitle.fontStyle = TMPro.FontStyles.Bold;
        resultTitle.alignment = TMPro.TextAlignmentOptions.TopLeft;
        SetPreferredHeight(resultTitle.gameObject, 60);

        CreateDivider(resultPanel.transform, new Color(1f, 0.85f, 0.2f, 0.4f));

        // 결과 텍스트
        resultText = CreateText(resultPanel.transform, "ResultText", "", 24, RESULT_NEUTRAL);
        resultText.alignment = TMPro.TextAlignmentOptions.TopLeft;
        resultText.overflowMode = TMPro.TextOverflowModes.Overflow;
        resultText.lineSpacing = 40;
        LayoutElement resLE = resultText.gameObject.AddComponent<LayoutElement>();
        resLE.flexibleHeight = 1;

        resultPanel.SetActive(false);

        // ─── 계속하기 버튼 ───
        BuildReturnButton(parent);
    }

    /// <summary>
    /// 계속하기 버튼을 결과 패널과 별도로 우측 하단에 작게 배치합니다.
    /// </summary>
    private void BuildReturnButton(Transform parent)
    {
        GameObject btnObj = new GameObject("ReturnButton");
        btnObj.transform.SetParent(parent, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();

        // 우측 하단 앵커
        btnRect.anchorMin = new Vector2(1, 0);
        btnRect.anchorMax = new Vector2(1, 0);
        btnRect.pivot = new Vector2(1, 0);
        btnRect.anchoredPosition = new Vector2(-30, 20);
        btnRect.sizeDelta = new Vector2(140, 40);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.15f, 0.17f, 0.25f, 0.9f);

        returnButton = btnObj.AddComponent<Button>();
        ColorBlock cb = returnButton.colors;
        cb.normalColor = new Color(0.15f, 0.17f, 0.25f, 0.9f);
        cb.highlightedColor = new Color(0.25f, 0.30f, 0.42f, 0.95f);
        cb.pressedColor = new Color(0.10f, 0.12f, 0.20f, 0.95f);
        cb.selectedColor = new Color(0.25f, 0.30f, 0.42f, 0.95f);
        returnButton.colors = cb;
        returnButton.targetGraphic = btnBg;

        // 버튼 텍스트
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8, 2);
        textRect.offsetMax = new Vector2(-8, -2);

        TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "계속하기";
        tmp.fontSize = 16;
        tmp.color = new Color(0.85f, 0.85f, 0.88f);
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        if (mainFont != null) tmp.font = mainFont;

        returnButton.onClick.AddListener(OnReturnToMap);

        // 초기에 숨김 (결과 표시 시 활성화)
        btnObj.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // 이벤트 표시
    // ─────────────────────────────────────────────

    private void ShowEvent()
    {
        titleText.text = currentEvent.eventTitle;
        descriptionText.text = currentEvent.eventDescription;

        // 이벤트 이미지 (전체 화면 배경)
        if (currentEvent.eventImage != null)
        {
            backgroundImage.sprite = currentEvent.eventImage;
            backgroundImage.color = Color.white;
            backgroundImage.preserveAspect = false; // 화면 채우기
        }

        // 선택지 버튼 생성
        ClearOptions();
        for (int i = 0; i < currentEvent.options.Count; i++)
        {
            EventOption option = currentEvent.options[i];
            int capturedIndex = i;

            string btnText = $"{i + 1}.  {option.optionText}";

            if (option.requirementType != EventRequirementType.None)
            {
                string reqLabel = option.requirementType == EventRequirementType.RequireGold
                    ? $"(골드 {option.requirementValue} 필요)"
                    : $"(HP {option.requirementValue} 필요)";
                btnText += $"  <size=16><color=#999>{reqLabel}</color></size>";
            }

            string rewardPreview = GetOptionRewardPreviewText(option);
            if (!string.IsNullOrEmpty(rewardPreview))
            {
                btnText += $"\n<size=18>{rewardPreview}</size>";
            }

            Button btn = CreateOptionButton(optionsContainer.transform, btnText, () => OnOptionSelected(capturedIndex));
            SetPreferredHeight(btn.gameObject, 110);
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
    // 선택 처리
    // ─────────────────────────────────────────────

    private void OnOptionSelected(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= currentEvent.options.Count) return;

        EventOption selectedOption = currentEvent.options[optionIndex];

        if (selectedOption.requirementType == EventRequirementType.RequireGold)
        {
            Debug.Log($"[SetupEventScene] 골드 조건 확인 (미구현): {selectedOption.requirementValue} 골드 필요");
        }

        EventOutcome outcome = EventManager.Instance.ResolveOutcome(selectedOption);
        EventManager.Instance.ApplyRewards(outcome);
        ShowResult(selectedOption, outcome);
    }

    private void ShowResult(EventOption selectedOption, EventOutcome outcome)
    {
        eventBottomPanel.SetActive(false);
        resultPanel.SetActive(true);

        if (returnButton != null)
            returnButton.gameObject.SetActive(true);

        Color textColor = RESULT_NEUTRAL;
        if (outcome.rewards != null && outcome.rewards.Count > 0)
        {
            textColor = RESULT_SUCCESS;
            foreach (var r in outcome.rewards)
            {
                if (r.rewardType == EventRewardType.TakeDamage ||
                    r.rewardType == EventRewardType.LoseGold ||
                    r.rewardType == EventRewardType.TakeMentalDamage ||
                    r.rewardType == EventRewardType.UpgradeNextBattles)
                {
                    textColor = RESULT_FAIL;
                    break;
                }
            }
        }

        string resultMessage = outcome.outcomeText;
        string rewardSummary = GetRewardSummaryText(outcome);
        if (!string.IsNullOrEmpty(rewardSummary))
        {
            resultMessage += $"\n\n{rewardSummary}";
        }

        resultText.text = resultMessage;
        resultText.color = textColor;
    }

    private string GetRewardSummaryText(EventOutcome outcome)
    {
        if (outcome.rewards == null || outcome.rewards.Count == 0) return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var r in outcome.rewards)
        {
            switch (r.rewardType)
            {
                case EventRewardType.HealHP:           sb.AppendLine($"<color=#66EE77>모든 아군 HP +{r.rewardValue}</color>"); break;
                case EventRewardType.TakeDamage:       sb.AppendLine($"<color=#EE6666>모든 아군 HP -{r.rewardValue}</color>"); break;
                case EventRewardType.GainGold:         sb.AppendLine($"<color=#FFD700>골드 +{r.rewardValue}</color>"); break;
                case EventRewardType.LoseGold:         sb.AppendLine($"<color=#EE6666>골드 -{r.rewardValue}</color>"); break;
                case EventRewardType.GainRelic:        sb.AppendLine($"<color=#BB77FF>유물 획득: {r.rewardDataID}</color>"); break;
                case EventRewardType.GainCard:         sb.AppendLine($"<color=#77BBFF>카드 획득: {r.rewardDataID}</color>"); break;
                case EventRewardType.GainConsumable:   sb.AppendLine($"<color=#77EEFF>소모품 획득: {r.rewardDataID}</color>"); break;
                case EventRewardType.UpgradeTrainCar:  sb.AppendLine($"<color=#FFAA33>기차 칸 강화!</color>"); break;
                case EventRewardType.TakeMentalDamage: sb.AppendLine($"<color=#CC66EE>모든 아군 정신력 -{r.rewardValue}</color>"); break;
                case EventRewardType.UpgradeNextBattles:sb.AppendLine($"<color=#EE6666>다음 {r.rewardValue}회 전투가 강적 전투로 대체!</color>"); break;
                case EventRewardType.LoseRelic:        sb.AppendLine($"<color=#EE6666>유물 소실: {r.rewardDataID}</color>"); break;
                case EventRewardType.DamageTrainCar:   sb.AppendLine($"<color=#EE6666>기차 칸 파손!</color>"); break;
                case EventRewardType.GainActionPoints: sb.AppendLine($"<color=#44DDFF>다음 {r.rewardValue}회 전투 행동력 +2</color>"); break;
            }
        }
        return sb.ToString().TrimEnd();
    }

    // ─────────────────────────────────────────────
    // 씬 복귀
    // ─────────────────────────────────────────────

    private void OnReturnToMap()
    {
        SceneManager.LoadScene("MapScene");
    }

    // ─────────────────────────────────────────────
    // UI 유틸리티
    // ─────────────────────────────────────────────

    private TMPro.TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();

        TMPro.TextMeshProUGUI tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.richText = true;
        if (mainFont != null) tmp.font = mainFont;

        return tmp;
    }

    private void CreateDivider(Transform parent, Color color)
    {
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(parent, false);
        divider.AddComponent<RectTransform>();
        Image dImg = divider.AddComponent<Image>();
        dImg.color = color;
        dImg.raycastTarget = false;

        LayoutElement le = divider.AddComponent<LayoutElement>();
        le.preferredHeight = 2;
        le.flexibleWidth = 1;
    }

    private Button CreateOptionButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("OptionBtn");
        btnObj.transform.SetParent(parent, false);
        btnObj.AddComponent<RectTransform>();

        Image btnBg = btnObj.AddComponent<Image>();
        Sprite bgSprite = Resources.Load<Sprite>("Events/Eventbutton");
        if (bgSprite != null)
        {
            btnBg.sprite = bgSprite;
            btnBg.type = Image.Type.Sliced;
        }
        else
        {
            btnBg.color = BTN_NORMAL;
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
            cb.pressedColor = new Color(0.10f, 0.12f, 0.20f, 0.9f);
            cb.selectedColor = BTN_HOVER;
        }
        btn.colors = cb;
        btn.targetGraphic = btnBg;

        // 텍스트
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20, 5);
        textRect.offsetMax = new Vector2(-20, -5);

        TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 20;
        tmp.color = BTN_TEXT_COLOR;
        tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = true;
        tmp.richText = true;
        if (mainFont != null) tmp.font = mainFont;

        btn.onClick.AddListener(onClick);
        return btn;
    }

    private void SetPreferredHeight(GameObject obj, float height)
    {
        LayoutElement le = obj.GetComponent<LayoutElement>();
        if (le == null) le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = height;
    }

    private void SetFullScreen(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void ClearOptions()
    {
        foreach (var btn in optionButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        optionButtons.Clear();
    }
}
