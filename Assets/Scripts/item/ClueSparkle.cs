using UnityEngine;

// Soft emissive shimmer so readable clues are easy to spot in the dark.
// Attached at runtime by WorldReadable; removed once the clue is read.
public class ClueSparkle : MonoBehaviour
{
    const float PULSE_SPEED = 1.8f;
    // HDRP emissive color is in nits; the scene uses automatic exposure
    const float EMISSIVE_MIN = 0f;
    const float EMISSIVE_MAX = 1f;

    static readonly Color GLOW = new Color(1f, 0.85f, 0.45f);
    static readonly int EMISSIVE_COLOR = Shader.PropertyToID("_EmissiveColor");

    Renderer target;
    MaterialPropertyBlock block;
    float phase;

    void Start()
    {
        target = GetComponentInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        if (target == null) return;

        float pulse = Mathf.Lerp(EMISSIVE_MIN, EMISSIVE_MAX,
            (Mathf.Sin((Time.time + phase) * PULSE_SPEED) + 1f) * 0.5f);
        target.GetPropertyBlock(block);
        block.SetColor(EMISSIVE_COLOR, GLOW * pulse);
        target.SetPropertyBlock(block);
    }

    void OnDestroy()
    {
        if (target != null) target.SetPropertyBlock(null);
    }
}
