using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheLastArk.Data
{
    public enum SynergyType
    {
        // 세력 시너지
        ArchiumUnion,   // 아르키움 유니온 (2/4/6/8)
        Lionheart,      // 라이언하트 (3/5/7)
        Elysium,        // 엘리시움 (2/4/6/8)
        BlueTower,      // 푸른 마탑 (2/4/6)
        Elvenwood,      // 엘븐우드 대삼림 (2/4/6)
        Mirage,         // 신기루 (2/4/6/8)
        WhisperCult,    // 속삭임 교단 (3/5/7)
        Cheongwoon,     // 청운 (3/5/7)
        SilentArcadia,  // 침묵의 아르카디아 (1/2/3)

        // 직업 시너지
        Guardian,       // 수호자 (2/4)
        Warrior,        // 전사 (2/4)
        Assassin,       // 암살자 (2/4)
        Ranger,         // 사수 (2/4)
        Mage,           // 마술사 (2/4)
        Support,        // 지원가 (1/2/3)

        // 레거시 지원
        Defender,
        Steam,
        Mechanic,
        Vanguard
    }

    [Serializable]
    public class SynergyLevelBonus
    {
        public int requiredCount = 2;
        public float attackBonusPercent = 0.1f; // 10% 공격력 증가
        public float hpBonusPercent = 0.1f;     // 10% 최대 체력 증가
        public int bonusAP = 0;                 // 추가 AP
        public string description = "";
    }

    [CreateAssetMenu(fileName = "NewSynergyData", menuName = "TheLastArk/Synergy Data")]
    public class SynergyData : ScriptableObject
    {
        public SynergyType synergyType;
        public string displayName;
        public Sprite icon;
        [TextArea(2, 4)]
        public string description;
        public List<SynergyLevelBonus> levels = new List<SynergyLevelBonus>();

        public SynergyLevelBonus GetActiveLevelBonus(int count)
        {
            SynergyLevelBonus highest = null;
            foreach (var lvl in levels)
            {
                if (count >= lvl.requiredCount)
                {
                    if (highest == null || lvl.requiredCount > highest.requiredCount)
                    {
                        highest = lvl;
                    }
                }
            }
            return highest;
        }
    }
}
