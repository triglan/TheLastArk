using UnityEngine;
using UnityEngine.EventSystems;

namespace TheLastArk.UI
{
    public class HudHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public GameObject hint;
        public void OnPointerEnter(PointerEventData e) { Show(true); }
        public void OnPointerExit(PointerEventData e) { Show(false); }
        public void OnSelect(BaseEventData e) { Show(true); }
        public void OnDeselect(BaseEventData e) { Show(false); }
        private void OnDisable() { Show(false); }
        private void Show(bool value) { if (hint != null) hint.SetActive(value); }
    }
}
