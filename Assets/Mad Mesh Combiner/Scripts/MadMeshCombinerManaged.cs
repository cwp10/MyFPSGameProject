/*
* Copyright (c) Mad Pixel Machine
* http://www.madpixelmachine.com/
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MadMeshCombiner {

public class MadMeshCombinerManaged : MonoBehaviour {

    #region Public Fields

    public SourceAction sourceAction = SourceAction.DisableRenderers;

    public bool preserveLightmapping;

    public string saveFolderGUID;

    public List<SourceOrigin> sourceOrigins = new List<SourceOrigin>();

    #endregion

    #region Public Properties

    public bool combinedMeshEnabled {
        get {
            var combined = CombinedTransform();
            return combined.gameObject.activeSelf;
        }

        set {
            var combined = CombinedTransform();

            if (value) {
                combined.gameObject.SetActive(true);
                DisableSources();
            } else {
                combined.gameObject.SetActive(false);
                EnableSources();
            }
        }
    }

    public bool combinedMeshExists {
        get {
            var combined = CombinedTransform();
            return combined.childCount > 0;
        }
    }

    public bool hasSources {
        get {
            return SourceTransform().childCount > 0;
        }
    }

    #endregion

    #region Slots

    void Start() {
    }

    void Update() {
    }

    #endregion

    #region Public Methods

    public void AddGameObjects(IEnumerable<GameObject> gameObjects) {
        foreach (var go in gameObjects) {
            AddGameObject(go);
        }
    }

    private void AddGameObject(GameObject gameObject) {
        var source = SourceTransform();

        var origin = new SourceOrigin(gameObject);
        sourceOrigins.Add(origin);

#if UNITY_EDITOR
        Undo.SetTransformParent(gameObject.transform, source, "Creating Mesh");
#else
        gameObject.transform.parent = source;
#endif
    }

    #endregion

    #region Private Methods

    public void EnableSources() {
        var objects = SourceObjects();
        foreach (var obj in objects) {
            obj.SetActive(true);
        }

        var meshRenderers = MadTransform.FindChildren<MeshRenderer>(SourceTransform());
        foreach (var renderer in meshRenderers) {
            renderer.enabled = true;
        }
    }
	
    public void DisableSources() {

        switch (sourceAction) {
            case SourceAction.DisableGameObjects:
                var objects = SourceObjects();
                foreach (var obj in objects) {
#if UNITY_EDITOR
                    Undo.RegisterCompleteObjectUndo(obj, "Source Disabled");
#endif
                    obj.SetActive(false);
                }
                break;

            case SourceAction.DisableRenderers:
                var meshRenderers = MadTransform.FindChildren<MeshRenderer>(SourceTransform());
                foreach (var renderer in meshRenderers) {
					//if(renderer.gameObject.activeInHierarchy && renderer.sharedMaterial.mainTexture != null)
					if(renderer.gameObject.activeInHierarchy) {
#if UNITY_EDITOR
                    Undo.RegisterCompleteObjectUndo(renderer, "Source Disabled");
#endif
                    renderer.enabled = false; }
                }
                break;

            case SourceAction.DoNothing:
                break;

            default:
                Debug.LogError("Unknown source action type: " + sourceAction);
                break;
        }
    }

    private IEnumerable<GameObject> FilterChildren(IEnumerable<GameObject> gameObjects) {
        foreach (var go in gameObjects) {
            var parent = go.transform.parent;
            if (parent != null) {
                if (gameObjects.First((g) => parent == g) == null) {
                    yield return go;
                }
            } else {
                yield return go;
            }
        }
    }

    public Transform SourceTransform() {
        return MadTransform.GetOrCreateChild<Transform>(transform, "Source");
    }

    public Transform CombinedTransform() {
        return MadTransform.GetOrCreateChild<Transform>(transform, "Combined");
    }

    public IEnumerable<MadMeshCombinerMesh> CombinedObjects() {
        var combined = CombinedTransform();
        int childCount = combined.childCount;
        for (int i = 0;i < childCount; ++i) {
            yield return combined.GetChild(i).GetComponent<MadMeshCombinerMesh>();
        }
    }

    public IEnumerable<GameObject> SourceObjects() {
        var source = SourceTransform();
        int childCount = source.childCount;
        for (int i = 0; i < childCount; ++i) {
            yield return source.GetChild(i).gameObject;
        }
    }

    public GameObject[] OriginGameObjects() {
        var result = new List<GameObject>();
        foreach (var origin in sourceOrigins) {
            if (origin.sourceObject == null) {
                continue;
            }

            result.Add(origin.sourceObject.gameObject);
        }

        return result.ToArray();
    }

    #endregion

    #region Private Methods

    #endregion

    public void MarkCombineFailed() {
        saveFolderGUID = null;
    }

    public void RemoveSources() {
        var sources = SourceTransform();
        while (sources.transform.childCount > 0) {
            var child = sources.transform.GetChild(0);
            DestroyImmediate(child.gameObject);
        }
    }


    #region Public Static Methods
    #endregion

    #region Inner and Anonymous Classes

    public enum SourceAction {
        DoNothing,
        DisableRenderers,
        DisableGameObjects,
    }

    [Serializable]
    public class SourceOrigin {
        public Transform sourceObject;
        public Transform originParent;
        public bool parentNull;

        public bool parentRemoved {
            get {
                if (!parentNull && originParent == null) {
                    return true;
                }

                return false;
            }
        }

        public SourceOrigin(GameObject go) {
            sourceObject = go.transform;
            originParent = go.transform.parent;
            if (originParent == null) {
                parentNull = true;
            }
        }

    }

    #endregion
}

} // namespace