using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    // One continuous header surface; only the outer gear teeth use the original frame art.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BattleTopBarGraphic : MaskableGraphic
    {
        private Sprite source;
        public const float BandHeight = 84f;
        public override Texture mainTexture => source != null ? source.texture : s_WhiteTexture;

        public void SetSource(Sprite sprite)
        {
            source = sprite;
            raycastTarget = false;
            SetAllDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (source == null) return;
            Rect rect = rectTransform.rect;
            Rect pixels = source.rect;
            float scale = rect.width / pixels.width;
            const float sourceBandEdge = 116f;
            const float sourceTeethBottom = 147f;
            float top = rect.yMax - BandHeight;
            float bottom = top - (sourceTeethBottom - sourceBandEdge) * scale;
            float u0 = pixels.xMin / source.texture.width;
            float u1 = pixels.xMax / source.texture.width;
            float v0 = (pixels.yMax - sourceTeethBottom) / source.texture.height;
            float v1 = (pixels.yMax - sourceBandEdge) / source.texture.height;
            AddQuad(vh, new Rect(rect.xMin, bottom, rect.width * 0.10f, top - bottom),
                new Rect(u0, v0, (u1 - u0) * 0.10f, v1 - v0));
            AddQuad(vh, new Rect(rect.xMin + rect.width * 0.90f, bottom, rect.width * 0.10f, top - bottom),
                new Rect(Mathf.Lerp(u0, u1, 0.90f), v0, (u1 - u0) * 0.10f, v1 - v0));
            AddQuad(vh, new Rect(rect.xMin, rect.yMax - BandHeight, rect.width, BandHeight),
                new Rect(Mathf.Lerp(u0, u1, 0.3f), (pixels.yMax - 10f) / source.texture.height, 0f, 0f));
        }

        private void AddQuad(VertexHelper vh, Rect position, Rect uv)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(position.xMin, position.yMin), color, new Vector2(uv.xMin, uv.yMin));
            vh.AddVert(new Vector3(position.xMin, position.yMax), color, new Vector2(uv.xMin, uv.yMax));
            vh.AddVert(new Vector3(position.xMax, position.yMax), color, new Vector2(uv.xMax, uv.yMax));
            vh.AddVert(new Vector3(position.xMax, position.yMin), color, new Vector2(uv.xMax, uv.yMin));
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i + 2, i + 3, i);
        }
    }
}
