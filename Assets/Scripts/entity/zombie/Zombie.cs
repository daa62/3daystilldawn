using UnityEngine;

// Chases the player on sight, investigates noises otherwise. No navmesh — walks
// straight at its goal and slides along walls via the CharacterController.
[RequireComponent(typeof(CharacterController))]
public class Zombie : MonoBehaviour
{
    static readonly int SpeedParam  = Animator.StringToHash("Speed");
    static readonly int AttackParam = Animator.StringToHash("Attack");

    [Tooltip("Movement speed (units/second) — used for chasing and investigating alike.")]
    [SerializeField] float moveSpeed = GameManager.ZOMBIE_MOVE_SPEED;

    [Tooltip("Delay from the attack starting to the damage landing (match the bite frame).")]
    [SerializeField] float attackWindup = 0.4f;

    [Header("Obstacle avoidance")]
    [Tooltip("How far ahead the zombie probes for walls to steer around.")]
    [SerializeField] float avoidProbeDistance = 1.6f;
    [Tooltip("Layers treated as walls/obstacles. Set to your map geometry + props.")]
    [SerializeField] LayerMask obstacleMask = ~0;

    [Header("Idle wandering")]
    [Tooltip("How far the zombie strays from its post while idle. 0 = stays put (guard).")]
    [SerializeField] float wanderRadius = 5f;
    [Tooltip("Random pause range (seconds) standing still between strolls.")]
    [SerializeField] float wanderPauseMin = 3f;
    [SerializeField] float wanderPauseMax = 9f;

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

    // idle wandering
    Vector3 homePosition;      // the post the zombie loosely patrols around
    Vector3 wanderTarget;
    bool hasWanderTarget;
    float wanderPauseTimer;
    float wanderGiveUpTimer;

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

        homePosition = transform.position;
        wanderPauseTimer = Random.Range(0f, wanderPauseMax);   // desync the pack's first strolls
    }

    void Update()
    {
        if (attackCooldown > 0f) attackCooldown -= Time.deltaTime;
        tickPendingHit();

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
                if (hasNoise) investigate();
                else wander();
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
            moveToward(noisePosition, moveSpeed);
            return;
        }

        lingerTimer -= Time.deltaTime;
        if (lingerTimer <= 0f) hasNoise = false;
    }

    // Idle behaviour: mostly stand, but now and then shuffle to a random spot near the
    // zombie's post, then pause again. Keeps the store feeling alive without the zombie
    // straying off its guarded area. Straight-line movement — it slides off walls, and
    // a give-up timer stops it grinding on an unreachable point.
    void wander()
    {
        if (wanderRadius <= 0f) return;   // this zombie is a stationary guard

        if (wanderPauseTimer > 0f)
        {
            wanderPauseTimer -= Time.deltaTime;
            return;   // standing idle
        }

        if (!hasWanderTarget)
        {
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            wanderTarget = homePosition + new Vector3(offset.x, 0f, offset.y);
            hasWanderTarget = true;
            wanderGiveUpTimer = wanderRadius / Mathf.Max(0.1f, moveSpeed) + 2f;
        }

        Vector3 flat = wanderTarget - transform.position;
        flat.y = 0f;
        wanderGiveUpTimer -= Time.deltaTime;

        if (flat.magnitude <= 1f || wanderGiveUpTimer <= 0f)
        {
            hasWanderTarget = false;
            wanderPauseTimer = Random.Range(wanderPauseMin, wanderPauseMax);
            return;
        }

        moveToward(wanderTarget, moveSpeed);
    }

    float pendingHitTimer = -1f;   // <0 = no swing in progress

    // Start a swing: play the animation/sound now, but hold the damage until the bite
    // frame (attackWindup). The cooldown starts now so it can't re-trigger mid-swing.
    void tryAttack()
    {
        if (targetHealth == null || targetHealth.IsDead || attackCooldown > 0f) return;

        Vector3 flat = target.position - transform.position;
        flat.y = 0f;
        if (flat.magnitude > GameManager.ZOMBIE_ATTACK_RANGE) return;

        attackCooldown = GameManager.ZOMBIE_ATTACK_COOLDOWN;
        pendingHitTimer = attackWindup;
        animator?.SetTrigger(AttackParam);
        Sfx.playAt(Sfx.ZOMBIE_BITE, transform.position);
    }

    // The bite lands here, one windup later — and only if the player is still in reach,
    // so backing off during the swing dodges it.
    void tickPendingHit()
    {
        if (pendingHitTimer < 0f) return;

        pendingHitTimer -= Time.deltaTime;
        if (pendingHitTimer > 0f) return;
        pendingHitTimer = -1f;

        if (targetHealth == null || targetHealth.IsDead) return;

        Vector3 flat = target.position - transform.position;
        flat.y = 0f;
        if (flat.magnitude > GameManager.ZOMBIE_ATTACK_RANGE) return;

        targetHealth.damage(GameManager.ZOMBIE_ATTACK_DAMAGE);
        PlayerCondition.wound(GameManager.ZOMBIE_WOUND_MAX_HP);   // bites leave lasting damage
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
        moveToward(target.position, moveSpeed);
    }

    void moveToward(Vector3 point, float speed)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        direction.Normalize();
        direction = avoidObstacles(direction);   // steer around walls instead of grinding

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(direction),
            Time.deltaTime * GameManager.ZOMBIE_TURN_SPEED);

        controller.Move(direction * speed * Time.deltaTime);
    }

    static readonly float[] AvoidAngles = { 25f, 50f, 75f };

    // Whisker avoidance: if a wall is dead ahead, deflect toward the nearest clear
    // heading so the zombie rounds obstacles rather than shoving into them. The target
    // itself is never treated as an obstacle (so chasing still closes the gap).
    Vector3 avoidObstacles(Vector3 desired)
    {
        if (!blocked(desired)) return desired;

        foreach (float angle in AvoidAngles)
            foreach (int side in Sides)
            {
                Vector3 candidate = Quaternion.Euler(0f, angle * side, 0f) * desired;
                if (!blocked(candidate)) return candidate;
            }
        return desired;   // boxed in on all sides — nothing to do but push (rare)
    }

    static readonly int[] Sides = { 1, -1 };

    bool blocked(Vector3 dir)
    {
        // start the probe just outside our own capsule so we don't self-hit
        Vector3 origin = transform.position + Vector3.up * (controller.height * 0.5f)
                         + dir * (controller.radius + 0.05f);
        if (!Physics.Raycast(origin, dir, out RaycastHit hit, avoidProbeDistance,
                             obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        // don't avoid the player (or its children) — that's who we're trying to reach
        if (target != null && (hit.transform == target || hit.transform.IsChildOf(target)))
            return false;

        return true;
    }

    void applyGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;
        velocity.y += GameManager.PLAYER_GRAVITY * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
