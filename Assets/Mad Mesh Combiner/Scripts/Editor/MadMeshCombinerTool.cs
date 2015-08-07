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

public class MadMeshCombinerTool : MonoBehaviour {

    #region Fields

    public static int maxVerticesPerMesh = 65000;

    public static bool preserveLightmapping;

    #endregion

    #region Analyze

    public static List<ObjectGroup> AnalyzeSelected() {
        return Analyze(Selection.gameObjects);
    }

    public static List<ObjectGroup> Analyze(IEnumerable<GameObject> objects) {
        List<ObjectRef> allObjectRefs = new List<ObjectRef>();

        // collect all object refs
        foreach (var obj in objects) {
            var scanned = Scan(obj);
            allObjectRefs.AddRange(RemoveDisabled(scanned));
        }

        // remove duplicates references and apply these with higher top-level object
        for (int i = 0; i < allObjectRefs.Count;) {
            var objRef = allObjectRefs[i];

            // ReSharper disable once PossibleUnintendedReferenceComparison
            int otherRefIndex = allObjectRefs.FindIndex((o) => o != objRef && o.Equals(objRef));

            if (otherRefIndex != -1) {
                var otherRef = allObjectRefs[otherRefIndex];
                if (!IsHigher(objRef.topLevelObject.transform, otherRef.topLevelObject.transform)) {
                    objRef.topLevelObject = otherRef.topLevelObject;
                }

                allObjectRefs.RemoveAt(otherRefIndex);
            } else {
                i++;
            }
        }

        return SplitToGroups(allObjectRefs).ToList();
    }

    private static IEnumerable<ObjectRef> RemoveDisabled(IEnumerable<ObjectRef> objectRefs) {
        foreach (var r in objectRefs)  {
            if (r.activeInHierarchy) {
                yield return r;
            }
        }
    }

    private static IEnumerable<ObjectRef> Scan(GameObject gameObject) {
        if (gameObject.GetComponent<MeshFilter>() != null && gameObject.GetComponent<Renderer>() != null) {
            var mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
            var materials = gameObject.GetComponent<Renderer>().sharedMaterials;

            for (int i = 0; i < mesh.subMeshCount; i++) {
                if (materials.Length > i && materials[i] != null) {
                    yield return new ObjectRef(gameObject, gameObject, i);
                }
            }
        }

        var meshFilters = MadTransform.FindChildren<MeshFilter>(gameObject.transform, (o) => o.GetComponent<Renderer>() != null && o.GetComponent<Renderer>().sharedMaterial != null);
        foreach (var filter in meshFilters) {

            var mesh = filter.sharedMesh;
            var materials = filter.gameObject.GetComponent<Renderer>().sharedMaterials;

            for (int i = 0; i < mesh.subMeshCount; i++) {
                if (materials.Length > i && materials[i] != null) {
                    yield return new ObjectRef(filter.gameObject, gameObject, i);
                }
            }
        }
    }

    private static bool IsHigher(Transform a, Transform b) {
        if (MadTransform.FindParent<Transform>(a, (t) => t == b) != null) {
            return false;
        } else if (MadTransform.FindParent<Transform>(b, (t) => t == a) != null) {
            return true;
        } else {
            Debug.LogError("Transform " + a + " and " + b + " are not in the same tree", a);
            return false;
        }
    }

    #endregion

    #region Managed

    public static void RecombineManaged(MadMeshCombinerManaged managed) {
        if (MadTrialEditor.isTrialVersion && MadTrialEditor.expired) {
            TrialInfo.TrialEnded();
            return;
        }

        var saveFolder = GetSaveFolder(managed);
        if (string.IsNullOrEmpty(saveFolder)) {
            return;
        }

        preserveLightmapping = managed.preserveLightmapping;

        // store save folder
        managed.saveFolderGUID = AssetDatabase.AssetPathToGUID(saveFolder);

        // remove combined
        MadMeshCombinerManagedEditor.RemoveCombined(managed);

        // enable source objects
        managed.EnableSources();

        // read source and generate new combinerMeshes
        var sourceObjects = managed.SourceObjects();
        var objectGroups = Analyze(sourceObjects);

        groupCount = objectGroups.Count;
        groupNum = 0;

        try {
            foreach (var group in objectGroups) {
                groupNum++;
                Combine(saveFolder, group, managed.CombinedTransform());
            }
        } finally {
            ProgressEnd();
        }

        // disable source objects
        managed.DisableSources();
        managed.combinedMeshEnabled = true;
    }

