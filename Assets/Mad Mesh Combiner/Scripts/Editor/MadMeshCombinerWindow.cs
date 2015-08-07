/*
* Copyright (c) Mad Pixel Machine
* http://www.madpixelmachine.com/
*/

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MadMeshCombiner {

public class MadMeshCombinerWindow : EditorWindow {

    #region Fields

    private Mode mode = Mode.Managed;
    private MadMeshCombinerManaged.SourceAction sourceAction = MadMeshCombinerManaged.SourceAction.DisableRenderers;

    private List<MadMeshCombinerTool.ObjectGroup> groups = new List<MadMeshCombinerTool.ObjectGroup>();

    private int meshCount;
    private int vertexCount;

    private int vcEntriesIndex = 4;

    private HelpButton combineModeHelpButton = new HelpButton();
    private HelpButton vertCountHelpButton = new HelpButton();
    private HelpButton preserveLightmappingHelpButton = new HelpButton();

    #endregion

    #region Public Properties
    #endregion

    #region Slots

    void OnEnable() {
        MadMeshCombinerTool.maxVerticesPerMesh = 5000; // default value
        MadMeshCombinerTool.preserveLightmapping = false; // default value
        AnalyzeSelected();
        minSize = new Vector2(320, 250);
    }

    void OnSelectionChange() {
        AnalyzeSelected();
        Repaint();
    }

    void AnalyzeSelected() {
        groups = MadMeshCombinerTool.AnalyzeSelected();
        meshCount = 0;
        vertexCount = 0;

        foreach (var group in groups) {
            meshCount += group.objects.Count;
            vertexCount += group.CountVerticles();
        }

        objectsWithSubmeshes = ScanForSubmeshes();
    }

    List<GameObject> ScanForSubmeshes() {
        List<GameObject> result = new List<GameObject>();

        var gameObjects = Selection.gameObjects;
        foreach (var gameObject in gameObjects) {
            var meshFilter = gameObject.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null && meshFilter.gameObject.activeInHierarchy && meshFilter.sharedMesh != null) {
                if (meshFilter.sharedMesh.subMeshCount > 1) {
                    result.Add(gameObject);
                }
            }
        }

        return result;
    }

    Vector2 scrollPosition;
    private List<GameObject> objectsWithSubmeshes = new List<GameObject>();

    void OnGUI() {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (objectsWithSubmeshes.Count > 0) {
            bool print = MadGUI.InfoFix(
                "There are " + objectsWithSubmeshes.Count + " object(s) selected that have more than one submesh." +
                "These objects will need more time to combine.", "Print To Console");
            if (print) {
                foreach (var objectsWithSubmesh in objectsWithSubmeshes) {
                    var subMeshCount = objectsWithSubmesh.GetComponent<MeshFilter>().sharedMesh.subMeshCount;
                    Debug.LogWarning(objectsWithSubmesh.name + " have " + subMeshCount + " submeshes. (click to select)", objectsWithSubmesh);
                }
            }
        }

        GUILayout.Label("Selected Objects", "HeaderLabel");

        EditorGUI.indentLevel++;
        ReadOnly("Mesh Count", meshCount.ToString());
        ReadOnly("Vertex Count", vertexCount.ToString());
        ReadOnly("Meshes To Generate", groups.Count.ToString());
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();
        GUILayout.Label("Combine Settings", "HeaderLabel");

        EditorGUILayout.BeginHorizontal();
        mode = (Mode) EditorGUILayout.EnumPopup("Combine Mode", mode);
        combineModeHelpButton.Draw();
        EditorGUILayout.EndHorizontal();

        if (combineModeHelpButton.enabled) {
            switch (mode) {
                case Mode.Managed:
                    MadGUI.Info("Managed mode will create new game object and it will nest all selected game objects within. "
                        + "This approach allows to rebuild combined mesh without removing the old one.");
                    break;
                case Mode.Simple:
                    MadGUI.Info("Simple mode will create a new mesh without moving the original objects in the hierarchy. "
                        + "No reference is kept to original game objects.");
                    break;
            }
        }

        EditorGUILayout.Space();

        if (mode == Mode.Simple) {
            GUI.enabled = false;
        }

        List<string> vcName = new List<string>();
        List<int> vcCount = new List<int>();

        vcName.Add("65000 (MAX)"); vcCount.Add(65000);
        vcName.Add("32000"); vcCount.Add(32000);
        vcName.Add("16000"); vcCount.Add(16000);
        vcName.Add("8000 (Mobile max)"); vcCount.Add(8000);
        vcName.Add("5000 (Mobile optimal)"); vcCount.Add(5000);
        vcName.Add("2500"); vcCount.Add(2500);

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        vcEntriesIndex = EditorGUILayout.Popup("Vert Count Per Mesh", vcEntriesIndex, vcName.ToArray());
        if (EditorGUI.EndChangeCheck()) {
            MadMeshCombinerTool.maxVerticesPerMesh = vcCount[vcEntriesIndex];
            AnalyzeSelected();
        }

        vertCountHelpButton.Draw();

        //MadGUI.Button("?", Color.green, GUILayout.Width(20));
        EditorGUILayout.EndHorizontal();

        if (vertCountHelpButton.enabled) {
            MadGUI.Info("On some platforms low vertex count per mesh can be more important than number of draw calls. " +
                        "Choose optimal vertex count for your game to get the best possible performance.");
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        MadMeshCombinerTool.preserveLightmapping = EditorGUILayout.Toggle("Preseve Lightmapping",
            MadMeshCombinerTool.preserveLightmapping);

        preserveLightmappingHelpButton.Draw();

        EditorGUILayout.EndHorizontal();

        if (preserveLightmappingHelpButton.enabled) {
            MadGUI.Info("If your scene is already lightmapped enabling this option won't break your current lightmapping. " +
                        "It's recommended to NOT USE this option and do the lightmapping right after combining once more, " +
                        "because it will break future lightmapping attempts.");
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("What to do with source objects?");
        sourceAction = (MadMeshCombinerManaged.SourceAction) EditorGUILayout.EnumPopup(sourceAction);

        if (mode == Mode.Simple) {
            GUI.enabled = true;
            MadGUI.Warning("Simple mode will not modify the source objects. You have to do it yourself.");
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (MadGUI.Button("Combine Now", Color.yellow)) {
            Combine();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    #endregion

    #region Private Methods
    
    private void ReadOnly(string label, string value) {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(150));
        GUILayout.Label(value);
        EditorGUILayout.EndHorizontal();
    }

    private void Combine() {
        switch (mode) {
            case Mode.Managed:
                CombineManaged();
                break;

            case Mode.Simple:
                CombineSimple();
                break;
        }
    }

    private void CombineManaged() {
        var managed = MadTransform.CreateChild<MadMeshCombinerManaged>(null, "Managed Combined Mesh");

        managed.sourceAction = sourceAction;
        managed.preserveLightmapping = MadMeshCombinerTool.preserveLightmapping;

        managed.AddGameObjects(TopGameObjects());

        try {
            MadMeshCombinerTool.RecombineManaged(managed);
        } catch (Exception e) {
            Selection.activeObject = managed;
            EditorUtility.DisplayDialog("Combine failed",
                "Combine operation threw an error. Please review your source objects and try again. If this situation will still preserve, please write to support@madpixelmachine.com.",
                "OK");
            managed.MarkCombineFailed();
            throw e;
        }

        Selection.activeObject = managed;
    }

    private void CombineSimple() {
        try {
            MadMeshCombinerTool.CombineSimple(TopGameObjects());
        } catch (Exception e) {
            EditorUtility.DisplayDialog("Combine failed",
                "Combine operation threw an error. Please review your source objects and try again. If this situation will still preserve, please write to support@madpixelmachine.com.",
                "OK");
            throw e;
        }
    }

    private IEnumerable<GameObject> TopGameObjects() {
        List<MadMeshCombinerTool.ObjectRef> allRefs = new List<MadMeshCombinerTool.ObjectRef>();
        foreach (var g in groups) {
            allRefs.AddRange(g.objects);
        }

        var objs = from o in allRefs select o.topLevelObject;
        return objs.Distinct();
    }

    #endregion

    #region Public Static Methods

    #endregion

    public enum Mode {
        Simple,
        Managed,
    }

    public class HelpButton {
        public bool enabled;

        public void Draw() {
            var rect = EditorGUILayout.BeginHorizontal(GUILayout.Width(20));
            GUI.backgroundColor = Color.green;
            enabled = EditorGUILayout.Toggle(enabled, "Button", GUILayout.Width(20));
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            GUI.Label(new Rect(rect.xMin + 5, rect.yMin + 2, rect.width - 5, rect.height - 2), "?");
        }
    }

    #region Inner and Anonymous Classes
    #endregion
}

} // namespace