using UnityEngine;
using UnityEngine.UI;

/// <summary>One bounded UI mesh per stroke, clipped by the map viewport.</summary>
public class MapAnnotationGraphic : MaskableGraphic
{
    private MapAnnotationStroke stroke;
    public void Bind(MapAnnotationStroke value)
    {
        stroke = value;
        raycastTarget = false;
        color = MapAnnotationController.GetPlayerColor(value.playerSlot);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (stroke == null || stroke.points == null) return;
        Rect rect = rectTransform.rect;
        const float radius = 2.5f;
        Vector2 previous = Vector2.zero;
        int count = Mathf.Min(stroke.points.Count, MapAnnotationController.MaxPointsPerStroke);
        for (int i = 0; i < count; i++)
        {
            Vector2 p = rect.min + Vector2.Scale(stroke.points[i], rect.size);
            if (i > 0 && (p - previous).sqrMagnitude > 0.001f)
            {
                Vector2 delta = (p - previous).normalized;
                Vector2 n = new Vector2(-delta.y, delta.x) * radius;
                int start = vh.currentVertCount;
                vh.AddVert(previous - n, color, Vector2.zero);
                vh.AddVert(previous + n, color, Vector2.zero);
                vh.AddVert(p + n, color, Vector2.zero);
                vh.AddVert(p - n, color, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
            int center = vh.currentVertCount;
            vh.AddVert(p, color, Vector2.zero);
            for (int j = 0; j < 8; j++)
            {
                float angle = j * Mathf.PI / 4;
                vh.AddVert(p + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
            }
            for (int j = 0; j < 8; j++) vh.AddTriangle(center, center + 1 + j, center + 1 + (j + 1) % 8);
            previous = p;
        }
    }
}
