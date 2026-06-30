using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// One-off helper: merges the ProBuilder map scene (withaisles) into the
// fully-wired GameScene so the map gains GameScene's camera/player/UI.
// Run from the menu: Tools > Merge Map into GameScene
public static class MapMerger
{
    const string DEST = "Assets/Scenes/GameScene.unity";
    const string SRC  = "Assets/withaisles.unity";

    [MenuItem("Tools/Merge Map into GameScene")]
    public static void MergeMap()
    {
        // let the user save anything open first
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var dest = EditorSceneManager.OpenScene(DEST, OpenSceneMode.Single);
        var src  = EditorSceneManager.OpenScene(SRC, OpenSceneMode.Additive);

        // moves every root object from withaisles into GameScene (handles ids/ProBuilder properly)
        EditorSceneManager.MergeScenes(src, dest);

        // remove the leftover StarterAssets player (now a broken/missing-script object)
        var capsule = GameObject.Find("PlayerCapsule");
        if (capsule != null) Object.DestroyImmediate(capsule);

        EditorSceneManager.MarkSceneDirty(dest);
        EditorSceneManager.SaveScene(dest);

        Debug.Log("[MapMerger] Map merged into GameScene. " +
                  "Next: delete the old prototype floor/walls and move the Player onto the new floor, then save.");
    }

    // Removes the old prototype level geometry from the currently open scene (GameScene).
    // Run from the menu: Tools > Delete Old Prototype Map
    [MenuItem("Tools/Delete Old Prototype Map")]
    public static void DeleteOldMap()
    {
        string[] names = {
            "Ground",
            "Wall_N", "Wall_S", "Wall_E", "Wall_W",
            "Room_N", "Room_S_R", "Room_S_L", "Room_SW", "Room_SE",
            "Obstacle_0", "Obstacle_1", "Obstacle_2", "Obstacle_3",
            "Obstacle_4", "Obstacle_5", "Obstacle_6", "Obstacle_7",
            "Environment", // container, deleted last in case the above were its children
        };

        int deleted = 0;
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null)
            {
                Debug.Log("[Cleanup] Deleted " + n);
                Object.DestroyImmediate(go);
                deleted++;
            }
        }

        if (deleted > 0)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }
        Debug.Log($"[Cleanup] Removed {deleted} old prototype object(s). Now move the Player onto the new floor.");
    }

    // Moves the Player to the spawn point manmeet used for the StarterAssets player in withaisles.
    // Run from the menu: Tools > Place Player at Map Spawn
    [MenuItem("Tools/Place Player at Map Spawn")]
    public static void PlacePlayer()
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[Spawn] No active 'Player' object found in the open scene.");
            return;
        }

        Undo.RecordObject(player.transform, "Place Player at Map Spawn");
        player.transform.position = new Vector3(0f, 0f, 1.9f); // manmeet's spawn (world)
        player.transform.rotation = Quaternion.identity;       // facing +Z

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Spawn] Player moved to manmeet's spawn (0, 0, 1.9). If it sinks into the floor, raise the Y a little.");
    }

    // Removes leftover missing-prefab instances (e.g. the deleted PlayerCapsule) and recolours the
    // white map meshes to manmeet's grey-blue. Run from: Tools > Clean Scene and Recolor Map
    [MenuItem("Tools/Clean Scene and Recolor Map")]
    public static void CleanAndRecolor()
    {
        var scene = SceneManager.GetActiveScene();

        // 1) drop any root object whose prefab asset is missing (the broken PlayerCapsule)
        int removed = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root != null && PrefabUtility.IsPrefabAssetMissing(root))
            {
                Debug.Log("[Clean] Removing missing prefab instance: " + root.name);
                Object.DestroyImmediate(root);
                removed++;
            }
        }

        // 2) build/load a grey Built-in material using manmeet's GreyBlue colour
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        const string matPath = "Assets/Materials/MapGrey.mat";
        var grey = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (grey == null)
        {
            grey = new Material(Shader.Find("Standard"));
            grey.color = new Color(0.511f, 0.6265f, 0.6887f, 1f);
            grey.SetFloat("_Glossiness", 0.1f);
            grey.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(grey, matPath);
        }

        // 3) swap every mesh still on the white built-in default over to the grey material
        var def = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
        int recolored = 0;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] == def || mats[i] == null) { mats[i] = grey; changed = true; }
            if (changed) { r.sharedMaterials = mats; recolored++; }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[Clean] Removed {removed} missing prefab(s); recoloured {recolored} mesh(es) to grey.");
    }

    // Groups loose root objects under empty "folder" containers in the Hierarchy.
    // Run from: Tools > Organize Hierarchy
    [MenuItem("Tools/Organize Hierarchy")]
    public static void OrganizeHierarchy()
    {
        var scene = SceneManager.GetActiveScene();
        var map      = GetOrCreate("--- Map ---", scene);
        var lighting = GetOrCreate("--- Lighting ---", scene);
        var systems  = GetOrCreate("--- Systems ---", scene);

        int moved = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            var t = root.transform;
            if (t == map || t == lighting || t == systems) continue;

            string n = root.name.ToLower();
            bool isMap   = n == "floor" || n.StartsWith("wall") || n.StartsWith("aisle") ||
                           n.StartsWith("asile") || n == "door" || n.StartsWith("pb_mesh");
            bool isLight = root.GetComponent<Light>() != null;
            bool isMgr   = root.GetComponent<GameManager>() != null;

            Transform parent = isMap ? map : isLight ? lighting : isMgr ? systems : null;
            if (parent != null)
            {
                Undo.SetTransformParent(t, parent, "Organize Hierarchy");
                t.SetParent(parent, true); // keep world position
                moved++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[Organize] Grouped {moved} object(s). Player / Canvas / WorldItems left at root (already clear).");
    }

    // Aligns a selected child model so its feet sit on the parent collider's bottom and it is
    // centred horizontally on the parent. Select the imported model (child of NPC/Zombie) first.
    // Run from: Tools > Center Selected Model on Parent
    [MenuItem("Tools/Center Selected Model on Parent")]
    public static void CenterModel()
    {
        var go = Selection.activeGameObject;
        if (go == null || go.transform.parent == null)
        {
            Debug.LogError("[Center] Select the model that is a CHILD of the NPC/Zombie object first.");
            return;
        }

        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError("[Center] The selection has no renderers to measure.");
            return;
        }

        Bounds model = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) model.Encapsulate(renderers[i].bounds);

        var parent = go.transform.parent;
        var col = parent.GetComponentInChildren<Collider>();   // CapsuleCollider or CharacterController
        float floorY = col != null ? col.bounds.min.y : parent.position.y;

        Vector3 delta = new Vector3(
            parent.position.x - model.center.x,   // centre horizontally on the parent
            floorY - model.min.y,                 // feet onto the collider bottom
            parent.position.z - model.center.z);

        Undo.RecordObject(go.transform, "Center Model");
        go.transform.position += delta;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Center] Model centred on parent; feet aligned to the collider bottom.");
    }

    static Transform GetOrCreate(string name, UnityEngine.SceneManagement.Scene scene)
    {
        foreach (var r in scene.GetRootGameObjects())
            if (r.name == name) return r.transform;
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go.transform;
    }
}
