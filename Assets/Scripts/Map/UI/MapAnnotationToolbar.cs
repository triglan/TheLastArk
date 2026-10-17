using UnityEngine;

namespace TheLastArk.Map.UI
{
    public sealed class MapAnnotationToolbar : MonoBehaviour
    {
        private MapAnnotationController controller;
        private readonly UnityEngine.UI.Image[] backgrounds = new UnityEngine.UI.Image[3];
        private readonly MapAnnotationToolIcon[] icons = new MapAnnotationToolIcon[3];

        public void Initialize(MapAnnotationController owner)
        {
            controller = owner;
            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-28, -120);
            rect.sizeDelta = new Vector2(166, 56);
            var panel = gameObject.AddComponent<UnityEngine.UI.Image>();
            panel.color = new Color32(49, 46, 42, 240);
            var border = gameObject.AddComponent<UnityEngine.UI.Outline>();
            border.effectColor = new Color32(181, 165, 129, 180);
            border.effectDistance = new Vector2(1, -1);

            string[] names = { "Pen", "Eraser", "ClearMine" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var obj = new GameObject(names[i], typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                var buttonRect = obj.GetComponent<RectTransform>();
                buttonRect.SetParent(transform, false);
                buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0, 0.5f);
                buttonRect.pivot = new Vector2(0, 0.5f);
                buttonRect.anchoredPosition = new Vector2(6 + i * 52, 0);
                buttonRect.sizeDelta = new Vector2(50, 44);
                backgrounds[i] = obj.GetComponent<UnityEngine.UI.Image>();
                var button = obj.GetComponent<UnityEngine.UI.Button>();
                button.targetGraphic = backgrounds[i];
                button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
                button.onClick.AddListener(() =>
                {
                    if (index == 2) controller.ClearOwnStrokes();
                    else controller.SelectTool((MapAnnotationController.Tool)index);
                    Refresh();
                });
                var iconObject = new GameObject("Icon", typeof(RectTransform));
                var iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.SetParent(buttonRect, false);
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(30, 30);
                icons[i] = iconObject.AddComponent<MapAnnotationToolIcon>();
                icons[i].Kind = i;
                icons[i].raycastTarget = false;
            }
            Refresh();
        }

        public void Refresh()
        {
            if (controller == null) return;
            for (int i = 0; i < backgrounds.Length; i++)
            {
                if (backgrounds[i] == null) continue;
                bool selected = i == (int)controller.SelectedTool;
                backgrounds[i].color = selected ? new Color32(205, 185, 140, 255) : new Color32(66, 61, 54, 255);
                icons[i].color = selected ? (i == 0 ? MapAnnotationController.GetPlayerColor(controller.LocalPlayerSlot) : new Color32(46, 44, 42, 255))
                    : new Color32(218, 209, 188, 255);
            }
        }
    }
}
