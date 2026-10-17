using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Right-drag ink. A future transport assigns LocalPlayerSlot, sends LocalStrokeCompleted
/// and calls ApplyRemoteStroke on peers for the same map. No transport dependency here.
/// </summary>
public class MapAnnotationController : MonoBehaviour
{
    public const int MaxPointsPerStroke = 512;
    public const int MaxStrokes = 128;
    public enum Tool { Pen, Eraser }
    public Tool SelectedTool { get; private set; }
    private const float EraserRadius = 18f;
    [SerializeField, Range(0, 3)] private int localPlayerSlot;
    public int LocalPlayerSlot
    {
        get => localPlayerSlot;
        set { EndGesture(); localPlayerSlot = Mathf.Clamp(value, 0, 3); toolbar?.Refresh(); }
    }
    public event Action<MapAnnotationStroke> LocalStrokeCompleted;
    // Future transport must authenticate the sender's slot before applying a removal.
    public event Action<int, string> LocalStrokeRemoved;

    private ScrollRect scroll;
    private MapData data;
    private RectTransform layer;
    private MapAnnotationStroke activeStroke;
    private MapAnnotationGraphic activeGraphic;
    private readonly Dictionary<string, MapAnnotationGraphic> graphics = new Dictionary<string, MapAnnotationGraphic>();
    private readonly List<RaycastResult> hits = new List<RaycastResult>();
    private PointerEventData pointer;
    private EventSystem pointerEventSystem;
    private bool drawingGesture;
    private bool hasEraserPoint;
    private Vector2 lastEraserPoint;
    private TheLastArk.Map.UI.MapAnnotationToolbar toolbar;

    public void SelectTool(Tool tool)
    {
        EndGesture();
        SelectedTool = tool;
        toolbar?.Refresh();
    }

    public static Color GetPlayerColor(int slot)
    {
        switch (slot)
        {
            case 1: return new Color32(40, 110, 225, 255);
            case 2: return new Color32(35, 155, 70, 255);
            case 3: return new Color32(150, 65, 200, 255);
            default: return new Color32(205, 45, 45, 255);
        }
    }

    public void Initialize(ScrollRect mapScroll, MapData mapData)
    {
        EndGesture();
        scroll = mapScroll;
        data = mapData;
        if (data.annotations == null) data.annotations = new List<MapAnnotationStroke>();
        if (layer != null) { layer.gameObject.SetActive(false); MapManager.SafeDestroy(layer.gameObject); }
        graphics.Clear();
        var obj = new GameObject("MapAnnotations", typeof(RectTransform));
        layer = obj.GetComponent<RectTransform>();
        layer.SetParent(scroll.content, false);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;
        foreach (var stroke in data.annotations)
            if (IsValid(stroke) && !graphics.ContainsKey(stroke.id)) CreateGraphic(stroke);
        if (toolbar == null)
        {
            var tools = new GameObject("MapDrawingTools", typeof(RectTransform));
            tools.transform.SetParent(scroll.transform, false);
            toolbar = tools.AddComponent<TheLastArk.Map.UI.MapAnnotationToolbar>();
            toolbar.Initialize(this);
        }
        toolbar.transform.SetAsLastSibling();
        toolbar.Refresh();
    }

    private void Update()
    {
        if (scroll == null || data == null || !scroll.isActiveAndEnabled) { EndGesture(); return; }
        if (!Input.GetMouseButton(1)) { EndGesture(); return; }
        Vector2 point;
        if (!TryGetMapPoint(out point)) { hasEraserPoint = false; FinishStroke(); return; }
        if (Input.GetMouseButtonDown(1)) drawingGesture = true;
        if (!drawingGesture) return;
        if (SelectedTool == Tool.Eraser)
        {
            scroll.StopMovement();
            EraseOwnStrokesBetween(hasEraserPoint ? lastEraserPoint : point, point);
            lastEraserPoint = point;
            hasEraserPoint = true;
            return;
        }
        // Resume as a new segment after leaving the viewport; never bridge across a popup.
        if (activeStroke == null)
        {
            scroll.StopMovement();
            activeStroke = new MapAnnotationStroke { id = Guid.NewGuid().ToString("N"), playerSlot = localPlayerSlot };
            data.annotations.Add(activeStroke);
            activeGraphic = CreateGraphic(activeStroke);
            TrimOldStrokes();
        }
        var points = activeStroke.points;
        if (points.Count > 0 && Vector2.Scale(point - points[points.Count - 1], scroll.content.rect.size).sqrMagnitude < 4f) return;
        points.Add(point);
        activeGraphic.SetVerticesDirty();
        if (points.Count >= MaxPointsPerStroke) FinishStroke();
    }

