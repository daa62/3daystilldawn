using UnityEngine;
using UnityEngine.AI;

// Chases the player on sight, investigates noises otherwise. NavMesh-driven: the agent
// paths around shelves and aisles instead of the old straight-line whisker steering.
// The CharacterController stays purely as the physical collider (the player bumps into
// zombies); this script never moves it — the agent moves the transform.
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NavMeshAgent))]
public class Zombie : MonoBehaviour
{
    static readonly int SpeedParam  = Animator.StringToHash("Speed");
    static readonly int AttackParam = Animator.StringToHash("Attack");

    // the script owns stoppingDistance per mode: close enough to bite while chasing,
    // tight for wander/investigate — one inspector value for both broke arrivals
    const float CHASE_STOP_DISTANCE  = 1.2f;
    const float TRAVEL_STOP_DISTANCE = 0.2f;

    [Tooltip("Movement speed (units/second) — used for chasing and wandering alike.")]
    [SerializeField] float moveSpeed = GameManager.ZOMBIE_MOVE_SPEED;

    [Tooltip("Delay from the attack starting to the damage landing (match the bite frame).")]
    [SerializeField] float attackWindup = 0.4f;

    [Header("Idle wandering")]
    [Tooltip("How far the zombie strays from its post while idle. 0 = stays put (guard).")]
    [SerializeField] float wanderRadius = 5f;
    [Tooltip("Random pause range (seconds) standing still between strolls.")]
    [SerializeField] float wanderPauseMin = 3f;
    [SerializeField] float wanderPauseMax = 9f;

    [Header("Debug")]
    [Tooltip("Log AI decisions to the console and draw the current destination in the Scene view.")]
    [SerializeField] bool debugAi = false;

    NavMeshAgent agent;
    Animator animator;   // on the model child; optional — AI runs fine without one
    Transform target;
    Health targetHealth;
    float chaseMemory;
    float attackCooldown;

    // noise investigation
    bool hasNoise;
    Vector3 noisePosition;
    float lingerTimer;
    Vector3 lastKnownTargetPos;   // where the search continues after losing a chase

    bool wasChasing;
    float groanTimer;

    // idle wandering
    Vector3 homePosition;      // the post the zombie loosely patrols around
    bool hasWanderTarget;
    float wanderPauseTimer;

    void Awake()
    {
        agent    = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        lastPosition = transform.position;
        groanTimer = Random.Range(2f, 12f);   // desync the pack's first groans

        agent.speed        = moveSpeed;
        agent.angularSpeed = GameManager.ZOMBIE_TURN_SPEED * 45f;   // slerp factor -> deg/sec
        agent.acceleration = 8f;
        agent.autoBraking  = false;   // shamble through waypoints instead of easing at each
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

        // spawned zombies (night pack) may land slightly off the mesh — snap on
        if (!agent.isOnNavMesh &&
            NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            agent.Warp(hit.position);

        homePosition = transform.position;
        wanderPauseTimer = Random.Range(0f, wanderPauseMax);   // desync the pack's first strolls
    }

    void Update()
    {
        if (attackCooldown > 0f) attackCooldown -= Time.deltaTime;
        tickPendingHit();

        if (target != null && agent.isOnNavMesh)
        {
            if (canSeeTarget())
                chaseMemory = GameManager.ZOMBIE_SIGHT_MEMORY;
            else if (chaseMemory > 0f)
                chaseMemory -= Time.deltaTime;

            if (chaseMemory > 0f)
            {
                if (!wasChasing) { Sfx.playAt(Sfx.ZOMBIE_ALERT, transform.position); log("spotted the player — chasing"); }
                wasChasing = true;
                hasNoise = false;                       // the live chase supersedes older noises
                lastKnownTargetPos = target.position;   // remember where to search if we lose them
                chase();
                tryAttack();
            }
            else
            {
                if (wasChasing)
                {
                    // lost them: press on to where they were last seen instead of
                    // snapping around toward some stale footstep heard mid-chase
                    log("lost the player — checking where they were last seen");
                    hasNoise = true;
                    noisePosition = lastKnownTargetPos;
                    lingerTimer = GameManager.ZOMBIE_INVESTIGATE_LINGER;
                }
                wasChasing = false;
                if (hasNoise) investigate();
                else wander();
            }
        }

        updateGroan();
        updateAnimator();
    }

    // animator speed comes from frame displacement — one signal for any mover
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
        hasWanderTarget = false;   // drop the idle stroll — something made a sound
        log($"heard a noise at {position}");
    }

    void investigate()
    {
        Vector3 flat = noisePosition - transform.position;
        flat.y = 0f;

        if (flat.magnitude > 1f)
        {
            agent.speed = GameManager.ZOMBIE_INVESTIGATE_SPEED;
            agent.stoppingDistance = TRAVEL_STOP_DISTANCE;
            agent.SetDestination(noisePosition);
            return;
        }

        // at the noise spot: linger, then drift back to idling
        agent.ResetPath();
        lingerTimer -= Time.deltaTime;
        if (lingerTimer <= 0f) { hasNoise = false; log("done investigating — back to roaming"); }
    }

    // Idle behaviour: mostly stand, but now and then shuffle to a random reachable
    // spot near the zombie's post, then pause again. Keeps the store feeling alive
    // without the zombie straying off its guarded area — a chase or noise can pull
    // it away, and the next stroll naturally draws it back home.
    void wander()
    {
        if (wanderRadius <= 0f || agent.pathPending) return;

        if (hasWanderTarget)
        {
            // arrival must respect the agent's stopping distance — a fixed threshold
            // tighter than it left zombies frozen mid-"stroll" forever
            if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.4f)
                return;   // still strolling

            hasWanderTarget = false;
            agent.ResetPath();
            wanderPauseTimer = Random.Range(wanderPauseMin, wanderPauseMax);
            log($"stroll done — standing for {wanderPauseTimer:F1}s");
            return;
        }

        wanderPauseTimer -= Time.deltaTime;
        if (wanderPauseTimer > 0f) return;   // standing idle

        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        Vector3 candidate = homePosition + new Vector3(offset.x, 0f, offset.y);
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = TRAVEL_STOP_DISTANCE;
            hasWanderTarget = agent.SetDestination(hit.position);
            log(hasWanderTarget ? $"new wander target {hit.position}" : "SetDestination refused the target");
        }
        else log($"no navmesh near candidate {candidate}");
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
        agent.speed = moveSpeed;
        agent.stoppingDistance = CHASE_STOP_DISTANCE;
        agent.SetDestination(target.position);
    }

    void log(string message)
    {
        if (debugAi) Debug.Log($"[Zombie] {name}: {message}", this);
    }

    void OnDrawGizmosSelected()
    {
        if (!debugAi || agent == null || !agent.hasPath) return;
        Gizmos.color = wasChasing ? Color.red : Color.cyan;
        Gizmos.DrawSphere(agent.destination, 0.2f);
        Gizmos.DrawLine(transform.position, agent.destination);
    }
}
