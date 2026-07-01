using UnityEngine;
using UnityEngine.Events;

// Reusable hit-point container for any damageable entity (player, friend NPC, ...).
// Fires events so UI and game-over logic can react without knowing about each other.
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

    public void damage(float amount)
    {
        if (amount <= 0f || IsDead) return;

        applyDelta(-amount);
        if (IsDead) onDeath.Invoke();
    }

    public void heal(float amount)
    {
        if (amount <= 0f || IsDead) return;
        applyDelta(amount);
    }

    void applyDelta(float delta)
    {
        Current = Mathf.Clamp(Current + delta, 0f, maxHealth);
        onHealthChanged.Invoke(Current, maxHealth);
    }
}
