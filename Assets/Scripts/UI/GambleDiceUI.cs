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
    public class GambleDiceUI : MonoBehaviour
    {
        private static GambleDiceUI _instance;
        public static GambleDiceUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GambleDiceUI>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GambleDiceUI");
                        _instance = go.AddComponent<GambleDiceUI>();
                    }
                }
                return _instance;
            }
        }

        private GameObject popupPanel;
        private Transform diceContainer;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI subInfoText;
        private TextMeshProUGUI resultSummaryText;
        private TextMeshProUGUI rerollBtnText;
        private Image rerollBtnImg;
        private Button rerollButton;
        private Button confirmButton;
        private TextMeshProUGUI confirmBtnText;
        private TMP_FontAsset mainFont;

        private TrainCar currentNexusCar;
        private GambleRollResult currentRoll;
        private int remainingRerolls = 1;
        private Action<int> onConfirmCallback;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
        }

        public void ShowDiceRoll(TrainCar nexusCar, Action<int> onConfirm)
        {
            currentNexusCar = nexusCar;
            onConfirmCallback = onConfirm;
            remainingRerolls = GambleDiceManager.GetMaxRerolls(nexusCar);

            if (popupPanel == null)
            {
                CreateUI();
            }

            RollDice();
            popupPanel.SetActive(true);
            popupPanel.transform.SetAsLastSibling();
        }

        private void CreateUI()
        {
            mainFont = Resources.Load<TMP_FontAsset>("Fonts/Main_Fonts");

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("GambleCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            popupPanel = new GameObject("GambleDicePopup");
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
            modalRect.sizeDelta = new Vector2(580, 420);

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
            titleText.text = "<color=#FFCC00><b>[모듈: 갬블] 행동력 주사위</b></color>";
            titleText.fontSize = 24;
            titleText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) titleText.font = mainFont;

            // 2. Sub Info (Nexus Level & Dice Info)
            GameObject subObj = new GameObject("SubInfo");
            subObj.transform.SetParent(modalBox.transform, false);
            LayoutElement sLe = subObj.AddComponent<LayoutElement>();
            sLe.preferredHeight = 24;
            sLe.flexibleWidth = 1f;
            subInfoText = subObj.AddComponent<TextMeshProUGUI>();
            subInfoText.text = "주사위 구성";
            subInfoText.fontSize = 16;
            subInfoText.color = new Color(0.7f, 0.85f, 1f, 1f);
            subInfoText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) subInfoText.font = mainFont;

            // 3. Dice Container (Horizontal Area)
            GameObject diceArea = new GameObject("DiceArea");
            diceArea.transform.SetParent(modalBox.transform, false);
            LayoutElement daLe = diceArea.AddComponent<LayoutElement>();
            daLe.preferredHeight = 125;
            daLe.flexibleWidth = 1f;

            Image daBg = diceArea.AddComponent<Image>();
            daBg.color = new Color(0.05f, 0.07f, 0.11f, 0.85f);

            HorizontalLayoutGroup daHlg = diceArea.AddComponent<HorizontalLayoutGroup>();
            daHlg.padding = new RectOffset(15, 15, 10, 10);
            daHlg.spacing = 20;
            daHlg.childAlignment = TextAnchor.MiddleCenter;
            daHlg.childControlWidth = false;
            daHlg.childControlHeight = false;

            diceContainer = diceArea.transform;

            // 4. Result Summary Text
            GameObject sumObj = new GameObject("Summary");
            sumObj.transform.SetParent(modalBox.transform, false);
            LayoutElement sumLe = sumObj.AddComponent<LayoutElement>();
            sumLe.preferredHeight = 52;
            sumLe.flexibleWidth = 1f;
            resultSummaryText = sumObj.AddComponent<TextMeshProUGUI>();
            resultSummaryText.text = "결과 요약";
            resultSummaryText.fontSize = 17;
            resultSummaryText.color = Color.white;
            resultSummaryText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) resultSummaryText.font = mainFont;

            // 5. Buttons Row
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

            // 5-1. Reroll Button
            GameObject rerollObj = new GameObject("RerollButton");
            rerollObj.transform.SetParent(btnRow.transform, false);
            rerollBtnImg = rerollObj.AddComponent<Image>();
            rerollBtnImg.color = new Color(0.28f, 0.35f, 0.70f, 1f);
            rerollButton = rerollObj.AddComponent<Button>();
            rerollButton.onClick.AddListener(OnRerollClicked);

            GameObject rTxtObj = new GameObject("Text");
            rTxtObj.transform.SetParent(rerollObj.transform, false);
            RectTransform rTxtRect = rTxtObj.AddComponent<RectTransform>();
            rTxtRect.anchorMin = Vector2.zero;
            rTxtRect.anchorMax = Vector2.one;
            rTxtRect.offsetMin = Vector2.zero;
            rTxtRect.offsetMax = Vector2.zero;
            rerollBtnText = rTxtObj.AddComponent<TextMeshProUGUI>();
            rerollBtnText.text = "다시 굴리기 (남은 횟수: 1)";
            rerollBtnText.fontSize = 16;
            rerollBtnText.color = Color.white;
            rerollBtnText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) rerollBtnText.font = mainFont;

            // 5-2. Confirm Button
            GameObject confirmObj = new GameObject("ConfirmButton");
            confirmObj.transform.SetParent(btnRow.transform, false);
            Image confirmImg = confirmObj.AddComponent<Image>();
            confirmImg.color = new Color(0.20f, 0.65f, 0.35f, 1f);
            confirmButton = confirmObj.AddComponent<Button>();
            confirmButton.onClick.AddListener(OnConfirmClicked);

            GameObject cTxtObj = new GameObject("Text");
            cTxtObj.transform.SetParent(confirmObj.transform, false);
            RectTransform cTxtRect = cTxtObj.AddComponent<RectTransform>();
            cTxtRect.anchorMin = Vector2.zero;
            cTxtRect.anchorMax = Vector2.one;
            cTxtRect.offsetMin = Vector2.zero;
            cTxtRect.offsetMax = Vector2.zero;
            confirmBtnText = cTxtObj.AddComponent<TextMeshProUGUI>();
            confirmBtnText.text = "행동력 확정";
            confirmBtnText.fontSize = 18;
            confirmBtnText.color = Color.white;
            confirmBtnText.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) confirmBtnText.font = mainFont;
        }

        private void RollDice()
        {
            currentRoll = GambleDiceManager.Roll(currentNexusCar);
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (currentRoll == null || currentNexusCar == null) return;

            int level = currentNexusCar.level;
            bool hasChaos = currentNexusCar.HasPartEffect(TrainPartEffectType.GambleChaosDice);
            subInfoText.text = $"<color=yellow>넥서스 Lv.{level}</color> | {GambleDiceManager.GetDiceDescription(level, hasChaos)}";

            // Clear old dice items
            foreach (Transform child in diceContainer)
            {
                Destroy(child.gameObject);
            }

            // Render each die card
            for (int i = 0; i < currentRoll.dice.Count; i++)
            {
                var die = currentRoll.dice[i];
                GameObject dieCard = new GameObject($"Die_{i}");
                dieCard.transform.SetParent(diceContainer, false);
                RectTransform dcRect = dieCard.AddComponent<RectTransform>();
                dcRect.sizeDelta = new Vector2(105, 105);

                Image dcBg = dieCard.AddComponent<Image>();
                dcBg.color = new Color(0.16f, 0.22f, 0.34f, 1f);

                VerticalLayoutGroup dVlg = dieCard.AddComponent<VerticalLayoutGroup>();
                dVlg.padding = new RectOffset(6, 6, 8, 8);
                dVlg.spacing = 2;
                dVlg.childAlignment = TextAnchor.MiddleCenter;
                dVlg.childControlWidth = true;
                dVlg.childControlHeight = true;
                dVlg.childForceExpandWidth = true;
                dVlg.childForceExpandHeight = false;

                // Value Display
                GameObject valObj = new GameObject("ValueText");
                valObj.transform.SetParent(dieCard.transform, false);
                LayoutElement vLe = valObj.AddComponent<LayoutElement>();
                vLe.preferredHeight = 52;
                vLe.flexibleWidth = 1f;
                TextMeshProUGUI vTmp = valObj.AddComponent<TextMeshProUGUI>();
                string valColor = die.wasAdjustedByMisfortune ? "#FFCC00" : "#FFFFFF";
                vTmp.text = $"<color={valColor}><b>{die.finalValue}</b></color>";
                vTmp.fontSize = 36;
                vTmp.alignment = TextAlignmentOptions.Center;
                if (mainFont != null) vTmp.font = mainFont;

                // Sub Info (dice type / adjustment)
                GameObject subBadgeObj = new GameObject("BadgeText");
                subBadgeObj.transform.SetParent(dieCard.transform, false);
                LayoutElement bLe = subBadgeObj.AddComponent<LayoutElement>();
                bLe.preferredHeight = 26;
                bLe.flexibleWidth = 1f;
                TextMeshProUGUI bTmp = subBadgeObj.AddComponent<TextMeshProUGUI>();
                string badge = die.wasAdjustedByMisfortune ? "<color=#FF9900>불운(1→2)</color>" : $"<color=#88BBFF>d{die.sides}</color>";
                bTmp.text = badge;
                bTmp.fontSize = 13;
                bTmp.alignment = TextAlignmentOptions.Center;
                if (mainFont != null) bTmp.font = mainFont;
            }

            // Summary text
            string sum = currentRoll.summary;
            if (currentRoll.isPairBonusTriggered)
            {
                sum += "\n<size=14><color=#66FFCC>[에너지쌍] 같은 숫자 2개! 행동력 +2 추가 획득!</color></size>";
            }
            resultSummaryText.text = sum;

            // Reroll Button status
            rerollBtnText.text = $"다시 굴리기 (남은 횟수: {remainingRerolls})";
            rerollButton.interactable = remainingRerolls > 0;
            if (rerollBtnImg != null)
            {
                rerollBtnImg.color = remainingRerolls > 0 ? new Color(0.28f, 0.35f, 0.70f, 1f) : new Color(0.35f, 0.35f, 0.40f, 1f);
            }

            // Confirm Button
            confirmBtnText.text = $"행동력 확정 (+{currentRoll.totalGainedAP} AP)";
        }

        private void OnRerollClicked()
        {
            if (remainingRerolls <= 0) return;
            remainingRerolls--;
            RollDice();
            NotificationManager.Instance?.ShowMessage("주사위를 다시 굴렸습니다!", Color.cyan);
        }

        private void OnConfirmClicked()
        {
            int ap = currentRoll != null ? currentRoll.totalGainedAP : 4;
            popupPanel.SetActive(false);
            onConfirmCallback?.Invoke(ap);
        }
    }
}
