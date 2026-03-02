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
            barUI.UpdateAllBars(status.currentHp, status.origin.maxHp,
                               status.currentMental, status.origin.maxMental);
        }
    }
}