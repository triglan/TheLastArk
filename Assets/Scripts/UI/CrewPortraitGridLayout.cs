using UnityEngine;

namespace TheLastArk.UI
{
    /// <summary>Four columns and two rows of crew, paged without disabling character objects or their bindings.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(UnityEngine.UI.GridLayoutGroup))]
    public sealed class CrewPortraitGridLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private UnityEngine.UI.ScrollRect scroll;
        [SerializeField] private UnityEngine.UI.Button previous, next;
        [SerializeField] private TMPro.TextMeshProUGUI pageLabel;
        private const int Columns = 4, PageSize = 8;
        private int currentPage;
        private int pageCount = 1;
        private bool applying;
        private void OnEnable() { BindControls(); ApplyLayout(); }
        private void OnRectTransformDimensionsChange() => ApplyLayout();
        private void OnTransformChildrenChanged() => ApplyLayout();

        public void Configure(RectTransform view, UnityEngine.UI.ScrollRect scroller,
            UnityEngine.UI.Button back, UnityEngine.UI.Button forward, TMPro.TextMeshProUGUI counter = null)
        {
            viewport = view; scroll = scroller; previous = back; next = forward;
            pageLabel = counter;
            BindControls();
            ApplyLayout();
        }

        private void BindControls()
        {
            if (previous != null) { previous.onClick.RemoveAllListeners(); previous.onClick.AddListener(() => Move(-1)); }
            if (next != null) { next.onClick.RemoveAllListeners(); next.onClick.AddListener(() => Move(1)); }
        }

        private void Move(int direction)
        {
            currentPage = Mathf.Clamp(currentPage + direction, 0, pageCount - 1);
            ApplyLayout();
        }

        private void RefreshArrows()
        {
            if (previous != null) previous.interactable = currentPage > 0;
            if (next != null) next.interactable = currentPage < pageCount - 1;
            if (pageLabel != null) pageLabel.text = (currentPage + 1) + "/" + pageCount;
        }

        public void EnsureVisible(RectTransform card)
        {
            if (card == null || viewport == null) return;
            int index = 0;
            foreach (Transform child in transform)
            {
                if (child == card) break;
                if (child.gameObject.activeSelf) index++;
            }
            currentPage = index / PageSize;
            ApplyLayout();
        }

        public void ApplyLayout()
        {
            if (applying || viewport == null) return;
            var rect = transform as RectTransform;
            var grid = GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (rect == null || grid == null || viewport.rect.width <= 0 || viewport.rect.height <= 0) return;
            applying = true;
            const float gap = 10;
            int count = 0;
            foreach (Transform child in transform) if (child.gameObject.activeSelf) count++;
            pageCount = Mathf.Max(1, Mathf.CeilToInt(count / (float)PageSize));
            currentPage = Mathf.Clamp(currentPage, 0, pageCount - 1);
            float width = Mathf.Max(1, (viewport.rect.width - gap * (Columns - 1)) / Columns);
            float height = Mathf.Max(1, Mathf.Min(width*1.15f, (viewport.rect.height - gap) / 2));
            int index = 0;
            foreach (Transform child in transform)
            {
                if (!child.gameObject.activeSelf) continue;
                bool visible = index++ / PageSize == currentPage;
                var element = child.GetComponent<UnityEngine.UI.LayoutElement>();
                if (element == null) element = child.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                element.ignoreLayout = !visible;
                var visibility = child.GetComponent<CanvasGroup>();
                if (visibility == null) visibility = child.gameObject.AddComponent<CanvasGroup>();
                visibility.alpha = visible ? 1 : 0;
                visibility.interactable = visible;
                visibility.blocksRaycasts = visible;
            }
            grid.padding = new RectOffset();
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.startCorner = UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = UnityEngine.UI.GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.spacing = new Vector2(gap, gap);
            grid.cellSize = new Vector2(width, height);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            if (scroll != null) { scroll.StopMovement(); scroll.enabled = false; }
            applying = false;
            RefreshArrows();
        }
    }
}
