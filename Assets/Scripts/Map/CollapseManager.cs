using System;
using UnityEngine;

/// <summary>
/// 턴 카운팅과 층 붕괴(추격 시스템)를 관리합니다.
/// 3턴마다 가장 낮은 층부터 순서대로 무너지며,
/// 무너지기 1턴 전에 경고를 발생시킵니다.
/// </summary>
[System.Serializable]
public class CollapseManager
{
    // ─────────────────────────────────────────────
    // 설정값
    // ─────────────────────────────────────────────

    /// <summary>몇 턴마다 붕괴가 발생하는지</summary>
    public const int TURNS_PER_COLLAPSE = 3;

    // ─────────────────────────────────────────────
    // 상태
    // ─────────────────────────────────────────────

    /// <summary>총 이동(행동) 횟수</summary>
    public int turnCount;

    /// <summary>현재까지 무너진 최고 층 (0이면 아직 무너진 층 없음)</summary>
    public int collapsedFloor;

    /// <summary>다음 턴에 붕괴 경고 중인지 여부</summary>
    public bool isWarning;

    /// <summary>붕괴 경고 이벤트가 발생할 다음 층</summary>
    public int warningFloor;

    // ─────────────────────────────────────────────
    // 이벤트 (UI 연동용)
    // ─────────────────────────────────────────────

    /// <summary>경고 발생 시 호출 (경고 대상 층 번호 전달)</summary>
    public event Action<int> OnCollapseWarning;

    /// <summary>붕괴 발생 시 호출 (붕괴된 층 번호 전달)</summary>
    public event Action<int> OnFloorCollapsed;

    /// <summary>게임 오버 시 호출</summary>
    public event Action OnGameOver;

    /// <summary>턴 카운트 변경 시 호출 (현재 턴, 다음 붕괴까지 남은 턴)</summary>
    public event Action<int, int> OnTurnChanged;

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────

    public CollapseManager()
    {
        turnCount = 0;
        collapsedFloor = 0;
        isWarning = false;
        warningFloor = 0;
    }

    // ─────────────────────────────────────────────
    // 핵심 로직
    // ─────────────────────────────────────────────

    /// <summary>
    /// 플레이어가 이동을 완료한 후 호출합니다.
    /// 턴을 증가시키고, 붕괴/경고 판정을 수행합니다.
    /// </summary>
    /// <param name="mapData">현재 맵 데이터</param>
    /// <returns>이번 턴의 결과</returns>
    public CollapseResult ProcessTurn(MapData mapData)
    {
        turnCount++;

        int turnsUntilCollapse = GetTurnsUntilNextCollapse();

        // 턴 변경 이벤트 발화
        OnTurnChanged?.Invoke(turnCount, turnsUntilCollapse);

        Debug.Log($"[CollapseManager] Turn {turnCount} | 다음 붕괴까지 {turnsUntilCollapse}턴 | 현재 붕괴층: {collapsedFloor}");

        // 붕괴 판정 (3의 배수 턴)
        if (turnCount % TURNS_PER_COLLAPSE == 0)
        {
            return ExecuteCollapse(mapData);
        }

        // 경고 판정 (붕괴 1턴 전 = 3의 배수 - 1)
        if (turnCount % TURNS_PER_COLLAPSE == TURNS_PER_COLLAPSE - 1)
        {
            return ExecuteWarning();
        }

        // 일반 턴
        isWarning = false;
        return CollapseResult.Normal;
    }

