/*
* Mad Level Manager by Mad Pixel Machine
* http://www.madpixelmachine.com
*/

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace MadMeshCombiner {

public class MadMeshCombinerAtlasBuilder : MonoBehaviour {

    // ===========================================================
    // Constants
    // ===========================================================

    // ===========================================================
    // Fields
    // ===========================================================

    // ===========================================================
    // Methods for/from SuperClass/Interfaces
    // ===========================================================

    // ===========================================================
    // Methods
    // ===========================================================

    private static bool IsReadable(Texture2D texture) {
        var path = AssetDatabase.GetAssetPath(texture);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        return importer != null && importer.isReadable;
    }
    
    private static List<TextureModification> PrepareTextures(Texture2D[] textures) {
        List<TextureModification> modifications = new List<TextureModification>();
    
        foreach (var texture in textures) {
            var path = AssetDatabase.GetAssetPath(texture);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null) {
                var modification = new TextureModification(importer);

                if (!importer.isReadable) {
                    importer.isReadable = true;
                }

                if (importer.textureType != TextureImporterType.Advanced) {
                    importer.textureType = TextureImporterType.Advanced;
                }

                if (importer.textureFormat != TextureImporterFormat.RGBA32) {
                    importer.textureFormat = TextureImporterFormat.RGBA32;
                }

                //if (importer.normalmap) {
                //    importer.normalmap = false;
                //}

                //if (importer.convertToNormalmap) {
                //    importer.convertToNormalmap = false;
                //}

                if (modification.HasChanged()) {
                    AssetDatabase.ImportAsset(importer.assetPath, ImportAssetOptions.ForceUpdate);
                    modifications.Add(modification);
                }
            }
        }

        return modifications;
    }

    private static void RevertAll(List<TextureModification> modifications) {
        foreach (var modification in modifications) {
            modification.Revert();
        }
    }

    // without material
    public static MadMeshCombinerAtlas CreateAtlas(string texturePath, Texture2D[] textures) {
        var madeReadable = PrepareTextures(textures);
        try {
            List<MadMeshCombinerAtlas.Item> items = new List<MadMeshCombinerAtlas.Item>();

            PackTextures(textures, texturePath, ref items);

            var atlas = new MadMeshCombinerAtlas();
            atlas.atlasTexture = AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D)) as Texture2D;
            atlas.AddItemRange(items);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return atlas;
        } finally {
            RevertAll(madeReadable);
        }
    }
    
    // with material
    public static MadMeshCombinerAtlas CreateAtlas(string texturePath, Texture2D[] textures, Shader shader) {
        var madeReadable = PrepareTextures(textures);
        try {
            List<MadMeshCombinerAtlas.Item> items = new List<MadMeshCombinerAtlas.Item>();

            PackTextures(textures, texturePath, ref items);

            var atlas = new MadMeshCombinerAtlas();
            atlas.atlasTexture = AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D)) as Texture2D;
            atlas.AddItemRange(items);

            // create material out of atlas
            var materialPath = System.IO.Path.ChangeExtension(texturePath, "mat");
            //var atlasMaterial = new Material(Shader.Find("Transparent/Cutout/Diffuse"));
            var atlasMaterial = new Material(shader);
            atlasMaterial.mainTexture = atlas.atlasTexture;
            atlas.atlasMaterial = atlasMaterial;

            AssetDatabase.CreateAsset(atlasMaterial, materialPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return atlas;
        } finally {
            RevertAll(madeReadable);
        }
    }

    private static List<MadMeshCombinerAtlas.Item> LiveItems(MadMeshCombinerAtlas atlas) {
        return (from item in atlas.items where MadMeshCombinerAtlasUtil.GetItemOrigin(item) != null select item).ToList();
    }

    private static void PackTextures(Texture2D[] textures, string path, ref List<MadMeshCombinerAtlas.Item> items) {
        int padding = 2;
        
        var atlasTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        //var reloaded = ReloadTextures(textures);
        var rects = atlasTexture.PackTextures(textures, padding, 4096);

        if (atlasTexture.format != TextureFormat.ARGB32) {
            // need to rewrite texture to a new one
            var newAtlasTexture = new Texture2D(atlasTexture.width, atlasTexture.height, TextureFormat.ARGB32, false);
            newAtlasTexture.SetPixels32(atlasTexture.GetPixels32());
            newAtlasTexture.Apply();
            DestroyImmediate(atlasTexture);
            atlasTexture = newAtlasTexture;
        }

        var bytes = atlasTexture.EncodeToPNG();
        DestroyImmediate(atlasTexture);
        
        File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();
        
        for (int i = 0; i < textures.Length; ++i) {
            var texture = textures[i];
            var region = rects[i];
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(texture));
            var item = (from el in items where el.textureGUID == guid select el).FirstOrDefault();

            if (item != null) {
                item.region = region;
            } else {
                item = CreateItem(texture, region);
                items.Add(item);
            }
        }

        // set texture max size to 4086
        var importer = TextureImporter.GetAtPath(path) as TextureImporter;
        importer.maxTextureSize = 4086;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    //private static IEnumerable<Texture2D> ReloadTextures(IEnumerable<Texture2D> textures) {
    //    foreach (var texture in textures) {
    //        var nTexture = new Texture2D(texture.width, texture.height, texture.format, false);
    //        var pixels = texture.GetPixels32();
    //        nTexture.SetPixels32(pixels);
    //        yield return nTexture;
    //    }
    //}

    private static MadMeshCombinerAtlas.Item CreateItem(Texture2D texture, Rect region) {
        var item = new MadMeshCombinerAtlas.Item();
        
        item.name = texture.name;
        item.pixelsWidth = texture.width;
        item.pixelsHeight = texture.height;
        item.region = region;
        item.textureGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(texture));
        
        return item;
    }

    #region Inner Types

    private class TextureModification {
        private TextureImporter importer;

        private bool origReadable;
        private TextureImporterType origType;
        private TextureImporterFormat origFormat;
        private bool origNormalMap;
        private bool origConvertToNormalmap;


        public TextureModification(TextureImporter importer) {
            this.importer = importer;
            this.origReadable = importer.isReadable;
            this.origType = importer.textureType;
            this.origFormat = importer.textureFormat;
            this.origNormalMap = importer.normalmap;
            this.origConvertToNormalmap = importer.convertToNormalmap;
        }

        public void Revert() {
            if (HasChanged()) {
                importer.isReadable = origReadable;
                importer.textureType = origType;
                importer.textureFormat = origFormat;
                importer.normalmap = origNormalMap;
                importer.convertToNormalmap = origConvertToNormalmap;
                AssetDatabase.ImportAsset(importer.assetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        public bool HasChanged() {
            if (origReadable != importer.isReadable) {
                return true;
            }

            if (origType != importer.textureType) {
                return true;
            }

            if (origFormat != importer.textureFormat) {
                return true;
            }

            if (origNormalMap != importer.normalmap) {
                return true;
            }

            if (origConvertToNormalmap != importer.convertToNormalmap) {
                return true;
            }

            return false;
        }
    }

    #endregion

}

} // namespace