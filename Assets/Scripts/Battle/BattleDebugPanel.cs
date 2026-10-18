#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using TheLastArk.UI;

namespace TheLastArk.Battle
{
    /// <summary>Runtime-only tooling: reuses scene slots and never edits CharacterData assets.</summary>
    public sealed class BattleDebugPanel : MonoBehaviour
    {
        public const string CanvasObjectName = "BattleDebugCanvas";
        private const int PageSize = 5;
        private const float RefreshInterval = 0.25f;
        private static readonly Color Surface = new Color(0.055f, 0.075f, 0.10f, 0.98f);
        private static readonly Color ButtonColor = new Color(0.14f, 0.20f, 0.27f);
        private static readonly Color Accent = new Color(0.12f, 0.40f, 0.43f);
        private GameManager gameManager;
        private BattleManager manager;
        private EnemyBattleCharacter[] slots;
        private CharacterData[] catalog;
        private readonly List<CharacterData> filtered = new List<CharacterData>();
        private readonly List<UnityEngine.UI.Button> commands = new List<UnityEngine.UI.Button>();
        private readonly List<UnityEngine.UI.Button> slotButtons = new List<UnityEngine.UI.Button>();
        private readonly List<UnityEngine.UI.Button> catalogButtons = new List<UnityEngine.UI.Button>();
        private readonly List<UnityEngine.UI.Button> allyButtons = new List<UnityEngine.UI.Button>();
        private readonly List<UnityEngine.UI.Button> panicTypeButtons = new List<UnityEngine.UI.Button>();
        private CharacterData selectedData;
        private int selectedSlot;
        private int selectedAlly;
        private PanicType selectedPanic = PanicType.Hallucination;
        private int page;
        private float nextRefresh;
        private bool openedBefore;
        private RectTransform canvasRoot;
        private RectTransform panel;
        private RectTransform enemyContent, panicContent;
        private Canvas debugCanvas;
        private UnityEngine.UI.Button panicToggle, applyPanicButton, randomPanicButton, clearPanicButton;
        private TextMeshProUGUI panicStatusText, panicDescriptionText;
        private TextMeshProUGUI launcherText, stateText, pageText, selectionText, patternText, detailText, messageText;
        private UnityEngine.UI.Button practiceButton;
        private TMP_InputField search;

        public IReadOnlyList<EnemyBattleCharacter> Slots => slots;
        public EnemyBattleCharacter SelectedEnemy => slots != null && selectedSlot < slots.Length ? slots[selectedSlot] : null;

        public void Initialize(GameManager owner)
        {
            if (manager != null || owner == null || owner.battleManager == null) return;
            gameManager = owner;
            manager = owner.battleManager;
            var found = new List<EnemyBattleCharacter>();
            var formation = owner.GetComponent<BattleEnemyFormationLayout>();
            // Include inactive slots excluded from the current encounter party.
            if (formation != null && formation.Slots != null)
                found.AddRange(formation.Slots);
            else
            {
                foreach (var enemy in FindObjectsOfType<EnemyBattleCharacter>(true))
                    if (enemy.gameObject.scene == manager.gameObject.scene) found.Add(enemy);
                found.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            }
            slots = found.ToArray();
            catalog = Array.FindAll(Resources.LoadAll<CharacterData>("Characters"), data => data != null && data.isEnemy);
            Array.Sort(catalog, (a, b) => string.Compare(a.characterName, b.characterName, StringComparison.CurrentCulture));
            BuildView();
            FilterCatalog("");
            RefreshView();
            panel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (manager == null) return;
            // Let the actual panic illustration and its input blocker cover the debug panel.
            debugCanvas.sortingOrder = manager.DebugPanicPresenting ? 450 : 2000;
            if (Input.GetKeyDown(KeyCode.F8)) TogglePanel();
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshInterval;
            launcherText.text = manager.DebugHoldsBattleOpen ? "전투 디버그 · 연습 중  [F8]" : "전투 디버그  [F8]";
            if (panel.gameObject.activeSelf) RefreshView();
        }

        public static bool ContainsGraphic(GameObject target)
        {
            var canvas = target.GetComponentInParent<Canvas>();
            return canvas != null && canvas.rootCanvas.name == CanvasObjectName;
        }

        private void OnDestroy()
        {
            if (canvasRoot != null) Destroy(canvasRoot.gameObject);
        }

