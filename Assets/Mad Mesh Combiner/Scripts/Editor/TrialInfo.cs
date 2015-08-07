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

public class TrialInfo {

    public static void TrialStarted() {
        EditorUtility.DisplayDialog(
            "Mad Mesh Combiner Evaluation",
            "This is a full-featured evaluation version of Mad Mesh Combiner. Feel free to use it for 14 days.\n\n"
            + "Please be aware that all content created by this evaluation version IS NOT USABLE with the full version of Mad Mesh Combiner (You will need to configure all things again).\n",
            "OK");
    }

    public static void TrialEnded() {
        if (EditorUtility.DisplayDialog(
            "Mad Mesh Combiner Evalutation Expired.",
            "Mad Mesh Combiner evaluation period has expired. We hope you have had a good time while evaluating Mad Mesh Combiner. \n\n"
            + "Please purchase our product for continuous use or request another evaluation key from the Tools/Mad Mesh Combiner/Request Evaluation Key menu.",
                "Purchase", "Cancel")) {
            Application.OpenURL("_TRIAL_PURCHASE_URL_");
        }
    }

}

}