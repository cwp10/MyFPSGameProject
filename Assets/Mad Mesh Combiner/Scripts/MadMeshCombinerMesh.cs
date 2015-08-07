/*
* Copyright (c) Mad Pixel Machine
* http://www.madpixelmachine.com/
*/

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR

#endif

namespace MadMeshCombiner {

public class MadMeshCombinerMesh : MonoBehaviour {
    #region Fields

    [HideInInspector]
    public Mesh generatedMesh;

    [HideInInspector]
    public List<Material> generatedMaterials = new List<Material>();

    [HideInInspector]
    public List<Texture> generatedTextures = new List<Texture>();

    [HideInInspector]
    public List<GameObject> generatedOtherAssets = new List<GameObject>();

    #endregion

    #region Methods

    #endregion
}

} // namespace
