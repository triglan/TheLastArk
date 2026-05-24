using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 런 전반 상태를 보관하고 씬 전환을 담당하는 싱글톤.
/// 맵·노드·턴 수는 기존과 동일하게 유지하고,
/// 런 내 자원·파티·유물은 RunState 객체로 위임합니다.
/// </summary>
public class RunManager : MonoBehaviour
{
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
    public int     CurrentTurn { get; set; }

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
                SceneManager.LoadScene("BattleScene");
                break;

            case NodeType.Event:
                // MapManager에서 EventManager를 통해 처리
                Debug.Log($"[RunManager] 이벤트 노드 — MapManager에서 팝업 처리");
                break;

            case NodeType.Rest:
                Debug.Log($"[RunManager] 휴식 노드 — RestScene 미구현");
                break;
        }
    }

    // ── 편의 메서드 ───────────────────────────────────────────────
    /// <summary>새 런을 시작할 때 호출합니다.</summary>
    public void StartNewRun()
    {
        CurrentMap  = null;
        CurrentNode = null;
        CurrentTurn = 0;
        State.Reset();
    }
}
