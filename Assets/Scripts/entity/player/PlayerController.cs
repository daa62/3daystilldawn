using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] Transform cameraHolder;

    CharacterController controller;
    Vector3 velocity;
    float xRotation;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
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
        if (grounded && velocity.y < 0f)
            velocity.y = -2f;

        bool inputEnabled = Cursor.lockState == CursorLockMode.Locked;

        float h = inputEnabled ? Input.GetAxis("Horizontal") : 0f;
        float v = inputEnabled ? Input.GetAxis("Vertical")   : 0f;
        Vector3 move = transform.right * h + transform.forward * v;
        controller.Move(move * GameManager.PLAYER_MOVE_SPEED * Time.deltaTime);

        if (inputEnabled && Input.GetButtonDown("Jump") && grounded)
            velocity.y = Mathf.Sqrt(GameManager.PLAYER_JUMP_HEIGHT * -2f * GameManager.PLAYER_GRAVITY);

        velocity.y += GameManager.PLAYER_GRAVITY * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void lockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }
}
