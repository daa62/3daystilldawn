using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Self-building dialogue window. Callers chain show() / showChoice() calls.
// While open the cursor is freed, which also stops player movement.
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    public bool IsOpen { get; private set; }

    // PlayerInteractor checks this so the E that dismissed the final line
    // can't re-trigger the NPC on the same frame
    public int LastClosedFrame { get; private set; } = -1;

    // likewise, the E that opened the dialogue would instantly advance it
    // (GetKeyDown stays true all frame) — ignore input on the frame a line appears
    int shownFrame = -1;

    static readonly Color PANEL_BG = new Color(0.04f, 0.05f, 0.07f, 0.94f);

    GameObject panel;
    RectTransform panelRect;
    TextMeshProUGUI speakerLabel;
    TextMeshProUGUI bodyLabel;
    Button continueButton;
    RectTransform choiceArea;
    GridLayoutGroup choiceGrid;

    readonly List<GameObject> choiceButtons = new List<GameObject>();
    Action onContinue;
    Action<int> onChoose;

    void Awake()
    {
        Instance = this;
        build();
        panel.SetActive(false);
    }

    void Update()
    {
        // let Space / E advance a plain line (choices must be clicked)
        if (IsOpen && shownFrame != Time.frameCount && continueButton.gameObject.activeSelf &&
            (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E)))
            continueClicked();
    }

    // ---- public API ----

    public void show(string speaker, string line, Action onContinue)
    {
        open(speaker, line);
        this.onContinue = onContinue;
        this.onChoose = null;
        clearChoices();
        continueButton.gameObject.SetActive(true);
        relayout(0);
    }

    public void showChoice(string speaker, string line, string[] labels, Action<int> onChoose)
    {
        open(speaker, line);
        this.onChoose = onChoose;
        this.onContinue = null;
        continueButton.gameObject.SetActive(false);
        buildChoices(labels);
        relayout(labels.Length);
    }

    public void close()
    {
        IsOpen = false;
        LastClosedFrame = Time.frameCount;
        onContinue = null;
        onChoose = null;
        clearChoices();
        panel.SetActive(false);
        freezePlayer(false);
    }

    // ---- internals ----

    void open(string speaker, string line)
    {
        if (!IsOpen)
        {
            IsOpen = true;
            panel.SetActive(true);
            freezePlayer(true);
        }
        shownFrame = Time.frameCount;
        speakerLabel.text = speaker;
        bodyLabel.text = line;
    }

    void continueClicked()
    {
        Sfx.play(Sfx.UI_CLICK, 0.5f);
        Action cb = onContinue;
        onContinue = null;
        if (cb != null) cb();
        else close();
    }

    void choiceClicked(int index)
    {
        Sfx.play(Sfx.UI_CLICK, 0.5f);
        Action<int> cb = onChoose;
        onChoose = null;
        clearChoices();
        if (cb != null) cb(index);
        else close();
    }

    void freezePlayer(bool frozen)
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.lockCursor(!frozen);   // unlocked cursor = player input disabled
    }

    // ---- build ----

    void build()
    {
        UiFactory.ensureEventSystem();
        var canvas = UiFactory.overlayCanvas(transform, "DialogueCanvas");

        var bg = UiFactory.image(canvas.transform, "Panel", PANEL_BG);
        panel = bg.gameObject;
        panelRect = bg.rectTransform;
        UiFactory.anchor(bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
        bg.rectTransform.anchoredPosition = new Vector2(0, 40);
        bg.rectTransform.sizeDelta = new Vector2(1500, 400);

        speakerLabel = UiFactory.text(panel.transform, "Speaker", "", 30,
                                      new Color(0.85f, 0.5f, 0.3f, 1f), TextAlignmentOptions.TopLeft);
        UiFactory.anchor(speakerLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        speakerLabel.rectTransform.anchoredPosition = new Vector2(40, -24);
        speakerLabel.rectTransform.sizeDelta = new Vector2(1000, 44);
        speakerLabel.fontStyle = FontStyles.Bold;

        bodyLabel = UiFactory.text(panel.transform, "Body", "", 30, Color.white, TextAlignmentOptions.TopLeft);
        UiFactory.anchor(bodyLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        bodyLabel.rectTransform.anchoredPosition = new Vector2(40, -80);
        bodyLabel.rectTransform.sizeDelta = new Vector2(1420, 100);

        continueButton = UiFactory.button(panel.transform, "Continue", "Continue  [Space]", 26f);
        UiFactory.anchor(continueButton.image.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));
        continueButton.image.rectTransform.anchoredPosition = new Vector2(-40, 30);
        continueButton.image.rectTransform.sizeDelta = new Vector2(320, 60);
        continueButton.onClick.AddListener(continueClicked);

        // layout group + fitter stack the choice buttons, no per-button position math
        var areaGO = new GameObject("Choices", typeof(RectTransform));
        areaGO.transform.SetParent(panel.transform, false);
        choiceArea = areaGO.GetComponent<RectTransform>();
        UiFactory.anchor(choiceArea, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
        choiceArea.anchoredPosition = new Vector2(0, 24);
        choiceArea.sizeDelta = new Vector2(1000, 0);

        // grid so long menus (the give list) can wrap to two columns
        choiceGrid = areaGO.AddComponent<GridLayoutGroup>();
        choiceGrid.spacing = new Vector2(10f, 6f);
        choiceGrid.childAlignment = TextAnchor.LowerCenter;
        choiceGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

        var fitter = areaGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    void buildChoices(string[] labels)
    {
        clearChoices();

        // short menus read best as one column; long ones wrap to two
        int columns = labels.Length > 4 ? 2 : 1;
        choiceGrid.constraintCount = columns;
        choiceGrid.cellSize = new Vector2(columns == 2 ? 495f : 1000f, 42f);

        for (int i = 0; i < labels.Length; i++)
        {
            int index = i;
            var btn = UiFactory.button(choiceArea, "Choice" + i, labels[i], 20f);
            btn.onClick.AddListener(() => choiceClicked(index));
            choiceButtons.Add(btn.gameObject);
        }
    }

    // grow the panel to fit long bodies and tall choice stacks — a fixed height let
    // big give-menus climb up and over the body text
    void relayout(int choiceCount)
    {
        float bodyH = Mathf.Max(60f, bodyLabel.GetPreferredValues(bodyLabel.text, 1420f, 0f).y);
        bodyLabel.rectTransform.sizeDelta = new Vector2(1420f, bodyH);

        int rows = choiceCount == 0 ? 0
            : Mathf.CeilToInt(choiceCount / (float)choiceGrid.constraintCount);
        float footerH = rows == 0
            ? 100f                                                      // continue button row
            : rows * choiceGrid.cellSize.y + (rows - 1) * choiceGrid.spacing.y + 48f;

        float height = Mathf.Max(400f, 90f + bodyH + 24f + footerH);    // 90 = speaker header
        panelRect.sizeDelta = new Vector2(1500f, height);
    }

    void clearChoices()
    {
        foreach (var go in choiceButtons)
        {
            if (go == null) continue;
            // Destroy() is deferred — deactivate first so the layout group
            // doesn't count dead buttons when the next menu builds
            go.SetActive(false);
            Destroy(go);
        }
        choiceButtons.Clear();
    }
}
