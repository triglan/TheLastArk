using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TheLastArk.Editor
{
    // Import-time references are serialized into CharacterData, so builds require no file-name lookup.
    public sealed class MirageMonsterArtImporter : AssetPostprocessor
    {
        public const string ArtFolder = "Assets/Sprites/Monster/1. mirage";
        private const string DataFolder = "Assets/Resources/Characters/Enemy";
        private static bool queued;
        private static bool binding;

        public static string NormalizeName(string name)
        {
            return new string((name ?? "").Normalize(NormalizationForm.FormC)
                .Where(c => !char.IsWhiteSpace(c) && c != '_' && c != '-').ToArray()).ToLowerInvariant();
        }

        private static bool IsArt(string path) => path.StartsWith(ArtFolder + "/", StringComparison.OrdinalIgnoreCase);

        private void OnPreprocessTexture()
        {
            if (!IsArt(assetPath)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (binding || queued) return;
            if (!imported.Concat(deleted).Concat(moved).Concat(movedFrom).Any(p => IsArt(p) || p.StartsWith(DataFolder + "/", StringComparison.OrdinalIgnoreCase))) return;
            queued = true;
            EditorApplication.delayCall += BindWhenReady;
        }

        private static void BindWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += BindWhenReady;
                return;
            }
            queued = false;
            BindAll();
        }

        [MenuItem("Tools/Battle/Bind Mirage Monster Illustrations")]
        public static void BindAll()
        {
            if (binding || !AssetDatabase.IsValidFolder(ArtFolder)) return;
            binding = true;
            try
            {
                var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder })
                    .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal).ToArray();
                var sprites = new Dictionary<string, List<Sprite>>();
                foreach (string path in paths)
                {
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;
                    if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.alphaIsTransparency = true;
                        importer.mipmapEnabled = false;
                        importer.SaveAndReimport();
                    }
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite == null) continue;
                    string key = NormalizeName(Path.GetFileNameWithoutExtension(path));
                    if (!sprites.TryGetValue(key, out List<Sprite> matches)) sprites[key] = matches = new List<Sprite>();
                    matches.Add(sprite);
                }
                int changed = 0;
                foreach (string guid in AssetDatabase.FindAssets("t:CharacterData", new[] { DataFolder }))
                {
                    CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(guid));
                    if (data == null || !data.isEnemy) continue;
                    string key = NormalizeName(data.characterName);
                    bool dirty = false;
                    Sprite standing = Match(sprites, key);
                    Sprite portrait = Match(sprites, key + "portrait");
                    Sprite closed = Match(sprites, key + "closed");
                    if (standing != null && data.standingSprite != standing) { data.standingSprite = standing; dirty = true; }
                    if (portrait == null && sprites.ContainsKey(key + "portrait")) portrait = data.portraitSprite;
                    if (portrait == null && (data.portraitSprite == null || IsArt(AssetDatabase.GetAssetPath(data.portraitSprite)))) portrait = standing;
                    if (portrait != null && data.portraitSprite != portrait) { data.portraitSprite = portrait; dirty = true; }
                    if (closed != null && data.closedEyesSprite != closed) { data.closedEyesSprite = closed; dirty = true; }
                    if (!dirty) continue;
                    EditorUtility.SetDirty(data);
                    changed++;
                }
                if (changed > 0)
                {
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[Mirage Art] Updated illustrations for {changed} enemies.");
                }
            }
            finally { binding = false; }
        }

        private static Sprite Match(Dictionary<string, List<Sprite>> sprites, string key)
        {
            if (!sprites.TryGetValue(key, out List<Sprite> matches)) return null;
            if (matches.Count == 1) return matches[0];
            Debug.LogWarning($"[Mirage Art] Duplicate name '{key}'; keep one matching illustration. Assignment skipped.");
            return null;
        }
    }
}
