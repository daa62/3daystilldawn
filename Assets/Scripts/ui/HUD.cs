using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI promptText;

    public void setPrompt(string text)
    {
        if (promptText != null) {
            promptText.text = text;
        }
    }
}
