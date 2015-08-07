/*
* Copyright (c) Mad Pixel Machine
* http://www.madpixelmachine.com/
*/

using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MadMeshCombiner {

public class MadMeshCombinerManagedEditor {

    public static void RemoveCombined(MadMeshCombinerManaged managed) {
        var combined = managed.CombinedObjects().ToList(); // ToList() because we will be removing children
        foreach (var c in combined) {
            RemoveGeneratedAssets(c);
            GameObject.DestroyImmediate(c.gameObject);
        }
    }

    public static void RemoveGeneratedAssets(MadMeshCombinerMesh c) {
        RemoveAsset(c.generatedMesh);

        foreach (var material in c.generatedMaterials) {
            RemoveAsset(material);
        }

        foreach (var texture in c.generatedTextures) {
            RemoveAsset(texture);
        }

        foreach (var other in c.generatedOtherAssets) {
            RemoveAsset(other);
        }

        AssetDatabase.Refresh();
    }

    private static void RemoveAsset(UnityEngine.Object asset) {
        var path = AssetDatabase.GetAssetPath(asset);
        AssetDatabase.DeleteAsset(path);
    }

}

}