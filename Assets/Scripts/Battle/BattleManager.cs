using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI;
using UnityEngine;
using TheLastArk.UI;
using TheLastArk.Managers;
using TheLastArk.Battle;
using TheLastArk.Data;

public class BattleManager : MonoBehaviour
{
    private const string SkillFirstTargetingKey = "Battle.SkillFirstTargeting";

    public static bool SkillFirstTargeting
    {
        get => PlayerPrefs.GetInt(SkillFirstTargetingKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(SkillFirstTargetingKey, value ? 1 : 0);
            PlayerPrefs.Save();
            BattleManager manager = FindObjectOfType<BattleManager>();
            if (manager != null) manager.CancelPendingSelection();
        }
    }

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
    private bool _hasResurrectedThisBattle = false;
    private int _turnCount = 1;
    private int _lastTurnEndClickFrame = -1;
    private bool _hasUsedFirstSkillInBattle = false;
    private bool _turnEndButtonListenerBound;
    private int _blueTowerSkillUses = 0;
    private int _cheongwoonApSpent = 0;
    private int _archiumInventionStep = 0;
    private int _carriedOverAP = 0;
    private int _nextTurnLockedAP = 0;
    private bool _overloadedThisTurn = false;
    private bool _canOverloadThisTurn = true;
    private int _skillsUsedThisBattle = 0;
    private readonly BattleSelectionState _selection = new BattleSelectionState();

    private int _gambleRolledAP = -1;
    private int _limitRolledAP = -1;
    private int _clusterRolledAP = -1;
    private int _arcanaRolledAP = -1;
    private int _sinRolledAP = -1;
    public TheLastArk.Battle.ArcanaBattleState _arcanaState = new TheLastArk.Battle.ArcanaBattleState();
    public TheLastArk.Battle.SinActiveState _sinState = new TheLastArk.Battle.SinActiveState();
    private bool _limitFirstSkillFreeThisTurn = false;
    private bool _limitRetainAllSkillsThisTurn = false;

    public bool IsPlayerTurn => _currentPhase == BattlePhase.PlayerTurn;
    public BattleConfig Config => battleConfig;

