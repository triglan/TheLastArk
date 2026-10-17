using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    public sealed class BattleHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string content;
        private RectTransform panel;
        private TextMeshProUGUI label;
        private RectTransform canvasRect;
        private bool hovered;

        public void SetContent(string value)
        {
            content = value;
            if (hovered) Show();
        }

        public void OnPointerEnter(PointerEventData eventData) { if (eventData != null && eventData.dragging) return; hovered = true; Show(); }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; Hide(); }
        private void OnDisable() { hovered = false; Hide(); }
        private void OnDestroy() { if (panel != null) Destroy(panel.gameObject); }
        private void Hide() { if (panel != null) panel.gameObject.SetActive(false); }

        private void Show()
        {
            if (string.IsNullOrWhiteSpace(content)) { Hide(); return; }
            if (panel == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas == null) return;
                canvasRect = canvas.rootCanvas.transform as RectTransform;
                var root = new GameObject("Battle Detail Tooltip", typeof(RectTransform), typeof(Image), typeof(Canvas));
                root.transform.SetParent(canvasRect, false);
                panel = root.GetComponent<RectTransform>();
                panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
                panel.pivot = new Vector2(0f, 1f);
                var overlay = root.GetComponent<Canvas>(); overlay.overrideSorting = true; overlay.sortingOrder = 500;
                var image = root.GetComponent<Image>(); image.color = new Color(0.035f, 0.05f, 0.075f, 0.98f); image.raycastTarget = false;
                var text = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI));
                text.transform.SetParent(panel, false);
                label = text.GetComponent<TextMeshProUGUI>();
                label.fontSize = 16f; label.enableWordWrapping = true; label.raycastTarget = false;
                label.alignment = TextAlignmentOptions.TopLeft;
                TMPFontManager.ApplyFont(label);
                label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(12f, 10f); label.rectTransform.offsetMax = new Vector2(-12f, -10f);
            }
            label.text = content;
            float width = Mathf.Min(260f, canvasRect.rect.width - 24f);
            float height = Mathf.Min(canvasRect.rect.height - 24f, label.GetPreferredValues(content, width - 24f, 0f).y + 24f);
            panel.sizeDelta = new Vector2(width, height);
            Vector3 position = canvasRect.InverseTransformPoint(transform.position);
            float x = position.x + 30f;
            if (x + width > canvasRect.rect.xMax - 12f) x = position.x - width - 30f;
            panel.anchoredPosition = new Vector2(Mathf.Clamp(x, canvasRect.rect.xMin + 12f, canvasRect.rect.xMax - width - 12f),
                Mathf.Clamp(position.y + 20f, canvasRect.rect.yMin + height + 12f, canvasRect.rect.yMax - 12f));
            panel.SetAsLastSibling(); panel.gameObject.SetActive(true);
        }
    }
}
