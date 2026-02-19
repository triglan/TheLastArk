using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class TargetHandler : MonoBehaviour
{
    public RectTransform arrowUI;
    public GameObject currentTarget;

    void Start()
    {
        // 1. 씬에서 "Enemy 1"이라는 이름을 가진 오브젝트를 찾습니다.
        GameObject firstEnemy = GameObject.Find("Enemy 1");

        if (firstEnemy != null)
        {
            // 2. 찾았다면 타겟으로 설정하고 화살표 위치를 갱신합니다.
            currentTarget = firstEnemy;
            UpdateArrowPosition();
        }
        else
        {
            // 만약 시작할 때 화살표를 숨기고 싶다면
            if (arrowUI != null) arrowUI.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // 마우스 클릭 시 타겟 변경 (기존 코드 유지)
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null &&
                hit.collider.name.Contains("Enemy"))
            {
                currentTarget = hit.collider.gameObject;
                UpdateArrowPosition();
            }
        }
    }

    public void UpdateArrowPosition()
    {
        if (currentTarget == null || arrowUI == null) return;

        arrowUI.gameObject.SetActive(true);

        // Canvas가 "Screen Space - Camera" 모드이므로 transform.position을 그대로 사용 가능합니다.
        arrowUI.position = currentTarget.transform.position;

        // 적 캐릭터의 머리 위로 화살표 올리기 (수치는 캐릭터 크기에 맞춰 조정)
        arrowUI.anchoredPosition += new Vector2(0, 70f);
    }
}


//using UnityEngine;
//using UnityEngine.EventSystems; // UI 클릭 감지를 위해 필요
//using System.Collections.Generic;

//public class TargetHandler : MonoBehaviour
//{
//    public RectTransform arrowUI;
//    public GameObject currentTarget;

//    void Update()
//    {
//        if (Input.GetMouseButtonDown(0))
//        {
//            // 마우스 위치에 있는 모든 UI 요소를 검사합니다.
//            PointerEventData eventData = new PointerEventData(EventSystem.current);
//            eventData.position = Input.mousePosition;
//            List<RaycastResult> results = new List<RaycastResult>();
//            EventSystem.current.RaycastAll(eventData, results);

//            foreach (RaycastResult result in results)
//            {
//                // 클릭한 UI 이름에 "Enemy"가 들어있다면
//                if (result.gameObject.name.Contains("Enemy"))
//                {
//                    currentTarget = result.gameObject;
//                    UpdateArrowPosition();
//                    break;
//                }
//            }
//        }
//    }

//    void UpdateArrowPosition()
//    {
//        if (currentTarget == null || arrowUI == null) return;
//        arrowUI.gameObject.SetActive(true);

//        // UI 캐릭터의 정중앙 좌표를 화살표에 그대로 대입합니다.
//        arrowUI.position = currentTarget.transform.position;

//        arrowUI.anchoredPosition += new Vector2(0, 100f);
//    }
//}