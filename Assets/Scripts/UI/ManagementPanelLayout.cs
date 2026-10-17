using UnityEngine;

namespace TheLastArk.UI
{
    public static class ManagementPanelLayout
    {
        public static void DockRight(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 16f);
            rect.offsetMax = new Vector2(-16f, -96f);
        }

        public static void MakeScrollable(RectTransform content)
        {
            var parent = content.parent;
            var root = new GameObject(content.name + "Scroll", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.ScrollRect));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());
            root.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.01f);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(UnityEngine.UI.RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            var viewRect = viewport.GetComponent<RectTransform>();
            Stretch(viewRect);
            content.SetParent(viewRect, false);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            var layout = content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
            }
            var scroll = root.GetComponent<UnityEngine.UI.ScrollRect>();
            scroll.viewport = viewRect; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
