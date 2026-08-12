using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TheLastArk.UI
{
    /// <summary>
    /// 배틀 씬에서 상단바 지도 버튼을 클릭했을 때 표시되는 읽기 전용 맵 팝업 UI입니다.
    /// 전투 진행 중에도 맵 진행 상황 및 현재 위치를 확인할 수 있으며, 노드 이동은 불가능합니다.
    /// </summary>
    public class BattleMapPopupUI : MonoBehaviour
    {
        private static BattleMapPopupUI instance;
        private GameObject popupPanel;
        private MapManager mapManager;

        public static void Show()
        {
            if (instance == null)
            {
                GameObject go = new GameObject("BattleMapPopupUI");
                instance = go.AddComponent<BattleMapPopupUI>();
                DontDestroyOnLoad(go);
            }

            instance.OpenPopup();
        }

        public static void Hide()
        {
            if (instance != null && instance.popupPanel != null)
            {
                instance.popupPanel.SetActive(false);
            }
        }

        private void OpenPopup()
        {
            if (popupPanel == null)
            {
                CreatePopupUI();
            }
            else
            {
                popupPanel.SetActive(true);
                popupPanel.transform.SetAsLastSibling();
                if (mapManager != null)
                {
                    mapManager.ScrollToCurrentNode();
                }
            }
        }

        private void CreatePopupUI()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[BattleMapPopupUI] Canvas를 찾을 수 없습니다.");
                return;
            }

            // 1. 전체 화면 오버레이 루트
            popupPanel = new GameObject("BattleMapPopupPanel");
            popupPanel.transform.SetParent(canvas.transform, false);
            popupPanel.transform.SetAsLastSibling();

            RectTransform mainRect = popupPanel.AddComponent<RectTransform>();
            mainRect.anchorMin = Vector2.zero;
            mainRect.anchorMax = Vector2.one;
            mainRect.offsetMin = Vector2.zero;
            mainRect.offsetMax = Vector2.zero;

            // 반투명 어두운 배경 (뒤쪽 배틀 UI 클릭 방지)
            Image bg = popupPanel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);
            bg.raycastTarget = true;

            // 2. 맵 표시 컨테이너 (상단 헤더 아래 영역 차지)
            GameObject mapContainerObj = new GameObject("MapContainer");
            mapContainerObj.transform.SetParent(popupPanel.transform, false);

            RectTransform mapContainerRect = mapContainerObj.AddComponent<RectTransform>();
            mapContainerRect.anchorMin = Vector2.zero;
            mapContainerRect.anchorMax = Vector2.one;
            mapContainerRect.offsetMin = Vector2.zero;
            mapContainerRect.offsetMax = new Vector2(0, -60); // 상단 60px은 헤더 패널용

            // 3. MapManager 생성 및 읽기 전용 설정
            mapManager = mapContainerObj.AddComponent<MapManager>();
            mapManager.isReadOnly = true;
            mapManager.customCanvasTransform = mapContainerObj.transform;

            // 4. 상단 헤더 패널 (타이틀 & 닫기 버튼)
            GameObject headerObj = new GameObject("PopupHeader");
            headerObj.transform.SetParent(popupPanel.transform, false);
            headerObj.transform.SetAsLastSibling();

            RectTransform headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 60);

            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color(0.12f, 0.12f, 0.18f, 0.98f);

            TMP_FontAsset mainFont = Resources.Load<TMP_FontAsset>("Fonts/Main_Fonts");

            // 타이틀 텍스트
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(25, 0);
            titleRect.offsetMax = new Vector2(-80, 0);

            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "🗺️ 탐사 지도 현황 (전투 진행 중 - 이동 불가)";
            titleTmp.fontSize = 22;
            titleTmp.color = new Color(1f, 0.85f, 0.3f);
            titleTmp.alignment = TextAlignmentOptions.Left;
            if (mainFont != null) titleTmp.font = mainFont;

            // 닫기 버튼 (X)
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(headerObj.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 0.5f);
            closeRect.anchorMax = new Vector2(1, 0.5f);
            closeRect.pivot = new Vector2(1, 0.5f);
            closeRect.anchoredPosition = new Vector2(-15, 0);
            closeRect.sizeDelta = new Vector2(40, 40);

            Image closeBg = closeBtnObj.AddComponent<Image>();
            closeBg.color = new Color(0.75f, 0.2f, 0.2f, 1f);

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => Hide());

            GameObject closeTextObj = new GameObject("CloseText");
            closeTextObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform closeTextRect = closeTextObj.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI closeTmp = closeTextObj.AddComponent<TextMeshProUGUI>();
            closeTmp.text = "✕";
            closeTmp.fontSize = 22;
            closeTmp.color = Color.white;
            closeTmp.alignment = TextAlignmentOptions.Center;
            if (mainFont != null) closeTmp.font = mainFont;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
