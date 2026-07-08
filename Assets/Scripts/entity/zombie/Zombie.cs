using UnityEngine;

// Chases the player on sight, investigates noises otherwise. No navmesh — walks
// straight at its goal and slides along walls via the CharacterController.
[RequireComponent(typeof(CharacterController))]
public class Zombie : MonoBehaviour
{
    static readonly int SpeedParam  = Animator.StringToHash("Speed");
    static readonly int AttackParam = Animator.StringToHash("Attack");

    CharacterController controller;
    Animator animator;   // on the model child; optional — AI runs fine without one
    Transform target;
    Health targetHealth;
    Vector3 velocity;
    float chaseMemory;
    float attackCooldown;

    // noise investigation
    bool hasNoise;
    Vector3 noisePosition;
    float lingerTimer;

    bool wasChasing;
    float groanTimer;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator   = GetComponentInChildren<Animator>();
        lastPosition = transform.position;
        groanTimer = Random.Range(2f, 12f);   // desync the pack's first groans
    }

    void OnEnable()
    {
        Noise.onNoise += onNoise;
    }

    void OnDisable()
    {
        Noise.onNoise -= onNoise;   // static event — must unsubscribe
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
                if (!wasChasing) Sfx.playAt(Sfx.ZOMBIE_ALERT, transform.position);
                wasChasing = true;
                chase();
                tryAttack();
            }
            else
            {
                wasChasing = false;
                investigate();
            }
        }

        updateGroan();
        applyGravity();
        updateAnimator();
    }

    // animator speed comes from frame displacement — controller.velocity only sees
    // the last Move() call, and the gravity Move zeroes its horizontal part
    Vector3 lastPosition;

    // actual horizontal speed this frame (FootIK reads it to know when to plant feet)
    public float CurrentSpeed { get; private set; }

    void updateAnimator()
    {
        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        lastPosition = transform.position;

        CurrentSpeed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;

        if (animator == null) return;
        animator.SetFloat(SpeedParam, CurrentSpeed, 0.1f, Time.deltaTime);
    }

    // newest audible noise wins; an active chase still takes priority in Update
    void onNoise(Vector3 position, float radius)
    {
        if ((position - transform.position).sqrMagnitude > radius * radius) return;

        hasNoise = true;
        noisePosition = position;
        lingerTimer = GameManager.ZOMBIE_INVESTIGATE_LINGER;
    }

    void investigate()
    {
        if (!hasNoise) return;

        Vector3 flat = noisePosition - transform.position;
        flat.y = 0f;

        if (flat.magnitude > 1f)
        {
            moveToward(noisePosition, GameManager.ZOMBIE_INVESTIGATE_SPEED);
            return;
        }

        lingerTimer -= Time.deltaTime;
        if (lingerTimer <= 0f) hasNoise = false;
    }

    void tryAttack()
    {
        if (targetHealth == null || targetHealth.IsDead || attackCooldown > 0f) return;

        Vector3 flat = target.position - transform.position;
        flat.y = 0f;
        if (flat.magnitude > GameManager.ZOMBIE_ATTACK_RANGE) return;

        targetHealth.damage(GameManager.ZOMBIE_ATTACK_DAMAGE);
        PlayerCondition.wound(GameManager.ZOMBIE_WOUND_MAX_HP);   // bites leave lasting damage
        attackCooldown = GameManager.ZOMBIE_ATTACK_COOLDOWN;
        animator?.SetTrigger(AttackParam);
        Sfx.playAt(Sfx.ZOMBIE_BITE, transform.position);
        Sfx.play(Sfx.PLAYER_HURT, 0.8f);
    }

    // ambient groans on a loose random timer, a bit more often while chasing
    void updateGroan()
    {
        groanTimer -= Time.deltaTime;
        if (groanTimer > 0f) return;

        groanTimer = wasChasing ? Random.Range(2f, 5f) : Random.Range(5f, 12f);
        Sfx.playAt(Sfx.ZOMBIE_GROAN, transform.position, 0.7f);
    }

    bool canSeeTarget()
    {
        Vector3 eye         = transform.position + Vector3.up * GameManager.ZOMBIE_EYE_HEIGHT;
        Vector3 targetPoint = target.position + Vector3.up;          // aim at the player's torso
        Vector3 toTarget    = targetPoint - eye;
        float   distance    = toTarget.magnitude;

        if (distance > GameManager.ZOMBIE_SIGHT_RANGE) return false;

        // walls block awareness (ignore our own body)
        if (Physics.Raycast(eye, toTarget.normalized, out RaycastHit hit, distance))
        {
            bool isTarget = hit.transform == target || hit.transform.IsChildOf(target);
            bool isSelf   = hit.transform == transform || hit.transform.IsChildOf(transform);
            if (!isTarget && !isSelf) return false;
        }

        // close enough to hear from any direction, otherwise needs the view cone
        if (distance <= GameManager.ZOMBIE_HEARING_RANGE) return true;
        return Vector3.Angle(transform.forward, toTarget) <= GameManager.ZOMBIE_FOV * 0.5f;
    }

    void chase()
    {
        moveToward(target.position, GameManager.ZOMBIE_MOVE_SPEED);
    }

    void moveToward(Vector3 point, float speed)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        direction.Normalize();
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(direction),
            Time.deltaTime * GameManager.ZOMBIE_TURN_SPEED);

        controller.Move(direction * speed * Time.deltaTime);
    }

    void applyGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;
        velocity.y += GameManager.PLAYER_GRAVITY * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
