using System.Collections.Generic;
using TheLastArk.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    public sealed class BattleDiceUI : MonoBehaviour
    {
        private BattleManager _battle;
        private RectTransform _root;
        private readonly List<Image> _faces = new List<Image>(ActionDicePool.MaxDicePerTurn);
        private readonly List<DiceDragSource> _sources = new List<DiceDragSource>(ActionDicePool.MaxDicePerTurn);
        private readonly List<TextMeshProUGUI> _labels = new List<TextMeshProUGUI>(ActionDicePool.MaxDicePerTurn);
        private readonly Dictionary<int, Sprite> _sprites = new Dictionary<int, Sprite>();

        public void Initialize(BattleManager battle, Transform canvas)
        {
            _battle = battle;
            if (_root != null) return;

            var panel = new GameObject("BattleDicePool", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            panel.transform.SetParent(canvas, false);
            _root = panel.GetComponent<RectTransform>();
            _root.anchorMin = new Vector2(0.5f, 0f);
            _root.anchorMax = new Vector2(0.5f, 0f);
            _root.pivot = new Vector2(0.5f, 0f);
            _root.anchoredPosition = new Vector2(0f, 440f);
            _root.sizeDelta = new Vector2(650f, 76f);
            panel.GetComponent<Image>().color = new Color(0.04f, 0.09f, 0.16f, 0.82f);
            var layout = panel.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            for (int i = 0; i < ActionDicePool.MaxDicePerTurn; i++)
            {
                var dieObject = new GameObject($"Die_{i + 1}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(LayoutElement), typeof(DiceDragSource));
                dieObject.transform.SetParent(panel.transform, false);
                var element = dieObject.GetComponent<LayoutElement>();
                element.preferredWidth = 58f;
                element.preferredHeight = 58f;
                Image face = dieObject.GetComponent<Image>();
                face.preserveAspect = true;
                _faces.Add(face);
                _sources.Add(dieObject.GetComponent<DiceDragSource>());

                var labelObject = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(dieObject.transform, false);
                var rect = labelObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                var label = labelObject.GetComponent<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 34f;
                label.raycastTarget = false;
                TMPFontManager.ApplyFont(label);
                _labels.Add(label);
            }
        }

        public void Refresh(IReadOnlyList<ActionDie> dice, IReadOnlyList<int> selected)
        {
            if (_root == null) return;
            _root.gameObject.SetActive(_battle != null && _battle.UsesDice);
            if (!_root.gameObject.activeSelf) return;

            for (int i = 0; i < _faces.Count; i++)
            {
                Image face = _faces[i];
                bool exists = dice != null && i < dice.Count;
                face.gameObject.SetActive(exists);
                if (!exists) continue;

                ActionDie die = dice[i];
                _sources[i].Configure(die.Id, _root.parent as RectTransform);
                _sources[i].enabled = die.IsAvailable && _battle.IsPlayerTurn;
                int spriteValue = die.Value <= 6 ? die.Value : 0;
                if (!_sprites.TryGetValue(spriteValue, out Sprite sprite))
                {
                    sprite = Resources.Load<Sprite>($"Dice/die_{spriteValue}");
                    _sprites[spriteValue] = sprite;
                }
                face.sprite = sprite;
                bool isSelected = selected != null && Contains(selected, die.Id);
                face.color = die.IsSpent ? new Color(0.38f, 0.4f, 0.42f)
                    : die.IsLocked ? new Color(0.5f, 0.18f, 0.18f)
                    : isSelected ? new Color(1f, 0.82f, 0.42f)
                    : Color.white;
                _labels[i].text = die.IsLocked ? "×" : die.Value > 6 || die.Value == 0 ? die.Value.ToString() : "";
                _labels[i].color = die.IsSpent ? Color.gray : new Color(0.1f, 0.19f, 0.23f);
            }
        }

        private static bool Contains(IReadOnlyList<int> values, int value)
        {
            for (int i = 0; i < values.Count; i++)
                if (values[i] == value) return true;
            return false;
        }

        private void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
        }
    }
}
