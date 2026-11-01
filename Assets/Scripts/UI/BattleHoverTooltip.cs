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
        private static Canvas tooltipCanvas;
        private const int TooltipSortingOrder = 500;

        private static Canvas GetTooltipCanvas(Canvas source)
        {
            if (tooltipCanvas != null) return tooltipCanvas;
            // Camera-space character UI is always behind overlay skill controls, regardless
            // of its sortingOrder. Host all hover details in the same overlay coordinate space.
            var root = new GameObject("Battle Tooltip Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, source.gameObject.scene);
            tooltipCanvas = root.GetComponent<Canvas>();
            tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tooltipCanvas.sortingOrder = TooltipSortingOrder;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.enabled = false;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;
            scaler.enabled = true;
            // Tooltips contain no interactive controls and must never intercept the pointer.
            return tooltipCanvas;
        }

        public void SetContent(string value)
        {
            content = value;
            if (hovered) Show();
        }

        public void OnPointerEnter(PointerEventData eventData) { if (eventData != null && eventData.dragging) return; hovered = true; Show(); }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; Hide(); }
        private void OnDisable() { hovered = false; Hide(); }
        private void OnDestroy()
        {
            if (panel == null) return;
            if (Application.isPlaying) Destroy(panel.gameObject);
            else DestroyImmediate(panel.gameObject);
        }
        private void Hide() { if (panel != null) panel.gameObject.SetActive(false); }

        private void Show()
        {
            if (string.IsNullOrWhiteSpace(content)) { Hide(); return; }
            Canvas sourceCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (sourceCanvas == null) return;
            if (panel == null)
            {
                canvasRect = GetTooltipCanvas(sourceCanvas).transform as RectTransform;
                var root = new GameObject("Battle Detail Tooltip", typeof(RectTransform), typeof(Image));
                root.transform.SetParent(canvasRect, false);
                panel = root.GetComponent<RectTransform>();
                panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
                panel.pivot = new Vector2(0f, 1f);
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
            Canvas.ForceUpdateCanvases();
            label.text = content;
            float width = Mathf.Min(260f, canvasRect.rect.width - 24f);
            float height = Mathf.Min(canvasRect.rect.height - 24f, label.GetPreferredValues(content, width - 24f, 0f).y + 24f);
            panel.sizeDelta = new Vector2(width, height);
            Camera sourceCamera = sourceCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : sourceCanvas.worldCamera;
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(sourceCamera, transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 position);
            float x = position.x + 30f;
            if (x + width > canvasRect.rect.xMax - 12f) x = position.x - width - 30f;
            panel.anchoredPosition = new Vector2(Mathf.Clamp(x, canvasRect.rect.xMin + 12f, canvasRect.rect.xMax - width - 12f),
                Mathf.Clamp(position.y + 20f, canvasRect.rect.yMin + height + 12f, canvasRect.rect.yMax - 12f));
            panel.SetAsLastSibling(); panel.gameObject.SetActive(true);
        }
    }
}