    public static void CombineSimple(IEnumerable<GameObject> sourceObjects) {
        var saveFolder = GetSaveFolder(null);
        if (string.IsNullOrEmpty(saveFolder)) {
            return;
        }

        var objectGroups = Analyze(sourceObjects);

        groupNum = 0;
        groupCount = objectGroups.Count;

        try {
            foreach (var group in objectGroups) {
                groupNum++;
                Combine(saveFolder, group, null);
            }
        } finally {
            ProgressEnd();
        }
    }

    private static string GetSaveFolder(MadMeshCombinerManaged managed) {
        if (managed != null && !string.IsNullOrEmpty(managed.saveFolderGUID)) {
            var dir = AssetDatabase.GUIDToAssetPath(managed.saveFolderGUID);
            if (!string.IsNullOrEmpty(dir)) {
                return dir;
            }
        }

        if (!string.IsNullOrEmpty(EditorApplication.currentScene)) {
            var sceneDir = System.IO.Path.GetDirectoryName(EditorApplication.currentScene);
            if (!System.IO.Directory.Exists(sceneDir + "/Combined")) {
                AssetDatabase.CreateFolder(sceneDir, "Combined");
                AssetDatabase.Refresh();
            }
            return sceneDir + "/Combined";
        } else {
            try {
                var combinedPath = EditorPrefs.GetString("MadMeshCombiner_CombinedPath", "Assets");
                var newPath = EditorUtility.SaveFolderPanel("Mesh Save Folder", combinedPath, "Combined");
                if (newPath.StartsWith(Application.dataPath)) {
                    newPath = "Assets" + newPath.Substring(Application.dataPath.Length);
                }

                EditorPrefs.SetString("MadMeshCombiner_CombinedPath", newPath);
                return newPath;
            } finally {
                AssetDatabase.Refresh();
            }
        }
    }

    #endregion

    #region Combine

    private static IEnumerable<ObjectGroup> SplitToGroups(IEnumerable<ObjectRef> objects) {
        Dictionary<string, ObjectGroup> groups = new Dictionary<string, ObjectGroup>();

        foreach (var obj in objects) {
            var material = obj.material;
            string key = CreateKey(obj);

            ObjectGroup group;
            if (groups.ContainsKey(key)) {
                group = groups[key];
            } else {
                group = new ObjectGroup(material, obj.lightmapIndex);
                groups[key] = group;
            }

            group.objects.Add(obj);
        }

        var splitted = SplitGroupsByTextureArea(groups.Values);
        splitted = SplitGroupsByVerticesCount(splitted);

        return splitted;
    }

    // creates a key for a material
    // when material offset and scale are non-standard, then key contains texture guid
    // because tiled meshes shouldn't have an atlas and should be grupped by texture
    private static string CreateKey(ObjectRef reference) {
        var material = reference.material;
        string shaderName = material.shader.name;

        string colorKey = ColorKey(material);
        string cubeMapKey = CubeMapKey(material);
        int lightmappingKey = reference.lightmapIndex;

        if (reference.isTiled) {
            var mainTexturePath = AssetDatabase.GetAssetPath(material.mainTexture);
            var guid = AssetDatabase.AssetPathToGUID(mainTexturePath);
            return string.Format("{0}, {1}, {2}, {3}, {4}", colorKey, cubeMapKey, lightmappingKey, shaderName, guid);
        } else {
            return string.Format("{0}, {1}, {2}, {3}", colorKey, cubeMapKey, lightmappingKey, shaderName);
        }
    }

    private static string ColorKey(Material material) {
        Color main = ColorOrWhite(material, "_Color");
        Color spec = ColorOrWhite(material, "_SpecColor");
        Color emission = ColorOrWhite(material, "_Emission");
        Color reflect = ColorOrWhite(material, "_Reflect");

        return string.Format("{0} {1} {2} {3}", main, spec, emission, reflect);
    }

    private static string CubeMapKey(Material material) {
        if (material.HasProperty("_Cube")) {
            Texture cube = material.GetTexture("_Cube");
            if (cube == null) {
                return "";
            }

            return cube.GetInstanceID().ToString();
        } else {
            return "";
        }
    }

    private static Color ColorOrWhite(Material material, string name) {
        return material.HasProperty(name) ? material.GetColor(name) : Color.white;
    }

