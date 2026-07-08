using System;
using UnityEngine;

// Noise event bus. Anything loud calls emit(); listeners (zombies) decide
// whether they were close enough to hear it.
public static class Noise
{
    // (position of the sound, how far it carries in units)
    public static event Action<Vector3, float> onNoise;

    public static void emit(Vector3 position, float radius)
    {
        onNoise?.Invoke(position, radius);
    }
}
