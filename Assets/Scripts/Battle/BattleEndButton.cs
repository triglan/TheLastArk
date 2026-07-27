using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TheLastArk.UI;

/// <summary>
/// BattleScene에 턴 종료 버튼을 생성하고 BattleManager.EndPlayerTurn()과 연결합니다.
/// </summary>
public class BattleEndButton : MonoBehaviour
{
    private BattleManager _battleManager;
    private Button _button;

    public void Initialize(BattleManager battleManager)
    {
        Debug.Log("[BattleEndButton] Initialize() 호출");
        _battleManager = battleManager;

        if (_battleManager == null)
        {
            Debug.LogError("[BattleEndButton] BattleManager가 null입니다.");
            return;
        }

        CreateEndBattleButton();
    }

    private void CreateEndBattleButton()
    {
        Debug.Log("[BattleEndButton] 턴 종료 버튼 생성 시작");

        EnsureEventSystem();
        Canvas canvas = EnsureCanvas();

        GameObject btnObj = new GameObject("TurnEndButton");
        btnObj.transform.SetParent(canvas.transform, false);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.85f, 0.2f, 0.2f, 0.9f);
        btnImage.raycastTarget = true;

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 1f);
        btnRect.anchorMax = new Vector2(1f, 1f);
        btnRect.pivot = new Vector2(1f, 1f);
        btnRect.anchoredPosition = new Vector2(-70f, -70f);
        btnRect.sizeDelta = new Vector2(60f, 60f);

        _button = btnObj.AddComponent<Button>();
        _button.targetGraphic = btnImage;
        _button.interactable = true;

        Navigation nav = _button.navigation;
        nav.mode = Navigation.Mode.None;
        _button.navigation = nav;

        ColorBlock colors = _button.colors;
        colors.normalColor = new Color(0.85f, 0.2f, 0.2f, 0.9f);
        colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
        colors.selectedColor = new Color(1f, 0.3f, 0.3f, 1f);
        colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
        _button.colors = colors;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = "턴\n종료";
        tmpText.fontSize = 24;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        TMPFontManager.ApplyFont(tmpText);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnTurnEndButtonClicked);

        _battleManager.turnEndButton = _button;
        Debug.Log("[BattleEndButton] 턴 종료 버튼 생성 완료");
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null) return;

        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();
        esObj.AddComponent<StandaloneInputModule>();
        Debug.Log("[BattleEndButton] EventSystem 생성");
    }

    private static Canvas EnsureCanvas()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("BattleCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            Debug.Log("[BattleEndButton] Canvas 생성");
            return canvas;
        }

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    private void OnTurnEndButtonClicked()
    {
        Debug.Log("[BattleEndButton] 턴 종료 버튼 클릭");

        if (_battleManager == null)
        {
            Debug.LogError("[BattleEndButton] BattleManager가 null입니다.");
            return;
        }

        _battleManager.EndPlayerTurn();
    }

    private void Update()
    {
        if (_button == null || !Input.GetMouseButtonDown(0) || EventSystem.current == null) return;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            if (result.gameObject != _button.gameObject) continue;
            Debug.Log("[BattleEndButton] EventSystem이 턴 종료 버튼 클릭을 감지했습니다.");
            break;
        }
    }
}
