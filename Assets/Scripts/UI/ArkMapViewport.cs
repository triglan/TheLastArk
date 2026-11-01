using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class ArkMapViewport : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const float MinZoom = .6f, MaxZoom = 3f;
        private RectTransform viewport, content;
        private Vector2 pan, parallax, lastPointer, lastSize;
        private bool dragging;
        public float Zoom { get; private set; } = 1f;
        public Vector2 Pan => pan;
        public System.Action<float> ZoomChanged;

        public void Initialize(RectTransform board)
        {
            viewport = (RectTransform)transform; content = board;
            content.anchorMin = content.anchorMax = content.pivot = Vector2.one * .5f;
            UpdateSize(); Apply();
        }

        private void LateUpdate() { UpdateSize(); }
        private void OnDisable() { dragging = false; }
        private void UpdateSize()
        {
            if (viewport == null || content == null || lastSize == viewport.rect.size) return;
            lastSize = viewport.rect.size;
            content.sizeDelta = new Vector2(Mathf.Max(1,lastSize.x-64),Mathf.Max(1,lastSize.y-72));
            ClampPan(); Apply();
        }
        public void SetParallax(Vector2 offset) { parallax = dragging ? Vector2.zero : offset; Apply(); }
        public void ResetView() { Zoom = 1f; pan = Vector2.zero; Apply(); ZoomChanged?.Invoke(Zoom); }
        public void Focus(Vector2 normalizedPosition)
        {
            pan = -Vector2.Scale(content.rect.size, normalizedPosition-Vector2.one*.5f)*Zoom;
            ClampPan(); Apply();
        }
        public void ZoomAt(float factor, Vector2 localPoint)
        {
            float next = Mathf.Clamp(Zoom*factor,MinZoom,MaxZoom);
            pan = localPoint-(localPoint-pan-parallax)*(next/Zoom)-parallax;
            Zoom = next; ClampPan(); Apply(); ZoomChanged?.Invoke(Zoom);
        }
        public void OnScroll(PointerEventData e)
        {
            if(content==null || e.scrollDelta.y==0)return;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,e.position,e.enterEventCamera,out Vector2 point))
                ZoomAt(Mathf.Exp(e.scrollDelta.y*.15f),point);
            e.Use();
        }
        public void OnBeginDrag(PointerEventData e)
        {
            var hit=e.pointerPressRaycast.gameObject;
            dragging=e.button==PointerEventData.InputButton.Left && (hit==null || hit.GetComponentInParent<Button>()==null);
            if(dragging)RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,e.position,e.pressEventCamera,out lastPointer);
        }
        public void OnDrag(PointerEventData e)
        {
            if(!dragging || content==null)return;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,e.position,e.pressEventCamera,out Vector2 point))
            { pan+=point-lastPointer;lastPointer=point;ClampPan();Apply(); }
        }
        public void OnEndDrag(PointerEventData e) { dragging=false; }
        private void ClampPan()
        {
            if(content==null || viewport==null)return;
            Vector2 limit=(viewport.rect.size+content.rect.size*Zoom)*.5f-Vector2.one*100;
            pan=new Vector2(Mathf.Clamp(pan.x,-Mathf.Max(0,limit.x),Mathf.Max(0,limit.x)),Mathf.Clamp(pan.y,-Mathf.Max(0,limit.y),Mathf.Max(0,limit.y)));
        }
        private void Apply()
        {
            if(content==null)return;
            content.localScale=Vector3.one*Zoom;content.anchoredPosition=pan+parallax;
        }
    }
}
