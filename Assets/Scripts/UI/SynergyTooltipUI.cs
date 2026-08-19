using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using TheLastArk.Data;
using TheLastArk.Character;

namespace TheLastArk.UI
{
    public class SynergyTooltipUI : MonoBehaviour
    {
        private static SynergyTooltipUI instance;
        private static bool isQuitting = false;

        public static bool HasInstance => instance != null && !isQuitting;

        public static SynergyTooltipUI Instance
        {
            get
            {
                if (isQuitting) return null;
                if (instance == null)
                {
                    instance = FindObjectOfType<SynergyTooltipUI>();
                    if (instance == null && Application.isPlaying)
                    {
                        GameObject go = new GameObject("SynergyTooltipUI");
                        instance = go.AddComponent<SynergyTooltipUI>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        private GameObject tooltipPanel;
        private RectTransform tooltipRect;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI categoryText;
        private TextMeshProUGUI descriptionText;
        private TextMeshProUGUI tiersText;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                isQuitting = true;
            }
        }

        private void Update()
        {
            if (tooltipPanel != null && tooltipPanel.activeSelf)
            {
                FollowMousePosition();
            }
        }

        private void FollowMousePosition()
        {
            if (tooltipRect == null) return;

            Vector2 mousePos = Input.mousePosition;
            float width = tooltipRect.sizeDelta.x;
            float height = tooltipRect.sizeDelta.y;

            float pivotX = (mousePos.x + width + 30 > Screen.width) ? 1.05f : -0.05f;
            float pivotY = (mousePos.y + height + 30 > Screen.height) ? 1.05f : -0.05f;

            tooltipRect.pivot = new Vector2(pivotX, pivotY);
            tooltipRect.position = mousePos;
        }

        public void ShowTooltip(SynergyType type, int count)
        {
            EnsureUI();

            SynergyInfo info = SynergyDatabase.GetInfo(type);

            // 1. Title & Count
            titleText.text = $"{info.displayName}  <color=#FFD700>({count}명)</color>";

            // 2. Category Tag
            string catType = info.isFaction ? "세력 시너지" : "직업 시너지";
            categoryText.text = $"<color=#00FFCE>[{catType}]</color>";

            // 3. Summary Description
            descriptionText.text = info.summaryDescription;

            // 4. Tier Threshold List
            StringBuilder tiersSb = new StringBuilder();
            tiersSb.AppendLine("<color=#A0A0A0>── 시너지 단계별 효과 ──</color>");

            var activeTier = info.GetCurrentActiveTier(count);

            if (info.tiers != null)
            {
                foreach (var tier in info.tiers)
                {
                    bool isActive = (count >= tier.threshold);
                    bool isHighestActive = (activeTier != null && activeTier.threshold == tier.threshold);

                    if (isHighestActive)
                    {
                        tiersSb.AppendLine($"<color=#79FF5B>> {tier.description} <color=#FFD700>[적용 중]</color></color>");
                    }
                    else if (isActive)
                    {
                        tiersSb.AppendLine($"<color=#51CF66>[활성] {tier.description}</color>");
                    }
                    else
                    {
                        tiersSb.AppendLine($"<color=#777777>  {tier.description}</color>");
                    }
                }
            }

            tiersText.text = tiersSb.ToString().TrimEnd();

            tooltipPanel.SetActive(true);
            FollowMousePosition();
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        private void EnsureUI()
        {
            if (tooltipPanel != null) return;

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject cObj = new GameObject("SynergyTooltipCanvas");
                canvas = cObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 130;
                cObj.AddComponent<CanvasScaler>();
                cObj.AddComponent<GraphicRaycaster>();
            }

            tooltipPanel = new GameObject("SynergyTooltipPanel");
            tooltipPanel.transform.SetParent(canvas.transform, false);

            tooltipRect = tooltipPanel.AddComponent<RectTransform>();
            tooltipRect.sizeDelta = new Vector2(340, 240);

            Image bg = tooltipPanel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.12f, 0.94f); // 반투명 다크 블루 패널
            bg.raycastTarget = false;

            VerticalLayoutGroup layout = tooltipPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 8;
            layout.childControlHeight = false;

            ContentSizeFitter fitter = tooltipPanel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(tooltipPanel.transform, false);
            LayoutElement tLe = titleObj.AddComponent<LayoutElement>();
            tLe.preferredHeight = 30;
            titleText = CreateTextUI(titleObj.transform, "시너지 이름", 22, Color.white);

            // Category
            GameObject catObj = new GameObject("Category");
            catObj.transform.SetParent(tooltipPanel.transform, false);
            LayoutElement cLe = catObj.AddComponent<LayoutElement>();
            cLe.preferredHeight = 22;
            categoryText = CreateTextUI(catObj.transform, "시너지 분류", 16, Color.cyan);

            // Divider line
            GameObject divObj = new GameObject("Divider");
            divObj.transform.SetParent(tooltipPanel.transform, false);
            LayoutElement dLe = divObj.AddComponent<LayoutElement>();
            dLe.preferredHeight = 2;
            Image dImg = divObj.AddComponent<Image>();
            dImg.color = new Color(1, 1, 1, 0.2f);
            dImg.raycastTarget = false;

            // Summary Description
            GameObject descObj = new GameObject("Description");
            descObj.transform.SetParent(tooltipPanel.transform, false);
            LayoutElement descLe = descObj.AddComponent<LayoutElement>();
            descLe.preferredHeight = 50;
            descriptionText = CreateTextUI(descObj.transform, "시너지요약설명", 16, Color.white);

            // Tiers
            GameObject tiersObj = new GameObject("Tiers");
            tiersObj.transform.SetParent(tooltipPanel.transform, false);
            LayoutElement pLe = tiersObj.AddComponent<LayoutElement>();
            pLe.preferredHeight = 100;
            tiersText = CreateTextUI(tiersObj.transform, "단계별 효과", 15, Color.gray);

            tooltipPanel.SetActive(false);
            TMPFontManager.ApplyFontToAll(tooltipPanel.transform);
        }

        private TextMeshProUGUI CreateTextUI(Transform parent, string text, int fontSize, Color color)
        {
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(parent, false);
            RectTransform rect = txtObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.enableWordWrapping = true;
            tmp.font = TMPFontManager.MainKoreanFont;
            tmp.raycastTarget = false;

            return tmp;
        }
    }
}
