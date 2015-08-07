/*
* Copyright (c) Mad Pixel Machine
* http://www.madpixelmachine.com/
*/

using System;
using UnityEditor;
using UnityEngine;

namespace MadMeshCombiner {

[CustomEditor(typeof(MadMeshCombinerManaged))]
public class MadMeshCombinerManagedInspector : Editor {

    private MadMeshCombinerManaged managed;

    void OnEnable() {
        managed = target as MadMeshCombinerManaged;
    }

    public override void OnInspectorGUI() {

        MadGUI.Box("Combined Mesh", () => {
            MadGUI.Info("You can toggle between generated mesh and sources. Use this to analyze rendering stats and to adjust source objects positions.");

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = managed.combinedMeshExists;
            if (managed.combinedMeshEnabled) {
                EditorGUILayout.LabelField("Mesh Enabled");
                GUILayout.FlexibleSpace();

                if (MadGUI.Button("Toggle (Enable Sources)", Color.gray)) {
                    managed.combinedMeshEnabled = false;
                }
            } else {
                EditorGUILayout.LabelField("Mesh Disabled");
                GUILayout.FlexibleSpace();

                if (MadGUI.Button("Toggle (Enable Mesh)", Color.green)) {
                    managed.combinedMeshEnabled = true;
                }
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUI.enabled = managed.hasSources;
            if (MadGUI.Button("Recombine", Color.yellow)) {
                try {
                    MadMeshCombinerTool.RecombineManaged(managed);
                } catch (Exception e) {
                    EditorUtility.DisplayDialog("Combine failed",
                        "Combine operation threw an error. Please review your source objects and try again. If this situation will still preserve, please write to support@madpixelmachine.com.",
                        "OK");
                    managed.MarkCombineFailed();
                    throw e;
                }
            }
            GUI.enabled = true;

            if (!managed.hasSources) {
                EditorGUILayout.Space();

                GUI.enabled = managed.combinedMeshExists;
                if (MadGUI.Button(managed.combinedMeshExists ? "Remove Mesh" : "(Mesh Removed)", Color.red)) {
                    if (AreYouSure("Remove Mesh", "Are you sure that you want to remove combined objects from your project? This cannot be undone.")) {
                        MadMeshCombinerManagedEditor.RemoveCombined(managed);
                    }
                }
                GUI.enabled = true;
            }
        });

        MadGUI.Box("Source Objects", () => {
            if (!managed.hasSources) {
                if (managed.combinedMeshExists) {
                    MadGUI.Info("Source objects for this mesh has been removed.");
                } else {
                    MadGUI.Info("There are no source objects. Move your objects to the Source object in the hierarchy.");
                }
            } else {

                GUI.enabled = managed.hasSources;

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField("What to do with source objects?");
                managed.sourceAction = (MadMeshCombinerManaged.SourceAction) EditorGUILayout.EnumPopup(managed.sourceAction);
                if (EditorGUI.EndChangeCheck()) {
                    managed.EnableSources();
                    managed.DisableSources();
                }

                EditorGUI.BeginChangeCheck();
                managed.preserveLightmapping = EditorGUILayout.Toggle("Preserve Lightmapping",
                    managed.preserveLightmapping);
                if (EditorGUI.EndChangeCheck()) {
                    EditorUtility.SetDirty(managed);
                }

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();

                if (MadGUI.Button("Remove Sources", Color.cyan)) {
                    if (AreYouSure("Remove Sources", "Are you sure that you want to remove your source objects? This cannot be undone.")) {
                        managed.RemoveSources();
                    }
                }

                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
            }
        });

        if (managed.hasSources) {

            EditorGUILayout.Space();

            MadGUI.Info("This will move your sources objects to thier original position and remove combined mesh.");

            if (MadGUI.Button("Revert All", Color.red)) {
                if (AreYouSure("Revert All", "Are you sure that you want to move your source objects to their original position?")) {
                    managed.EnableSources();
                    MadMeshCombinerManagedEditor.RemoveCombined(managed);
                    MoveSourcesToOrigin();

                    if (managed.hasSources) {
                        EditorUtility.DisplayDialog("Reverted", "Your mesh is removed, but not all source objects could be moved to thier original position.", "OK");
                    } else {
                        EditorUtility.DisplayDialog("Reverted", "All done! You can now remove the managed object.", "OK");
                    }
                }
            }
        }

    }

    private bool AreYouSure(string title, string message) {
        return EditorUtility.DisplayDialog(title, message, "Yes", "No");
    }

    public void MoveSourcesToOrigin() {

        bool parentRemoved = false;

        foreach (var origin in managed.sourceOrigins) {
            if (origin.sourceObject == null) {
                continue;
            }

            Undo.SetTransformParent(origin.sourceObject, origin.originParent, "Move To Origin");
            if (!parentRemoved) {
                parentRemoved = origin.parentRemoved;
            }
        }

        if (parentRemoved) {
            EditorUtility.DisplayDialog("Some Parens Removed", "Some source objects parents are not longer in the scene. You have to move them manually.", "OK");
        } else if (managed.hasSources) {
            EditorUtility.DisplayDialog("Origin Not Known", "Not all source objects were moved away because I don't know where they belongs. Please move them manually.", "OK");
        }

        Selection.objects = managed.OriginGameObjects();

        managed.sourceOrigins.Clear();

        if (!managed.hasSources && !managed.combinedMeshExists) {
            managed.gameObject.SetActive(false);
        }

    }

}

} // namespace