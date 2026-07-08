using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] Transform cameraHolder;

    CharacterController controller;
    Stamina stamina;   // optional — movement works without it
    Health health;

    Vector3 horizontalMotion;   // world XZ, carried between ticks
    float verticalMotion;       // world Y
    Vector3 moveVelocity;       // velocity we actually move by until the next tick
    float tickTimer;
    float xRotation;

    // input sampled every frame, read on the next tick
    float inputStrafe;
    float inputForward;
    bool inputSprint;
    bool inputCrouch;
    bool inputJump;

    // read-only feel state for CameraEffects (FOV kick / head bob) and PlayerNoise
    public bool IsSprinting { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool IsGrounded => controller != null && controller.isGrounded;
    // current horizontal speed in units/second (moveVelocity is blocks-per-tick)
    public float HorizontalSpeed =>
        new Vector2(moveVelocity.x, moveVelocity.z).magnitude / GameManager.MC_TICK;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        stamina    = GetComponent<Stamina>();
        health     = GetComponent<Health>();

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

    void Start()
    {
        // bind the persistent condition (wounds, hunger) to this scene's components
        applyCondition();
        PlayerCondition.onChanged += applyCondition;
    }

    void OnDestroy()
    {
        PlayerCondition.onChanged -= applyCondition;   // static event — must let go
    }

    void applyCondition()
    {
        if (health  != null) health.setMax(PlayerCondition.MaxHealth);
        if (stamina != null) stamina.setMax(PlayerCondition.MaxStamina);
    }

    void Update()
    {
        handleLook();
        captureInput();

        tickTimer += Time.deltaTime;
        int guard = 0;
        while (tickTimer >= GameManager.MC_TICK && guard++ < 5)
        {
            tickMotion();
            tickTimer -= GameManager.MC_TICK;
        }

        // move smoothly every frame using the current velocity (blocks/tick -> this frame)
        controller.Move(moveVelocity * (Time.deltaTime / GameManager.MC_TICK));

        updateCrouchCamera();

        // slow natural recovery — only ever up to the current (possibly wounded) max
        health?.heal(GameManager.PLAYER_HEALTH_REGEN * Time.deltaTime);
    }

    // ease the eye down while crouched; only the holder's height moves,
    // pitch and head bob live elsewhere
    void updateCrouchCamera()
    {
        if (cameraHolder == null) return;

        float targetY = GameManager.PLAYER_EYE_HEIGHT
                        - (IsCrouching ? GameManager.CROUCH_CAMERA_DROP : 0f);
        Vector3 p = cameraHolder.localPosition;
        p.y = Mathf.Lerp(p.y, targetY, GameManager.CROUCH_LERP_SPEED * Time.deltaTime);
        cameraHolder.localPosition = p;
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
        inputCrouch  = enabled && Input.GetKey(KeyCode.LeftControl);
        if (enabled && Input.GetButton("Jump")) inputJump = true;
    }

    void tickMotion()
    {
        bool grounded = controller.isGrounded;
        if (grounded && verticalMotion < 0f)
            verticalMotion = -GameManager.MC_GRAVITY;     // small stick so isGrounded stays stable

        // crouching excludes sprinting; Minecraft only sprints going forward
        bool crouching = inputCrouch;
        bool sprinting = !crouching && inputSprint && inputForward > 0f
                         && (stamina == null || stamina.CanSprint);
        IsSprinting = sprinting;
        IsCrouching = crouching;
        stamina?.setSprinting(sprinting);

        if (grounded && inputJump)
        {
            bool canJump = stamina == null || stamina.spendJump();
            if (canJump)
            {
                verticalMotion = GameManager.MC_JUMP_VELOCITY;
                if (sprinting)
                {
                    Vector3 f = transform.forward;
                    f.y = 0f;
                    horizontalMotion += f.normalized * GameManager.MC_SPRINT_JUMP_BOOST;
                }
            }
        }
        inputJump = false;

        // acceleration in the input direction (diagonals aren't faster)
        Vector3 wish = transform.right * inputStrafe + transform.forward * inputForward;
        wish.y = 0f;
        float wishLen = wish.magnitude;
        if (wishLen > 0f) wish /= Mathf.Max(1f, wishLen);
        float accel = accelForTick(grounded, sprinting);
        if (crouching) accel *= GameManager.MC_CROUCH_MULTIPLIER;
        horizontalMotion += wish * accel;

        // mc moves by the pre-friction velocity; friction lands on the next tick
        moveVelocity = new Vector3(horizontalMotion.x, verticalMotion, horizontalMotion.z);

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
