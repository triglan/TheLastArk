using System.Collections;
using System.Collections.Generic;
using System.Text;
using TheLastArk.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    public sealed class BattleDiceUI : MonoBehaviour
    {
        public const float DieSize = 64f;
        private const int MaxPipValue = 9;
        private const int MaxSpriteValue = 6;
        private const float PipSize = 11f;
        private const float AppearStagger = 0.05f;
        private const float AppearDuration = 0.12f;
        private const float EmphasisDuration = 0.22f;
        private const float EmphasisScale = 1.22f;
        private static readonly Color PipColor = new Color(0.09f, 0.19f, 0.25f);
        private static readonly Color AuraColor = new Color(1f, 0.86f, 0.45f, 0.75f);
        private static readonly Color PlusBadgeColor = new Color(0.25f, 0.62f, 0.33f);
        private static readonly Color MinusBadgeColor = new Color(0.72f, 0.26f, 0.24f);

        private BattleManager _battle;
        private RectTransform _root;
        private RectTransform _dragCanvas;
        private RectTransform _tray;
        private bool? _dockedDiceMode;
        private TextMeshProUGUI _hint;
        private TextMeshProUGUI _usageLog;
        private readonly List<DieView> _views = new List<DieView>(ActionDicePool.MaxStoredDice);
        private readonly Dictionary<int, Sprite> _sprites = new Dictionary<int, Sprite>();
        private readonly HashSet<int> _shownIds = new HashSet<int>();
        private readonly HashSet<int> _currentIds = new HashSet<int>();
        private readonly StringBuilder _logBuilder = new StringBuilder();
        private Sprite _pipSprite;
        private string _defaultHint;

        private sealed class DieView
        {
            public RectTransform Rect;
            public Image Face;
            public Image Aura;
            public DiceDragSource Source;
            public TextMeshProUGUI Label;
            public TextMeshProUGUI Badge;
            public Image BadgeBack;
            public readonly List<Image> Pips = new List<Image>(MaxPipValue);
            public Coroutine Animation;
        }

        public void Initialize(BattleManager battle, Transform canvas)
        {
            _battle = battle;
            if (_root != null) return;
            _dragCanvas = canvas as RectTransform;
            _tray = canvas.Find("BattleSkillTray") as RectTransform;
            _pipSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");

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

            for (int i = 0; i < ActionDicePool.MaxStoredDice; i++)
                _views.Add(CreateDieView(panel.transform, i));

            _hint = CreateLabel("DiceHint", new Vector2(0f, 16f), 18f, new Color(0.7f, 0.82f, 0.86f));
            _hint.rectTransform.anchorMin = _hint.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _defaultHint = "대상 선택 후 주사위를 스킬로";
            _hint.text = _defaultHint;
            _usageLog = CreateLabel("DiceUsageLog", new Vector2(0f, -14f), 15f, new Color(0.6f, 0.68f, 0.74f));
            _usageLog.rectTransform.anchorMin = _usageLog.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            ApplyTrayLayout(_battle != null && _battle.UsesDice);
        }

        private DieView CreateDieView(Transform parent, int index)
        {
            var view = new DieView();
            var dieObject = new GameObject($"Die_{index + 1}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(LayoutElement), typeof(DiceDragSource));
            dieObject.transform.SetParent(parent, false);
            var element = dieObject.GetComponent<LayoutElement>();
            element.preferredWidth = DieSize;
            element.preferredHeight = DieSize;
            view.Rect = dieObject.GetComponent<RectTransform>();
            view.Face = dieObject.GetComponent<Image>();
            view.Face.rectTransform.sizeDelta = Vector2.one * DieSize;
            view.Face.preserveAspect = true;
            view.Source = dieObject.GetComponent<DiceDragSource>();

            var auraObject = new GameObject("Aura", typeof(RectTransform), typeof(Image));
            auraObject.transform.SetParent(dieObject.transform, false);
            view.Aura = auraObject.GetComponent<Image>();
            view.Aura.sprite = _pipSprite;
            view.Aura.raycastTarget = false;
            Stretch(view.Aura.rectTransform, -10f);
            view.Aura.transform.SetAsFirstSibling();
            view.Aura.gameObject.SetActive(false);

            for (int p = 0; p < MaxPipValue; p++)
            {
                var pipObject = new GameObject($"Pip_{p}", typeof(RectTransform), typeof(Image));
                pipObject.transform.SetParent(dieObject.transform, false);
                Image pip = pipObject.GetComponent<Image>();
                pip.sprite = _pipSprite;
                pip.color = PipColor;
                pip.raycastTarget = false;
                pip.rectTransform.sizeDelta = Vector2.one * PipSize;
                pip.gameObject.SetActive(false);
                view.Pips.Add(pip);
            }

            var labelObject = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(dieObject.transform, false);
            view.Label = labelObject.GetComponent<TextMeshProUGUI>();
            Stretch(view.Label.rectTransform, 0f);
            view.Label.alignment = TextAlignmentOptions.Center;
            view.Label.fontSize = 34f;
            view.Label.raycastTarget = false;
            TMPFontManager.ApplyFont(view.Label);

            var badgeObject = new GameObject("ModifierBadge", typeof(RectTransform), typeof(Image));
            badgeObject.transform.SetParent(dieObject.transform, false);
            view.BadgeBack = badgeObject.GetComponent<Image>();
            view.BadgeBack.raycastTarget = false;
            var badgeRect = view.BadgeBack.rectTransform;
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.sizeDelta = new Vector2(26f, 18f);
            badgeRect.anchoredPosition = new Vector2(-6f, -4f);
            var badgeTextObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeTextObject.transform.SetParent(badgeObject.transform, false);
            view.Badge = badgeTextObject.GetComponent<TextMeshProUGUI>();
            Stretch(view.Badge.rectTransform, 0f);
            view.Badge.alignment = TextAlignmentOptions.Center;
            view.Badge.fontSize = 13f;
            view.Badge.color = Color.white;
            view.Badge.raycastTarget = false;
            TMPFontManager.ApplyFont(view.Badge);
            badgeObject.SetActive(false);
            return view;
        }

        private TextMeshProUGUI CreateLabel(string name, Vector2 position, float size, Color color)
        {
            var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObject.transform.SetParent(_root, false);
            labelObject.GetComponent<LayoutElement>().ignoreLayout = true;
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.rectTransform.sizeDelta = new Vector2(376f, 24f);
            label.rectTransform.anchoredPosition = position;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.raycastTarget = false;
            TMPFontManager.ApplyFont(label);
            return label;
        }

        public void ApplyTrayLayout(bool diceMode)
        {
            if (_tray == null || _dockedDiceMode == diceMode) return;
            _dockedDiceMode = diceMode;
            BattleHudLayout.ApplySkillGrid(_tray, diceMode);
        }

        public void Refresh(ActionDicePool pool, IReadOnlyList<int> selected, string previewHint = null)
        {
            if (_root == null) return;
            ApplyTrayLayout(_battle != null && _battle.UsesDice);
            _root.gameObject.SetActive(_battle != null && _battle.UsesDice);
            if (!_root.gameObject.activeSelf) return;

            IReadOnlyList<ActionDie> dice = pool?.Dice;
            _currentIds.Clear();
            int appearOrder = 0;
            for (int i = 0; i < _views.Count; i++)
            {
                DieView view = _views[i];
                bool exists = dice != null && i < dice.Count;
                view.Face.gameObject.SetActive(exists);
                if (!exists) continue;

                ActionDie die = dice[i];
                _currentIds.Add(die.Id);
                view.Source.Configure(die.Id, _dragCanvas);
                view.Source.enabled = die.IsAvailable && _battle.IsPlayerTurn;
                bool isSelected = selected != null && Contains(selected, die.Id);
                ApplyFace(view, die, isSelected);
                if (_shownIds.Add(die.Id))
                {
                    if (view.Animation != null) StopCoroutine(view.Animation);
                    view.Animation = StartCoroutine(PlayRoll(view, appearOrder++ * AppearStagger));
                }
            }
            _shownIds.IntersectWith(_currentIds);

            _hint.text = string.IsNullOrEmpty(previewHint) ? _defaultHint : previewHint;
            _usageLog.text = BuildUsageLog(pool?.UsedThisTurn);
        }

        private void ApplyFace(DieView view, ActionDie die, bool isSelected)
        {
            int value = die.Value;
            bool usePips = value >= 1 && value <= MaxPipValue;
            bool useSprite = usePips && value <= MaxSpriteValue;
            view.Face.sprite = LoadFace(useSprite ? value : 0);
            view.Face.color = die.IsLocked ? new Color(0.5f, 0.18f, 0.18f)
                : isSelected ? new Color(1f, 0.82f, 0.42f)
                : Color.white;

            // 1–6 use the face sprites; 7–9 draw pips on the blank face; 0 and 10+ show the number.
            int pipCount = usePips && !useSprite ? value : 0;
            for (int p = 0; p < view.Pips.Count; p++)
            {
                bool on = p < pipCount;
                view.Pips[p].gameObject.SetActive(on);
                if (on) view.Pips[p].rectTransform.anchoredPosition = PipPosition(pipCount, p);
            }
            view.Label.text = die.IsLocked ? "×" : usePips ? "" : value.ToString();
            view.Label.color = new Color(0.1f, 0.19f, 0.23f);

            int modifier = die.Modifier;
            view.BadgeBack.gameObject.SetActive(modifier != 0);
            if (modifier != 0)
            {
                view.Badge.text = modifier.ToString("+0;-0");
                view.BadgeBack.color = modifier > 0 ? PlusBadgeColor : MinusBadgeColor;
            }
        }

        private Sprite LoadFace(int value)
        {
            if (!_sprites.TryGetValue(value, out Sprite sprite))
            {
                sprite = Resources.Load<Sprite>($"Dice/die_{value}");
                _sprites[value] = sprite;
            }
            return sprite;
        }

        // 3x3 grid cells (0 = top-left, 4 = centre) for 7–9 pips.
        private static readonly int[][] PipCells =
        {
            new[] { 0, 2, 3, 5, 6, 8, 4 },
            new[] { 0, 1, 2, 3, 5, 6, 7, 8 },
            new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 },
        };

        private static Vector2 PipPosition(int count, int index)
        {
            const float step = 15f;
            int cell = PipCells[count - (MaxSpriteValue + 1)][index];
            return new Vector2((cell % 3 - 1) * step, (1 - cell / 3) * step);
        }

        private string BuildUsageLog(IReadOnlyList<ActionDie> used)
        {
            if (used == null || used.Count == 0) return "";
            _logBuilder.Clear();
            _logBuilder.Append("사용: ");
            for (int i = 0; i < used.Count; i++)
            {
                if (i > 0) _logBuilder.Append(' ');
                _logBuilder.Append(used[i].Value);
            }
            return _logBuilder.ToString();
        }

        private IEnumerator PlayRoll(DieView view, float delay)
        {
            Transform target = view.Rect;
            target.localScale = Vector3.zero;
            view.Aura.gameObject.SetActive(false);
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            for (float t = 0f; t < AppearDuration; t += Time.unscaledDeltaTime)
            {
                target.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, t / AppearDuration);
                yield return null;
            }
            view.Aura.gameObject.SetActive(true);
            for (float t = 0f; t < EmphasisDuration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Sin(Mathf.PI * t / EmphasisDuration);
                target.localScale = Vector3.one * Mathf.Lerp(1f, EmphasisScale, k);
                view.Aura.color = new Color(AuraColor.r, AuraColor.g, AuraColor.b, AuraColor.a * k);
                yield return null;
            }
            target.localScale = Vector3.one;
            view.Aura.gameObject.SetActive(false);
            view.Animation = null;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static bool Contains(IReadOnlyList<int> values, int value)
        {
            for (int i = 0; i < values.Count; i++)
                if (values[i] == value) return true;
            return false;
        }

        private void OnDisable()
        {
            for (int i = 0; i < _views.Count; i++)
            {
                _views[i].Animation = null;
                if (_views[i].Rect != null) _views[i].Rect.localScale = Vector3.one;
                if (_views[i].Aura != null) _views[i].Aura.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
        }
    }
}
