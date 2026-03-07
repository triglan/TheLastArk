using UnityEngine;

namespace UI
{
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance;

        public GameObject notificationPrefab; // BattleNotification 스크립트가 붙은 프리팹
        public Transform canvasTransform;     // UI 상위 캔버스

        private void Awake()
        {
            // 싱글톤 초기화
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void ShowMessage(string message, Color color)
        {
            if (notificationPrefab == null || canvasTransform == null)
            {
                Debug.LogWarning("NotificationManager: 프리팹이나 캔버스가 연결되지 않았습니다.");
                return;
            }

            GameObject obj = Instantiate(notificationPrefab, canvasTransform);

            // 화면 정중앙(0,0)에 생성
            obj.transform.localPosition = Vector3.zero;

            NotificationUI noti = obj.GetComponent<NotificationUI>();
            if (noti != null)
            {
                noti.Show(message, color);
            }
        }
    }
}