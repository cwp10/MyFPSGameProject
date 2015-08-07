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

public class MenuItems {

    private const string DocumentationGUID = "024ff9e2b2c541c4094a1120ef5be9f5";

    [MenuItem("Tools/Mad Mesh Combiner/Combine Selected...", false, 100)]
    public static void DisplayCombinerWindow() {
        if (MadTrialEditor.isTrialVersion && MadTrialEditor.expired) {
            TrialInfo.TrialEnded();
            return;
        }

        EditorWindow.GetWindow<MadMeshCombinerWindow>(false, "Mesh Combine", true);
    }

#if TRIAL

    [MenuItem("Tools/Mad Mesh Combiner/Request Evaluation Key", false, 200)]
    public static void RequestEvaluationKey() {
        MadTrialEditor.RequestExtend("Mad Mesh Combiner");
    }

#endif

    [MenuItem("Tools/Mad Mesh Combiner/Documentation", false, 300)]
    public static void OpenDocumentation() {
        var documentation = AssetDatabase.GUIDToAssetPath(DocumentationGUID);
        if (string.IsNullOrEmpty(documentation)) {
            EditorUtility.DisplayDialog("Documentation missing", "I cannot find documentation file. Have you deleted it?", "Ohh...");
            return;
        }

        AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath(documentation, typeof(UnityEngine.Object)));
    }

    [MenuItem("Tools/Mad Mesh Combiner/About", false, 301)]
    public static void About() {
        string trialInfo = "";

        if (MadTrialEditor.isTrialVersion) {
            if (MadTrialEditor.expired) {
                trialInfo = "\n\nYour evaluation period has expired!";

                if (EditorUtility.DisplayDialog("Mad Mesh Combiner",
                    "Copyright (c) Mad Pixel Machine\nVersion: _VERSION_\n\nhttp://madlevelmanager.madpixelmachine.com/" +
                    trialInfo,
                    "Enter New Evaluation Key", "OK")) {

                    MadTrialEditor.EnterEvaluationKeyDialog();
                }

                return;

            } else {
                int daysLeft = MadTrialEditor.DaysLeft();
                trialInfo = "\n\nYour have " + daysLeft + " evaluation days left.";
            }
        }

        EditorUtility.DisplayDialog("Mad Mesh Combiner",
        "Copyright (c) Mad Pixel Machine\nVersion: _VERSION_\n\nhttp://madlevelmanager.madpixelmachine.com/" + trialInfo,
        "OK");

        
    }
}

} // namespace