using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] Transform cameraHolder;

    CharacterController controller;
    Vector3 velocity;
    float xRotation;
    float currentSpeed;
    float jumpTimeoutDelta;
    float fallTimeoutDelta;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        jumpTimeoutDelta = GameManager.PLAYER_JUMP_TIMEOUT;
        fallTimeoutDelta = GameManager.PLAYER_FALL_TIMEOUT;
        lockCursor(true);
    }

    void Update()
    {
        handleLook();
        handleMove();
    }

    void handleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxis("Mouse X") * GameManager.PLAYER_LOOK_SENSITIVITY;
        float mouseY = Input.GetAxis("Mouse Y") * GameManager.PLAYER_LOOK_SENSITIVITY;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -GameManager.VERTICAL_CLAMP, GameManager.VERTICAL_CLAMP);

        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void handleMove()
    {
        bool grounded = controller.isGrounded;
        bool inputEnabled = Cursor.lockState == CursorLockMode.Locked;

        // ground state + jump/fall timeouts (fallTimeout doubles as coyote-time grace)
        if (grounded) {
            fallTimeoutDelta = GameManager.PLAYER_FALL_TIMEOUT;
            if (velocity.y < 0f) velocity.y = -2f;
            if (jumpTimeoutDelta >= 0f) jumpTimeoutDelta -= Time.deltaTime;
        } else {
            jumpTimeoutDelta = GameManager.PLAYER_JUMP_TIMEOUT;
            if (fallTimeoutDelta >= 0f) fallTimeoutDelta -= Time.deltaTime;
        }

        // horizontal move with sprint and smoothed acceleration
        float h = inputEnabled ? Input.GetAxisRaw("Horizontal") : 0f;
        float v = inputEnabled ? Input.GetAxisRaw("Vertical")   : 0f;
        Vector3 dir = (transform.right * h + transform.forward * v).normalized;

        bool sprinting = inputEnabled && Input.GetKey(KeyCode.LeftShift);
        float targetSpeed = sprinting ? GameManager.PLAYER_SPRINT_SPEED : GameManager.PLAYER_MOVE_SPEED;
        if (dir == Vector3.zero) targetSpeed = 0f;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * GameManager.PLAYER_SPEED_CHANGE_RATE);
        controller.Move(dir * currentSpeed * Time.deltaTime);

        // jump: allowed within the coyote window and once the jump cooldown has elapsed
        bool canJump = (grounded || fallTimeoutDelta > 0f) && jumpTimeoutDelta <= 0f;
        if (inputEnabled && Input.GetButtonDown("Jump") && canJump) {
            velocity.y = Mathf.Sqrt(GameManager.PLAYER_JUMP_HEIGHT * -2f * GameManager.PLAYER_GRAVITY);
            jumpTimeoutDelta = GameManager.PLAYER_JUMP_TIMEOUT;
            fallTimeoutDelta = 0f;
        }

        velocity.y += GameManager.PLAYER_GRAVITY * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void lockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }
}
