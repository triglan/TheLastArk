using UnityEngine;

public class TargetArrow : MonoBehaviour
{
    public RectTransform arrowUI;
    public GameObject target;

    [Header("Animation Settings")]
    public float floatSpeed = 5f;     // 움직이는 속도
    public float floatAmplitude = 10f; // 움직이는 높이 폭
    public float yOffset = 70f;       // 기본 머리 위 높이

    void Start()
    {
        // 1. 씬에서 "Enemy 1"이라는 이름을 가진 오브젝트를 찾습니다.
        // 2. 찾았다면 타겟으로 설정하고 화살표 위치를 갱신합니다.
        GameObject firstEnemy = GameObject.Find("Enemy 1");
        if (firstEnemy != null) target = firstEnemy; 
        else if (arrowUI != null) arrowUI.gameObject.SetActive(false);
    }

    void Update()
    {
        HandleMouseClick();
        if (target != null && arrowUI != null) AnimateArrow();
    }

    private void HandleMouseClick()// 마우스 클릭 시 타겟 변경
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null && hit.collider.name.Contains("Enemy"))
            {
                target = hit.collider.gameObject;
                arrowUI.gameObject.SetActive(true);
            }
        }
    }
    private void AnimateArrow()
    {
        // 1. 기본 위치: 타겟의 현재 월드 좌표를 UI 좌표로 동기화
        arrowUI.position = target.transform.position;

        // 2. 둥실둥실 계산: Sin 함수를 이용해 시간에 따른 높이 변화를 줍니다.
        // Mathf.Sin(Time.time * 속도) * 높이
        float newY = yOffset + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;

        // 3. AnchoredPosition을 통해 로컬 오프셋 적용
        arrowUI.anchoredPosition += new Vector2(0, newY);
    }
}

