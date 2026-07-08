using UnityEngine;
using UnityEngine.Events;

// Hit points for anything damageable; fires events for the HUD / death logic.
public class Health : MonoBehaviour
{
    [SerializeField] float maxHealth = 100f;

    public float Max => maxHealth;
    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;

    // (current, max)
    public UnityEvent<float, float> onHealthChanged = new UnityEvent<float, float>();
    public UnityEvent onDeath = new UnityEvent();

    void Awake()
    {
        Current = maxHealth;
    }

    // lasting wounds / treatment change the ceiling
    public void setMax(float newMax)
    {
        maxHealth = Mathf.Max(1f, newMax);
        Current = Mathf.Min(Current, maxHealth);
        onHealthChanged.Invoke(Current, maxHealth);
    }

    public void damage(float amount)
    {
        if (amount <= 0f || IsDead) return;

        applyDelta(-amount);
        if (IsDead) onDeath.Invoke();
    }

    public void heal(float amount)
    {
        if (amount <= 0f || IsDead || Current >= maxHealth) return;
        applyDelta(amount);
    }

    void applyDelta(float delta)
    {
        Current = Mathf.Clamp(Current + delta, 0f, maxHealth);
        onHealthChanged.Invoke(Current, maxHealth);
    }
}
