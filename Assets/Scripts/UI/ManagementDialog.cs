using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace TheLastArk.UI
{
    /// <summary>Shared framing and product cards for management-related dialogs.</summary>
    public static class ManagementDialog
    {
        public static GameObject Create(Canvas canvas, string name, string title, string subtitle,
            UnityAction close, float sideInset, out RectTransform window)
        {
            var root = ManagementTheme.Panel(name, canvas.transform, new Color(0.02f, 0.04f, 0.06f, 0.78f));
            root.SetAsLastSibling();
            window = ManagementTheme.Panel(name + "Window", root, ManagementTheme.Background, true);
            ManagementTheme.Place(window, new Vector2(sideInset, 0.07f), new Vector2(1 - sideInset, 0.91f));
            var accent = ManagementTheme.Panel("HeaderRule", window, ManagementTheme.Gold);
            ManagementTheme.Top(accent, 0, 2);
            var heading = ManagementTheme.Label(window, title, 32, ManagementTheme.Text);
            ManagementTheme.Top(heading.rectTransform, 18, 42, 28, 140);
            var caption = ManagementTheme.Label(window, subtitle, 14, ManagementTheme.Gold);
            ManagementTheme.Top(caption.rectTransform, 62, 22, 30, 140);
            var closeButton = ManagementTheme.Button(window, "CloseButton", "닫기", close);
            ManagementTheme.Place((RectTransform)closeButton.transform, Vector2.one, Vector2.one,
                new Vector2(-118, -66), new Vector2(-28, -24));
            var footer = ManagementTheme.Label(window, "THE LAST ARK   /   방주 기록", 12, ManagementTheme.Muted);
            ManagementTheme.Place(footer.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(28, 7), new Vector2(-28, 27));
            return root.gameObject;
        }

        public static RectTransform Toolbar(Transform window)
        {
            var bar = ManagementTheme.Panel("Toolbar", window, ManagementTheme.Surface, true);
            ManagementTheme.Top(bar, 100, 56, 28, 28);
            return bar;
        }

        public static RectTransform Body(Transform window)
        {
            var body = ManagementTheme.Rect("Body", window);
            ManagementTheme.Place(body, Vector2.zero, Vector2.one, new Vector2(28, 38), new Vector2(-28, -174));
            return body;
        }

        public static RectTransform Product(Transform parent, string objectName, InventoryCategory category,
            string tag, string title, string description, Sprite icon, int price, bool sold,
            string unavailable, UnityAction buy)
        {
            var card = ManagementTheme.Panel(objectName, parent, ManagementTheme.Surface, true);
            var art = ManagementTheme.Panel("Icon", card, ManagementTheme.Background);
            ManagementTheme.Place(art, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -90), new Vector2(88, -20));
            if (icon != null)
            {
                var picture = ManagementTheme.Rect("Art", art).gameObject.AddComponent<UnityEngine.UI.Image>();
                picture.sprite = icon; picture.preserveAspect = true; picture.raycastTarget = false;
            }
            else ManagementTheme.InventorySymbol(art, category);
            var categoryLabel = ManagementTheme.Label(card, tag, 16, category == InventoryCategory.Consumable ? ManagementTheme.Cyan : ManagementTheme.Gold);
            ManagementTheme.Top(categoryLabel.rectTransform, 18, 24, 106, 18);
            var name = ManagementTheme.Label(card, title, 25);
            ManagementTheme.Top(name.rectTransform, 47, 50, 106, 18);
            var descriptionText = ManagementTheme.Label(card, description, 20, ManagementTheme.Muted, TextAlignmentOptions.TopLeft);
            ManagementTheme.Place(descriptionText.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 96), new Vector2(-18, -114));
            var tooltip = card.gameObject.AddComponent<BattleHoverTooltip>();
            tooltip.SetContent(title + "\n" + description);
            var rule = ManagementTheme.Panel("PriceRule", card, ManagementTheme.Line);
            ManagementTheme.Place(rule, Vector2.zero, new Vector2(1, 0), new Vector2(18, 86), new Vector2(-18, 87));
            var state = ManagementTheme.Label(card, sold ? "판매 완료" : unavailable, 16, ManagementTheme.Gold);
            ManagementTheme.Place(state.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(18, 61), new Vector2(-18, 83));
            var cost = ManagementTheme.Label(card, price + " G", 26, sold ? ManagementTheme.Muted : ManagementTheme.Gold);
            ManagementTheme.Place(cost.rectTransform, Vector2.zero, new Vector2(0.5f, 0), new Vector2(18, 14), new Vector2(-10, 56));
            var button = ManagementTheme.Button(card, "BuyButton", sold ? "품절" : "구매", buy, !sold);
            ManagementTheme.Place((RectTransform)button.transform, new Vector2(0.55f, 0), new Vector2(1, 0), new Vector2(0, 14), new Vector2(-18, 56));
            button.interactable = !sold && string.IsNullOrEmpty(unavailable);
            return card;
        }
    }
}
