using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 맵 등 전역 게임 진행 데이터를 유지하는 싱글톤 매니저입니다.
/// </summary>
public class RunManager : MonoBehaviour
{
    private static RunManager instance;

    public static RunManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<RunManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("RunManager");
                    instance = go.AddComponent<RunManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    public MapData CurrentMap { get; set; }
    public MapNode CurrentNode { get; set; }
    public int CurrentTurn { get; set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void GoToNodeScene(MapNode node, int turn)
    {
        CurrentNode = node;
        CurrentTurn = turn;

        switch (node.nodeType)
        {
            case NodeType.Combat:
            case NodeType.Elite:
            case NodeType.Boss:
                Debug.Log($"[RunManager] 전투 씬(BattleScene)으로 이동: {node.nodeType}");
                SceneManager.LoadScene("BattleScene");
                break;

            case NodeType.Event:
                Debug.Log($"[RunManager] 이벤트 씬(EventScene, 미구현)으로 이동 - 임시로 유지");
                break;

            case NodeType.Rest:
                Debug.Log($"[RunManager] 마을 씬(RestScene, 미구현)으로 이동 - 임시로 유지");
                break;
        }
    }
}