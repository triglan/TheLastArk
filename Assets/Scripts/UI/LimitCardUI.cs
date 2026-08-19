using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheLastArk.Data;
using TheLastArk.Battle;
using TheLastArk.Managers;

namespace TheLastArk.UI
{
    public class LimitCardUI : MonoBehaviour
    {
        private static LimitCardUI _instance;
        public static LimitCardUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<LimitCardUI>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("LimitCardUI");
                        _instance = go.AddComponent<LimitCardUI>();
                    }
                }
                return _instance;
            }
        }

        private GameObject popupPanel;
        private Transform cardsContainer;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI subInfoText;
        private TextMeshProUGUI sumStatusText;
        private TextMeshProUGUI resultSummaryText;
        private Button hitButton;
        private TextMeshProUGUI hitBtnText;
        private Image hitBtnImg;
        private Button standButton;
        private TextMeshProUGUI standBtnText;
        private TMP_FontAsset mainFont;

        private TrainCar currentNexusCar;
        private List<int> currentDrawnCards = new List<int>();
        private LimitCardResult currentResult;
        private Action<int, bool, bool> onConfirmCallback; // (ap, isFirstSkillFree, isRetainAllSkills)

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
        }

        public void ShowCardDraw(TrainCar nexusCar, Action<int, bool, bool> onConfirm)
        {
            currentNexusCar = nexusCar;
            onConfirmCallback = onConfirm;
            currentDrawnCards.Clear();

            if (popupPanel == null)
            {
                CreateUI();
            }

            // Draw initial card
            currentDrawnCards.Add(LimitCardManager.DrawCard());
            EvaluateAndRefresh();

            popupPanel.SetActive(true);
            popupPanel.transform.SetAsLastSibling();
        }

        private void CreateUI()
        {
            mainFont = Resources.Load<TMP_FontAsset>("Fonts/Main_Fonts");

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("LimitCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            popupPanel = new GameObject("LimitCardPopup");
            popupPanel.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = popupPanel.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Semi-transparent backdrop
            Image bgOverlay = popupPanel.AddComponent<Image>();
            bgOverlay.color = new Color(0f, 0f, 0f, 0.75f);

            // Center Modal Window
            GameObject modalBox = new GameObject("ModalBox");
            modalBox.transform.SetParent(popupPanel.transform, false);
            RectTransform modalRect = modalBox.AddComponent<RectTransform>();
            modalRect.anchorMin = new Vector2(0.5f, 0.5f);
            modalRect.anchorMax = new Vector2(0.5f, 0.5f);
            modalRect.pivot = new Vector2(0.5f, 0.5f);
            modalRect.sizeDelta = new Vector2(620, 460);

            Image modalBg = modalBox.AddComponent<Image>();
            modalBg.color = new Color(0.09f, 0.12f, 0.18f, 0.98f);

            VerticalLayoutGroup vLayout = modalBox.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(24, 24, 20, 20);
            vLayout.spacing = 10;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            // 1. Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(modalBox.transform, false);
            LayoutElement tLe = titleObj.AddComponent<LayoutElement>();
            tLe.preferredHeight = 36;
            tLe.flexibleWidth = 1f;
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "<color=#FFCC00><b>[모듈: 리미트] 카드 뽑기</b></color>";
            titleText.fontSize = 24;
            titleText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) titleText.font = mainFont;

            // 2. Sub Info (Level & Ratio & Threshold)
            GameObject subObj = new GameObject("SubInfo");
            subObj.transform.SetParent(modalBox.transform, false);
            LayoutElement sLe = subObj.AddComponent<LayoutElement>();
            sLe.preferredHeight = 24;
            sLe.flexibleWidth = 1f;
            subInfoText = subObj.AddComponent<TextMeshProUGUI>();
            subInfoText.text = "리미트 사양";
            subInfoText.fontSize = 15;
            subInfoText.color = new Color(0.7f, 0.85f, 1f, 1f);
            subInfoText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) subInfoText.font = mainFont;

            // 3. Cards Scroll/Container Area
            GameObject cardsArea = new GameObject("CardsArea");
            cardsArea.transform.SetParent(modalBox.transform, false);
            LayoutElement caLe = cardsArea.AddComponent<LayoutElement>();
            caLe.preferredHeight = 125;
            caLe.flexibleWidth = 1f;

            Image caBg = cardsArea.AddComponent<Image>();
            caBg.color = new Color(0.05f, 0.07f, 0.11f, 0.85f);

            ScrollRect cardsScroll = cardsArea.AddComponent<ScrollRect>();
            cardsScroll.horizontal = true;
            cardsScroll.vertical = false;

            GameObject vpObj = new GameObject("Viewport");
            vpObj.transform.SetParent(cardsArea.transform, false);
            RectTransform vpRect = vpObj.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            vpObj.AddComponent<RectMask2D>();

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(vpObj.transform, false);
            RectTransform cRect = contentObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 0);
            cRect.anchorMax = new Vector2(0, 1);
            cRect.pivot = new Vector2(0, 0.5f);
            cRect.sizeDelta = new Vector2(0, 0);

            HorizontalLayoutGroup caHlg = contentObj.AddComponent<HorizontalLayoutGroup>();
            caHlg.padding = new RectOffset(15, 15, 10, 10);
            caHlg.spacing = 14;
            caHlg.childAlignment = TextAnchor.MiddleLeft;
            caHlg.childControlWidth = false;
            caHlg.childControlHeight = false;

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            cardsScroll.viewport = vpRect;
            cardsScroll.content = cRect;
            cardsContainer = contentObj.transform;

            // 4. Sum & Status Text
            GameObject sumStatusObj = new GameObject("SumStatus");
            sumStatusObj.transform.SetParent(modalBox.transform, false);
            LayoutElement ssLe = sumStatusObj.AddComponent<LayoutElement>();
            ssLe.preferredHeight = 32;
            ssLe.flexibleWidth = 1f;
            sumStatusText = sumStatusObj.AddComponent<TextMeshProUGUI>();
            sumStatusText.text = "현재 합계";
            sumStatusText.fontSize = 19;
            sumStatusText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) sumStatusText.font = mainFont;

            // 5. Result Summary Text
            GameObject sumObj = new GameObject("Summary");
            sumObj.transform.SetParent(modalBox.transform, false);
            LayoutElement sumLe = sumObj.AddComponent<LayoutElement>();
            sumLe.preferredHeight = 44;
            sumLe.flexibleWidth = 1f;
            resultSummaryText = sumObj.AddComponent<TextMeshProUGUI>();
            resultSummaryText.text = "결과 요약";
            resultSummaryText.fontSize = 16;
            resultSummaryText.color = Color.white;
            resultSummaryText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) resultSummaryText.font = mainFont;

            // 6. Buttons Row
            GameObject btnRow = new GameObject("ButtonRow");
            btnRow.transform.SetParent(modalBox.transform, false);
            LayoutElement brLe = btnRow.AddComponent<LayoutElement>();
            brLe.preferredHeight = 50;
            brLe.flexibleWidth = 1f;

            HorizontalLayoutGroup brHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            brHlg.spacing = 16;
            brHlg.childControlWidth = true;
            brHlg.childControlHeight = true;
            brHlg.childForceExpandWidth = true;
            brHlg.childForceExpandHeight = true;

            // 6-1. Hit Button (카드 뽑기)
            GameObject hitObj = new GameObject("HitButton");
            hitObj.transform.SetParent(btnRow.transform, false);
            hitBtnImg = hitObj.AddComponent<Image>();
            hitBtnImg.color = new Color(0.28f, 0.35f, 0.70f, 1f);
            hitButton = hitObj.AddComponent<Button>();
            hitButton.onClick.AddListener(OnHitClicked);

            GameObject hTxtObj = new GameObject("Text");
            hTxtObj.transform.SetParent(hitObj.transform, false);
            RectTransform hTxtRect = hTxtObj.AddComponent<RectTransform>();
            hTxtRect.anchorMin = Vector2.zero;
            hTxtRect.anchorMax = Vector2.one;
            hTxtRect.offsetMin = Vector2.zero;
            hTxtRect.offsetMax = Vector2.zero;
            hitBtnText = hTxtObj.AddComponent<TextMeshProUGUI>();
            hitBtnText.text = "+ 카드 1장 뽑기 (1~10)";
            hitBtnText.fontSize = 16;
            hitBtnText.color = Color.white;
            hitBtnText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) hitBtnText.font = mainFont;

            // 6-2. Stand Button (행동력 확정)
            GameObject standObj = new GameObject("StandButton");
            standObj.transform.SetParent(btnRow.transform, false);
            Image standImg = standObj.AddComponent<Image>();
            standImg.color = new Color(0.20f, 0.65f, 0.35f, 1f);
            standButton = standObj.AddComponent<Button>();
            standButton.onClick.AddListener(OnStandClicked);

            GameObject sTxtObj = new GameObject("Text");
            sTxtObj.transform.SetParent(standObj.transform, false);
            RectTransform stTxtRect = sTxtObj.AddComponent<RectTransform>();
            stTxtRect.anchorMin = Vector2.zero;
            stTxtRect.anchorMax = Vector2.one;
            stTxtRect.offsetMin = Vector2.zero;
            stTxtRect.offsetMax = Vector2.zero;
            standBtnText = sTxtObj.AddComponent<TextMeshProUGUI>();
            standBtnText.text = "행동력 확정";
            standBtnText.fontSize = 17;
            standBtnText.color = Color.white;
            standBtnText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) standBtnText.font = mainFont;
        }

        private void EvaluateAndRefresh()
        {
            currentResult = LimitCardManager.Evaluate(currentNexusCar, currentDrawnCards);

            int level = currentNexusCar != null ? currentNexusCar.level : 0;
            float ratio = LimitCardManager.GetRatio(level);
            int threshold = LimitCardManager.GetThreshold(currentNexusCar);

            subInfoText.text = $"<color=yellow>넥서스 Lv.{level}</color> | 한도: <b>{threshold}</b> | 전환 비율: <b>{(ratio * 100):F0}%</b> (반올림)";

            // Render cards in container
            foreach (Transform child in cardsContainer)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < currentResult.drawnCards.Count; i++)
            {
                int cardVal = currentResult.drawnCards[i];
                GameObject cardObj = new GameObject($"Card_{i}");
                cardObj.transform.SetParent(cardsContainer, false);
                RectTransform cRect = cardObj.AddComponent<RectTransform>();
                cRect.sizeDelta = new Vector2(75, 105);

                Image cBg = cardObj.AddComponent<Image>();
                cBg.color = new Color(0.18f, 0.24f, 0.36f, 1f);

                VerticalLayoutGroup cVlg = cardObj.AddComponent<VerticalLayoutGroup>();
                cVlg.padding = new RectOffset(4, 4, 6, 6);
                cVlg.spacing = 2;
                cVlg.childAlignment = TextAnchor.MiddleCenter;
                cVlg.childControlWidth = true;
                cVlg.childControlHeight = true;
                cVlg.childForceExpandWidth = true;
                cVlg.childForceExpandHeight = false;

                // Card Value
                GameObject valObj = new GameObject("ValueText");
                valObj.transform.SetParent(cardObj.transform, false);
                LayoutElement vLe = valObj.AddComponent<LayoutElement>();
                vLe.preferredHeight = 55;
                vLe.flexibleWidth = 1f;
                TextMeshProUGUI vTmp = valObj.AddComponent<TextMeshProUGUI>();
                vTmp.text = $"<b>{cardVal}</b>";
                vTmp.fontSize = 36;
                vTmp.color = Color.white;
                vTmp.alignment = TextAlignmentOptions.Center;
                if (mainFont != null) vTmp.font = mainFont;

                // Card Index
                GameObject idxObj = new GameObject("IndexText");
                idxObj.transform.SetParent(cardObj.transform, false);
                LayoutElement iLe = idxObj.AddComponent<LayoutElement>();
                iLe.preferredHeight = 22;
                iLe.flexibleWidth = 1f;
                TextMeshProUGUI iTmp = idxObj.AddComponent<TextMeshProUGUI>();
                iTmp.text = $"#{i + 1}";
                iTmp.fontSize = 12;
                iTmp.color = new Color(0.7f, 0.85f, 1f, 1f);
                iTmp.alignment = TextAlignmentOptions.Center;
                if (mainFont != null) iTmp.font = mainFont;
            }

            // Sum Status Text
            string sumColor = currentResult.isBust ? "#FF4444" : (currentResult.currentSum == threshold ? "#00FFCC" : "#FFCC00");
            string statusDesc = currentResult.isBust ? "<color=#FF4444><b>[버스트 초과!]</b></color>" : "";
            sumStatusText.text = $"현재 합계: <color={sumColor}><b>{currentResult.currentSum}</b></color> / {threshold} {statusDesc}";

            // Result Summary Text
            resultSummaryText.text = currentResult.summary;

            // Hit button availability
            hitButton.interactable = !currentResult.isBust;
            if (hitBtnImg != null)
            {
                hitBtnImg.color = !currentResult.isBust ? new Color(0.28f, 0.35f, 0.70f, 1f) : new Color(0.35f, 0.35f, 0.40f, 1f);
            }

            // Stand button label
            standBtnText.text = $"행동력 확정 (+{currentResult.totalGainedAP} AP)";
        }

        private void OnHitClicked()
        {
            if (currentResult != null && currentResult.isBust) return;

            int newCard = LimitCardManager.DrawCard();
            currentDrawnCards.Add(newCard);
            EvaluateAndRefresh();

            if (currentResult.isBust)
            {
                NotificationManager.Instance?.ShowMessage($"카드 {newCard} 뽑음! 합계 {currentResult.currentSum}으로 버스트 초과!", Color.red);
            }
            else
            {
                NotificationManager.Instance?.ShowMessage($"카드 {newCard} 뽑음! 현재 합계: {currentResult.currentSum}", Color.cyan);
            }
        }

        private void OnStandClicked()
        {
            int ap = currentResult != null ? currentResult.totalGainedAP : 1;
            bool freeSkill = currentResult != null && currentResult.isPerfectNumberFreeSkillTriggered;
            bool retainAll = currentResult != null && currentResult.isExact21Triggered;

            popupPanel.SetActive(false);
            onConfirmCallback?.Invoke(ap, freeSkill, retainAll);
        }
    }
}
