// Every scene change fades through black (see SceneTransition). Call sites don't need
// to know — they just ask for the scene and get the wipe for free.
public static class SceneLoader
{
    public static void load(string sceneName) => SceneTransition.loadWithFade(sceneName);
}
