using UnityEngine;

// Basic zombie enemy. Stands idle until the player enters its view cone (range +
// angle + unobstructed line of sight), then chases. No navmesh: it walks straight
// toward the player and slides along walls via the CharacterController. Keeps
// chasing for a short memory window after losing sight.
[RequireComponent(typeof(CharacterController))]
public class Zombie : MonoBehaviour
{
    CharacterController controller;
    Transform target;
    Vector3 velocity;
    float chaseMemory;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null) target = player.transform;
    }

    void Update()
    {
        if (target != null)
        {
            if (canSeeTarget())
                chaseMemory = GameManager.ZOMBIE_SIGHT_MEMORY;
            else if (chaseMemory > 0f)
                chaseMemory -= Time.deltaTime;

            if (chaseMemory > 0f)
                chase();
        }

        applyGravity();
    }

    bool canSeeTarget()
    {
        Vector3 eye         = transform.position + Vector3.up * GameManager.ZOMBIE_EYE_HEIGHT;
        Vector3 targetPoint = target.position + Vector3.up;          // aim at the player's torso
        Vector3 toTarget    = targetPoint - eye;
        float   distance    = toTarget.magnitude;

        if (distance > GameManager.ZOMBIE_SIGHT_RANGE) return false;
        if (Vector3.Angle(transform.forward, toTarget) > GameManager.ZOMBIE_FOV * 0.5f) return false;

        // anything solid between us and the player blocks sight
        if (Physics.Raycast(eye, toTarget.normalized, out RaycastHit hit, distance))
        {
            if (hit.transform != target && !hit.transform.IsChildOf(target)) return false;
        }
        return true;
    }

    void chase()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        direction.Normalize();
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(direction),
            Time.deltaTime * GameManager.ZOMBIE_TURN_SPEED);

        controller.Move(direction * GameManager.ZOMBIE_MOVE_SPEED * Time.deltaTime);
    }

    void applyGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;
        velocity.y += GameManager.PLAYER_GRAVITY * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
