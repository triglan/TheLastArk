using UnityEngine;
using UnityEngine.UI;

public class CharacterView : MonoBehaviour
{
    public bool isStandingIllust = false;

    [Header("Manual Setup")]
    public Image displayImage;
    public BarUI barUI;

    // 이제 데이터를 생성하지 않고, 받은 데이터를 그리기만 합니다.
    public void UpdateVisual(CharacterStatus status)
    {
        if (displayImage != null && status.origin != null)
        {
            displayImage.sprite = isStandingIllust ? status.origin.standingSprite : status.origin.portraitSprite;
        }

        if (barUI != null && status.origin != null)
        {
            // [FIX 5] 강화 배율이 적용된 FinalMaxHp / FinalMaxMental을 사용
            // 이전 코드(status.origin.maxHp)는 강화 단계를 무시하여 바가 잘못 표시됨
            barUI.UpdateAllBars(status.currentHp, status.FinalMaxHp,
                               status.currentMental, status.FinalMaxMental);
        }
    }
}