using UnityEngine;

[CreateAssetMenu(fileName = "BattleConfig", menuName = "TheLastArk/Battle/Battle Config")]
public class BattleConfig : ScriptableObject
{
    // BattleConfig가 할당되지 않았을 때 BattleManager가 사용하는 안전 기본값입니다.
    public const int DefaultMaxAP = 10;
    public const float DefaultEnemyActionDelay = 0.4f;
    public const float DefaultStatusEffectDelay = 0.3f;
    public const int DefaultVictoryGold = 100;
    public const int DefaultVictoryExp = 50;
    public const string DefaultMapSceneName = "MapScene";

    [Header("Action Point")]
    [Tooltip("플레이어 턴이 시작될 때 회복되는 최대 행동력입니다.")]
    [SerializeField, Min(1)] private int maxAP = DefaultMaxAP;

    [Header("Timing")]
    [Tooltip("적 한 명이 행동한 뒤 다음 적 행동으로 넘어가기 전 대기 시간입니다.")]
    [SerializeField, Min(0f)] private float enemyActionDelay = DefaultEnemyActionDelay;
    [Tooltip("상태이상 효과를 모두 처리한 뒤 턴 종료 단계로 넘어가기 전 대기 시간입니다.")]
    [SerializeField, Min(0f)] private float statusEffectDelay = DefaultStatusEffectDelay;

    [Header("Reward")]
    [Tooltip("전투 승리 시 지급할 골드입니다.")]
    [SerializeField, Min(0)] private int victoryGold = DefaultVictoryGold;
    [Tooltip("전투 승리 시 지급할 경험치입니다.")]
    [SerializeField, Min(0)] private int victoryExp = DefaultVictoryExp;

    [Header("Scene")]
    [Tooltip("전투 종료 후 돌아갈 씬 이름입니다. 비워두면 기본값 MapScene을 사용합니다.")]
    [SerializeField] private string mapSceneName = DefaultMapSceneName;

    // 외부에서는 설정값을 읽기만 하게 해서 런타임 중 의도치 않은 변경을 막습니다.
    public int MaxAP => maxAP;
    public float EnemyActionDelay => enemyActionDelay;
    public float StatusEffectDelay => statusEffectDelay;
    public int VictoryGold => victoryGold;
    public int VictoryExp => victoryExp;
    public string MapSceneName => string.IsNullOrWhiteSpace(mapSceneName) ? DefaultMapSceneName : mapSceneName;

    private void OnValidate()
    {
        // Inspector에서 잘못된 값이 들어와도 전투 흐름이 깨지지 않도록 최소값을 보정합니다.
        maxAP = Mathf.Max(1, maxAP);
        enemyActionDelay = Mathf.Max(0f, enemyActionDelay);
        statusEffectDelay = Mathf.Max(0f, statusEffectDelay);
        victoryGold = Mathf.Max(0, victoryGold);
        victoryExp = Mathf.Max(0, victoryExp);
    }
}
