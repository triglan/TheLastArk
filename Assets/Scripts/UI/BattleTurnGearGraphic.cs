using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    public sealed class BattleTurnGearGraphic : MaskableGraphic
    {
        public bool Face { get; set; }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            const int segments = 120;
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
            for (int n = 0; n < segments; n++)
            {
                float a = n * Mathf.PI * 2f / segments;
                float b = (n + 1) * Mathf.PI * 2f / segments;
                float outer = Face ? radius : radius * (n % 10 >= 2 && n % 10 < 7 ? 1f : 0.88f);
                float inner = Face ? 0f : radius * 0.70f;
                int start = vh.currentVertCount;
                vh.AddVert(new Vector3(Mathf.Sin(a) * inner, Mathf.Cos(a) * inner), color, Vector2.zero);
                vh.AddVert(new Vector3(Mathf.Sin(a) * outer, Mathf.Cos(a) * outer), color, Vector2.zero);
                vh.AddVert(new Vector3(Mathf.Sin(b) * outer, Mathf.Cos(b) * outer), color, Vector2.zero);
                vh.AddVert(new Vector3(Mathf.Sin(b) * inner, Mathf.Cos(b) * inner), color, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start + 2, start + 3, start);
            }
        }
    }
}