        public void TogglePanel()
        {
            if (panel == null) return;
            bool open = !panel.gameObject.activeSelf;
            if (open && !openedBefore && manager.IsPlayerTurn)
            {
                manager.DebugSetPracticeMode(true);
                openedBefore = true;
            }
            panel.gameObject.SetActive(open);
            if (open) RefreshView();
        }

        public bool SelectSlot(int index)
        {
            if (slots == null || index < 0 || index >= slots.Length) return false;
            selectedSlot = index;
            RefreshView();
            return true;
        }

        public bool SpawnSelected(CharacterData data)
        {
            if (!CanEdit() || data == null || !data.isEnemy || SelectedEnemy == null) return false;
            var enemy = SelectedEnemy;
            enemy.AssignAndInitialize(data);
            var character = enemy.BattleCharacterComponent;
            // The same data may have state-dependent art (for example, closed eyes).
            character.view?.UpdateVisual(character.status);
            if (!manager.enemyParty.Contains(character)) manager.enemyParty.Add(character);
            if (!gameManager.allCharacters.Contains(character)) gameManager.allCharacters.Add(character);
            manager.DebugRefreshCombatants();
            gameManager.GetComponent<BattleEnemyFormationLayout>()?.Refresh();
            RefreshView();
            Tell($"슬롯 {selectedSlot + 1}: {data.characterName} 소환 · 체력/상태/패턴 초기화");
            return true;
        }

        public bool KillSelected()
        {
            if (!CanEdit() || SelectedEnemy == null || !SelectedEnemy.gameObject.activeSelf) return false;
            Kill(SelectedEnemy);
            manager.DebugRefreshCombatants();
            manager.DebugCheckBattleEnd();
            RefreshView();
            Tell("선택한 몬스터를 처치했습니다. 슬롯에 다시 소환할 수 있어요.");
            return true;
        }

        public void KillAll()
        {
            if (!CanEdit()) return;
            foreach (var enemy in slots)
                if (enemy != null && enemy.gameObject.activeSelf) Kill(enemy);
            manager.DebugRefreshCombatants();
            manager.DebugCheckBattleEnd();
            RefreshView();
            Tell("모든 몬스터를 처치했습니다.");
        }

        private static void Kill(EnemyBattleCharacter enemy)
        {
            var character = enemy.BattleCharacterComponent;
            if (character.status != null)
            {
                character.status.currentHp = 0f;
                if (character.status.origin != null) character.view?.UpdateVisual(character.status);
            }
            enemy.ClaimDefeatReward();
            enemy.GetComponent<EnemyAI>()?.ResetForBattle();
            enemy.GetComponent<EnemyIntentUI>()?.Hide();
            enemy.gameObject.SetActive(false);
        }

        public void RestoreParty()
        {
            if (!CanEdit()) return;
            foreach (var ally in manager.playerParty)
            {
                if (ally?.status == null) continue;
                ally.status.activeStatusEffects.Clear();
                ally.status.currentHp = ally.status.FinalMaxHp;
                ally.status.currentMental = ally.status.FinalMaxMental;
                ally.view?.UpdateVisual(ally.status);
            }
            manager.DebugRefreshCombatants();
            Tell("아군 체력·정신력 회복, 상태이상 해제 완료");
        }

        public bool SelectPattern(int index)
        {
            if (!CanEdit() || !IsAlive(SelectedEnemy)) return false;
            bool ready = SelectedEnemy.GetComponent<EnemyAI>().DebugSelectPattern(index);
            manager.DebugRefreshCombatants();
            RefreshView();
            Tell(ready ? "다음 패턴을 변경했습니다. 내부 누적 상태는 유지됩니다." : "기절 또는 비어 있는 패턴으로 실행할 수 없습니다.");
            return ready;
        }

        public bool ExecuteSelectedPattern()
        {
            bool started = CanEdit() && IsAlive(SelectedEnemy) && manager.DebugExecutePattern(SelectedEnemy.BattleCharacterComponent);
            Tell(started ? "선택한 몬스터의 패턴 실행 중…" : "실행 가능한 패턴과 생존 상태를 확인하세요.");
            RefreshView();
            return started;
        }

