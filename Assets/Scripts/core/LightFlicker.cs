using UnityEngine;

// Dying-fluorescent flicker: mostly steady with a Perlin shimmer, plus occasional
// stutters to black. Drives the Light and dims the fixture's emissive panel in sync
// (property block, so the shared material is untouched). Put it on a Ceiling Light
// prefab root — or add it to just the instances that should misbehave.
public class LightFlicker : MonoBehaviour
{
    static readonly int EMISSIVE_COLOR = Shader.PropertyToID("_EmissiveColor");

    [Tooltip("Continuous shimmer strength: 0 = rock steady, 1 = swings to black.")]
    [Range(0f, 1f)] [SerializeField] float shimmer = 0.4f;
    [Tooltip("How fast the shimmer wanders.")]
    [SerializeField] float shimmerSpeed = 14f;
    [Tooltip("Average seconds between blink bursts. 0 = never.")]
    [SerializeField] float stutterEvery = 5f;
    [Tooltip("Min/max seconds each dark blink lasts.")]
    [SerializeField] Vector2 stutterLength = new Vector2(0.04f, 0.45f);
    [Tooltip("How many rapid blinks a burst chains together.")]
    [SerializeField] Vector2Int blinksPerBurst = new Vector2Int(1, 4);
    [Tooltip("The glowing panel to dim in sync. Found in children if left empty.")]
    [SerializeField] Renderer emissivePanel;

    Light[] lights;            // every light in the fixture (main spot + bounce spot)
    float[] baseIntensities;
    MaterialPropertyBlock block;
    Color baseEmissive;
    float seed;
    float stutterTimer;   // >0: counting down to the next burst
    float stutterLeft;    // >0: currently dark
    float gapLeft;        // >0: briefly lit between blinks of a burst
    float surgeLeft;      // >0: overshooting after the tube catches again
    int   blinksLeft;     // remaining blinks in the current burst

    void Awake()
    {
        lights          = GetComponentsInChildren<Light>();
        baseIntensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
            baseIntensities[i] = lights[i].intensity;

        seed         = Random.value * 100f;
        stutterTimer = nextStutterDelay();

        if (emissivePanel == null) emissivePanel = findGlowingPanel();
        if (emissivePanel != null) {
            block        = new MaterialPropertyBlock();
            baseEmissive = emissivePanel.sharedMaterial.GetColor(EMISSIVE_COLOR);
        }
    }

    // the panel is whichever child actually glows — the housing's emissive is black,
    // so "first renderer" would silently animate the wrong mesh
    Renderer findGlowingPanel()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) {
            if (!r.sharedMaterial || !r.sharedMaterial.HasColor(EMISSIVE_COLOR)) continue;
            if (r.sharedMaterial.GetColor(EMISSIVE_COLOR).maxColorComponent > 0f) return r;
        }
        return null;
    }

    void Update()
    {
        float level = 1f - shimmer * Mathf.PerlinNoise(Time.time * shimmerSpeed, seed);

        if (stutterLeft > 0f) {
            // dark blink; when it ends, either gap to the next blink or surge back on
            stutterLeft -= Time.deltaTime;
            level = 0.02f;   // not quite zero — a dying tube still ghosts
            if (stutterLeft <= 0f) {
                if (--blinksLeft > 0) gapLeft = Random.Range(0.04f, 0.12f);
                else {
                    surgeLeft    = Random.Range(0.1f, 0.25f);
                    stutterTimer = nextStutterDelay();
                }
            }
        }
        else if (gapLeft > 0f) {
            // brief lit moment inside a burst, then dark again
            gapLeft -= Time.deltaTime;
            if (gapLeft <= 0f) stutterLeft = Random.Range(stutterLength.x, stutterLength.y);
        }
        else if (surgeLeft > 0f) {
            // the tube catches: a hot overshoot before settling
            surgeLeft -= Time.deltaTime;
            level = 1.35f;
        }
        else if (stutterEvery > 0f) {
            stutterTimer -= Time.deltaTime;
            if (stutterTimer <= 0f) {
                blinksLeft  = Random.Range(blinksPerBurst.x, blinksPerBurst.y + 1);
                stutterLeft = Random.Range(stutterLength.x, stutterLength.y);
            }
        }

        apply(level);
    }

    float nextStutterDelay() => stutterEvery * Random.Range(0.3f, 1.7f);

    void apply(float level)
    {
        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null) lights[i].intensity = baseIntensities[i] * level;

        if (emissivePanel != null) {
            emissivePanel.GetPropertyBlock(block);
            block.SetColor(EMISSIVE_COLOR, baseEmissive * level);
            emissivePanel.SetPropertyBlock(block);
        }
    }

    void OnDisable()
    {
        // hand the fixture back exactly as we found it
        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null) lights[i].intensity = baseIntensities[i];
        emissivePanel?.SetPropertyBlock(null);
    }
}
