using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// BattleScene에 "턴 종료" 버튼을 생성합니다.
/// GameManager.BeginBattleSetup()에서 Initialize()를 호출하여 초기화됩니다.
/// </summary>
public class BattleEndButton : MonoBehaviour
{
    private BattleManager _battleManager;
    private Button _button;

    /// <summary>
    /// GameManager의 BeginBattleSetup()에서 호출됩니다.
    /// </summary>
    public void Initialize(BattleManager battleManager)
    {
        Debug.Log("[BattleEndButton] 🔧 Initialize() 호출");
        _battleManager = battleManager;

        if (_battleManager == null)
        {
            Debug.LogError("[BattleEndButton] ❌ BattleManager가 null입니다!");
            return;
        }

        CreateEndBattleButton();
    }

    private void CreateEndBattleButton()
    {
        Debug.Log("[BattleEndButton] 🎬 버튼 생성 시작");

        // EventSystem 확인
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            eventSystem = esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
            Debug.Log("[BattleEndButton] ✓ EventSystem 생성");
        }

        // Canvas 찾기 또는 생성
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("BattleCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            Debug.Log("[BattleEndButton] ✓ Canvas 생성");
        }
        else
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        // Button GameObject 생성
        GameObject btnObj = new GameObject("TurnEndButton");
        btnObj.transform.SetParent(canvas.transform, false);

        // Image 컴포넌트
        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.85f, 0.2f, 0.2f, 0.9f);
        btnImage.raycastTarget = true;

        // RectTransform 설정
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 1f);
        btnRect.anchorMax = new Vector2(1f, 1f);
        btnRect.pivot = new Vector2(1f, 1f);
        btnRect.anchoredPosition = new Vector2(-70f, -70f);
        btnRect.sizeDelta = new Vector2(60f, 60f);

        // Button 컴포넌트
        _button = btnObj.AddComponent<Button>();
        _button.targetGraphic = btnImage;
        _button.interactable = true;

        // Navigation 설정
        Navigation nav = _button.navigation;
        nav.mode = Navigation.Mode.None;
        _button.navigation = nav;

        // ColorBlock 설정
        ColorBlock colors = _button.colors;
        colors.normalColor = new Color(0.85f, 0.2f, 0.2f, 0.9f);
        colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
        colors.selectedColor = new Color(1f, 0.3f, 0.3f, 1f);
        colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
        _button.colors = colors;

        // 텍스트 추가
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        
        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = "턴\n종료";
        tmpText.fontSize = 36;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // 클릭 이벤트 등록
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnTurnEndButtonClicked);
        Debug.Log("[BattleEndButton] ✓ onClick 리스너 등록 완료");

        // BattleManager의 turnEndButton에 버튼 할당
        _battleManager.turnEndButton = _button;
        Debug.Log("[BattleEndButton] ✓ BattleManager.turnEndButton에 할당 완료");

        Debug.Log("[BattleEndButton] ✓ 턴 종료 버튼 생성 완료");
        Debug.Log($"[BattleEndButton] 📊 버튼 상태: GameObject={btnObj.name}, Button.enabled={_button.enabled}, Button.interactable={_button.interactable}, Canvas.enabled={canvas.enabled}");
    }

    private void OnTurnEndButtonClicked()
    {
        Debug.LogWarning("[BattleEndButton] 🎯🎯🎯 버튼 클릭됨! 🎯🎯🎯");
        
        if (_battleManager == null)
        {
            Debug.LogError("[BattleEndButton] ❌ BattleManager가 null입니다!");
            return;
        }

        Debug.Log("[BattleEndButton] ✓ EndPlayerTurn() 호출 중...");
        _battleManager.EndPlayerTurn();
    }

    private void Update()
    {
        // 버튼이 정말 클릭되고 있는지 수동 확인
        if (_button != null && Input.GetMouseButtonDown(0))
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
            {
                foreach (var result in results)
                {
                    if (result.gameObject == _button.gameObject)
                    {
                        Debug.LogWarning("[BattleEndButton] 🎯 EventSystem이 버튼 클릭 감지!");
                        break;
                    }
                }
            }
        }
    }
}