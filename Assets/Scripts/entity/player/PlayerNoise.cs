using UnityEngine;

// Footstep noise: sprinting is loud, walking is quiet, crouching is silent.
[RequireComponent(typeof(PlayerController))]
public class PlayerNoise : MonoBehaviour
{
    PlayerController player;
    float footstepTimer;

    void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    void Update()
    {
        float radius = footstepRadius();
        if (radius <= 0f) {
            footstepTimer = 0f;   // next audible movement starts with an immediate step
            return;
        }

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f) {
            Noise.emit(transform.position, radius);
            Sfx.playAt(Sfx.STEP, transform.position, player.IsSprinting ? 0.9f : 0.45f);
            footstepTimer = player.IsSprinting ? GameManager.NOISE_SPRINT_FOOTSTEP_INTERVAL
                                               : GameManager.NOISE_FOOTSTEP_INTERVAL;
        }
    }

    float footstepRadius()
    {
        bool moving = player.IsGrounded && player.HorizontalSpeed > GameManager.HEADBOB_MIN_SPEED;
        if (!moving || player.IsCrouching) return 0f;
        return player.IsSprinting ? GameManager.NOISE_SPRINT_RADIUS
                                  : GameManager.NOISE_WALK_RADIUS;
    }
}
