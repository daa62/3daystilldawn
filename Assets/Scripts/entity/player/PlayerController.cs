using UnityEngine;

// First-person controller with Minecraft Java movement. The velocity is advanced with
// Minecraft's per-tick math (acceleration, ground/air friction, gravity, the sprint-jump
// impulse) so speeds match and sprint-jumping/bhopping is faster than plain sprinting —
// but the character is *moved* every frame by that velocity, so it renders smoothly at any
// frame rate instead of stepping at 20 Hz. Mouse look is per-frame.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] Transform cameraHolder;

    CharacterController controller;

    // Minecraft motion state, in blocks-per-tick (1 unit = 1 block)
    Vector3 horizontalMotion;   // world XZ, carried between ticks
    float verticalMotion;       // world Y
    Vector3 moveVelocity;       // velocity we actually move by until the next tick
    float tickTimer;
    float xRotation;

    // input sampled every frame, read on the next tick
    float inputStrafe;
    float inputForward;
    bool inputSprint;
    bool inputJump;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Minecraft proportions: 1.8 tall, 0.6 wide, eyes at 1.62 (origin at the feet)
        controller.height = GameManager.PLAYER_HEIGHT;
        controller.radius = GameManager.PLAYER_RADIUS;
        controller.center = new Vector3(0f, GameManager.PLAYER_HEIGHT * 0.5f, 0f);
        if (cameraHolder != null)
        {
            Vector3 p = cameraHolder.localPosition;
            p.y = GameManager.PLAYER_EYE_HEIGHT;
            cameraHolder.localPosition = p;
        }

        lockCursor(true);
    }

    void Update()
    {
        handleLook();
        captureInput();

        // advance the Minecraft velocity in fixed 20 Hz ticks (state only, no movement here)
        tickTimer += Time.deltaTime;
        int guard = 0;
        while (tickTimer >= GameManager.MC_TICK && guard++ < 5)
        {
            tickMotion();
            tickTimer -= GameManager.MC_TICK;
        }

        // move smoothly every frame using the current velocity (blocks/tick -> this frame)
        controller.Move(moveVelocity * (Time.deltaTime / GameManager.MC_TICK));
    }

    void handleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxis("Mouse X") * GameManager.PLAYER_LOOK_SENSITIVITY;
        float mouseY = Input.GetAxis("Mouse Y") * GameManager.PLAYER_LOOK_SENSITIVITY;

        xRotation = Mathf.Clamp(xRotation - mouseY, -GameManager.VERTICAL_CLAMP, GameManager.VERTICAL_CLAMP);
        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void captureInput()
    {
        bool enabled = Cursor.lockState == CursorLockMode.Locked;
        inputStrafe  = enabled ? Input.GetAxisRaw("Horizontal") : 0f;
        inputForward = enabled ? Input.GetAxisRaw("Vertical")   : 0f;
        inputSprint  = enabled && Input.GetKey(KeyCode.LeftShift);
        // held jump auto-fires on landing, which is how Minecraft bunny-hopping works
        if (enabled && Input.GetButton("Jump")) inputJump = true;
    }

    // One Minecraft tick: update the velocity (moveVelocity is what we render-move by).
    void tickMotion()
    {
        bool grounded = controller.isGrounded;
        if (grounded && verticalMotion < 0f)
            verticalMotion = -GameManager.MC_GRAVITY;     // small stick so isGrounded stays stable

        bool sprinting = inputSprint && inputForward > 0f; // Minecraft only sprints going forward

        if (grounded && inputJump)
        {
            verticalMotion = GameManager.MC_JUMP_VELOCITY;
            if (sprinting)
            {
                Vector3 f = transform.forward;
                f.y = 0f;
                horizontalMotion += f.normalized * GameManager.MC_SPRINT_JUMP_BOOST;
            }
        }
        inputJump = false;

        // acceleration in the input direction (diagonals aren't faster)
        Vector3 wish = transform.right * inputStrafe + transform.forward * inputForward;
        wish.y = 0f;
        float wishLen = wish.magnitude;
        if (wishLen > 0f) wish /= Mathf.Max(1f, wishLen);
        horizontalMotion += wish * accelForTick(grounded, sprinting);

        // this pre-friction velocity is what MC moves by; hold it until the next tick
        moveVelocity = new Vector3(horizontalMotion.x, verticalMotion, horizontalMotion.z);

        // leave friction / drag for the next tick
        float hFriction = grounded ? GameManager.MC_GROUND_SLIPPERINESS * GameManager.MC_AIR_DRAG
                                   : GameManager.MC_AIR_DRAG;
        horizontalMotion.x *= hFriction;
        horizontalMotion.z *= hFriction;
        verticalMotion = (verticalMotion - GameManager.MC_GRAVITY) * GameManager.MC_Y_DRAG;
    }

    float accelForTick(bool grounded, bool sprinting)
    {
        if (grounded)
        {
            // normalise by friction so default ground gives the base attribute speed
            float friction = GameManager.MC_GROUND_SLIPPERINESS * GameManager.MC_AIR_DRAG;
            float baseAccel = sprinting ? GameManager.MC_WALK_ACCEL * GameManager.MC_SPRINT_MULTIPLIER
                                        : GameManager.MC_WALK_ACCEL;
            return baseAccel * (0.16277136f / (friction * friction * friction));
        }
        return sprinting ? GameManager.MC_AIR_ACCEL * GameManager.MC_SPRINT_MULTIPLIER
                         : GameManager.MC_AIR_ACCEL;
    }

    public void lockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }
}
