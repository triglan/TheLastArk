using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BarUI : MonoBehaviour
{
    [Header("HP Controls")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;

    [Header("Mental Controls")]
    public Slider mentalSlider;
    public TextMeshProUGUI mentalText;

    // 데이터를 받아 UI를 한 번에 갱신합니다.
    public void UpdateAllBars(float curHp, float maxHp, float curMn, float maxMn)
    {
        if (hpSlider) hpSlider.value = curHp / maxHp;
        if (hpText) hpText.text = $"{curHp}/{maxHp}";

        if (mentalSlider) mentalSlider.value = curMn / maxMn;
        if (mentalText) mentalText.text = $"{curMn}/{maxMn}";
    }
}