using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TheLastArk.UI;

public class BattleDebugVictoryButton : MonoBehaviour
{
    private const string ButtonObjectName = "DebugVictoryButton";

    private BattleManager battleManager;

    public void Initialize(BattleManager targetBattleManager)
    {
        battleManager = targetBattleManager;
        if (battleManager == null)
        {
            Debug.LogError("[BattleDebugVictoryButton] BattleManager is null.");
            return;
        }

        CreateOrBindButton();
    }

    private void CreateOrBindButton()
    {
        EnsureEventSystem();
        Canvas canvas = EnsureCanvas();

        GameObject existingButton = GameObject.Find(ButtonObjectName);
        Button button = existingButton != null ? existingButton.GetComponent<Button>() : null;
        if (button == null)
            button = CreateButton(canvas.transform);

        button.onClick.RemoveListener(OnDebugVictoryClicked);
        button.onClick.AddListener(OnDebugVictoryClicked);
        button.interactable = true;
        button.gameObject.SetActive(true);

        TMPFontManager.ApplyFontToAll(button.transform);
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static Canvas EnsureCanvas()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        GameObject canvasObject = new GameObject("BattleDebugCanvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static Button CreateButton(Transform parent)
    {
        GameObject buttonObject = new GameObject(ButtonObjectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-70f, -140f);
        rect.sizeDelta = new Vector2(180f, 56f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.18f, 0.92f);
        image.raycastTarget = true;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.18f, 0.18f, 0.18f, 0.92f);
        colors.highlightedColor = new Color(0.32f, 0.32f, 0.32f, 1f);
        colors.pressedColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = "Debug:\n전투 승리";
        text.fontSize = 20f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        TMPFontManager.ApplyFont(text);

        return button;
    }

    private void OnDebugVictoryClicked()
    {
        if (battleManager == null) return;
        battleManager.DebugWinBattle();
    }
}
