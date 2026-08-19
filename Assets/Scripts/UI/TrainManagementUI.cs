using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using TheLastArk.Managers;
using TheLastArk.Data;
using TheLastArk.Character;

namespace TheLastArk.UI
{
    public class TrainManagementUI : MonoBehaviour
    {
        private GameObject popupPanel;
        private Transform carsContainer;
        private Transform charactersContainer;
        private TMP_FontAsset mainFont;
        private TextMeshProUGUI statusSummaryText;

        // Popup details
        private GameObject detailPopupPanel;
        private Image detailPortrait;
        private TextMeshProUGUI detailStatsText;
        private TextMeshProUGUI detailSkillsText;

        // Part install modal
        private GameObject partModalPanel;
        private Transform partModalContent;
        private TextMeshProUGUI partModalTitle;

        // Optional Car Build modal
        private GameObject buildModalPanel;
        private Transform buildModalContent;
        private TextMeshProUGUI buildModalTitle;
        private int currentBuildingSlotIndex = 1;

        // Synergy Select modal
        private GameObject synergyModalPanel;
        private Transform synergyModalContent;
        private TextMeshProUGUI synergyModalTitle;

        // Module Select modal
        private GameObject moduleModalPanel;
        private Transform moduleModalContent;
        private TextMeshProUGUI moduleModalTitle;

        public void Show()
        {
            if (popupPanel == null)
            {
                CreateUI();
            }
            UpdateUI();
            popupPanel.SetActive(true);
        }

        public void Hide()
        {
            if (popupPanel != null) popupPanel.SetActive(false);
            if (detailPopupPanel != null) detailPopupPanel.SetActive(false);
            if (partModalPanel != null) partModalPanel.SetActive(false);
            if (buildModalPanel != null) buildModalPanel.SetActive(false);
            if (synergyModalPanel != null) synergyModalPanel.SetActive(false);
            if (moduleModalPanel != null) moduleModalPanel.SetActive(false);
        }

        private void CreateUI()
        {
            mainFont = Resources.Load<TMP_FontAsset>("Fonts/Main_Fonts");

            Canvas canvas = FindObjectOfType<Canvas>();
            popupPanel = new GameObject("TrainManagementPopup");
            popupPanel.transform.SetParent(canvas.transform, false);
            popupPanel.transform.SetAsLastSibling();

            RectTransform rect = popupPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = popupPanel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.07f, 0.11f, 0.96f);

            // 닫기 버튼
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(popupPanel.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-30, -30);
            closeRect.sizeDelta = new Vector2(100, 48);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.color = new Color(0.85f, 0.25f, 0.25f, 1f);

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);

            CreateTextUI(closeBtnObj.transform, "닫기", 22, Color.white);

            // Title
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(popupPanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.93f);
            titleRect.anchorMax = new Vector2(0.95f, 0.98f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            CreateTextUI(titleObj.transform, "기차 관리 (칸 강화, 파츠 및 선택 칸 관리)", 30, new Color(1f, 0.85f, 0.3f, 1f));

            // 기차 체력바 및 상태 요약 영역
            GameObject hpBarArea = new GameObject("TrainHpArea");
            hpBarArea.transform.SetParent(popupPanel.transform, false);
            RectTransform hpRect = hpBarArea.AddComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0.5f, 0.90f);
            hpRect.anchorMax = new Vector2(0.5f, 0.90f);
            hpRect.pivot = new Vector2(0.5f, 1f);
            hpRect.anchoredPosition = new Vector2(0, 0);
            hpRect.sizeDelta = new Vector2(650, 30);

            Image hpBg = hpBarArea.AddComponent<Image>();
            hpBg.color = new Color(0.18f, 0.2f, 0.25f, 1f);

            GameObject hpFill = new GameObject("Fill");
            hpFill.transform.SetParent(hpBarArea.transform, false);
            RectTransform fillRect = hpFill.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image hpImg = hpFill.AddComponent<Image>();
            hpImg.color = new Color(0.2f, 0.75f, 0.4f, 1f);
            
            GameObject hpTextObj = new GameObject("HpText");
            hpTextObj.transform.SetParent(hpBarArea.transform, false);
            RectTransform hpTextRect = hpTextObj.AddComponent<RectTransform>();
            hpTextRect.anchorMin = Vector2.zero;
            hpTextRect.anchorMax = Vector2.one;
            hpTextRect.offsetMin = Vector2.zero;
            hpTextRect.offsetMax = Vector2.zero;
            TextMeshProUGUI hpText = hpTextObj.AddComponent<TextMeshProUGUI>();
            hpText.fontSize = 18;
            hpText.alignment = TextAlignmentOptions.Center;
            hpText.color = Color.white;
            if (mainFont != null) hpText.font = mainFont;

            hpBarArea.AddComponent<TrainHpUpdater>().Init(hpRect, fillRect, hpText);

            // Summary Status text (AP info / Crew Capacity info)
            GameObject summaryObj = new GameObject("StatusSummaryText");
            summaryObj.transform.SetParent(popupPanel.transform, false);
            RectTransform summaryRect = summaryObj.AddComponent<RectTransform>();
            summaryRect.anchorMin = new Vector2(0.05f, 0.83f);
            summaryRect.anchorMax = new Vector2(0.95f, 0.87f);
            summaryRect.offsetMin = Vector2.zero;
            summaryRect.offsetMax = Vector2.zero;
            statusSummaryText = summaryObj.AddComponent<TextMeshProUGUI>();
            statusSummaryText.fontSize = 19;
            statusSummaryText.alignment = TextAlignmentOptions.Center;
            statusSummaryText.color = new Color(0.7f, 0.85f, 1f, 1f);
            if (mainFont != null) statusSummaryText.font = mainFont;

            // Top area: train cars (4 Slots)
            GameObject carsArea = new GameObject("CarsArea");
            carsArea.transform.SetParent(popupPanel.transform, false);
            RectTransform carsAreaRect = carsArea.AddComponent<RectTransform>();
            carsAreaRect.anchorMin = new Vector2(0.02f, 0.42f);
            carsAreaRect.anchorMax = new Vector2(0.98f, 0.82f);
            carsAreaRect.offsetMin = Vector2.zero;
            carsAreaRect.offsetMax = Vector2.zero;

            Image carsBg = carsArea.AddComponent<Image>();
            carsBg.color = new Color(0.08f, 0.11f, 0.16f, 0.9f);

            ScrollRect carsScroll = carsArea.AddComponent<ScrollRect>();
            carsScroll.horizontal = true;
            carsScroll.vertical = false;
            
            GameObject carsViewport = new GameObject("Viewport");
            carsViewport.transform.SetParent(carsArea.transform, false);
            RectTransform cVpRect = carsViewport.AddComponent<RectTransform>();
            cVpRect.anchorMin = Vector2.zero;
            cVpRect.anchorMax = Vector2.one;
            cVpRect.sizeDelta = Vector2.zero;
            carsViewport.AddComponent<RectMask2D>();

            GameObject carsContent = new GameObject("Content");
            carsContent.transform.SetParent(carsViewport.transform, false);
            RectTransform cContentRect = carsContent.AddComponent<RectTransform>();
            cContentRect.anchorMin = new Vector2(0, 0);
            cContentRect.anchorMax = new Vector2(0, 1);
            cContentRect.pivot = new Vector2(0, 0.5f);
            cContentRect.sizeDelta = new Vector2(0, 0);

            HorizontalLayoutGroup carsLayout = carsContent.AddComponent<HorizontalLayoutGroup>();
            carsLayout.padding = new RectOffset(12, 12, 10, 10);
            carsLayout.spacing = 12;
            carsLayout.childControlWidth = false;
            carsLayout.childControlHeight = true;
            carsLayout.childForceExpandWidth = false;

