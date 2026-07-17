using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    readonly Dictionary<string, bool> flags = new Dictionary<string, bool>();
    readonly Dictionary<string, int> counters = new Dictionary<string, int>();

    public UnityEvent<string, bool> onFlagChanged = new UnityEvent<string, bool>();
    public UnityEvent<string, int> onCounterChanged = new UnityEvent<string, int>();

    void Awake()
    {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);   // DontDestroyOnLoad is silently ignored on child objects
        DontDestroyOnLoad(gameObject);

        // a new game started from the title screen (no GameState there) seeds here
        DayCycle.applyPendingSeed();
    }

    public void setFlag(string key, bool value = true)
    {
        bool changed = !flags.TryGetValue(key, out bool current) || current != value;
        flags[key] = value;
        if (changed) onFlagChanged.Invoke(key, value);
    }

    public bool getFlag(string key) => flags.TryGetValue(key, out bool value) && value;

    public bool hasFlag(string key) => flags.ContainsKey(key);

    public void clearFlag(string key)
    {
        if (flags.Remove(key)) onFlagChanged.Invoke(key, false);
    }

    // numeric progress (bond, friend health, ...) — survives scene loads
    public int getCounter(string key) => counters.TryGetValue(key, out int value) ? value : 0;

    public void setCounter(string key, int value)
    {
        if (getCounter(key) == value) return;
        counters[key] = value;
        onCounterChanged.Invoke(key, value);
    }

    public void addCounter(string key, int delta)
    {
        if (delta != 0) setCounter(key, getCounter(key) + delta);
    }
}
