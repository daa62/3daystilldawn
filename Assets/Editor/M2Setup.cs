using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Wires the Milestone 2 gameplay systems into the currently open scene so they don't
// have to be added by hand. Run once on GameScene.
public static class M2Setup
{
    // Tools > M2 > Setup Survival (Health + HUD + test Zombie)
    [MenuItem("Tools/M2/Setup Survival (Health + HUD + test Zombie)")]
    public static void SetupSurvival()
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("[M2] No PlayerController in the scene. Open GameScene first.");
            return;
        }

        // give the player hit points
        var health = player.GetComponent<Health>();
        if (health == null)
        {
            health = Undo.AddComponent<Health>(player.gameObject);
            Debug.Log("[M2] Added Health to the player.");
        }

        // make sure the self-building HUD is present
        if (Object.FindAnyObjectByType<PlayerHUD>() == null)
        {
            var hud = new GameObject("PlayerHUD");
            hud.AddComponent<PlayerHUD>();
            Undo.RegisterCreatedObjectUndo(hud, "Create PlayerHUD");
            Debug.Log("[M2] Created PlayerHUD (health bar + objective + death screen build at play time).");
        }

        // a zombie to test the damage loop against
        if (Object.FindAnyObjectByType<Zombie>() == null)
            EntitySpawner.CreateZombie();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[M2] Survival setup done. Press Play: the zombie will chase and bite; watch the health bar. " +
                  "Save the scene (Ctrl+S) to keep the Health component and PlayerHUD.");
    }

    // Tools > M2 > Setup Friend Dialogue (NPC talk + DialogueUI)
    [MenuItem("Tools/M2/Setup Friend Dialogue (NPC talk + DialogueUI)")]
    public static void SetupFriendDialogue()
    {
        var npc = Object.FindAnyObjectByType<Npc>();
        if (npc == null)
        {
            Debug.LogError("[M2] No Npc in the scene. Create one with Tools > Create NPC first.");
            return;
        }

        // make the friend interactable ([E] to talk)
        if (npc.GetComponent<FriendNpc>() == null)
        {
            Undo.AddComponent<FriendNpc>(npc.gameObject);
            Debug.Log("[M2] Added FriendNpc (dialogue) to '" + npc.name + "'.");
        }

        // the interactor only raycasts against the Interactable layer
        int layer = LayerMask.NameToLayer(GameManager.INTERACTABLE_LAYER_NAME);
        if (layer < 0)
            Debug.LogError("[M2] No 'Interactable' layer exists — add it under Project Settings > Tags and Layers, then re-run.");
        else if (npc.gameObject.layer != layer)
        {
            npc.gameObject.layer = layer;
            Debug.Log("[M2] Set the NPC to the Interactable layer so the player can target it.");
        }

        // the self-building dialogue window
        if (Object.FindAnyObjectByType<DialogueUI>() == null)
        {
            var d = new GameObject("DialogueUI");
            d.AddComponent<DialogueUI>();
            Undo.RegisterCreatedObjectUndo(d, "Create DialogueUI");
            Debug.Log("[M2] Created DialogueUI.");
        }

        // GameState stores the narrative flags/counters that dialogue writes; without it,
        // choices wouldn't survive into the ending. Attach it to the GameManager object.
        if (Object.FindAnyObjectByType<GameState>() == null)
        {
            var gm = Object.FindAnyObjectByType<GameManager>();
            var host = gm != null ? gm.gameObject : new GameObject("GameState");
            Undo.AddComponent<GameState>(host);
            Debug.Log("[M2] Added GameState (narrative flags/counters) to '" + host.name + "'.");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[M2] Friend dialogue setup done. Press Play, walk up to the NPC and press [E] to talk. " +
                  "Save the scene (Ctrl+S) to keep it.");
    }

    // Tools > M2 > Match NPC Height To Player (1.8m)
    // Scales the NPC model so it stands the same height as the player (Minecraft 1.8m),
    // so their eye levels line up, and matches its capsule to the player's proportions.
    [MenuItem("Tools/M2/Match NPC Height To Player (1.8m)")]
    public static void MatchNpcHeight()
    {
        var npc = Object.FindAnyObjectByType<Npc>();
        if (npc == null)
        {
            Debug.LogError("[M2] No Npc in the scene.");
            return;
        }

        if (!tryMeasure(npc, out Bounds bounds))
        {
            Debug.LogError("[M2] NPC has no Renderer to measure. Aborting.");
            return;
        }

        // 1) scale the whole NPC so the visible model is exactly PLAYER_HEIGHT tall
        float currentHeight = bounds.size.y;
        if (currentHeight > 0.001f)
        {
            Undo.RecordObject(npc.transform, "Match NPC height");
            npc.transform.localScale *= GameManager.PLAYER_HEIGHT / currentHeight;
            Debug.Log($"[M2] Scaled NPC from {currentHeight:0.00}m to {GameManager.PLAYER_HEIGHT}m tall.");
        }

        // 2) drop a leftover CapsuleCollider — the CharacterController is the only collider we want
        var extra = npc.GetComponent<CapsuleCollider>();
        if (extra != null)
        {
            Undo.DestroyObjectImmediate(extra);
            Debug.Log("[M2] Removed a redundant CapsuleCollider from the NPC.");
        }

        // 3) put the model's feet on the floor (works regardless of where the pivot is)
        snapToFloor(npc);

        // 4) rebuild the CharacterController from the model's real bounds, so the capsule
        //    wraps the model no matter whether the pivot is at the feet or the centre
        var cc = npc.GetComponent<CharacterController>();
        if (cc != null && tryMeasure(npc, out Bounds finalBounds))
        {
            Transform t = npc.transform;
            float sy  = Mathf.Approximately(t.lossyScale.y, 0f) ? 1f : t.lossyScale.y;
            float sxz = Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.z));
            if (sxz <= 0f) sxz = 1f;

            float worldCenterY = finalBounds.min.y + finalBounds.size.y * 0.5f;
            Undo.RecordObject(cc, "Match NPC capsule");
            cc.height = finalBounds.size.y / sy;
            cc.radius = GameManager.PLAYER_RADIUS / sxz;
            cc.center = new Vector3(0f, (worldCenterY - t.position.y) / sy, 0f);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[M2] NPC height matched and placed on the floor. Save the scene (Ctrl+S) to keep it.");
    }

    static bool tryMeasure(Npc npc, out Bounds bounds)
    {
        var renderers = npc.GetComponentsInChildren<Renderer>();
        bounds = default;
        if (renderers.Length == 0) return false;

        bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        return true;
    }

    // Lifts/drops the NPC so the bottom of its model rests exactly on the floor below it.
    // All of the NPC's own colliders are disabled during the raycast so it can't hit itself.
    static void snapToFloor(Npc npc)
    {
        if (!tryMeasure(npc, out Bounds bounds)) return;

        // CharacterController is itself a Collider, so this also covers it — disable every
        // collider so the downward ray can't hit the NPC's own body, then restore them.
        var colliders = npc.GetComponentsInChildren<Collider>();
        var wasEnabled = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++) { wasEnabled[i] = colliders[i].enabled; colliders[i].enabled = false; }

        // start just above the head (avoids hitting a roof) and cast down to the floor
        Vector3 origin = new Vector3(bounds.center.x, bounds.max.y + 0.3f, bounds.center.z);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 200f))
        {
            float lift = hit.point.y - bounds.min.y;   // move feet onto the floor
            Undo.RecordObject(npc.transform, "Snap NPC to floor");
            npc.transform.position += Vector3.up * lift;
        }
        else
        {
            Debug.LogWarning("[M2] Couldn't find a floor under the NPC to snap to — position it manually.");
        }

        for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = wasEnabled[i];
    }
}
