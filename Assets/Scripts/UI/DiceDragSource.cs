using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    public sealed class DiceDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform _canvas;
        private RectTransform _ghost;
        private CanvasGroup _group;
        private Image _image;

        public int DieId { get; private set; }

        public void Configure(int dieId, RectTransform canvas)
        {
            DieId = dieId;
            _canvas = canvas;
            if (_group == null) _group = GetComponent<CanvasGroup>();
            if (_image == null) _image = GetComponent<Image>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!enabled || _canvas == null || _image == null) return;
            var preview = new GameObject("DraggedDie", typeof(RectTransform), typeof(Image));
            preview.transform.SetParent(_canvas, false);
            _ghost = preview.GetComponent<RectTransform>();
            _ghost.sizeDelta = new Vector2(72f, 72f);
            Image ghostImage = preview.GetComponent<Image>();
            ghostImage.sprite = _image.sprite;
            ghostImage.color = Color.white;
            ghostImage.raycastTarget = false;
            _group.blocksRaycasts = false;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_ghost == null) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas, eventData.position, eventData.pressEventCamera, out Vector2 position))
                _ghost.anchoredPosition = position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_group != null) _group.blocksRaycasts = true;
            if (_ghost != null) Destroy(_ghost.gameObject);
            _ghost = null;
        }

        private void OnDisable()
        {
            if (_group != null) _group.blocksRaycasts = true;
            if (_ghost != null) Destroy(_ghost.gameObject);
            _ghost = null;
        }
    }
}
