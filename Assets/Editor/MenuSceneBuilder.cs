using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// One-shot builder for the Milestone 2 scene structure:
//   Title  ->  Intro  ->  GameScene   (three linked scenes)
// Creates Assets/Scenes/Title.unity and Intro.unity fully wired (Canvas, buttons,
// TMP text, MainMenu navigation) and registers all three in Build Settings in order.
//
// Run:  Tools > M2 > Build Menu Scenes (Title + Intro)
public static class MenuSceneBuilder
{
    const string SCENES_DIR = "Assets/Scenes";
    const string TITLE_PATH = "Assets/Scenes/Title.unity";
    const string INTRO_PATH = "Assets/Scenes/Intro.unity";
    const string GAME_PATH  = "Assets/Scenes/GameScene.unity";

    static readonly Color BG      = new Color(0.06f, 0.07f, 0.09f, 1f);
    static readonly Color PANEL   = new Color(0f, 0f, 0f, 0.75f);
    static readonly Color BTN     = new Color(0.16f, 0.18f, 0.22f, 1f);
    static readonly Color ACCENT  = new Color(0.85f, 0.30f, 0.25f, 1f);

    [MenuItem("Tools/M2/Build Menu Scenes (Title + Intro)")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return; // user cancelled; don't clobber their open scene

        BuildTitle();
        BuildIntro();
        RegisterBuildSettings();

        AssetDatabase.SaveAssets();
        Debug.Log("[M2] Built Title.unity + Intro.unity and set Build Settings order " +
                  "(Title -> Intro -> GameScene). Open Title.unity and press Play to test the flow.");
        EditorUtility.DisplayDialog("M2 Menu Scenes",
            "Created:\n  • Assets/Scenes/Title.unity\n  • Assets/Scenes/Intro.unity\n\n" +
            "Build Settings order:\n  0  Title\n  1  Intro\n  2  GameScene\n\n" +
            "Open Title.unity and press Play.", "OK");
    }

    // ---------------------------------------------------------------- Title

    static void BuildTitle()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        NewEventSystem();
        var menu = new GameObject("MenuController").AddComponent<MainMenu>();

        var canvas = NewCanvas("TitleCanvas");
        Background(canvas.transform);

        Label(canvas.transform, "Title", "3 DAYS TILL DAWN", 96, ACCENT,
              FontStyles.Bold, new Vector2(0, 220), new Vector2(1400, 160));
        Label(canvas.transform, "Subtitle", "A narrative survival game — survive three days until rescue.",
              32, Color.white, FontStyles.Normal, new Vector2(0, 120), new Vector2(1400, 60));

        var start = Button(canvas.transform, "StartButton", "Start Game", new Vector2(0, -20));
        var instr = Button(canvas.transform, "InstructionsButton", "Instructions", new Vector2(0, -110));
        var quit  = Button(canvas.transform, "QuitButton", "Quit", new Vector2(0, -200));

        // instructions overlay (hidden at runtime by MainMenu.Start)
        var panel = Panel(canvas.transform, "InstructionsPanel");
        Label(panel.transform, "InstrTitle", "How to Play", 56, ACCENT, FontStyles.Bold,
              new Vector2(0, 300), new Vector2(1200, 90));
        Label(panel.transform, "InstrBody", ControlsText(), 34, Color.white, FontStyles.Normal,
              new Vector2(0, 20), new Vector2(1100, 480), TextAlignmentOptions.TopLeft);
        var back = Button(panel.transform, "BackButton", "Back", new Vector2(0, -330));

        Wire(start.onClick, menu.startGame);
        Wire(instr.onClick, menu.showInstructions);
        Wire(quit.onClick,  menu.quitGame);
        Wire(back.onClick,  menu.hideInstructions);

        // link the panel into MainMenu so it can toggle it
        var so = new SerializedObject(menu);
        so.FindProperty("instructionsPanel").objectReferenceValue = panel;
        so.ApplyModifiedPropertiesWithoutUndo();
        panel.SetActive(false);

