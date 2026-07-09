using UnityEngine;

// Dims the assigned daylight lights as the DaylightTimer runs down, dark at night.
// Authored intensities are cached as the full-morning values and scaled from there.
public class DaylightVisuals : MonoBehaviour
{
    [Tooltip("Lights that represent daylight. Their current intensity = the full-morning look.")]
    [SerializeField] Light[] daylightLights;

    [Tooltip("Brightness factor once night has fallen (0 = fully dark). The day dims linearly toward this.")]
    [SerializeField] float nightFactor = 0f;

    float[] authoredIntensity;

    void Awake()
    {
        if (daylightLights == null) return;

        authoredIntensity = new float[daylightLights.Length];
        for (int i = 0; i < daylightLights.Length; i++)
            if (daylightLights[i] != null)
                authoredIntensity[i] = daylightLights[i].intensity;
    }

    void Update()
    {
        DaylightTimer timer = DaylightTimer.Instance;
        if (timer == null || daylightLights == null) return;

        // linear dim across the whole day: full brightness in the morning, reaching
        // nightFactor exactly as the timer hits 0 (NightFell then holds it there)
        float progress = 1f - Mathf.Clamp01(timer.RemainingSeconds / GameManager.DAYLIGHT_SECONDS);
        float factor = timer.NightFell ? nightFactor : Mathf.Lerp(1f, nightFactor, progress);

        for (int i = 0; i < daylightLights.Length; i++)
            if (daylightLights[i] != null)
                daylightLights[i].intensity = authoredIntensity[i] * factor;
    }
}
