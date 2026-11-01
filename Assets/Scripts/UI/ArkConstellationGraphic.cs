using TheLastArk.Data;
using TheLastArk.Run;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    public enum ArkVisual { Nebula, DistantStars, NearStars, Constellation, Star, Symbol }

    /// <summary>Native UI geometry: no per-frame textures, materials, or star GameObjects.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ArkConstellationGraphic : UnityEngine.UI.MaskableGraphic
    {
        public ArkVisual visual;
        public ArkNodeKind symbol;
        public bool major;
        public bool compact;
        public bool highlighted;
        public string selectedId;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            if (visual == ArkVisual.Nebula)
            {
                var random = new System.Random(417);
                for (int i = 0; i < 22; i++)
                {
                    float t = i / 21f;
                    Vector2 p = new Vector2(r.xMin + r.width * t, r.yMin + r.height * (.26f + t * .42f + (float)random.NextDouble() * .18f));
                    Color c = Color.Lerp(new Color(.12f,.19f,.42f,.24f), new Color(.24f,.10f,.35f,.20f), (float)random.NextDouble());
                    Glow(vh, p, new Vector2(r.width * .21f, r.height * .29f), c);
                }
                Glow(vh, new Vector2(r.xMin+r.width*.36f,r.yMin+r.height*.54f), new Vector2(r.width*.32f,r.height*.32f), new Color(.12f,.32f,.42f,.12f));
            }
            else if (visual == ArkVisual.DistantStars || visual == ArkVisual.NearStars)
            {
                bool near = visual == ArkVisual.NearStars;
                var random = new System.Random(near ? 144 : 732);
                int count = near ? 65 : 330;
                for (int i = 0; i < count; i++)
                {
                    Vector2 p = new Vector2(Mathf.Lerp(r.xMin,r.xMax,(float)random.NextDouble()),Mathf.Lerp(r.yMin,r.yMax,(float)random.NextDouble()));
                    float size = near ? 1.1f + (float)random.NextDouble()*1.7f : .5f + (float)random.NextDouble()*.8f;
                    Color c = new Color(.88f,.94f,1f, (near ? .25f : .10f) + (float)random.NextDouble()*.45f);
                    Glow(vh,p,Vector2.one*size*2.5f,new Color(c.r,c.g,c.b,c.a*.28f),2,8);
                    Diamond(vh,p,new Vector2(size,size),c);
                    if (near && i%5==0) { Line(vh,p-Vector2.right*size*3,p+Vector2.right*size*3,.6f,c); Line(vh,p-Vector2.up*size*4,p+Vector2.up*size*4,.6f,c); }
                }
            }
            else if (visual == ArkVisual.Constellation)
            {
                var config = ArkProgressionConfig.Load();
                var data = ArkProgression.Data;
                var route = new HashSet<string>();
                CollectRoute(config, selectedId, route);
                foreach (var node in config.nodes)
                {
                    if (node == null) continue;
                    if (node.prerequisites == null || node.prerequisites.Length == 0)
                        Connection(vh, r, ArkProgressionConfig.Origin, node, true, route.Contains(node.id));
                    else foreach (string id in node.prerequisites)
                    {
                        var parent = config.Find(id); if (parent == null) continue;
                        bool open = !parent.placeholder && data.Level(id) >= parent.MaxLevel;
                        Connection(vh, r, parent.position, node, open, route.Contains(node.id) && route.Contains(id));
                    }
                }
                var origin = r.min + Vector2.Scale(r.size, ArkProgressionConfig.Origin);
                Glow(vh, origin, Vector2.one * 22, new Color(.6f,.8f,1f,.3f));
                Diamond(vh, origin, new Vector2(3,9), new Color(.8f,.9f,1f,.9f));
                Diamond(vh, origin, new Vector2(9,3), new Color(.8f,.9f,1f,.9f));
            }
            else if (visual == ArkVisual.Star)
            {
                float radius=Mathf.Min(r.width,r.height)*.5f;
                if (compact)
                {
                    Glow(vh, r.center, Vector2.one * radius, color * new Color(1,1,1,.42f));
                    for (int i=0;i<4;i++)
                    {
                        float angle=i*Mathf.PI*.5f;
                        var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                        var side=new Vector2(-direction.y,direction.x)*radius*.055f;
                        Triangle(vh,r.center+direction*radius*.25f-side,r.center+direction*radius*(major?1f:.88f),r.center+direction*radius*.25f+side,color);
                    }
                    if (highlighted) Ring(vh,r.center,radius*.65f,.8f,color*new Color(1,1,1,.55f));
                    return;
                }
                Glow(vh,r.center,Vector2.one*radius,color*new Color(1,1,1,.25f));
                Glow(vh,r.center,Vector2.one*radius*.5f,color*new Color(1,1,1,.25f));
                Ring(vh,r.center,radius*.39f,1f,color*new Color(1,1,1,.7f));
                Ring(vh,r.center,radius*.47f,.7f,color*new Color(1,1,1,.22f));
                for(int i=0;i<4;i++)
                {
                    float angle=i*Mathf.PI*.5f;
                    Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                    Vector2 side=new Vector2(-direction.y,direction.x)*radius*.025f;
                    Triangle(vh,r.center+direction*radius*.45f-side,r.center+direction*radius*(major ? .94f : .75f),r.center+direction*radius*.45f+side,color*new Color(1,1,1,.85f));
                }
                if(major) for(int i=0;i<4;i++)
                {
                    float angle=(i+.5f)*Mathf.PI*.5f;var p=r.center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius*.48f;
                    Diamond(vh,p,Vector2.one*2f,color);
                }
            }
            else DrawSymbol(vh,r.center,Mathf.Min(r.width,r.height)*.43f);
        }

        private static void CollectRoute(ArkProgressionConfig config, string id, HashSet<string> route)
        {
            if (string.IsNullOrEmpty(id) || !route.Add(id)) return;
            var node = config.Find(id);
            if (node?.prerequisites != null) foreach (string parent in node.prerequisites) CollectRoute(config, parent, route);
        }

        private static void Connection(VertexHelper vh, Rect rect, Vector2 from, ArkNodeDefinition node, bool open, bool selected)
        {
            Vector2 a=rect.min+Vector2.Scale(rect.size,from), b=rect.min+Vector2.Scale(rect.size,node.position);
            Vector2 direction=(b-a).normalized;
            bool completed=!node.placeholder && ArkProgression.Data.Level(node.id)>=node.MaxLevel;
            Color tint=selected?new Color(.55f,.84f,1f,.8f):completed?new Color(.28f,.73f,1f,.6f):open?new Color(.85f,.92f,1f,.48f):new Color(.48f,.59f,.75f,.26f);
            Line(vh,a+direction*12,b-direction*(node.major?22:13),selected?1.7f:1.3f,tint);
            if (open && !completed) Diamond(vh,Vector2.Lerp(a,b,.55f),Vector2.one*1.7f,tint);
        }

        private void DrawSymbol(VertexHelper vh, Vector2 p, float s)
        {
            Color c=color; float w=Mathf.Max(1.6f,s*.08f);
            if(symbol==ArkNodeKind.LeaderHealth)
            {
                Vector2[] v={new Vector2(0,-.8f),new Vector2(-.85f,.1f),new Vector2(-.75f,.65f),new Vector2(-.35f,.8f),new Vector2(0,.45f),new Vector2(.35f,.8f),new Vector2(.75f,.65f),new Vector2(.85f,.1f),new Vector2(0,-.8f)};
                for(int i=1;i<v.Length;i++)Line(vh,p+v[i-1]*s,p+v[i]*s,w,c);
            }
            else if(symbol==ArkNodeKind.LeaderMental)
            {
                for(int i=0;i<32;i++){float t=i/32f*Mathf.PI*2, t2=(i+1)/32f*Mathf.PI*2;Line(vh,p+new Vector2(Mathf.Cos(t),Mathf.Sin(t)*.55f)*s,p+new Vector2(Mathf.Cos(t2),Mathf.Sin(t2)*.55f)*s,w,c);}Ring(vh,p,s*.25f,w,c);
            }
            else if(symbol==ArkNodeKind.LeaderArmor || symbol==ArkNodeKind.LeaderMagicResist)
            {
                Vector2[] v={new Vector2(0,1),new Vector2(.8f,.6f),new Vector2(.65f,-.4f),new Vector2(0,-1),new Vector2(-.65f,-.4f),new Vector2(-.8f,.6f),new Vector2(0,1)};
                for(int i=1;i<v.Length;i++)Line(vh,p+v[i-1]*s,p+v[i]*s,w,c);
                if(symbol==ArkNodeKind.LeaderMagicResist) Diamond(vh,p,new Vector2(.22f,.48f)*s,c);
            }
            else if(symbol==ArkNodeKind.StartingGold)
            {
                Ring(vh,p,s*.78f,w,c);Line(vh,p-Vector2.up*s*.45f,p+Vector2.up*s*.45f,w,c);
            }
            else if(symbol==ArkNodeKind.VictoryMemory)
            {
                Vector2 a=p+new Vector2(-.7f,-.5f)*s,b=p+new Vector2(0,.8f)*s,d=p+new Vector2(.8f,-.2f)*s;
                Line(vh,a,b,w,c);Line(vh,b,d,w,c);Diamond(vh,a,Vector2.one*s*.18f,c);Diamond(vh,b,Vector2.one*s*.18f,c);Diamond(vh,d,Vector2.one*s*.18f,c);
            }
            else if(symbol==ArkNodeKind.LeaderAttack)
            {
                Vector2 tip=p+new Vector2(.8f,.95f)*s, grip=p-new Vector2(.75f,.8f)*s;
                Line(vh,grip,tip,w,c);Line(vh,p+new Vector2(-.6f,.05f)*s,p+new Vector2(.15f,-.6f)*s,w,c);
                Line(vh,tip,p+new Vector2(.05f,.75f)*s,w,c);Line(vh,tip,p+new Vector2(.7f,.2f)*s,w,c);
            }
            else if(symbol==ArkNodeKind.LeaderSpellPower)
            {
                Line(vh,p-Vector2.up*s,p+Vector2.up*s*.3f,w,c);
                Diamond(vh,p+Vector2.up*s*.6f,new Vector2(.4f,.55f)*s,c);
                Line(vh,p+new Vector2(-.65f,.6f)*s,p+new Vector2(-.95f,.6f)*s,w,c);
                Line(vh,p+new Vector2(.65f,.6f)*s,p+new Vector2(.95f,.6f)*s,w,c);
            }
            else if(symbol==ArkNodeKind.Character)
            {
                Ring(vh,p+Vector2.up*s*.35f,s*.28f,w,c);
                Line(vh,p+new Vector2(-.65f,-.75f)*s,p+new Vector2(-.55f,-.25f)*s,w,c);
                Line(vh,p+new Vector2(-.55f,-.25f)*s,p+new Vector2(.55f,-.25f)*s,w,c);
                Line(vh,p+new Vector2(.55f,-.25f)*s,p+new Vector2(.65f,-.75f)*s,w,c);
                Line(vh,p+new Vector2(-.65f,-.75f)*s,p+new Vector2(.65f,-.75f)*s,w,c);
            }
            else if(symbol==ArkNodeKind.TrainCustomization)
            {
                Box(vh,p,new Vector2(s*.7f,s*.8f),w,c);Box(vh,p+Vector2.up*s*.25f,new Vector2(s*.42f,s*.25f),w,c);
                Ring(vh,p+new Vector2(-.4f,-.48f)*s,s*.10f,w,c);Ring(vh,p+new Vector2(.4f,-.48f)*s,s*.10f,w,c);
                Line(vh,p+new Vector2(-.6f,-.8f)*s,p+new Vector2(-.85f,-1f)*s,w,c);Line(vh,p+new Vector2(.6f,-.8f)*s,p+new Vector2(.85f,-1f)*s,w,c);
            }
            else if(symbol==ArkNodeKind.RelicSummon)
            {
                Vector2[] v={new Vector2(0,1),new Vector2(.7f,.3f),new Vector2(.5f,-.65f),new Vector2(0,-1),new Vector2(-.5f,-.65f),new Vector2(-.7f,.3f),new Vector2(0,1)};
                for(int i=1;i<v.Length;i++)Line(vh,p+v[i-1]*s,p+v[i]*s,w,c);
                Line(vh,p+Vector2.up*s,p-Vector2.up*s,w*.7f,c);Line(vh,p+new Vector2(-.7f,.3f)*s,p+new Vector2(.7f,.3f)*s,w*.7f,c);
            }
            else
            {
                Box(vh,p,Vector2.one*s*.72f,w,c);
                Diamond(vh,p,Vector2.one*s*.09f,c);
                Diamond(vh,p+Vector2.one*s*.35f,Vector2.one*s*.09f,c);Diamond(vh,p-Vector2.one*s*.35f,Vector2.one*s*.09f,c);
                if(symbol==ArkNodeKind.Reroll){Line(vh,p+new Vector2(-1,.8f)*s,p+new Vector2(.3f,1.1f)*s,w,c);Line(vh,p+new Vector2(.3f,1.1f)*s,p+new Vector2(0,1.3f)*s,w,c);}
                else {var q=p+new Vector2(.85f,.85f)*s;Line(vh,q-Vector2.right*s*.3f,q+Vector2.right*s*.3f,w,c);Line(vh,q-Vector2.up*s*.3f,q+Vector2.up*s*.3f,w,c);}
            }
        }

        private static void Box(VertexHelper vh,Vector2 p,Vector2 s,float w,Color c){Line(vh,p+new Vector2(-s.x,-s.y),p+new Vector2(s.x,-s.y),w,c);Line(vh,p+new Vector2(s.x,-s.y),p+s,w,c);Line(vh,p+s,p+new Vector2(-s.x,s.y),w,c);Line(vh,p+new Vector2(-s.x,s.y),p-s,w,c);}
        private static void Ring(VertexHelper vh,Vector2 p,float r,float width,Color c){for(int i=0;i<64;i++){float a=i/64f*Mathf.PI*2,b=(i+1)/64f*Mathf.PI*2;Line(vh,p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,p+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r,width,c);}}
        private static void Glow(VertexHelper vh,Vector2 p,Vector2 size,Color c,int rings=5,int segments=24)
        {
            for(int ring=0;ring<rings;ring++)for(int i=0;i<segments;i++)
            {
                float a=i/(float)segments*Mathf.PI*2,b=(i+1)/(float)segments*Mathf.PI*2,t0=ring/(float)rings,t1=(ring+1)/(float)rings;
                Color inner=c;inner.a*=Mathf.Pow(1-t0,2.4f);Color outer=c;outer.a*=Mathf.Pow(1-t1,2.4f);
                Vector2 d0=Vector2.Scale(new Vector2(Mathf.Cos(a),Mathf.Sin(a)),size),d1=Vector2.Scale(new Vector2(Mathf.Cos(b),Mathf.Sin(b)),size);
                Quad(vh,p+d0*t0,p+d0*t1,p+d1*t1,p+d1*t0,inner,outer,outer,inner);
            }
        }
        private static void Diamond(VertexHelper vh,Vector2 p,Vector2 s,Color c)=>Quad(vh,p+new Vector2(0,s.y),p+new Vector2(s.x,0),p-new Vector2(0,s.y),p-new Vector2(s.x,0),c,c,c,c);
        private static void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color tint){int start=vh.currentVertCount;vh.AddVert(a,tint,Vector2.zero);vh.AddVert(b,tint,Vector2.zero);vh.AddVert(c,tint,Vector2.zero);vh.AddTriangle(start,start+1,start+2);}
        private static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c){Vector2 d=(b-a).normalized;Vector2 n=new Vector2(-d.y,d.x)*width*.5f;Quad(vh,a+n,b+n,b-n,a-n,c,c,c,c);}
        private static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color ca,Color cb,Color cc,Color cd){int n=vh.currentVertCount;vh.AddVert(a,ca,Vector2.zero);vh.AddVert(b,cb,Vector2.zero);vh.AddVert(c,cc,Vector2.zero);vh.AddVert(d,cd,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }
}
