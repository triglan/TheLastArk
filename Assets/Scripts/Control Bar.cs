using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header("UI References")] // 인스펙터 창을 예쁘게 정리해줍니다.
    public Slider hpSlider;
    public Slider mentalSlider;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI mentalText;

    [Header("Stats")]
    public float currentHp = 200f;
    public float maxHp = 200f;
    public float currentMental = 200f;
    public float maxMental = 200f;

    void Start()
    {
        UpdateUI();
    }

    public void LoseHp(float damage)
    {
        currentHp = Mathf.Clamp(currentHp - damage, 0, maxHp); // 한 줄로 깔끔하게 정리
        UpdateUI();
    }

    public void LoseMental(float amount)
    {
        currentMental = Mathf.Clamp(currentMental - amount, 0, maxMental);
        UpdateUI();
    }

    void UpdateUI()
    {
        // HP 업데이트: 슬라이더와 텍스트를 각각 안전하게 체크합니다.
        if (hpSlider != null) hpSlider.value = currentHp / maxHp;
        if (hpText != null) hpText.text = $"{(int)currentHp} / {(int)maxHp}"; // 문자열 보간법($) 사용

        // 멘탈 업데이트
        if (mentalSlider != null) mentalSlider.value = currentMental / maxMental;
        if (mentalText != null) mentalText.text = $"{(int)currentMental} / {(int)maxMental}";
    }
}