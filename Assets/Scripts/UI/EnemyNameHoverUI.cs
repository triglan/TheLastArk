using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBattleCharacter))]
    public sealed class EnemyNameHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Hover")]
        [SerializeField, Min(0f)] private float hoverDelay = 0.5f;

        [Header("Name Label")]
        [SerializeField, Range(0f, 1f)] private float normalizedHeight = 0.6667f;
        [SerializeField] private Vector2 labelSize = new Vector2(300f, 64f);
        [SerializeField, Min(1f)] private float fontSize = 40f;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);

        private EnemyBattleCharacter enemy;
        private GameObject labelRoot;
        private TextMeshProUGUI nameText;
        private bool isPointerOver;
        private bool isVisible;
        private float pointerEnteredAt;

        private void Awake()
        {
            enemy = GetComponent<EnemyBattleCharacter>();
            CreateLabel();
            SetVisible(false);
        }

        private void Update()
        {
            if (!isPointerOver || isVisible) return;
            if (Time.unscaledTime - pointerEnteredAt < hoverDelay) return;

            ShowName();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;
            pointerEnteredAt = Time.unscaledTime;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;
            SetVisible(false);
        }

        private void OnDisable()
        {
            isPointerOver = false;
            SetVisible(false);
        }

        private void ShowName()
        {
            if (nameText == null) CreateLabel();

            string displayName = ResolveDisplayName();
            if (string.IsNullOrWhiteSpace(displayName)) return;

            nameText.text = displayName;
            SetVisible(true);
        }

        private string ResolveDisplayName()
        {
            CharacterData data = enemy != null && enemy.HasRuntimeStatus
                ? enemy.CurrentStatus.origin
                : enemy != null ? enemy.enemyData : null;

            return data != null ? data.DisplayName : string.Empty;
        }

        private void CreateLabel()
        {
            if (labelRoot != null) return;

            labelRoot = new GameObject("Enemy Name Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform labelRect = labelRoot.GetComponent<RectTransform>();
            labelRect.SetParent(transform, false);
            labelRect.anchorMin = new Vector2(0.5f, normalizedHeight);
            labelRect.anchorMax = new Vector2(0.5f, normalizedHeight);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = labelSize;

            Image background = labelRoot.GetComponent<Image>();
            background.color = backgroundColor;
            background.raycastTarget = false;

            GameObject textObject = new GameObject("Name Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(labelRect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);

            nameText = textObject.GetComponent<TextMeshProUGUI>();
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontSize = fontSize;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = textColor;
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.raycastTarget = false;
            TMPFontManager.ApplyFont(nameText);
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;
            if (labelRoot != null) labelRoot.SetActive(visible);
        }
    }
}
