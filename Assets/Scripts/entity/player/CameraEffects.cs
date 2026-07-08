using UnityEngine;

// FOV kick while sprinting + head bob. Lives on the Camera itself so the bob
// stacks under the look pitch without fighting it. Cosmetic only.
[RequireComponent(typeof(Camera))]
public class CameraEffects : MonoBehaviour
{
    Camera cam;
    PlayerController player;

    float baseFov;
    Vector3 basePosition;
    float bobPhase;

    void Awake()
    {
        cam    = GetComponent<Camera>();
        player = GetComponentInParent<PlayerController>();

        baseFov      = cam.fieldOfView;
        basePosition = transform.localPosition;
    }

    void Update()
    {
        if (player == null) return;

        updateFov();
        updateHeadBob();
    }

    void updateFov()
    {
        float target = player.IsSprinting ? baseFov + GameManager.SPRINT_FOV_KICK : baseFov;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, target,
                                     GameManager.FOV_LERP_SPEED * Time.deltaTime);
    }

    void updateHeadBob()
    {
        float speed = player.HorizontalSpeed;
        bool moving = player.IsGrounded && speed > GameManager.HEADBOB_MIN_SPEED;

        Vector3 offset = Vector3.zero;
        if (moving)
        {
            float sprintLerp = player.IsSprinting ? GameManager.HEADBOB_SPRINT_MULT : 1f;
            float amplitude  = GameManager.HEADBOB_AMPLITUDE * sprintLerp;

            bobPhase += speed * GameManager.HEADBOB_FREQUENCY * Time.deltaTime;
            offset.y = Mathf.Sin(bobPhase) * amplitude;
            offset.x = Mathf.Cos(bobPhase * 0.5f) * amplitude * 0.5f;   // subtle side sway
        }
        else
        {
            bobPhase = 0f;   // next step starts from neutral
        }

        Vector3 goal = basePosition + offset;
        transform.localPosition = Vector3.Lerp(transform.localPosition, goal,
                                               GameManager.FOV_LERP_SPEED * Time.deltaTime);
    }
}
