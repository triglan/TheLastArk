using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI;
using UnityEngine;

/// <summary>
/// 턴제 전투의 상태 머신을 관리합니다.
///
/// 페이즈 흐름:
///   PlayerTurn → (턴 종료 버튼) → EnemyTurn → StatusEffect → TurnEnd → PlayerTurn ...
///
/// 외부 진입점:
///   - GameManager.BeginBattleSetup() → InitializeAP() 호출
///   - SkillSlotUI.OnClickSlot()      → SelectSkill() 호출
///   - 턴 종료 버튼                   → EndPlayerTurn() 호출
/// </summary>
public class BattleManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────
    [Header("Parties")]
    public List<BattleCharacter> playerParty;
    public List<BattleCharacter> enemyParty;

    [Header("References")]
    public TargetArrow targetHandler;

    [Header("AP")]
    public int maxAP = 10;
    public int currentAP;
    public TextMeshProUGUI apText;

    [Header("Turn UI")]
    [Tooltip("현재 턴 수를 표시하는 텍스트")]
    public TextMeshProUGUI turnCountText;
    [Tooltip("현재 페이즈(아군/적군)를 표시하는 텍스트")]
    public TextMeshProUGUI phaseText;
    [Tooltip("적 턴·상태이상 처리 사이의 대기 시간(초)")]
    public float enemyActionDelay = 0.8f;

    // ── 내부 상태 ─────────────────────────────────────────────────
    private BattlePhase _currentPhase = BattlePhase.None;
    private int _turnCount = 1;
    private readonly BattleSelectionState _selection = new BattleSelectionState();

    // ── 프로퍼티 ──────────────────────────────────────────────────
    /// <summary>현재 플레이어 입력을 받을 수 있는 상태인지</summary>
    public bool IsPlayerTurn => _currentPhase == BattlePhase.PlayerTurn;

    // ══════════════════════════════════════════════════════════════
    // 초기화
    // ══════════════════════════════════════════════════════════════

    /// <summary>GameManager.BeginBattleSetup()에서 호출합니다.</summary>
    public void InitializeAP()
    {
        currentAP = maxAP;
        UpdateAPUI();
        UpdateTurnUI();

        // EnemyAI에 아군 파티 주입
        foreach (var enemy in enemyParty)
        {
            var ai = enemy.GetComponent<EnemyAI>();
            if (ai != null) ai.targetParty = playerParty;
        }

        EnterPhase(BattlePhase.PlayerTurn);
    }

    // ══════════════════════════════════════════════════════════════
    // 상태 머신 — 페이즈 전환
    // ══════════════════════════════════════════════════════════════

    private void EnterPhase(BattlePhase next)
    {
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

    // ── PlayerTurn ────────────────────────────────────────────────

    private void OnEnterPlayerTurn()
    {
        currentAP = maxAP;
        UpdateAPUI();
        _selection.Clear();
        Debug.Log($"[Battle] === Turn {_turnCount} — 아군 턴 ===");
    }

    /// <summary>턴 종료 버튼 UI에서 호출합니다.</summary>
    public void EndPlayerTurn()
    {
        if (!IsPlayerTurn) return;
        EnterPhase(BattlePhase.EnemyTurn);
    }

    // ── EnemyTurn ─────────────────────────────────────────────────

    private IEnumerator RunEnemyTurn()
    {
        Debug.Log($"[Battle] === Turn {_turnCount} — 적군 턴 ===");

        foreach (var enemy in enemyParty)
        {
            if (enemy == null || enemy.status.currentHp <= 0) continue;

            var ai = enemy.GetComponent<EnemyAI>();
            if (ai != null) ai.ExecuteTurn();

            yield return new WaitForSeconds(enemyActionDelay);

            // 적 행동 후 즉시 아군 전멸 체크
            if (IsPartyWiped(playerParty))
            {
                EnterPhase(BattlePhase.BattleEnd);
                yield break;
            }
        }

        EnterPhase(BattlePhase.StatusEffect);
    }

    // ── StatusEffect ──────────────────────────────────────────────

    private IEnumerator RunStatusEffectPhase()
    {
        Debug.Log($"[Battle] === Turn {_turnCount} — 상태이상 페이즈 ===");

        var all = new List<BattleCharacter>(playerParty);
        all.AddRange(enemyParty);
        StatusEffectPhaseHandler.ProcessAll(all);

        yield return new WaitForSeconds(0.3f);

        EnterPhase(BattlePhase.TurnEnd);
    }

    // ── TurnEnd ───────────────────────────────────────────────────

    private void OnEnterTurnEnd()
    {
        // 사망한 캐릭터 처리 (뷰 갱신 등은 BattleCharacter 쪽에서 담당 예정)
        RemoveDeadCharacters();

        // 승패 판정
        if (IsPartyWiped(playerParty))  { EnterPhase(BattlePhase.BattleEnd); return; }
        if (IsPartyWiped(enemyParty))   { EnterPhase(BattlePhase.BattleEnd); return; }

        _turnCount++;
        UpdateTurnUI();
        Debug.Log($"[Battle] 턴 종료 → {_turnCount}턴으로");

        EnterPhase(BattlePhase.PlayerTurn);
    }

    // ── BattleEnd ─────────────────────────────────────────────────

    private void Awake()
    {
        // 씬 내에 BattleResultUIManager가 없다면 안전하게 자동 추가하여 에러 방지
        if (FindObjectOfType<BattleResultUIManager>() == null)
        {
            gameObject.AddComponent<BattleResultUIManager>();
        }
    }

    private void OnEnterBattleEnd()
    {
        bool playerWin = IsPartyWiped(enemyParty);
        Debug.Log($"[Battle] 전투 종료 — {(playerWin ? "승리" : "패배")}");

        if (BattleResultUIManager.Instance != null)
        {
            if (playerWin)
            {
                // 승리 시: 기획 수치에 따른 골드 100, 경험치 50 연출 보상과 함께 맵 씬 로드 콜백 전달
                BattleResultUIManager.Instance.ShowVictoryScreen(100, 50, LoadMapScene);
            }
            else
            {
                // 패배 시: 게임 오버 팝업 및 퇴각 로드 콜백 전달
                BattleResultUIManager.Instance.ShowDefeatScreen(LoadMapScene);
            }
        }
        else
        {
            // 혹시라도 매니저가 유실되었을 경우의 예외 복구(Fallback) 처리
            NotificationManager.Instance.ShowMessage(
                playerWin ? "승리!" : "패배...",
                playerWin ? UnityEngine.Color.cyan : UnityEngine.Color.red);
            Invoke(nameof(LoadMapScene), 2f);
        }
    }

    private void LoadMapScene()
    {
        Debug.Log("[Battle] 맵 씬(MapScene)으로 이동합니다.");
        UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene");
    }

    // ══════════════════════════════════════════════════════════════
    // 스킬 선택 & 실행 (PlayerTurn에서만 동작)
    // ══════════════════════════════════════════════════════════════

    /// <summary>SkillSlotUI.OnClickSlot()에서 호출합니다.</summary>
    public void SelectSkill(SkillInfo skill, BattleCharacter actor)
    {
        if (!IsPlayerTurn) return;

        _selection.Set(skill, actor);

        if (targetHandler.target != null)
            PerformSkill();
        else
            NotificationManager.Instance.ShowMessage("타겟을 선택하세요!", UnityEngine.Color.yellow);
    }

    /// <summary>타겟 선택 완료 후 실행됩니다.</summary>
    public void PerformSkill()
    {
        if (!IsPlayerTurn) return;
        if (!_selection.IsReady || targetHandler.target == null) return;

        var primaryTarget = targetHandler.target.GetComponent<BattleCharacter>();
        if (primaryTarget == null) return;

        var skill    = _selection.Skill;
        var actor    = _selection.Actor;
        int skillIdx = actor.status.SkillLevelIndex;
        var levelData = skill.levels[Mathf.Clamp(skillIdx, 0, skill.levels.Length - 1)];

        if (!TargetResolver.IsValid(primaryTarget, levelData.targetType, playerParty, enemyParty))
        {
            NotificationManager.Instance.ShowMessage(
                $"[타겟 오류] {skill.skillName}은(는) 해당 대상에 사용 불가.", UnityEngine.Color.red);
            _selection.Clear();
            return;
        }

        int cost = (levelData.overrideCost != -1) ? levelData.overrideCost : skill.baseCost;
        if (currentAP < cost)
        {
            NotificationManager.Instance.ShowMessage("AP가 부족합니다.", UnityEngine.Color.red);
            return;
        }

        currentAP -= cost;
        UpdateAPUI();

        var targets = TargetResolver.Resolve(primaryTarget, levelData.targetType, playerParty, enemyParty);
        EffectEngine.ProcessSkill(actor, targets, levelData);

        _selection.Clear();

        // AP 소진 시 자동으로 턴 종료
        if (currentAP <= 0) EndPlayerTurn();
    }

    // ══════════════════════════════════════════════════════════════
    // 헬퍼
    // ══════════════════════════════════════════════════════════════

    private bool IsPartyWiped(List<BattleCharacter> party)
        => party.TrueForAll(c => c == null || c.status.currentHp <= 0);

    private void RemoveDeadCharacters()
    {
        // 현재는 로그만 출력. 향후 사망 애니메이션·오브젝트 비활성화 연결 가능.
        foreach (var c in playerParty)
            if (c != null && c.status.currentHp <= 0)
                Debug.Log($"[Battle] {c.characterName} 사망");
        foreach (var c in enemyParty)
            if (c != null && c.status.currentHp <= 0)   
                Debug.Log($"[Battle] {c.characterName} 사망");
    }

    // ── UI 갱신 ──────────────────────────────────────────────────

    public void UpdateAPUI()
    {
        if (apText != null) apText.text = $"AP: {currentAP} / {maxAP}";
    }

    private void UpdatePhaseUI()
    {
        if (phaseText == null) return;
        phaseText.text = _currentPhase switch
        {
            BattlePhase.PlayerTurn   => "아군 턴",
            BattlePhase.EnemyTurn    => "적군 턴",
            BattlePhase.StatusEffect => "상태이상",
            BattlePhase.TurnEnd      => "턴 종료",
            BattlePhase.BattleEnd    => "전투 종료",
            _                        => "-------"
        };
    }

    private void UpdateTurnUI()
    {
        if (turnCountText != null) turnCountText.text = $"Turn {_turnCount}";
    }
}
