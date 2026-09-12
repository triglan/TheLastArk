using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CombatIconEntry
{
    public EffectType effectType;
    public Sprite icon;
}

[CreateAssetMenu(fileName = "CombatIconLibrary", menuName = "Battle/Combat Icon Library")]
public sealed class CombatIconLibrary : ScriptableObject
{
    public Sprite commonEnhancementIcon;
    public List<CombatIconEntry> entries = new List<CombatIconEntry>();

    private static CombatIconLibrary cached;

    private void OnValidate()
    {
        if (entries == null) entries = new List<CombatIconEntry>();
        EnsureEffectEntries();
    }

    public Sprite GetIcon(EffectType effectType)
    {
        foreach (CombatIconEntry entry in entries)
            if (entry != null && entry.effectType == effectType) return entry.icon;
        return null;
    }

    public void SetIcon(EffectType effectType, Sprite icon)
    {
        CombatIconEntry entry = entries.Find(item => item != null && item.effectType == effectType);
        if (entry == null)
        {
            entry = new CombatIconEntry { effectType = effectType };
            entries.Add(entry);
        }
        entry.icon = icon;
    }

    public void EnsureEffectEntries()
    {
        foreach (EffectType type in Enum.GetValues(typeof(EffectType)))
        {
            bool exists = entries.Exists(entry => entry != null && entry.effectType == type);
            if (!exists) entries.Add(new CombatIconEntry { effectType = type });
        }
    }

    public static Sprite FindIcon(EffectType effectType)
    {
        if (cached == null) cached = Resources.Load<CombatIconLibrary>("CombatIconLibrary");
        return cached != null ? cached.GetIcon(effectType) : null;
    }

    public static Sprite FindEnhancementIcon()
    {
        if (cached == null) cached = Resources.Load<CombatIconLibrary>("CombatIconLibrary");
        return cached != null ? cached.commonEnhancementIcon : null;
    }

    public static string GetDisplayName(EffectType type)
    {
        switch (type)
        {
            case EffectType.Damage: return "피해";
            case EffectType.Heal: return "회복";
            case EffectType.Stun: return "기절";
            case EffectType.Bleed: return "출혈";
            case EffectType.Taunt: return "도발";
            case EffectType.Counter: return "반격";
            case EffectType.Shield: return "보호막";
            case EffectType.Resurrection: return "부활";
            case EffectType.Poison: return "독";
            case EffectType.Burn: return "화상";
            case EffectType.Strength: return "힘";
            case EffectType.Blockade: return "봉쇄";
            case EffectType.Fatigue: return "피로";
            case EffectType.Confusion: return "혼란";
            case EffectType.Frost: return "냉기";
            case EffectType.Fear: return "공포";
            case EffectType.Pressure: return "압박";
            case EffectType.Despair: return "절망";
            case EffectType.Weakness: return "약화";
            case EffectType.Protection: return "보호";
            case EffectType.Vulnerable: return "취약";
            case EffectType.Pierce: return "관통";
            case EffectType.Guard: return "보호";
            case EffectType.Focus: return "집중";
            case EffectType.MentalDamage: return "정신 피해";
            case EffectType.MentalHeal: return "정신 회복";
            case EffectType.Amplification: return "증폭";
            case EffectType.Frailty: return "허약";
            case EffectType.MagicGuard: return "마법 보호";
            case EffectType.Corrosion: return "부식";
            case EffectType.Reflect: return "반사";
            default: return type.ToString();
        }
    }
}