    private static string FindFileName(string format) {
        int counter = 1;
        string resultPath;
        do {
            resultPath = string.Format(format, counter);
            counter++;
        } while (System.IO.File.Exists(resultPath));

        return resultPath;
    }
        
    public static MadMeshCombinerMesh Combine(string directory, ObjectGroup group, Transform parent = null) {
        var objects = group.objects;

        Texture2D[] mainTextures = null, bumpMapTextures = null;

        if (group.hasMainTexture) {
            mainTextures = group.MainTextures();
        }

        if (group.hasBumpMap) {
            bumpMapTextures = group.BumpMapTextures();
        }

        string meshPath = FindFileName(directory + "/CombinedMesh{0}.asset");

        MadMeshCombinerAtlas mainAtlas = null, bumpAtlas = null;
        bool usingAtlas = false;

        if (mainTextures != null && mainTextures.Length > 1) {

            Progress(0, objects.Count());

            string mainAtlasTexturePath = System.IO.Path.ChangeExtension(meshPath, "png");
            mainAtlas = MadMeshCombinerAtlasBuilder.CreateAtlas(mainAtlasTexturePath, mainTextures, group.shader);

            if (group.hasBumpMap) {
                string bumpAtlasTexturePath =
                    System.IO.Path.GetDirectoryName(meshPath) + "/"
                    + System.IO.Path.GetFileNameWithoutExtension(meshPath)
                    + "_Bump.png";

                bumpAtlas = MadMeshCombinerAtlasBuilder.CreateAtlas(bumpAtlasTexturePath, bumpMapTextures);

                // set bump atlas as normal map
                var texturePath = AssetDatabase.GetAssetPath(bumpAtlas.atlasTexture);
                var importer = TextureImporter.GetAtPath(texturePath) as TextureImporter;
                importer.textureType = TextureImporterType.Bump;
                importer.normalmap = true;
                AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            }

            usingAtlas = true;

            if (mainAtlas == null) {
                return null;
            }
        }

        Vector3 middlePoint = MiddlePoint(objects);

        List<Vector3> allVertices = new List<Vector3>();
        List<Vector3> allNormals = new List<Vector3>();
        List<Vector4> allTangents = new List<Vector4>();
        List<Color> allColors = new List<Color>();
        List<Vector2> allUvs = new List<Vector2>();
        List<Vector2> allUvs2 = new List<Vector2>();
        List<int> allTriangles = new List<int>();

        int objNum = 0;
        foreach (var obj in objects) {
            objNum++;
            Progress(objNum, objects.Count());

            Vector3 translate = obj.position - middlePoint;
            Quaternion rotate = obj.rotation;
            Vector3 scale = obj.lossyScale;

            int offset = allVertices.Count;

            var material = obj.material;

            /* [me]
            if (material.mainTexture == null) { // texutre set to null
                continue;
            }
            */

            bool reverseTriangles = false;
            Vector3 multiplier = Vector3.zero;

            for (int i = 0; i < 3; ++i) {
                multiplier[i] = Math.Sign(scale[i]);
                if (multiplier[i] < 0) {
                    reverseTriangles = !reverseTriangles;
                }
            }

            // vertices
            var vertices = obj.vertices;
            foreach (var v in vertices) {
                var scaled = new Vector3(v.x * scale.x, v.y * scale.y, v.z * scale.z);

                var rotated = rotate * scaled;
                var translated = rotated + translate;
                allVertices.Add(translated);
            }

            // normals
            var normals = obj.normals;
            foreach (var normal in normals) {
                allNormals.Add(rotate * Multiply(normal, multiplier));
            }

            // tangents
            var tangents = obj.tangents;
            foreach (var tangent in tangents) {
                var t = rotate * Multiply(tangent, multiplier);
                allTangents.Add(new Vector4(t.x, t.y, t.z, tangent.w));
            }
                
            // colors
            var colors = obj.colors;
            var vertexCount = obj.vertexCount;
            if (colors.Count() == vertexCount) {
                allColors.AddRange(colors);
            } else { // when there are no colors, I am adding white
                for (int i = 0; i < vertexCount; ++i) {
                    allColors.Add(Color.white);
                }
            }

            // UVs
            if (usingAtlas) {

                if (material.mainTexture == null) {
                    allUvs.AddRange(obj.uv);
                } else {
                    var textureGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material.mainTexture));
                    var atlasItem = mainAtlas.GetItem(textureGUID);

                    var uvs = obj.uv;

                    foreach (var uv in uvs) {
                        float u = uv.x * atlasItem.region.width + atlasItem.region.x;
                        float v = uv.y * atlasItem.region.height + atlasItem.region.y;
                        allUvs.Add(new Vector2(u, v));
                    }
                }

            } else {
                allUvs.AddRange(obj.uv);
            }

