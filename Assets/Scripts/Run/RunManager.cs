using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 런 전반 상태를 보관하고 씬 전환을 담당하는 싱글톤.
/// 맵·노드·턴 수는 기존과 동일하게 유지하고,
/// 런 내 자원·파티·유물은 RunState 객체로 위임합니다.
/// </summary>
public class RunManager : MonoBehaviour
{
    private const string CurrentRegionId = EnemyEncounterPool.DefaultRegionId;
    public const int MaxPartySize = 4;

    // ── 싱글톤 ────────────────────────────────────────────────────
    private static RunManager _instance;
    public static RunManager Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = FindObjectOfType<RunManager>();
            if (_instance != null) return _instance;

            var go = new GameObject("RunManager");
            _instance = go.AddComponent<RunManager>();
            DontDestroyOnLoad(go);
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    // ── 맵 / 진행 상태 ────────────────────────────────────────────
    public MapData CurrentMap  { get; set; }
    public MapNode CurrentNode { get; set; }
    public int CurrentTurn { get; set; }
    public EnemyEncounterData CurrentEncounter { get; private set; }
    public EnemyEncounterPool CurrentEncounterPool { get; private set; }

    [Header("Battle Encounter")]
    [SerializeField] private BattleEncounterTable encounterTable;

    public int GetPartyTotalSense()
    {
        return 10;
    }

    // ── 런 상태 (개선 #4) ─────────────────────────────────────────
    /// <summary>
    /// 현재 런의 자원·파티·유물·소모품 등 상태.
    /// 새 런 시작 시 State.Reset()을 호출하세요.
    /// </summary>
    public RunState State { get; private set; } = new RunState();

    // ── 씬 전환 ───────────────────────────────────────────────────
    public void GoToNodeScene(MapNode node, int turn)
    {
        CurrentNode = node;
        CurrentTurn = turn;

        switch (node.nodeType)
        {
            case NodeType.Combat:
            case NodeType.Elite:
            case NodeType.Boss:
                if (!TryPrepareBattleEncounter(node))
                {
                    Debug.LogError($"[RunManager] No encounter matched region={CurrentRegionId}, floor={node.floor}, nodeType={node.nodeType}, combatCount={State.combatCount}.");
                    return;
                }
                SceneManager.LoadScene("BattleScene");
                break;

            case NodeType.Event:
                // MapManager에서 EventManager를 통해 처리
                Debug.Log($"[RunManager] 이벤트 노드 — MapManager에서 팝업 처리");
                break;

            case NodeType.Rest:
                SceneManager.LoadScene("VillageScene");
                break;
        }
    }

    private bool TryPrepareBattleEncounter(MapNode node)
    {
        if (encounterTable == null)
            encounterTable = BattleEncounterTable.LoadDefault();

        EnemyEncounterData encounter = BattleEncounterResolver.Resolve(
            encounterTable,
            CurrentRegionId,
            node.nodeType,
            node.floor,
            State.combatCount,
            State.appearedEncounterIDs,
            out EnemyEncounterPool pool);

        if (encounter == null) return false;

        CurrentEncounter = encounter;
        CurrentEncounterPool = pool;
        if (!State.appearedEncounterIDs.Contains(encounter.EncounterId))
            State.appearedEncounterIDs.Add(encounter.EncounterId);
        State.combatCount++;

        Debug.Log($"[RunManager] Encounter selected: {encounter.DisplayName}, entryCount={State.combatCount}");
        return true;
    }

    // ── 파티 관리 ─────────────────────────────────────────────────
    public bool CanAddPartyMember(CharacterData characterData)
    {
        if (characterData == null) return false;
        string id = characterData.DataId;
        return !string.IsNullOrEmpty(id)
            && !State.partyDataIDs.Contains(id)
            && State.partyDataIDs.Count < MaxPartySize;
    }

    public void AddPartyMember(CharacterData characterData)
    {
        if (!CanAddPartyMember(characterData)) return;
        
        string id = characterData.DataId;
        State.partyDataIDs.Add(id);
        State.partyStatuses.Add(new CharacterStatus(characterData));
        SynchronizeLeaderWithPartyOrder();
    }

    public bool RemovePartyMember(string characterId)
    {
        if (string.IsNullOrEmpty(characterId)) return false;
        if (!State.partyDataIDs.Remove(characterId)) return false;

        State.partyStatuses.RemoveAll(status =>
            status != null && status.origin != null && status.origin.DataId == characterId);
        SynchronizeLeaderWithPartyOrder();
        return true;
    }

    public bool MovePartyMember(int fromIndex, int toIndex)
    {
        int partyCount = State.partyDataIDs.Count;
        if (fromIndex < 0 || fromIndex >= partyCount) return false;
        if (toIndex < 0 || toIndex >= partyCount) return false;
        if (fromIndex == toIndex) return false;

        string characterId = State.partyDataIDs[fromIndex];
        State.partyDataIDs.RemoveAt(fromIndex);
        State.partyDataIDs.Insert(toIndex, characterId);
        SynchronizeLeaderWithPartyOrder();
        return true;
    }

    public bool MovePartyMember(string characterId, int direction)
    {
        if (string.IsNullOrEmpty(characterId) || direction == 0) return false;

        int fromIndex = State.partyDataIDs.IndexOf(characterId);
        if (fromIndex < 0) return false;

        int toIndex = fromIndex + (direction < 0 ? -1 : 1);
        return MovePartyMember(fromIndex, toIndex);
    }

    public void SynchronizeLeaderWithPartyOrder()
    {
        State.leaderCharacterID = State.partyDataIDs.Count > 0
            ? State.partyDataIDs[0]
            : string.Empty;
    }

    // ── 편의 메서드 ───────────────────────────────────────────────
    /// <summary>새 런을 시작할 때 호출합니다.</summary>
    public void StartNewRun()
    {
        CurrentMap  = null;
        CurrentNode = null;
        CurrentTurn = 0;
        State.Reset();
        CurrentEncounter = null;
        CurrentEncounterPool = null;

        if (TheLastArk.Managers.ResourceManager.IsInitialized)
            TheLastArk.Managers.ResourceManager.Instance.NotifyGoldChanged();
    }
}
