using UnityEngine;

public class TargetArrow : MonoBehaviour
{
    public System.Action<GameObject> TargetSelected;
    public System.Action TargetCanceled;
    public RectTransform arrowUI;
    public GameObject target;

    [Header("Animation Settings")]
    public float floatSpeed = 5f;     // 움직이는 속도
    public float floatAmplitude = 10f; // 움직이는 높이 폭

    private Transform _cachedTargetPoint; // 현재 타겟의 화살표 위치 오브젝트

    void Update()
    {
        HandleMouseClick();
        if (target != null && arrowUI != null) AnimateArrow();
    }

    private void HandleMouseClick()// 마우스 클릭 시 타겟 변경
    {
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Deselect();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (Camera.main == null) return;
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null && (hit.collider.name.Contains("Player") || hit.collider.name.Contains("Enemy")))
            {
                target = hit.collider.gameObject;
                    if (arrowUI != null)
                    {
                arrowUI.gameObject.SetActive(true);
                    }

                _cachedTargetPoint = target.transform.Find("TargetPoint");
                TargetSelected?.Invoke(target);
            }
        }
    }
    
    private void AnimateArrow()
    {
        // 원래 좌표 계산
        //arrowUI.position = target.transform.position;

        // TargetPoint가 있으면 그 위치를 쓰고, 없으면 캐릭터의 transform 위치를 씁니다.
        Vector3 basePos = (_cachedTargetPoint != null) ? _cachedTargetPoint.position : target.transform.position;

        // UI 좌표 동기화
        arrowUI.position = basePos;

        // 둥실둥실 애니메이션
        float newY = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        arrowUI.anchoredPosition += new Vector2(0, newY);
    }
    public void Deselect(bool notify = true)
    {
        // 1. 현재 선택된 타겟 정보를 비웁니다.
        target = null;

        // 2. 화살표 시각적 요소를 숨깁니다. 
        // (만약 이미지를 끄는 변수명이 다르다면 그에 맞춰 수정하세요)
        if (arrowUI != null) arrowUI.gameObject.SetActive(false);
        _cachedTargetPoint = null;
        if (notify) TargetCanceled?.Invoke();

        Debug.Log("타겟 선택이 해제되었습니다.");
    }
}

