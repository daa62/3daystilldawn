using UnityEngine;

public class FloatingText : MonoBehaviour
{
    private string text;
    private Color color;
    private float duration;
    private float floatSpeed;
    private float elapsed;
    private Vector3 worldPos;
    private GUIStyle style;

    public static void Spawn(string text, Vector3 worldPos, Color color, float duration = 1f, float floatSpeed = 1.5f)
    {
        var obj = new GameObject("FloatingText");
        var ft = obj.AddComponent<FloatingText>();
        ft.text = text;
        ft.worldPos = worldPos;
        ft.color = color;
        ft.duration = duration;
        ft.floatSpeed = floatSpeed;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        worldPos.y += floatSpeed * Time.deltaTime;
        if (elapsed >= duration) Destroy(gameObject);
    }

    void OnGUI()
    {
        if (style == null) {
            style = new GUIStyle(GUI.skin.label) {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
        }

        float alpha = 1f - (elapsed / duration);
        style.normal.textColor = new Color(color.r, color.g, color.b, alpha);

        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        if (screenPos.z < 0) return;

        float x = screenPos.x - 50f;
        float y = Screen.height - screenPos.y - 20f;
        GUI.Label(new Rect(x, y, 100f, 40f), text, style);
    }
}