        private bool CanEdit() => manager != null && manager.IsPlayerTurn;
        private static bool IsAlive(EnemyBattleCharacter enemy) => enemy != null && enemy.gameObject.activeInHierarchy && enemy.HasRuntimeStatus && enemy.CurrentHp > 0f;
        private void Tell(string message) { if (messageText != null) messageText.text = message; }

        private BattleCharacter SelectedAlly => manager?.playerParty != null && selectedAlly < manager.playerParty.Count
            ? manager.playerParty[selectedAlly] : null;

        public void TogglePanicTests()
        {
            bool show = !panicContent.gameObject.activeSelf;
            panicContent.gameObject.SetActive(show);
            enemyContent.gameObject.SetActive(!show);
            panicToggle.GetComponentInChildren<TextMeshProUGUI>().text = show ? "< 적 관리" : "패닉 테스트 >";
            Tell(show ? "발동 시 정신력 0 · 기존 패닉을 교체하고 연출을 재생합니다." : "1회 실행은 턴/상태이상 시간을 진행하지 않습니다.");
            RefreshView();
        }

        private void TriggerPanic(bool random)
        {
            var ally = SelectedAlly;
            bool applied = random ? manager.DebugRollPanic(ally) : manager.DebugApplyPanic(ally, selectedPanic);
            Tell(applied ? $"{ally.characterName} · {PanicState.Name(ally.status.panic.Type)} 발동!" : "플레이어 턴에 살아 있는 아군을 선택하세요.");
            if (applied) debugCanvas.sortingOrder = 450;
            RefreshView();
        }

        private void ClearSelectedPanic()
        {
            bool cleared = manager.DebugClearPanic(SelectedAlly);
            Tell(cleared ? "선택 아군의 패닉 해제 · 정신력 최대 회복 (디버그 전용)" : "플레이어 턴에 살아 있는 아군을 선택하세요.");
            RefreshView();
        }

        private void RefreshPanicView(bool canEdit)
        {
            for (int i = 0; i < allyButtons.Count; i++)
            {
                var ally = i < manager.playerParty.Count ? manager.playerParty[i] : null;
                bool alive = ally?.status != null && ally.gameObject.activeInHierarchy && ally.status.currentHp > 0f;
                allyButtons[i].interactable = ally != null;
                allyButtons[i].targetGraphic.color = i == selectedAlly ? Accent : ButtonColor;
                allyButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = ally == null ? "비어 있음"
                    : $"{i + 1}  {ally.characterName}\n{(!alive ? "전투 불능" : ally.status.panic.IsActive ? PanicState.Name(ally.status.panic.Type) : "정상")}";
            }
            var actor = SelectedAlly;
            bool valid = actor?.status != null && actor.gameObject.activeInHierarchy && actor.status.currentHp > 0f;
            applyPanicButton.interactable = randomPanicButton.interactable = canEdit && valid;
            clearPanicButton.interactable = canEdit && valid && actor.status.panic.IsActive;
            for (int i = 0; i < panicTypeButtons.Count; i++)
                panicTypeButtons[i].targetGraphic.color = (int)selectedPanic == i + 1 ? Accent : ButtonColor;
            panicDescriptionText.text = PanicState.Name(selectedPanic) + "  /  " + PanicState.Description(selectedPanic);
            panicStatusText.text = actor?.status == null ? "테스트할 아군이 없습니다."
                : $"{actor.characterName}   HP {actor.status.currentHp:0.#}/{actor.status.FinalMaxHp:0.#}  ·  정신력 {actor.status.currentMental:0.#}/{actor.status.FinalMaxMental:0.#}\n"
                + (actor.status.panic.IsActive ? $"현재 패닉: {PanicState.Name(actor.status.panic.Type)} · 전투 종료까지 유지" : "현재 패닉: 없음")
                + (actor.status.panic.LockedSkill != null ? $"\n고정된 스킬: {actor.status.panic.LockedSkill.skillName}" : "");
        }

        private void ShiftPattern(int direction)
        {
            var enemy = SelectedEnemy;
            int count = enemy?.Patterns?.Count ?? 0;
            if (count == 0) return;
            SelectPattern((enemy.GetComponent<EnemyAI>().DebugPatternIndex + direction + count) % count);
        }

