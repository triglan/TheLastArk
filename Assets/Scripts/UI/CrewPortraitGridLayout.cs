using UnityEngine;

namespace TheLastArk.UI
{
    /// <summary>Keep portrait cells square when the canvas or game-view aspect ratio changes.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(UnityEngine.UI.GridLayoutGroup))]
    public sealed class CrewPortraitGridLayout : MonoBehaviour
    {
        private void OnEnable() => ApplyLayout();
        private void OnRectTransformDimensionsChange() => ApplyLayout();

        public void ApplyLayout()
        {
            var rect = transform as RectTransform;
            var grid = GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (rect == null || grid == null || rect.rect.width <= 0) return;
            const int columns = 3;
            const float gap = 14;
            float side = Mathf.Max(1, (rect.rect.width - grid.padding.horizontal - gap * (columns - 1)) / columns);
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.startCorner = UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = UnityEngine.UI.GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.spacing = new Vector2(gap, gap);
            grid.cellSize = new Vector2(side, side);
        }
    }
}