            // UV2 (lightmapping)
            if (preserveLightmapping) {
                if (group.hasUv2 || group.hasLightmap) {
                    Vector2[] source;

                    var uv2 = obj.uv2;
                    if (uv2.Count > 0) {
                        source = obj.uv2.ToArray();
                    } else {
                        source = obj.uv.ToArray();
                    }

                    Vector4 to = obj.lightmapTilingOffset;
                    Vector2 sc = to;
                    Vector2 off = new Vector2(to.z, to.w);

                    foreach (var uv in source) {
                        float u = (uv.x * sc.x + off.x);
                        float v = (uv.y * sc.y + off.y);

                        allUvs2.Add(new Vector2(u, v));
                    }
                }
            }


            // triangles
            var triangles = obj.triangles;

            int[] triangle = new int[3];
            int j = 0;

            foreach (int t in triangles) {
                triangle[j++] = t;
                if (j == 3) {
                    if (reverseTriangles) {
                        triangle = triangle.Reverse().ToArray();
                    }

                    foreach (var index in triangle) {
                        allTriangles.Add(index + offset);
                    }

                    j = 0;
                }
            }
        }

        var bigMesh = new Mesh();

        bigMesh.vertices = allVertices.ToArray();
        bigMesh.colors = allColors.ToArray();
        bigMesh.normals = allNormals.ToArray();
        bigMesh.tangents = allTangents.ToArray();
        bigMesh.uv = allUvs.ToArray();
        if (allUvs2.Count > 0) {
            bigMesh.uv2 = allUvs2.ToArray();
        }
        bigMesh.triangles = allTriangles.ToArray();

        //bigMesh.RecalculateNormals();
        bigMesh.Optimize();

        AssetDatabase.CreateAsset(bigMesh, meshPath);
        AssetDatabase.Refresh();

        bigMesh = AssetDatabase.LoadAssetAtPath(meshPath, typeof(Mesh)) as Mesh;

        var go = new GameObject("combined");
        if (group.allStatic) {
            go.isStatic = true;
        }

        go.transform.position = middlePoint;
        var filter = go.AddComponent<MeshFilter>();
        filter.mesh = bigMesh;

        go.AddComponent<MeshRenderer>();
        if (usingAtlas) {
            go.GetComponent<Renderer>().sharedMaterial = mainAtlas.atlasMaterial;
            CopyColors(group.material, go.GetComponent<Renderer>().sharedMaterial);

            if (group.hasBumpMap) {
                go.GetComponent<Renderer>().sharedMaterial.SetTexture("_BumpMap", bumpAtlas.atlasTexture);
            }
        } else {
            go.GetComponent<Renderer>().sharedMaterial = group.material;
        }

        go.GetComponent<Renderer>().lightmapIndex = group.lightmapIndex;

        var combinerMesh = go.AddComponent<MadMeshCombinerMesh>();
        combinerMesh.generatedMesh = bigMesh;

        if (usingAtlas) {
            combinerMesh.generatedMaterials.Add(mainAtlas.atlasMaterial);
            combinerMesh.generatedTextures.Add(mainAtlas.atlasTexture);

            if (group.hasBumpMap) {
                combinerMesh.generatedTextures.Add(bumpAtlas.atlasTexture);
            }
        }

        if (group.hasCubeMap) {
            go.GetComponent<Renderer>().sharedMaterial.SetTexture("_Cube", group.cubeMap);
        }

        go.transform.parent = parent;

        return combinerMesh;
    }

    private static Vector3 Multiply(Vector3 a, Vector3 b) {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    private static void CopyColors(Material src, Material dest) {
        CopyColor(src, dest, "_Color");
        CopyColor(src, dest, "_SpecColor");
        CopyColor(src, dest, "_Emission");
        CopyColor(src, dest, "_ReflectColor");
    }

    private static void CopyColor(Material src, Material dest, string name) {
        if (src.HasProperty(name)) {
            dest.SetColor(name, src.GetColor(name));
        }
    }

    private static IEnumerable<Color> CombineColors(IEnumerable<Color> colors, Color c) {
        foreach (var color in colors) {
            yield return c * color;
        }
    }

    private static IEnumerable<GameObject> Filter(GameObject[] objects) {
        var query = from o in objects where o.GetComponent<MeshFilter>() != null && o.GetComponent<Renderer>().sharedMaterial != null select o;
        return query;
    }

    private static Vector3 MiddlePoint(IEnumerable<ObjectRef> objects) {
        Bounds bounds = new Bounds();
        bool first = true;

        foreach (var obj in objects) {
            if (first) {
                bounds.SetMinMax(obj.position, obj.position);
            } else {
                bounds.Expand(obj.position);
            }
        }

        return bounds.center;
    }

    #endregion

    #region Optimization

    private static IEnumerable<ObjectGroup> SplitGroupsByTextureArea(IEnumerable<ObjectGroup> groups) {
        int maxFill = (int) (4096 * 4096 * 0.8f);

        foreach (var group in groups) {
            int fill = group.CountTextureArea();
            if (fill <= maxFill) {
                yield return group;
            } else {
                fill = 0;
                ObjectGroup newGroup = new ObjectGroup(group.material, group.lightmapIndex);

                foreach (var obj in group.objects) {
                    var texture = obj.material.mainTexture;
                    int textureFill = texture.width * texture.height;

                    fill += textureFill;

                    if (fill <= maxFill) {
                        newGroup.objects.Add(obj);
                    } else {
                        yield return newGroup;

                        newGroup = new ObjectGroup(group.material, group.lightmapIndex);
                        newGroup.objects.Add(obj);

                        fill = textureFill;
                    }
                }

                yield return newGroup;
            }
        }
    }

    private static IEnumerable<ObjectGroup> SplitGroupsByVerticesCount(IEnumerable<ObjectGroup> groups) {
        int maxCount = maxVerticesPerMesh;

        foreach (var group in groups) {
            var currentCount = group.CountVerticles();
            if (currentCount <= maxCount) {
                yield return group;
            } else {
                currentCount = 0;
                ObjectGroup newGroup = new ObjectGroup(group.material, group.lightmapIndex);

                foreach (var obj in group.objects) {
                    var vertexCount = obj.vertexCount;
                    currentCount += vertexCount;

                    if (currentCount <= maxCount) {
                        newGroup.objects.Add(obj);
                    } else {
                        yield return newGroup;

                        newGroup = new ObjectGroup(group.material, group.lightmapIndex);
                        newGroup.objects.Add(obj);

                        currentCount = vertexCount;
                    }
                }

                yield return newGroup;
            }
        }
    }

    #endregion

    #region Progress

    private static int groupCount;
    private static int groupNum;

    private static void Progress(int objectNum, int objectCount) {
        float stage = 1 / (float) groupCount;
        float progress = (groupNum - 1) / (float) groupCount;
        progress += stage * (objectNum / (float) objectCount);

        string info;
        if (objectNum == 0) {
            info = string.Format("Creating atlas (group {0} of {1})", groupNum, groupCount);
        } else {
            info = string.Format("Combining {0} of {1} meshes (group {2} of {3})",
                    objectNum, objectCount, groupNum, groupCount);
        }

        EditorUtility.DisplayProgressBar(
            "Combining Meshes...", info, progress);
    }

    private static void ProgressEnd() {
        EditorUtility.ClearProgressBar();
    }

    #endregion

    #region Inner Types

    public class MeshView {
        private Mesh mesh;
        private int submeshIndex;

        private Dictionary<int, int> vertexIndexMappingToTarget;
        private List<int> vertexIndexToSource;

        public MeshView(Mesh mesh, int submeshIndex) {
            this.mesh = mesh;
            this.submeshIndex = submeshIndex;
        }

        public List<int> triangles {
            get {
                var result = new List<int>();
                foreach (var vertIndex in mesh.GetTriangles(submeshIndex)) {
                    result.Add(MapVertexIndex(vertIndex));
                }

                return result;
            }
        }

        private void CreateVertexMapping() {
            if ((submeshIndex == 0 && mesh.subMeshCount == 1) || vertexIndexMappingToTarget != null) {
                return;
            }

            vertexIndexMappingToTarget = new Dictionary<int, int>();
            vertexIndexToSource = new List<int>();
            var triangles = new List<int>(mesh.GetTriangles(submeshIndex));

            triangles.Sort();
            triangles = triangles.Distinct().ToList();

            int i = 0;
            foreach (var triangle in triangles) {
                vertexIndexMappingToTarget.Add(triangle, i);
                vertexIndexToSource.Add(triangle);

                i++;
            }
        }

        private int MapVertexIndex(int index) {
            CreateVertexMapping();
            if (vertexIndexMappingToTarget != null) {
                return vertexIndexMappingToTarget[index];
            }

            return index;
        }

        public int vertexCount {
            get {
                CreateVertexMapping();
                if (vertexIndexMappingToTarget != null) {
                    return vertexIndexMappingToTarget.Count;
                }

                return mesh.vertexCount;
            }
        }

        public List<Vector3> vertices {
            get {
                var result = new List<Vector3>();
                var mesh = this.mesh;

                int count = vertexCount;
                if (vertexIndexMappingToTarget != null) {
                    for (int i = 0; i < count; i++) {
                        int index = vertexIndexToSource[i];
                        var vertex = mesh.vertices[index];
                        result.Add(vertex);
                    }
                } else {
                    result.AddRange(mesh.vertices);
                }

                return result;
            }
        }

        public List<Vector3> normals {
            get {
                var result = new List<Vector3>();
                var mesh = this.mesh;

                int count = vertexCount;
                if (vertexIndexMappingToTarget != null) {
                    for (int i = 0; i < count; i++) {
                        int index = vertexIndexToSource[i];
                        var normal = mesh.normals[index];
                        result.Add(normal);
                    }
                } else {
                    result.AddRange(mesh.normals);
                }

                return result;
            }
        }

        public List<Vector4> tangents {
            get {
                var result = new List<Vector4>();
                var mesh = this.mesh;

                int count = vertexCount;
                if (vertexIndexMappingToTarget != null) {
                    for (int i = 0; i < count; i++) {
                        int index = vertexIndexToSource[i];
                        if (mesh.tangents.Length > index) {
                            var tangent = mesh.tangents[index];
                            result.Add(tangent);
                        }
                    }
                } else {
                    result.AddRange(mesh.tangents);
                }

                return result;
            }
        }

        public List<Color> colors {
            get {
                var result = new List<Color>();
                var mesh = this.mesh;

                int count = vertexCount;
                if (vertexIndexMappingToTarget != null) {
                    for (int i = 0; i < count; i++) {
                        int index = vertexIndexToSource[i];
                        if (mesh.colors.Length > index) {
                            var color = mesh.colors[index];
                            result.Add(color);
                        }
                    }
                } else {
                    result.AddRange(mesh.colors);
                }

                return result;
            }
        }

        public List<Vector2> uv {
            get {
                var result = new List<Vector2>();
                var mesh = this.mesh;

                int count = vertexCount;
                if (vertexIndexMappingToTarget != null) {
                    for (int i = 0; i < count; i++) {
                        int index = vertexIndexToSource[i];
                        var uv = mesh.uv[index];
                        result.Add(uv);
                    }
                } else {
                    result.AddRange(mesh.uv);
                }

                return result;
            }
        }

        public List<Vector2> uv2 {
            get {
                var result = new List<Vector2>();
                var mesh = this.mesh;

                int count = vertexCount;
                if (vertexIndexMappingToTarget != null) {
                    for (int i = 0; i < count; i++) {
                        int index = vertexIndexToSource[i];
                        var uv = mesh.uv2[index];
                        result.Add(uv);
                    }
                } else {
                    result.AddRange(mesh.uv2);
                }

                return result;
            }
        }

        public bool isTiled {
            get {
                if (_isTiledCached) {
                    return isTiled;
                }

                foreach (var uv in this.uv) {
                    if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) {
                        _isTiled = true;
                        break;
                    }
                }

                _isTiledCached = true;
                return _isTiled;
            }
        }

        private bool _isTiled;
        private bool _isTiledCached;
    }

    public class ObjectRef {
        public GameObject topLevelObject;

        private GameObject gameObject;
        private int submeshIndex;

        private MeshView meshView;

        public Material material {
            get { return gameObject.GetComponent<Renderer>().sharedMaterials[submeshIndex]; }
        }

        public bool activeInHierarchy {
            get { return gameObject.activeInHierarchy; }
        }

        public int lightmapIndex {
            get { return gameObject.GetComponent<Renderer>().lightmapIndex; }
        }

        private Mesh mesh {
            get {
                var meshFilter = gameObject.GetComponent<MeshFilter>();
                var mesh = meshFilter.sharedMesh;
                return mesh;
            }
        }

        public List<int> triangles {
            get { return meshView.triangles; }
        }

        public int vertexCount {
            get { return meshView.vertexCount; }
        }

        public List<Vector3> vertices {
            get { return meshView.vertices; }
        }

        public List<Vector3> normals {
            get { return meshView.normals; }
        }

        public List<Vector4> tangents {
            get { return meshView.tangents; }
        }

        public List<Color> colors {
            get { return meshView.colors; }
        }

        public List<Vector2> uv {
            get { return meshView.uv; }
        }

        public List<Vector2> uv2 {
            get { return meshView.uv2; }
        }



        public bool isStatic {
            get { return gameObject.isStatic; }
        }

        public bool isTiled {
            get {
                if (material.mainTextureOffset != Vector2.zero || material.mainTextureScale != Vector2.one) {
                    return true;
                }

                if (meshView.isTiled) {
                    return true;
                }

                return false;
            }
        }

        public Vector3 position {
            get { return gameObject.transform.position; }
        }

        public Quaternion rotation {
            get { return gameObject.transform.rotation; }
        }

        public Vector3 lossyScale {
            get { return gameObject.transform.lossyScale; }
        }

        public Vector4 lightmapTilingOffset {
            get { return gameObject.GetComponent<Renderer>().lightmapScaleOffset; }
        }


        public ObjectRef(GameObject gameObject, GameObject topLevelObject, int submeshIndex = 0) {
            this.gameObject = gameObject;
            this.topLevelObject = topLevelObject;
            this.submeshIndex = submeshIndex;

            meshView = new MeshView(mesh, submeshIndex);
        }

        protected bool Equals(ObjectRef other) {
            return gameObject.Equals(other.gameObject) && submeshIndex == other.submeshIndex;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != this.GetType()) {
                return false;
            }
            return Equals((ObjectRef) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return (gameObject.GetHashCode() * 397) ^ submeshIndex;
            }
        }
    }

    public class ObjectGroup {
        public List<ObjectRef> objects = new List<ObjectRef>();
        public Material material;
        public Shader shader;
        public int lightmapIndex = 255;

        public bool hasLightmap {
            get {
                return lightmapIndex != 255;
            }
        }

        public bool hasMainTexture {
            get {
                return material.HasProperty("_MainTex");
            }
        }

        public bool hasBumpMap {
            get {
                return material.HasProperty("_BumpMap");
            }
        }

        public bool hasCubeMap {
            get {
                return material.HasProperty("_Cube");
            }
        }

        public Cubemap cubeMap {
            get {
                return (Cubemap) material.GetTexture("_Cube");
            }
        }

        public bool hasUv2 {
            get {
                if (!_analyzedUv2) {
                    foreach (var obj in objects) {
                        if (obj.uv2.Count > 0) {
                            _hasUv2 = true;
                            break;
                        }
                    }
                    _analyzedUv2 = true;
                }

                return _hasUv2;
            }
        }

        public bool allStatic {
            get {
                foreach (var obj in objects) {
                    if (!obj.isStatic) {
                        return false;
                    }
                }

                return true;
            }
        }

        private bool _analyzedUv2 = false;
        private bool _hasUv2;

        public ObjectGroup(Material material, int lightmapIndex) {
            this.material = material;
            this.shader = material.shader;
            this.lightmapIndex = lightmapIndex;
        }

        public Texture2D[] MainTextures() {
            return RemoveNull(objects.Select(o => (Texture2D) o.material.mainTexture).Distinct()).ToArray();
        }

        public Texture2D[] BumpMapTextures() {
            return RemoveNull(objects.Select(o => (Texture2D) o.material.GetTexture("_BumpMap")).Distinct()).ToArray();
        }

        private IEnumerable<T> RemoveNull<T>(IEnumerable<T> elements) {
            return from el in elements where el != null select el;
        }

        public int CountVerticles() {
            int count = 0;

            foreach (var obj in objects) {
                count += obj.vertexCount;
            }

            return count;
        }

        public int CountTextureArea() {
            var allTextures = from ro in objects where ro.material.mainTexture != null select ro.material.mainTexture;
            var uniqueTextures = allTextures.Distinct();
            var uniqueTextureSizes = from t in uniqueTextures select t.width * t.height;
            int total = uniqueTextureSizes.Sum();

            return total;
        }

    }

    #endregion
}

} // namespace