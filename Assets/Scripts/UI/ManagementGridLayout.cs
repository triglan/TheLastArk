using UnityEngine;

namespace TheLastArk.UI
{
    [RequireComponent(typeof(UnityEngine.UI.GridLayoutGroup))]
    public sealed class ManagementGridLayout : MonoBehaviour
    {
        private float minimumWidth = 165, height = 184;
        private int maxColumns = 4;
        private float previousWidth = -1;

        public void Configure(float minWidth, float cellHeight, int columns)
        {
            minimumWidth = minWidth; height = cellHeight; maxColumns = columns;
            previousWidth = -1; Apply();
        }

        private void OnEnable() => Apply();
        private void OnRectTransformDimensionsChange() => Apply();
        private void Apply()
        {
            var rect = transform as RectTransform;
            if (rect == null || rect.rect.width <= 0 || Mathf.Approximately(rect.rect.width, previousWidth)) return;
            previousWidth = rect.rect.width;
            var grid = GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (grid == null) return;
            const float gap = 12;
            int columns = Mathf.Clamp(Mathf.FloorToInt((rect.rect.width + gap) / (minimumWidth + gap)), 1, maxColumns);
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.padding = new RectOffset(2, 2, 2, 2);
            grid.spacing = new Vector2(gap, gap);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.cellSize = new Vector2((rect.rect.width - 4 - gap * (columns - 1)) / columns, height);
        }
    }
}