            ContentSizeFitter carsFitter = carsContent.AddComponent<ContentSizeFitter>();
            carsFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            carsScroll.viewport = cVpRect;
            carsScroll.content = cContentRect;
            carsContainer = carsContent.transform;

            // Bottom area: character list header
            GameObject charHeaderArea = new GameObject("CharHeader");
            charHeaderArea.transform.SetParent(popupPanel.transform, false);
            RectTransform chRect = charHeaderArea.AddComponent<RectTransform>();
            chRect.anchorMin = new Vector2(0.03f, 0.35f);
            chRect.anchorMax = new Vector2(0.97f, 0.40f);
            chRect.offsetMin = Vector2.zero;
            chRect.offsetMax = Vector2.zero;

            CreateTextUI(charHeaderArea.transform, "보유 승무원 목록", 24, Color.yellow, new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(0.5f, 1));

            // Bottom area: character list
            GameObject charsArea = new GameObject("CharactersArea");
            charsArea.transform.SetParent(popupPanel.transform, false);
            RectTransform charsAreaRect = charsArea.AddComponent<RectTransform>();
            charsAreaRect.anchorMin = new Vector2(0.03f, 0.03f);
            charsAreaRect.anchorMax = new Vector2(0.97f, 0.34f);
            charsAreaRect.offsetMin = Vector2.zero;
            charsAreaRect.offsetMax = Vector2.zero;

            Image charsBg = charsArea.AddComponent<Image>();
            charsBg.color = new Color(0.08f, 0.11f, 0.16f, 0.9f);

            ScrollRect charsScroll = charsArea.AddComponent<ScrollRect>();
            charsScroll.horizontal = true;
            charsScroll.vertical = false;

            GameObject charsViewport = new GameObject("Viewport");
            charsViewport.transform.SetParent(charsArea.transform, false);
            RectTransform chVpRect = charsViewport.AddComponent<RectTransform>();
            chVpRect.anchorMin = Vector2.zero;
            chVpRect.anchorMax = Vector2.one;
            chVpRect.sizeDelta = Vector2.zero;
            charsViewport.AddComponent<RectMask2D>();

            GameObject charsContent = new GameObject("Content");
            charsContent.transform.SetParent(charsViewport.transform, false);
            RectTransform chContentRect = charsContent.AddComponent<RectTransform>();
            chContentRect.anchorMin = new Vector2(0, 0);
            chContentRect.anchorMax = new Vector2(0, 1);
            chContentRect.pivot = new Vector2(0, 0.5f);
            chContentRect.sizeDelta = new Vector2(0, 0);

            HorizontalLayoutGroup charsLayout = charsContent.AddComponent<HorizontalLayoutGroup>();
            charsLayout.padding = new RectOffset(12, 12, 10, 10);
            charsLayout.spacing = 12;
            charsLayout.childControlWidth = false;
            charsLayout.childControlHeight = true;
            charsLayout.childForceExpandWidth = false;

            ContentSizeFitter charsFitter = charsContent.AddComponent<ContentSizeFitter>();
            charsFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            charsScroll.viewport = chVpRect;
            charsScroll.content = chContentRect;
            charactersContainer = charsContent.transform;

