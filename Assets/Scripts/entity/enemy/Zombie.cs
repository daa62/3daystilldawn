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
    Health targetHealth;
    Vector3 velocity;
    float chaseMemory;
    float attackCooldown;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            target = player.transform;
            targetHealth = player.GetComponent<Health>();
        }
    }

    void Update()
    {
        if (attackCooldown > 0f) attackCooldown -= Time.deltaTime;

        if (target != null)
        {
            if (canSeeTarget())
                chaseMemory = GameManager.ZOMBIE_SIGHT_MEMORY;
            else if (chaseMemory > 0f)
                chaseMemory -= Time.deltaTime;

            if (chaseMemory > 0f)
            {
                chase();
                tryAttack();
            }
        }

        applyGravity();
    }

    // Bite the player whenever it stays within reach, throttled by a cooldown.
    void tryAttack()
    {
        if (targetHealth == null || targetHealth.IsDead || attackCooldown > 0f) return;

        Vector3 flat = target.position - transform.position;
        flat.y = 0f;
        if (flat.magnitude > GameManager.ZOMBIE_ATTACK_RANGE) return;

        targetHealth.damage(GameManager.ZOMBIE_ATTACK_DAMAGE);
        attackCooldown = GameManager.ZOMBIE_ATTACK_COOLDOWN;
    }

    bool canSeeTarget()
    {
        Vector3 eye         = transform.position + Vector3.up * GameManager.ZOMBIE_EYE_HEIGHT;
        Vector3 targetPoint = target.position + Vector3.up;          // aim at the player's torso
        Vector3 toTarget    = targetPoint - eye;
        float   distance    = toTarget.magnitude;

        if (distance > GameManager.ZOMBIE_SIGHT_RANGE) return false;

        // anything solid between us and the player blocks awareness (ignore our own body)
        if (Physics.Raycast(eye, toTarget.normalized, out RaycastHit hit, distance))
        {
            bool isTarget = hit.transform == target || hit.transform.IsChildOf(target);
            bool isSelf   = hit.transform == transform || hit.transform.IsChildOf(transform);
            if (!isTarget && !isSelf) return false;
        }

        // close by: hears/smells the player from any direction, so the view cone doesn't matter
        if (distance <= GameManager.ZOMBIE_HEARING_RANGE) return true;

        // farther away: must be inside the forward view cone
        return Vector3.Angle(transform.forward, toTarget) <= GameManager.ZOMBIE_FOV * 0.5f;
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
