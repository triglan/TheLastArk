using System.Collections.Generic;
using TheLastArk.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    public sealed class BattleDiceUI : MonoBehaviour
    {
        public const float DieSize = 64f;
        private BattleManager _battle;
        private RectTransform _root;
        private RectTransform _dragCanvas;
        private RectTransform _tray;
        private bool? _dockedDiceMode;
        private readonly List<Image> _faces = new List<Image>(ActionDicePool.MaxDicePerTurn);
        private readonly List<DiceDragSource> _sources = new List<DiceDragSource>(ActionDicePool.MaxDicePerTurn);
        private readonly List<TextMeshProUGUI> _labels = new List<TextMeshProUGUI>(ActionDicePool.MaxDicePerTurn);
        private readonly Dictionary<int, Sprite> _sprites = new Dictionary<int, Sprite>();

        public void Initialize(BattleManager battle, Transform canvas)
        {
            _battle = battle;
            if (_root != null) return;
            _dragCanvas = canvas as RectTransform;
            _tray = canvas.Find("BattleSkillTray") as RectTransform;

            var panel = new GameObject("BattleDicePool", typeof(RectTransform), typeof(Image), typeof(GridLayoutGroup));
            panel.transform.SetParent(_tray != null ? _tray : canvas, false);
            _root = panel.GetComponent<RectTransform>();
            _root.anchorMin = _root.anchorMax = _tray != null ? new Vector2(1f, 0.5f) : new Vector2(0.5f, 0f);
            _root.pivot = new Vector2(0.5f, 0.5f);
            _root.anchoredPosition = _tray != null ? new Vector2(-330f, -10f) : new Vector2(0f, 90f);
            _root.sizeDelta = new Vector2(376f, 152f);
            panel.GetComponent<Image>().color = new Color(0.04f, 0.09f, 0.16f, 0.82f);
            panel.GetComponent<Image>().raycastTarget = false;
            var layout = panel.GetComponent<GridLayoutGroup>();
            layout.cellSize = Vector2.one * DieSize;
            layout.spacing = Vector2.one * 10f;
            layout.padding = new RectOffset(8, 8, 7, 7);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;

            for (int i = 0; i < ActionDicePool.MaxDicePerTurn; i++)
            {
                var dieObject = new GameObject($"Die_{i + 1}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(LayoutElement), typeof(DiceDragSource));
                dieObject.transform.SetParent(panel.transform, false);
                var element = dieObject.GetComponent<LayoutElement>();
                element.preferredWidth = DieSize;
                element.preferredHeight = DieSize;
                Image face = dieObject.GetComponent<Image>();
                face.rectTransform.sizeDelta = Vector2.one * DieSize;
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
            var hintObject = new GameObject("DiceHint", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            hintObject.transform.SetParent(_root, false);
            hintObject.GetComponent<LayoutElement>().ignoreLayout = true;
            var hint = hintObject.GetComponent<TextMeshProUGUI>();
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            hint.rectTransform.sizeDelta = new Vector2(376f, 26f);
            hint.rectTransform.anchoredPosition = new Vector2(0f, 16f);
            hint.text = "대상 선택 후 주사위를 스킬로";
            hint.fontSize = 18f; hint.alignment = TextAlignmentOptions.Center;
            hint.color = new Color(0.7f, 0.82f, 0.86f); hint.raycastTarget = false;
            TMPFontManager.ApplyFont(hint);
            ApplyTrayLayout(_battle != null && _battle.UsesDice);
        }

        public void ApplyTrayLayout(bool diceMode)
        {
            if (_tray == null || _dockedDiceMode == diceMode) return;
            _dockedDiceMode = diceMode;
            BattleHudLayout.ApplySkillGrid(_tray, diceMode);
        }

        public void Refresh(IReadOnlyList<ActionDie> dice, IReadOnlyList<int> selected)
        {
            if (_root == null) return;
            ApplyTrayLayout(_battle != null && _battle.UsesDice);
            _root.gameObject.SetActive(_battle != null && _battle.UsesDice);
            if (!_root.gameObject.activeSelf) return;

            for (int i = 0; i < _faces.Count; i++)
            {
                Image face = _faces[i];
                bool exists = dice != null && i < dice.Count;
                face.gameObject.SetActive(exists);
                if (!exists) continue;

                ActionDie die = dice[i];
                _sources[i].Configure(die.Id, _dragCanvas);
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
