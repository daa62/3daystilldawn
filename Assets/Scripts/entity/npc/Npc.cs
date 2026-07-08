using UnityEngine;

// Passive standing NPC — just gravity, no AI.
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

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += GameManager.PLAYER_GRAVITY * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }
}
