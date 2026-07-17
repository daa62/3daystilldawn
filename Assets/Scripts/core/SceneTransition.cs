using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A brief fade-to-black wrapped around a scene load: cover the screen, swap the scene,
// then fade back in — so every transition reads as a beat instead of a hard cut. Built
// procedurally (UiFactory, like CutscenePlayer) and survives the load via
// DontDestroyOnLoad. All scene changes route through SceneLoader.load, so this is the
// single place that gives every one of them the wipe.
public class SceneTransition : MonoBehaviour
{
    const float FADE_SECONDS = 0.4f;

    static SceneTransition active;   // one transition at a time

    Image cover;

    public static void loadWithFade(string sceneName)
    {
        if (active != null) return;   // already mid-transition — ignore the extra request

        var go = new GameObject("SceneTransition");
        DontDestroyOnLoad(go);
        active = go.AddComponent<SceneTransition>();
        active.StartCoroutine(active.run(sceneName));
    }

    void Awake()
    {
        UiFactory.ensureEventSystem();
        var canvas = UiFactory.overlayCanvas(transform, "TransitionCanvas");
        canvas.sortingOrder = 1000;   // above all gameplay UI (and the cutscene canvas)

        cover = UiFactory.image(canvas.transform, "Cover", Color.black);
        UiFactory.stretch(cover.rectTransform);
        setAlpha(0f);
    }

    IEnumerator run(string sceneName)
    {
        yield return fade(0f, 1f);          // out to black
        SceneManager.LoadScene(sceneName);
        yield return null;                  // one frame for the new scene to wake up
        yield return fade(1f, 0f);          // back in
        active = null;
        Destroy(gameObject);
    }

    // unscaled so a paused timeScale (e.g. an ending) can't stall the wipe
    IEnumerator fade(float from, float to)
    {
        for (float t = 0f; t < FADE_SECONDS; t += Time.unscaledDeltaTime) {
            setAlpha(Mathf.Lerp(from, to, t / FADE_SECONDS));
            yield return null;
        }
        setAlpha(to);
    }

    void setAlpha(float a) => cover.color = new Color(0f, 0f, 0f, a);
}
