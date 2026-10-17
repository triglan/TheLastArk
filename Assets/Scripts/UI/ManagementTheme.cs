using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace TheLastArk.UI
{
    public static class ManagementTheme
    {
        public static readonly Color Background = new Color32(17, 24, 31, 255);
        public static readonly Color Surface = new Color32(28, 39, 48, 255);
        public static readonly Color Raised = new Color32(39, 53, 62, 255);
        public static readonly Color Line = new Color32(75, 85, 86, 255);
        public static readonly Color Gold = new Color32(200, 174, 119, 255);
        public static readonly Color Text = new Color32(232, 229, 214, 255);
        public static readonly Color Muted = new Color32(161, 177, 181, 255);
        public static readonly Color Cyan = new Color32(146, 199, 205, 255);
        public static readonly Color Danger = new Color32(165, 102, 96, 255);

        public static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static RectTransform Panel(string name, Transform parent, Color color, bool border = false)
        {
            var rect = Rect(name, parent);
            rect.gameObject.AddComponent<UnityEngine.UI.Image>().color = color;
            if (border) Border(rect, Line);
            return rect;
        }

        public static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 insetMin = default, Vector2 insetMax = default)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = insetMin; rect.offsetMax = insetMax;
        }

        public static void Top(RectTransform rect, float top, float height, float left = 0, float right = 0)
            => Place(rect, new Vector2(0, 1), Vector2.one, new Vector2(left, -top - height), new Vector2(-right, -top));

        public static TextMeshProUGUI Label(Transform parent, string text, float size = 20, Color? color = null, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var label = Rect("Label", parent).gameObject.AddComponent<TextMeshProUGUI>();
            TMPFontManager.ApplyFont(label);
            label.text = text; label.fontSize = size; label.color = color ?? Text;
            label.alignment = alignment; label.raycastTarget = false;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.margin = new Vector4(2, 0, 2, 0);
            return label;
        }

        public static UnityEngine.UI.Button Button(Transform parent, string name, string text, UnityAction action, bool active = false)
        {
            var rect = Panel(name, parent, active ? Raised : Surface, true);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            StyleButton(button);
            if (action != null) button.onClick.AddListener(action);
            Label(rect, text, 20, active ? Gold : Text, TextAlignmentOptions.Center);
            return button;
        }

        public static void StyleButton(UnityEngine.UI.Button button)
        {
            button.targetGraphic = button.GetComponent<UnityEngine.UI.Image>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.22f, 1.22f, 1.18f);
            colors.pressedColor = new Color(0.75f, 0.86f, 0.9f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.60f, 0.62f, 0.7f);
            colors.fadeDuration = 0.12f;
            button.colors = colors;
        }

        public static void Border(Transform parent, Color color)
        {
            var root = Rect("Frame", parent);
            root.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
            for (int i = 0; i < 4; i++)
            {
                var line = Rect("Edge", root);
                bool horizontal = i < 2;
                var position = i == 0 ? new Vector2(0, 1) : i == 3 ? new Vector2(1, 0) : Vector2.zero;
                line.anchorMin = position;
                line.anchorMax = i == 1 ? new Vector2(1, 0) : i == 2 ? new Vector2(0, 1) : Vector2.one;
                line.sizeDelta = horizontal ? new Vector2(0, 1) : new Vector2(1, 0);
                line.anchoredPosition = Vector2.zero;
                var image = line.gameObject.AddComponent<UnityEngine.UI.Image>();
                image.color = color; image.raycastTarget = false;
            }
        }

        public static RectTransform Scroll(Transform parent, string name, out RectTransform content)
        {
            var root = Panel(name, parent, Background);
            var scroll = root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.scrollSensitivity = 38;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            var viewport = Rect("Viewport", root);
            viewport.offsetMax = new Vector2(-12, 0);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1);
            content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            var track = Panel("ScrollTrack", root, Surface);
            Place(track, new Vector2(1, 0), Vector2.one, new Vector2(-5, 0), Vector2.zero);
            var handle = Panel("Handle", track, Line);
            var bar = track.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();
            bar.handleRect = handle; bar.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>();
            bar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
            return root;
        }

        public static void RowLayout(Transform parent, float spacing = 8)
        {
            var layout = parent.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
        }

        public static void Height(Transform child, float height)
        {
            var element = child.GetComponent<UnityEngine.UI.LayoutElement>() ?? child.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            element.minHeight = element.preferredHeight = height;
        }

        // Small geometric emblems keep items without artwork visually consistent.
        public static void InventorySymbol(RectTransform parent, InventoryCategory category)
        {
            if (category == InventoryCategory.Equipment)
            {
                var sword = Rect("Sword", parent); sword.localRotation = Quaternion.Euler(0, 0, -35);
                SymbolPart(sword, "Blade", new Vector2(29, 26), new Vector2(41, 63), Text);
                SymbolPart(sword, "Guard", new Vector2(18, 22), new Vector2(52, 28), Gold);
                SymbolPart(sword, "Grip", new Vector2(31, 7), new Vector2(39, 22), Gold);
            }
            else if (category == InventoryCategory.Relic)
            {
                var diamond = SymbolPart(parent, "Relic", new Vector2(15, 15), new Vector2(55, 55), Gold);
                diamond.localRotation = Quaternion.Euler(0, 0, 45);
                var center = Rect("Inset", diamond); center.offsetMin = new Vector2(4, 4); center.offsetMax = new Vector2(-4, -4);
                center.gameObject.AddComponent<UnityEngine.UI.Image>().color = Surface;
                SymbolPart(parent, "Core", new Vector2(29, 29), new Vector2(41, 41), Cyan).localRotation = Quaternion.Euler(0, 0, 45);
            }
            else
            {
                SymbolPart(parent, "Stopper", new Vector2(26, 54), new Vector2(44, 61), Gold);
                SymbolPart(parent, "Neck", new Vector2(29, 40), new Vector2(41, 54), Muted);
                var bottle = SymbolPart(parent, "Bottle", new Vector2(16, 9), new Vector2(54, 43), Muted);
                var liquid = Rect("Liquid", bottle); liquid.offsetMin = new Vector2(4, 4); liquid.offsetMax = new Vector2(-4, -12);
                liquid.gameObject.AddComponent<UnityEngine.UI.Image>().color = Cyan;
            }
            foreach (var graphic in parent.GetComponentsInChildren<UnityEngine.UI.Graphic>()) graphic.raycastTarget = false;
        }

        private static RectTransform SymbolPart(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var part = Panel(name, parent, color);
            Place(part, Vector2.zero, Vector2.zero, min, max);
            return part;
        }
    }
}
