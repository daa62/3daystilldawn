using UnityEngine;

// RPG-Maker style shimmer so readable clues are easy to spot from a distance.
// Attached at runtime by WorldReadable; removed once the clue is read.
public class ClueSparkle : MonoBehaviour
{
    const float SPIN_SPEED = 60f;
    const float BOB_AMPLITUDE = 0.07f;
    const float BOB_SPEED = 2.2f;
    const float PULSE_SPEED = 3.5f;
    // HDRP emissive color is in nits; the scene uses automatic exposure
    const float EMISSIVE_MIN = 30f;
    const float EMISSIVE_MAX = 250f;

    static readonly Color GLOW = new Color(1f, 0.85f, 0.45f);
    static readonly int EMISSIVE_COLOR = Shader.PropertyToID("_EmissiveColor");

    public bool animate = true;   // spin + bob; the emissive pulse always runs

    Renderer target;
    MaterialPropertyBlock block;
    Vector3 basePos;
    float phase;

    void Start()
    {
        target = GetComponentInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        basePos = transform.localPosition;
        phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        if (target == null) return;
        float t = Time.time + phase;

        float pulse = Mathf.Lerp(EMISSIVE_MIN, EMISSIVE_MAX, (Mathf.Sin(t * PULSE_SPEED) + 1f) * 0.5f);
        target.GetPropertyBlock(block);
        block.SetColor(EMISSIVE_COLOR, GLOW * pulse);
        target.SetPropertyBlock(block);

        if (!animate) return;
        transform.Rotate(0f, SPIN_SPEED * Time.deltaTime, 0f, Space.World);
        transform.localPosition = basePos + Vector3.up * (Mathf.Sin(t * BOB_SPEED) * BOB_AMPLITUDE);
    }

    void OnDestroy()
    {
        if (target != null) target.SetPropertyBlock(null);
        if (animate) transform.localPosition = basePos;
    }
}
