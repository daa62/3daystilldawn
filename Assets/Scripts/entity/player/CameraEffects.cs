using UnityEngine;

// First-person camera feel: a field-of-view kick while sprinting and a gentle head
// bob that scales with movement speed. Reads state off PlayerController rather than
// touching movement, and lives on the Camera itself (child of the controller's
// cameraHolder) so bobbing the local position stacks under the look pitch without
// fighting it. Purely cosmetic — safe to remove.
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
            // pace and depth grow with speed; sprint pushes both a little further
            float sprintLerp = player.IsSprinting ? GameManager.HEADBOB_SPRINT_MULT : 1f;
            float amplitude  = GameManager.HEADBOB_AMPLITUDE * sprintLerp;

            bobPhase += speed * GameManager.HEADBOB_FREQUENCY * Time.deltaTime;
            offset.y = Mathf.Sin(bobPhase) * amplitude;
            offset.x = Mathf.Cos(bobPhase * 0.5f) * amplitude * 0.5f;   // subtle side sway
        }
        else
        {
            // settle smoothly and reset the cycle so the next step starts from neutral
            bobPhase = 0f;
        }

        Vector3 goal = basePosition + offset;
        transform.localPosition = Vector3.Lerp(transform.localPosition, goal,
                                               GameManager.FOV_LERP_SPEED * Time.deltaTime);
    }
}
