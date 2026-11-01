using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    // Soft, slightly uneven edges using the normal UI material; the source illustration is untouched.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FeatheredIllustration : MaskableGraphic
    {
        private Sprite source;
        public Sprite Sprite => source;
        public override Texture mainTexture => source != null ? source.texture : Texture2D.whiteTexture;
        public void SetSprite(Sprite sprite)
        {
            if (source==sprite) return;
            source=sprite; SetAllDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if(source==null) return;
            Rect area=GetPixelAdjustedRect();
            float scale=Mathf.Min(area.width/source.rect.width,area.height/source.rect.height);
            Vector2 size=source.rect.size*scale;
            Vector2 start=area.center-size*.5f;
            Vector4 uv=UnityEngine.Sprites.DataUtility.GetOuterUV(source);
            const int segments=40;
            for(int y=0;y<=segments;y++)for(int x=0;x<=segments;x++)
            {
                float u=x/(float)segments,v=y/(float)segments;
                float edge=Mathf.Min(Mathf.Min(u,1-u),Mathf.Min(v,1-v));
                float radius=new Vector2((u-.5f)*2,(v-.5f)*2).magnitude;
                float feather=.14f+.025f*Mathf.Sin(u*21+v*13)+.015f*Mathf.Cos(v*29-u*9);
                float alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/feather))
                    * (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.95f,1.32f,radius)));
                Color tint=color;tint.a*=alpha;
                vh.AddVert(start+Vector2.Scale(size,new Vector2(u,v)),tint,new Vector2(Mathf.Lerp(uv.x,uv.z,u),Mathf.Lerp(uv.y,uv.w,v)));
            }
            for(int y=0;y<segments;y++)for(int x=0;x<segments;x++)
            {
                int i=y*(segments+1)+x;
                vh.AddTriangle(i,i+segments+1,i+1);vh.AddTriangle(i+1,i+segments+1,i+segments+2);
            }
        }
    }
}
