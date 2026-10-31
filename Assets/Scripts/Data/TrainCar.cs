using System.Collections.Generic;
using UnityEngine;

namespace TheLastArk.Data
{
    public enum TrainCarType
    {
        Nexus,                // 넥서스 칸 (고정 1)
        CrewQuarters,         // 승무원실 (고정 2)
        Optional,             // 미건설 선택 칸 (3, 4)
        Infirmary,            // 의무실 (선택)
        CombatEnhancement,    // 전투 강화소 (선택)
        PrayerRoom,           // 기도실 (선택)
        TraitTrainingCamp,    // 특성 훈련소 (선택)

        // 호환성 유지용 alias
        Core = Nexus,
        Quarters = CrewQuarters
    }

    [System.Serializable]
    public class PartLevelEntry
    {
        public string partId;
        public int level;
    }

    [System.Serializable]
    public class TrainCar
    {
        public const int OptionalCarBuildCost = 100;
        public const int OptionalCarDismantleCost = 50;

        public string carName;
        public TrainCarType carType;
        public int level = 0;
        public int maxLevel = 4;
        public int baseUpgradeCost = 100;

        public string installedModuleId = "";
        public List<string> installedParts = new List<string>();
        // Per-run Origin part state: craft unlocks and part levels survive unequip / module swap.
        public List<string> craftedParts = new List<string>();
        public List<PartLevelEntry> partLevels = new List<PartLevelEntry>();
        public List<SynergyType> selectedSynergies = new List<SynergyType>();
        public CharacterData assignedCharacter;

        public int MaxLevel
        {
            get
            {
                if (carType == TrainCarType.Nexus)
                {
                    var mod = NexusModuleDatabase.GetModule(installedModuleId);
                    return mod != null ? mod.maxLevel : maxLevel;
                }
                return maxLevel;
            }
        }

        public int UpgradeCost => carType switch
        {
            TrainCarType.Nexus => NexusModuleDatabase.GetModule(installedModuleId)?.baseUpgradeCost ?? 140,
            TrainCarType.CrewQuarters => 40,
            TrainCarType.Infirmary => 100,
            TrainCarType.CombatEnhancement => 100,
            TrainCarType.PrayerRoom => 100,
            TrainCarType.TraitTrainingCamp => 200,
            _ => baseUpgradeCost
        };

        public int MaxPartSlots
        {
            get
            {
                if (carType == TrainCarType.Nexus)
                {
                    var mod = NexusModuleDatabase.GetModule(installedModuleId);
                    return mod != null ? mod.basePartSlots : 2;
                }
                return 2;
            }
        }

        public int MaxSelectableSynergies
        {
            get
            {
                if (carType != TrainCarType.TraitTrainingCamp) return 0;
                int count = 1 + level; // 0강: 1, 1강: 2, 2강: 3
                if (HasPartEffect(TrainPartEffectType.TraitExpander)) count += 1;
                return count;
            }
        }

        public bool IsOptionalCar => carType == TrainCarType.Infirmary ||
                                     carType == TrainCarType.CombatEnhancement ||
                                     carType == TrainCarType.PrayerRoom ||
                                     carType == TrainCarType.TraitTrainingCamp ||
                                     carType == TrainCarType.Optional;

        public bool IsBuiltOptionalCar => carType == TrainCarType.Infirmary ||
                                          carType == TrainCarType.CombatEnhancement ||
                                          carType == TrainCarType.PrayerRoom ||
                                          carType == TrainCarType.TraitTrainingCamp;

        public TrainCar(string name, TrainCarType type, int initialLevel = 0)
        {
            this.carName = name;
            this.carType = type;
            this.level = initialLevel;
            this.installedParts = new List<string>();
            this.selectedSynergies = new List<SynergyType>();

            SetupCarDefaults(type);
        }

        public void SetupCarDefaults(TrainCarType type)
        {
            this.carType = type;
            switch (type)
            {
                case TrainCarType.Nexus:
                    this.carName = "넥서스 칸";
                    this.maxLevel = 4;
                    this.baseUpgradeCost = 140;
                    this.installedModuleId = "Module_Origin";
                    break;
                case TrainCarType.CrewQuarters:
                    this.carName = "승무원실";
                    this.maxLevel = 4;
                    this.baseUpgradeCost = 40;
                    break;
                case TrainCarType.Infirmary:
                    this.carName = "의무실";
                    this.maxLevel = 3;
                    this.baseUpgradeCost = 100;
                    break;
                case TrainCarType.CombatEnhancement:
                    this.carName = "전투 강화소";
                    this.maxLevel = 5;
                    this.baseUpgradeCost = 100;
                    break;
                case TrainCarType.PrayerRoom:
                    this.carName = "기도실";
                    this.maxLevel = 3;
                    this.baseUpgradeCost = 100;
                    break;
                case TrainCarType.TraitTrainingCamp:
                    this.carName = "특성 훈련소";
                    this.maxLevel = 2;
                    this.baseUpgradeCost = 200;
                    break;
                case TrainCarType.Optional:
                default:
                    this.carName = "선택 칸 (미건설)";
                    this.maxLevel = 0;
                    this.baseUpgradeCost = 0;
                    break;
            }
        }

