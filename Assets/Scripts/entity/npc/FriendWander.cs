using UnityEngine;
using UnityEngine.AI;

// Samuel restlessly paces the safe room. NavMesh-driven: he picks random reachable
// spots near wherever he currently is and lets the agent path around the furniture —
// the old raycast whiskers kept steering him into walls. Untethered, so over time he
// drifts anywhere the baked mesh allows. He holds still whenever a conversation
// is open, and the agent's movement drives FriendAnimator's Speed (via displacement),
// so the walk clip plays itself — no extra wiring.
//
// Needs a baked NavMesh in the room (NavMeshSurface) and a NavMeshAgent on this object.
// The CharacterController stays purely as the interaction raycast collider; this script
// never moves it.
[RequireComponent(typeof(NavMeshAgent))]
public class FriendWander : MonoBehaviour
{
    [Header("Wandering")]
    [Tooltip("How far each stroll reaches from where he's standing. 0 = stays put.")]
    [SerializeField] float wanderRadius = 5f;
    [Tooltip("Walk speed (units/second). Tune so the feet don't slide against the walk clip.")]
    [SerializeField] float moveSpeed = 3f;
    [Tooltip("Random pause range (seconds) standing still between strolls.")]
    [SerializeField] float pauseMin = 1f;
    [SerializeField] float pauseMax = 5f;

    [Header("Debug")]
    [Tooltip("Log every wander decision to the console and draw the current destination in the Scene view.")]
    [SerializeField] bool debugWander = false;

    NavMeshAgent agent;
    float pauseTimer;
    float giveUpTimer;
    bool  strolling;
    bool  warnedOffMesh;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
    }

    void Start()
    {
        pauseTimer = 5f;   // don't stroll the instant the scene loads
    }

    void Update()
    {
        if (!agent.isOnNavMesh) {
            // no bake (or he was placed off it) — stay put instead of spamming errors
            if (!warnedOffMesh) {
                warnedOffMesh = true;
                Debug.LogWarning("[FriendWander] Not on a NavMesh — bake a NavMeshSurface in this room.", this);
            }
            return;
        }

        // never wander off mid-conversation
        var dialogue = DialogueUI.Instance;
        bool talking = dialogue != null && dialogue.IsOpen;
        agent.isStopped = talking;
        if (talking) {
            log("blocked: dialogue open");
            return;
        }

        wander();
    }

    void wander()
    {
        if (wanderRadius <= 0f || agent.pathPending) return;

        if (strolling) {
            // a partial path (target on an unreachable patch) never "arrives" — the
            // give-up timer shrugs and re-rolls instead of freezing at the dead end
            giveUpTimer -= Time.deltaTime;
            if (agent.remainingDistance > agent.stoppingDistance && giveUpTimer > 0f)
                return;   // still walking

            log(giveUpTimer <= 0f ? "gave up on unreachable target" : "arrived");
            strolling  = false;
            agent.ResetPath();
            pauseTimer = Random.Range(pauseMin, pauseMax);   // arrived (or gave up) — stand a moment
            return;
        }

        pauseTimer -= Time.deltaTime;
        if (pauseTimer > 0f) return;

        // random spot near where he stands, snapped onto the navmesh; if it lands
        // inside furniture (no mesh within reach), just try again next frame.
        // sample at mesh height, not at the root — base offset can hold the transform
        // well above the floor, which would put every candidate out of sampling reach
        Vector2 offset    = Random.insideUnitCircle * wanderRadius;
        Vector3 candidate = transform.position + new Vector3(offset.x, -agent.baseOffset, offset.y);
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas)) {
            strolling   = agent.SetDestination(hit.position);
            giveUpTimer = wanderRadius / Mathf.Max(0.1f, moveSpeed) + 2f;   // generous walk budget
            log(strolling ? $"new target {hit.position}" : "SetDestination refused the target");
        } else {
            log($"no navmesh within 2m of candidate {candidate}");
        }
    }

    void log(string message)
    {
        if (debugWander) Debug.Log($"[FriendWander] {message}", this);
    }

    void OnDrawGizmosSelected()
    {
        if (!debugWander || agent == null || !agent.hasPath) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(agent.destination, 0.15f);
        Gizmos.DrawLine(transform.position, agent.destination);
    }
}
