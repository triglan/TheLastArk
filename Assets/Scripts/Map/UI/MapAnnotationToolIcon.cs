using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.Map.UI
{
    /// <summary>Small vector UI glyphs, independent of fonts and texture imports.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapAnnotationToolIcon : MaskableGraphic
    {
        public int Kind { get; set; }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Kind == 0)
            {
                Line(vh, 5, 5, 8, 13); Line(vh, 8, 13, 24, 29);
                Line(vh, 24, 29, 29, 24); Line(vh, 29, 24, 13, 8);
                Line(vh, 13, 8, 5, 5); Line(vh, 21, 26, 26, 21);
                Line(vh, 8, 13, 13, 8); Line(vh, 5, 5, 8, 6);
            }
            else if (Kind == 1)
            {
                Line(vh, 4, 12, 18, 27); Line(vh, 18, 27, 29, 17);
                Line(vh, 29, 17, 17, 5); Line(vh, 17, 5, 11, 5);
                Line(vh, 11, 5, 4, 12); Line(vh, 11, 19, 22, 10);
                Line(vh, 19, 4, 29, 4);
            }
            else
            {
                Line(vh, 5, 25, 27, 25); Line(vh, 12, 25, 12, 29);
                Line(vh, 12, 29, 20, 29); Line(vh, 20, 29, 20, 25);
                Line(vh, 8, 22, 10, 4); Line(vh, 10, 4, 22, 4);
                Line(vh, 22, 4, 24, 22); Line(vh, 13, 20, 14, 8);
                Line(vh, 19, 20, 18, 8);
            }
        }

        private void Line(VertexHelper vh, float x1, float y1, float x2, float y2)
        {
            Rect r = rectTransform.rect;
            Vector2 a = r.min + Vector2.Scale(new Vector2(x1, y1) / 32, r.size);
            Vector2 b = r.min + Vector2.Scale(new Vector2(x2, y2) / 32, r.size);
            Vector2 d = (b - a).normalized;
            Vector2 n = new Vector2(-d.y, d.x) * (r.width / 32 * 1.1f);
            int start = vh.currentVertCount;
            vh.AddVert(a - n, color, Vector2.zero); vh.AddVert(a + n, color, Vector2.zero);
            vh.AddVert(b + n, color, Vector2.zero); vh.AddVert(b - n, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
