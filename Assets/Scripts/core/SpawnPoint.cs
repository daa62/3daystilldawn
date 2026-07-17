using UnityEngine;

// Where the player appears after a scene transition. TransitionDoor sets
// nextSpawnId before loading; the matching SpawnPoint moves the player onto itself.
// Also usable mid-scene: movePlayerTo teleports to a named point (e.g. waking up
// across the room after a night's rest, behind the rest fade).
public class SpawnPoint : MonoBehaviour
{
    public const string SAFE_ROOM_DOOR = "SafeRoomDoor";
    public const string STORE_DOOR     = "StoreDoor";
    public const string WAKE_UP        = "WakeUp";

    public static string nextSpawnId;

    [SerializeField] string id = STORE_DOOR;

    void Start()
    {
        if (nextSpawnId != id) return;
        nextSpawnId = null;
        placePlayer();
    }

    // teleport the player to the SpawnPoint with this id, if the scene has one
    public static void movePlayerTo(string spawnId)
    {
        foreach (SpawnPoint point in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None)) {
            if (point.id == spawnId) {
                point.placePlayer();
                return;
            }
        }
        Debug.LogWarning($"SpawnPoint: no point with id '{spawnId}' in this scene.");
    }

    void placePlayer()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player == null) {
            Debug.LogWarning($"SpawnPoint '{id}': no PlayerController in scene.");
            return;
        }

        // CharacterController ignores transform writes while enabled, so toggle it
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        player.transform.SetPositionAndRotation(
            transform.position,
            Quaternion.Euler(0f, transform.eulerAngles.y, 0f));

        if (controller != null) controller.enabled = true;
    }
}
