/*
* Mad Level Manager by Mad Pixel Machine
* http://www.madpixelmachine.com
*/

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace MadMeshCombiner {

public class MadMeshCombinerAtlasUtil : MonoBehaviour {

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

    public static string GetItemOriginPath(MadMeshCombinerAtlas.Item item) {
        var path = AssetDatabase.GUIDToAssetPath(item.textureGUID);
        return path;
    }

    public static Texture2D GetItemOrigin(MadMeshCombinerAtlas.Item item) {
        var path = AssetDatabase.GUIDToAssetPath(item.textureGUID);
        if (string.IsNullOrEmpty(path)) {
            return null;
        }
        
        return AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;
    }

    // ===========================================================
    // Static Methods
    // ===========================================================

    // ===========================================================
    // Inner and Anonymous Classes
    // ===========================================================

}

} // namespace