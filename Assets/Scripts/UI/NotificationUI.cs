using UnityEngine;
using TMPro;
using System.Collections;

namespace UI // 폴더 이름에 맞게 네임스페이스 지정 (선택 사항)
{
    public class NotificationUI : MonoBehaviour
    {
        public TextMeshProUGUI messageText;

        [Header("Animation Settings")]
        public float duration = 3.0f;     // 유지 시간
        public float moveSpeed = 200.0f;   // 위로 올라가는 속도
        public float fadeSpeed = 3.0f;    // 투명해지는 속도

        public void Show(string message, Color color)
        {
            if (messageText == null) messageText = GetComponentInChildren<TextMeshProUGUI>();

            messageText.text = message;
            messageText.color = color;
            StartCoroutine(NotificationRoutine());
        }

        private IEnumerator NotificationRoutine()
        {
            float elapsed = 0;
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;
            Color startColor = messageText.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // 1. 위로 이동
                transform.position = startPos + (Vector3.up * moveSpeed * progress);

                // 2. 점점 작아짐 (Slerp나 Lerp로 부드럽게)
                transform.localScale = Vector3.Lerp(startScale, startScale * 0.5f, progress);

                // 3. 투명해짐 (Fade Out)
                startColor.a = Mathf.Lerp(1, 0, progress * fadeSpeed);
                messageText.color = startColor;

                yield return null;
            }

            Destroy(gameObject); // 연출 종료 후 파괴
        }
    }
}