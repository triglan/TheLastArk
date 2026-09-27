using UnityEngine;

namespace TheLastArk.UI
{
    // Resolution-independent HUD symbols: no font glyph or external texture dependency.
    [RequireComponent(typeof(CanvasRenderer))]
    public class BattleHudGlyph : UnityEngine.UI.MaskableGraphic
    {
        public enum Symbol { Energy, EndTurn, Map }
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
        private void Quad(UnityEngine.UI.VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            int i=mesh.currentVertCount; Vertex(mesh,a); Vertex(mesh,b); Vertex(mesh,c); Vertex(mesh,d);
            mesh.AddTriangle(i,i+1,i+2); mesh.AddTriangle(i,i+2,i+3);
        }
    }
}
