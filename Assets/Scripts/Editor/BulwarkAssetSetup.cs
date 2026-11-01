using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TheLastArk.Data;
using TheLastArk.Skills;

namespace TheLastArk.Editor
{
    public static class BulwarkAssetSetup
    {
        public const string AssetPath = "Assets/Resources/Characters/Player/Data/Bulwark_Data.asset";

        public static CharacterData CreateIfMissing()
        {
            var existing = AssetDatabase.LoadAssetAtPath<CharacterData>(AssetPath);
            if (existing != null) return existing;
            var data = ScriptableObject.CreateInstance<CharacterData>();
            data.characterId = "07"; data.characterName = "시안 페룸";
            data.jobName = "Bulwark"; data.jobDisplayName = "성채";
            data.synergies = new List<SynergyType> { SynergyType.ArchiumUnion, SynergyType.Guardian };
            data.baseAttack = 5; data.spellPower = 6; data.maxHp = 35; data.maxMental = 30;
            data.armor = 2; data.magicResist = 3; data.critRate = 0;
            data.standingSprite = Resources.Load<Sprite>("Characters/Player/Illust/Bulwark/Bulwark_Illust");
            data.useStandingPortrait = true; data.portraitFocus = new Vector2(.53f, .82f); data.portraitZoom = 3f;
            data.passiveSkill = Skill("철의 축전로", "magicShield");
            data.passiveSkill.passiveTrait = PassiveTrait.IronCapacitor;
            data.activeSkills = new[] { Skill("차폐막 전개", "shield"), Skill("전자기장", "magicShield"),
                Skill("전하 충격", "stun"), Skill("최후의 방주 프로토콜", "shield") };
            CrewSkillRevision.ApplyBulwark(data);
            AssetDatabase.CreateAsset(data, AssetPath);
            AssetDatabase.SaveAssetIfDirty(data);
            return data;
        }

        private static SkillInfo Skill(string name, string icon) => new SkillInfo
        { skillName = name, baseCost = 0, skillIcon = Resources.Load<Sprite>("Icons/" + icon), levels = new SkillLevelData[3] };
        private static SkillLevelData Level(CharacterData data, int slot, int rank, TargetType target)
        {
            var level = new SkillLevelData { targetType = target };
            data.activeSkills[slot].levels[rank] = level;
            return level;
        }
    }
}