    private bool TryGetMapPoint(out Vector2 normalized)
    {
        normalized = Vector2.zero;
        if (EventSystem.current == null || scroll.viewport == null) return false;
        if (pointer == null || pointerEventSystem != EventSystem.current)
        {
            pointerEventSystem = EventSystem.current;
            pointer = new PointerEventData(pointerEventSystem);
        }
        pointer.Reset();
        pointer.position = Input.mousePosition;
        hits.Clear();
        EventSystem.current.RaycastAll(pointer, hits);
        if (hits.Count == 0 || !hits[0].gameObject.transform.IsChildOf(scroll.transform)) return false;
        if (toolbar != null && hits[0].gameObject.transform.IsChildOf(toolbar.transform)) return false;
        var raycaster = hits[0].module;
        Camera camera = raycaster != null ? raycaster.eventCamera : null;
        if (!RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport, pointer.position, camera)) return false;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(scroll.content, pointer.position, camera, out local)) return false;
        Rect rect = scroll.content.rect;
        if (rect.width <= 0 || rect.height <= 0 || !rect.Contains(local)) return false;
        normalized = new Vector2((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height);
        return true;
    }

    private MapAnnotationGraphic CreateGraphic(MapAnnotationStroke stroke)
    {
        var obj = new GameObject("Ink_" + stroke.id, typeof(RectTransform), typeof(CanvasRenderer));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(layer, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var graphic = obj.AddComponent<MapAnnotationGraphic>();
        graphic.Bind(stroke);
        graphics.Add(stroke.id, graphic);
        return graphic;
    }

    private void FinishStroke()
    {
        var completed = activeStroke;
        activeStroke = null;
        activeGraphic = null;
        if (completed != null && completed.points.Count > 0) LocalStrokeCompleted?.Invoke(Clone(completed));
    }

    private void EndGesture()
    {
        drawingGesture = false;
        hasEraserPoint = false;
        FinishStroke();
    }

    public void ClearOwnStrokes()
    {
        EndGesture();
        if (data == null) return;
        for (int i = data.annotations.Count - 1; i >= 0; i--)
            if (data.annotations[i].playerSlot == localPlayerSlot) RemoveAt(i, true);
    }

    /// <summary>Cut only the swept region out of owned strokes; preserve disconnected remnants.</summary>
    public int EraseOwnStrokesBetween(Vector2 from, Vector2 to)
    {
        if (data == null || scroll == null) return 0;
        Vector2 size = scroll.content.rect.size;
        Vector2 a = Vector2.Scale(from, size);
        Vector2 b = Vector2.Scale(to, size);
        int removed = 0;
        for (int i = data.annotations.Count - 1; i >= 0; i--)
        {
            var stroke = data.annotations[i];
            if (stroke.playerSlot != localPlayerSlot || stroke == activeStroke || stroke.points.Count == 0) continue;
            Vector2 previous = Vector2.Scale(stroke.points[0], size);
            bool touched = PointSegmentDistanceSquared(previous, a, b) <= EraserRadius * EraserRadius;
            for (int j = 1; !touched && j < stroke.points.Count; j++)
            {
                Vector2 next = Vector2.Scale(stroke.points[j], size);
                touched = SegmentDistanceSquared(a, b, previous, next) <= EraserRadius * EraserRadius;
                previous = next;
            }
            if (!touched) continue;
            bool changed;
            var remnants = TheLastArk.Map.UI.MapStrokeEraser.Cut(stroke.points, size, from, to, EraserRadius, out changed);
            if (!changed) continue;
            RemoveAt(i, true);
            int insertion = i;
            foreach (var points in remnants)
            {
                // Splitting ink must never evict unrelated strokes through the drawing cap.
                for (int offset = 0; offset < points.Count;)
                {
                    int count = Mathf.Min(MaxPointsPerStroke, points.Count - offset);
                    var fragment = new MapAnnotationStroke
                    {
                        id = Guid.NewGuid().ToString("N"), playerSlot = stroke.playerSlot,
                        points = points.GetRange(offset, count)
                    };
                    data.annotations.Insert(insertion++, fragment);
                    CreateGraphic(fragment);
                    LocalStrokeCompleted?.Invoke(Clone(fragment));
                    if (offset + count == points.Count) break;
                    offset += count - 1;
                }
            }
            removed++;
        }
        return removed;
    }

    public bool ApplyRemoteRemoval(int ownerSlot, string strokeId)
    {
        if (data == null || ownerSlot < 0 || ownerSlot > 3) return false;
        int index = data.annotations.FindIndex(s => s.id == strokeId && s.playerSlot == ownerSlot);
        if (index < 0 || data.annotations[index] == activeStroke) return false;
        RemoveAt(index, false);
        return true;
    }

    private void RemoveAt(int index, bool notify)
    {
        var stroke = data.annotations[index];
        data.annotations.RemoveAt(index);
        if (graphics.TryGetValue(stroke.id, out var graphic))
        {
            graphic.gameObject.SetActive(false);
            MapManager.SafeDestroy(graphic.gameObject);
            graphics.Remove(stroke.id);
        }
        if (notify) LocalStrokeRemoved?.Invoke(stroke.playerSlot, stroke.id);
    }

    private static float PointSegmentDistanceSquared(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 delta = b - a;
        float t = delta.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point - a, delta) / delta.sqrMagnitude) : 0;
        return (point - (a + t * delta)).sqrMagnitude;
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    private static float SegmentDistanceSquared(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        Vector2 ab = b - a, cd = d - c;
        float denominator = Cross(ab, cd);
        if (Mathf.Abs(denominator) > 0.0001f)
        {
            float t = Cross(c - a, cd) / denominator;
            float u = Cross(c - a, ab) / denominator;
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1) return 0;
        }
        return Mathf.Min(Mathf.Min(PointSegmentDistanceSquared(a, c, d), PointSegmentDistanceSquared(b, c, d)),
            Mathf.Min(PointSegmentDistanceSquared(c, a, b), PointSegmentDistanceSquared(d, a, b)));
    }

    /// <summary>Returns false for invalid or duplicate payloads; never echoes remote data.</summary>
    public bool ApplyRemoteStroke(MapAnnotationStroke stroke)
    {
        if (data == null || !IsValid(stroke) || graphics.ContainsKey(stroke.id)) return false;
        var copy = Clone(stroke);
        data.annotations.Add(copy);
        CreateGraphic(copy);
        TrimOldStrokes();
        return true;
    }

    private static bool IsValid(MapAnnotationStroke stroke)
    {
        if (stroke == null || string.IsNullOrEmpty(stroke.id) || stroke.id.Length > 64 ||
            stroke.playerSlot < 0 || stroke.playerSlot > 3 || stroke.points == null ||
            stroke.points.Count == 0 || stroke.points.Count > MaxPointsPerStroke) return false;
        foreach (var p in stroke.points)
            if (float.IsNaN(p.x) || float.IsNaN(p.y) || p.x < 0 || p.x > 1 || p.y < 0 || p.y > 1) return false;
        return true;
    }

    private static MapAnnotationStroke Clone(MapAnnotationStroke stroke) => new MapAnnotationStroke
    {
        id = stroke.id, playerSlot = stroke.playerSlot, points = new List<Vector2>(stroke.points)
    };

    private void TrimOldStrokes()
    {
        while (data.annotations.Count > MaxStrokes)
        {
            int index = data.annotations[0] == activeStroke ? 1 : 0;
            RemoveAt(index, false);
        }
    }

    private void OnDisable() => EndGesture();
    private void OnApplicationFocus(bool focused) { if (!focused) EndGesture(); }
}
