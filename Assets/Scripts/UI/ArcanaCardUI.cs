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
    public class ArcanaCardUI : MonoBehaviour
    {
        private static ArcanaCardUI _instance;
        public static ArcanaCardUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ArcanaCardUI>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ArcanaCardUI");
                        _instance = go.AddComponent<ArcanaCardUI>();
                    }
                }
                return _instance;
            }
        }

        private GameObject popupPanel;
        private Transform cardContainer;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI subInfoText;
        private TextMeshProUGUI resultSummaryText;

        private Button rerollButton;
        private TextMeshProUGUI rerollBtnText;
        private Image rerollBtnImg;

        private Button confirmButton;
        private TextMeshProUGUI confirmBtnText;

        // Devil Contract Selection Area
        private GameObject devilContractPanel;
        private Button contractBtn1;
        private Button contractBtn2;
        private Button contractBtn3;
        private TextMeshProUGUI contractTxt1;
        private TextMeshProUGUI contractTxt2;
        private TextMeshProUGUI contractTxt3;

        private TMP_FontAsset mainFont;

        private TrainCar currentNexusCar;
        private ArcanaBattleState currentBattleState;
        private ArcanaCardInfo currentDrawnCard;
        private ArcanaDrawResult currentResult;
        private int remainingRerolls = 1;
        private DevilContractType selectedDevilContract = DevilContractType.Option1_MoreAP_MoreDevil;
        private Action<int, ArcanaDrawResult> onConfirmCallback;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
        }

        public void ShowTarotDraw(TrainCar nexusCar, ArcanaBattleState battleState, Action<int, ArcanaDrawResult> onConfirm)
        {
            currentNexusCar = nexusCar;
            currentBattleState = battleState ?? new ArcanaBattleState();
            onConfirmCallback = onConfirm;
            remainingRerolls = 1; // 기본 1회 재시도
            selectedDevilContract = DevilContractType.Option1_MoreAP_MoreDevil;

            if (popupPanel == null)
            {
                CreateUI();
            }

            DrawNewCard();
            EvaluateAndRefreshUI();

            popupPanel.SetActive(true);
            popupPanel.transform.SetAsLastSibling();
        }

        private void DrawNewCard()
        {
            var pool = ArcanaCardManager.GetAvailableCardPool(currentNexusCar, currentBattleState);
            currentDrawnCard = ArcanaCardManager.DrawRandomCard(pool, currentBattleState);
        }

        private void CreateUI()
        {
            mainFont = Resources.Load<TMP_FontAsset>("Fonts/Main_Fonts");

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("ArcanaCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            popupPanel = new GameObject("ArcanaCardPopup");
            popupPanel.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = popupPanel.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image bgOverlay = popupPanel.AddComponent<Image>();
            bgOverlay.color = new Color(0.02f, 0.02f, 0.05f, 0.85f);

            // Center Modal Box
            GameObject modalBox = new GameObject("ModalBox");
            modalBox.transform.SetParent(popupPanel.transform, false);
            RectTransform modalRect = modalBox.AddComponent<RectTransform>();
            modalRect.anchorMin = new Vector2(0.5f, 0.5f);
            modalRect.anchorMax = new Vector2(0.5f, 0.5f);
            modalRect.pivot = new Vector2(0.5f, 0.5f);
            modalRect.sizeDelta = new Vector2(640, 520);

            Image modalBg = modalBox.AddComponent<Image>();
            modalBg.color = new Color(0.08f, 0.07f, 0.14f, 0.98f);

            VerticalLayoutGroup vLayout = modalBox.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(20, 20, 16, 16);
            vLayout.spacing = 8;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            // 1. Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(modalBox.transform, false);
            LayoutElement tLe = titleObj.AddComponent<LayoutElement>();
            tLe.preferredHeight = 34;
            tLe.flexibleWidth = 1f;
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "<color=#E6C657><b>[모듈: 아르카나] 운명의 타로 개방</b></color>";
            titleText.fontSize = 23;
            titleText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) titleText.font = mainFont;

            // 2. Sub Info
            GameObject subObj = new GameObject("SubInfo");
            subObj.transform.SetParent(modalBox.transform, false);
            LayoutElement sLe = subObj.AddComponent<LayoutElement>();
            sLe.preferredHeight = 22;
            sLe.flexibleWidth = 1f;
            subInfoText = subObj.AddComponent<TextMeshProUGUI>();
            subInfoText.text = "아르카나 사양";
            subInfoText.fontSize = 14;
            subInfoText.color = new Color(0.75f, 0.8f, 1f, 1f);
            subInfoText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) subInfoText.font = mainFont;

            // 3. Card Container (Center Card Presentation)
            GameObject cardContainerObj = new GameObject("CardContainer");
            cardContainerObj.transform.SetParent(modalBox.transform, false);
            LayoutElement ccLe = cardContainerObj.AddComponent<LayoutElement>();
            ccLe.preferredHeight = 220;
            ccLe.flexibleWidth = 1f;

            HorizontalLayoutGroup ccHlg = cardContainerObj.AddComponent<HorizontalLayoutGroup>();
            ccHlg.padding = new RectOffset(8, 8, 8, 8);
            ccHlg.spacing = 14;
            ccHlg.childAlignment = TextAnchor.MiddleCenter;
            ccHlg.childControlWidth = true;
            ccHlg.childControlHeight = true;
            ccHlg.childForceExpandWidth = true;
            ccHlg.childForceExpandHeight = true;

            cardContainer = cardContainerObj.transform;

            // 4. Devil Contract Options Panel (Visible only when Devil is drawn)
            devilContractPanel = new GameObject("DevilContractPanel");
            devilContractPanel.transform.SetParent(modalBox.transform, false);
            LayoutElement dLe = devilContractPanel.AddComponent<LayoutElement>();
            dLe.preferredHeight = 85;
            dLe.flexibleWidth = 1f;

            VerticalLayoutGroup dVlg = devilContractPanel.AddComponent<VerticalLayoutGroup>();
            dVlg.padding = new RectOffset(4, 4, 2, 2);
            dVlg.spacing = 4;
            dVlg.childControlWidth = true;
            dVlg.childControlHeight = true;
            dVlg.childForceExpandWidth = true;
            dVlg.childForceExpandHeight = true;

            // Devil Option 1
            contractBtn1 = CreateContractButton(devilContractPanel.transform, out contractTxt1, "계약 1: 즉시 +6 AP (+누적), 악마 확률 +66%", DevilContractType.Option1_MoreAP_MoreDevil);
            // Devil Option 2
            contractBtn2 = CreateContractButton(devilContractPanel.transform, out contractTxt2, "계약 2: 매 턴 +6 AP, 매 턴 전원 정신력 -6, 악마 재등장 불가", DevilContractType.Option2_EveryTurnAP_LoseMental_NoMoreDevil);
            // Devil Option 3
            contractBtn3 = CreateContractButton(devilContractPanel.transform, out contractTxt3, "계약 3: 매 턴 +6 AP, 매 턴 전원 약화 1 / 취약 1 획득", DevilContractType.Option3_EveryTurnAP_GainDebuffs);

            // 5. Result Summary
            GameObject sumObj = new GameObject("Summary");
            sumObj.transform.SetParent(modalBox.transform, false);
            LayoutElement sumLe = sumObj.AddComponent<LayoutElement>();
            sumLe.preferredHeight = 36;
            sumLe.flexibleWidth = 1f;
            resultSummaryText = sumObj.AddComponent<TextMeshProUGUI>();
            resultSummaryText.text = "결과 요약";
            resultSummaryText.fontSize = 15;
            resultSummaryText.color = Color.white;
            resultSummaryText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) resultSummaryText.font = mainFont;

            // 6. Buttons Row
            GameObject btnRow = new GameObject("ButtonRow");
            btnRow.transform.SetParent(modalBox.transform, false);
            LayoutElement brLe = btnRow.AddComponent<LayoutElement>();
            brLe.preferredHeight = 46;
            brLe.flexibleWidth = 1f;

            HorizontalLayoutGroup brHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            brHlg.spacing = 14;
            brHlg.childControlWidth = true;
            brHlg.childControlHeight = true;
            brHlg.childForceExpandWidth = true;
            brHlg.childForceExpandHeight = true;

            // 6-1. Reroll Button
            GameObject rObj = new GameObject("RerollButton");
            rObj.transform.SetParent(btnRow.transform, false);
            rerollBtnImg = rObj.AddComponent<Image>();
            rerollBtnImg.color = new Color(0.35f, 0.25f, 0.65f, 1f);
            rerollButton = rObj.AddComponent<Button>();
            rerollButton.onClick.AddListener(OnRerollClicked);

            GameObject rTxtObj = new GameObject("Text");
            rTxtObj.transform.SetParent(rObj.transform, false);
            RectTransform rTxtRect = rTxtObj.AddComponent<RectTransform>();
            rTxtRect.anchorMin = Vector2.zero;
            rTxtRect.anchorMax = Vector2.one;
            rTxtRect.offsetMin = Vector2.zero;
            rTxtRect.offsetMax = Vector2.zero;
            rerollBtnText = rTxtObj.AddComponent<TextMeshProUGUI>();
            rerollBtnText.text = "다시 뽑기 (1회 가능)";
            rerollBtnText.fontSize = 15;
            rerollBtnText.color = Color.white;
            rerollBtnText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) rerollBtnText.font = mainFont;

            // 6-2. Confirm Button
            GameObject cObj = new GameObject("ConfirmButton");
            cObj.transform.SetParent(btnRow.transform, false);
            Image cImg = cObj.AddComponent<Image>();
            cImg.color = new Color(0.18f, 0.65f, 0.35f, 1f);
            confirmButton = cObj.AddComponent<Button>();
            confirmButton.onClick.AddListener(OnConfirmClicked);

            GameObject cTxtObj = new GameObject("Text");
            cTxtObj.transform.SetParent(cObj.transform, false);
            RectTransform cTxtRect = cTxtObj.AddComponent<RectTransform>();
            cTxtRect.anchorMin = Vector2.zero;
            cTxtRect.anchorMax = Vector2.one;
            cTxtRect.offsetMin = Vector2.zero;
            cTxtRect.offsetMax = Vector2.zero;
            confirmBtnText = cTxtObj.AddComponent<TextMeshProUGUI>();
            confirmBtnText.text = "운명 수용 (확정)";
            confirmBtnText.fontSize = 16;
            confirmBtnText.color = Color.white;
            confirmBtnText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) confirmBtnText.font = mainFont;
        }

        private Button CreateContractButton(Transform parent, out TextMeshProUGUI textComp, string label, DevilContractType contractType)
        {
            GameObject btnObj = new GameObject($"ContractBtn_{contractType}");
            btnObj.transform.SetParent(parent, false);
            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.1f, 0.15f, 1f);
            Button btn = btnObj.AddComponent<Button>();

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(8, 2);
            tRect.offsetMax = new Vector2(-8, -2);
            textComp = txtObj.AddComponent<TextMeshProUGUI>();
            textComp.text = label;
            textComp.fontSize = 12;
            textComp.color = Color.white;
            textComp.alignment = TextAlignmentOptions.Left;
            if (mainFont != null) textComp.font = mainFont;

            btn.onClick.AddListener(() =>
            {
                selectedDevilContract = contractType;
                EvaluateAndRefreshUI();
            });

            return btn;
        }

        private void EvaluateAndRefreshUI()
        {
            int ac = currentNexusCar != null ? currentNexusCar.level : 0;
            currentResult = ArcanaCardManager.EvaluateDraw(currentDrawnCard, currentNexusCar, currentBattleState, selectedDevilContract);

            subInfoText.text = $"<color=yellow>아르카나 엔진 Lv.{ac} (AC = {ac})</color> | 남은 재시도: <color=#00FFCC><b>{remainingRerolls}</b>회</color>";

            // Clear previous cards UI
            foreach (Transform child in cardContainer)
            {
                Destroy(child.gameObject);
            }

            // Render Main Drawn Card
            RenderCardBox(currentResult.drawnCard, isMain: true);

            // If Hermit Chained Card exists, render it too
            if (currentResult.hermitChainedCard != null)
            {
                RenderCardBox(currentResult.hermitChainedCard, isMain: false);
            }

            // Handle Devil UI
            bool isDevil = currentDrawnCard.cardType == TarotCardType.Devil;
            devilContractPanel.SetActive(isDevil);
            if (isDevil)
            {
                UpdateContractButtonVisual(contractBtn1, contractTxt1, DevilContractType.Option1_MoreAP_MoreDevil, "계약 1: 즉시 +6 AP (+누적), 악마 등장확률 +66%");
                UpdateContractButtonVisual(contractBtn2, contractTxt2, DevilContractType.Option2_EveryTurnAP_LoseMental_NoMoreDevil, "계약 2: 매 턴 +6 AP, 매 턴 전원 정신력 -6 (악마 재등장 불가)");
                UpdateContractButtonVisual(contractBtn3, contractTxt3, DevilContractType.Option3_EveryTurnAP_GainDebuffs, "계약 3: 매 턴 +6 AP, 매 턴 전원 약화 1 / 취약 1 획득");
            }

            // Summary Text
            string logs = currentResult.detailLogs.Count > 0 ? $" ({string.Join(", ", currentResult.detailLogs)})" : "";
            resultSummaryText.text = $"<color=#00FFCC><b>{currentResult.summary}</b></color>{logs}";

            // Reroll button
            rerollButton.interactable = remainingRerolls > 0;
            if (rerollBtnImg != null)
            {
                rerollBtnImg.color = remainingRerolls > 0 ? new Color(0.35f, 0.25f, 0.65f, 1f) : new Color(0.3f, 0.3f, 0.35f, 1f);
            }
            rerollBtnText.text = remainingRerolls > 0 ? "다시 뽑기 (1회 가능)" : "다시 뽑기 (소진됨)";

            // Confirm button label
            confirmBtnText.text = $"운명 수용 (+{currentResult.gainedAP} AP 획득)";
        }

        private void RenderCardBox(ArcanaCardInfo card, bool isMain)
        {
            GameObject cardBox = new GameObject($"CardBox_{card.cardType}");
            cardBox.transform.SetParent(cardContainer, false);

            Image bg = cardBox.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.10f, 0.20f, 1f);

            Outline outline = cardBox.AddComponent<Outline>();
            outline.effectColor = card.themeColor;
            outline.effectDistance = new Vector2(2f, 2f);

            VerticalLayoutGroup vlg = cardBox.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 8, 8);
            vlg.spacing = 4;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 1. Roman & Card Title
            GameObject titleObj = new GameObject("CardTitle");
            titleObj.transform.SetParent(cardBox.transform, false);
            LayoutElement tLe = titleObj.AddComponent<LayoutElement>();
            tLe.preferredHeight = 28;
            tLe.flexibleWidth = 1f;
            TextMeshProUGUI tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            string colHex = ColorUtility.ToHtmlStringRGB(card.themeColor);
            string prefix = isMain ? "" : "<color=#FFD700>[연쇄] </color>";
            tTmp.text = $"{prefix}<color=#{colHex}><b>{card.romanNumeral}. {card.cardNameKorean}</b></color> <size=12><color=gray>({card.cardNameEnglish})</color></size>";
            tTmp.fontSize = 18;
            tTmp.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) tTmp.font = mainFont;

            // 2. Short Summary Badge
            GameObject badgeObj = new GameObject("Badge");
            badgeObj.transform.SetParent(cardBox.transform, false);
            LayoutElement bLe = badgeObj.AddComponent<LayoutElement>();
            bLe.preferredHeight = 24;
            bLe.flexibleWidth = 1f;
            TextMeshProUGUI bTmp = badgeObj.AddComponent<TextMeshProUGUI>();
            bTmp.text = $"<color=#00FFCC><b>★ {card.shortEffectSummary}</b></color>";
            bTmp.fontSize = 13;
            bTmp.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) bTmp.font = mainFont;

            // 3. Full Description
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(cardBox.transform, false);
            LayoutElement dLe = descObj.AddComponent<LayoutElement>();
            dLe.preferredHeight = 135;
            dLe.flexibleWidth = 1f;
            TextMeshProUGUI dTmp = descObj.AddComponent<TextMeshProUGUI>();
            dTmp.text = card.description;
            dTmp.fontSize = 13;
            dTmp.color = new Color(0.9f, 0.9f, 0.95f, 1f);
            dTmp.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) dTmp.font = mainFont;
        }

        private void UpdateContractButtonVisual(Button btn, TextMeshProUGUI txt, DevilContractType type, string label)
        {
            bool isSelected = selectedDevilContract == type;
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isSelected ? new Color(0.65f, 0.15f, 0.25f, 1f) : new Color(0.18f, 0.12f, 0.15f, 1f);
            }
            txt.text = isSelected ? $"<b><color=#FFD700>[선택됨] {label}</color></b>" : $"<color=#CCCCCC>{label}</color>";
        }

        private void OnRerollClicked()
        {
            if (remainingRerolls <= 0) return;

            remainingRerolls--;
            DrawNewCard();
            EvaluateAndRefreshUI();
            NotificationManager.Instance?.ShowMessage($"[아르카나] 카드를 다시 뽑았습니다: {currentDrawnCard.FullTitle}", Color.cyan);
        }

        private void OnConfirmClicked()
        {
            popupPanel.SetActive(false);
            onConfirmCallback?.Invoke(currentResult.gainedAP, currentResult);
        }
    }
}
