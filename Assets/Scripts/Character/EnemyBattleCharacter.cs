using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(BattleCharacter))]
[RequireComponent(typeof(EnemyAI))]
public class EnemyBattleCharacter : MonoBehaviour
{
    [Header("적 캐릭터 데이터")]
    public CharacterData enemyData;

    [Header("초기화 옵션")]
    public bool initializeOnStart;

    private BattleCharacter battleCharacter;
    private bool _initialized;

    public BattleCharacter BattleCharacterComponent
    {
        get
        {
            if (battleCharacter == null) CacheComponents();
            return battleCharacter;
        }
    }

    public CharacterStatus CurrentStatus => BattleCharacterComponent != null ? BattleCharacterComponent.status : null;
    public bool HasRuntimeStatus => CurrentStatus != null && CurrentStatus.origin != null;
    public float CurrentHp => HasRuntimeStatus ? CurrentStatus.currentHp : (enemyData != null ? enemyData.maxHp : 0f);
    public float MaxHp => HasRuntimeStatus ? CurrentStatus.FinalMaxHp : (enemyData != null ? enemyData.maxHp : 0f);
    public float CurrentMental => HasRuntimeStatus ? CurrentStatus.currentMental : (enemyData != null ? enemyData.maxMental : 0f);
    public float MaxMental => HasRuntimeStatus ? CurrentStatus.FinalMaxMental : (enemyData != null ? enemyData.maxMental : 0f);
    public float BaseAttack => enemyData != null ? enemyData.baseAttack : 0f;
    public float BonusAttack => HasRuntimeStatus ? CurrentStatus.bonusAttack : 0f;
    public float TotalAttack => HasRuntimeStatus ? CurrentStatus.FinalAttack : BaseAttack;
    public List<EnemyPatternData> Patterns => enemyData != null ? enemyData.enemyPatterns : CurrentStatus?.origin?.enemyPatterns;

    private void Reset()
    {
        CacheComponents();
        ApplyDataReference();
    }

    private void OnValidate()
    {
        CacheComponents();
        ApplyDataReference();
    }

    private void Awake()
    {
        CacheComponents();
        ApplyDataReference();
    }

    private void Start()
    {
        if (initializeOnStart && !_initialized)
            InitializeForBattle();
    }

    [ContextMenu("적 데이터 적용")]
    public void ApplyDataReference()
    {
        // 인스펙터에 넣은 적 데이터를 BattleCharacter가 쓰는 데이터 칸에 연결합니다.
        if (battleCharacter == null) CacheComponents();
        if (battleCharacter == null || enemyData == null) return;

        if (!enemyData.isEnemy)
            Debug.LogWarning($"[EnemyBattleCharacter] {enemyData.characterName} 데이터는 적 데이터가 아닙니다.", this);

        battleCharacter.testData = enemyData;
        battleCharacter.isLeader = false;
    }

    [ContextMenu("전투 데이터 초기화")]
    public void InitializeForBattle()
    {
        if (_initialized) return;

        // 전투 시작 전에 적 데이터를 즉시 CharacterStatus로 만듭니다.
        ApplyDataReference();
        if (battleCharacter == null || enemyData == null)
        {
            Debug.LogWarning("[EnemyBattleCharacter] 적 캐릭터 데이터가 비어 있습니다.", this);
            return;
        }

        battleCharacter.Init(enemyData, false);
        _initialized = true;

        Debug.Log($"[EnemyBattleCharacter] {enemyData.characterName} 적 전투 데이터 초기화 완료", this);
    }

    private void CacheComponents()
    {
        battleCharacter = GetComponent<BattleCharacter>();
    }
}
