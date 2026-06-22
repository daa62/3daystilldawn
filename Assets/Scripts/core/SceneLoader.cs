using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static void load(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
