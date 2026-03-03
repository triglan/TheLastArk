using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class CharacterAutoGenerator : EditorWindow
{
    [MenuItem("Assets/Create/Battle/Generate Full Data &g", false, 1)]
    public static void GenerateFullData()
    {
        Object[] selectedObjects = Selection.objects;
        List<Sprite> sprites = new List<Sprite>();

        foreach (Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) sprites.Add(s);
        }

        if (sprites.Count < 7)
        {
            EditorUtility.DisplayDialog("경고", $"7개의 이미지가 필요합니다. (현재 {sprites.Count}개)", "확인");
            return;
        }

        Sprite illust = sprites.Find(s => s.name.ToLower().Contains("illust"));
        Sprite portrait = sprites.Find(s => s.name.ToLower().Contains("portrait"));
        Sprite skill0 = sprites.Find(s => s.name.Contains("Skill 0"));

        // 이름 추출: 키워드 앞부분까지를 캐릭터 이름으로 인식
        string rawName = illust != null ? illust.name : portrait.name;
        string charName = rawName;
        string[] keywords = { "_Illust", "Illust", "_Portrait", "Portrait", "_Skill", "Skill" };

        foreach (string kw in keywords)
        {
            int index = charName.IndexOf(kw);
            if (index != -1)
            {
                charName = charName.Substring(0, index);
                break;
            }
        }

        string folderPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(sprites[0]));

        // 데이터 생성 및 연결
        SkillData passiveData = CreateSkillAsset(skill0, folderPath);
        SkillData[] activeDatas = new SkillData[4];
        for (int i = 1; i <= 4; i++)
        {
            Sprite s = sprites.Find(x => x.name.Contains("Skill " + i));
            activeDatas[i - 1] = CreateSkillAsset(s, folderPath);
        }

        CharacterData charData = ScriptableObject.CreateInstance<CharacterData>();
        charData.characterName = charName;
        charData.standingSprite = illust;
        charData.portraitSprite = portrait;
        charData.passiveSkill = passiveData;
        charData.activeSkills = activeDatas;

        AssetDatabase.CreateAsset(charData, Path.Combine(folderPath, charName + "_Data.asset"));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("성공", $"'{charName}' 데이터 셋 생성 완료 (기본 코스트 2)!", "확인");
    }

    private static SkillData CreateSkillAsset(Sprite sprite, string folderPath)
    {
        if (sprite == null) return null;
        SkillData asset = ScriptableObject.CreateInstance<SkillData>();
        asset.skillName = sprite.name;
        asset.skillIcon = sprite;
        asset.baseCost = 2; // 🔥 기본 코스트를 2로 자동 설정
        AssetDatabase.CreateAsset(asset, Path.Combine(folderPath, sprite.name + ".asset"));
        return asset;
    }
}