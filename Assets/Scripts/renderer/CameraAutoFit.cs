using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAutoFit : MonoBehaviour
{
    public int gridWidth = GameManager.MAP_WIDTH;
    public int gridHeight = GameManager.MAP_HEIGHT;
    public float cellSize = GameManager.CELL_SIZE;
    public float padding = GameManager.CAMERA_PADDING;
    public float followSpeed = GameManager.CAMERA_FOLLOW_SPEED;

    private Camera cam;
    private Transform target;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        gridWidth = GameManager.MAP_WIDTH;
        gridHeight = GameManager.MAP_HEIGHT;
        cellSize = GameManager.CELL_SIZE;
        padding = GameManager.CAMERA_PADDING;
        followSpeed = GameManager.CAMERA_FOLLOW_SPEED;

        float totalW = gridWidth * cellSize;
        float totalH = gridHeight * cellSize;
        float halfViewWidth = totalW * 0.5f + padding;

        cam.orthographicSize = GameManager.CAMERA_SHOW_FULL_MAP
            ? totalH * 0.5f + padding
            : halfViewWidth / cam.aspect;

        target = FindAnyObjectByType<Player>()?.transform;
        Vector3 start = GameManager.CAMERA_SHOW_FULL_MAP
            ? new Vector3(totalW * 0.5f, totalH * 0.5f, 0f)
            : target != null ? target.position : new Vector3(totalW * 0.5f, 0f, 0f);
        transform.position = new Vector3(start.x, start.y, GameManager.CAMERA_Z);
    }

    void LateUpdate()
    {
        if (target == null) {
            target = FindAnyObjectByType<Player>()?.transform;
            if (target == null) {
                return;
            }
        }

        if (GameManager.CAMERA_SHOW_FULL_MAP) {
            return;
        }

        Vector3 next = new Vector3(target.position.x, target.position.y, GameManager.CAMERA_Z);
        transform.position = Vector3.Lerp(transform.position, next, followSpeed * Time.deltaTime);
    }
}
