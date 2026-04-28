using UnityEditor;
using UnityEngine;
using System.IO;

public class UISpriteProcessor : Editor
{
    [MenuItem("Assets/RUSH-Xtreme/Process UI Sprites")]
    public static void ProcessSprites()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!Directory.Exists(path))
        {
            Debug.LogError("Please select a folder of sprites.");
            return;
        }

        string[] fileEntries = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
        int processedCount = 0;

        foreach (string fileName in fileEntries)
        {
            string ext = Path.GetExtension(fileName).ToLower();
            if (ext == ".png" || ext == ".jpg" || ext == ".tga")
            {
                TextureImporter importer = AssetImporter.GetAtPath(fileName) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    
                    // Set compression
                    TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings();
                    settings.format = TextureImporterFormat.RGBA32;
                    settings.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SetPlatformTextureSettings(settings);

                    importer.SaveAndReimport();
                    processedCount++;
                }
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"Successfully processed {processedCount} UI sprites in {path}");
    }
}
