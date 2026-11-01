using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TheLastArk.UI;

// Runs before the EventSystem so the end-turn key can clear the UI selection before Space submits it.
[DefaultExecutionOrder(-1100)]
public class TargetArrow : MonoBehaviour
{
    public System.Action<GameObject> TargetSelected;
    public System.Action TargetCanceled;
    public RectTransform arrowUI;
    public GameObject target;

    [Header("Animation Settings")]
    public float floatSpeed = 5f;     // 움직이는 속도
    public float floatAmplitude = 10f; // 움직이는 높이 폭

    [Header("Target Hotkeys")]
    [Tooltip("적 1~4 (화면 왼쪽부터)")]
    [SerializeField] private KeyCode[] enemyTargetKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 };
    [Tooltip("아군 1~4 (파티 순서)")]
    [SerializeField] private KeyCode[] allyTargetKeys = { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R };
    [Tooltip("턴 종료")]
    [SerializeField] private KeyCode endTurnKey = KeyCode.Space;

    private Transform _cachedTargetPoint; // 현재 타겟의 화살표 위치 오브젝트
    private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
    private readonly List<BattleCharacter> _hotkeyEnemies = new List<BattleCharacter>();

    void Update()
    {
        HandleMouseClick();
        HandleTargetHotkeys();
        if (target != null && arrowUI != null) AnimateArrow();
    }

    // 단축키 타게팅: 해당 유닛을 클릭한 것과 같습니다. 쓰러진 유닛은 무시합니다.
    private void HandleTargetHotkeys()
    {
        if (!Input.anyKeyDown || IsTypingInField()) return;
        BattleManager battle = BattleManager.Active;
        if (battle == null || !battle.IsBattleOngoing) return;
        if (Input.GetKeyDown(endTurnKey))
        {
            // Space is also uGUI Submit: drop the focused button so the key never clicks it.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            // Same as clicking the end-turn button, which is only interactable on the player turn.
            var button = battle.turnEndButton;
            if (button != null && button.IsActive() && button.interactable && !battle.IsDiceDialogOpen)
                button.onClick.Invoke();
            return;
        }
        for (int i = 0; i < enemyTargetKeys.Length; i++)
        {
            if (!Input.GetKeyDown(enemyTargetKeys[i])) continue;
            _hotkeyEnemies.Clear();
            foreach (BattleCharacter enemy in battle.enemyParty)
                if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.status != null && enemy.status.currentHp > 0f)
                    _hotkeyEnemies.Add(enemy);
            _hotkeyEnemies.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            SelectHotkeyTarget(_hotkeyEnemies, i);
            return;
        }
        for (int i = 0; i < allyTargetKeys.Length; i++)
        {
            if (!Input.GetKeyDown(allyTargetKeys[i])) continue;
            SelectHotkeyTarget(battle.playerParty, i);
            return;
        }
    }

    private void SelectHotkeyTarget(List<BattleCharacter> candidates, int index)
    {
        if (candidates == null || index >= candidates.Count) return;
        BattleCharacter character = candidates[index];
        if (character == null || character.status == null || character.status.currentHp <= 0f) return;
        var enemy = character.GetComponent<EnemyBattleCharacter>();
        if (enemy != null && !enemy.HasRuntimeStatus) return;
        SelectTarget(character.gameObject);
    }

    private static bool IsTypingInField()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected != null && (selected.GetComponent<TMPro.TMP_InputField>() != null
            || selected.GetComponent<UnityEngine.UI.InputField>() != null);
    }

    private void HandleMouseClick()// 마우스 클릭 시 타겟 변경
    {
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Deselect();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (TrySelectUIAt(Input.mousePosition)) return;
            if (Camera.main == null) return;
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null && (hit.collider.name.Contains("Player") || hit.collider.name.Contains("Enemy")))
            {
                SelectTarget(hit.collider.gameObject);
            }
        }
    }

    private void SelectTarget(GameObject selected)
    {
        target = selected;
        if (arrowUI != null) arrowUI.gameObject.SetActive(true);
        _cachedTargetPoint = target.transform.Find("TargetPoint");
        TargetSelected?.Invoke(target);
    }

    public bool TrySelectUIAt(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;
        _uiHits.Clear();
        var pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
        EventSystem.current.RaycastAll(pointer, _uiHits);
        // EventSystem also includes Physics2DRaycaster hits. Legacy tall colliders must not
        // outrank the exposed artwork of a rear creature or the UI drawn over it.
        for (int i = _uiHits.Count - 1; i >= 0; i--)
            if (!(_uiHits[i].module is UnityEngine.UI.GraphicRaycaster)) _uiHits.RemoveAt(i);
        foreach (RaycastResult hit in _uiHits)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (hit.gameObject != null && TheLastArk.Battle.BattleDebugPanel.ContainsGraphic(hit.gameObject))
                return true;
#endif
            if (hit.gameObject != null && hit.gameObject.GetComponentInParent<DiceDragSource>() != null)
                return true;
        }
        // Use UI draw order for overlapping enemies, including exposed back-row HP bars.
        // Physics2D.Raycast alone can select a different collider behind the visible image.
        if (_uiHits.Count > 0 && _uiHits[0].gameObject != null)
        {
            var enemy = _uiHits[0].gameObject.GetComponentInParent<EnemyBattleCharacter>();
            if (enemy != null)
            {
                if (enemy.HasRuntimeStatus && enemy.CurrentHp > 0f)
                    SelectTarget(enemy.gameObject);
                return true;
            }
            var character = _uiHits[0].gameObject.GetComponentInParent<BattleCharacter>();
            if (character != null && character.status != null && character.status.currentHp > 0f)
                SelectTarget(character.gameObject);
            // Buttons, bars and panels consume the click instead of falling through to a collider.
            return true;
        }
        return false;
    }
    
    private void AnimateArrow()
    {
        // 원래 좌표 계산
        //arrowUI.position = target.transform.position;

        // TargetPoint가 있으면 그 위치를 쓰고, 없으면 캐릭터의 transform 위치를 씁니다.
        Vector3 basePos = (_cachedTargetPoint != null) ? _cachedTargetPoint.position : target.transform.position;

        // UI 좌표 동기화
        arrowUI.position = basePos;

        // 둥실둥실 애니메이션
        float newY = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        arrowUI.anchoredPosition += new Vector2(0, newY);
    }
    public void Deselect(bool notify = true)
    {
        // 1. 현재 선택된 타겟 정보를 비웁니다.
        target = null;

        // 2. 화살표 시각적 요소를 숨깁니다. 
        // (만약 이미지를 끄는 변수명이 다르다면 그에 맞춰 수정하세요)
        if (arrowUI != null) arrowUI.gameObject.SetActive(false);
        _cachedTargetPoint = null;
        if (notify) TargetCanceled?.Invoke();

        Debug.Log("타겟 선택이 해제되었습니다.");
    }
}

