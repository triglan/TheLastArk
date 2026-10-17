using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using TheLastArk.Editor;

public class MirageMonsterArtImportTests
{
    [UnityTest]
    public IEnumerator ImportedArtAutomaticallyBindsAndDuplicatesPreserveAssignment()
    {
        string id = "ArtTest" + System.Guid.NewGuid().ToString("N");
        string folder = MirageMonsterArtImporter.ArtFolder + "/" + id;
        string dataPath = "Assets/Resources/Characters/Enemy/" + id + ".asset";
        var texture = new Texture2D(2, 2);
        var data = ScriptableObject.CreateInstance<CharacterData>();
        data.isEnemy = true;
        data.characterName = id;
        try
        {
            AssetDatabase.CreateFolder(MirageMonsterArtImporter.ArtFolder, id);
            AssetDatabase.CreateAsset(data, dataPath);
            string imagePath = folder + "/" + id + ".png";
            File.WriteAllBytes(imagePath, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(imagePath, ImportAssetOptions.ForceSynchronousImport);
            for (int i = 0; i < 30 && data.standingSprite == null; i++) yield return null;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
            Assert.IsNotNull(sprite);
            Assert.AreSame(sprite, data.standingSprite, "Import callback should bind automatically.");
            Assert.AreSame(sprite, data.portraitSprite);
            var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
            Assert.IsTrue(importer.alphaIsTransparency);

            string portraitPath = folder + "/" + id + "_portrait.png";
            File.WriteAllBytes(portraitPath, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(portraitPath, ImportAssetOptions.ForceSynchronousImport);
            MirageMonsterArtImporter.BindAll();
            Assert.AreSame(AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath), data.portraitSprite);
            Assert.AreSame(sprite, data.standingSprite);

            string duplicate = folder + "/" + id + ".jpg";
            File.WriteAllBytes(duplicate, texture.EncodeToJPG());
            AssetDatabase.ImportAsset(duplicate, ImportAssetOptions.ForceSynchronousImport);
            MirageMonsterArtImporter.BindAll();
            Assert.AreSame(sprite, data.standingSprite, "Ambiguous files must not silently overwrite an assignment.");
        }
        finally
        {
            AssetDatabase.DeleteAsset(dataPath);
            AssetDatabase.DeleteAsset(folder);
            Object.DestroyImmediate(texture);
        }
        yield return null;
    }
}
