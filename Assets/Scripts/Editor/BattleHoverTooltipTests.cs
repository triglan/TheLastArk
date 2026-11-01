using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.TestTools;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TheLastArk.UI;

public class BattleHoverTooltipTests
{
    private GameObject source, hud, camera;
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [TearDown] public void Cleanup()
    {
        if (source != null) Object.DestroyImmediate(source);
        if (hud != null) Object.DestroyImmediate(hud);
        if (camera != null) Object.DestroyImmediate(camera);
        var overlay = GameObject.Find("Battle Tooltip Canvas");
        if (overlay != null) Object.DestroyImmediate(overlay);
    }

    [TestCase(RenderMode.ScreenSpaceCamera)]
    [TestCase(RenderMode.ScreenSpaceOverlay)]
    public void TooltipStaysAboveOverlayTrayAndReusesPanelAfterHide(RenderMode mode)
    {
        camera = new GameObject("Tooltip test camera", typeof(Camera));
        var cam = camera.GetComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 540;
        cam.transform.position = new Vector3(0, 0, -100);
        source = new GameObject("Character canvas", typeof(RectTransform), typeof(Canvas));
        var sourceCanvas = source.GetComponent<Canvas>();
        sourceCanvas.renderMode = mode;
        sourceCanvas.worldCamera = cam; sourceCanvas.planeDistance = 5;
        hud = new GameObject("Test command canvas", typeof(RectTransform), typeof(Canvas));
        var commandCanvas = hud.GetComponent<Canvas>();
        commandCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        commandCanvas.sortingOrder = 100;
        var icon = new GameObject("Capacitor", typeof(RectTransform), typeof(Image), typeof(BattleHoverTooltip));
        icon.transform.SetParent(source.transform, false);
        var rect = icon.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-18, 18);
        var tooltip = icon.GetComponent<BattleHoverTooltip>();
        tooltip.SetContent("축전 2 / 10\n자기 턴 시작 시 +2\n보호막으로 피해를 흡수하면 축전 획득\n전투마다 초기화");
        tooltip.OnPointerEnter(null);
        var panel = (RectTransform)typeof(BattleHoverTooltip).GetField("panel", Private).GetValue(tooltip);
        var canvas = panel.GetComponentInParent<Canvas>();
        Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
        Assert.Greater(canvas.sortingOrder, commandCanvas.sortingOrder);
        Assert.AreNotSame(sourceCanvas, canvas);
        Assert.IsNull(canvas.GetComponent<GraphicRaycaster>());
        foreach (var graphic in panel.GetComponentsInChildren<Graphic>()) Assert.IsFalse(graphic.raycastTarget);
        var host = (RectTransform)canvas.transform;
        Assert.GreaterOrEqual(panel.anchoredPosition.x, host.rect.xMin);
        Assert.LessOrEqual(panel.anchoredPosition.x + panel.rect.width, host.rect.xMax);
        Assert.GreaterOrEqual(panel.anchoredPosition.y - panel.rect.height, host.rect.yMin);
        Assert.LessOrEqual(panel.anchoredPosition.y, host.rect.yMax);
        tooltip.OnPointerExit(null);
        Assert.IsFalse(panel.gameObject.activeSelf);
        tooltip.OnPointerEnter(null);
        Assert.AreSame(panel, typeof(BattleHoverTooltip).GetField("panel", Private).GetValue(tooltip));
        Assert.IsTrue(panel.gameObject.activeSelf);
        Assert.Greater(canvas.sortingOrder, commandCanvas.sortingOrder);
        // Runtime MonoBehaviour callbacks are not invoked by SetActive in Edit Mode.
        if (Application.isPlaying)
        {
            icon.SetActive(false);
            Assert.IsFalse(panel.gameObject.activeSelf);
        }
    }

    [UnityTest]
    public IEnumerator TooltipAlsoHidesWhenCharacterIsDisabledInPlayMode()
    {
        yield return new EnterPlayMode();
        TooltipStaysAboveOverlayTrayAndReusesPanelAfterHide(RenderMode.ScreenSpaceCamera);
        Cleanup();
        yield return new ExitPlayMode();
    }
}
