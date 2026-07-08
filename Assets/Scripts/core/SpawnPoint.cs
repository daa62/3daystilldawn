using UnityEngine;

// Where the player appears after a scene transition. TransitionDoor sets
// nextSpawnId before loading; the matching SpawnPoint moves the player onto itself.
public class SpawnPoint : MonoBehaviour
{
    public const string SAFE_ROOM_DOOR = "SafeRoomDoor";
    public const string STORE_DOOR     = "StoreDoor";

    public static string nextSpawnId;

    [SerializeField] string id = STORE_DOOR;

    void Start()
    {
        if (nextSpawnId != id) return;
        nextSpawnId = null;

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