            CreateDetailPopup(canvas.transform);
            CreatePartModal(canvas.transform);
            CreateBuildModal(canvas.transform);
            CreateSynergyModal(canvas.transform);
            CreateModuleModal(canvas.transform);
        }

        private void CreateDetailPopup(Transform parent)
        {
            detailPopupPanel = new GameObject("DetailPopup");
            detailPopupPanel.transform.SetParent(parent, false);
            detailPopupPanel.transform.SetAsLastSibling();

            RectTransform rect = detailPopupPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.15f);
            rect.anchorMax = new Vector2(0.8f, 0.85f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = detailPopupPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.15f, 0.98f);

            // Illustration
            GameObject illObj = new GameObject("Illustration");
            illObj.transform.SetParent(detailPopupPanel.transform, false);
            RectTransform illRect = illObj.AddComponent<RectTransform>();
            illRect.anchorMin = new Vector2(0.05f, 0.1f);
            illRect.anchorMax = new Vector2(0.45f, 0.9f);
            illRect.offsetMin = Vector2.zero;
            illRect.offsetMax = Vector2.zero;
            detailPortrait = illObj.AddComponent<Image>();
            detailPortrait.preserveAspect = true;

            // Stats text
            GameObject statsObj = new GameObject("StatsText");
            statsObj.transform.SetParent(detailPopupPanel.transform, false);
            RectTransform statsRect = statsObj.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.5f, 0.5f);
            statsRect.anchorMax = new Vector2(0.95f, 0.9f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;
            detailStatsText = statsObj.AddComponent<TextMeshProUGUI>();
            detailStatsText.fontSize = 20;
            detailStatsText.color = Color.white;
            if (mainFont != null) detailStatsText.font = mainFont;

            // Skills text
            GameObject skillsObj = new GameObject("SkillsText");
            skillsObj.transform.SetParent(detailPopupPanel.transform, false);
            RectTransform skillsRect = skillsObj.AddComponent<RectTransform>();
            skillsRect.anchorMin = new Vector2(0.5f, 0.15f);
            skillsRect.anchorMax = new Vector2(0.95f, 0.48f);
            skillsRect.offsetMin = Vector2.zero;
            skillsRect.offsetMax = Vector2.zero;
            detailSkillsText = skillsObj.AddComponent<TextMeshProUGUI>();
            detailSkillsText.fontSize = 18;
            detailSkillsText.color = Color.white;
            if (mainFont != null) detailSkillsText.font = mainFont;

            // Close button
            GameObject closeBtnObj = new GameObject("CloseDetailButton");
            closeBtnObj.transform.SetParent(detailPopupPanel.transform, false);
            RectTransform cbRect = closeBtnObj.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.9f, 0.9f);
            cbRect.anchorMax = new Vector2(0.98f, 0.98f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;
            Image cbImg = closeBtnObj.AddComponent<Image>();
            cbImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            Button cb = closeBtnObj.AddComponent<Button>();
            cb.onClick.AddListener(() => detailPopupPanel.SetActive(false));
            CreateTextUI(closeBtnObj.transform, "X", 24, Color.white);

            detailPopupPanel.SetActive(false);
        }

        private void CreatePartModal(Transform parent)
        {
            partModalPanel = new GameObject("PartModalPopup");
            partModalPanel.transform.SetParent(parent, false);
            partModalPanel.transform.SetAsLastSibling();

            RectTransform rect = partModalPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.25f, 0.2f);
            rect.anchorMax = new Vector2(0.75f, 0.8f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = partModalPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.12f, 0.18f, 0.98f);

            // Title
            GameObject titleObj = new GameObject("PartModalTitle");
            titleObj.transform.SetParent(partModalPanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.88f);
            titleRect.anchorMax = new Vector2(0.85f, 0.96f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            partModalTitle = titleObj.AddComponent<TextMeshProUGUI>();
            partModalTitle.fontSize = 24;
            partModalTitle.color = Color.yellow;
            if (mainFont != null) partModalTitle.font = mainFont;

            // Close button
            GameObject closeBtnObj = new GameObject("ClosePartModalButton");
            closeBtnObj.transform.SetParent(partModalPanel.transform, false);
            RectTransform cbRect = closeBtnObj.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.88f, 0.88f);
            cbRect.anchorMax = new Vector2(0.96f, 0.96f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;
            Image cbImg = closeBtnObj.AddComponent<Image>();
            cbImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            Button cb = closeBtnObj.AddComponent<Button>();
            cb.onClick.AddListener(() => partModalPanel.SetActive(false));
            CreateTextUI(closeBtnObj.transform, "X", 22, Color.white);

            // Scroll view for parts
            GameObject svObj = new GameObject("PartsScrollView");
            svObj.transform.SetParent(partModalPanel.transform, false);
            RectTransform svRect = svObj.AddComponent<RectTransform>();
            svRect.anchorMin = new Vector2(0.05f, 0.05f);
            svRect.anchorMax = new Vector2(0.95f, 0.85f);
            svRect.offsetMin = Vector2.zero;
            svRect.offsetMax = Vector2.zero;

            ScrollRect sv = svObj.AddComponent<ScrollRect>();
            sv.horizontal = false;
            sv.vertical = true;

            GameObject vpObj = new GameObject("Viewport");
            vpObj.transform.SetParent(svObj.transform, false);
            RectTransform vpRect = vpObj.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            vpObj.AddComponent<RectMask2D>();

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(vpObj.transform, false);
            RectTransform cRect = contentObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            cRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 10;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sv.viewport = vpRect;
            sv.content = cRect;
            partModalContent = contentObj.transform;

            partModalPanel.SetActive(false);
        }

        private void CreateBuildModal(Transform parent)
        {
            buildModalPanel = new GameObject("BuildModalPopup");
            buildModalPanel.transform.SetParent(parent, false);
            buildModalPanel.transform.SetAsLastSibling();

            RectTransform rect = buildModalPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.22f, 0.18f);
            rect.anchorMax = new Vector2(0.78f, 0.82f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = buildModalPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.12f, 0.18f, 0.98f);

            // Title
            GameObject titleObj = new GameObject("BuildModalTitle");
            titleObj.transform.SetParent(buildModalPanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.88f);
            titleRect.anchorMax = new Vector2(0.85f, 0.96f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            buildModalTitle = titleObj.AddComponent<TextMeshProUGUI>();
            buildModalTitle.fontSize = 24;
            buildModalTitle.color = Color.yellow;
            if (mainFont != null) buildModalTitle.font = mainFont;

            // Close button
            GameObject closeBtnObj = new GameObject("CloseBuildModalButton");
            closeBtnObj.transform.SetParent(buildModalPanel.transform, false);
            RectTransform cbRect = closeBtnObj.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.88f, 0.88f);
            cbRect.anchorMax = new Vector2(0.96f, 0.96f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;
            Image cbImg = closeBtnObj.AddComponent<Image>();
            cbImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            Button cb = closeBtnObj.AddComponent<Button>();
            cb.onClick.AddListener(() => buildModalPanel.SetActive(false));
            CreateTextUI(closeBtnObj.transform, "X", 22, Color.white);

            // Scroll view
            GameObject svObj = new GameObject("BuildScrollView");
            svObj.transform.SetParent(buildModalPanel.transform, false);
            RectTransform svRect = svObj.AddComponent<RectTransform>();
            svRect.anchorMin = new Vector2(0.05f, 0.05f);
            svRect.anchorMax = new Vector2(0.95f, 0.85f);
            svRect.offsetMin = Vector2.zero;
            svRect.offsetMax = Vector2.zero;

            ScrollRect sv = svObj.AddComponent<ScrollRect>();
            sv.horizontal = false;
            sv.vertical = true;

            GameObject vpObj = new GameObject("Viewport");
            vpObj.transform.SetParent(svObj.transform, false);
            RectTransform vpRect = vpObj.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            vpObj.AddComponent<RectMask2D>();

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(vpObj.transform, false);
            RectTransform cRect = contentObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            cRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 10;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sv.viewport = vpRect;
            sv.content = cRect;
            buildModalContent = contentObj.transform;

            buildModalPanel.SetActive(false);
        }

        private void CreateSynergyModal(Transform parent)
        {
            synergyModalPanel = new GameObject("SynergyModalPopup");
            synergyModalPanel.transform.SetParent(parent, false);
            synergyModalPanel.transform.SetAsLastSibling();

            RectTransform rect = synergyModalPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.22f, 0.15f);
            rect.anchorMax = new Vector2(0.78f, 0.85f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = synergyModalPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.12f, 0.18f, 0.98f);

            // Title
            GameObject titleObj = new GameObject("SynergyModalTitle");
            titleObj.transform.SetParent(synergyModalPanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.88f);
            titleRect.anchorMax = new Vector2(0.85f, 0.96f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            synergyModalTitle = titleObj.AddComponent<TextMeshProUGUI>();
            synergyModalTitle.fontSize = 24;
            synergyModalTitle.color = Color.yellow;
            if (mainFont != null) synergyModalTitle.font = mainFont;

            // Close button
            GameObject closeBtnObj = new GameObject("CloseSynergyModalButton");
            closeBtnObj.transform.SetParent(synergyModalPanel.transform, false);
            RectTransform cbRect = closeBtnObj.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.88f, 0.88f);
            cbRect.anchorMax = new Vector2(0.96f, 0.96f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;
            Image cbImg = closeBtnObj.AddComponent<Image>();
            cbImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            Button cb = closeBtnObj.AddComponent<Button>();
            cb.onClick.AddListener(() => synergyModalPanel.SetActive(false));
            CreateTextUI(closeBtnObj.transform, "X", 22, Color.white);

            // Scroll view
            GameObject svObj = new GameObject("SynergyScrollView");
            svObj.transform.SetParent(synergyModalPanel.transform, false);
            RectTransform svRect = svObj.AddComponent<RectTransform>();
            svRect.anchorMin = new Vector2(0.05f, 0.05f);
            svRect.anchorMax = new Vector2(0.95f, 0.85f);
            svRect.offsetMin = Vector2.zero;
            svRect.offsetMax = Vector2.zero;

            ScrollRect sv = svObj.AddComponent<ScrollRect>();
            sv.horizontal = false;
            sv.vertical = true;

            GameObject vpObj = new GameObject("Viewport");
            vpObj.transform.SetParent(svObj.transform, false);
            RectTransform vpRect = vpObj.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            vpObj.AddComponent<RectMask2D>();

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(vpObj.transform, false);
            RectTransform cRect = contentObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            cRect.sizeDelta = new Vector2(0, 0);

            GridLayoutGroup glg = contentObj.AddComponent<GridLayoutGroup>();
            glg.padding = new RectOffset(10, 10, 10, 10);
            glg.spacing = new Vector2(10, 10);
            glg.cellSize = new Vector2(240, 50);

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sv.viewport = vpRect;
            sv.content = cRect;
            synergyModalContent = contentObj.transform;

            synergyModalPanel.SetActive(false);
        }

        private void CreateModuleModal(Transform parent)
        {
            moduleModalPanel = new GameObject("ModuleModalPopup");
            moduleModalPanel.transform.SetParent(parent, false);
            moduleModalPanel.transform.SetAsLastSibling();

            RectTransform rect = moduleModalPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.20f, 0.15f);
            rect.anchorMax = new Vector2(0.80f, 0.85f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = moduleModalPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.12f, 0.18f, 0.98f);

            // Title
            GameObject titleObj = new GameObject("ModuleModalTitle");
            titleObj.transform.SetParent(moduleModalPanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.88f);
            titleRect.anchorMax = new Vector2(0.85f, 0.96f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            moduleModalTitle = titleObj.AddComponent<TextMeshProUGUI>();
            moduleModalTitle.fontSize = 24;
            moduleModalTitle.color = Color.yellow;
            if (mainFont != null) moduleModalTitle.font = mainFont;

            // Close button
            GameObject closeBtnObj = new GameObject("CloseModuleModalButton");
            closeBtnObj.transform.SetParent(moduleModalPanel.transform, false);
            RectTransform cbRect = closeBtnObj.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.88f, 0.88f);
            cbRect.anchorMax = new Vector2(0.96f, 0.96f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;
            Image cbImg = closeBtnObj.AddComponent<Image>();
            cbImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            Button cb = closeBtnObj.AddComponent<Button>();
            cb.onClick.AddListener(() => moduleModalPanel.SetActive(false));
            CreateTextUI(closeBtnObj.transform, "X", 22, Color.white);

            // Scroll view
            GameObject svObj = new GameObject("ModuleScrollView");
            svObj.transform.SetParent(moduleModalPanel.transform, false);
            RectTransform svRect = svObj.AddComponent<RectTransform>();
            svRect.anchorMin = new Vector2(0.05f, 0.05f);
            svRect.anchorMax = new Vector2(0.85f, 0.85f);
            svRect.offsetMin = Vector2.zero;
            svRect.offsetMax = Vector2.zero;

            ScrollRect sv = svObj.AddComponent<ScrollRect>();
            sv.horizontal = false;
            sv.vertical = true;

            GameObject vpObj = new GameObject("Viewport");
            vpObj.transform.SetParent(svObj.transform, false);
            RectTransform vpRect = vpObj.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            vpObj.AddComponent<RectMask2D>();

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(vpObj.transform, false);
            RectTransform cRect = contentObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            cRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 10;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sv.viewport = vpRect;
            sv.content = cRect;
            moduleModalContent = contentObj.transform;

            moduleModalPanel.SetActive(false);
        }

        private void OpenModuleSelectModal(TrainCar car)
        {
            if (car == null || car.carType != TrainCarType.Nexus) return;

            string curModName = NexusModuleDatabase.GetModule(car.installedModuleId)?.moduleName ?? "오리진";
            moduleModalTitle.text = $"넥서스 칸 모듈 교체 (현재: {curModName})";

            foreach (Transform child in moduleModalContent) Destroy(child.gameObject);

            var allModules = NexusModuleDatabase.GetAllModules();
            foreach (var mod in allModules)
            {
                var modData = mod;
                bool isCurrent = (car.installedModuleId == modData.moduleId) ||
                                 (string.IsNullOrEmpty(car.installedModuleId) && modData.moduleId == NexusModuleDatabase.OriginId);

                GameObject itemObj = new GameObject($"ModuleItem_{modData.moduleId}");
                itemObj.transform.SetParent(moduleModalContent, false);
                RectTransform iRect = itemObj.AddComponent<RectTransform>();
                iRect.sizeDelta = new Vector2(0, 120);
                LayoutElement iLe = itemObj.AddComponent<LayoutElement>();
                iLe.preferredHeight = 120;

                Image iBg = itemObj.AddComponent<Image>();
                iBg.color = isCurrent ? new Color(0.18f, 0.28f, 0.40f, 1f) : new Color(0.12f, 0.16f, 0.24f, 1f);

                HorizontalLayoutGroup hlg = itemObj.AddComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(15, 15, 10, 10);
                hlg.spacing = 15;
                hlg.childControlWidth = false;
                hlg.childControlHeight = true;

                // Info Area
                GameObject infoObj = new GameObject("Info");
                infoObj.transform.SetParent(itemObj.transform, false);
                LayoutElement infoLe = infoObj.AddComponent<LayoutElement>();
                infoLe.preferredWidth = 440;

                string badge = isCurrent ? " <color=cyan>[현재 장착 중]</color>" : "";
                string specs = $"<color=#88CCFF>최대 강화: Lv.{modData.maxLevel} | 강화 비용: {modData.baseUpgradeCost}G | 파츠 슬롯: {modData.basePartSlots}개</color>";
                
                // Get compatible parts
                var parts = TrainPartDatabase.GetPartsForNexusModule(modData.moduleId);
                string partList = parts.Count > 0 ? string.Join(", ", parts.Select(p => p.partName)) : "없음";
                string partsInfo = $"<size=13><color=#AAAAAA>전용 파츠: {partList}</color></size>";

                CreateTextUI(infoObj.transform, $"<color=yellow><b>{modData.moduleName}</b></color>{badge}\n<size=14>{modData.description}</size>\n{specs}\n{partsInfo}", 15, Color.white, new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(1, 1));

                // Action Button
                GameObject actBtnObj = new GameObject("ActionButton");
                actBtnObj.transform.SetParent(itemObj.transform, false);
                LayoutElement actLe = actBtnObj.AddComponent<LayoutElement>();
                actLe.preferredWidth = 110;

                Image actImg = actBtnObj.AddComponent<Image>();
                Button actBtn = actBtnObj.AddComponent<Button>();

                if (isCurrent)
                {
                    actImg.color = new Color(0.35f, 0.35f, 0.40f, 1f);
                    actBtn.interactable = false;
                    CreateTextUI(actBtnObj.transform, "장착 중", 15, Color.gray);
                }
                else
                {
                    actImg.color = new Color(0.20f, 0.65f, 0.35f, 1f);
                    actBtn.interactable = true;
                    CreateTextUI(actBtnObj.transform, "모듈 장착", 15, Color.white);

                    actBtn.onClick.AddListener(() =>
                    {
                        if (TrainManager.Instance.TryChangeNexusModule(modData.moduleId))
                        {
                            NotificationManager.Instance?.ShowMessage($"[{modData.moduleName}] 모듈 장착 완료!", Color.green);
                            moduleModalPanel.SetActive(false);
                            UpdateUI();
                        }
                    });
                }
            }

            moduleModalPanel.SetActive(true);
        }

        private TextMeshProUGUI CreateTextUI(Transform parent, string text, float fontSize, Color color,
            Vector2 pivot = default, Vector2 anchorMin = default, Vector2 anchorMax = default)
        {
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(parent, false);
            RectTransform rect = txtObj.AddComponent<RectTransform>();
            
            if (anchorMin != default || anchorMax != default)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.sizeDelta = new Vector2(300, 50);
            }
            
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) tmp.font = mainFont;

            return tmp;
        }

        private void UpdateUI()
        {
            UpdateSummaryText();
            UpdateCarsUI();
            UpdateCharactersUI();
        }

        private void UpdateSummaryText()
        {
            if (statusSummaryText == null || TrainManager.Instance == null) return;

            int baseAP = TrainManager.Instance.GetNexusBaseAP();
            int currentCrew = TrainManager.Instance.GetCurrentCrewCount();
            int maxCrew = TrainManager.Instance.GetMaxCrewCapacity();
            int curGold = ResourceManager.Instance != null ? ResourceManager.Instance.Gold : 0;

            statusSummaryText.text = $"<color=yellow>보유 골드: {curGold}G</color>  |  " +
                                     $"<color=#66CCFF>넥서스 기본 AP: {baseAP}</color>  |  " +
                                     $"<color=#66FF99>승무원실 수용량: {currentCrew} / {maxCrew}명</color>";
        }

        private void UpdateCarsUI()
        {
            foreach (Transform child in carsContainer) Destroy(child.gameObject);

            if (TrainManager.Instance == null) return;

            var allCars = TrainManager.Instance.GetAllCars();
            for (int i = 0; i < allCars.Count; i++)
            {
                CreateCarCard(allCars[i], i + 1);
            }
        }

        private void CreateCarCard(TrainCar car, int slotIndex)
        {
            if (car == null) return;

            GameObject carObj = new GameObject($"Car_Slot_{slotIndex}");
            carObj.transform.SetParent(carsContainer, false);
            
            RectTransform rect = carObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320, 340);
            LayoutElement layout = carObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 320;
            layout.preferredHeight = 340;

            Image bg = carObj.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.18f, 0.25f, 1f);

            VerticalLayoutGroup vLayout = carObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(15, 15, 12, 12);
            vLayout.spacing = 6;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;

            // 1. Slot badge & Car Name
            string badgeText = car.carType switch
            {
                TrainCarType.Nexus => $"[고정 {slotIndex}] 넥서스 칸",
                TrainCarType.CrewQuarters => $"[고정 {slotIndex}] 승무원실",
                TrainCarType.Infirmary => $"[선택 {slotIndex}] 의무실",
                TrainCarType.CombatEnhancement => $"[선택 {slotIndex}] 전투 강화소",
                TrainCarType.PrayerRoom => $"[선택 {slotIndex}] 기도실",
                TrainCarType.TraitTrainingCamp => $"[선택 {slotIndex}] 특성 훈련소",
                _ => $"[선택 {slotIndex}] 선택 칸 (미건설)"
            };

            string levelText = car.carType != TrainCarType.Optional
                ? $"\n<size=16>강화 레벨: Lv.{car.level} / {car.MaxLevel}</size>"
                : "\n<size=16><color=gray>미건설 상태</color></size>";

            CreateTextUI(carObj.transform, $"<color=#FFCC00><b>{badgeText}</b></color>{levelText}", 19, Color.white)
                .rectTransform.sizeDelta = new Vector2(290, 44);

            // 2. Sub Info
            string subInfo = car.carType switch
            {
                TrainCarType.Nexus => car.installedModuleId switch
                {
                    NexusModuleDatabase.GambleId => $"<color=#66CCFF>모듈: 갬블 ({Battle.GambleDiceManager.GetDiceDescription(car.level, car.HasPartEffect(TrainPartEffectType.GambleChaosDice))})</color>",
                    NexusModuleDatabase.LimitId => $"<color=#66CCFF>모듈: 리미트 (전환 {(Battle.LimitCardManager.GetRatio(car.level) * 100):F0}%, 한도 {Battle.LimitCardManager.GetThreshold(car)})</color>",
                    NexusModuleDatabase.ClusterId => $"<color=#66CCFF>모듈: 클러스터 (재시도 {Battle.ClusterCardManager.GetMaxRerolls(car)}회, 분모 {Mathf.Max(1, 5 - car.level)})</color>",
                    NexusModuleDatabase.ArcanaId => $"<color=#66CCFF>모듈: 아르카나 (AC = {car.level}, 해금 카드 {Battle.ArcanaCardManager.GetAvailableCardPool(car, null).Count}종)</color>",
                    NexusModuleDatabase.SinId => $"<color=#66CCFF>모듈: 씬 (3턴 주기, 해금 죄악 {Battle.SinModuleManager.GetAvailableSinPool(car).Count}종)</color>",
                    _ => $"<color=#66CCFF>모듈: 오리진 (기본 AP +{car.level + 4})</color>"
                },
                TrainCarType.CrewQuarters => $"<color=#66FF99>최대 수용: {TrainManager.Instance.GetMaxCrewCapacity()}명 (기본 {8 + car.level}명)</color>",
                TrainCarType.Infirmary => $"<color=#66FF99>전투 종료 시 아군 체력 +{2 + car.level} 회복</color>",
                TrainCarType.CombatEnhancement => $"<color=#FF9966>전투 시 공격력/주문력 +{car.level * 5}%</color>",
                TrainCarType.PrayerRoom => $"<color=#FFCC66>전투 중 치유량 +{(TrainManager.Instance.GetPrayerRoomHealMultiplier() * 100):F0}%</color>",
                TrainCarType.TraitTrainingCamp => $"<color=#CC99FF>시너지 +1 (선택: {car.selectedSynergies.Count}/{car.MaxSelectableSynergies})</color>",
                _ => "<color=gray>원하는 칸을 건설하여 효과를 활성화하세요.</color>"
            };
            CreateTextUI(carObj.transform, subInfo, 14, Color.white).rectTransform.sizeDelta = new Vector2(290, 20);

            // 3. Action / Upgrade / Build Button
            if (car.carType == TrainCarType.Optional)
            {
                // [선택 칸 건설 버튼 (100G)]
                GameObject buildBtnObj = new GameObject("BuildButton");
                buildBtnObj.transform.SetParent(carObj.transform, false);
                RectTransform bRect = buildBtnObj.AddComponent<RectTransform>();
                bRect.sizeDelta = new Vector2(290, 36);

                Image bImg = buildBtnObj.AddComponent<Image>();
                Button bBtn = buildBtnObj.AddComponent<Button>();

                int buildCost = TrainManager.Instance.GetDiscountedCost(TrainCar.OptionalCarBuildCost);
                bool canAfford = ResourceManager.Instance != null && ResourceManager.Instance.Gold >= buildCost;
                bImg.color = canAfford ? new Color(0.2f, 0.6f, 0.35f, 1f) : new Color(0.4f, 0.4f, 0.4f, 1f);
                bBtn.interactable = canAfford;

                CreateTextUI(buildBtnObj.transform, $"+ 선택 칸 건설 ({buildCost}G)", 16, Color.white);
                int optSlotIdx = slotIndex - 2; // slot 3 -> 1, slot 4 -> 2
                bBtn.onClick.AddListener(() => OpenBuildOptionalCarModal(optSlotIdx));
            }
            else
            {
                // Upgrade Button
                GameObject upBtnObj = new GameObject("UpgradeButton");
                upBtnObj.transform.SetParent(carObj.transform, false);
                RectTransform upRect = upBtnObj.AddComponent<RectTransform>();
                upRect.sizeDelta = new Vector2(290, 32);

                Image upImg = upBtnObj.AddComponent<Image>();
                Button upBtn = upBtnObj.AddComponent<Button>();

                if (car.CanUpgrade)
                {
                    int cost = TrainManager.Instance.GetDiscountedCost(car.UpgradeCost);
                    bool canAfford = ResourceManager.Instance != null && ResourceManager.Instance.Gold >= cost;
                    upImg.color = canAfford ? new Color(0.2f, 0.6f, 0.35f, 1f) : new Color(0.4f, 0.4f, 0.4f, 1f);
                    upBtn.interactable = canAfford;

                    string bonusLabel = car.carType switch
                    {
                        TrainCarType.Nexus => car.installedModuleId switch
                        {
                            NexusModuleDatabase.GambleId => "주사위 강화",
                            NexusModuleDatabase.LimitId => "+10% 비율",
                            NexusModuleDatabase.ClusterId => "재시도 +2회",
                            NexusModuleDatabase.ArcanaId => "카드 풀 확장",
                            NexusModuleDatabase.SinId => "죄악 풀 확장",
                            _ => "+1 AP"
                        },
                        TrainCarType.CrewQuarters => "+1명 수용",
                        TrainCarType.Infirmary => "+1 회복",
                        TrainCarType.CombatEnhancement => "+5% 공/주",
                        TrainCarType.PrayerRoom => "치유량 증가",
                        TrainCarType.TraitTrainingCamp => "+1 슬롯",
                        _ => "+1"
                    };
                    CreateTextUI(upBtnObj.transform, $"강화 ({cost}G) [{bonusLabel}]", 15, Color.white);

                    upBtn.onClick.AddListener(() =>
                    {
                        if (TrainManager.Instance.TryUpgradeCar(car))
                        {
                            NotificationManager.Instance?.ShowMessage($"{car.carName} Lv.{car.level} 강화 성공!", Color.green);
                            UpdateUI();
                        }
                        else
                        {
                            NotificationManager.Instance?.ShowMessage("골드가 부족합니다!", Color.red);
                        }
                    });
                }
                else
                {
                    upImg.color = new Color(0.3f, 0.3f, 0.35f, 1f);
                    upBtn.interactable = false;
                    CreateTextUI(upBtnObj.transform, "최대 강화 완료 (MAX)", 15, Color.yellow);
                }

                // Extra Action Buttons for Nexus (Change Module)
                if (car.carType == TrainCarType.Nexus)
                {
                    GameObject subActionsObj = new GameObject("NexusSubActions");
                    subActionsObj.transform.SetParent(carObj.transform, false);
                    RectTransform saRect = subActionsObj.AddComponent<RectTransform>();
                    saRect.sizeDelta = new Vector2(290, 28);

                    HorizontalLayoutGroup saHlg = subActionsObj.AddComponent<HorizontalLayoutGroup>();
                    saHlg.spacing = 8;
                    saHlg.childControlWidth = true;
                    saHlg.childControlHeight = true;

                    GameObject modBtnObj = new GameObject("ChangeModuleButton");
                    modBtnObj.transform.SetParent(subActionsObj.transform, false);
                    Image modImg = modBtnObj.AddComponent<Image>();
                    modImg.color = new Color(0.25f, 0.45f, 0.70f, 1f);
                    Button modBtn = modBtnObj.AddComponent<Button>();
                    CreateTextUI(modBtnObj.transform, "🔄 넥서스 모듈 교체", 14, Color.white);
                    modBtn.onClick.AddListener(() => OpenModuleSelectModal(car));
                }

                // Extra Action Buttons for Optional Cars
                if (car.IsBuiltOptionalCar)
                {
                    GameObject subActionsObj = new GameObject("SubActions");
                    subActionsObj.transform.SetParent(carObj.transform, false);
                    RectTransform saRect = subActionsObj.AddComponent<RectTransform>();
                    saRect.sizeDelta = new Vector2(290, 28);

                    HorizontalLayoutGroup saHlg = subActionsObj.AddComponent<HorizontalLayoutGroup>();
                    saHlg.spacing = 8;
                    saHlg.childControlWidth = true;
                    saHlg.childControlHeight = true;

                    // If TraitTrainingCamp, show Synergy Select button
                    if (car.carType == TrainCarType.TraitTrainingCamp)
                    {
                        GameObject synBtnObj = new GameObject("SynergySelectButton");
                        synBtnObj.transform.SetParent(subActionsObj.transform, false);
                        Image synImg = synBtnObj.AddComponent<Image>();
                        synImg.color = new Color(0.45f, 0.25f, 0.65f, 1f);
                        Button synBtn = synBtnObj.AddComponent<Button>();
                        CreateTextUI(synBtnObj.transform, "시너지 선택", 14, Color.white);
                        synBtn.onClick.AddListener(() => OpenSynergySelectModal(car));
                    }

                    // Dismantle Button
                    GameObject disBtnObj = new GameObject("DismantleButton");
                    disBtnObj.transform.SetParent(subActionsObj.transform, false);
                    Image disImg = disBtnObj.AddComponent<Image>();
                    int disCost = TrainManager.Instance.GetDiscountedCost(TrainCar.OptionalCarDismantleCost);
                    bool canAffordDis = ResourceManager.Instance != null && ResourceManager.Instance.Gold >= disCost;
                    disImg.color = canAffordDis ? new Color(0.7f, 0.25f, 0.25f, 1f) : new Color(0.4f, 0.4f, 0.4f, 1f);
                    Button disBtn = disBtnObj.AddComponent<Button>();
                    disBtn.interactable = canAffordDis;
                    CreateTextUI(disBtnObj.transform, $"철거 ({disCost}G)", 14, Color.white);

                    int optSlotIdx = slotIndex - 2;
                    disBtn.onClick.AddListener(() =>
                    {
                        if (TrainManager.Instance.TryDismantleOptionalCar(optSlotIdx))
                        {
                            NotificationManager.Instance?.ShowMessage($"{car.carName} 철거 완료", Color.gray);
                            UpdateUI();
                        }
                        else
                        {
                            NotificationManager.Instance?.ShowMessage("골드가 부족합니다!", Color.red);
                        }
                    });
                }
            }

            // 4. Parts Section Header
            int maxSlots = car.MaxPartSlots;
            CreateTextUI(carObj.transform, $"<color=#E0E0E0>장착 파츠 ({car.installedParts.Count}/{maxSlots})</color>", 15, Color.white)
                .rectTransform.sizeDelta = new Vector2(290, 18);

            // 5. Parts Slots
            for (int s = 0; s < maxSlots; s++)
            {
                int slotIdx = s;
                bool hasPart = slotIdx < car.installedParts.Count;
                string partId = hasPart ? car.installedParts[slotIdx] : null;
                TrainPartData partData = hasPart ? TrainPartDatabase.GetPart(partId) : null;

                GameObject slotObj = new GameObject($"PartSlot_{slotIdx}");
                slotObj.transform.SetParent(carObj.transform, false);
                RectTransform sRect = slotObj.AddComponent<RectTransform>();
                sRect.sizeDelta = new Vector2(290, 28);

                Image sImg = slotObj.AddComponent<Image>();
                sImg.color = hasPart ? new Color(0.2f, 0.28f, 0.38f, 1f) : new Color(0.1f, 0.13f, 0.18f, 0.8f);

                if (hasPart && partData != null)
                {
                    // Slot Content (Part Name + Remove Button)
                    HorizontalLayoutGroup hlg = slotObj.AddComponent<HorizontalLayoutGroup>();
                    hlg.padding = new RectOffset(6, 6, 3, 3);
                    hlg.spacing = 6;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = true;

                    GameObject nameObj = new GameObject("PartName");
                    nameObj.transform.SetParent(slotObj.transform, false);
                    LayoutElement nLe = nameObj.AddComponent<LayoutElement>();
                    nLe.preferredWidth = 220;
                    CreateTextUI(nameObj.transform, $"<color=cyan>{partData.partName}</color>", 13, Color.white, new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(1, 1));

                    GameObject unmountBtnObj = new GameObject("UnmountBtn");
                    unmountBtnObj.transform.SetParent(slotObj.transform, false);
                    LayoutElement uLe = unmountBtnObj.AddComponent<LayoutElement>();
                    uLe.preferredWidth = 48;
                    Image uImg = unmountBtnObj.AddComponent<Image>();
                    uImg.color = new Color(0.7f, 0.25f, 0.25f, 1f);
                    Button uBtn = unmountBtnObj.AddComponent<Button>();
                    CreateTextUI(unmountBtnObj.transform, "해제", 12, Color.white);

                    uBtn.onClick.AddListener(() =>
                    {
                        TrainManager.Instance.TryUninstallPart(car, partId);
                        NotificationManager.Instance?.ShowMessage($"[{partData.partName}] 파츠 해제 완료", Color.gray);
                        UpdateUI();
                    });
                }
                else
                {
                    // Empty Slot -> Mount Part Button
                    Button sBtn = slotObj.AddComponent<Button>();
                    sBtn.interactable = car.carType != TrainCarType.Optional;
                    CreateTextUI(slotObj.transform, "+ 파츠 장착", 13, car.carType != TrainCarType.Optional ? Color.gray : new Color(0.3f, 0.3f, 0.3f, 1f));

                    if (car.carType != TrainCarType.Optional)
                    {
                        sBtn.onClick.AddListener(() => OpenPartModal(car));
                    }
                }
            }
        }

        private void OpenBuildOptionalCarModal(int slotIndex)
        {
            currentBuildingSlotIndex = slotIndex;
            buildModalTitle.text = $"선택 칸 {slotIndex} 모듈 건설 (100G)";

            foreach (Transform child in buildModalContent) Destroy(child.gameObject);

            var options = new (TrainCarType type, string name, string desc)[]
            {
                (TrainCarType.Infirmary, "의무실", "전투 종료 후 모든 아군의 체력을 2 회복합니다. (강화 시 회복량 증가)"),
                (TrainCarType.CombatEnhancement, "전투 강화소", "전투 시 모든 아군의 공격력과 주문력이 강화됩니다. (강화 시 단계별 +5%)"),
                (TrainCarType.PrayerRoom, "기도실", "전투 중 주고 받는 치유량이 10% 증가합니다. (강화 시 치유량 증가)"),
                (TrainCarType.TraitTrainingCamp, "특성 훈련소", "원하는 시너지를 선택하여 시너지 카운트 +1을 획득합니다. (강화 시 선택 수 증가)")
            };

            int buildCost = TrainManager.Instance.GetDiscountedCost(TrainCar.OptionalCarBuildCost);
            bool canAfford = ResourceManager.Instance != null && ResourceManager.Instance.Gold >= buildCost;

            foreach (var opt in options)
            {
                GameObject itemObj = new GameObject($"BuildOption_{opt.type}");
                itemObj.transform.SetParent(buildModalContent, false);
                RectTransform iRect = itemObj.AddComponent<RectTransform>();
                iRect.sizeDelta = new Vector2(0, 75);
                LayoutElement iLe = itemObj.AddComponent<LayoutElement>();
                iLe.preferredHeight = 75;

                Image iBg = itemObj.AddComponent<Image>();
                iBg.color = new Color(0.12f, 0.16f, 0.24f, 1f);

                HorizontalLayoutGroup hlg = itemObj.AddComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(15, 15, 8, 8);
                hlg.spacing = 10;
                hlg.childControlWidth = false;
                hlg.childControlHeight = true;

                // Info Area
                GameObject infoObj = new GameObject("Info");
                infoObj.transform.SetParent(itemObj.transform, false);
                LayoutElement infoLe = infoObj.AddComponent<LayoutElement>();
                infoLe.preferredWidth = 380;

                CreateTextUI(infoObj.transform, $"<color=yellow><b>{opt.name}</b></color>\n<size=14>{opt.desc}</size>", 16, Color.white, new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(1, 1));

                // Build Button
                GameObject actBtnObj = new GameObject("BuildButton");
                actBtnObj.transform.SetParent(itemObj.transform, false);
                LayoutElement actLe = actBtnObj.AddComponent<LayoutElement>();
                actLe.preferredWidth = 100;

                Image actImg = actBtnObj.AddComponent<Image>();
                actImg.color = canAfford ? new Color(0.2f, 0.65f, 0.3f, 1f) : new Color(0.4f, 0.4f, 0.4f, 1f);
                Button actBtn = actBtnObj.AddComponent<Button>();
                actBtn.interactable = canAfford;
                CreateTextUI(actBtnObj.transform, $"건설 ({buildCost}G)", 15, Color.white);

                actBtn.onClick.AddListener(() =>
                {
                    if (TrainManager.Instance.TryBuildOptionalCar(currentBuildingSlotIndex, opt.type))
                    {
                        NotificationManager.Instance?.ShowMessage($"[{opt.name}] 건설 완료!", Color.green);
                        buildModalPanel.SetActive(false);
                        UpdateUI();
                    }
                    else
                    {
                        NotificationManager.Instance?.ShowMessage("골드가 부족합니다!", Color.red);
                    }
                });
            }

            buildModalPanel.SetActive(true);
        }

        private void OpenSynergySelectModal(TrainCar car)
        {
            if (car == null || car.carType != TrainCarType.TraitTrainingCamp) return;

            int maxAllowed = car.MaxSelectableSynergies;
            bool canDuplicate = car.HasPartEffect(TrainPartEffectType.FateStackingModule);

            synergyModalTitle.text = $"특성 훈련소 시너지 선택 (선택: {car.selectedSynergies.Count} / {maxAllowed})";

            foreach (Transform child in synergyModalContent) Destroy(child.gameObject);

            var allSynergies = Enum.GetValues(typeof(SynergyType)).Cast<SynergyType>().ToList();

            foreach (var syn in allSynergies)
            {
                var synType = syn;
                int currentSelectedCount = car.selectedSynergies.Count(s => s == synType);
                bool isSelected = currentSelectedCount > 0;

                GameObject synObj = new GameObject($"SynergyItem_{synType}");
                synObj.transform.SetParent(synergyModalContent, false);

                Image sBg = synObj.AddComponent<Image>();
                sBg.color = isSelected ? new Color(0.25f, 0.45f, 0.7f, 1f) : new Color(0.14f, 0.18f, 0.26f, 1f);

                Button sBtn = synObj.AddComponent<Button>();

                string countLabel = currentSelectedCount > 1 ? $" (x{currentSelectedCount})" : (isSelected ? " [선택됨]" : "");
                var sInfo = SynergyDatabase.GetInfo(synType);
                string synName = sInfo != null ? sInfo.displayName : synType.ToString();
                CreateTextUI(synObj.transform, $"{synName}{countLabel}", 15, isSelected ? Color.yellow : Color.white);

                sBtn.onClick.AddListener(() =>
                {
                    if (isSelected && !canDuplicate)
                    {
                        // Remove instance
                        car.selectedSynergies.Remove(synType);
                        NotificationManager.Instance?.ShowMessage($"[{synName}] 시너지 선택 해제", Color.gray);
                    }
                    else if (isSelected && canDuplicate && car.selectedSynergies.Count >= maxAllowed)
                    {
                        // Remove instance when limit reached
                        car.selectedSynergies.Remove(synType);
                        NotificationManager.Instance?.ShowMessage($"[{synName}] 시너지 1개 해제", Color.gray);
                    }
                    else
                    {
                        // Add instance if limit not reached
                        if (car.selectedSynergies.Count < maxAllowed)
                        {
                            car.selectedSynergies.Add(synType);
                            NotificationManager.Instance?.ShowMessage($"[{synName}] 시너지 +1 적용!", Color.green);
                        }
                        else
                        {
                            NotificationManager.Instance?.ShowMessage($"최대 {maxAllowed}개까지만 선택할 수 있습니다!", Color.red);
                        }
                    }

                    OpenSynergySelectModal(car);
                    UpdateSummaryText();
                });
            }

            synergyModalPanel.SetActive(true);
        }

        private void OpenPartModal(TrainCar car)
        {
            if (car == null) return;

            partModalTitle.text = $"[{car.carName}] 파츠 장착 및 구매";
            foreach (Transform child in partModalContent) Destroy(child.gameObject);

            var availableParts = TrainPartDatabase.GetPartsForCar(car.carType, car.installedModuleId);
            if (availableParts.Count == 0)
            {
                CreateTextUI(partModalContent, "장착 가능한 파츠가 없습니다.", 18, Color.gray);
            }

            foreach (var part in availableParts)
            {
                bool isInstalled = car.HasPart(part.partId);
                int cost = TrainManager.Instance.GetDiscountedCost(part.cost);
                bool canAfford = ResourceManager.Instance != null && ResourceManager.Instance.Gold >= cost;

                GameObject itemObj = new GameObject($"PartItem_{part.partId}");
                itemObj.transform.SetParent(partModalContent, false);
                RectTransform iRect = itemObj.AddComponent<RectTransform>();
                iRect.sizeDelta = new Vector2(0, 70);
                LayoutElement iLe = itemObj.AddComponent<LayoutElement>();
                iLe.preferredHeight = 70;

                Image iBg = itemObj.AddComponent<Image>();
                iBg.color = new Color(0.12f, 0.16f, 0.24f, 1f);

                HorizontalLayoutGroup hlg = itemObj.AddComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(15, 15, 8, 8);
                hlg.spacing = 10;
                hlg.childControlWidth = false;
                hlg.childControlHeight = true;

                // Info Area
                GameObject infoObj = new GameObject("Info");
                infoObj.transform.SetParent(itemObj.transform, false);
                LayoutElement infoLe = infoObj.AddComponent<LayoutElement>();
                infoLe.preferredWidth = 360;

                CreateTextUI(infoObj.transform, $"<color=yellow><b>{part.partName}</b></color> ({cost}G)\n<size=14>{part.description}</size>", 16, Color.white, new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(1, 1));

                // Action Button
                GameObject actBtnObj = new GameObject("ActionButton");
                actBtnObj.transform.SetParent(itemObj.transform, false);
                LayoutElement actLe = actBtnObj.AddComponent<LayoutElement>();
                actLe.preferredWidth = 110;

                Image actImg = actBtnObj.AddComponent<Image>();
                Button actBtn = actBtnObj.AddComponent<Button>();

                if (isInstalled)
                {
                    actImg.color = new Color(0.3f, 0.35f, 0.4f, 1f);
                    actBtn.interactable = false;
                    CreateTextUI(actBtnObj.transform, "장착 중", 15, Color.gray);
                }
                else
                {
                    actImg.color = canAfford ? new Color(0.2f, 0.65f, 0.3f, 1f) : new Color(0.4f, 0.4f, 0.4f, 1f);
                    actBtn.interactable = canAfford;
                    CreateTextUI(actBtnObj.transform, "구매 & 장착", 15, Color.white);

                    actBtn.onClick.AddListener(() =>
                    {
                        if (TrainManager.Instance.TryBuyAndInstallPart(car, part.partId))
                        {
                            NotificationManager.Instance?.ShowMessage($"[{part.partName}] 장착 성공!", Color.green);
                            partModalPanel.SetActive(false);
                            UpdateUI();
                        }
                        else
                        {
                            NotificationManager.Instance?.ShowMessage("골드가 부족하거나 슬롯이 가득 찼습니다!", Color.red);
                        }
                    });
                }
            }

            partModalPanel.SetActive(true);
        }

        private void UpdateCharactersUI()
        {
            foreach (Transform child in charactersContainer) Destroy(child.gameObject);

            if (ResourceManager.Instance == null || RunManager.Instance == null) return;

            var partyDataIDs = RunManager.Instance.State.partyDataIDs;
            var leaderID = RunManager.Instance.State.leaderCharacterID;

            CharacterData[] allCharacters = Resources.LoadAll<CharacterData>("Characters");
            Dictionary<string, CharacterData> characterById = new Dictionary<string, CharacterData>();
            foreach (var data in allCharacters)
            {
                if (data == null || data.isEnemy) continue;
                characterById[data.DataId] = data;
            }

            HashSet<string> displayedCharacterIds = new HashSet<string>();
            foreach (string partyId in partyDataIDs)
            {
                if (!characterById.TryGetValue(partyId, out CharacterData partyData)) continue;

                displayedCharacterIds.Add(partyId);
                bool isLeader = partyId == leaderID;
                CreateCharUI(partyData, isLeader, true);
            }

            foreach (var data in allCharacters)
            {
                if (data == null || data.isEnemy || displayedCharacterIds.Contains(data.DataId)) continue;

                string dataId = data.DataId;
                int cards = ResourceManager.Instance.GetCardCount(dataId);
                if (cards > 0)
                {
                    CreateCharUI(data, false, false);
                }
            }
        }

        private void CreateCharUI(CharacterData data, bool isLeader, bool inParty)
        {
            string charId = data.DataId;
            string charName = data.DisplayName;
            int cardCount = ResourceManager.Instance.GetCardCount(charId);
            int level = ResourceManager.Instance.GetCharacterLevelFromCards(cardCount);
            if (level < 0) level = 0;

            GameObject charObj = new GameObject($"Char_{charName}");
            charObj.transform.SetParent(charactersContainer, false);
            RectTransform rect = charObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180, 200);
            LayoutElement layout = charObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 180;
            layout.preferredHeight = 200;

            Image bg = charObj.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.18f, 0.24f, 1f);

            // Leader Highlight Border
            if (isLeader)
            {
                GameObject borderObj = new GameObject("LeaderBorder");
                borderObj.transform.SetParent(charObj.transform, false);
                RectTransform borderRect = borderObj.AddComponent<RectTransform>();
                borderRect.anchorMin = Vector2.zero;
                borderRect.anchorMax = Vector2.one;
                borderRect.offsetMin = new Vector2(-4, -4);
                borderRect.offsetMax = new Vector2(4, 4);
                Image borderImg = borderObj.AddComponent<Image>();
                borderImg.color = new Color(1f, 0.8f, 0f, 1f);
                borderObj.transform.SetAsFirstSibling();
            }

            Button btn = charObj.AddComponent<Button>();
            btn.onClick.AddListener(() => OnCharacterClicked(data));

            VerticalLayoutGroup vLayout = charObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(8, 8, 8, 8);
            vLayout.spacing = 4;
            vLayout.childControlHeight = false;

            // Portrait
            if (data.portraitSprite != null)
            {
                GameObject portObj = new GameObject("Portrait");
                portObj.transform.SetParent(charObj.transform, false);
                RectTransform pRect = portObj.AddComponent<RectTransform>();
                pRect.sizeDelta = new Vector2(160, 100);
                Image pImg = portObj.AddComponent<Image>();
                pImg.sprite = data.portraitSprite;
                pImg.preserveAspect = true;
            }

            string partyBadge = inParty ? "<color=#66FF99>[전투 참가]</color> " : "<color=gray>[대기]</color> ";
            CreateTextUI(charObj.transform, $"{partyBadge}{charName}\n<size=14>Lv.{level} ({cardCount}장)</size>", 15, Color.white)
                .rectTransform.sizeDelta = new Vector2(160, 40);

            CreateTextUI(charObj.transform, "<size=12><color=#AAAAAA>클릭하여 상세 정보</color></size>", 12, Color.gray)
                .rectTransform.sizeDelta = new Vector2(160, 20);
        }

        private void OnCharacterClicked(CharacterData data)
        {
            ShowDetailPopup(data);
        }

        private void ShowDetailPopup(CharacterData data)
        {
            if (data.standingSprite != null)
            {
                detailPortrait.sprite = data.standingSprite;
            }
            else if (data.portraitSprite != null)
            {
                detailPortrait.sprite = data.portraitSprite;
            }
            
            CharacterStatus tempStatus = new CharacterStatus(data);
            
            detailStatsText.text = $"<color=yellow>[ {data.DisplayName} ]</color>\n" +
                                   $"HP: {tempStatus.FinalMaxHp:F0}\n" +
                                   $"Mental: {tempStatus.FinalMaxMental:F0}\n" +
                                   $"Attack: {tempStatus.FinalAttack:F0}\n" +
                                   $"Spell: {tempStatus.FinalSpellPower:F0}\n" +
                                   $"Armor: {tempStatus.FinalArmor:F0}\n" +
                                   $"Magic Resist: {tempStatus.FinalMagicResist:F0}";

            string skillText = "<color=cyan>-- 보유 스킬 --</color>\n";
            if (data.passiveSkill != null && !string.IsNullOrEmpty(data.passiveSkill.skillName))
            {
                skillText += $"[패시브] {data.passiveSkill.skillName}\n";
            }
            foreach (var s in data.activeSkills)
            {
                if (s != null && !string.IsNullOrEmpty(s.skillName))
                {
                    skillText += $"[액티브] {s.skillName} (비용: {s.baseCost})\n";
                }
            }
            detailSkillsText.text = skillText;

            detailPopupPanel.SetActive(true);
        }
    }

    // Helper to auto update HP bar
    public class TrainHpUpdater : MonoBehaviour
    {
        private RectTransform fill;
        private TextMeshProUGUI text;

        public void Init(RectTransform bgRect, RectTransform fillRect, TextMeshProUGUI tmp)
        {
            fill = fillRect;
            text = tmp;
        }

        private void Update()
        {
            if (TrainManager.Instance != null)
            {
                float cur = TrainManager.Instance.currentTrainDurability;
                float max = TrainManager.Instance.maxTrainDurability;
                float ratio = max > 0 ? cur / max : 0;
                
                if (fill != null) fill.anchorMax = new Vector2(ratio, 1f);
                if (text != null) text.text = $"기차 내구도: {cur} / {max}";
            }
        }
    }
}
