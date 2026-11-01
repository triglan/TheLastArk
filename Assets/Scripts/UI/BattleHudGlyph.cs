using UnityEngine;

namespace TheLastArk.UI
{
    // Resolution-independent HUD symbols: no font glyph or external texture dependency.
    [RequireComponent(typeof(CanvasRenderer))]
    public class BattleHudGlyph : UnityEngine.UI.MaskableGraphic
    {
        public enum Symbol { Energy, EndTurn, Map, EnemyTarget, SelectedTarget }
        public Symbol symbol;

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            if (symbol == Symbol.Energy)
            {
                Triangle(mesh, new Vector2(.58f,.95f), new Vector2(.16f,.43f), new Vector2(.51f,.43f));
                Triangle(mesh, new Vector2(.42f,.05f), new Vector2(.84f,.57f), new Vector2(.49f,.57f));
                Quad(mesh, new Vector2(.43f,.43f), new Vector2(.51f,.43f), new Vector2(.57f,.57f), new Vector2(.49f,.57f));
            }
            else if (symbol == Symbol.EndTurn)
            {
                for (int i = 0; i < 42; i++)
                {
                    float a = (55f + i * 280f / 42f) * Mathf.Deg2Rad;
                    float b = (55f + (i+1) * 280f / 42f) * Mathf.Deg2Rad;
                    Quad(mesh, Ring(a,.35f), Ring(a,.43f), Ring(b,.43f), Ring(b,.35f));
                }
                Triangle(mesh,new Vector2(.95f,.57f),new Vector2(.70f,.55f),new Vector2(.85f,.30f));
                Triangle(mesh,new Vector2(.43f,.34f),new Vector2(.43f,.66f),new Vector2(.66f,.5f));
            }
            else if (symbol == Symbol.EnemyTarget)
            {
                DrawDisc(mesh, .50f, new Color(.05f,.025f,.035f,1f));
                DrawDisc(mesh, .44f, new Color(.96f,.28f,.29f,1f));
                DrawDisc(mesh, .34f, new Color(.24f,.025f,.045f,1f));
                Quad(mesh, new Vector2(.41f,.43f), new Vector2(.59f,.43f), new Vector2(.59f,.77f), new Vector2(.41f,.77f));
                Quad(mesh, new Vector2(.41f,.23f), new Vector2(.59f,.23f), new Vector2(.59f,.36f), new Vector2(.41f,.36f));
            }
            else if (symbol == Symbol.SelectedTarget)
            {
                Triangle(mesh, new Vector2(.03f,.88f), new Vector2(.97f,.88f), new Vector2(.5f,.06f), new Color(.035f,.045f,.06f,1f));
                Triangle(mesh, new Vector2(.17f,.79f), new Vector2(.83f,.79f), new Vector2(.5f,.20f));
            }
            else
            {
                Quad(mesh,new Vector2(.08f,.18f),new Vector2(.34f,.08f),new Vector2(.34f,.78f),new Vector2(.08f,.9f));
                Quad(mesh,new Vector2(.40f,.08f),new Vector2(.64f,.2f),new Vector2(.64f,.92f),new Vector2(.40f,.78f));
                Quad(mesh,new Vector2(.70f,.2f),new Vector2(.92f,.1f),new Vector2(.92f,.82f),new Vector2(.70f,.92f));
            }
        }
        private static Vector2 Ring(float a,float radius) => new Vector2(.5f+Mathf.Cos(a)*radius,.5f+Mathf.Sin(a)*radius);
        private void Vertex(UnityEngine.UI.VertexHelper mesh, Vector2 p)
        {
            Rect r = rectTransform.rect;
            mesh.AddVert(new Vector3(r.xMin+p.x*r.width,r.yMin+p.y*r.height,0),color,Vector2.zero);
        }
        private void Triangle(UnityEngine.UI.VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c)
        {
            int i=mesh.currentVertCount; Vertex(mesh,a); Vertex(mesh,b); Vertex(mesh,c); mesh.AddTriangle(i,i+1,i+2);
        }
        private void Triangle(UnityEngine.UI.VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Color tint)
        {
            Rect r=rectTransform.rect;
            int i=mesh.currentVertCount;
            mesh.AddVert(new Vector3(r.xMin+a.x*r.width,r.yMin+a.y*r.height,0f),tint,Vector2.zero);
            mesh.AddVert(new Vector3(r.xMin+b.x*r.width,r.yMin+b.y*r.height,0f),tint,Vector2.zero);
            mesh.AddVert(new Vector3(r.xMin+c.x*r.width,r.yMin+c.y*r.height,0f),tint,Vector2.zero);
            mesh.AddTriangle(i,i+1,i+2);
        }
        private void DrawDisc(UnityEngine.UI.VertexHelper mesh,float radius,Color tint)
        {
            const int segments=32;
            for(int i=0;i<segments;i++)
                Triangle(mesh,new Vector2(.5f,.5f),Ring(i*Mathf.PI*2/segments,radius),Ring((i+1)*Mathf.PI*2/segments,radius),tint);
        }
        private void Quad(UnityEngine.UI.VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            int i=mesh.currentVertCount; Vertex(mesh,a); Vertex(mesh,b); Vertex(mesh,c); Vertex(mesh,d);
            mesh.AddTriangle(i,i+1,i+2); mesh.AddTriangle(i,i+2,i+3);
        }
    }
}
