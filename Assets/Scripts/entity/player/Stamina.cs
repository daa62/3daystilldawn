using UnityEngine;
using UnityEngine.Events;

// Sprint/jump stamina. Once emptied, sprint stays locked out until it climbs
// back past the recovery threshold.
public class Stamina : MonoBehaviour
{
    public float Max { get; private set; } = GameManager.STAMINA_MAX;
    public float Current { get; private set; }

    public bool IsExhausted { get; private set; }
    public bool CanSprint => !IsExhausted && Current > 0f;

    // (current, max)
    public UnityEvent<float, float> onChanged = new UnityEvent<float, float>();

    bool sprinting;
    float regenCooldown;   // time until regen resumes after the last exertion

    void Awake()
    {
        Current = Max;
    }

    public void setMax(float newMax)
    {
        Max = Mathf.Max(1f, newMax);
        Current = Mathf.Min(Current, Max);
        onChanged.Invoke(Current, Max);
    }

    void Update()
    {
        if (sprinting)
            drain(GameManager.STAMINA_SPRINT_DRAIN * Time.deltaTime);
        else if (regenCooldown > 0f)
            regenCooldown -= Time.deltaTime;
        else
            recover(GameManager.STAMINA_REGEN * Time.deltaTime);
    }

    public void setSprinting(bool value)
    {
        sprinting = value;
        if (value) regenCooldown = GameManager.STAMINA_REGEN_DELAY;
    }

    // returns false (and spends nothing) if too low for a jump
    public bool spendJump()
    {
        if (Current < GameManager.STAMINA_JUMP_COST) return false;
        drain(GameManager.STAMINA_JUMP_COST);
        regenCooldown = GameManager.STAMINA_REGEN_DELAY;
        return true;
    }

    void drain(float amount)
    {
        setCurrent(Current - amount);
        if (Current <= 0f) IsExhausted = true;   // locked out until recovery threshold
    }

    void recover(float amount)
    {
        setCurrent(Current + amount);
        if (IsExhausted && Current >= GameManager.STAMINA_RECOVER_THRESHOLD)
            IsExhausted = false;
    }

    void setCurrent(float value)
    {
        float clamped = Mathf.Clamp(value, 0f, Max);
        if (Mathf.Approximately(clamped, Current)) return;
        Current = clamped;
        onChanged.Invoke(Current, Max);
    }
}
