using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Header("파티")]
    public List<BattleCharacter> playerParty;
    public List<BattleCharacter> enemyParty;

    [Header("참조")]
    public TargetArrow targetHandler;

    [Header("행동력")]
    public int maxAP = 10;
    public int currentAP;
    public TextMeshProUGUI apText;

    [Header("턴 UI")]
    [Tooltip("현재 턴 수를 표시합니다.")]
    public TextMeshProUGUI turnCountText;
    [Tooltip("현재 전투 페이즈를 표시합니다.")]
    public TextMeshProUGUI phaseText;
    [Tooltip("적 행동과 상태이상 처리 사이의 대기 시간입니다.")]
    public float enemyActionDelay = 0.8f;

    private BattlePhase _currentPhase = BattlePhase.None;
    private int _turnCount = 1;
    private readonly BattleSelectionState _selection = new BattleSelectionState();

    public bool IsPlayerTurn => _currentPhase == BattlePhase.PlayerTurn;

    private void Awake()
    {
        // 전투 결과 화면이 없으면 자동으로 붙입니다.
        if (FindObjectOfType<BattleResultUIManager>() == null)
            gameObject.AddComponent<BattleResultUIManager>();
    }

    public void InitializeAP()
    {
        // 전투 시작 시 행동력, 화면 표시, 적 행동 대상을 준비합니다.
        currentAP = maxAP;
        UpdateAPUI();
        UpdateTurnUI();

        foreach (var enemy in enemyParty)
        {
            if (enemy == null) continue;
            var ai = enemy.GetComponent<EnemyAI>();
            if (ai == null) continue;

            ai.targetParty = playerParty;
            ai.allyParty = enemyParty;
        }

        EnsureTopResourceUI();
        EnterPhase(BattlePhase.PlayerTurn);
    }

    private void EnsureTopResourceUI()
    {
        // 전투 장면에서도 상단 자원 화면을 볼 수 있게 캔버스를 보장합니다.
        GameObject topBarCanvasObj = GameObject.Find("TopBarCanvas");
        if (topBarCanvasObj == null)
        {
            topBarCanvasObj = new GameObject("TopBarCanvas");

            Canvas canvas = topBarCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = topBarCanvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            topBarCanvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        Transform existingResourcePanel = topBarCanvasObj.transform.Find("ResourcePanel");
        if (existingResourcePanel != null) return;

        var resourceUI = gameObject.GetComponent<TheLastArk.UI.ExplorationResourceUI>();
        if (resourceUI == null)
            resourceUI = gameObject.AddComponent<TheLastArk.UI.ExplorationResourceUI>();

        resourceUI.Initialize(topBarCanvasObj.transform);
    }

    private void EnterPhase(BattlePhase next)
    {
        // 전투 페이즈를 바꾸고 해당 페이즈의 진입 처리를 실행합니다.
        _currentPhase = next;
        UpdatePhaseUI();

        switch (next)
        {
            case BattlePhase.PlayerTurn:
                OnEnterPlayerTurn();
                break;
            case BattlePhase.EnemyTurn:
                StartCoroutine(RunEnemyTurn());
                break;
            case BattlePhase.StatusEffect:
                StartCoroutine(RunStatusEffectPhase());
                break;
            case BattlePhase.TurnEnd:
                OnEnterTurnEnd();
                break;
            case BattlePhase.BattleEnd:
                OnEnterBattleEnd();
                break;
        }
    }

    public void SelectSkill(SkillInfo skill, BattleCharacter actor)
    {
        // 플레이어가 스킬을 고르면 대상 선택 상태로 저장합니다.
        if (!IsPlayerTurn) return;

        _selection.Clear();
        _selection.Set(skill, actor);

        if (targetHandler.target != null)
            PerformSkill();
        else
            NotificationManager.Instance.ShowMessage("대상을 선택하세요.", Color.yellow);
    }

    public void SelectConsumable(int consumableIndex)
    {
        // 플레이어가 소모품을 고르면 대상 선택 또는 즉시 사용을 처리합니다.
        if (!IsPlayerTurn) return;

        var resMgr = TheLastArk.Managers.ResourceManager.Instance;
        if (resMgr == null || consumableIndex < 0 || consumableIndex >= resMgr.Consumables.Count) return;

        var consumable = resMgr.Consumables[consumableIndex];
        _selection.Clear();
        _selection.SetConsumable(consumable, consumableIndex);

        if (consumable.effectType == TheLastArk.Data.ConsumableEffectType.DamageAll)
        {
            PerformConsumable();
            return;
        }

        if (targetHandler.target != null)
            PerformConsumable();
        else
            NotificationManager.Instance.ShowMessage("대상을 선택하세요.", Color.yellow);
    }

    public void PerformSkill()
    {
        // 선택된 스킬을 행동력과 대상 조건을 확인한 뒤 실행합니다.
        if (!IsPlayerTurn) return;
        if (!_selection.IsReady || targetHandler.target == null) return;

        BattleCharacter primaryTarget = targetHandler.target.GetComponent<BattleCharacter>();
        if (primaryTarget == null) return;

        SkillInfo skill = _selection.Skill;
        BattleCharacter actor = _selection.Actor;
        int skillIdx = actor.status.SkillLevelIndex;
        SkillLevelData levelData = skill.levels[Mathf.Clamp(skillIdx, 0, skill.levels.Length - 1)];

        if (!TargetResolver.IsValid(primaryTarget, levelData.targetType, playerParty, enemyParty))
        {
            NotificationManager.Instance.ShowMessage($"{skill.skillName}에 맞는 대상이 아닙니다.", Color.red);
            _selection.Clear();
            return;
        }

        int cost = levelData.overrideCost != -1 ? levelData.overrideCost : skill.baseCost;
        if (currentAP < cost)
        {
            NotificationManager.Instance.ShowMessage("행동력이 부족합니다.", Color.red);
            return;
        }

        currentAP -= cost;
        UpdateAPUI();

        List<BattleCharacter> targets = TargetResolver.Resolve(primaryTarget, levelData.targetType, playerParty, enemyParty);
        EffectEngine.ProcessSkill(actor, targets, levelData);

        _selection.Clear();
        if (currentAP <= 0) EndPlayerTurn();
    }

    public void PerformConsumable()
    {
        // 선택된 소모품을 대상 조건에 맞게 적용합니다.
        if (!IsPlayerTurn) return;
        if (!_selection.IsReady || _selection.Consumable == null) return;

        var consumable = _selection.Consumable;
        var targets = new List<BattleCharacter>();

        BattleCharacter primaryTarget = null;
        if (targetHandler.target != null)
            primaryTarget = targetHandler.target.GetComponent<BattleCharacter>();

        switch (consumable.effectType)
        {
            case TheLastArk.Data.ConsumableEffectType.DamageSingle:
                if (primaryTarget == null || playerParty.Contains(primaryTarget))
                {
                    NotificationManager.Instance.ShowMessage("적을 선택하세요.", Color.red);
                    _selection.Clear();
                    return;
                }
                targets.Add(primaryTarget);
                break;

            case TheLastArk.Data.ConsumableEffectType.HealHP:
            case TheLastArk.Data.ConsumableEffectType.HealMental:
                if (primaryTarget == null || enemyParty.Contains(primaryTarget))
                {
                    NotificationManager.Instance.ShowMessage("아군을 선택하세요.", Color.red);
                    _selection.Clear();
                    return;
                }
                targets.Add(primaryTarget);
                break;

            case TheLastArk.Data.ConsumableEffectType.DamageAll:
                targets.AddRange(enemyParty);
                break;
        }

        foreach (BattleCharacter target in targets)
        {
            if (target == null || target.status.currentHp <= 0) continue;

            if (consumable.effectType == TheLastArk.Data.ConsumableEffectType.DamageSingle ||
                consumable.effectType == TheLastArk.Data.ConsumableEffectType.DamageAll)
            {
                target.ReceiveDamage(consumable.effectValue, null);
            }
            else if (consumable.effectType == TheLastArk.Data.ConsumableEffectType.HealHP)
            {
                target.ReceiveHeal(consumable.effectValue, null);
            }
            else if (consumable.effectType == TheLastArk.Data.ConsumableEffectType.HealMental)
            {
                Debug.Log($"[BattleManager] 정신력 회복: {consumable.effectValue}");
            }
        }

        NotificationManager.Instance.ShowMessage($"{consumable.consumableName} 사용.", Color.cyan);

        var resMgr = TheLastArk.Managers.ResourceManager.Instance;
        if (resMgr != null) resMgr.RemoveConsumable(_selection.ConsumableIndex);

        _selection.Clear();
    }

    public void EndPlayerTurn()
    {
        // 플레이어 턴을 끝내고 적 턴으로 넘깁니다.
        if (!IsPlayerTurn) return;
        EnterPhase(BattlePhase.EnemyTurn);
    }

    private void OnEnterPlayerTurn()
    {
        // 플레이어 턴이 시작되면 행동력과 선택 상태를 초기화합니다.
        currentAP = maxAP;
        UpdateAPUI();
        _selection.Clear();
        Debug.Log($"[Battle] {_turnCount}턴: 아군 턴");
    }

    private IEnumerator RunEnemyTurn()
    {
        Debug.Log($"[Battle] {_turnCount}턴: 적 턴");

        foreach (BattleCharacter enemy in enemyParty)
        {
            if (enemy == null || enemy.status.currentHp <= 0) continue;

            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai != null) ai.ExecuteTurn();

            yield return new WaitForSeconds(enemyActionDelay);

            if (IsPartyWiped(playerParty))
            {
                EnterPhase(BattlePhase.BattleEnd);
                yield break;
            }
        }

        EnterPhase(BattlePhase.StatusEffect);
    }

    private IEnumerator RunStatusEffectPhase()
    {
        Debug.Log($"[Battle] {_turnCount}턴: 상태이상 처리");

        var allCombatants = new List<BattleCharacter>(playerParty);
        allCombatants.AddRange(enemyParty);
        StatusEffectPhaseHandler.ProcessAll(allCombatants);

        yield return new WaitForSeconds(0.3f);
        EnterPhase(BattlePhase.TurnEnd);
    }

    private void OnEnterTurnEnd()
    {
        // 사망 여부를 정리하고 다음 턴으로 넘어갑니다.
        RemoveDeadCharacters();

        if (IsPartyWiped(playerParty)) { EnterPhase(BattlePhase.BattleEnd); return; }
        if (IsPartyWiped(enemyParty)) { EnterPhase(BattlePhase.BattleEnd); return; }

        _turnCount++;
        UpdateTurnUI();
        EnterPhase(BattlePhase.PlayerTurn);
    }

    private void OnEnterBattleEnd()
    {
        // 전투 결과 화면을 표시하고 이후 맵 장면으로 돌아갑니다.
        bool playerWin = IsPartyWiped(enemyParty);
        Debug.Log($"[Battle] 전투 종료: {(playerWin ? "승리" : "패배")}");

        if (BattleResultUIManager.Instance != null)
        {
            if (playerWin)
                BattleResultUIManager.Instance.ShowVictoryScreen(100, 50, LoadMapScene);
            else
                BattleResultUIManager.Instance.ShowDefeatScreen(LoadMapScene);
            return;
        }

        NotificationManager.Instance.ShowMessage(playerWin ? "승리!" : "패배...", playerWin ? Color.cyan : Color.red);
        Invoke(nameof(LoadMapScene), 2f);
    }

    private void LoadMapScene()
    {
        Debug.Log("[Battle] MapScene으로 이동합니다.");
        UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene");
    }

    public void UpdateAPUI()
    {
        if (apText != null) apText.text = $"행동력: {currentAP} / {maxAP}";
    }

    private void UpdatePhaseUI()
    {
        if (phaseText == null) return;
        phaseText.text = _currentPhase switch
        {
            BattlePhase.PlayerTurn => "아군 턴",
            BattlePhase.EnemyTurn => "적 턴",
            BattlePhase.StatusEffect => "상태이상",
            BattlePhase.TurnEnd => "턴 종료",
            BattlePhase.BattleEnd => "전투 종료",
            _ => "-------"
        };
    }

    private void UpdateTurnUI()
    {
        if (turnCountText != null) turnCountText.text = $"{_turnCount}턴";
    }

    private bool IsPartyWiped(List<BattleCharacter> party)
    {
        return party.TrueForAll(c => c == null || c.status.currentHp <= 0);
    }

    private void RemoveDeadCharacters()
    {
        foreach (BattleCharacter character in playerParty)
        {
            if (character != null && character.status.currentHp <= 0)
                Debug.Log($"[Battle] {character.characterName} is down.");
        }

        foreach (BattleCharacter character in enemyParty)
        {
            if (character != null && character.status.currentHp <= 0)
                Debug.Log($"[Battle] {character.characterName} is down.");
        }
    }
}
