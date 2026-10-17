using System.Collections;
using UnityEngine;
using TheLastArk.UI;
using TheLastArk.Battle;

public partial class BattleManager
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public bool DebugHoldsBattleOpen { get; private set; }
    public bool DebugActionRunning { get; private set; }
    public bool DebugPanicPresenting => _panicPresentation != null && _panicPresentation.IsPresenting;

    public bool DebugApplyPanic(BattleCharacter ally, PanicType type)
    {
        if (type < PanicType.SelfLoathing || type > PanicType.Compulsion) return false;
        return DebugSetPanicState(ally, type);
    }

    public bool DebugRollPanic(BattleCharacter ally)
    {
        if (!CanDebugPanic(ally)) return false;
        return DebugSetPanicState(ally, (PanicType)Random.Range(1, 7));
    }

    public bool DebugClearPanic(BattleCharacter ally) => DebugSetPanicState(ally, PanicType.None);

    private bool CanDebugPanic(BattleCharacter ally) => IsPlayerTurn && ally != null
        && playerParty != null && playerParty.Contains(ally) && ally.gameObject.activeInHierarchy
        && ally.status?.origin != null && !ally.status.origin.isEnemy && ally.status.currentHp > 0f;

    private bool DebugSetPanicState(BattleCharacter ally, PanicType type)
    {
        if (!CanDebugPanic(ally)) return false;
        CancelPendingSelection();
        targetHandler?.Deselect(false);
        if (!ally.DebugSetPanic(type)) return false;
        RefreshSkillSlots();
        RefreshDiceUI();
        UpdateTurnEndButtonState();
        if (Application.isPlaying && DebugPanicPresenting) StartCoroutine(DebugRefreshAfterPanicPresentation());
        return true;
    }

    private IEnumerator DebugRefreshAfterPanicPresentation()
    {
        while (DebugPanicPresenting) yield return null;
        RefreshSkillSlots();
        RefreshDiceUI();
        UpdateTurnEndButtonState();
    }

    public void DebugSetPracticeMode(bool enabled)
    {
        if (!IsPlayerTurn) return;
        DebugHoldsBattleOpen = enabled;
        if (!enabled) TryEndBattleIfPartyWiped();
    }

    public void DebugRefreshCombatants()
    {
        CancelPendingSelection();
        targetHandler?.Deselect(false);
        ClearEnemyTargetMarkers();
        // Keep already telegraphed random targets stable when another slot changes.
        foreach (var enemy in enemyParty)
        {
            if (enemy == null || enemy.status == null) continue;
            var ui = enemy.GetComponent<EnemyIntentUI>();
            if (!enemy.gameObject.activeInHierarchy || enemy.status.currentHp <= 0f)
            {
                ui?.Hide();
                continue;
            }
            var ai = enemy.GetComponent<EnemyAI>();
            if (ai == null) continue;
            ai.allyParty = enemyParty;
            ai.targetParty = playerParty;
            if (ui == null) ui = enemy.gameObject.AddComponent<EnemyIntentUI>();
            if (enemy.GetComponent<EnemyEnhancementUI>() == null)
                enemy.gameObject.AddComponent<EnemyEnhancementUI>();
            if (ai.HasPreparedTurn || ai.PrepareTurn())
            {
                ui.Show(ai.PreparedIntent);
                SetEnemyTargetMarkers(ai.PreparedTargets, true);
            }
            else ui.Hide();
        }
    }

    public bool DebugExecutePattern(BattleCharacter enemy)
    {
        if (!IsPlayerTurn || enemy == null || !enemyParty.Contains(enemy)
            || !enemy.gameObject.activeInHierarchy || enemy.status == null || enemy.status.currentHp <= 0f)
            return false;
        var ai = enemy.GetComponent<EnemyAI>();
        if (ai == null || (!ai.HasPreparedTurn && !ai.PrepareTurn())) return false;
        StartCoroutine(DebugRunPattern(enemy, ai));
        return true;
    }

    private IEnumerator DebugRunPattern(BattleCharacter enemy, EnemyAI ai)
    {
        DebugActionRunning = true;
        CancelPendingSelection();
        targetHandler?.Deselect(false);
        ClearEnemyTargetMarkers();
        UpdateTurnEndButtonState();
        try
        {
            SetEnemyTargetMarkers(ai.PreparedTargets, true);
            yield return new WaitForSeconds(EnemyActionDelay);
            if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.status.currentHp > 0f)
                ai.ExecuteTurn();
        }
        finally
        {
            DebugActionRunning = false;
            ClearEnemyTargetMarkers();
            UpdateTurnEndButtonState();
        }
        if (!TryEndBattleIfPartyWiped()) DebugRefreshCombatants();
    }

    public void DebugCheckBattleEnd() => TryEndBattleIfPartyWiped();
#else
    public bool DebugHoldsBattleOpen => false;
    public bool DebugActionRunning => false;
#endif
}
