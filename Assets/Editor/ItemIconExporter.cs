using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Batch-renders inventory icons from selected prefabs / FBX models using Unity's
// built-in asset previews — transparent background, no camera or lighting rig needed
// (which sidesteps HDRP's fiddly transparent-clear setup). Select the item prefabs or
// model assets in the Project window and run Tools > M2 > Export Item Icons. PNGs land
// in Assets/Art/ItemIcons, imported as sprites. If a selected prefab carries a
// WorldItem, its ItemData.icon slot is filled in automatically.
//
// Previews bake asynchronously on the main thread, so the work is pumped from
// EditorApplication.update one item at a time rather than in a blocking loop.
public static class ItemIconExporter
{
    const string OutputFolder = "Assets/Art/ItemIcons";
    const int    MaxWaitTicks = 200;   // give up on an item whose preview never bakes (~a few seconds)

    static Queue<GameObject> pending;
    static int waited;
    static int exported;

    [MenuItem("Tools/M2/Export Item Icons")]
    public static void exportSelectedIcons()
    {
        var targets = new List<GameObject>();
        foreach (Object obj in Selection.objects)
            if (obj is GameObject go && AssetDatabase.Contains(go))   // project assets only, not scene objects
                targets.Add(go);

        if (targets.Count == 0)
        {
            Debug.LogWarning("[IconExporter] Select item prefabs or FBX models in the Project window first.");
            return;
        }

        Directory.CreateDirectory(OutputFolder);
        pending  = new Queue<GameObject>(targets);
        waited   = 0;
        exported = 0;
        EditorApplication.update += tick;
        Debug.Log($"[IconExporter] Rendering {targets.Count} icon(s)…");
    }

    static void tick()
    {
        if (pending.Count == 0)
        {
            EditorApplication.update -= tick;
            AssetDatabase.SaveAssets();
            Debug.Log($"[IconExporter] Done — exported {exported} icon(s) to {OutputFolder}.");
            return;
        }

        GameObject go  = pending.Peek();
        Texture2D  tex = AssetPreview.GetAssetPreview(go);   // first call queues the bake, returns null until ready

        if (tex == null && waited < MaxWaitTicks)
        {
            waited++;
            return;
        }

        pending.Dequeue();
        waited = 0;

        if (tex == null)
        {
            Debug.LogWarning($"[IconExporter] No preview baked for '{go.name}' — skipped.");
            return;
        }

        string path   = $"{OutputFolder}/{go.name}.png";
        savePng(tex, path);
        Sprite sprite = importAsSprite(path);
        assignToItem(go, sprite);
        exported++;
    }

    // AssetPreview textures aren't guaranteed CPU-readable, so blit through a
    // RenderTexture into one we can encode. ARGB32 keeps the preview's alpha.
    static void savePng(Texture2D src, string path)
    {
        RenderTexture rt = RenderTexture.GetTemporary(
            src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(src, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        var readable = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
        readable.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        File.WriteAllBytes(path, readable.EncodeToPNG());
        Object.DestroyImmediate(readable);
    }

    static Sprite importAsSprite(string path)
    {
        AssetDatabase.ImportAsset(path);
        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureType        = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // A pickup prefab links its ItemData through WorldItem; fill the icon there so
    // the asset is ready to use. Raw models with no WorldItem just get a PNG on disk.
    static void assignToItem(GameObject go, Sprite sprite)
    {
        var worldItem = go.GetComponent<WorldItem>();
        if (worldItem == null) return;

        var itemProp = new SerializedObject(worldItem).FindProperty("itemData");   // itemData is [SerializeField] private
        if (itemProp.objectReferenceValue is not ItemData data) return;

        data.icon = sprite;
        EditorUtility.SetDirty(data);
    }
}
