using UnityEngine;

namespace TheLastArk.UI
{
    /// <summary>Battle-only spacing; the shared exploration HUD keeps its map layout.</summary>
    public static class BattleHudLayout
    {
        public const float TrayHeight = 216f;
        public const float TrayWidth = 1320f;
        public const float SkillSize = 84f;

        public static void Apply(Transform canvas, bool diceMode)
        {
            if (canvas == null) return;
            var hud = canvas.Find("ExplorationHUD");
            if (hud != null)
            {
                var frame = hud.Find("BattleTopFrame") as RectTransform;
                if (frame != null)
                {
                    frame.anchorMin = new Vector2(0f, 1f);
                    frame.anchorMax = Vector2.one;
                    frame.pivot = new Vector2(0.5f, 1f);
                    frame.anchoredPosition = Vector2.zero;
                    frame.sizeDelta = new Vector2(0f, 140f);
                    var art = frame.Find("TopFrameArt")?.GetComponent<UnityEngine.UI.Image>();
                    if (art != null)
                    {
                        var strip = frame.GetComponent<BattleTopBarGraphic>() ?? frame.gameObject.AddComponent<BattleTopBarGraphic>();
                        strip.SetSource(art.sprite);
                        art.enabled = false;
                    }
                }
                Set(hud, "ManagementIcons", new Vector2(-20f, -44f), scale: 0.72f);
                Set(hud, "Consumables", new Vector2(490f, -44f), scale: 0.72f);
                Set(hud, "GoldText", new Vector2(35f, -44f));
                Set(hud, "ResourcePanel", new Vector2(220f, -10f), height: 68f);
                Set(hud, "ResourcePanel/TrainHpTrack", new Vector2(0f, 8f), height: 8f);
            }

            var tray = canvas.Find("BattleSkillTray") as RectTransform;
            if (tray == null) return;
            tray.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, TrayWidth);
            tray.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, TrayHeight);
            ApplySkillGrid(tray, diceMode);
            // The battle tray displays the dice themselves; the old AP/count card is redundant.
            tray.Find("ActionPoints")?.gameObject.SetActive(false);
            Set(tray, "CommandDivider", new Vector2(-124f, 0f), height: 180f);
            Set(tray, "TurnEndButton", new Vector2(-62f, 100f), scale: 0.9f);
            var accent = tray.Find("TrayAccent") as RectTransform;
            if (accent != null) accent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, TrayWidth);
            var turn = tray.Find("TurnEndButton");
            if (turn != null && turn.Find("TurnEndLabel") == null)
            {
                var label = new GameObject("TurnEndLabel", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                label.transform.SetParent(turn, false);
                var text = label.GetComponent<TMPro.TextMeshProUGUI>();
                text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                text.rectTransform.sizeDelta = new Vector2(110f, 26f);
                text.rectTransform.anchoredPosition = new Vector2(0f, -14f);
                text.text = "턴 종료"; text.fontSize = 20f;
                text.alignment = TMPro.TextAlignmentOptions.Center;
                text.color = new Color(0.88f, 0.82f, 0.66f); text.raycastTarget = false;
                TMPFontManager.ApplyFont(text);
            }
        }

        public static void ApplySkillGrid(Transform tray, bool diceMode)
        {
            var grid = tray.Find("SkillGrid") as RectTransform;
            if (grid == null) return;
            grid.anchorMin = grid.anchorMax = Vector2.zero;
            grid.pivot = Vector2.zero;
            grid.anchoredPosition = new Vector2(18f, 6f);
            grid.sizeDelta = new Vector2(diceMode ? 756f : 1152f, 204f);
            var row = grid.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            if (row != null) row.spacing = 12f;
            foreach (var group in grid.GetComponentsInChildren<UnityEngine.UI.GridLayoutGroup>(true))
            {
                float width = diceMode ? 180f : 279f;
                var rect = group.transform as RectTransform;
                rect.sizeDelta = new Vector2(width, 204f);
                group.cellSize = Vector2.one * SkillSize;
                group.spacing = Vector2.one * 8f;
                group.padding.top = 28;
                group.padding.left = Mathf.RoundToInt((width - SkillSize * 2f - 8f) * 0.5f);
                group.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
                group.constraintCount = 2;
                var surface = group.GetComponent<UnityEngine.UI.Image>() ?? group.gameObject.AddComponent<UnityEngine.UI.Image>();
                surface.color = new Color(0.035f, 0.065f, 0.09f, 0.8f);
                surface.raycastTarget = false;
                var name = group.transform.Find("CharacterName") as RectTransform;
                if (name != null)
                {
                    name.anchoredPosition = new Vector2(0f, -12f);
                    name.sizeDelta = new Vector2(width - 8f, 24f);
                    var label = name.GetComponent<TMPro.TextMeshProUGUI>();
                    if (label != null) { label.fontSize = 18f; label.enableAutoSizing = false; label.overflowMode = TMPro.TextOverflowModes.Ellipsis; }
                }
                var accent = group.transform.Find("GroupAccent") as RectTransform;
                if (accent != null)
                {
                    accent.anchoredPosition = new Vector2(width * 0.5f, -25f);
                    accent.sizeDelta = new Vector2(width - 8f, 1f);
                }
                foreach (var slot in group.GetComponentsInChildren<global::UI.SkillSlotUI>(true)) StyleSkillSlot(slot);
            }
        }

        private static void StyleSkillSlot(global::UI.SkillSlotUI slot)
        {
            var background = slot.transform.Find("CostBackground") as RectTransform;
            if (background == null) return;
            background.anchorMin = Vector2.zero; background.anchorMax = new Vector2(1f, 0f);
            background.pivot = new Vector2(0.5f, 0f);
            background.sizeDelta = new Vector2(0f, 28f);
            background.anchoredPosition = Vector2.zero;
            var image = background.GetComponent<UnityEngine.UI.Image>();
            if (image != null) { image.sprite = null; image.color = new Color(0.025f, 0.055f, 0.08f, 0.94f); image.raycastTarget = false; }
            var text = background.Find("CostText") as RectTransform;
            if (text != null)
            {
                text.anchorMin = Vector2.zero; text.anchorMax = Vector2.one;
                text.offsetMin = new Vector2(2f, 1f); text.offsetMax = new Vector2(-2f, -1f);
                var label = text.GetComponent<TMPro.TextMeshProUGUI>();
                if (label != null) { label.fontSize = 14f; label.fontSizeMax = 14f; label.fontSizeMin = 12f; label.enableAutoSizing = true; label.raycastTarget = false; }
            }
        }

        private static void Set(Transform parent, string path, Vector2 position, float height = -1f, float scale = 1f)
        {
            var rect = parent.Find(path) as RectTransform;
            if (rect == null) return;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one * scale;
            if (height >= 0f) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
    }
}
