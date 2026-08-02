using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI;
using UnityEngine;
using TheLastArk.UI;
using TheLastArk.Managers;

public class BattleManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private BattleConfig battleConfig;

    [Header("Party")]
    public List<BattleCharacter> playerParty;
    public List<BattleCharacter> enemyParty;

    [Header("References")]
    public TargetArrow targetHandler;

    [Header("Action Points")]
    public int currentAP;
    public TextMeshProUGUI apText;

    [Header("Turn UI")]
    [Tooltip("현재 턴 수를 표시합니다.")]
    public TextMeshProUGUI turnCountText;
    [Tooltip("플레이어 턴을 종료하는 버튼입니다. 비워두면 TurnEndButton 이름으로 자동 검색합니다.")]
    public UnityEngine.UI.Button turnEndButton;

    private BattlePhase _currentPhase = BattlePhase.None;
    private int _turnCount = 1;
    private int _lastTurnEndClickFrame = -1;
    private bool _turnEndButtonListenerBound;
    private readonly BattleSelectionState _selection = new BattleSelectionState();

    public bool IsPlayerTurn => _currentPhase == BattlePhase.PlayerTurn;
    public BattleConfig Config => battleConfig;

    private int MaxAP
    {
        get
        {
            int baseAP = battleConfig != null ? battleConfig.MaxAP : BattleConfig.DefaultMaxAP;
            int relicAP = TheLastArk.Managers.ResourceManager.Instance != null ? (int)TheLastArk.Managers.ResourceManager.Instance.GetRelicBonus(TheLastArk.Data.RelicEffectType.BonusAP) : 0;
            int synergyAP = TheLastArk.Character.SynergyCalculator.GetTotalSynergyBonusAP();
            return baseAP + relicAP + synergyAP;
        }
    }

    private float EnemyActionDelay => battleConfig != null ? battleConfig.EnemyActionDelay : BattleConfig.DefaultEnemyActionDelay;
    private float StatusEffectDelay => battleConfig != null ? battleConfig.StatusEffectDelay : BattleConfig.DefaultStatusEffectDelay;
    private int VictoryGold => battleConfig != null ? battleConfig.VictoryGold : BattleConfig.DefaultVictoryGold;
    private string MapSceneName => battleConfig != null ? battleConfig.MapSceneName : BattleConfig.DefaultMapSceneName;

    private void Start()
    {
        Debug.Log("[Battle] BattleManager.Start() - BeginBattleSetup()은 GameManager에서 호출되어야 합니다.");
    }

    public void InitializeAP()
    {
        Debug.Log("[Battle] InitializeAP() 호출 - 전투 시작.");
        currentAP = MaxAP;
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
        Debug.Log($"[Battle] 페이즈 전환: {_currentPhase} -> {next}");
        _currentPhase = next;
        UpdateTurnEndButtonState();

        switch (next)
        {
            case BattlePhase.PlayerTurn:
                Debug.Log("[Battle] OnEnterPlayerTurn() 호출");
                OnEnterPlayerTurn();
                break;
            case BattlePhase.EnemyTurn:
                Debug.Log("[Battle] RunEnemyTurn() 코루틴 시작");
                StartCoroutine(RunEnemyTurn());
                break;
            case BattlePhase.StatusEffect:
                Debug.Log("[Battle] RunStatusEffectPhase() 코루틴 시작");
                StartCoroutine(RunStatusEffectPhase());
                break;
            case BattlePhase.TurnEnd:
                Debug.Log("[Battle] OnEnterTurnEnd() 호출");
                OnEnterTurnEnd();
                break;
            case BattlePhase.BattleEnd:
                Debug.Log("[Battle] OnEnterBattleEnd() 호출");
                OnEnterBattleEnd();
                break;
        }
    }

    public void SelectSkill(SkillInfo skill, BattleCharacter actor)
    {
        if (!IsPlayerTurn) return;

        _selection.Clear();
        _selection.Set(skill, actor);

        if (targetHandler != null && targetHandler.target != null)
            PerformSkill();
        else
            NotificationManager.Instance.ShowMessage("대상을 선택하세요.", Color.yellow);
    }

    public void SelectConsumable(int consumableIndex)
    {
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

        if (targetHandler != null && targetHandler.target != null)
            PerformConsumable();
        else
            NotificationManager.Instance.ShowMessage("대상을 선택하세요.", Color.yellow);
    }

    public void PerformSkill()
    {
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
        if (TryEndBattleIfPartyWiped()) return;

        if (currentAP <= 0) EndPlayerTurn();
    }

    public void PerformConsumable()
    {
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
                target.ReceiveDamage(consumable.effectValue, null, DamageType.True);
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
        if (_lastTurnEndClickFrame == Time.frameCount) return;
        _lastTurnEndClickFrame = Time.frameCount;

        Debug.Log($"[Battle] EndPlayerTurn() 호출. 현재 페이즈: {_currentPhase}");

        if (!IsPlayerTurn)
        {
            Debug.LogWarning($"[Battle] 턴 종료 입력 무시: 현재 페이즈가 플레이어 턴이 아닙니다. CurrentPhase: {_currentPhase}");
            return;
        }

        Debug.Log($"[Battle] 적 턴으로 전환합니다. EnemyParty 수: {enemyParty.Count}");
        EnterPhase(BattlePhase.EnemyTurn);
    }

    public void DebugWinBattle()
    {
        if (_currentPhase == BattlePhase.BattleEnd) return;

        Debug.LogWarning("[Battle] DebugWinBattle() 호출. 적 파티를 즉시 전멸 처리합니다.");
        if (enemyParty != null)
        {
            foreach (BattleCharacter enemy in enemyParty)
            {
                if (enemy == null || enemy.status == null) continue;
                enemy.status.currentHp = 0f;
                if (enemy.view != null) enemy.view.UpdateVisual(enemy.status);
            }
        }

        EnterPhase(BattlePhase.BattleEnd);
    }

    private void OnEnterPlayerTurn()
    {
        ClearEnemyTargetMarkers();
        Debug.Log($"[Battle] {_turnCount}턴: 아군 턴 시작");
        currentAP = MaxAP;
        UpdateAPUI();
        _selection.Clear();
    }

    private IEnumerator RunEnemyTurn()
    {
        ClearEnemyTargetMarkers();
        Debug.Log($"[Battle] {_turnCount}턴: 적 턴 시작. 생존 적 수 확인 중...");
        Debug.Log($"[Battle] enemyParty 총 수: {enemyParty.Count}");

        int aliveEnemies = 0;
        foreach (BattleCharacter enemy in enemyParty)
        {
            if (enemy != null && enemy.status.currentHp > 0)
                aliveEnemies++;
        }
        Debug.Log($"[Battle] 생존 적: {aliveEnemies}명");

        if (aliveEnemies == 0)
        {
            Debug.LogWarning("[Battle] 생존 적이 없습니다. 상태이상 처리로 넘어갑니다.");
            EnterPhase(BattlePhase.StatusEffect);
            yield break;
        }

        foreach (BattleCharacter enemy in enemyParty)
        {
            if (enemy == null)
            {
                Debug.LogWarning("[Battle] Enemy가 null입니다.");
                continue;
            }

            if (enemy.status.currentHp <= 0)
            {
                Debug.Log($"[Battle] {enemy.characterName}은 이미 사망했습니다.");
                continue;
            }

            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai == null)
            {
                Debug.LogError($"[Battle] {enemy.characterName}에 EnemyAI 컴포넌트가 없습니다.");
                continue;
            }

            Debug.Log($"[Battle] {enemy.characterName}이 행동합니다.");
            if (!ai.PrepareTurn()) continue;

            var preparedTargets = new List<BattleCharacter>(ai.PreparedTargets);
            SetEnemyTargetMarkers(preparedTargets, true);
            yield return new WaitForSeconds(EnemyActionDelay);
            ai.ExecuteTurn();
            SetEnemyTargetMarkers(preparedTargets, false);

            if (IsPartyWiped(playerParty))
            {
                Debug.Log("[Battle] 플레이어 파티 전멸. 전투 종료로 넘어갑니다.");
                EnterPhase(BattlePhase.BattleEnd);
                yield break;
            }
        }

        Debug.Log("[Battle] 적 턴 완료. 상태이상 처리로 넘어갑니다.");
        EnterPhase(BattlePhase.StatusEffect);
    }

    private IEnumerator RunStatusEffectPhase()
    {
        Debug.Log($"[Battle] {_turnCount}턴: 상태이상 처리 시작");

        var allCombatants = new List<BattleCharacter>(playerParty);
        allCombatants.AddRange(enemyParty);
        StatusEffectPhaseHandler.ProcessAll(allCombatants);

        yield return new WaitForSeconds(StatusEffectDelay);
        Debug.Log("[Battle] 상태이상 처리 완료. 턴 종료 단계로 넘어갑니다.");
        EnterPhase(BattlePhase.TurnEnd);
    }

    private void OnEnterTurnEnd()
    {
        Debug.Log($"[Battle] {_turnCount}턴: 턴 종료 단계");
        RemoveDeadCharacters();

        if (TryEndBattleIfPartyWiped()) return;

        _turnCount++;
        Debug.Log($"[Battle] 턴 카운트 증가: {_turnCount}턴으로 진입");
        UpdateTurnUI();
        EnterPhase(BattlePhase.PlayerTurn);
    }

    private void OnEnterBattleEnd()
    {
        ClearEnemyTargetMarkers();
        bool playerWin = IsPartyWiped(enemyParty);
        Debug.Log($"[Battle] 전투 종료: {(playerWin ? "승리" : "패배")}");

        if (BattleResultUIManager.Instance != null)
        {
            if (playerWin)
            {
                GiveVictoryRewards();
                BattleResultUIManager.Instance.ShowVictoryScreen(VictoryGold, LoadMapScene);
            }
            else
                BattleResultUIManager.Instance.ShowDefeatScreen(LoadMapScene);
            return;
        }

        if (playerWin) GiveVictoryRewards();
        NotificationManager.Instance.ShowMessage(playerWin ? "승리!" : "패배...", playerWin ? Color.cyan : Color.red);
        Invoke(nameof(LoadMapScene), 2f);
    }

    private void GiveVictoryRewards()
    {
        if (VictoryGold <= 0) return;

        ResourceManager.Instance.AddGold(VictoryGold);
        Debug.Log($"[Battle] 승리 보상 지급: 골드 +{VictoryGold}");
    }

    private void LoadMapScene()
    {
        Debug.Log($"[Battle] {MapSceneName}으로 이동합니다.");
        UnityEngine.SceneManagement.SceneManager.LoadScene(MapSceneName);
    }

    public void UpdateAPUI()
    {
        if (apText == null) return;

        TMPFontManager.ApplyFont(apText);
        apText.text = $"행동력 {currentAP} / {MaxAP}";
    }

    private void SetEnemyTargetMarkers(IEnumerable<BattleCharacter> targets, bool visible)
    {
        if (targets == null || playerParty == null) return;

        foreach (BattleCharacter target in targets)
        {
            if (target == null || target.view == null || !playerParty.Contains(target)) continue;
            target.view.SetEnemyTargeted(visible);
        }
    }

    private void ClearEnemyTargetMarkers()
    {
        SetEnemyTargetMarkers(playerParty, false);
    }

    private void CacheTurnEndButton()
    {
        Debug.Log("[Battle] CacheTurnEndButton() 호출");

        if (turnEndButton == null)
        {
            Debug.Log("[Battle] turnEndButton이 null입니다. TurnEndButton 오브젝트를 검색합니다.");
            GameObject buttonObject = GameObject.Find("TurnEndButton");
            if (buttonObject == null)
            {
                Debug.LogWarning("[Battle] TurnEndButton 오브젝트를 찾지 못했습니다.");
                return;
            }

            Debug.Log("[Battle] TurnEndButton GameObject 찾음");
            turnEndButton = buttonObject.GetComponent<UnityEngine.UI.Button>();
        }

        if (turnEndButton == null)
        {
            Debug.LogWarning("[Battle] TurnEndButton 오브젝트에 Button 컴포넌트가 없습니다.");
            return;
        }

        Debug.Log("[Battle] TurnEndButton Button 컴포넌트 획득");

        if (_turnEndButtonListenerBound)
        {
            Debug.Log("[Battle] 턴 종료 버튼 리스너가 이미 등록되어 있습니다.");
            return;
        }

        turnEndButton.onClick.RemoveListener(EndPlayerTurn);
        turnEndButton.onClick.AddListener(EndPlayerTurn);
        _turnEndButtonListenerBound = true;
        Debug.Log("[Battle] EndPlayerTurn 리스너 등록 완료");
    }

    public void UpdateTurnEndButtonState()
    {
        if (turnEndButton == null)
        {
            Debug.LogWarning("[Battle] turnEndButton이 null이어서 Interactable을 설정할 수 없습니다.");
            return;
        }

        bool shouldBeInteractable = IsPlayerTurn;
        if (turnEndButton.interactable != shouldBeInteractable)
        {
            Debug.Log($"[Battle] TurnEndButton Interactable 변경: {turnEndButton.interactable} -> {shouldBeInteractable}");
            turnEndButton.interactable = shouldBeInteractable;
        }
    }

    private void UpdateTurnUI()
    {
        if (turnCountText == null) return;

        TMPFontManager.ApplyFont(turnCountText);
        turnCountText.text = $"{_turnCount}턴";
    }

    private bool IsPartyWiped(List<BattleCharacter> party)
    {
        return party.TrueForAll(c => c == null || c.status.currentHp <= 0);
    }

    private bool TryEndBattleIfPartyWiped()
    {
        if (IsPartyWiped(playerParty))
        {
            Debug.LogWarning("[Battle] 플레이어 파티 전멸.");
            EnterPhase(BattlePhase.BattleEnd);
            return true;
        }

        if (IsPartyWiped(enemyParty))
        {
            Debug.LogWarning("[Battle] 적 파티 전멸.");
            EnterPhase(BattlePhase.BattleEnd);
            return true;
        }

        return false;
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
