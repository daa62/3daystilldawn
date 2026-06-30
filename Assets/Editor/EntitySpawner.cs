using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Dev helper: spawns ready-to-use NPC / Zombie capsules in front of the player.
// Tools > Create NPC  and  Tools > Create Zombie
public static class EntitySpawner
{
    [MenuItem("Tools/Create NPC")]
    public static void CreateNpc()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "NPC_Survivor";

        // CharacterController handles collision + gravity, so drop the primitive's capsule collider
        Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
        go.AddComponent<Npc>();            // RequireComponent pulls in the CharacterController

        var cc = go.GetComponent<CharacterController>();
        cc.height = 2f;
        cc.center = new Vector3(0f, 1f, 0f);
        cc.radius = 0.5f;

        Place(go, 4f, false);
        Finish(go, "Create NPC");
    }

    // Builds an idle Animator Controller from any imported "*idle*" clip and puts it on the
    // selected character model. Import a Humanoid idle (e.g. Mixamo, Rig=Humanoid) first.
    // Tools > Setup Idle Animation on Selected
    [MenuItem("Tools/Setup Idle Animation on Selected")]
    public static void SetupIdle()
    {
        var go = Selection.activeGameObject;
        if (go == null) { Debug.LogError("[Idle] Select the character model in the Hierarchy first."); return; }

        AnimationClip idle = null;
        foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            bool pathHasIdle = path.ToLower().Contains("idle");
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview") &&
                    (pathHasIdle || clip.name.ToLower().Contains("idle")))
                { idle = clip; break; }
            }
            if (idle != null) break;
        }
        if (idle == null)
        {
            Debug.LogError("[Idle] No AnimationClip with 'idle' in its name was found. Import a Humanoid " +
                           "idle animation (e.g. from Mixamo, set Rig = Humanoid) first, then run this again.");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");
        var controller = UnityEditor.Animations.AnimatorController
            .CreateAnimatorControllerAtPathWithClip("Assets/Animations/NpcIdle.controller", idle);

        var animator = go.GetComponent<Animator>();
        if (animator == null) animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        // make sure the Animator uses a Humanoid avatar so the clip retargets
        if (animator.avatar == null || !animator.avatar.isHuman)
        {
            // 1) the model's own source FBX
            var source = PrefabUtility.GetCorrespondingObjectFromSource(go);
            string fbxPath = source != null ? AssetDatabase.GetAssetPath(source) : null;
            var found = !string.IsNullOrEmpty(fbxPath) ? FindHumanAvatar(fbxPath) : null;

            // 2) fallback: any humanoid avatar anywhere in the project
            if (found == null)
                foreach (var guid in AssetDatabase.FindAssets("t:Model"))
                {
                    found = FindHumanAvatar(AssetDatabase.GUIDToAssetPath(guid));
                    if (found != null) break;
                }

            if (found != null) animator.avatar = found;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        string avatarNote = (animator.avatar != null && animator.avatar.isHuman)
            ? "Avatar OK (" + animator.avatar.name + ")."
            : "No Humanoid avatar found - select the model root, or set its Avatar manually.";
        Debug.Log($"[Idle] Idle animator set on '{go.name}' using clip '{idle.name}'. {avatarNote}");
    }

    [MenuItem("Tools/Create Zombie")]
    public static void CreateZombie()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Zombie";

        // the CharacterController handles collision, so drop the primitive's capsule collider
        Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
        go.AddComponent<Zombie>();         // RequireComponent pulls in the CharacterController

        var cc = go.GetComponent<CharacterController>();
        cc.height = 2f;
        cc.center = new Vector3(0f, 1f, 0f);
        cc.radius = 0.5f;

        Place(go, 8f, true);               // spawn facing the player so the chase is easy to test
        Finish(go, "Create Zombie");
    }

    static Avatar FindHumanAvatar(string assetPath)
    {
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            if (a is Avatar av && av.isHuman) return av;
        return null;
    }

    static void Place(GameObject go, float ahead, bool facePlayer)
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        Vector3 basePos = player != null ? player.transform.position : Vector3.zero;
        Vector3 forward = player != null ? player.transform.forward  : Vector3.forward;

        Vector3 pos = basePos + forward * ahead;
        pos.y = 1f;                         // capsule pivot so its base rests on the floor
        go.transform.position = pos;

        if (facePlayer && player != null)
        {
            Vector3 look = player.transform.position - pos;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
                go.transform.rotation = Quaternion.LookRotation(look);
        }
    }

    static void Finish(GameObject go, string label)
    {
        Undo.RegisterCreatedObjectUndo(go, label);
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[Spawn] {go.name} created at {go.transform.position}. Move it where you want, then Ctrl+S.");
    }
}
