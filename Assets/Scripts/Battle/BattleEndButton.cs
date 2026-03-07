using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// BattleScene에 "전투 종료" 테스트 버튼을 자동 생성합니다.
/// 이 스크립트를 BattleScene의 아무 GameObject에 붙이면 됩니다.
/// </summary>
public class BattleEndButton : MonoBehaviour
{
    private void Start()
    {
        CreateEndBattleButton();
    }

private void CreateEndBattleButton()
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
        }

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 작은 정사각형 버튼
        GameObject btnObj = new GameObject("BattleEndButton");
        btnObj.transform.SetParent(canvas.transform, false);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.85f, 0.2f, 0.2f, 0.9f);

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 1f);
        btnRect.anchorMax = new Vector2(1f, 1f);
        btnRect.pivot = new Vector2(1f, 1f);
        btnRect.anchoredPosition = new Vector2(-10f, -10f);
        btnRect.sizeDelta = new Vector2(40f, 40f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;

        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
        btn.colors = colors;

        btn.onClick.AddListener(OnBattleEnd);
    }

    private void OnBattleEnd()
    {
        Debug.Log("[BattleEndButton] 전투 종료! MapScene으로 복귀합니다.");
        SceneManager.LoadScene("MapScene");
    }
}