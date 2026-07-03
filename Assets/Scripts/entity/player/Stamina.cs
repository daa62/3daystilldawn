using UnityEngine;
using UnityEngine.Events;

// Sprint/jump stamina for the player. Mirrors Health: a self-contained resource with
// change events so the HUD can react without coupling. The controller feeds it state
// (setSprinting / spendJump); it owns its own regen. Hard-lockout exhaustion: once
// emptied, sprint stays disabled until stamina climbs back past a recovery threshold.
public class Stamina : MonoBehaviour
{
    public float Max => GameManager.STAMINA_MAX;
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

    void Update()
    {
        if (sprinting)
            drain(GameManager.STAMINA_SPRINT_DRAIN * Time.deltaTime);
        else if (regenCooldown > 0f)
            regenCooldown -= Time.deltaTime;
        else
            recover(GameManager.STAMINA_REGEN * Time.deltaTime);
    }

    // Controller tells us whether the player is actively sprinting this frame.
    public void setSprinting(bool value)
    {
        sprinting = value;
        if (value) regenCooldown = GameManager.STAMINA_REGEN_DELAY;
    }

    // Try to pay for a jump. Returns false (and spends nothing) if too low.
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
