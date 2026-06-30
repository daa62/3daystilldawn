using UnityEngine;

// A passive standing NPC (mannequin). Has simple gravity so it rests on the ground,
// but no movement AI yet. To give it dialogue later, implement IInteractable here
// (see WorldItem for the pattern) and put its collider on the Interactable layer.
[RequireComponent(typeof(CharacterController))]
public class Npc : MonoBehaviour
{
    [SerializeField] string npcName = "Survivor";

    CharacterController controller;
    float verticalVelocity;

    public string getName() => npcName;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (controller == null) return;

        // basic gravity so the NPC stays grounded instead of floating
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += GameManager.PLAYER_GRAVITY * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }
}
