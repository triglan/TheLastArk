using UnityEngine;
using UnityEngine.UI;

public class CharacterView : MonoBehaviour
{
    public bool isStandingIllust = false;

    [Header("수동 연결")]
    public Image displayImage;
    public BarUI barUI;

    // 이제 데이터를 생성하지 않고, 받은 데이터를 그리기만 합니다.
    public void UpdateVisual(CharacterStatus status)
    {
        if (displayImage != null && status.origin != null)
        {
            displayImage.sprite = (status.origin.isEnemy || isStandingIllust)
                ? status.origin.standingSprite
                : status.origin.portraitSprite;
        }

        if (barUI != null && status.origin != null)
        {
            // 강화 배율이 적용된 최종 체력과 정신력을 바에 표시합니다.
            barUI.UpdateAllBars(status.currentHp, status.FinalMaxHp,
                               status.currentMental, status.FinalMaxMental);
        }
    }
}
