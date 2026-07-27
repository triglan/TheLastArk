using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace TheLastArk.UI
{
    public class NotificationManager : MonoBehaviour
    {
        private static NotificationManager instance;
        public static NotificationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<NotificationManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("NotificationManager");
                        instance = go.AddComponent<NotificationManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        private GameObject notificationCanvasObj;
        private TextMeshProUGUI notificationText;
        private Coroutine hideCoroutine;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void ShowMessage(string message, Color color)
        {
            Debug.Log($"[Notification] {message}");
            EnsureUI();

            if (notificationText != null)
            {
                notificationText.text = message;
                notificationText.color = color;
                notificationCanvasObj.SetActive(true);

                if (hideCoroutine != null) StopCoroutine(hideCoroutine);
                hideCoroutine = StartCoroutine(HideMessageAfterDelay(2.5f));
            }
        }

        private IEnumerator HideMessageAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (notificationCanvasObj != null)
            {
                notificationCanvasObj.SetActive(false);
            }
        }

        private void EnsureUI()
        {
            if (notificationCanvasObj != null && notificationText != null) return;

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject cObj = new GameObject("NotificationCanvas");
                canvas = cObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                cObj.AddComponent<CanvasScaler>();
                cObj.AddComponent<GraphicRaycaster>();
            }

            notificationCanvasObj = new GameObject("NotificationPanel");
            notificationCanvasObj.transform.SetParent(canvas.transform, false);
            notificationCanvasObj.transform.SetAsLastSibling();

            RectTransform rect = notificationCanvasObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.3f, 0.75f);
            rect.anchorMax = new Vector2(0.7f, 0.85f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = notificationCanvasObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            bg.raycastTarget = false;

            GameObject textObj = new GameObject("MessageText");
            textObj.transform.SetParent(notificationCanvasObj.transform, false);
            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            notificationText = textObj.AddComponent<TextMeshProUGUI>();
            notificationText.fontSize = 26;
            notificationText.alignment = TextAlignmentOptions.Center;
            notificationText.font = TMPFontManager.MainKoreanFont;
            notificationText.raycastTarget = false;

            notificationCanvasObj.SetActive(false);
        }
    }
}