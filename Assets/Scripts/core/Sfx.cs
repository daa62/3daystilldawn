using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Central audio player. Clips load by name from Resources/Audio; missing ones are skipped.
public class Sfx : MonoBehaviour
{
    // one-shots (Resources/Audio/<name>.wav or .ogg)
    public const string UI_CLICK     = "ui_click";
    public const string STEP         = "step";
    public const string PICKUP       = "pickup";
    public const string ITEM_USE     = "item_use";
    public const string PAPER        = "paper";
    public const string DOOR         = "door";
    public const string FLASHLIGHT   = "flashlight";
    public const string ZOMBIE_GROAN = "zombie_groan";
    public const string ZOMBIE_ALERT = "zombie_alert";
    public const string ZOMBIE_BITE  = "zombie_bite";
    public const string PLAYER_HURT  = "player_hurt";
    public const string NIGHT_FALL   = "night_fall";

    // loops
    public const string AMB_STORE       = "amb_store";
    public const string AMB_STORE_NIGHT = "amb_store_night";
    public const string AMB_SAFEROOM    = "amb_saferoom";
    public const string MUSIC_MENU      = "music_menu";
    public const string MUSIC_ENDING    = "music_ending";
    public const string MUSIC_CUTSCENE  = "music_cutscene";

    const float SFX_VOLUME      = 0.6f;   // master scale for one-shot effects (play / playAt)
    const float AMBIENCE_VOLUME = 0.5f;
    const float FADE_PER_SECOND = 0.8f;
    const float PITCH_JITTER    = 0.04f;   // tiny random detune so repeats don't sound stamped

    static Sfx instance;
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    AudioSource oneShot2d;
    AudioSource ambienceA;
    AudioSource ambienceB;
    AudioSource ambienceActive;
    AudioListener fallbackListener;   // for UI-only scenes with no camera

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void bootstrap()
    {
        ensure().onSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static Sfx ensure()
    {
        if (instance == null) {
            var go = new GameObject("Sfx");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Sfx>();
        }
        return instance;
    }

    void Awake()
    {
        oneShot2d = gameObject.AddComponent<AudioSource>();
        oneShot2d.playOnAwake = false;

        ambienceA = makeLoopSource();
        ambienceB = makeLoopSource();
        ambienceActive = ambienceA;

        SceneManager.sceneLoaded += onSceneLoaded;
    }

    void OnDestroy()
    {
        if (instance == this) SceneManager.sceneLoaded -= onSceneLoaded;
    }

    AudioSource makeLoopSource()
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.loop = true;
        src.playOnAwake = false;
        src.volume = 0f;
        return src;
    }

    void Update()
    {
        // crossfade: whichever loop source is active rises, the other one dies out
        float step = FADE_PER_SECOND * Time.unscaledDeltaTime;
        fadeToward(ambienceA, ambienceA == ambienceActive && ambienceA.clip != null ? AMBIENCE_VOLUME : 0f, step);
        fadeToward(ambienceB, ambienceB == ambienceActive && ambienceB.clip != null ? AMBIENCE_VOLUME : 0f, step);
    }

    static void fadeToward(AudioSource src, float target, float step)
    {
        src.volume = Mathf.MoveTowards(src.volume, target, step);
        if (src.volume <= 0f && src.isPlaying && target <= 0f) src.Stop();
    }

    static AudioClip find(string name)
    {
        if (!cache.TryGetValue(name, out AudioClip clip)) {
            clip = Resources.Load<AudioClip>("Audio/" + name);
            cache[name] = clip;   // cache misses too, one Resources hit per name
        }
        return clip;
    }

    // 2d one-shot, for UI and player-local feedback
    public static void play(string name, float volume = 1f)
    {
        AudioClip clip = find(name);
        if (clip == null) return;

        var self = ensure();
        self.oneShot2d.pitch = 1f + Random.Range(-PITCH_JITTER, PITCH_JITTER);
        self.oneShot2d.PlayOneShot(clip, volume * SFX_VOLUME);
    }

    // 3d one-shot at a world position (footsteps, zombies, doors)
    public static void playAt(string name, Vector3 position, float volume = 1f)
    {
        AudioClip clip = find(name);
        if (clip == null) return;

        // roll our own instead of PlayClipAtPoint so pitch can vary
        var go = new GameObject("Sfx3d");
        go.transform.position = position;
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume * SFX_VOLUME;
        src.pitch = 1f + Random.Range(-PITCH_JITTER, PITCH_JITTER);
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.maxDistance = 25f;
        src.Play();
        Destroy(go, clip.length / src.pitch + 0.1f);
    }

    // swap the looping background track; pass null/"" to fade to silence
    public static void ambience(string name)
    {
        var self = ensure();
        AudioClip clip = string.IsNullOrEmpty(name) ? null : find(name);

        if (self.ambienceActive.clip == clip && clip != null) return;

        var next = self.ambienceActive == self.ambienceA ? self.ambienceB : self.ambienceA;
        next.clip = clip;
        if (clip != null && !next.isPlaying) next.Play();
        self.ambienceActive = next;
    }

    // per-scene default track; the store scene is handled by DaylightTimer instead,
    // since day and night there use different loops
    void onSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ensureListener();

        switch (scene.name) {
            case GameManager.SCENE_TITLE:
            case GameManager.SCENE_INTRO:     ambience(MUSIC_MENU);   break;
            case GameManager.SCENE_SAFE_ROOM: ambience(AMB_SAFEROOM); break;
            case GameManager.SCENE_ENDING:    ambience(MUSIC_ENDING); break;
        }
    }

    // menu scenes have no camera, so nothing carries an AudioListener there.
    // add one ourselves and drop it again in scenes that bring their own
    void ensureListener()
    {
        var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

        if (fallbackListener != null) {
            foreach (var l in listeners) {
                if (l != fallbackListener) {
                    Destroy(fallbackListener);
                    fallbackListener = null;
                    break;
                }
            }
            return;
        }

        if (listeners.Length > 0) return;

        Camera cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        fallbackListener = (cam != null ? cam.gameObject : gameObject).AddComponent<AudioListener>();
    }
}
