using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TheLastArk.Editor
{
    // Bind at import time so builds use serialized sprites, without searching files during combat.
    public sealed class PanicIllustrationImporter : AssetPostprocessor
    {
        private const string ArtRoot="Assets/Resources/Characters/Player/Illust";
        private const string DataRoot="Assets/Resources/Characters/Player/Data";
        private static bool queued, binding;

        private static string Normalize(string value)=>value.Replace("_","").Replace(" ","").ToLowerInvariant();
        private static bool IsPanicArt(string path)
        {
            if (!path.StartsWith(ArtRoot+"/",StringComparison.OrdinalIgnoreCase)) return false;
            string name=Path.GetFileNameWithoutExtension(path);
            return name.StartsWith("Panic_",StringComparison.OrdinalIgnoreCase)
                && name.Length>6 && name[name.Length-1]>='1' && name[name.Length-1]<='4';
        }
        private void OnPreprocessTexture()
        {
            if (!IsPanicArt(assetPath)) return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;
        }
        private static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] movedFrom)
        {
            if (binding || queued) return;
            foreach (var paths in new[]{imported,deleted,moved,movedFrom})
                foreach (string path in paths)
                    if (IsPanicArt(path) || path.StartsWith(DataRoot+"/",StringComparison.OrdinalIgnoreCase))
                    {
                        queued=true; EditorApplication.delayCall+=BindWhenReady; return;
                    }
        }
        private static void BindWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {EditorApplication.delayCall+=BindWhenReady;return;}
            queued=false;
            BindAll();
        }
        [MenuItem("Tools/Battle/패닉 일러스트 1~4 자동 연결")]
        public static void BindAll()
        {
            if (binding) return;
            binding=true;
            try
            {
                foreach (string guid in AssetDatabase.FindAssets("t:CharacterData",new[]{DataRoot}))
                    Bind(AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(guid)));
            }
            finally {binding=false;}
        }
        public static int Bind(CharacterData data,bool overwrite=false)
        {
            if (data==null || data.isEnemy || string.IsNullOrWhiteSpace(data.jobName)) return 0;
            string folder=ArtRoot+"/"+data.jobName;
            if (!AssetDatabase.IsValidFolder(folder)) return 0;
            var slots=new Sprite[4];
            var ambiguous=new bool[4];
            foreach (string guid in AssetDatabase.FindAssets("t:Sprite",new[]{folder}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                if (!IsPanicArt(path)) continue;
                string name=Normalize(Path.GetFileNameWithoutExtension(path));
                for (int i=0;i<4;i++)
                {
                    if (name!=Normalize("Panic_"+data.jobName+(i+1))) continue;
                    var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite==null) continue;
                    if (slots[i]!=null && slots[i]!=sprite) ambiguous[i]=true;
                    slots[i]=sprite;
                }
            }
            int changed=0;
            for (int i=0;i<4;i++)
            {
                if (slots[i]==null || ambiguous[i]) continue;
                var current=data.panicIllustrations!=null && i<data.panicIllustrations.Count ? data.panicIllustrations[i] : null;
                if (current==slots[i] || !overwrite && current!=null && !IsPanicArt(AssetDatabase.GetAssetPath(current))) continue;
                if (data.panicIllustrations==null) data.panicIllustrations=new List<Sprite>();
                while(data.panicIllustrations.Count<4) data.panicIllustrations.Add(null);
                data.panicIllustrations[i]=slots[i];changed++;
            }
            if (changed>0){EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);}
            return changed;
        }
    }
}
