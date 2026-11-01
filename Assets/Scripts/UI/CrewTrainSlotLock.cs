using TMPro;
using UnityEngine;

namespace TheLastArk.UI
{
    /// <summary>Presentation-only lock; Ark progression remains the authority for customization.</summary>
    public sealed class CrewTrainSlotLock : MonoBehaviour
    {
        [SerializeField] private bool captured;
        [SerializeField] private Color unlockedColor;
        [SerializeField] private UnityEngine.UI.ColorBlock unlockedButtonColors;
        [SerializeField] private bool unlockedInteractable;
        [SerializeField] private string unlockedStatus;
        [SerializeField] private Color unlockedStatusColor;
        [SerializeField] private UnityEngine.UI.Image lockIcon;

        public void Apply(bool unlocked, Sprite sprite)
        {
            var image = GetComponent<UnityEngine.UI.Image>();
            var button = GetComponent<UnityEngine.UI.Button>();
            var status = transform.Find("CarStatus")?.GetComponent<TextMeshProUGUI>();
            if (image == null || button == null) return;
            if (!captured)
            {
                unlockedColor = image.color; unlockedButtonColors = button.colors;
                unlockedInteractable = button.interactable;
                if (status != null) { unlockedStatus = status.text; unlockedStatusColor = status.color; }
                captured = true;
            }
            if (lockIcon == null)
            {
                var go = new GameObject("CustomizationLock", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                go.transform.SetParent(transform, false);
                lockIcon = go.GetComponent<UnityEngine.UI.Image>();
                var rect = lockIcon.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .68f);
                rect.sizeDelta = new Vector2(30, 30);
                lockIcon.preserveAspect = true; lockIcon.raycastTarget = false;
            }
            lockIcon.sprite = sprite; lockIcon.color = new Color(.9f, .92f, .93f, 1);
            lockIcon.rectTransform.anchorMin = lockIcon.rectTransform.anchorMax = new Vector2(.5f, .68f);
            lockIcon.gameObject.SetActive(!unlocked); lockIcon.transform.SetAsLastSibling();
            image.color = unlocked ? unlockedColor : new Color(.43f, .46f, .48f, unlockedColor.a);
            var colors = unlockedButtonColors;
            if (!unlocked) colors.disabledColor = Color.white;
            button.colors = colors;
            button.interactable = unlocked && unlockedInteractable;
            if (status != null)
            {
                status.text = unlocked ? unlockedStatus : "방주에서 해금";
                status.color = unlocked ? unlockedStatusColor : new Color(.72f, .76f, .78f, 1);
            }
        }
    }
}