        public bool CanUpgrade => level < MaxLevel;

        public bool HasPart(string partId)
        {
            return installedParts != null && installedParts.Contains(partId);
        }

        /// <summary>Non-Origin parts need no crafting; Origin parts must be crafted this run.</summary>
        public bool IsPartCrafted(string partId)
        {
            return !TrainPartDatabase.IsOriginPart(partId) || (craftedParts != null && craftedParts.Contains(partId));
        }

        public void MarkPartCrafted(string partId)
        {
            if (craftedParts == null) craftedParts = new List<string>();
            if (!craftedParts.Contains(partId)) craftedParts.Add(partId);
        }

        /// <summary>Part level (0 = 기본형). Unrecorded parts are 기본형.</summary>
        public int GetPartLevel(string partId)
        {
            if (partLevels == null) return 0;
            foreach (var entry in partLevels)
                if (entry != null && entry.partId == partId) return entry.level;
            return 0;
        }

        public void SetPartLevel(string partId, int level)
        {
            if (partLevels == null) partLevels = new List<PartLevelEntry>();
            foreach (var entry in partLevels)
            {
                if (entry == null || entry.partId != partId) continue;
                entry.level = level;
                return;
            }
            partLevels.Add(new PartLevelEntry { partId = partId, level = level });
        }

        /// <summary>Level of the equipped part with this effect, or -1 when not equipped.</summary>
        public int GetEquippedPartLevel(TrainPartEffectType effectType)
        {
            if (installedParts == null) return -1;
            foreach (var pId in installedParts)
            {
                var partData = TrainPartDatabase.GetPart(pId);
                if (partData != null && partData.effectType == effectType) return GetPartLevel(pId);
            }
            return -1;
        }

        /// <summary>New run: Origin parts are locked again, unequipped, and back to 기본형.</summary>
        public void ResetOriginPartState()
        {
            installedParts?.RemoveAll(TrainPartDatabase.IsOriginPart);
            craftedParts?.Clear();
            partLevels?.Clear();
        }

        public bool HasPartEffect(TrainPartEffectType effectType)
        {
            if (installedParts == null) return false;
            foreach (var pId in installedParts)
            {
                var partData = TrainPartDatabase.GetPart(pId);
                if (partData != null && partData.effectType == effectType) return true;
            }
            return false;
        }

        public bool CanInstallPart(string partId)
        {
            if (string.IsNullOrEmpty(partId)) return false;
            if (installedParts.Count >= MaxPartSlots) return false;
            if (installedParts.Contains(partId)) return false;

            var partData = TrainPartDatabase.GetPart(partId);
            if (partData == null) return false;
            if (partData.targetCarType != carType) return false;

            if (carType == TrainCarType.Nexus && !string.IsNullOrEmpty(partData.targetModuleId))
            {
                if (partData.targetModuleId != installedModuleId) return false;
            }
            // Origin equips only its five parts, and only after crafting.
            if (carType == TrainCarType.Nexus && installedModuleId == NexusModuleDatabase.OriginId
                && partData.targetModuleId == NexusModuleDatabase.OriginId && !TrainPartDatabase.IsOriginPart(partId)) return false;
            if (!IsPartCrafted(partId)) return false;

            return true;
        }

        public bool InstallPart(string partId)
        {
            if (!CanInstallPart(partId)) return false;
            installedParts.Add(partId);
            return true;
        }

        public bool UninstallPart(string partId)
        {
            if (installedParts == null) return false;
            return installedParts.Remove(partId);
        }

        public List<string> ChangeNexusModule(string newModuleId)
        {
            List<string> removedParts = new List<string>();
            if (carType != TrainCarType.Nexus) return removedParts;

            var moduleData = NexusModuleDatabase.GetModule(newModuleId);
            if (moduleData == null) return removedParts;

            installedModuleId = newModuleId;
            maxLevel = moduleData.maxLevel;
            level = Mathf.Clamp(level, 0, MaxLevel);

            // Incompatible parts auto-removal
            if (installedParts != null && installedParts.Count > 0)
            {
                List<string> toKeep = new List<string>();
                foreach (var pId in installedParts)
                {
                    var pData = TrainPartDatabase.GetPart(pId);
                    if (pData != null && (string.IsNullOrEmpty(pData.targetModuleId) || pData.targetModuleId == newModuleId))
                    {
                        toKeep.Add(pId);
                    }
                    else
                    {
                        removedParts.Add(pData != null ? pData.partName : pId);
                    }
                }

                while (toKeep.Count > MaxPartSlots)
                {
                    string removed = toKeep[toKeep.Count - 1];
                    toKeep.RemoveAt(toKeep.Count - 1);
                    var pData = TrainPartDatabase.GetPart(removed);
                    removedParts.Add(pData != null ? pData.partName : removed);
                }

                installedParts = toKeep;
            }

            return removedParts;
        }

        public void ResetCarToEmptyOptional()
        {
            this.carType = TrainCarType.Optional;
            this.carName = "선택 칸 (미건설)";
            this.level = 0;
            this.maxLevel = 0;
            this.installedParts.Clear();
            this.selectedSynergies.Clear();
            this.installedModuleId = "";
        }
    }
}
