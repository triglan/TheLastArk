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

    [Header("Floating marker")]
    public float floatSpeed = 3f;
    public float floatAmplitude = 3f;

    [Header("Target Hotkeys")]
    [Tooltip("적 1~4 (화면 왼쪽부터)")]
    [SerializeField] private KeyCode[] enemyTargetKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 };
    [Tooltip("아군 1~4 (파티 순서)")]
    [SerializeField] private KeyCode[] allyTargetKeys = { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R };

    private Transform _cachedTargetPoint;
    private CharacterView _selectedView;
    private BattleCharacter _selectedCharacter;
    private bool _selectedEnemy;
    private RectTransform _styledArrow;
    private Transform _markerCanvas;
    private Canvas _markerOverlay;
    private Canvas _markerRootCanvas;
    private float _selectedAt;
    private const float MarkerSize = 40f;
    [Tooltip("턴 종료")]
    [SerializeField] private KeyCode endTurnKey = KeyCode.Space;
    private const float MarkerGap = 8f;
    private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();

    void Update()
    {
        HandleMouseClick();
    }

    private void Start()
    {
        EnsureMarkerStyle();
        if (target == null && arrowUI != null) arrowUI.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (arrowUI == null) return;
        if (target == null || !target.activeInHierarchy || (_selectedCharacter?.status != null && _selectedCharacter.status.currentHp <= 0f))
        {
            if (target != null) Deselect(false);
            else arrowUI.gameObject.SetActive(false);
            return;
        }
        AnimateArrow();
    }
    private readonly List<BattleCharacter> _hotkeyEnemies = new List<BattleCharacter>();

    private void OnDisable()
    {
        Deselect(false);
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
        if (selected == null) { Deselect(); return; }
        EnsureMarkerStyle();
        if (target != selected) _selectedAt = Time.unscaledTime;
        target = selected;
        if (arrowUI != null)
        {
            arrowUI.gameObject.SetActive(true);
            RefreshMarkerSorting();
        }
        _cachedTargetPoint = target.transform.Find("TargetPoint");
        _selectedView = target.GetComponent<CharacterView>();
        _selectedCharacter = target.GetComponent<BattleCharacter>();
        _selectedEnemy = target.GetComponent<EnemyBattleCharacter>() != null;
        if (arrowUI != null) AnimateArrow();
        TargetSelected?.Invoke(target);
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
    
    private void EnsureMarkerStyle()
    {
        if (arrowUI == null || _styledArrow == arrowUI) return;
        _styledArrow = arrowUI;
        var image = arrowUI.GetComponent<UnityEngine.UI.Image>();
        if (image == null) image = arrowUI.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = Resources.Load<Sprite>("UI/BattleMarkers/SelectionArrow");
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = true;
        var icon = arrowUI.Find("Selection Icon");
        if (icon != null) icon.gameObject.SetActive(false);
        arrowUI.sizeDelta = Vector2.one * MarkerSize;
        arrowUI.pivot = Vector2.one * .5f;
        var parentCanvas = arrowUI.parent != null ? arrowUI.parent.GetComponentInParent<Canvas>() : null;
        _markerRootCanvas = parentCanvas != null ? parentCanvas.rootCanvas : null;
        _markerCanvas = _markerRootCanvas != null ? _markerRootCanvas.transform : arrowUI.parent;
        _markerOverlay = arrowUI.GetComponent<Canvas>();
        if (_markerOverlay == null) _markerOverlay = arrowUI.gameObject.AddComponent<Canvas>();
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

    private void RefreshMarkerSorting()
    {
        if (_markerOverlay == null) return;
        // Unity initializes a Canvas added to an inactive object on its first activation.
        // Apply sorting after SetActive, otherwise that initialization resets it to order 0.
        // Above character art/HUDs, below the command panels, tooltips and full-screen overlays.
        _markerOverlay.overrideSorting = true;
        _markerOverlay.sortingOrder = (_markerRootCanvas != null ? _markerRootCanvas.sortingOrder : 0) + 25;
        if (_markerRootCanvas != null) _markerOverlay.sortingLayerID = _markerRootCanvas.sortingLayerID;
    }

    private void AnimateArrow()
    {
        PositionMarker(Time.unscaledTime - _selectedAt);
    }

    private void PositionMarker(float elapsed)
    {
        Vector3 position;
        if (_selectedEnemy && _selectedView != null && _selectedView.displayImage != null)
        {
            // Follow the illustration, including creature sizes and formation rescaling, rather than its HUD.
            Rect bounds = _selectedView.IllustrationBounds;
            position = _selectedView.transform.TransformPoint(new Vector3(bounds.center.x, bounds.yMax, 0f));
            // Let the arrow overlap the artwork slightly instead of shrinking enemies to make room.
            position += CanvasOffset(Vector3.down * (MarkerSize * .2f));
        }
        else if (_selectedView != null && _selectedView.barUI != null && _selectedView.barUI.hpSlider != null)
        {
            var anchor = (RectTransform)_selectedView.barUI.hpSlider.transform;
            position = anchor.TransformPoint(new Vector3(anchor.rect.center.x, anchor.rect.yMax, 0f))
                + CanvasOffset(Vector3.up * (MarkerSize * .5f + MarkerGap));
        }
        else position = _cachedTargetPoint != null ? _cachedTargetPoint.position : target.transform.position;
        arrowUI.position = position + CanvasOffset(Vector3.up * (Mathf.Sin(elapsed * floatSpeed) * floatAmplitude));
        arrowUI.localScale = Vector3.one;
    }

    private Vector3 CanvasOffset(Vector3 offset) => _markerCanvas != null ? _markerCanvas.TransformVector(offset) : offset;
    public void Deselect(bool notify = true)
    {
        // 1. 현재 선택된 타겟 정보를 비웁니다.
        target = null;

        // 2. 화살표 시각적 요소를 숨깁니다. 
        // (만약 이미지를 끄는 변수명이 다르다면 그에 맞춰 수정하세요)
        if (arrowUI != null) arrowUI.gameObject.SetActive(false);
        _cachedTargetPoint = null;
        _selectedView = null;
        _selectedCharacter = null;
        if (notify) TargetCanceled?.Invoke();

        Debug.Log("타겟 선택이 해제되었습니다.");
    }
}

