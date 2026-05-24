using System.Collections.Generic;

/// <summary>
/// 한 번의 런(플레이스루) 동안 유지되는 모든 상태를 담는 순수 C# 클래스.
/// RunManager가 직접 필드를 노출하는 대신 이 객체 하나를 들고 있으므로
/// 저장/불러오기, 디버그 덤프, 유닛 테스트가 쉬워집니다.
/// </summary>
[System.Serializable]
public class RunState
{
    // ── 자원 ──────────────────────────────────────────────────────
    public int gold = 0;

    // ── 파티 ──────────────────────────────────────────────────────
    /// <summary>현재 파티 캐릭터 원본 데이터 ID 목록 (ScriptableObject 이름 등)</summary>
    public List<string> partyDataIDs = new List<string>();

    // ── 유물·소모품 ───────────────────────────────────────────────
    public List<string> relicIDs      = new List<string>();
    public List<string> consumableIDs = new List<string>();

    // ── 전투 버프 ─────────────────────────────────────────────────
    /// <summary>다음 N번 전투에 적용되는 업그레이드 스택 수 (이벤트 보상 등)</summary>
    public int upgradeNextBattlesCount = 0;

    // ── 초기화 ────────────────────────────────────────────────────
    public void Reset()
    {
        gold                    = 0;
        partyDataIDs            = new List<string>();
        relicIDs                = new List<string>();
        consumableIDs           = new List<string>();
        upgradeNextBattlesCount = 0;
    }
}
