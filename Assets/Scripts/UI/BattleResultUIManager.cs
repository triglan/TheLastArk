using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TheLastArk.UI;

namespace UI
{
    public class BattleResultUIManager : MonoBehaviour
    {
        public static BattleResultUIManager Instance { get; private set; }

        private Canvas _targetCanvas;
        private Sprite _whiteSprite;
        private TMP_FontAsset _cachedFont;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        private Canvas GetCanvas()
        {
            if (_targetCanvas != null) return _targetCanvas;

            _targetCanvas = FindObjectOfType<Canvas>();
            if (_targetCanvas != null) return _targetCanvas;

            GameObject canvasObj = new GameObject("BattleResultCanvas");
            _targetCanvas = canvasObj.AddComponent<Canvas>();
            _targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            return _targetCanvas;
        }

        private TMP_FontAsset GetFontAsset()
        {
            if (_cachedFont == null)
                _cachedFont = TMPFontManager.MainKoreanFont;

            if (_cachedFont != null) return _cachedFont;

            var allTMPs = FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (var tmp in allTMPs)
            {
                if (tmp.font == null) continue;
                _cachedFont = tmp.font;
                break;
            }

            return _cachedFont;
        }

        public void ShowVictoryScreen(int gold, Action onExit)
        {
            Canvas canvas = GetCanvas();
            TMP_FontAsset font = GetFontAsset();

            GameObject overlayObj = CreateOverlay(canvas.transform, "VictoryOverlay", new Color(0.07f, 0.09f, 0.15f, 0.88f));
            GameObject cardObj = CreateCard(overlayObj.transform, "VictoryCard", new Vector2(480, 440), new Color(0.12f, 0.16f, 0.26f, 0.95f), new Color(0.85f, 0.65f, 0.2f, 0.6f));

            var layout = cardObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 40, 40);
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            CreateText(cardObj.transform, "TitleText", font, "BATTLE VICTORY", 32, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.95f, 0.8f, 0.3f, 1f), new Vector2(420, 60));
            CreateLine(cardObj.transform, new Vector2(300, 2), new Color(0.85f, 0.65f, 0.2f, 0.3f));
            CreateText(cardObj.transform, "RewardHeader", font, "전투 획득 보상", 18, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.8f, 0.9f, 1f, 0.8f), new Vector2(420, 30));
            CreateRewardItem(cardObj.transform, "GoldItem", font, "획득 골드", $"+ {gold} G", new Color(0.9f, 0.75f, 0.2f));
            CreateSpacer(cardObj.transform, new Vector2(420, 20));
            CreateExitButton(cardObj.transform, font, "보상 확인 후 이동", new Color(0.18f, 0.44f, 0.35f, 1f), new Color(0.22f, 0.55f, 0.44f, 1f), onExit);
        }

        public void ShowDefeatScreen(Action onExit)
        {
            Canvas canvas = GetCanvas();
            TMP_FontAsset font = GetFontAsset();

            GameObject overlayObj = CreateOverlay(canvas.transform, "DefeatOverlay", new Color(0.08f, 0.04f, 0.04f, 0.94f));
            GameObject cardObj = CreateCard(overlayObj.transform, "DefeatCard", new Vector2(480, 420), new Color(0.18f, 0.10f, 0.10f, 0.96f), new Color(0.7f, 0.15f, 0.15f, 0.5f));

            var layout = cardObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 45, 45);
            layout.spacing = 25;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            CreateText(cardObj.transform, "TitleText", font, "DEFEAT", 36, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.85f, 0.2f, 0.2f, 1f), new Vector2(420, 60));
            CreateLine(cardObj.transform, new Vector2(260, 2), new Color(0.85f, 0.2f, 0.2f, 0.3f));
            CreateText(cardObj.transform, "DescText", font, "모든 아군 캐릭터가 쓰러졌습니다.\n전력을 가다듬고 다시 도전해보세요.", 16, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.85f, 0.8f), new Vector2(400, 50));
            CreateSpacer(cardObj.transform, new Vector2(420, 10));
            CreateExitButton(cardObj.transform, font, "피해를 수습하고 이동", new Color(0.48f, 0.16f, 0.16f, 1f), new Color(0.6f, 0.2f, 0.2f, 1f), onExit);
        }

        private GameObject CreateOverlay(Transform parent, string name, Color color)
        {
            GameObject overlayObj = new GameObject(name);
            overlayObj.transform.SetParent(parent, false);

            RectTransform rect = overlayObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image image = overlayObj.AddComponent<Image>();
            image.sprite = _whiteSprite;
            image.color = color;
            return overlayObj;
        }

        private GameObject CreateCard(Transform parent, string name, Vector2 size, Color cardColor, Color outlineColor)
        {
            GameObject cardObj = new GameObject(name);
            cardObj.transform.SetParent(parent, false);

            RectTransform rect = cardObj.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            Image image = cardObj.AddComponent<Image>();
            image.sprite = _whiteSprite;
            image.color = cardColor;

            GameObject outlineObj = new GameObject("Outline");
            outlineObj.transform.SetParent(cardObj.transform, false);
            RectTransform outlineRect = outlineObj.AddComponent<RectTransform>();
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.sizeDelta = new Vector2(6, 6);
            outlineRect.anchoredPosition = Vector2.zero;

            Image outlineImage = outlineObj.AddComponent<Image>();
            outlineImage.sprite = _whiteSprite;
            outlineImage.color = outlineColor;
            outlineObj.transform.SetAsFirstSibling();

            return cardObj;
        }

        private void CreateText(Transform parent, string name, TMP_FontAsset font, string text, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color color, Vector2 size)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.alignment = alignment;
            tmp.color = color;
            TMPFontManager.ApplyFont(tmp);
        }

        private void CreateLine(Transform parent, Vector2 size, Color color)
        {
            GameObject lineObj = new GameObject("DecoLine");
            lineObj.transform.SetParent(parent, false);

            RectTransform rect = lineObj.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            Image image = lineObj.AddComponent<Image>();
            image.sprite = _whiteSprite;
            image.color = color;
        }

        private void CreateSpacer(Transform parent, Vector2 size)
        {
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(parent, false);
            RectTransform rect = spacer.AddComponent<RectTransform>();
            rect.sizeDelta = size;
        }

        private GameObject CreateRewardItem(Transform parent, string objName, TMP_FontAsset font, string label, string val, Color valColor)
        {
            GameObject container = new GameObject(objName);
            container.transform.SetParent(parent, false);

            RectTransform rectContainer = container.AddComponent<RectTransform>();
            rectContainer.sizeDelta = new Vector2(400, 45);

            Image background = container.AddComponent<Image>();
            background.sprite = _whiteSprite;
            background.color = new Color(1f, 1f, 1f, 0.05f);

            CreateAnchoredText(container.transform, "LabelText", font, label, 16, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, Color.white, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), new Vector2(15, 0));
            CreateAnchoredText(container.transform, "ValText", font, val, 18, FontStyles.Bold, TextAlignmentOptions.MidlineRight, valColor, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-15, 0));

            return container;
        }

        private void CreateExitButton(Transform parent, TMP_FontAsset font, string text, Color normalColor, Color hoverColor, Action onExit)
        {
            GameObject buttonObj = new GameObject("ExitButton");
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280, 50);

            Image image = buttonObj.AddComponent<Image>();
            image.sprite = _whiteSprite;
            image.color = normalColor;

            Button button = buttonObj.AddComponent<Button>();
            button.onClick.AddListener(() => onExit?.Invoke());

            UIHoverInteraction hover = buttonObj.AddComponent<UIHoverInteraction>();
            hover.targetImage = image;
            hover.normalColor = normalColor;
            hover.hoverColor = hoverColor;

            CreateAnchoredText(buttonObj.transform, "BtnText", font, text, 18, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero);
        }

        private void CreateAnchoredText(Transform parent, string name, TMP_FontAsset font, string text, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.alignment = alignment;
            tmp.color = color;
            TMPFontManager.ApplyFont(tmp);
        }
    }

    public class UIHoverInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Image targetImage;
        public Color normalColor;
        public Color hoverColor;

        private Vector3 _originalScale = Vector3.one;
        private Vector3 _targetScale = Vector3.one;
        private Color _targetColor;

        private void Start()
        {
            _originalScale = transform.localScale;
            _targetScale = _originalScale;
            _targetColor = normalColor;
            if (targetImage != null) targetImage.color = normalColor;
        }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.unscaledDeltaTime * 12f);
            if (targetImage != null)
                targetImage.color = Color.Lerp(targetImage.color, _targetColor, Time.unscaledDeltaTime * 12f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _targetScale = _originalScale * 1.05f;
            _targetColor = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _targetScale = _originalScale;
            _targetColor = normalColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _targetScale = _originalScale * 0.96f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _targetScale = _originalScale * 1.05f;
        }
    }
}
