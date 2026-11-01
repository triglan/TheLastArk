using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TheLastArk.UI
{
    public static class CrewSelectionPreferences
    {
        public const string LastLeaderKey = "CrewSelection.LastLeaderId";
        public static CultureInfo LanguageCulture(int language)
        {
            string[] cultures = { "ko-KR", "en-US", "ja-JP", "zh-CN" };
            return CultureInfo.GetCultureInfo(cultures[Mathf.Clamp(language, 0, cultures.Length - 1)]);
        }

        public static List<CharacterData> OrderedRoster(IEnumerable<CharacterData> source, int language)
        {
            var result = new List<CharacterData>();
            foreach (var character in source)
                if (character != null && !character.isEnemy) result.Add(character);
            var comparer = StringComparer.Create(LanguageCulture(language), true);
            result.Sort((a,b) =>
            {
                int order = comparer.Compare(a.DisplayName, b.DisplayName);
                return order != 0 ? order : StringComparer.Ordinal.Compare(a.DataId, b.DataId);
            });
            return result;
        }

        public static CharacterData InitialLeader(IList<CharacterData> ordered, string lastId)
        {
            foreach (var character in ordered)
                if (character.DataId == lastId) return character;
            return ordered.Count > 0 ? ordered[0] : null;
        }

        public static void Remember(CharacterData character)
        {
            // Editor preview changes must never replace the player's selection.
            if (!Application.isPlaying || character == null || character.isEnemy) return;
            PlayerPrefs.SetString(LastLeaderKey, character.DataId);
            PlayerPrefs.Save();
        }
    }
}
