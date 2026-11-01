using TheLastArk.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    /// <summary>Small line emblems, used until a synergy supplies its own icon asset.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CrewSynergyGlyph : MaskableGraphic
    {
        [SerializeField] private SynergyType synergy;
        public void SetSynergy(SynergyType value) { synergy=value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            switch(synergy)
            {
                case SynergyType.Guardian: case SynergyType.Defender:
                    Path(vh,.2f,.82f,.5f,.9f,.8f,.82f,.75f,.36f,.5f,.12f,.25f,.36f,.2f,.82f); Line(vh,.5f,.75f,.5f,.3f); break;
                case SynergyType.Warrior: case SynergyType.Vanguard:
                    Line(vh,.2f,.2f,.8f,.8f);Line(vh,.2f,.8f,.8f,.2f);Line(vh,.19f,.38f,.38f,.19f);Line(vh,.62f,.19f,.81f,.38f);break;
                case SynergyType.Assassin:
                    Path(vh,.48f,.13f,.48f,.38f,.3f,.38f,.7f,.38f,.48f,.38f,.35f,.67f,.55f,.9f,.65f,.67f,.48f,.38f);break;
                case SynergyType.Ranger:
                    Path(vh,.3f,.12f,.56f,.3f,.65f,.5f,.56f,.7f,.3f,.88f,.3f,.12f);Line(vh,.15f,.5f,.88f,.5f);Path(vh,.7f,.67f,.88f,.5f,.7f,.33f);break;
                case SynergyType.Mage:
                    Line(vh,.5f,.13f,.5f,.58f);Path(vh,.5f,.92f,.7f,.72f,.5f,.52f,.3f,.72f,.5f,.92f);Line(vh,.2f,.35f,.32f,.35f);Line(vh,.75f,.52f,.88f,.52f);break;
                case SynergyType.Support:
                    Path(vh,.39f,.86f,.61f,.86f,.61f,.61f,.86f,.61f,.86f,.39f,.61f,.39f,.61f,.14f,.39f,.14f,.39f,.39f,.14f,.39f,.14f,.61f,.39f,.61f,.39f,.86f);break;
                case SynergyType.ArchiumUnion: case SynergyType.Mechanic: case SynergyType.Steam:
                    Circle(vh,.5f,.5f,.25f);Circle(vh,.5f,.5f,.1f);
                    for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(vh,.5f+Mathf.Cos(a)*.26f,.5f+Mathf.Sin(a)*.26f,.5f+Mathf.Cos(a)*.39f,.5f+Mathf.Sin(a)*.39f);}break;
                case SynergyType.Lionheart:
                    Path(vh,.2f,.3f,.12f,.74f,.36f,.56f,.5f,.9f,.64f,.56f,.88f,.74f,.8f,.3f,.2f,.3f);Line(vh,.25f,.15f,.75f,.15f);break;
                case SynergyType.Elvenwood:
                    Path(vh,.5f,.9f,.22f,.57f,.36f,.57f,.15f,.3f,.85f,.3f,.64f,.57f,.78f,.57f,.5f,.9f);Line(vh,.5f,.3f,.5f,.1f);break;
                case SynergyType.Mirage:
                    Circle(vh,.5f,.66f,.19f);Line(vh,.1f,.38f,.9f,.38f);Path(vh,.15f,.18f,.35f,.25f,.55f,.18f,.8f,.25f);break;
                case SynergyType.BlueTower:
                    Path(vh,.23f,.13f,.77f,.13f,.68f,.63f,.8f,.63f,.5f,.9f,.2f,.63f,.32f,.63f,.23f,.13f);Line(vh,.5f,.24f,.5f,.46f);break;
                case SynergyType.WhisperCult:
                    Path(vh,.1f,.5f,.3f,.7f,.5f,.78f,.7f,.7f,.9f,.5f,.7f,.3f,.5f,.22f,.3f,.3f,.1f,.5f);Circle(vh,.5f,.5f,.14f);break;
                case SynergyType.Cheongwoon:
                    Path(vh,.1f,.3f,.45f,.85f,.65f,.55f,.78f,.7f,.94f,.3f,.1f,.3f);Line(vh,.2f,.15f,.8f,.15f);break;
                default:
                    Path(vh,.5f,.92f,.62f,.62f,.92f,.5f,.62f,.38f,.5f,.08f,.38f,.38f,.08f,.5f,.38f,.62f,.5f,.92f);break;
            }
        }
        private void Path(VertexHelper vh,params float[] points)
        { for(int i=2;i<points.Length;i+=2)Line(vh,points[i-2],points[i-1],points[i],points[i+1]); }
        private void Circle(VertexHelper vh,float x,float y,float radius)
        { for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Line(vh,x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius);} }
        private void Line(VertexHelper vh,float x0,float y0,float x1,float y1)
        {
            Rect r=GetPixelAdjustedRect();float side=Mathf.Min(r.width,r.height);Vector2 origin=r.center-Vector2.one*side*.5f;
            Vector2 a=origin+new Vector2(x0,y0)*side,b=origin+new Vector2(x1,y1)*side;
            Vector2 d=b-a;if(d.sqrMagnitude<.001f)return;
            Vector2 normal=new Vector2(-d.y,d.x).normalized*Mathf.Max(.65f,side*.025f);
            int n=vh.currentVertCount;vh.AddVert(a-normal,color,Vector2.zero);vh.AddVert(a+normal,color,Vector2.zero);
            vh.AddVert(b+normal,color,Vector2.zero);vh.AddVert(b-normal,color,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
    }
}
