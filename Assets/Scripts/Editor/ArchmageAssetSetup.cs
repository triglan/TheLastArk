using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TheLastArk.Data;
using TheLastArk.Skills;

namespace TheLastArk.Editor
{
    public static class ArchmageAssetSetup
    {
        public const string AssetPath = "Assets/Resources/Characters/Player/Data/Archmage_Data.asset";

        // Create-only: preserve future adjustments in the character editor.
        public static CharacterData CreateIfMissing()
        {
            var existing = AssetDatabase.LoadAssetAtPath<CharacterData>(AssetPath);
            if (existing != null) return existing;
            var data = ScriptableObject.CreateInstance<CharacterData>();
            data.characterId = "05";
            data.characterName = "멀린";
            data.jobName = "Archmage";
            data.jobDisplayName = "원소술사";
            data.synergies = new List<SynergyType> { SynergyType.BlueTower, SynergyType.Mage };
            data.baseAttack = 0; data.spellPower = 7;
            data.maxHp = 25; data.maxMental = 40;
            data.armor = 1; data.magicResist = 3; data.critRate = 30;
            data.standingSprite = Resources.Load<Sprite>("Characters/Player/Illust/_TempIllust/_TempIllust");
            data.portraitSprite = Resources.Load<Sprite>("Characters/Player/Illust/_TempIllust/_TempPortrait");
            data.useStandingPortrait = true;
            data.passiveSkill = Skill("태초의 마력");
            data.passiveSkill.passiveTrait = PassiveTrait.PrimordialMana;
            data.activeSkills = new[] { Skill("마력탄"), Skill("마나 집결"), Skill("마술의 땅"), Skill("근원") };
            ApplyCurrentSkillDesign(data);
            AssetDatabase.CreateAsset(data, AssetPath);
            AssetDatabase.SaveAssetIfDirty(data);
            return data;
        }

        private static SkillInfo Skill(string name) => new SkillInfo { skillName = name, baseCost = 0, levels = new SkillLevelData[3] };

        // Explicit migration: changes only requested active-skill rules, preserving art, VFX, stats and passive data.
        public static void ApplyCurrentSkillDesign(CharacterData data) => CrewSkillRevision.ApplyArchmage(data);
    }
}