    public int MaxAP
    {
        get
        {
            int baseAP = 4;
            if (TheLastArk.Managers.TrainManager.IsInitialized)
            {
                if (TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
                    TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.GambleId)
                {
                    baseAP = _gambleRolledAP > 0 ? _gambleRolledAP : TheLastArk.Managers.TrainManager.Instance.GetNexusBaseAP();
                }
                else if (TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
                    TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.LimitId)
                {
                    baseAP = _limitRolledAP > 0 ? _limitRolledAP : TheLastArk.Managers.TrainManager.Instance.GetNexusBaseAP();
                }
                else if (TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
                    TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.ClusterId)
                {
                    baseAP = _clusterRolledAP > 0 ? _clusterRolledAP : TheLastArk.Managers.TrainManager.Instance.GetNexusBaseAP();
                }
                else if (TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
                    TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.ArcanaId)
                {
                    baseAP = _arcanaRolledAP > 0 ? _arcanaRolledAP : TheLastArk.Managers.TrainManager.Instance.GetNexusBaseAP();
                }
                else if (TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
                    TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.SinId)
                {
                    baseAP = _sinRolledAP > 0 ? _sinRolledAP : TheLastArk.Managers.TrainManager.Instance.GetNexusBaseAP();
                }
                else
                {
                    baseAP = TheLastArk.Managers.TrainManager.Instance.GetNexusTurnAP(_turnCount);
                }
            }
            else if (battleConfig != null)
            {
                baseAP = battleConfig.MaxAP;
            }

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
        if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.GambleId)
        {
            TheLastArk.UI.GambleDiceUI.Instance.ShowDiceRoll(TheLastArk.Managers.TrainManager.Instance.nexusCar, (rolledAP) =>
            {
                _gambleRolledAP = rolledAP;
                _limitRolledAP = -1;
                _clusterRolledAP = -1;
                currentAP = MaxAP;
                UpdateAPUI();
            });
        }
        else if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.LimitId)
        {
            TheLastArk.UI.LimitCardUI.Instance.ShowCardDraw(TheLastArk.Managers.TrainManager.Instance.nexusCar, (rolledAP, freeSkill, retainAll) =>
            {
                _limitRolledAP = rolledAP;
                _gambleRolledAP = -1;
                _clusterRolledAP = -1;
                _limitFirstSkillFreeThisTurn = freeSkill;
                _limitRetainAllSkillsThisTurn = retainAll;
                currentAP = MaxAP;
                UpdateAPUI();
            });
        }
        else if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.ClusterId)
        {
            TheLastArk.UI.ClusterCardUI.Instance.ShowCardDraw(TheLastArk.Managers.TrainManager.Instance.nexusCar, (rolledAP, result) =>
            {
                _clusterRolledAP = rolledAP;
                _gambleRolledAP = -1;
                _limitRolledAP = -1;
                _arcanaRolledAP = -1;
                currentAP = MaxAP;
                UpdateAPUI();
                ApplyClusterSuitEffects(result);
            });
        }
        else if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.ArcanaId)
        {
            TheLastArk.UI.ArcanaCardUI.Instance.ShowTarotDraw(TheLastArk.Managers.TrainManager.Instance.nexusCar, _arcanaState, (rolledAP, result) =>
            {
                _arcanaRolledAP = rolledAP;
                _gambleRolledAP = -1;
                _limitRolledAP = -1;
                _clusterRolledAP = -1;
                _sinRolledAP = -1;
                currentAP = MaxAP;
                UpdateAPUI();
                ApplyArcanaCardEffects(result);
            });
        }
        else if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.SinId)
        {
            _sinState.ClearCurrentSin();
            var newSin = TheLastArk.Battle.SinModuleManager.DrawNextSin(TheLastArk.Managers.TrainManager.Instance.nexusCar, _sinState);
            _sinState.currentSin = newSin;
            _sinState.remainingTurns = 3;

            TheLastArk.UI.SinModuleUI.Instance.ShowSinManifestation(newSin, TheLastArk.Managers.TrainManager.Instance.nexusCar, _sinState, (rolledAP) =>
            {
                _sinRolledAP = rolledAP;
                _gambleRolledAP = -1;
                _limitRolledAP = -1;
                _clusterRolledAP = -1;
                _arcanaRolledAP = -1;
                currentAP = MaxAP;
                UpdateAPUI();
                ApplySinStartOfTurnEffects();
            });
        }
        else
        {
            _gambleRolledAP = -1;
            _limitRolledAP = -1;
            _clusterRolledAP = -1;
            _arcanaRolledAP = -1;
            _sinRolledAP = -1;
            _limitFirstSkillFreeThisTurn = false;
            _limitRetainAllSkillsThisTurn = false;
            currentAP = MaxAP;
            UpdateAPUI();
        }

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

        _hasUsedFirstSkillInBattle = false;

        // [철벽 성채의 조각] 유물: 전투 시작 시 모든 아군에게 보호 20(2턴) 부여
        if (TheLastArk.Managers.ResourceManager.Instance != null &&
            TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.IronFortressFragment))
        {
            foreach (var ally in playerParty)
            {
                if (ally != null && ally.status != null)
                {
                    ally.status.ApplyStatusEffect(EffectType.Shield, 20f, 2);
                    Debug.Log($"[철벽 성채의 조각] {ally.characterName}에게 보호막 20 부여!");
                }
            }
        }

        // [적응형 보호막 생성기] 전투 시작 시 모든 아군이 잃은 체력의 20%만큼 1턴간 보호막 생성
        if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.HasPartEffectInAnyCar(TheLastArk.Data.TrainPartEffectType.AdaptiveShieldGenerator))
        {
            foreach (var ally in playerParty)
            {
                if (ally != null && ally.status != null && ally.status.currentHp > 0)
                {
                    float lostHp = Mathf.Max(0f, ally.status.FinalMaxHp - ally.status.currentHp);
                    if (lostHp > 0f)
                    {
                        float shieldAmount = lostHp * 0.20f;
                        ally.status.ApplyStatusEffect(EffectType.Shield, shieldAmount, 1);
                        Debug.Log($"[적응형 보호막 생성기] {ally.characterName}에게 잃은 체력 20% 보호막({shieldAmount:F0}) 생성");
                    }
                }
            }
        }

        if (targetHandler != null)
        {
            targetHandler.TargetSelected -= OnTargetSelected;
            targetHandler.TargetSelected += OnTargetSelected;
            targetHandler.TargetCanceled -= CancelPendingSelection;
            targetHandler.TargetCanceled += CancelPendingSelection;
        }
        EnterPhase(BattlePhase.PlayerTurn);
    }

    private void OnDestroy()
    {
        if (targetHandler == null) return;
        targetHandler.TargetSelected -= OnTargetSelected;
        targetHandler.TargetCanceled -= CancelPendingSelection;
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
        if (existingResourcePanel == null)
        {
            var resourceUI = gameObject.GetComponent<TheLastArk.UI.ExplorationResourceUI>();
            if (resourceUI == null)
                resourceUI = gameObject.AddComponent<TheLastArk.UI.ExplorationResourceUI>();

            resourceUI.Initialize(topBarCanvasObj.transform);
        }

        EnsureTurnCountUI(topBarCanvasObj.transform);
    }

    private void EnsureTurnCountUI(Transform topBarCanvasTransform)
    {
        if (turnCountText == null)
        {
            GameObject existingTurnObj = GameObject.Find("TurnCountText");
            if (existingTurnObj != null)
            {
                turnCountText = existingTurnObj.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject turnObj = new GameObject("TurnCountText");
                turnCountText = turnObj.AddComponent<TextMeshProUGUI>();
            }
        }

        if (turnCountText != null)
        {
            turnCountText.transform.SetParent(topBarCanvasTransform, false);
            turnCountText.transform.SetAsLastSibling();

            RectTransform rect = turnCountText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -20f); // 화면 최상단 중앙 위치
            rect.sizeDelta = new Vector2(250f, 50f);

            turnCountText.fontSize = 32;
            turnCountText.alignment = TextAlignmentOptions.Center;
            turnCountText.color = Color.white;
            turnCountText.fontStyle = FontStyles.Bold;

            TMPFontManager.ApplyFont(turnCountText);
        }
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

        if (SkillFirstTargeting && _selection.Skill == skill && _selection.Actor == actor)
        {
            CancelPendingSelection();
            return;
        }

        _selection.Clear();
        _selection.Set(skill, actor);

        if (SkillFirstTargeting)
        {
            if (targetHandler != null) targetHandler.Deselect(false);
            NotificationManager.Instance.ShowMessage("대상을 선택하세요. (우클릭/Esc: 취소)", Color.yellow);
            return;
        }

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

        if (SkillFirstTargeting)
        {
            if (targetHandler != null) targetHandler.Deselect(false);
            NotificationManager.Instance.ShowMessage("대상을 선택하세요. (우클릭/Esc: 취소)", Color.yellow);
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
        if (actor.status.GetStatus(EffectType.Stun) != null)
        {
            NotificationManager.Instance.ShowMessage("[기절] 행동할 수 없습니다.", Color.red);
            return;
        }
        int selectedSkillSlot = actor.status.dynamicActiveSkill.IndexOf(skill);
        bool isBlocked = actor.status.activeStatusEffects.Exists(e => e.effectType == EffectType.Blockade
            && e.remainingTurns > 0 && (e.skillSlot < 0 || e.skillSlot == selectedSkillSlot));
        if (isBlocked)
        {
            NotificationManager.Instance.ShowMessage("[봉쇄] 해당 스킬칸을 사용할 수 없습니다.", Color.red);
            return;
        }
        if (actor.status.GetStatus(EffectType.Fatigue) != null && actor.status.lastUsedSkill == skill)
        {
            NotificationManager.Instance.ShowMessage("[피로] 같은 스킬을 연속으로 사용할 수 없습니다.", Color.red);
            return;
        }
        int skillIdx = actor.status.SkillLevelIndex;
        SkillLevelData levelData = skill.levels[Mathf.Clamp(skillIdx, 0, skill.levels.Length - 1)];

        if (!TargetResolver.IsValid(primaryTarget, levelData.targetType, playerParty, enemyParty))
        {
            NotificationManager.Instance.ShowMessage($"{skill.skillName}에 맞는 대상이 아닙니다.", Color.red);
            if (!SkillFirstTargeting) _selection.Clear();
            return;
        }

        // [색욕] 매혹된 캐릭터 직접 조종 제한
        if (_sinState.charmedCharacters != null && _sinState.charmedCharacters.Contains(actor))
        {
            NotificationManager.Instance.ShowMessage($"[매혹] {actor.characterName}(은)는 매혹되어 이번 턴에 직접 조종할 수 없습니다!", Color.magenta);
            if (!SkillFirstTargeting) _selection.Clear();
            return;
        }

        // [나태] 스킬 최대 3회 사용 제한 (순교자의 서약 제외)
        bool hasMartyrVow = TheLastArk.Managers.TrainManager.IsInitialized && TheLastArk.Managers.TrainManager.Instance.nexusCar != null && TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TrainPartEffectType.SinMartyrVow);
        if (!hasMartyrVow && _sinState.currentSin == TheLastArk.Battle.SinType.Sloth && !_sinState.isIndulgedCurrentSin)
        {
            if (_sinState.skillsUsedThisTurn >= 3)
            {
                NotificationManager.Instance.ShowMessage("[나태의 죄] 이번 턴에는 스킬을 최대 3회까지만 사용할 수 있습니다!", Color.red);
                if (!SkillFirstTargeting) _selection.Clear();
                return;
            }
        }

        // [황제] 이전 턴 사용 스킬 금지 체크
        if (_arcanaState.emperorBannedSkillsNextTurn != null && _arcanaState.emperorBannedSkillsNextTurn.Contains(skill))
        {
            NotificationManager.Instance.ShowMessage("[황제] 이전 턴에 사용했던 스킬은 이번 턴에 사용할 수 없습니다!", Color.red);
            if (!SkillFirstTargeting) _selection.Clear();
            return;
        }

        // [황제] 이번 턴 이미 사용한 스킬 중복 시전 금지 체크
        if (_arcanaState.isEmperorActiveThisTurn && _arcanaState.emperorUsedSkillsThisTurn.Contains(skill))
        {
            NotificationManager.Instance.ShowMessage("[황제] 이번 턴에 이미 사용한 스킬은 다시 사용할 수 없습니다!", Color.red);
            if (!SkillFirstTargeting) _selection.Clear();
            return;
        }

        int cost = levelData.overrideCost != -1 ? levelData.overrideCost : skill.baseCost;

        // [황제 / 오만] 모든 스킬 비용 0 고정
        if (_arcanaState.isEmperorActiveThisTurn || (_sinState.currentSin == TheLastArk.Battle.SinType.Pride && !_sinState.isIndulgedCurrentSin))
        {
            cost = 0;
        }
        else
        {
            // [광대] 첫 2개 스킬 비용 0 소모
            if (_arcanaState.foolFreeSkillsRemaining > 0)
            {
                cost = 0;
                _arcanaState.foolFreeSkillsRemaining--;
                NotificationManager.Instance?.ShowMessage($"[광대] 스킬 비용 0 소모! (남은 횟수: {_arcanaState.foolFreeSkillsRemaining})", Color.yellow);
            }
            // [완전수 모듈] 수치가 6 또는 28일 때 이번 턴 첫 스킬 비용 0 소모
            else if (_limitFirstSkillFreeThisTurn)
            {
                cost = 0;
                _limitFirstSkillFreeThisTurn = false;
                NotificationManager.Instance?.ShowMessage("[완전수 모듈] 첫 스킬 비용 0 소모!", Color.green);
            }

            // [교황] 3코스트 이상 스킬 비용 -1
            if (_arcanaState.isHierophantActiveThisTurn && cost >= 3)
            {
                cost = Mathf.Max(0, cost - 1);
            }

            // [힘] 공격 스킬 비용 +1
            bool isAttackSkill = levelData.targetType == TargetType.SingleEnemy ||
                                 levelData.targetType == TargetType.LeftEnemy ||
                                 levelData.targetType == TargetType.RightEnemy ||
                                 levelData.targetType == TargetType.AdjacentEnemy ||
                                 levelData.targetType == TargetType.AllEnemy ||
                                 (levelData.effects != null && levelData.effects.Exists(e => e.type == EffectType.Damage));

            if (_arcanaState.isStrengthActiveThisTurn && isAttackSkill)
            {
                cost += 1;
            }
        }

        var frost = actor.status.GetStatus(EffectType.Frost);
        if (frost != null) cost += Mathf.Max(0, Mathf.RoundToInt(frost.damagePerTurn));

        if (_arcanaState.isEmperorActiveThisTurn)
        {
            _arcanaState.emperorUsedSkillsThisTurn.Add(skill);
        }

        if (currentAP < cost)
        {
            bool canOverload = TheLastArk.Managers.TrainManager.IsInitialized &&
                               TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
                               TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TheLastArk.Data.TrainPartEffectType.OverloadModule) &&
                               _canOverloadThisTurn && !_overloadedThisTurn;

            int needed = cost - currentAP;
            if (canOverload && needed <= 5)
            {
                _nextTurnLockedAP = needed;
                _overloadedThisTurn = true;
                currentAP += needed;
                NotificationManager.Instance.ShowMessage($"[과부하 모듈] 다음 턴 행동력 {needed}를 미리 사용합니다! (다음 턴 {needed} 잠김)", Color.yellow);
            }
            else
            {
                NotificationManager.Instance.ShowMessage(canOverload ? "행동력이 부족합니다. (과부하는 최대 5까지 가능)" : "행동력이 부족합니다.", Color.red);
                return;
            }
        }

        currentAP -= cost;
        UpdateAPUI();

        _sinState.skillsUsedThisTurn++;

        // [오만] 스킬 사용 시마다 무작위 아군 정신력 -6 감소
        if (_sinState.currentSin == TheLastArk.Battle.SinType.Pride && !_sinState.isIndulgedCurrentSin)
        {
            var livingAllies = new List<BattleCharacter>();
            foreach (var a in playerParty) { if (a != null && a.status != null && a.status.currentHp > 0) livingAllies.Add(a); }
            if (livingAllies.Count > 0)
            {
                var victim = livingAllies[UnityEngine.Random.Range(0, livingAllies.Count)];
                victim.status.currentMental = Mathf.Max(0f, victim.status.currentMental - 6f);
                if (victim.view != null) victim.view.UpdateVisual(victim.status);
                NotificationManager.Instance?.ShowMessage($"[오만의 죄] 스킬 사용으로 {victim.characterName} 정신력 -6 감소!", Color.magenta);
            }
        }

        // [연속 전투 촉진기] 한 전투에서 스킬을 7번 사용할 때마다 행동력 +2 획득
        _skillsUsedThisBattle++;
        if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.HasPartEffectInAnyCar(TheLastArk.Data.TrainPartEffectType.ContinuousCombatCatalyst))
        {
            if (_skillsUsedThisBattle % 7 == 0)
            {
                currentAP = Mathf.Min(MaxAP, currentAP + 2);
                UpdateAPUI();
                NotificationManager.Instance?.ShowMessage("[연속 전투 촉진기] 7회 스킬 사용 달성! 행동력 +2 획득!", Color.cyan);
            }
        }

        // [끊임없는 전투] 유물 스킬 발동 처리
        if (skill != null && !string.IsNullOrEmpty(skill.skillName) && skill.skillName.Contains("끊임없는 전투"))
        {
            float lostHp = Mathf.Max(0f, actor.status.FinalMaxHp - actor.status.currentHp);
            float atkBuff = lostHp * 0.20f;
            actor.status.bonusAttack += atkBuff;
            if (actor.view != null) actor.view.UpdateVisual(actor.status);

            int slashCount = skillIdx >= 1 ? 2 : 1;
            actor.CastSlashOnRandomEnemy(slashCount);

            NotificationManager.Instance.ShowMessage($"[끊임없는 전투] 잃은 체력 비례 공격력 +{atkBuff:F1} 획득 & 가르기 {slashCount}회 시전!", Color.red);

            if (skillIdx < 2)
            {
                actor.status.dynamicActiveSkill.Remove(skill);
                NotificationManager.Instance.ShowMessage("[끊임없는 전투] 일회성 스킬 소모 완료.", Color.gray);
            }
        }
        else
        {
            var confusion = actor.status.GetStatus(EffectType.Confusion);
            if (confusion != null && UnityEngine.Random.value < confusion.damagePerTurn * 0.01f)
            {
                var pool = levelData.targetType == TargetType.Friendly || levelData.targetType == TargetType.AllFriendly ? playerParty : enemyParty;
                var alive = pool.FindAll(c => c != null && c.status != null && c.status.currentHp > 0f);
                if (alive.Count > 0) primaryTarget = alive[UnityEngine.Random.Range(0, alive.Count)];
                NotificationManager.Instance?.ShowMessage("[혼란] 스킬 대상이 무작위로 변경되었습니다!", Color.magenta);
            }
            List<BattleCharacter> targets = TargetResolver.Resolve(primaryTarget, levelData.targetType, playerParty, enemyParty);
            EffectEngine.ProcessSkill(actor, targets, levelData, skill.skillName);

            // [탑] 이번 턴 처음 사용하는 스킬 2번 발동 (더블 캐스트)
            if (_arcanaState.isTowerDoubleCastActive)
            {
                _arcanaState.isTowerDoubleCastActive = false;
                List<BattleCharacter> doubleTargets = TargetResolver.Resolve(primaryTarget, levelData.targetType, playerParty, enemyParty);
                EffectEngine.ProcessSkill(actor, doubleTargets, levelData, skill.skillName);
                NotificationManager.Instance?.ShowMessage("[탑] 첫 스킬이 2번 연속 발동되었습니다!", Color.red);
            }

            // [식탐] 적 처치 시 추가 행동력 획득
            if (_sinState.currentSin == TheLastArk.Battle.SinType.Gluttony && !_sinState.isIndulgedCurrentSin)
            {
                bool anyEnemyKilled = false;
                foreach (var t in targets)
                {
                    if (t != null && t.status != null && t.status.origin != null && t.status.origin.isEnemy && t.status.currentHp <= 0)
                    {
                        anyEnemyKilled = true;
                        break;
                    }
                }

                if (anyEnemyKilled)
                {
                    _sinState.enemyKilledThisTurn = true;
                    bool hasMartyr = TheLastArk.Managers.TrainManager.IsInitialized && TheLastArk.Managers.TrainManager.Instance.nexusCar != null && TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TrainPartEffectType.SinMartyrVow);
                    bool hasGevil = TheLastArk.Managers.TrainManager.IsInitialized && TheLastArk.Managers.TrainManager.Instance.nexusCar != null && TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TrainPartEffectType.SinGreaterEvil);
                    int bonus = hasMartyr ? 6 : (hasGevil ? 6 : 3);

                    currentAP = Mathf.Min(MaxAP + 10, currentAP + bonus);
                    UpdateAPUI();
                    NotificationManager.Instance?.ShowMessage($"[식탐의 죄] 적 처치 달성! 행동력 +{bonus} 획득!", Color.yellow);
                }
            }
        }

        actor.status.lastUsedSkill = skill;
        actor.ResolveActionStatusEffects();

        // [별자리] 스킬 3회 사용 시마다 행동력 +1 획득
        if (_arcanaState.isConstellationActive)
        {
            _arcanaState.constellationSkillCount++;
            if (_arcanaState.constellationSkillCount % 3 == 0)
            {
                currentAP = Mathf.Min(MaxAP + 5, currentAP + 1);
                UpdateAPUI();
                NotificationManager.Instance?.ShowMessage("[별자리] 스킬 3회 사용 달성! 행동력 +1 획득!", Color.cyan);
            }
        }

        // [푸른 마탑] 시너지 및 [별의 그리모어] 유물: 스킬 사용 시 효과 발동
        if (actor.status.HasSynergy(TheLastArk.Data.SynergyType.BlueTower))
        {
            var activeSyn = TheLastArk.Character.SynergyCalculator.CalculateActiveSynergies();
            if (activeSyn.TryGetValue(TheLastArk.Data.SynergyType.BlueTower, out int btCount) && btCount >= 2)
            {
                bool hasGrimoire = TheLastArk.Managers.ResourceManager.Instance != null &&
                                   TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.GrimoireOfStars);
                int reqUses = (btCount >= 8 || hasGrimoire) ? 1 : (btCount >= 6 ? 2 : 3);
                _blueTowerSkillUses++;
                if (_blueTowerSkillUses >= reqUses)
                {
                    _blueTowerSkillUses = 0;
                    TriggerBlueTowerEffect(actor, btCount);
                }
            }
        }

        // [청운] 시너지 및 [청운] 유물: 행동력 소모 시 효과 발동
        if (cost > 0 && actor.status.HasSynergy(TheLastArk.Data.SynergyType.Cheongwoon))
        {
            var activeSyn = TheLastArk.Character.SynergyCalculator.CalculateActiveSynergies();
            if (activeSyn.TryGetValue(TheLastArk.Data.SynergyType.Cheongwoon, out int cwCount) && cwCount >= 3)
            {
                bool hasCwRelic = TheLastArk.Managers.ResourceManager.Instance != null &&
                                  TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.CheongwoonRelic);
                int apThreshold = (cwCount >= 7 || hasCwRelic) ? 2 : 3;
                _cheongwoonApSpent += cost;
                while (_cheongwoonApSpent >= apThreshold)
                {
                    _cheongwoonApSpent -= apThreshold;
                    TriggerCheongwoonEffect(cwCount);
                }
            }
        }

        // [한 잔 더!] 유물: 비용이 0인 스킬을 사용할 때 행동력 1 회복
        if (cost == 0 && TheLastArk.Managers.ResourceManager.Instance != null &&
            TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.OneMoreDrink))
        {
            currentAP = Mathf.Min(MaxAP, currentAP + 1);
            UpdateAPUI();
            NotificationManager.Instance.ShowMessage("[한 잔 더!] 행동력 1 회복!", Color.cyan);
        }

        // [기름칠된 톱니바퀴] 유물: 각 전투에서 가장 처음으로 사용하는 스킬에 유지 효과 부여
        if (!_hasUsedFirstSkillInBattle && TheLastArk.Managers.ResourceManager.Instance != null &&
            TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.OiledGear))
        {
            _hasUsedFirstSkillInBattle = true;
            NotificationManager.Instance.ShowMessage("[기름칠된 톱니바퀴] 첫 스킬 유지 효과 적용!", Color.yellow);
        }

        _selection.Clear();
        if (SkillFirstTargeting && targetHandler != null) targetHandler.Deselect(false);
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
                    if (!SkillFirstTargeting) _selection.Clear();
                    return;
                }
                targets.Add(primaryTarget);
                break;

            case TheLastArk.Data.ConsumableEffectType.HealHP:
            case TheLastArk.Data.ConsumableEffectType.HealMental:
                if (primaryTarget == null || enemyParty.Contains(primaryTarget))
                {
                    NotificationManager.Instance.ShowMessage("아군을 선택하세요.", Color.red);
                    if (!SkillFirstTargeting) _selection.Clear();
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
        if (SkillFirstTargeting && targetHandler != null) targetHandler.Deselect(false);
    }

    private void OnTargetSelected(GameObject selectedTarget)
    {
        if (!SkillFirstTargeting || !_selection.IsReady || selectedTarget == null) return;
        if (_selection.Skill != null) PerformSkill();
        else if (_selection.Consumable != null) PerformConsumable();
    }

    public void CancelPendingSelection()
    {
        _selection.Clear();
        if (targetHandler != null) targetHandler.Deselect(false);
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

        // [에너지 저장소] 미사용 행동력 최대 3까지 다음 턴으로 이월 저장
        if (TheLastArk.Managers.TrainManager.IsInitialized && TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TheLastArk.Data.TrainPartEffectType.EnergyStorage))
        {
            _carriedOverAP = Mathf.Clamp(currentAP, 0, 3);
            if (_carriedOverAP > 0)
            {
                Debug.Log($"[에너지 저장소] 미사용 행동력 {_carriedOverAP} 다음 턴 이월 저장.");
            }
        }
        else
        {
            _carriedOverAP = 0;
        }

        // [죽음] 턴 종료 시 모든 아군 체력/정신력 4 감소
        if (_arcanaState.isDeathPendingThisTurn)
        {
            foreach (var a in playerParty)
            {
                if (a != null && a.status != null && a.status.currentHp > 0)
                {
                    a.status.currentHp = Mathf.Max(1f, a.status.currentHp - 4f);
                    a.status.currentMental = Mathf.Max(0f, a.status.currentMental - 4f);
                    if (a.view != null) a.view.UpdateVisual(a.status);
                }
            }
            NotificationManager.Instance?.ShowMessage("[죽음] 턴 종료로 모든 아군 체력/정신력 -4 감소", Color.black);
        }

        // [악마 계약] 턴 종료 시 지속 효과 처리
        if (_arcanaState.activeDevilContract == TheLastArk.Battle.DevilContractType.Option2_EveryTurnAP_LoseMental_NoMoreDevil)
        {
            foreach (var a in playerParty)
            {
                if (a != null && a.status != null && a.status.currentHp > 0)
                {
                    a.status.currentMental = Mathf.Max(0f, a.status.currentMental - 6f);
                    if (a.view != null) a.view.UpdateVisual(a.status);
                }
            }
            NotificationManager.Instance?.ShowMessage("[악마 계약] 턴 종료로 모든 아군 정신력 -6 감소", Color.red);
        }
        else if (_arcanaState.activeDevilContract == TheLastArk.Battle.DevilContractType.Option3_EveryTurnAP_GainDebuffs)
        {
            foreach (var a in playerParty)
            {
                if (a != null && a.status != null && a.status.currentHp > 0)
                {
                    a.status.ApplyStatusEffect(EffectType.Bleed, 1f, 2);
                }
            }
            NotificationManager.Instance?.ShowMessage("[악마 계약] 턴 종료로 모든 아군 약화/취약 획득", Color.red);
        }

        // [황제] 다음 턴 스킬 밴 목록 갱신 및 턴 플래그 리셋
        _arcanaState.emperorBannedSkillsNextTurn = new HashSet<SkillInfo>(_arcanaState.emperorUsedSkillsThisTurn);
        _arcanaState.ResetTurnFlags();

        // [식탐] 적을 처치하지 못했을 시 다음 턴 행동력 감소 예약
        if (_sinState.currentSin == TheLastArk.Battle.SinType.Gluttony && !_sinState.isIndulgedCurrentSin)
        {
            if (!_sinState.enemyKilledThisTurn)
            {
                bool hasMartyr = TheLastArk.Managers.TrainManager.IsInitialized && TheLastArk.Managers.TrainManager.Instance.nexusCar != null && TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TrainPartEffectType.SinMartyrVow);
                bool hasGevil = TheLastArk.Managers.TrainManager.IsInitialized && TheLastArk.Managers.TrainManager.Instance.nexusCar != null && TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TrainPartEffectType.SinGreaterEvil);
                _sinState.pendingGluttonyPenaltyNextTurn = hasMartyr ? 9 : (hasGevil ? 6 : 3);
                NotificationManager.Instance?.ShowMessage($"[식탐의 죄] 적 미처치로 다음 턴 행동력 -{_sinState.pendingGluttonyPenaltyNextTurn} 감소 예정", Color.red);
            }
        }

        // [탐욕] 턴 종료 시 남은(미사용) 행동력 비례 모든 아군 정신력 피해
        if (_sinState.currentSin == TheLastArk.Battle.SinType.Greed && !_sinState.isIndulgedCurrentSin && currentAP > 0)
        {
            bool hasMartyr = TheLastArk.Managers.TrainManager.IsInitialized && TheLastArk.Managers.TrainManager.Instance.nexusCar != null && TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TrainPartEffectType.SinMartyrVow);
            bool hasGevil = TheLastArk.Managers.TrainManager.IsInitialized && TheLastArk.Managers.TrainManager.Instance.nexusCar != null && TheLastArk.Managers.TrainManager.Instance.nexusCar.HasPartEffect(TrainPartEffectType.SinGreaterEvil);

            foreach (var a in playerParty)
            {
                if (a != null && a.status != null && a.status.currentHp > 0)
                {
                    if (hasMartyr)
                    {
                        float mDmg = a.status.FinalMaxMental * 0.20f * currentAP;
                        a.status.currentMental = Mathf.Max(0f, a.status.currentMental - mDmg);
                    }
                    else
                    {
                        int pPerAp = hasGevil ? 6 : 3;
                        a.status.currentMental = Mathf.Max(0f, a.status.currentMental - (pPerAp * currentAP));
                    }
                    if (a.view != null) a.view.UpdateVisual(a.status);
                }
            }
            NotificationManager.Instance?.ShowMessage($"[탐욕의 죄] 미사용 행동력({currentAP}) 비례 모든 아군 정신력 감소!", Color.red);
        }

        // 씬 턴 카운터 리셋
        _sinState.ResetTurnCounters();

        CancelPendingSelection();
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

        _limitFirstSkillFreeThisTurn = false;
        _limitRetainAllSkillsThisTurn = false;

        if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.GambleId)
        {
            TheLastArk.UI.GambleDiceUI.Instance.ShowDiceRoll(TheLastArk.Managers.TrainManager.Instance.nexusCar, (rolledAP) =>
            {
                _gambleRolledAP = rolledAP;
                _limitRolledAP = -1;
                _clusterRolledAP = -1;
                ApplyPlayerTurnStartAP();
            });
        }
        else if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.LimitId)
        {
            TheLastArk.UI.LimitCardUI.Instance.ShowCardDraw(TheLastArk.Managers.TrainManager.Instance.nexusCar, (rolledAP, freeSkill, retainAll) =>
            {
                _limitRolledAP = rolledAP;
                _gambleRolledAP = -1;
                _clusterRolledAP = -1;
                _limitFirstSkillFreeThisTurn = freeSkill;
                _limitRetainAllSkillsThisTurn = retainAll;
                ApplyPlayerTurnStartAP();
            });
        }
        else if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.ClusterId)
        {
            TheLastArk.UI.ClusterCardUI.Instance.ShowCardDraw(TheLastArk.Managers.TrainManager.Instance.nexusCar, (rolledAP, result) =>
            {
                _clusterRolledAP = rolledAP;
                _gambleRolledAP = -1;
                _limitRolledAP = -1;
                _arcanaRolledAP = -1;
                ApplyPlayerTurnStartAP();
                ApplyClusterSuitEffects(result);
            });
        }
        else if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.ArcanaId)
        {
            TheLastArk.UI.ArcanaCardUI.Instance.ShowTarotDraw(TheLastArk.Managers.TrainManager.Instance.nexusCar, _arcanaState, (rolledAP, result) =>
            {
                _arcanaRolledAP = rolledAP;
                _gambleRolledAP = -1;
                _limitRolledAP = -1;
                _clusterRolledAP = -1;
                _sinRolledAP = -1;
                ApplyPlayerTurnStartAP();
                ApplyArcanaCardEffects(result);
            });
        }
        else if (TheLastArk.Managers.TrainManager.IsInitialized &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar != null &&
            TheLastArk.Managers.TrainManager.Instance.nexusCar.installedModuleId == TheLastArk.Data.NexusModuleDatabase.SinId)
        {
            var nexusCar = TheLastArk.Managers.TrainManager.Instance.nexusCar;
            bool shouldDrawNewSin = _sinState.rerollSinNextTurn || !_sinState.currentSin.HasValue || _sinState.remainingTurns <= 0;

            if (shouldDrawNewSin)
            {
                _sinState.rerollSinNextTurn = false;
                _sinState.isIndulgedCurrentSin = false;
                var newSin = TheLastArk.Battle.SinModuleManager.DrawNextSin(nexusCar, _sinState);
                _sinState.currentSin = newSin;
                _sinState.remainingTurns = 3;

                TheLastArk.UI.SinModuleUI.Instance.ShowSinManifestation(newSin, nexusCar, _sinState, (rolledAP) =>
                {
                    _sinRolledAP = rolledAP;
                    _gambleRolledAP = -1;
                    _limitRolledAP = -1;
                    _clusterRolledAP = -1;
                    _arcanaRolledAP = -1;
                    ApplyPlayerTurnStartAP();
                    ApplySinStartOfTurnEffects();
                });
            }
            else
            {
                _sinState.remainingTurns--;
                _sinRolledAP = TheLastArk.Battle.SinModuleManager.CalculateSinAP(_sinState.currentSin.Value, nexusCar);
                _gambleRolledAP = -1;
                _limitRolledAP = -1;
                _clusterRolledAP = -1;
                _arcanaRolledAP = -1;
                ApplyPlayerTurnStartAP();
                ApplySinStartOfTurnEffects();
                TheLastArk.UI.SinModuleUI.Instance.UpdateHUD(_sinState, nexusCar);
            }
        }
        else
        {
            _gambleRolledAP = -1;
            _limitRolledAP = -1;
            _clusterRolledAP = -1;
            _arcanaRolledAP = -1;
            _sinRolledAP = -1;
            ApplyPlayerTurnStartAP();
        }
    }

    private void ApplyArcanaCardEffects(TheLastArk.Battle.ArcanaDrawResult result)
    {
        if (result == null || result.drawnCard == null) return;
        if (!TheLastArk.Managers.TrainManager.IsInitialized || TheLastArk.Managers.TrainManager.Instance.nexusCar == null) return;

        ExecuteSingleTarotEffect(result.drawnCard, result.devilChoice);

        if (result.hermitChainedCard != null)
        {
            ExecuteSingleTarotEffect(result.hermitChainedCard, result.devilChoice);
        }

        if (!string.IsNullOrEmpty(result.summary))
        {
            NotificationManager.Instance?.ShowMessage($"[아르카나 개방] {result.summary}", Color.yellow);
        }
    }

    private void ExecuteSingleTarotEffect(TheLastArk.Battle.ArcanaCardInfo card, TheLastArk.Battle.DevilContractType devilChoice)
    {
        if (card == null) return;

        var livingAllies = new List<BattleCharacter>();
        if (playerParty != null)
        {
            foreach (var a in playerParty)
            {
                if (a != null && a.status != null && a.status.currentHp > 0)
                    livingAllies.Add(a);
            }
        }

        var livingEnemies = new List<BattleCharacter>();
        if (enemyParty != null)
        {
            foreach (var e in enemyParty)
            {
                if (e != null && e.status != null && e.status.currentHp > 0)
                    livingEnemies.Add(e);
            }
        }

        switch (card.cardType)
        {
            case TarotCardType.Fool:
                _arcanaState.foolFreeSkillsRemaining = 2;
                NotificationManager.Instance?.ShowMessage("[광대] 이번 턴 사용하는 첫 스킬 2개의 비용이 0이 됩니다!", Color.yellow);
                break;

            case TarotCardType.HighPriestess:
                foreach (var a in livingAllies)
                {
                    a.status.ApplyStatusEffect(EffectType.Shield, 1f, 1);
                }
                NotificationManager.Instance?.ShowMessage("[여사제] 모든 아군에게 보호(방어막) 1 부여!", Color.cyan);
                break;

            case TarotCardType.Lovers:
                _arcanaState.isLoversActiveThisTurn = true;
                NotificationManager.Instance?.ShowMessage("[연인] 이번 턴 아군이 얻는 힘/보호 효과가 전체에게 공유됩니다!", Color.magenta);
                break;

            case TarotCardType.Chariot:
                foreach (var a in livingAllies)
                {
                    a.status.bonusAttack += 1;
                    if (a.view != null) a.view.UpdateVisual(a.status);
                }
                NotificationManager.Instance?.ShowMessage("[전차] 모든 아군 힘(공격력) +1 증가!", Color.cyan);
                break;

            case TarotCardType.Strength:
                _arcanaState.isStrengthActiveThisTurn = true;
                foreach (var a in livingAllies)
                {
                    a.status.bonusAttack += 3;
                    if (a.view != null) a.view.UpdateVisual(a.status);
                }
                NotificationManager.Instance?.ShowMessage("[힘] 모든 아군 힘 +3 증가! (이번 턴 공격 스킬 비용 +1)", Color.red);
                break;

            case TarotCardType.WheelOfFortune:
                _arcanaState.isWheelOfFortunePending = true;
                NotificationManager.Instance?.ShowMessage("[운명] 다음 턴에 얻는 행동력이 2배로 증가합니다!", Color.yellow);
                break;

            case TarotCardType.Justice:
                foreach (var e in livingEnemies)
                {
                    e.status.ApplyStatusEffect(EffectType.Bleed, 3f, 3);
                }
                NotificationManager.Instance?.ShowMessage("[정의] 모든 적에게 약화 3 / 취약 3(출혈) 부여!", Color.cyan);
                break;

            case TarotCardType.Hierophant:
                _arcanaState.isHierophantActiveThisTurn = true;
                NotificationManager.Instance?.ShowMessage("[교황] 비용 3 이상인 스킬들의 비용이 -1 감소합니다!", Color.yellow);
                break;

            case TarotCardType.HangedMan:
                _arcanaState.hangedManPenaltyPending = 4;
                NotificationManager.Instance?.ShowMessage("[매달린 사람] 대량 AP 획득! (다음 턴 행동력 -4)", Color.magenta);
                break;

            case TarotCardType.Temperance:
                _arcanaState.temperanceAccumulatedAP += 1;
                NotificationManager.Instance?.ShowMessage($"[절제] 매 턴 행동력 +1 영구 누적! (현재 누적: +{_arcanaState.temperanceAccumulatedAP} AP)", Color.green);
                break;

            case TarotCardType.Tower:
                _arcanaState.isTowerDoubleCastActive = true;
                NotificationManager.Instance?.ShowMessage("[탑] 이번 턴에 처음 사용하는 스킬이 2번 발동(더블 캐스트)됩니다!", Color.red);
                break;

            case TarotCardType.Empress:
                foreach (var a in livingAllies)
                {
                    a.status.currentHp = Mathf.Min(a.status.FinalMaxHp, a.status.currentHp + 5);
                    a.ReceiveMentalHeal(5f);
                    if (a.view != null) a.view.UpdateVisual(a.status);
                }
                NotificationManager.Instance?.ShowMessage("[여제] 모든 아군 체력 +5, 정신력 +5 회복!", Color.green);
                break;

            case TarotCardType.Emperor:
                _arcanaState.isEmperorActiveThisTurn = true;
                NotificationManager.Instance?.ShowMessage("[황제] 이번 턴 모든 스킬 비용 0 고정! (스킬당 1회 제한, 사용 스킬 다음 턴 사용 불가)", Color.red);
                break;

            case TarotCardType.Death:
                _arcanaState.isDeathPendingThisTurn = true;
                NotificationManager.Instance?.ShowMessage("[죽음] 막대한 AP 획득! (턴 종료 시 모든 아군 체력/정신력 -4 감소)", Color.black);
                break;

            case TarotCardType.Devil:
                if (devilChoice == DevilContractType.Option1_MoreAP_MoreDevil)
                {
                    _arcanaState.devilOption1PickCount++;
                    NotificationManager.Instance?.ShowMessage("[악마] 탐욕 계약 체결! (악마 AP 및 등장 확률 증가)", Color.red);
                }
                else if (devilChoice == DevilContractType.Option2_EveryTurnAP_LoseMental_NoMoreDevil)
                {
                    _arcanaState.activeDevilContract = DevilContractType.Option2_EveryTurnAP_LoseMental_NoMoreDevil;
                    NotificationManager.Instance?.ShowMessage("[악마] 광기 계약 체결! (매 턴 +6 AP, 매 턴 전원 정신력 -6, 악마 등장 제외)", Color.red);
                }
                else if (devilChoice == DevilContractType.Option3_EveryTurnAP_GainDebuffs)
                {
                    _arcanaState.activeDevilContract = DevilContractType.Option3_EveryTurnAP_GainDebuffs;
                    NotificationManager.Instance?.ShowMessage("[악마] 고통 계약 체결! (매 턴 +6 AP, 매 턴 전원 약화/취약 획득)", Color.red);
                }
                break;

            case TarotCardType.Judgement:
                if (livingEnemies.Count > 0)
                {
                    BattleCharacter highestHpEnemy = livingEnemies[0];
                    foreach (var e in livingEnemies)
                    {
                        if (e.status.currentHp > highestHpEnemy.status.currentHp) highestHpEnemy = e;
                    }
                    highestHpEnemy.status.ApplyStatusEffect(EffectType.Stun, 0f, 1);
                    NotificationManager.Instance?.ShowMessage($"[심판] 최고 체력 적 {highestHpEnemy.characterName} 1턴 기절!", Color.yellow);
                }
                break;

            case TarotCardType.Sun:
                _arcanaState.isSunRetainActiveThisTurn = true;
                NotificationManager.Instance?.ShowMessage("[태양] 이번 턴 모든 스킬에 유지(Retain) 효과 부여!", Color.yellow);
                break;

            case TarotCardType.Moon:
                int highMentalCount = 0;
                foreach (var a in livingAllies)
                {
                    if (a.status.currentMental >= a.status.FinalMaxMental * 0.5f)
                    {
                        highMentalCount++;
                    }
                    else
                    {
                        a.ReceiveMentalHeal(a.status.FinalMaxMental * 0.15f);
                        if (a.view != null) a.view.UpdateVisual(a.status);
                    }
                }
                if (highMentalCount > 0)
                {
                    currentAP = Mathf.Min(MaxAP + 10, currentAP + highMentalCount);
                    UpdateAPUI();
                    NotificationManager.Instance?.ShowMessage($"[달] 정신력 50% 이상 아군({highMentalCount}명) 비례 행동력 +{highMentalCount} 추가 획득 & 회복!", Color.cyan);
                }
                break;

            case TarotCardType.Star:
                _arcanaState.isConstellationActive = true;
                NotificationManager.Instance?.ShowMessage("[별] 별자리 효과 획득! (스킬 3회 사용 시마다 행동력 +1 충전)", Color.cyan);
                break;

            case TarotCardType.World:
                foreach (var a in livingAllies)
                {
                    a.status.RemoveAllStatusEffects();
                    if (a.view != null) a.view.UpdateVisual(a.status);
                }
                NotificationManager.Instance?.ShowMessage("[세계] 모든 아군의 모든 상태이상이 완전히 정화되었습니다!", Color.green);
                break;
        }
    }

    private void ApplySinStartOfTurnEffects()
    {
        if (!TheLastArk.Managers.TrainManager.IsInitialized || TheLastArk.Managers.TrainManager.Instance.nexusCar == null) return;
        var nexusCar = TheLastArk.Managers.TrainManager.Instance.nexusCar;

        // 식탐 페널티 적용
        if (_sinState.pendingGluttonyPenaltyNextTurn > 0)
        {
            currentAP = Mathf.Max(1, currentAP - _sinState.pendingGluttonyPenaltyNextTurn);
            UpdateAPUI();
            NotificationManager.Instance?.ShowMessage($"<color=#FF5555>[식탐 페널티] 이전 턴 적 미처치로 행동력 -{_sinState.pendingGluttonyPenaltyNextTurn} 감소</color>", Color.red);
            _sinState.pendingGluttonyPenaltyNextTurn = 0;
        }

        // 면죄된 경우 부가효과 스킵
        if (_sinState.isIndulgedCurrentSin || !_sinState.currentSin.HasValue) return;

        var sin = _sinState.currentSin.Value;
        bool hasGreaterEvil = nexusCar.HasPartEffect(TrainPartEffectType.SinGreaterEvil);
        bool hasMartyrVow = nexusCar.HasPartEffect(TrainPartEffectType.SinMartyrVow);

        var livingAllies = new List<BattleCharacter>();
        if (playerParty != null)
        {
            foreach (var a in playerParty) { if (a != null && a.status != null && a.status.currentHp > 0) livingAllies.Add(a); }
        }

        var livingEnemies = new List<BattleCharacter>();
        if (enemyParty != null)
        {
            foreach (var e in enemyParty) { if (e != null && e.status != null && e.status.currentHp > 0) livingEnemies.Add(e); }
        }

        switch (sin)
        {
            case SinType.Sloth:
                if (hasMartyrVow)
                {
                    ExecuteSlothMartyrAutoSkills(livingAllies, livingEnemies);
                }
                break;

            case SinType.Lust:
                ExecuteLustCharm(livingAllies, livingEnemies, hasGreaterEvil, hasMartyrVow);
                break;

            case SinType.Wrath:
                int wrathBuff = hasMartyrVow ? 12 : (hasGreaterEvil ? 6 : 3);
                foreach (var a in livingAllies)
                {
                    a.status.bonusAttack += wrathBuff;
                    a.status.ApplyStatusEffect(EffectType.Bleed, wrathBuff, 2);
                    if (a.view != null) a.view.UpdateVisual(a.status);
                }
                foreach (var e in livingEnemies)
                {
                    e.status.bonusAttack += wrathBuff;
                    e.status.ApplyStatusEffect(EffectType.Bleed, wrathBuff, 2);
                    if (e.view != null) e.view.UpdateVisual(e.status);
                }
                NotificationManager.Instance?.ShowMessage($"[분노의 죄] 모든 아군과 적에게 힘 +{wrathBuff}, 취약 +{wrathBuff} 동시 부여!", Color.red);
                break;

            case SinType.Envy:
                if (hasMartyrVow)
                {
                    ExecuteEnvyMartyrLifeDrain(livingAllies, livingEnemies);
                }
                else
                {
                    ExecuteEnvyBuffCopy(livingAllies, livingEnemies, hasGreaterEvil);
                }
                break;
        }
    }

    private void ExecuteSlothMartyrAutoSkills(List<BattleCharacter> livingAllies, List<BattleCharacter> livingEnemies)
    {
        if (livingAllies.Count == 0) return;
        NotificationManager.Instance?.ShowMessage("[순교자 나태] 턴 시작 시 무작위 스킬 3개 자동 시전!", Color.cyan);

        for (int i = 0; i < 3; i++)
        {
            var actor = livingAllies[UnityEngine.Random.Range(0, livingAllies.Count)];
            if (actor.status == null || actor.status.dynamicActiveSkill == null || actor.status.dynamicActiveSkill.Count == 0) continue;
            var skill = actor.status.dynamicActiveSkill[UnityEngine.Random.Range(0, actor.status.dynamicActiveSkill.Count)];
            if (skill == null || skill.levels == null || skill.levels.Length == 0) continue;

            int sIdx = Mathf.Clamp(actor.status.SkillLevelIndex, 0, skill.levels.Length - 1);
            var levelData = skill.levels[sIdx];

            BattleCharacter primaryTarget = (livingEnemies.Count > 0) ? livingEnemies[UnityEngine.Random.Range(0, livingEnemies.Count)] : actor;
            var targets = TargetResolver.Resolve(primaryTarget, levelData.targetType, playerParty, enemyParty);
            EffectEngine.ProcessSkill(actor, targets, levelData);
        }
    }

    private void ExecuteLustCharm(List<BattleCharacter> livingAllies, List<BattleCharacter> livingEnemies, bool greaterEvil, bool martyrVow)
    {
        List<BattleCharacter> charmedList = new List<BattleCharacter>();

        if (martyrVow)
        {
            charmedList.AddRange(livingAllies);
            charmedList.AddRange(livingEnemies);
        }
        else
        {
            int count = greaterEvil ? 2 : 1;
            List<BattleCharacter> aPool = new List<BattleCharacter>(livingAllies);
            for (int i = 0; i < count && aPool.Count > 0; i++)
            {
                var chosen = aPool[UnityEngine.Random.Range(0, aPool.Count)];
                charmedList.Add(chosen);
                aPool.Remove(chosen);
            }
            List<BattleCharacter> ePool = new List<BattleCharacter>(livingEnemies);
            for (int i = 0; i < count && ePool.Count > 0; i++)
            {
                var chosen = ePool[UnityEngine.Random.Range(0, ePool.Count)];
                charmedList.Add(chosen);
                ePool.Remove(chosen);
            }
        }

        _sinState.charmedCharacters = new List<BattleCharacter>(charmedList);

        foreach (var c in charmedList)
        {
            ExecuteCharmedAutoCast(c, livingAllies, livingEnemies);
        }

        NotificationManager.Instance?.ShowMessage($"[색욕의 죄] {charmedList.Count}명이 '매혹'되어 자동 행동합니다!", Color.magenta);
    }

    private void ExecuteCharmedAutoCast(BattleCharacter actor, List<BattleCharacter> allies, List<BattleCharacter> enemies)
    {
        if (actor == null || actor.status == null || actor.status.currentHp <= 0) return;

        List<BattleCharacter> allTargets = new List<BattleCharacter>();
        allTargets.AddRange(allies);
        allTargets.AddRange(enemies);
        BattleCharacter target = allTargets.Count > 0 ? allTargets[UnityEngine.Random.Range(0, allTargets.Count)] : actor;

        // 아군인 경우
        if (actor.status.dynamicActiveSkill != null && actor.status.dynamicActiveSkill.Count > 0)
        {
            var skill = actor.status.dynamicActiveSkill[UnityEngine.Random.Range(0, actor.status.dynamicActiveSkill.Count)];
            if (skill != null && skill.levels != null && skill.levels.Length > 0)
            {
                int sIdx = Mathf.Clamp(actor.status.SkillLevelIndex, 0, skill.levels.Length - 1);
                var levelData = skill.levels[sIdx];

                var resolved = TargetResolver.Resolve(target, levelData.targetType, playerParty, enemyParty);
                EffectEngine.ProcessSkill(actor, resolved, levelData);
            }
        }
        // 적군인 경우
        else if (actor.status.origin != null && actor.status.origin.enemyPatterns != null && actor.status.origin.enemyPatterns.Count > 0)
        {
            var pattern = actor.status.origin.enemyPatterns[UnityEngine.Random.Range(0, actor.status.origin.enemyPatterns.Count)];
            var resolved = TargetResolver.Resolve(target, pattern.targetType, playerParty, enemyParty);
            SkillLevelData runtimePattern = new SkillLevelData
            {
                overrideCost = -1,
                targetType = pattern.targetType,
                effects = pattern.effects
            };
            EffectEngine.ProcessSkill(actor, resolved, runtimePattern);
        }
    }

    private void ExecuteEnvyMartyrLifeDrain(List<BattleCharacter> livingAllies, List<BattleCharacter> livingEnemies)
    {
        if (livingAllies.Count == 0 || livingEnemies.Count == 0) return;

        BattleCharacter lowestAlly = livingAllies[0];
        foreach (var a in livingAllies) { if (a.status.currentHp < lowestAlly.status.currentHp) lowestAlly = a; }

        BattleCharacter highestEnemy = livingEnemies[0];
        foreach (var e in livingEnemies) { if (e.status.currentHp > highestEnemy.status.currentHp) highestEnemy = e; }

        float drainAmount = highestEnemy.status.FinalMaxHp * 0.12f;
        float actualDrain = highestEnemy.ReceiveDamage(drainAmount, lowestAlly, DamageType.True);
        lowestAlly.ReceiveHeal(actualDrain, lowestAlly);

        NotificationManager.Instance?.ShowMessage($"[순교자 질투] {lowestAlly.characterName}이(가) {highestEnemy.characterName}의 체력 {actualDrain:F0} 강탈!", Color.green);
    }

    private void ExecuteEnvyBuffCopy(List<BattleCharacter> livingAllies, List<BattleCharacter> livingEnemies, bool greaterEvil)
    {
        if (livingAllies.Count == 0 || livingEnemies.Count == 0) return;

        float totalBonusAtk = 0f;
        float totalShield = 0f;
        foreach (var e in livingEnemies)
        {
            totalBonusAtk += e.status.bonusAttack;
            var shieldEffect = e.status.activeStatusEffects.Find(eff => eff.effectType == EffectType.Shield);
            if (shieldEffect != null) totalShield += shieldEffect.damagePerTurn;
        }

        if (greaterEvil)
        {
            totalBonusAtk *= 2f;
            totalShield *= 2f;
        }

        if (totalBonusAtk > 0f || totalShield > 0f)
        {
            var chosenAlly = livingAllies[UnityEngine.Random.Range(0, livingAllies.Count)];
            if (totalBonusAtk > 0f) chosenAlly.status.bonusAttack += totalBonusAtk;
            if (totalShield > 0f) chosenAlly.status.ApplyStatusEffect(EffectType.Shield, totalShield, 1);
            if (chosenAlly.view != null) chosenAlly.view.UpdateVisual(chosenAlly.status);

            NotificationManager.Instance?.ShowMessage($"[질투의 죄] 적 버프 복사 -> {chosenAlly.characterName}에게 공격력 +{totalBonusAtk:F0}, 보호막 +{totalShield:F0} 부여!", Color.green);
        }
    }

    private void ApplyClusterSuitEffects(TheLastArk.Battle.ClusterHandResult result)
    {
        if (result == null || result.cards == null) return;
        if (!TheLastArk.Managers.TrainManager.IsInitialized || TheLastArk.Managers.TrainManager.Instance.nexusCar == null) return;

        int level = TheLastArk.Managers.TrainManager.Instance.nexusCar.level;

        var livingEnemies = new List<BattleCharacter>();
        if (enemyParty != null)
        {
            foreach (var e in enemyParty)
            {
                if (e != null && e.status != null && e.status.currentHp > 0)
                    livingEnemies.Add(e);
            }
        }

        var livingAllies = new List<BattleCharacter>();
        if (playerParty != null)
        {
            foreach (var a in playerParty)
            {
                if (a != null && a.status != null && a.status.currentHp > 0)
                    livingAllies.Add(a);
            }
        }

        foreach (var card in result.cards)
        {
            int activeNum = card.resolvedNumber;
            CardSuit activeSuit = card.resolvedSuit;
            int suitVal = TheLastArk.Battle.ClusterCardManager.CalculateSuitValue(activeNum, level);

            if (suitVal <= 0) continue;

            switch (activeSuit)
            {
                case CardSuit.Spade:
                    if (livingEnemies.Count > 0)
                    {
                        var targetEnemy = livingEnemies[UnityEngine.Random.Range(0, livingEnemies.Count)];
                        targetEnemy.ReceiveDamage(suitVal, null, DamageType.True);
                        Debug.Log($"[클러스터 ♠] {targetEnemy.characterName}에게 ♠{activeNum} 효과로 관통 피해 {suitVal} 입힘!");
                    }
                    break;

                case CardSuit.Heart:
                    if (livingAllies.Count > 0)
                    {
                        var targetAlly = livingAllies[UnityEngine.Random.Range(0, livingAllies.Count)];
                        targetAlly.status.ApplyStatusEffect(EffectType.Shield, suitVal, 1);
                        Debug.Log($"[클러스터 ♥] {targetAlly.characterName}에게 ♥{activeNum} 효과로 방어막 {suitVal} 부여!");
                    }
                    break;

                case CardSuit.Diamond:
                    if (livingAllies.Count > 0)
                    {
                        var targetAlly = livingAllies[UnityEngine.Random.Range(0, livingAllies.Count)];
                        targetAlly.status.ApplyStatusEffect(EffectType.Shield, suitVal, 2); // 보호: 2턴 유지 보호막
                        Debug.Log($"[클러스터 ♦] {targetAlly.characterName}에게 ♦{activeNum} 효과로 보호 {suitVal} 부여!");
                    }
                    break;
            }
        }

        if (!string.IsNullOrEmpty(result.summary))
        {
            NotificationManager.Instance?.ShowMessage($"[클러스터 덱 완성] {result.summary}", Color.cyan);
        }
    }

    private void ApplyPlayerTurnStartAP()
    {
        currentAP = MaxAP;

        // [에너지 저장소] 이월 행동력 추가
        if (_carriedOverAP > 0)
        {
            currentAP += _carriedOverAP;
            NotificationManager.Instance?.ShowMessage($"[에너지 저장소] 이월 행동력 +{_carriedOverAP} 추가!", Color.cyan);
            _carriedOverAP = 0;
        }

        // [과부하 모듈] 잠금 처리
        if (_nextTurnLockedAP > 0)
        {
            int locked = _nextTurnLockedAP;
            currentAP = Mathf.Max(0, currentAP - locked);
            NotificationManager.Instance?.ShowMessage($"[과부하 모듈] 이전 턴 과부하로 행동력 -{locked} 잠김! (이번 턴 과부하 불가)", Color.red);
            _nextTurnLockedAP = 0;
            _canOverloadThisTurn = false;
        }
        else
        {
            _canOverloadThisTurn = true;
        }
        _overloadedThisTurn = false;

        UpdateAPUI();
        CancelPendingSelection();

        EffectEngine.ResetTurnBurnCounts(_turnCount);

        // [빨간 구두] 유물: 턴 시작 시 출혈을 보유하지 않은 모든 적에게 출혈 2 부여
        if (TheLastArk.Managers.ResourceManager.Instance != null &&
            TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.RedShoes))
        {
            foreach (var enemy in enemyParty)
            {
                if (enemy != null && enemy.status != null && enemy.status.currentHp > 0)
                {
                    bool hasBleed = enemy.status.activeStatusEffects.Exists(e => e.effectType == EffectType.Bleed);
                    if (!hasBleed)
                    {
                        enemy.status.ApplyStatusEffect(EffectType.Bleed, 2f, 2);
                        Debug.Log($"[빨간 구두] {enemy.characterName}에게 출혈 2 부여!");
                    }
                }
            }
        }

        // [연합의 문장] 유물: 수호자, 전사, 지원가 아군이 잃은 정신력의 3%만큼 회복
        if (TheLastArk.Managers.ResourceManager.Instance != null &&
            TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.AllianceCrest))
        {
            foreach (var ally in playerParty)
            {
                if (ally != null && ally.status != null && ally.status.currentHp > 0)
                {
                    if (ally.status.HasSynergy(TheLastArk.Data.SynergyType.Guardian) ||
                        ally.status.HasSynergy(TheLastArk.Data.SynergyType.Warrior) ||
                        ally.status.HasSynergy(TheLastArk.Data.SynergyType.Support))
                    {
                        float lostMental = ally.status.FinalMaxMental - ally.status.currentMental;
                        if (lostMental > 0)
                        {
                            float recovery = Mathf.Max(1f, lostMental * 0.03f);
                            ally.ReceiveMentalHeal(recovery);
                            if (ally.view != null) ally.view.UpdateVisual(ally.status);
                            Debug.Log($"[연합의 문장] {ally.characterName} 잃은 정신력 3%({recovery:F1}) 회복!");
                        }
                    }
                }
            }
        }

        // [아르키움 유니온] 발명품 발동 및 [마도공학의 심장] 공격력/주문력 증가
        var curSyn = TheLastArk.Character.SynergyCalculator.CalculateActiveSynergies();
        if (curSyn.TryGetValue(TheLastArk.Data.SynergyType.ArchiumUnion, out int auCount) && auCount >= 2)
        {
            TriggerArchiumUnionInvention(auCount);
        }
    }

    private void TriggerArchiumUnionInvention(int auCount)
    {
        _archiumInventionStep++;
        int mod = _archiumInventionStep % 3;
        string inventionName = "";

        if (auCount >= 8 || (auCount >= 6 && mod == 0))
        {
            // 기계장치의 신 (모든 발명품 발동)
            inventionName = "기계장치의 신 (모든 발명품)";
            currentAP = Mathf.Min(MaxAP, currentAP + 1);
            UpdateAPUI();
            foreach (var e in enemyParty)
            {
                if (e != null && e.status != null && e.status.currentHp > 0)
                {
                    e.status.ApplyStatusEffect(EffectType.Burn, 1f, 2);
                    e.status.ApplyStatusEffect(EffectType.Bleed, 1f, 2);
                    e.status.ApplyStatusEffect(EffectType.Poison, 1f, 2);
                }
            }
        }
        else if (mod == 1)
        {
            // 다각도 프레임: 행동력 +1
            inventionName = "다각도 프레임 (행동력 +1)";
            currentAP = Mathf.Min(MaxAP, currentAP + 1);
            UpdateAPUI();
        }
        else if (mod == 2)
        {
            // 연장 총열: 아군 버프
            inventionName = "연장 총열 (화력 지원)";
        }
        else
        {
            // 유해 화학품: 모든 적 화상/출혈/독 1
            inventionName = "유해 화학품 (상태이상 부여)";
            foreach (var e in enemyParty)
            {
                if (e != null && e.status != null && e.status.currentHp > 0)
                {
                    e.status.ApplyStatusEffect(EffectType.Burn, 1f, 2);
                    e.status.ApplyStatusEffect(EffectType.Bleed, 1f, 2);
                    e.status.ApplyStatusEffect(EffectType.Poison, 1f, 2);
                }
            }
        }

        Debug.Log($"[아르키움 유니온] 발명품 발동: {inventionName}");

        // [마도공학의 심장] 이번 전투동안 마법공학 발명품이 발동할 때 마다 아군의 공격력, 주문력 1 증가
        if (TheLastArk.Managers.ResourceManager.Instance != null &&
            TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.HeartOfMagitech))
        {
            foreach (var ally in playerParty)
            {
                if (ally != null && ally.status != null && ally.status.currentHp > 0)
                {
                    ally.status.bonusAttack += 1f;
                    if (ally.view != null) ally.view.UpdateVisual(ally.status);
                }
            }
            NotificationManager.Instance?.ShowMessage($"[마도공학의 심장] 발명품 '{inventionName}' 발동! 전 아군 공/주 +1!", Color.cyan);
        }
        else
        {
            NotificationManager.Instance?.ShowMessage($"[아르키움 유니온] 발명품 '{inventionName}' 발동!", Color.cyan);
        }
    }

    private void TriggerBlueTowerEffect(BattleCharacter actor, int btCount)
    {
        List<BattleCharacter> aliveEnemies = new List<BattleCharacter>();
        foreach (var e in enemyParty)
        {
            if (e != null && e.status != null && e.status.currentHp > 0) aliveEnemies.Add(e);
        }

        if (aliveEnemies.Count > 0)
        {
            var target = aliveEnemies[Random.Range(0, aliveEnemies.Count)];
            target.status.ApplyStatusEffect(EffectType.Burn, 5f, 2);
            Debug.Log($"[푸른 마탑] {target.characterName}에게 화상 5 부여!");
        }

        if (btCount >= 4 && actor != null && actor.status != null)
        {
            actor.ReceiveMentalHeal(actor.status.FinalMaxMental * 0.05f);
            if (actor.view != null) actor.view.UpdateVisual(actor.status);
            Debug.Log($"[푸른 마탑] {actor.characterName} 정신력 5% 회복!");
        }

        NotificationManager.Instance?.ShowMessage("[푸른 마탑] 마법 효과 발동!", Color.blue);
    }

    private void TriggerCheongwoonEffect(int cwCount)
    {
        List<BattleCharacter> aliveEnemies = new List<BattleCharacter>();
        foreach (var e in enemyParty)
        {
            if (e != null && e.status != null && e.status.currentHp > 0) aliveEnemies.Add(e);
        }

        float multiplier = cwCount >= 7 ? 2.0f : 1.0f;

        if (aliveEnemies.Count > 0)
        {
            var target = aliveEnemies[Random.Range(0, aliveEnemies.Count)];
            float dmg = Mathf.Max(1f, target.status.FinalMaxHp * 0.04f * multiplier);
            target.ReceiveDamage(dmg, null, DamageType.True);
            Debug.Log($"[청운] {target.characterName}에게 고정 피해 {dmg:F1} 부여!");
        }

        if (cwCount >= 5)
        {
            List<BattleCharacter> aliveAllies = new List<BattleCharacter>();
            foreach (var a in playerParty)
            {
                if (a != null && a.status != null && a.status.currentHp > 0) aliveAllies.Add(a);
            }
            if (aliveAllies.Count > 0)
            {
                var ally = aliveAllies[Random.Range(0, aliveAllies.Count)];
                float heal = Mathf.Max(1f, ally.status.FinalMaxHp * 0.05f * multiplier);
                ally.ReceiveHeal(heal, null);
                ally.ReceiveMentalHeal(ally.status.FinalMaxMental * 0.05f * multiplier);
                if (ally.view != null) ally.view.UpdateVisual(ally.status);
            }
        }

        if (cwCount >= 7)
        {
            currentAP = Mathf.Min(MaxAP, currentAP + 1);
            UpdateAPUI();
        }

        NotificationManager.Instance?.ShowMessage("[청운] 청운 기운 발동!", Color.green);
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
        CancelPendingSelection();
        ClearEnemyTargetMarkers();
        bool playerWin = IsPartyWiped(enemyParty);
        Debug.Log($"[Battle] 전투 종료: {(playerWin ? "승리" : "패배")}");

        // [의무실] 전투 승리 시 체력/정신력 회복 및 소생 적용
        if (playerWin && TheLastArk.Managers.TrainManager.IsInitialized)
        {
            TheLastArk.Managers.TrainManager.Instance.ApplyInfirmaryPostBattleEffect(playerParty);
        }

        if (BattleResultUIManager.Instance != null)
        {
            if (playerWin)
            {
                BattleResultUIManager.Instance.ShowVictoryScreen(
                    RunManager.Instance.CurrentEncounterPool,
                    VictoryGold,
                    LoadMapScene);
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
