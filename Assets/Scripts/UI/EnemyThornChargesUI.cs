using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    public sealed class EnemyThornChargesUI : Graphic
    {
        private int count;
        public void SetCount(int value)
        {
            value = Mathf.Clamp(value, 0, 5);
            gameObject.SetActive(value > 0);
            if (count == value) return;
            count = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            for (int i = 0; i < count; i++)
            {
                float x = (i - (count - 1) * 0.5f) * 18f;
                int start = mesh.currentVertCount;
                mesh.AddVert(new Vector3(x - 6f, -8f), color, Vector2.zero);
                mesh.AddVert(new Vector3(x, 10f), color, Vector2.zero);
                mesh.AddVert(new Vector3(x + 6f, -8f), color, Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2);
            }
        }
    }
}
