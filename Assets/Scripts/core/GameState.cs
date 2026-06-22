using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    readonly Dictionary<string, bool> flags = new Dictionary<string, bool>();

    public UnityEvent<string, bool> onFlagChanged = new UnityEvent<string, bool>();

    void Awake()
    {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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
}
