using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    /// <summary>Minimal modal list of choices on its own overlay canvas.</summary>
    public static class ChoicePopup
    {
        public static GameObject Show(string title, IReadOnlyList<string> options, Action<int> onChosen)
        {
            if (options == null || options.Count == 0 || !Application.isPlaying) return null;
            var root = new GameObject("ChoicePopup", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(root.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(520f, 90f + options.Count * 60f);
            panel.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.15f, 0.97f);
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 16, 16);
            layout.spacing = 10f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            Label(panel.transform, title, 24f, 40f);
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                var buttonObject = new GameObject("Option_" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                buttonObject.transform.SetParent(panel.transform, false);
                buttonObject.GetComponent<LayoutElement>().preferredHeight = 50f;
                buttonObject.GetComponent<Image>().color = new Color(0.18f, 0.32f, 0.42f);
                buttonObject.GetComponent<Button>().onClick.AddListener(() =>
                {
                    UnityEngine.Object.Destroy(root);
                    onChosen?.Invoke(index);
                });
                Label(buttonObject.transform, options[i], 20f, 50f);
            }
            return root;
        }

        private static void Label(Transform parent, string text, float size, float height)
        {
            var labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObject.transform.SetParent(parent, false);
            labelObject.GetComponent<LayoutElement>().preferredHeight = height;
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            TMPFontManager.ApplyFont(label);
        }
    }
}
