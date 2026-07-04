using UnityEngine;

// Two-light flashlight rig. A narrow bright "throw" light reaches distant surfaces
// while a dim wide "fill" softly lights the near foreground — together they avoid
// the blown-out-close / black-far look of a single spotlight. Holds references to
// two spot lights set up under the camera; their intensity, colour, shadows and
// HDRP volumetric settings are authored directly on the lights (this script does
// NOT touch intensity, so your lumen values stick). Drives only the on/off toggle
// and a subtle handheld sway. Sits on the camera, matching CameraEffects.
public class Flashlight : MonoBehaviour
{
    [Header("Lights (spot lights under the camera)")]
    [SerializeField] Light throwLight;   // narrow, long range — sees far
    [SerializeField] Light fillLight;    // wide, short range — soft near light

    [Header("Handheld sway (0 amounts = off)")]
    [SerializeField] float swayPosAmount = 0.015f;  // metres of positional bob while moving
    [SerializeField] float swayRotAmount = 0.1f;    // degrees of rotational bob while moving
    [SerializeField] float swayBobSpeed  = 0.1f;      // bob pace scales with player speed * this
    [SerializeField] float lookLag       = 3f;    // degrees the beam trails behind a turn
    [SerializeField] float swaySmooth    = 8f;      // how quickly the sway eases

    [Header("Control")]
    [SerializeField] KeyCode toggleKey = KeyCode.F;
    [SerializeField] bool startOn = true;

    bool on;

    // sway state
    PlayerController player;
    Vector3 throwBasePos, fillBasePos;
    Quaternion throwBaseRot, fillBaseRot;
    float bobPhase;
    Vector2 smoothedMouse;   // raw mouse delta is jittery; damp it before use
    Vector3 posOffset;
    Vector3 rotOffset;       // euler degrees

    const float BOB_BASE_FREQUENCY = 0.1f;   // radians/sec while moving (before the speed nudge)
    const float BOB_SPEED_CAP      = 6f;   // ignore speed above this for bob pace

    void Awake()
    {
        SetOn(startOn);

        player = GetComponentInParent<PlayerController>();
        if (throwLight != null) {
            throwBasePos = throwLight.transform.localPosition;
            throwBaseRot = throwLight.transform.localRotation;
        }
        if (fillLight != null) {
            fillBasePos = fillLight.transform.localPosition;
            fillBaseRot = fillLight.transform.localRotation;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) SetOn(!on);
        updateSway();
    }

    // Turn the whole rig on or off (both lights together). Public so future systems
    // — a dead battery, a scripted blackout — can drive it.
    public void SetOn(bool value)
    {
        on = value;
        if (throwLight != null) throwLight.enabled = value;
        if (fillLight  != null) fillLight.enabled  = value;
    }

    // A handheld feel: the beam trails slightly behind a turn (look lag) and bobs a
    // little while walking, easing back to centre when still.
    void updateSway()
    {
        // frame-rate-independent easing (a plain factor*dt overshoots at low fps and jitters)
        float ease = 1f - Mathf.Exp(-swaySmooth * Time.deltaTime);

        // damp the noisy per-frame mouse delta first, then use it for look lag
        smoothedMouse = Vector2.Lerp(smoothedMouse,
            new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")), ease);
        Vector3 targetRot = new Vector3(smoothedMouse.y * lookLag, -smoothedMouse.x * lookLag, 0f);
        Vector3 targetPos = Vector3.zero;

        float speed = player != null ? player.HorizontalSpeed : 0f;
        bool moving = player == null || (player.IsGrounded && speed > GameManager.HEADBOB_MIN_SPEED);
        if (moving)
        {
            // bob pace is a gentle base frequency nudged by (capped) speed — not scaled
            // straight off raw speed, which made a sprint bob frantically
            float cappedSpeed = Mathf.Min(speed, BOB_SPEED_CAP);
            bobPhase += (BOB_BASE_FREQUENCY + cappedSpeed * swayBobSpeed) * Time.deltaTime;
            targetPos += new Vector3(Mathf.Cos(bobPhase), Mathf.Sin(bobPhase * 2f), 0f) * swayPosAmount;
            targetRot += new Vector3(Mathf.Sin(bobPhase * 2f), Mathf.Cos(bobPhase), 0f) * swayRotAmount;
        }

        posOffset = Vector3.Lerp(posOffset, targetPos, ease);
        rotOffset = Vector3.Lerp(rotOffset, targetRot, ease);

        applySway(throwLight, throwBasePos, throwBaseRot);
        applySway(fillLight,  fillBasePos,  fillBaseRot);
    }

    void applySway(Light light, Vector3 basePos, Quaternion baseRot)
    {
        if (light == null) return;
        light.transform.localPosition = basePos + posOffset;
        light.transform.localRotation = baseRot * Quaternion.Euler(rotOffset);
    }
}