        private void FilterCatalog(string query)
        {
            filtered.Clear();
            foreach (var data in catalog)
                if (string.IsNullOrWhiteSpace(query) || (data.characterName ?? "").IndexOf(query.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0)
                    filtered.Add(data);
            page = 0;
            RefreshCatalog();
        }

        private void RefreshCatalog()
        {
            int pages = Mathf.Max(1, Mathf.CeilToInt(filtered.Count / (float)PageSize));
            page = Mathf.Clamp(page, 0, pages - 1);
            pageText.text = $"{page + 1} / {pages} 페이지 · {filtered.Count}종";
            for (int i = 0; i < PageSize; i++)
            {
                int index = page * PageSize + i;
                var button = catalogButtons[i];
                button.gameObject.SetActive(index < filtered.Count);
                if (index >= filtered.Count) continue;
                var data = filtered[index];
                button.GetComponentInChildren<TextMeshProUGUI>().text = $"{data.characterName}    HP {data.maxHp:0}  ·  패턴 {data.enemyPatterns?.Count ?? 0}";
                button.targetGraphic.color = selectedData == data ? Accent : ButtonColor;
            }
            selectionText.text = selectedData != null ? $"소환할 몬스터: {selectedData.characterName}" : "목록에서 몬스터를 선택하세요";
        }

        private void RefreshView()
        {
            if (stateText == null) return;
            bool canEdit = CanEdit();
            stateText.text = manager.DebugPanicPresenting ? "패닉 연출 중 · 잠시 기다려 주세요" : manager.DebugActionRunning ? "패턴 실행 중 · 잠시 기다려 주세요" : canEdit ? "플레이어 턴 · 조작 가능" : "턴 진행 / 전투 종료 · 조작 잠금";
            foreach (var button in commands) button.interactable = canEdit;
            RefreshPanicView(canEdit);
            practiceButton.GetComponentInChildren<TextMeshProUGUI>().text = manager.DebugHoldsBattleOpen ? "연습 모드: ON  ·  전멸해도 계속" : "연습 모드: OFF  ·  일반 승패 판정";
            for (int i = 0; i < slotButtons.Count; i++)
            {
                var enemy = slots[i];
                slotButtons[i].targetGraphic.color = i == selectedSlot ? Accent : ButtonColor;
                string name = IsAlive(enemy) ? enemy.enemyData.characterName
                    : enemy != null && enemy.gameObject.activeSelf && !enemy.HasRuntimeStatus ? "데이터 미설정" : "비어 있음";
                slotButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = $"슬롯 {i + 1}\n{name}";
            }
            var selected = SelectedEnemy;
            if (!IsAlive(selected))
            {
                patternText.text = "빈 슬롯 · 소환할 몬스터를 선택하세요";
                detailText.text = "소환은 선택 슬롯을 교체합니다.\n같은 몬스터를 다시 소환하면 체력, 상태이상, 내부 변수와 패턴이 초기화됩니다.";
                return;
            }
            var ai = selected.GetComponent<EnemyAI>();
            var intent = ai.PreparedIntent;
            int index = ai.DebugPatternIndex;
            int count = selected.Patterns?.Count ?? 0;
            patternText.text = $"패턴 {(count > 0 ? index + 1 : 0)}/{count} · {(count > 0 ? selected.Patterns[index]?.patternName : "없음")}";
            var text = new StringBuilder();
            text.AppendLine($"{selected.enemyData.characterName}  ·  HP {selected.CurrentHp:0.#}/{selected.MaxHp:0.#}");
            if (intent != null)
            {
                text.AppendLine(EnemyIntentUI.BuildDetail(intent));
            }
            else text.AppendLine("준비된 의도 없음 (기절 또는 실행할 행동 없음)");
            var variables = selected.enemyData.enemyRuntimeVariables;
            if (variables != null)
                foreach (var variable in variables)
                    if (variable != null && !string.IsNullOrEmpty(variable.id))
                        text.AppendLine($"{(string.IsNullOrEmpty(variable.displayName) ? variable.id : variable.displayName)}: {ai.RuntimeState.Get(variable.id)}");
            detailText.text = text.ToString();
        }

        private void BuildView()
        {
            // GameManager can live under a scaled scene Canvas; use an independent root.
            canvasRoot = Rect(CanvasObjectName, null, 0, 0, 0, 0);
            var canvas = canvasRoot.gameObject.AddComponent<Canvas>();
            debugCanvas = canvas;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;
            var scaler = canvasRoot.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasRoot.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            if (EventSystem.current == null)
                new GameObject("BattleDebugEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(canvasRoot, false);
            var launcher = Button("OpenDebug", canvasRoot, "전투 디버그  [F8]", 16, 138, 310, 44, TogglePanel, false);
            launcherText = launcher.GetComponentInChildren<TextMeshProUGUI>();
            panel = Rect("DebugPanel", canvasRoot, 16, 190, 610, 850);
            panel.gameObject.AddComponent<UnityEngine.UI.Image>().color = Surface;
            Label(panel, "BATTLE LAB  /  전투 디버그", 18, 12, 326, 32, 23);
            panicToggle = Button("PanicTests", panel, "패닉 테스트 >", 352, 12, 156, 32, TogglePanicTests, false);
            Button("CloseDebug", panel, "닫기", 520, 12, 72, 32, TogglePanel, false);
            stateText = Label(panel, "", 18, 48, 570, 24, 18);
            practiceButton = Button("PracticeMode", panel, "", 18, 78, 574, 36, () => { manager.DebugSetPracticeMode(!manager.DebugHoldsBattleOpen); RefreshView(); });
            enemyContent = Rect("EnemyControls", panel, 0, 0, 610, 780);
            Label(enemyContent, "01  소환 위치 선택", 18, 120, 570, 24, 19);
            float width = (574f - Mathf.Max(0, slots.Length - 1) * 6f) / Mathf.Max(1, slots.Length);
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                slotButtons.Add(Button("Slot" + i, enemyContent, "", 18 + i * (width + 6), 149, width, 55, () => SelectSlot(index), false));
            }
            Label(enemyContent, "02  몬스터 선택", 18, 211, 570, 24, 19);
            BuildSearch();
            for (int i = 0; i < PageSize; i++)
            {
                int row = i;
                catalogButtons.Add(Button("Monster" + i, enemyContent, "", 18, 279 + i * 32, 574, 29, () =>
                {
                    selectedData = filtered[page * PageSize + row];
                    RefreshCatalog();
                }, false));
            }
            Button("PreviousPage", enemyContent, "이전", 18, 442, 78, 30, () => { page--; RefreshCatalog(); }, false);
            pageText = Label(enemyContent, "", 104, 442, 400, 30, 18);
            Button("NextPage", enemyContent, "다음", 514, 442, 78, 30, () => { page++; RefreshCatalog(); }, false);
            selectionText = Label(enemyContent, "", 18, 477, 365, 30, 18);
            Button("Spawn", enemyContent, "선택 슬롯에 소환", 388, 477, 204, 34, () => { if (!SpawnSelected(selectedData)) Tell("목록에서 몬스터를 선택하고 플레이어 턴에 소환하세요."); });
            Label(enemyContent, "03  패턴 관찰", 18, 519, 570, 24, 19);
            Button("PreviousPattern", enemyContent, "<", 18, 548, 42, 32, () => ShiftPattern(-1));
            patternText = Label(enemyContent, "", 66, 548, 478, 32, 19);
            Button("NextPattern", enemyContent, ">", 550, 548, 42, 32, () => ShiftPattern(1));
            BuildDetails();
            Button("ExecutePattern", enemyContent, "선택 패턴 1회 실행", 18, 692, 283, 36, () => ExecuteSelectedPattern());
            Button("AdvanceTurn", enemyContent, "전체 적 턴 진행", 309, 692, 283, 36, () => manager.EndPlayerTurn());
            Button("KillSelected", enemyContent, "선택 몬스터 처치", 18, 736, 186, 36, () => KillSelected());
            Button("KillAll", enemyContent, "전체 처치", 212, 736, 186, 36, KillAll);
            Button("RestoreParty", enemyContent, "아군 완전 회복", 406, 736, 186, 36, RestoreParty);
            BuildPanicView();
            messageText = Label(panel, "1회 실행은 턴/상태이상 시간을 진행하지 않습니다.", 18, 781, 574, 28, 16);
            Label(panel, "패널을 닫아도 연습 모드 유지 · 패턴 피해/보상은 실제 적용", 18, 813, 574, 25, 16);
        }

        private void BuildPanicView()
        {
            panicContent = Rect("PanicControls", panel, 0, 120, 610, 652);
            Label(panicContent, "01  테스트할 아군", 18, 0, 574, 28, 20);
            for (int i = 0; i < manager.playerParty.Count; i++)
            {
                int index = i;
                allyButtons.Add(Button("PanicAlly" + i, panicContent, "", 18 + i % 2 * 291, 34 + i / 2 * 58, 283, 52,
                    () => { selectedAlly = index; RefreshView(); }, false));
            }
            var status = Rect("PanicStatus", panicContent, 18, 158, 574, 76);
            status.gameObject.AddComponent<UnityEngine.UI.Image>().color = ButtonColor;
            panicStatusText = Label(status, "", 10, 4, 554, 68, 18);
            Label(panicContent, "02  패닉 유형 선택", 18, 244, 574, 28, 20);
            for (int i = 0; i < 6; i++)
            {
                var type = (PanicType)(i + 1);
                panicTypeButtons.Add(Button("PanicType" + type, panicContent, PanicState.Name(type),
                    18 + i % 2 * 291, 278 + i / 2 * 48, 283, 42,
                    () => { selectedPanic = type; RefreshView(); }, false));
            }
            panicDescriptionText = Label(panicContent, "", 18, 426, 574, 66, 18);
            applyPanicButton = Button("ApplyPanic", panicContent, "선택 유형 발동", 18, 502, 283, 42, () => TriggerPanic(false));
            randomPanicButton = Button("RandomPanic", panicContent, "무작위 패닉 발동", 309, 502, 283, 42, () => TriggerPanic(true));
            clearPanicButton = Button("ClearPanic", panicContent, "패닉 해제 + 정신력 회복", 18, 554, 283, 38, ClearSelectedPanic);
            Button("PanicAdvanceTurn", panicContent, "다음 턴까지 진행", 309, 554, 283, 38, () => manager.EndPlayerTurn());
            Label(panicContent, "절망은 즉시 적용 · 나머지 효과는 다음 아군 턴 시작 시 적용\n다음 턴 진행 시 적도 행동합니다. 일반 회복으로는 패닉이 해제되지 않습니다.", 18, 602, 574, 48, 16);
            panicContent.gameObject.SetActive(false);
        }

        private void BuildSearch()
        {
            var root = Rect("MonsterSearch", enemyContent, 18, 240, 574, 32);
            root.gameObject.AddComponent<UnityEngine.UI.Image>().color = ButtonColor;
            search = root.gameObject.AddComponent<TMP_InputField>();
            var viewport = Rect("Viewport", root, 8, 0, 558, 32);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var inputText = Label(viewport, "", 0, 0, 558, 32, 18);
            var placeholder = Label(viewport, "몬스터 이름 검색…", 0, 0, 558, 32, 18);
            placeholder.color = new Color(0.65f, 0.72f, 0.78f);
            search.textViewport = viewport;
            search.textComponent = inputText;
            search.placeholder = placeholder;
            search.lineType = TMP_InputField.LineType.SingleLine;
            search.onValueChanged.AddListener(FilterCatalog);
        }

        private void BuildDetails()
        {
            var scrollRoot = Rect("PatternDetails", enemyContent, 18, 586, 574, 98);
            var scroll = scrollRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.horizontal = false;
            var viewport = Rect("Viewport", scrollRoot, 0, 0, 574, 98);
            viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = ButtonColor;
            viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
            detailText = Label(viewport, "", 8, 0, 558, 98, 18);
            detailText.alignment = TextAlignmentOptions.TopLeft;
            detailText.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = detailText.rectTransform;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        }

        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static TextMeshProUGUI Label(Transform parent, string text, float x, float y, float width, float height, float fontSize)
        {
            var label = Rect("Label", parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = new Color(0.91f, 0.95f, 0.98f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            TMPFontManager.ApplyFont(label);
            return label;
        }

        private UnityEngine.UI.Button Button(string name, Transform parent, string text, float x, float y, float width, float height, UnityEngine.Events.UnityAction action, bool command = true)
        {
            var rect = Rect(name, parent, x, y, width, height);
            var background = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = ButtonColor;
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = background;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            button.onClick.AddListener(action);
            var label = Label(rect, text, 6, 0, width - 12, height, 18);
            label.alignment = TextAlignmentOptions.Center;
            if (command) commands.Add(button);
            return button;
        }
    }
}
#endif
