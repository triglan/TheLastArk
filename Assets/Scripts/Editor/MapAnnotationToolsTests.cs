using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class MapAnnotationToolsTests
{
    private GameObject root;
    private MapData data;
    private MapAnnotationController controller;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("AnnotationTest", typeof(RectTransform), typeof(ScrollRect));
        var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(root.transform, false);
        content.sizeDelta = new Vector2(1000, 1000);
        var scroll = root.GetComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = (RectTransform)root.transform;
        data = new MapData();
        controller = root.AddComponent<MapAnnotationController>();
        controller.Initialize(scroll, data);
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(root);

    private void Add(string id, int slot, float y)
    {
        var stroke = new MapAnnotationStroke { id = id, playerSlot = slot };
        stroke.points.Add(new Vector2(0.2f, y));
        stroke.points.Add(new Vector2(0.8f, y));
        Assert.IsTrue(controller.ApplyRemoteStroke(stroke));
    }

    [Test]
    public void SweptEraserCutsOnlyTouchedSectionAndPreservesOtherPlayers()
    {
        Add("mine", 0, 0.5f);
        Add("other", 1, 0.5f);
        Add("distant", 0, 0.9f);
        int events = 0;
        controller.LocalStrokeRemoved += (slot, id) => { Assert.AreEqual(0, slot); events++; };
        Assert.AreEqual(1, controller.EraseOwnStrokesBetween(new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.7f)));
        Assert.AreEqual(4, data.annotations.Count);
        Assert.IsTrue(data.annotations.Exists(s => s.id == "other"));
        Assert.IsTrue(data.annotations.Exists(s => s.id == "distant"));
        Assert.AreEqual(1, events);
        var fragments = data.annotations.FindAll(s => s.playerSlot == 0 && s.id != "distant");
        Assert.AreEqual(2, fragments.Count);
        Assert.AreEqual(0.2f, fragments[0].points[0].x, 0.0001f);
        Assert.AreEqual(0.482f, fragments[0].points[fragments[0].points.Count - 1].x, 0.0001f);
        Assert.AreEqual(0.518f, fragments[1].points[0].x, 0.0001f);
        Assert.AreEqual(0.8f, fragments[1].points[fragments[1].points.Count - 1].x, 0.0001f);
        Assert.AreEqual(0, controller.EraseOwnStrokesBetween(new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.7f)));
        var restored = JsonUtility.FromJson<MapData>(JsonUtility.ToJson(data));
        Assert.AreEqual(4, restored.annotations.Count);
    }

    [Test]
    public void ButtonsSelectToolsAndImmediatelyClearOnlyLocalSlot()
    {
        Assert.IsNull(root.transform.Find("MapDrawingTools/ToolHint"));
        for (int i = 0; i < 4; i++) Add("player" + i, i, 0.5f);
        controller.LocalPlayerSlot = 2;
        root.transform.Find("MapDrawingTools/Eraser").GetComponent<Button>().onClick.Invoke();
        Assert.AreEqual(MapAnnotationController.Tool.Eraser, controller.SelectedTool);
        root.transform.Find("MapDrawingTools/Pen").GetComponent<Button>().onClick.Invoke();
        Assert.AreEqual(MapAnnotationController.Tool.Pen, controller.SelectedTool);
        root.transform.Find("MapDrawingTools/ClearMine").GetComponent<Button>().onClick.Invoke();
        Assert.AreEqual(3, data.annotations.Count);
        Assert.IsFalse(data.annotations.Exists(s => s.playerSlot == 2));
        var restored = JsonUtility.FromJson<MapData>(JsonUtility.ToJson(data));
        Assert.AreEqual(3, restored.annotations.Count);
        controller.ClearOwnStrokes();
        Assert.AreEqual(3, data.annotations.Count);
    }

    [Test]
    public void StationaryEraserCutsSparseLineAndClearRemovesItsFragments()
    {
        Add("mine", 0, 0.5f);
        Assert.AreEqual(1, controller.EraseOwnStrokesBetween(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)));
        Assert.AreEqual(2, data.annotations.Count);
        Assert.AreEqual(0.482f, data.annotations[0].points[data.annotations[0].points.Count - 1].x, 0.0001f);
        Assert.AreEqual(0.518f, data.annotations[1].points[0].x, 0.0001f);
        controller.ClearOwnStrokes();
        Assert.AreEqual(0, data.annotations.Count);
    }

    [Test]
    public void RemoteRemovalChecksOwnershipAndDoesNotEcho()
    {
        Add("foreign", 1, 0.5f);
        int events = 0;
        controller.LocalStrokeRemoved += (slot, id) => events++;
        Assert.IsFalse(controller.ApplyRemoteRemoval(0, "foreign"));
        Assert.AreEqual(1, data.annotations.Count);
        Assert.IsTrue(controller.ApplyRemoteRemoval(1, "foreign"));
        Assert.IsFalse(controller.ApplyRemoteRemoval(1, "foreign"));
        Assert.AreEqual(0, events);
    }
}
