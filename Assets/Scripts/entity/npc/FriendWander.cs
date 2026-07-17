using UnityEngine;

// Samuel paces the safe room now and then — the Zombie's idle-wander pattern: stroll to
// a random spot near where he started, pause, repeat. Straight-line movement with whisker
// avoidance so he rounds furniture instead of shoving into it. He holds still whenever a
// conversation is open, and the movement drives FriendAnimator's Speed (via displacement),
// so the walk clip plays itself — no extra wiring.
[RequireComponent(typeof(CharacterController))]
public class FriendWander : MonoBehaviour
{
    [Header("Wandering")]
    [Tooltip("How far he strays from his starting spot. 0 = stays put.")]
    [SerializeField] float wanderRadius = 3f;
    [Tooltip("Walk speed (units/second). Tune so the feet don't slide against the walk clip.")]
    [SerializeField] float moveSpeed = 1f;
    [SerializeField] float turnSpeed = 6f;
    [Tooltip("Random pause range (seconds) standing still between strolls.")]
    [SerializeField] float pauseMin = 4f;
    [SerializeField] float pauseMax = 12f;

    [Header("Obstacle avoidance")]
    [Tooltip("How far ahead he probes for walls to steer around.")]
    [SerializeField] float avoidProbeDistance = 1.2f;
    [Tooltip("Layers treated as walls/props. Set to the safe-room geometry.")]
    [SerializeField] LayerMask obstacleMask = ~0;

    CharacterController controller;
    FriendAnimator      condition;   // gates wandering on his health tier
    Vector3 homePosition;
    Vector3 wanderTarget;
    bool    hasWanderTarget;
    float   pauseTimer;
    float   giveUpTimer;
    float   verticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        condition  = GetComponent<FriendAnimator>();
    }

    void Start()
    {
        homePosition = transform.position;
        pauseTimer   = Random.Range(0f, pauseMax);   // don't stroll the instant the scene loads
    }

    void Update()
    {
        // never wander off mid-conversation
        var dialogue = DialogueUI.Instance;
        if (dialogue == null || !dialogue.IsOpen)
            wander();

        applyGravity();
    }

    void wander()
    {
        if (wanderRadius <= 0f) return;   // parked — he stays put

        // too hurt to pace: while he's below the healthy tier he stays where he is,
        // which reads as him resting/injured (FriendAnimator shows the injured idle)
        if (condition != null && !condition.IsHealthy)
        {
            hasWanderTarget = false;
            return;
        }

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            return;   // standing idle
        }

        if (!hasWanderTarget)
        {
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            wanderTarget    = homePosition + new Vector3(offset.x, 0f, offset.y);
            hasWanderTarget = true;
            giveUpTimer     = wanderRadius / Mathf.Max(0.1f, moveSpeed) + 2f;
        }

        Vector3 flat = wanderTarget - transform.position;
        flat.y = 0f;
        giveUpTimer -= Time.deltaTime;

        if (flat.magnitude <= 0.5f || giveUpTimer <= 0f)
        {
            hasWanderTarget = false;
            pauseTimer      = Random.Range(pauseMin, pauseMax);
            return;
        }

        moveToward(wanderTarget);
    }

    void moveToward(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        direction.Normalize();
        direction = avoidObstacles(direction);   // steer around walls instead of grinding

        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(direction), Time.deltaTime * turnSpeed);
        controller.Move(direction * moveSpeed * Time.deltaTime);
    }

    static readonly float[] AvoidAngles = { 25f, 50f, 75f };
    static readonly int[]   Sides       = { 1, -1 };

    Vector3 avoidObstacles(Vector3 desired)
    {
        if (!blocked(desired)) return desired;

        foreach (float angle in AvoidAngles)
            foreach (int side in Sides)
            {
                Vector3 candidate = Quaternion.Euler(0f, angle * side, 0f) * desired;
                if (!blocked(candidate)) return candidate;
            }
        return desired;   // boxed in — nothing to do but hold
    }

    bool blocked(Vector3 dir)
    {
        // probe just outside our own capsule so we don't self-hit
        Vector3 origin = transform.position + Vector3.up * (controller.height * 0.5f)
                         + dir * (controller.radius + 0.05f);
        return Physics.Raycast(origin, dir, avoidProbeDistance, obstacleMask,
                               QueryTriggerInteraction.Ignore);
    }

    void applyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += GameManager.PLAYER_GRAVITY * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }
}
