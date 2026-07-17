using UnityEngine;

// Drives Samuel's model animator from two signals:
//   Speed     (float) — idle vs walk, from real frame displacement (same trick the
//                       Zombie uses; controller.velocity misses code-driven moves)
//   Condition (int)   — which set to use, chosen from his current health:
//                       0 normal/upright, 1 injured/limping
// Animator lives on the model child, per the logic-root + art-child convention.
public class FriendAnimator : MonoBehaviour
{
    static readonly int SpeedParam     = Animator.StringToHash("Speed");
    static readonly int ConditionParam = Animator.StringToHash("Condition");

    [Header("Condition threshold (friend health, 0-100)")]
    [Tooltip("At or above this: normal/upright set. Below it: injured/limping.")]
    [SerializeField] int healthyAtOrAbove = 60;

    public enum Condition { Normal = 0, Injured = 1 }

    Animator animator;   // on the model child; harmless if Samuel has no model yet
    Vector3 lastPosition;

    void Awake()
    {
        animator     = GetComponentInChildren<Animator>();
        lastPosition = transform.position;
    }

    void Update()
    {
        if (animator == null) return;

        // horizontal speed from displacement, damped so the walk blend eases in/out
        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        lastPosition = transform.position;
        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        animator.SetFloat(SpeedParam, speed, 0.1f, Time.deltaTime);

        animator.SetInteger(ConditionParam, (int)conditionFromHealth());
    }

    Condition conditionFromHealth()
    {
        var state = GameState.Instance;
        if (state == null) return Condition.Normal;   // e.g. scrubbing the scene outside a run

        int health = state.getCounter(GameManager.COUNTER_FRIEND_HEALTH);
        return health >= healthyAtOrAbove ? Condition.Normal : Condition.Injured;
    }
}