        EditorSceneManager.SaveScene(scene, TITLE_PATH);
    }

    // ---------------------------------------------------------------- Intro

    static void BuildIntro()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        NewEventSystem();
        var menu = new GameObject("MenuController").AddComponent<MainMenu>();

        var canvas = NewCanvas("IntroCanvas");
        Background(canvas.transform);

        Label(canvas.transform, "Heading", "THREE DAYS TILL DAWN", 64, ACCENT, FontStyles.Bold,
              new Vector2(0, 400), new Vector2(1500, 100));

        Label(canvas.transform, "Story", StoryText(), 32, Color.white, FontStyles.Normal,
              new Vector2(0, 170), new Vector2(1300, 320), TextAlignmentOptions.Top);

        Label(canvas.transform, "Controls", ControlsText(), 30, new Color(0.8f, 0.85f, 0.9f, 1f),
              FontStyles.Normal, new Vector2(0, -190), new Vector2(1100, 300), TextAlignmentOptions.TopLeft);

        var cont = Button(canvas.transform, "ContinueButton", "Continue", new Vector2(320, -430));
        var back = Button(canvas.transform, "BackButton", "Back", new Vector2(-320, -430));

        Wire(cont.onClick, menu.continueToGame);
        Wire(back.onClick, menu.backToTitle);

        EditorSceneManager.SaveScene(scene, INTRO_PATH);
    }

    // ---------------------------------------------------------------- text

    static string StoryText()
    {
        return "A zombie horde has torn through the town. You and your friend are stranded " +
               "with no way out but to hold on.\n\n" +
               "Explore the abandoned mart, gather what you can, and keep your friend alive. " +
               "Rescue arrives in three days — if you both make it that far.\n\n" +
               "Watch your friend closely. Their body language tells you more than words ever could.";
    }

    static string ControlsText()
    {
        return "WASD  —  Move\n" +
               "Mouse  —  Look / Aim\n" +
               "Left Shift  —  Sprint\n" +
               "Space  —  Jump\n" +
               "E  —  Interact / Pick up\n" +
               "Tab  —  Toggle inventory";
    }

    // ---------------------------------------------------------------- helpers

    static void NewEventSystem()
    {
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    static Canvas NewCanvas(string name)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    static Image Background(Transform parent)
    {
        var go = new GameObject("Background", typeof(Image));
        Attach(go.transform, parent);
        Stretch(go.GetComponent<RectTransform>());
        go.GetComponent<Image>().color = BG;
        return go.GetComponent<Image>();
    }

    static GameObject Panel(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(Image));
        Attach(go.transform, parent);
        Stretch(go.GetComponent<RectTransform>());
        go.GetComponent<Image>().color = PANEL;
        return go;
    }

    static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color,
        FontStyles style, Vector2 pos, Vector2 sizeDelta,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject(name, typeof(TextMeshProUGUI));
        Attach(go.transform, parent);
        var rt = go.GetComponent<RectTransform>();
        Center(rt);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        return tmp;
    }

    static Button Button(Transform parent, string name, string label, Vector2 pos)
    {
        var go = new GameObject(name, typeof(Image), typeof(Button));
        Attach(go.transform, parent);
        var rt = go.GetComponent<RectTransform>();
        Center(rt);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(360, 72);
        var img = go.GetComponent<Image>();
        img.color = BTN;
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var txt = Label(go.transform, "Label", label, 34, Color.white, FontStyles.Bold,
                        Vector2.zero, new Vector2(360, 72));
        Stretch(txt.GetComponent<RectTransform>());
        return btn;
    }

    static void Wire(Button.ButtonClickedEvent evt, UnityEngine.Events.UnityAction action)
    {
        UnityEventTools.AddPersistentListener(evt, action);
    }

    static void Attach(Transform child, Transform parent)
    {
        child.SetParent(parent, false);
    }

    static void Center(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void RegisterBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(TITLE_PATH, true),
            new EditorBuildSettingsScene(INTRO_PATH, true),
            new EditorBuildSettingsScene(GAME_PATH,  true),
        };
    }
}
