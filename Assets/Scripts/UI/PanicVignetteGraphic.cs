using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    // Screen-edge dimming only. The illustrated border is a separate imported sprite.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PanicVignetteGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            const int segments = 64;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 edge = direction / Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));
                vh.AddVert(rect.center + Vector2.Scale(rect.size * .5f, edge * .48f), Color.clear, Vector2.zero);
                vh.AddVert(rect.center + Vector2.Scale(rect.size * .5f, edge), new Color(.18f, .008f, .025f, .88f), Vector2.zero);
                if (i == segments) continue;
                int n = i * 2;
                vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n + 1, n + 3, n + 2);
            }
        }
    }
}
