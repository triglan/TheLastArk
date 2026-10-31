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
        private static readonly Color FocusColor = new Color(0.62f, 0.92f, 1f);
        private static readonly Color NorthColor = new Color(0.78f, 0.86f, 1f);
        private static readonly Color CloudColor = new Color(0.86f, 0.9f, 0.92f);
        private static readonly Color ControlColor = new Color(0.16f, 0.32f, 0.42f);

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
        private ActionDicePool _pool;
        private readonly HashSet<int> _focused = new HashSet<int>();
        private readonly List<int> _focusBuffer = new List<int>();
        private RectTransform _controls;
        private Button _minusButton;
        private Button _plusButton;
        private Button _rerollButton;
        private TextMeshProUGUI _rerollLabel;
        private TextMeshProUGUI _manipulateLabel;
        private Button _goldButton;
        private Button _carryButton;
        private bool _carryMode;
        private static readonly Color CarryColor = new Color(0.78f, 0.62f, 1f);

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
            CreateControls();
            DiceDragSource.Clicked += OnDieClicked;
            ApplyTrayLayout(_battle != null && _battle.UsesDice);
        }

        /// <summary>숫자 조작 / 기회 프로토콜 buttons; dice are focused by clicking them.</summary>
        private void CreateControls()
        {
            var row = new GameObject("DicePartControls", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(_root, false);
            row.GetComponent<LayoutElement>().ignoreLayout = true;
            _controls = row.GetComponent<RectTransform>();
            _controls.anchorMin = _controls.anchorMax = new Vector2(0.5f, 0f);
            _controls.sizeDelta = new Vector2(376f, 28f);
            _controls.anchoredPosition = new Vector2(0f, -42f);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            _manipulateLabel = CreateControlLabel(row.transform);
            _minusButton = CreateControlButton(row.transform, "-1", () => Manipulate(-1));
            _plusButton = CreateControlButton(row.transform, "+1", () => Manipulate(1));
            _rerollButton = CreateControlButton(row.transform, "리롤", Reroll, 120f);
            _rerollLabel = _rerollButton.GetComponentInChildren<TextMeshProUGUI>();
            _goldButton = CreateControlButton(row.transform, "골드 주사위", () => { _battle?.TryBuyGoldDie(); }, 110f);
            _carryButton = CreateControlButton(row.transform, "이월 지정", () => { _carryMode = !_carryMode; Refresh(_pool, null, null); }, 90f);
        }

        private TextMeshProUGUI CreateControlLabel(Transform parent)
        {
            var labelObject = new GameObject("ManipulateUses", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObject.transform.SetParent(parent, false);
            labelObject.GetComponent<LayoutElement>().preferredWidth = 74f;
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 15f;
            label.alignment = TextAlignmentOptions.MidlineRight;
            label.color = new Color(0.7f, 0.82f, 0.86f);
            label.raycastTarget = false;
            TMPFontManager.ApplyFont(label);
            return label;
        }

        private Button CreateControlButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick, float width = 44f)
        {
            var buttonObject = new GameObject("Button_" + text, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            var element = buttonObject.GetComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = 26f;
            buttonObject.GetComponent<Image>().color = ControlColor;
            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(onClick);
            var labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            Stretch(label.rectTransform, 0f);
            label.text = text;
            label.fontSize = 15f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            TMPFontManager.ApplyFont(label);
            return button;
        }

        private GameObject _splitPanel;

        /// <summary>
        /// [연운·안개] Lets the player choose the result values (same total, each ≥ 0) and which result keeps the
        /// original's external modifier. <paramref name="onConfirm"/> returns false when the split is rejected.
        /// </summary>
        public void ShowSplitDialog(ActionDie die, int count, System.Func<List<int>, int, bool> onConfirm)
        {
            if (_root == null || die == null || count < 2) return;
            if (_splitPanel != null) Destroy(_splitPanel);
            int total = die.Value;
            var values = new List<int>(count);
            for (int i = 0; i < count; i++) values.Add(total / count + (i < total % count ? 1 : 0));
            int carrier = 0;

            _splitPanel = new GameObject("DiceSplitPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            _splitPanel.transform.SetParent(_dragCanvas != null ? _dragCanvas : _root, false);
            _splitPanel.GetComponent<LayoutElement>().ignoreLayout = true;
            var rect = _splitPanel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360f, 70f + count * 36f + 44f);
            _splitPanel.GetComponent<Image>().color = new Color(0.04f, 0.09f, 0.16f, 0.96f);
            var layout = _splitPanel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 6f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI header = CreatePanelText(_splitPanel.transform, "", 17f);
            var rowLabels = new List<TextMeshProUGUI>();
            var carrierLabels = new List<TextMeshProUGUI>();
            Button confirm = null;
            void Redraw()
            {
                int sum = 0;
                foreach (int v in values) sum += v;
                header.text = $"연운·안개  ·  합계 {sum} / {total}" + (die.Modifier != 0 ? $"  ·  보정 {die.Modifier:+0;-0} 승계" : "");
                for (int i = 0; i < values.Count; i++)
                {
                    rowLabels[i].text = values[i].ToString();
                    if (i < carrierLabels.Count) carrierLabels[i].text = i == carrier ? "●보정" : "○보정";
                }
                if (confirm != null) confirm.interactable = sum == total;
            }
            for (int i = 0; i < count; i++)
            {
                int index = i;
                var row = new GameObject("SplitRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(_splitPanel.transform, false);
                row.GetComponent<LayoutElement>().preferredHeight = 30f;
                var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 8f; rowLayout.childAlignment = TextAnchor.MiddleCenter;
                rowLayout.childControlWidth = rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = false;
                CreateControlButton(row.transform, "-1", () => { if (values[index] > 0) { values[index]--; Redraw(); } });
                rowLabels.Add(CreatePanelText(row.transform, "", 20f, 40f));
                CreateControlButton(row.transform, "+1", () => { values[index]++; Redraw(); });
                if (die.Modifier != 0)
                {
                    var carrierButton = CreateControlButton(row.transform, "보정", () => { carrier = index; Redraw(); }, 70f);
                    carrierLabels.Add(carrierButton.GetComponentInChildren<TextMeshProUGUI>());
                }
            }
            var actions = new GameObject("SplitActions", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            actions.transform.SetParent(_splitPanel.transform, false);
            actions.GetComponent<LayoutElement>().preferredHeight = 30f;
            var actionLayout = actions.GetComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 10f; actionLayout.childAlignment = TextAnchor.MiddleCenter;
            actionLayout.childControlWidth = actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = false;
            confirm = CreateControlButton(actions.transform, "분할", () =>
            {
                if (onConfirm(new List<int>(values), carrier)) CloseSplitDialog();
            }, 110f);
            CreateControlButton(actions.transform, "취소", CloseSplitDialog, 110f);
            Redraw();
        }

        public void CloseSplitDialog()
        {
            if (_splitPanel != null) Destroy(_splitPanel);
            _splitPanel = null;
        }

        private static TextMeshProUGUI CreatePanelText(Transform parent, string text, float size, float width = -1f)
        {
            var labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObject.transform.SetParent(parent, false);
            var element = labelObject.GetComponent<LayoutElement>();
            element.preferredHeight = 26f;
            if (width > 0f) element.preferredWidth = width;
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text; label.fontSize = size; label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white; label.raycastTarget = false;
            TMPFontManager.ApplyFont(label);
            return label;
        }

        private bool CanFocusDice => _battle != null && _battle.IsPlayerTurn
            && (_battle.HasNumberManipulator || _battle.HasChanceProtocol && !_battle.RerollTargetsAll);

        private void OnDieClicked(int dieId)
        {
            if (_carryMode && _battle != null && _battle.CarryCapacity > 0)
            {
                _battle.ToggleCarryMark(dieId);
                return;
            }
            if (!CanFocusDice) return;
            if (!_focused.Remove(dieId)) _focused.Add(dieId);
            Refresh(_pool, null, null);
        }

        private void Manipulate(int delta)
        {
            if (_battle == null || _focused.Count != 1) return;
            foreach (int id in _focused) { _battle.TryManipulateDie(id, delta); break; }
        }

        private void Reroll()
        {
            if (_battle == null) return;
            _focusBuffer.Clear();
            _focusBuffer.AddRange(_focused);
            if (_battle.TryRerollDice(_focusBuffer)) _focused.Clear();
            Refresh(_pool, null, null);
        }

        private void RefreshControls()
        {
            bool manipulate = _battle.HasNumberManipulator;
            bool reroll = _battle.HasChanceProtocol;
            bool turn = _battle.IsPlayerTurn;
            bool gold = TheLastArk.Character.SynergyEffects.Tier(TheLastArk.Data.SynergyType.Mirage) > 0;
            bool carry = _battle.CarryCapacity > 0;
            if (!carry) _carryMode = false;
            _controls.gameObject.SetActive(manipulate || reroll || gold || carry);
            _goldButton.gameObject.SetActive(gold);
            if (gold)
            {
                _goldButton.interactable = _battle.CanBuyGoldDie;
                _goldButton.GetComponentInChildren<TextMeshProUGUI>().text = $"골드 주사위 {_battle.GoldDieCost}G";
            }
            _carryButton.gameObject.SetActive(carry);
            if (carry) _carryButton.GetComponentInChildren<TextMeshProUGUI>().text = _carryMode ? $"이월 {_battle.CarryCapacity}개까지" : "이월 지정";
            _manipulateLabel.gameObject.SetActive(manipulate);
            _minusButton.gameObject.SetActive(manipulate);
            _plusButton.gameObject.SetActive(manipulate);
            _rerollButton.gameObject.SetActive(reroll);
            if (manipulate)
            {
                _manipulateLabel.text = $"조작 {_battle.ManipulateUsesLeft}";
                bool canAdjust = turn && _battle.ManipulateUsesLeft > 0 && _focused.Count == 1;
                _minusButton.interactable = canAdjust;
                _plusButton.interactable = canAdjust;
            }
            if (reroll)
            {
                bool all = _battle.RerollTargetsAll;
                _rerollLabel.text = all ? $"전체 리롤 {_battle.RerollUsesLeft}" : $"선택 리롤 {_battle.RerollUsesLeft}";
                _rerollButton.interactable = turn && _battle.RerollUsesLeft > 0 && (all || _focused.Count > 0);
            }
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

        private IReadOnlyList<int> _lastSelected;
        private string _lastHint;

        /// <summary>Pass null selection/hint only from internal refreshes to keep the last values.</summary>
        public void Refresh(ActionDicePool pool, IReadOnlyList<int> selected, string previewHint)
        {
            if (selected != null) { _lastSelected = selected; _lastHint = previewHint; }
            selected = _lastSelected;
            previewHint = _lastHint;
            _pool = pool;
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
                ApplyFace(view, die, isSelected, _focused.Contains(die.Id));
                if (_battle.IsMarkedForCarry(die.Id)) view.Face.color = CarryColor;
                if (_shownIds.Add(die.Id))
                {
                    if (view.Animation != null) StopCoroutine(view.Animation);
                    view.Animation = StartCoroutine(PlayRoll(view, appearOrder++ * AppearStagger));
                }
            }
            _shownIds.IntersectWith(_currentIds);
            _focused.IntersectWith(_currentIds);
            if (!CanFocusDice) _focused.Clear();
            RefreshControls();

            _hint.text = string.IsNullOrEmpty(previewHint) ? _defaultHint : previewHint;
            _usageLog.text = BuildUsageLog(pool?.UsedThisTurn);
        }

        private void ApplyFace(DieView view, ActionDie die, bool isSelected, bool isFocused)
        {
            int value = die.Value;
            bool usePips = value >= 1 && value <= MaxPipValue;
            bool useSprite = usePips && value <= MaxSpriteValue;
            view.Face.sprite = LoadFace(useSprite ? value : 0);
            view.Face.color = die.IsLocked ? new Color(0.5f, 0.18f, 0.18f)
                : isSelected ? new Color(1f, 0.82f, 0.42f)
                : isFocused ? FocusColor
                : die.Kind == DieKind.North ? NorthColor
                : die.Kind == DieKind.Cloud ? CloudColor
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
            DiceDragSource.Clicked -= OnDieClicked;
            CloseSplitDialog();
            if (_root != null) Destroy(_root.gameObject);
        }
    }
}