    /// <summary>
    /// 붕괴를 실행합니다.
    /// </summary>
    private CollapseResult ExecuteCollapse(MapData mapData)
    {
        collapsedFloor++;
        warningFloor = 0;
        isWarning = false;

        Debug.Log($"[CollapseManager] 💥 {collapsedFloor}층 붕괴!");

        // 해당 층의 모든 노드를 붕괴 처리
        mapData.CollapseFloor(collapsedFloor);

        // 붕괴 이벤트 발화
        OnFloorCollapsed?.Invoke(collapsedFloor);

        // 게임 오버 판정: 플레이어가 무너진 층에 있는 경우
        if (mapData.currentNode != null && mapData.currentNode.floor <= collapsedFloor)
        {
            Debug.Log($"[CollapseManager] 💀 게임 오버! 플레이어가 {mapData.currentNode.floor}층에 있는데 {collapsedFloor}층까지 붕괴됨.");
            OnGameOver?.Invoke();
            return CollapseResult.GameOver;
        }

        return CollapseResult.Collapsed;
    }

    /// <summary>
    /// 붕괴 경고를 발생시킵니다.
    /// </summary>
    private CollapseResult ExecuteWarning()
    {
        warningFloor = collapsedFloor + 1;
        isWarning = true;

        Debug.Log($"[CollapseManager] ⚠️ 경고! 다음 턴에 {warningFloor}층이 붕괴됩니다!");

        // 경고 이벤트 발화
        OnCollapseWarning?.Invoke(warningFloor);

        return CollapseResult.Warning;
    }

    // ─────────────────────────────────────────────
    // 조회 메서드
    // ─────────────────────────────────────────────

    /// <summary>
    /// 다음 붕괴까지 남은 턴 수를 반환합니다.
    /// </summary>
    public int GetTurnsUntilNextCollapse()
    {
        int remaining = TURNS_PER_COLLAPSE - (turnCount % TURNS_PER_COLLAPSE);
        return remaining == TURNS_PER_COLLAPSE ? TURNS_PER_COLLAPSE : remaining;
    }

    /// <summary>
    /// 특정 층이 현재 붕괴 상태인지 확인합니다.
    /// </summary>
    public bool IsFloorCollapsed(int floor)
    {
        return floor <= collapsedFloor;
    }

    /// <summary>
    /// 특정 층이 다음 턴에 붕괴될 예정인지 확인합니다.
    /// </summary>
    public bool IsFloorWarned(int floor)
    {
        return isWarning && floor == warningFloor;
    }

    /// <summary>
    /// 특정 노드로의 이동이 안전한지 확인합니다.
    /// 붕괴된 층이거나 경고 중인 층이면 false를 반환합니다.
    /// </summary>
    public MoveValidation ValidateMove(MapNode targetNode)
    {
        if (targetNode == null)
            return MoveValidation.Invalid;

        if (targetNode.isCollapsed)
            return MoveValidation.Blocked_Collapsed;

        if (IsFloorWarned(targetNode.floor))
            return MoveValidation.Risky_WillCollapse;

        return MoveValidation.Safe;
    }

    /// <summary>
    /// 현재 상태를 Debug.Log로 출력합니다.
    /// </summary>
    public void DebugPrint()
    {
        Debug.Log($"[CollapseManager] Turn:{turnCount} | Collapsed:{collapsedFloor}층 | Warning:{isWarning} (Floor:{warningFloor}) | 다음 붕괴까지: {GetTurnsUntilNextCollapse()}턴");
    }
}

/// <summary>
/// 턴 처리 결과를 나타내는 Enum.
/// </summary>
public enum CollapseResult
{
    /// <summary>일반 턴 - 아무 일도 안 일어남</summary>
    Normal,

    /// <summary>다음 턴에 붕괴 경고 발생</summary>
    Warning,

    /// <summary>층이 붕괴됨</summary>
    Collapsed,

    /// <summary>플레이어가 붕괴된 층에 있어서 게임 오버</summary>
    GameOver
}

/// <summary>
/// 이동 검증 결과를 나타내는 Enum.
/// </summary>
public enum MoveValidation
{
    /// <summary>안전한 이동</summary>
    Safe,

    /// <summary>위험 - 다음 턴에 해당 층이 붕괴 예정</summary>
    Risky_WillCollapse,

    /// <summary>불가 - 이미 붕괴된 층</summary>
    Blocked_Collapsed,

    /// <summary>불가 - 유효하지 않은 대상</summary>
    Invalid
}