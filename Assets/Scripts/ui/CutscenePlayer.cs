using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Full-screen slideshow; slides load by name from Resources/Cutscenes.
public class CutscenePlayer : MonoBehaviour
{
    const float FADE_SECONDS = 0.7f;

    static CutscenePlayer active;

    string[] slides;
    Action onDone;
    Image slideImage;

    public static void play(string[] slideNames, Action onDone)
    {
        if (active != null) return;

        var go = new GameObject("Cutscene");
        active = go.AddComponent<CutscenePlayer>();
        active.slides = slideNames;
        active.onDone = onDone;
        Sfx.ambience(Sfx.MUSIC_CUTSCENE);
    }

    void Start()
    {
        UiFactory.ensureEventSystem();
        var canvas = UiFactory.overlayCanvas(transform, "CutsceneCanvas");
        canvas.sortingOrder = 100;

        var bg = UiFactory.image(canvas.transform, "Background", Color.black);
        UiFactory.stretch(bg.rectTransform);

        slideImage = UiFactory.image(canvas.transform, "Slide", Color.white);
        UiFactory.stretch(slideImage.rectTransform);
        slideImage.preserveAspect = true;
        setAlpha(0f);

        var hint = UiFactory.text(canvas.transform, "Hint", "E — continue      Esc — skip", 22,
            new Color(1f, 1f, 1f, 0.45f), TextAlignmentOptions.Center);
        UiFactory.anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        hint.rectTransform.anchoredPosition = new Vector2(0, 28);
        hint.rectTransform.sizeDelta = new Vector2(600, 30);

        StartCoroutine(run());
    }

    IEnumerator run()
    {
        foreach (string name in slides) {
            var sprite = Resources.Load<Sprite>("Cutscenes/" + name);
            if (sprite == null) continue;

            slideImage.sprite = sprite;
            yield return fade(0f, 1f);

            bool skip = false;
            while (true) {
                if (Input.GetKeyDown(KeyCode.Escape)) { skip = true; break; }
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)
                    || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)) break;
                yield return null;
            }

            yield return fade(1f, 0f);
            if (skip) break;
        }
        finish();
    }

    IEnumerator fade(float from, float to)
    {
        for (float t = 0f; t < FADE_SECONDS; t += Time.deltaTime) {
            setAlpha(Mathf.Lerp(from, to, t / FADE_SECONDS));
            yield return null;
        }
        setAlpha(to);
    }

    void setAlpha(float a)
    {
        slideImage.color = new Color(1f, 1f, 1f, a);
    }

    void finish()
    {
        active = null;
        var done = onDone;
        Destroy(gameObject);
        done?.Invoke();
    }
}
