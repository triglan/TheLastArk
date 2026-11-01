using UnityEngine;
using UnityEngine.UI;
using TheLastArk.Battle;

namespace TheLastArk.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PanicIconGraphic : MaskableGraphic
    {
        private PanicType type;
        public void SetType(PanicType value){if(type==value)return;type=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            switch(type)
            {
                case PanicType.Hallucination:
                    Path(vh,new[]{new Vector2(-.85f,0),new Vector2(-.4f,.45f),new Vector2(.4f,.45f),new Vector2(.85f,0),new Vector2(.4f,-.45f),new Vector2(-.4f,-.45f),new Vector2(-.85f,0)});
                    Ring(vh,.2f);break;
                case PanicType.Compulsion:
                    Ring(vh,.72f);Ring(vh,.3f);break;
                case PanicType.Seizure:
                    Path(vh,new[]{new Vector2(.35f,.85f),new Vector2(-.45f,.05f),new Vector2(.3f,.05f),new Vector2(-.35f,-.85f)});break;
                case PanicType.Depression:
                    Line(vh,new Vector2(0,.75f),new Vector2(0,-.65f));
                    Path(vh,new[]{new Vector2(-.55f,-.1f),new Vector2(0,-.7f),new Vector2(.55f,-.1f)});break;
                case PanicType.Lethargy:
                    Path(vh,new[]{new Vector2(-.75f,.15f),new Vector2(-.35f,-.25f),new Vector2(.35f,-.25f),new Vector2(.75f,.15f)});
                    Line(vh,new Vector2(-.35f,-.25f),new Vector2(-.5f,-.6f));Line(vh,new Vector2(.35f,-.25f),new Vector2(.5f,-.6f));break;
                case PanicType.Agitation:
                    for(int i=-1;i<=1;i++)Path(vh,new[]{new Vector2(i*.5f-.12f,.7f),new Vector2(i*.5f+.12f,.2f),new Vector2(i*.5f-.12f,-.2f),new Vector2(i*.5f+.12f,-.7f)});break;
            }
        }
        private void Ring(VertexHelper vh,float radius)
        {
            const int steps=24;
            for(int i=0;i<steps;i++)
            {float a=i*Mathf.PI*2/steps,b=(i+1)*Mathf.PI*2/steps;Line(vh,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius);}
        }
        private void Path(VertexHelper vh,Vector2[] points){for(int i=1;i<points.Length;i++)Line(vh,points[i-1],points[i]);}
        private void Line(VertexHelper vh,Vector2 from,Vector2 to)
        {
            Rect r=GetPixelAdjustedRect();float scale=Mathf.Min(r.width,r.height)*.44f;
            Vector2 a=r.center+from*scale,b=r.center+to*scale,n=new Vector2(-(b-a).y,(b-a).x).normalized*1.15f;
            int start=vh.currentVertCount;
            vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
