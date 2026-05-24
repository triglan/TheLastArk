/// <summary>
/// 전투 진행 단계를 나타냅니다.
/// BattleManager가 이 값을 기준으로 입력 허용 여부와 자동 진행을 결정합니다.
/// </summary>
public enum BattlePhase
{
    None,           // 초기화 전
    PlayerTurn,     // 플레이어 입력 대기
    EnemyTurn,      // 적 AI 행동
    StatusEffect,   // 출혈·독 등 상태이상 발동
    TurnEnd,        // 턴 수 증가, 사망 체크
    BattleEnd       // 승리 또는 패배
}
