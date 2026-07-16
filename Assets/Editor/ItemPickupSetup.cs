using System.IO;
using UnityEditor;
using UnityEngine;

// Turns placeholder meshes into working pickups in bulk. Select the meshes for ONE
// item in the Hierarchy, pick that item's ItemData, and Convert: each mesh gets a
// collider, the Interactable layer, and a WorldItem wired to the data. With "Create /
// reuse prefab" on, one plain prefab is saved to Assets/Prefabs and every selected
// mesh becomes an instance of it (matching the project's plain-prefab convention) — so
// the icon exporter can then auto-fill the icon. The whole pass is one Undo step.
//
// Open via Tools > M2 > Item Pickup Setup.
public class ItemPickupSetup : EditorWindow
{
    const string PrefabFolder = "Assets/Prefabs";

    ItemData itemData;
    bool     createPrefab = true;

    [MenuItem("Tools/M2/Item Pickup Setup")]
    static void open() => GetWindow<ItemPickupSetup>("Item Pickup Setup");

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Select the placeholder meshes for ONE item in the Hierarchy, pick that " +
            "item's data, then Convert. Each mesh gets a collider, the Interactable " +
            "layer, and a WorldItem. Select meshes of the same item together.",
            MessageType.Info);

        itemData     = (ItemData)EditorGUILayout.ObjectField("Item Data", itemData, typeof(ItemData), false);
        createPrefab = EditorGUILayout.Toggle("Create / reuse prefab", createPrefab);

        int count = sceneSelection().Length;
        using (new EditorGUI.DisabledScope(itemData == null || count == 0))
            if (GUILayout.Button(count > 0 ? $"Convert {count} Selected" : "Convert Selection"))
                convert();
    }

    void convert()
    {
        int layer = LayerMask.NameToLayer(GameManager.INTERACTABLE_LAYER_NAME);
        if (layer < 0)
        {
            Debug.LogError($"[PickupSetup] Layer '{GameManager.INTERACTABLE_LAYER_NAME}' is missing — add it under Tags & Layers first.");
            return;
        }

        GameObject[] selection = sceneSelection();

        Undo.SetCurrentGroupName("Convert To Item Pickups");
        int undoGroup = Undo.GetCurrentGroup();

        string     prefabPath  = $"{PrefabFolder}/{sanitize(itemData.itemName)}.prefab";
        GameObject prefabAsset = createPrefab ? AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) : null;
        int        converted   = 0;

        foreach (GameObject go in selection)
        {
            if (!createPrefab)
            {
                configure(go, layer);
            }
            else if (prefabAsset == null)
            {
                // first mesh becomes the shared prefab; SaveAsPrefabAssetAndConnect leaves it an instance
                configure(go, layer);
                Directory.CreateDirectory(PrefabFolder);
                prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.UserAction);
            }
            else
            {
                // reuse the prefab: drop an instance at this mesh's transform, remove the placeholder
                replaceWithInstance(go, prefabAsset);
            }
            converted++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();
        Debug.Log($"[PickupSetup] Converted {converted} mesh(es) to '{itemData.itemName}' pickups. " +
                  (createPrefab ? $"Prefab: {prefabPath}. One Ctrl+Z reverts." : "In-place only, no prefab. One Ctrl+Z reverts."));
    }

    // collider + Interactable layer + WorldItem wired to the chosen data
    void configure(GameObject go, int layer)
    {
        Undo.RecordObject(go, "Configure Pickup");
        go.layer = layer;

        if (go.GetComponent<Collider>() == null)
        {
            if (go.GetComponent<MeshFilter>()?.sharedMesh != null)
                Undo.AddComponent<MeshCollider>(go);   // matches the Food Can prefab
            else
                Undo.AddComponent<BoxCollider>(go);    // fallback for a mesh-less placeholder
        }

        WorldItem item = go.GetComponent<WorldItem>() ?? Undo.AddComponent<WorldItem>(go);
        var so = new SerializedObject(item);
        so.FindProperty("itemData").objectReferenceValue = itemData;   // itemData is [SerializeField] private
        so.ApplyModifiedProperties();
    }

    void replaceWithInstance(GameObject placeholder, GameObject prefabAsset)
    {
        Transform  t    = placeholder.transform;
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, t.parent);
        inst.transform.SetPositionAndRotation(t.position, t.rotation);
        inst.transform.localScale = t.localScale;
        inst.name = placeholder.name;
        Undo.RegisterCreatedObjectUndo(inst, "Create Pickup Instance");
        Undo.DestroyObjectImmediate(placeholder);
    }

    // Hierarchy meshes only — never project assets that happen to be selected
    static GameObject[] sceneSelection()
    {
        var list = new System.Collections.Generic.List<GameObject>();
        foreach (GameObject go in Selection.gameObjects)
            if (go.scene.IsValid())
                list.Add(go);
        return list.ToArray();
    }

    static string sanitize(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");
        return name.Trim();
    }
}
