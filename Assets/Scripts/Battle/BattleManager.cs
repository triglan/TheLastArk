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
    [Tooltip("플레이어 턴을 종료하는 버튼입니다. 비워두면 TurnEndButton 이름으로 자동 검색합니다.")]
    public UnityEngine.UI.Button turnEndButton;
    [Tooltip("적 행동과 상태이상 처리 사이의 대기 시간입니다.")]
    public float enemyActionDelay = 0.8f;

    private BattlePhase _currentPhase = BattlePhase.None;
    private int _turnCount = 1;
    private int _lastTurnEndClickFrame = -1;
    private bool _turnEndButtonListenerBound;
    private readonly BattleSelectionState _selection = new BattleSelectionState();

    public bool IsPlayerTurn => _currentPhase == BattlePhase.PlayerTurn;

    private void Start()
    {
        Debug.Log("[Battle] ⚠️ BattleManager.Start() - BeginBattleSetup()은 GameManager에서 호출되어야 합니다!");
    }

    public void InitializeAP()
    {
        // 전투 시작 시 행동력, 화면 표시, 적 행동 대상을 준비합니다.
        Debug.Log("[Battle] 🎮 InitializeAP() 호출 - 전투 시작!");
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
        Debug.Log($"[Battle] 📍 페이즈 전환: {_currentPhase} → {next}");
        _currentPhase = next;
        UpdatePhaseUI();
        UpdateTurnEndButtonState();

        switch (next)
        {
            case BattlePhase.PlayerTurn:
                Debug.Log($"[Battle] ⏸️  OnEnterPlayerTurn() 호출");
                OnEnterPlayerTurn();
                break;
            case BattlePhase.EnemyTurn:
                Debug.Log($"[Battle] 🎬 RunEnemyTurn() 코루틴 시작");
                StartCoroutine(RunEnemyTurn());
                break;
            case BattlePhase.StatusEffect:
                Debug.Log($"[Battle] 🎬 RunStatusEffectPhase() 코루틴 시작");
                StartCoroutine(RunStatusEffectPhase());
                break;
            case BattlePhase.TurnEnd:
                Debug.Log($"[Battle] ⏸️  OnEnterTurnEnd() 호출");
                OnEnterTurnEnd();
                break;
            case BattlePhase.BattleEnd:
                Debug.Log($"[Battle] ⏸️  OnEnterBattleEnd() 호출");
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
        if (_lastTurnEndClickFrame == Time.frameCount) return;
        _lastTurnEndClickFrame = Time.frameCount;

        Debug.Log($"[Battle] ▶ EndPlayerTurn() 호출. 현재 페이즈: {_currentPhase}");

        if (!IsPlayerTurn)
        {
            Debug.LogWarning($"[Battle] ❌ 턴 종료 입력 무시: 현재 페이즈가 플레이어 턴이 아닙니다. CurrentPhase: {_currentPhase}");
            return;
        }

        Debug.Log($"[Battle] ✓ 적 턴으로 전환합니다. EnemyParty 수: {enemyParty.Count}");
        EnterPhase(BattlePhase.EnemyTurn);
    }

    private void OnEnterPlayerTurn()
    {
        // 플레이어 턴이 시작되면 행동력과 선택 상태를 초기화합니다.
        Debug.Log($"[Battle] 🎮 {_turnCount}턴: 아군 턴 시작");
        currentAP = maxAP;
        UpdateAPUI();
        _selection.Clear();
    }

    private IEnumerator RunEnemyTurn()
    {
        Debug.Log($"[Battle] 👿 {_turnCount}턴: 적 턴 시작. 생존 적 수 확인 중...");
        Debug.Log($"[Battle] 📊 enemyParty 총 수: {enemyParty.Count}");

        int aliveEnemies = 0;
        foreach (BattleCharacter enemy in enemyParty)
        {
            if (enemy != null && enemy.status.currentHp > 0)
                aliveEnemies++;
        }
        Debug.Log($"[Battle] 👿 생존 적: {aliveEnemies}명");

        if (aliveEnemies == 0)
        {
            Debug.LogWarning($"[Battle] ⚠️ 생존 적이 없습니다! 상태이상 처리로 넘어갑니다.");
            EnterPhase(BattlePhase.StatusEffect);
            yield break;
        }

        foreach (BattleCharacter enemy in enemyParty)
        {
            if (enemy == null)
            {
                Debug.LogWarning($"[Battle] ⚠️ Enemy가 null입니다!");
                continue;
            }

            if (enemy.status.currentHp <= 0)
            {
                Debug.Log($"[Battle] ⚠️ {enemy.characterName}은 이미 사망했습니다.");
                continue;
            }

            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai == null)
            {
                Debug.LogError($"[Battle] ❌ {enemy.characterName}에 EnemyAI 컴포넌트가 없습니다!");
                continue;
            }

            Debug.Log($"[Battle] 🔄 {enemy.characterName}이 행동합니다...");
            ai.ExecuteTurn();

            yield return new WaitForSeconds(enemyActionDelay);

            if (IsPartyWiped(playerParty))
            {
                Debug.Log($"[Battle] ☠️ 플레이어 파티 전멸! 전투 종료로 넘어갑니다.");
                EnterPhase(BattlePhase.BattleEnd);
                yield break;
            }
        }

        Debug.Log($"[Battle] ✓ 적 턴 완료. 상태이상 처리로 넘어갑니다.");
        EnterPhase(BattlePhase.StatusEffect);
    }

    private IEnumerator RunStatusEffectPhase()
    {
        Debug.Log($"[Battle] 🔬 {_turnCount}턴: 상태이상 처리 시작");

        var allCombatants = new List<BattleCharacter>(playerParty);
        allCombatants.AddRange(enemyParty);
        StatusEffectPhaseHandler.ProcessAll(allCombatants);

        yield return new WaitForSeconds(0.3f);
        Debug.Log($"[Battle] ✓ 상태이상 처리 완료. 턴 종료 단계로 넘어갑니다.");
        EnterPhase(BattlePhase.TurnEnd);
    }

    private void OnEnterTurnEnd()
    {
        // 사망 여부를 정리하고 다음 턴으로 넘어갑니다.
        Debug.Log($"[Battle] 🏁 {_turnCount}턴: 턴 종료 단계");
        RemoveDeadCharacters();

        if (IsPartyWiped(playerParty)) 
        { 
            Debug.LogWarning($"[Battle] ☠️ 플레이어 파티 전멸!");
            EnterPhase(BattlePhase.BattleEnd); 
            return; 
        }
        if (IsPartyWiped(enemyParty)) 
        { 
            Debug.LogWarning($"[Battle] 🎉 적 파티 전멸!");
            EnterPhase(BattlePhase.BattleEnd); 
            return; 
        }

        _turnCount++;
        Debug.Log($"[Battle] ⏭️  턴 카운트 증가: {_turnCount}턴으로 진입");
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

    private void CacheTurnEndButton()
    {
        Debug.Log("[Battle] 🔍 CacheTurnEndButton() 호출");
        
        if (turnEndButton == null)
        {
            Debug.Log("[Battle] ⚠️  turnEndButton이 null, GameObject.Find로 검색...");
            GameObject buttonObject = GameObject.Find("TurnEndButton");
            if (buttonObject == null)
            {
                Debug.LogWarning("[Battle] ❌ TurnEndButton 오브젝트를 찾지 못했습니다!");
                return;
            }

            Debug.Log("[Battle] ✓ TurnEndButton GameObject 찾음");
            turnEndButton = buttonObject.GetComponent<UnityEngine.UI.Button>();
        }

        if (turnEndButton == null)
        {
            Debug.LogWarning("[Battle] ❌ TurnEndButton 오브젝트에 Button 컴포넌트가 없습니다!");
            return;
        }

        Debug.Log("[Battle] ✓ TurnEndButton Button 컴포넌트 획득");

        if (_turnEndButtonListenerBound)
        {
            Debug.Log("[Battle] ℹ️  리스너가 이미 등록되어 있습니다");
            return;
        }

        turnEndButton.onClick.RemoveListener(EndPlayerTurn);
        turnEndButton.onClick.AddListener(EndPlayerTurn);
        _turnEndButtonListenerBound = true;
        Debug.Log("[Battle] ✓ EndPlayerTurn 리스너 등록 완료");
    }

    public void UpdateTurnEndButtonState()
    {
        if (turnEndButton == null)
        {
            Debug.LogWarning("[Battle] ⚠️  turnEndButton이 null이어서 Interactable을 설정할 수 없습니다!");
            return;
        }

        bool shouldBeInteractable = IsPlayerTurn;
        if (turnEndButton.interactable != shouldBeInteractable)
        {
            Debug.Log($"[Battle] 🔘 TurnEndButton Interactable 변경: {turnEndButton.interactable} → {shouldBeInteractable}");
            turnEndButton.interactable = shouldBeInteractable;
        }
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
