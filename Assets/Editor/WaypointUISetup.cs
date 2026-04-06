using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
// TextMeshPro is a package — if it is not installed this file will not compile.
// Install it via: Window > Package Manager > TextMeshPro > Install
// Then import the TMP Essential Resources when prompted.
using TMPro;
#endif

/// <summary>
/// CampusTour > Create Waypoint UI
///
/// Creates:
///   1. A Screen Space – Overlay Canvas ("WaypointInfoCanvas") locked to the
///      bottom centre of the screen, with a navy background, a TMP label,
///      and a "Step X of 5" sub-label.
///   2. A Screen Space – Overlay Canvas ("ResetTourOverlay") with a
///      "Reset Tour" button wired to WaypointManager.ResetTour().
///   3. An EventSystem if one does not already exist in the scene.
/// </summary>
public static class WaypointUISetup
{
    // ── colours ──────────────────────────────────────────────────────────────
    private static readonly Color NavyBackground = new Color(
        0x1A / 255f, 0x2C / 255f, 0x4E / 255f, 200 / 255f);

    private static readonly Color LabelWhite    = Color.white;
    private static readonly Color SubtextGray   = HexToColor("AAAAAA");
    private static readonly Color ButtonNavy    = HexToColor("1A2C4E");
    private static readonly Color ButtonText    = Color.white;

    // ── canvas dimensions ────────────────────────────────────────────────────
    private const float CanvasWidth   = 300f;
    private const float CanvasHeight  = 100f;
    private const float CanvasScale   = 0.01f;

    // ── layout inside the info canvas ────────────────────────────────────────
    private const float PanelPadding  = 8f;
    private const float LabelHeight   = 55f;
    private const float SubtextHeight = 28f;

    // ────────────────────────────────────────────────────────────────────────
    [MenuItem("CampusTour/Create Waypoint UI")]
    public static void CreateWaypointUI()
    {
        EnsureEventSystem();

        GameObject infoCanvas  = CreateInfoCanvas();
        GameObject resetCanvas = CreateResetOverlay();

        // Wire the Reset button to WaypointManager.ResetTour() if a manager
        // already exists in the scene.
        TryWireResetButton(resetCanvas);

        // Select the info canvas so the designer can inspect it immediately.
        Selection.activeGameObject = infoCanvas;

        Debug.Log("[WaypointUISetup] Waypoint UI created. " +
                  "Assign 'WaypointInfoCanvas' to WaypointManager.infoPanel.");
    }

    // ── 1. Screen Space – Overlay info canvas ────────────────────────────────

    private static GameObject CreateInfoCanvas()
    {
        GameObject canvasGO = new GameObject("WaypointInfoCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create WaypointInfoCanvas");

        // Screen Space – Overlay: always drawn on top, no world position needed.
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler     = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode      = CanvasScaler.ScaleMode.ConstantPixelSize;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Scale must be (1,1,1) — World Space scale of 0.01 would make the
        // panel invisible in Overlay mode.
        canvasGO.transform.localScale = Vector3.one;

        // ── Background panel — anchored bottom centre ─────────────────────────
        GameObject bgGO = CreateUIElement("Background", canvasGO);
        Image bg        = bgGO.AddComponent<Image>();
        bg.color        = NavyBackground;

        RectTransform bgRT      = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin          = new Vector2(0.5f, 0f);
        bgRT.anchorMax          = new Vector2(0.5f, 0f);
        bgRT.pivot              = new Vector2(0.5f, 0f);
        bgRT.sizeDelta          = new Vector2(640f, 110f);
        bgRT.anchoredPosition   = new Vector2(0f, 10f);

        // ── Main label (TMP) — sits above the step label ──────────────────────
        GameObject labelGO          = CreateUIElement("WaypointLabel", canvasGO);
        TextMeshProUGUI label       = labelGO.AddComponent<TextMeshProUGUI>();
        label.text                  = "Welcome to Michigan! Start your tour here.";
        label.fontSize              = 18f;
        label.color                 = LabelWhite;
        label.alignment             = TextAlignmentOptions.Center;
        label.enableWordWrapping    = true;

        RectTransform labelRT       = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin           = new Vector2(0.5f, 0f);
        labelRT.anchorMax           = new Vector2(0.5f, 0f);
        labelRT.pivot               = new Vector2(0.5f, 0f);
        labelRT.sizeDelta           = new Vector2(600f, 65f);
        labelRT.anchoredPosition    = new Vector2(0f, 44f);

        // ── Step sub-label (TMP) — sits at the bottom of the background ───────
        GameObject stepGO           = CreateUIElement("StepLabel", canvasGO);
        TextMeshProUGUI step        = stepGO.AddComponent<TextMeshProUGUI>();
        step.text                   = "Step 1 of 5";
        step.fontSize               = 13f;
        step.color                  = SubtextGray;
        step.alignment              = TextAlignmentOptions.Center;

        RectTransform stepRT        = stepGO.GetComponent<RectTransform>();
        stepRT.anchorMin            = new Vector2(0.5f, 0f);
        stepRT.anchorMax            = new Vector2(0.5f, 0f);
        stepRT.pivot                = new Vector2(0.5f, 0f);
        stepRT.sizeDelta            = new Vector2(600f, 28f);
        stepRT.anchoredPosition     = new Vector2(0f, 14f);

        // Hidden by default — WaypointManager.Start() controls visibility.
        canvasGO.SetActive(false);

        return canvasGO;
    }

    // ── 2. Screen Space – Overlay reset button ───────────────────────────────

    private static GameObject CreateResetOverlay()
    {
        GameObject overlayGO = new GameObject("ResetTourOverlay");
        Undo.RegisterCreatedObjectUndo(overlayGO, "Create ResetTourOverlay");

        Canvas overlay          = overlayGO.AddComponent<Canvas>();
        overlay.renderMode      = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder    = 10;   // always on top

        CanvasScaler scaler         = overlayGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight   = 0.5f;

        overlayGO.AddComponent<GraphicRaycaster>();

        // ── Button ───────────────────────────────────────────────────────────
        GameObject btnGO    = CreateUIElement("ResetTourButton", overlayGO);
        Button btn          = btnGO.AddComponent<Button>();

        Image btnImg        = btnGO.AddComponent<Image>();
        btnImg.color        = ButtonNavy;

        // Position: bottom-right corner, 20 px margin
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin     = new Vector2(1f, 0f);
        btnRT.anchorMax     = new Vector2(1f, 0f);
        btnRT.pivot         = new Vector2(1f, 0f);
        btnRT.sizeDelta     = new Vector2(160f, 40f);
        btnRT.anchoredPosition = new Vector2(-20f, 20f);

        // Button colour transitions
        ColorBlock colours          = btn.colors;
        colours.normalColor         = ButtonNavy;
        colours.highlightedColor    = HexToColor("233A65");
        colours.pressedColor        = HexToColor("0F1C30");
        colours.selectedColor       = ButtonNavy;
        btn.colors                  = colours;

        // ── Button label (TMP) ───────────────────────────────────────────────
        GameObject btnLabelGO = CreateUIElement("ButtonText", btnGO);
        TextMeshProUGUI btnLabel = btnLabelGO.AddComponent<TextMeshProUGUI>();
        btnLabel.text       = "Reset Tour";
        btnLabel.fontSize   = 14f;
        btnLabel.color      = ButtonText;
        btnLabel.alignment  = TextAlignmentOptions.Center;
        btnLabel.fontStyle  = FontStyles.Bold;
        StretchFill(btnLabelGO.GetComponent<RectTransform>());

        return overlayGO;
    }

    // ── 3. Wire Reset button → WaypointManager.ResetTour() ──────────────────

    private static void TryWireResetButton(GameObject overlayGO)
    {
        WaypointManager manager =
            Object.FindObjectOfType<WaypointManager>();

        if (manager == null)
        {
            Debug.LogWarning(
                "[WaypointUISetup] No WaypointManager found in the scene. " +
                "Wire the Reset Tour button's OnClick manually after adding WaypointManager.");
            return;
        }

        Button btn = overlayGO.GetComponentInChildren<Button>();
        if (btn == null) return;

        // Remove any previous listeners so re-running the tool stays idempotent.
        btn.onClick.RemoveAllListeners();

        UnityEventTools.AddPersistentListener(
            btn.onClick,
            manager.ResetTour);

        EditorUtility.SetDirty(btn);
        Debug.Log("[WaypointUISetup] Reset Tour button wired to WaypointManager.ResetTour().");
    }

    // ── 4. EventSystem guard ─────────────────────────────────────────────────

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;

        GameObject esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();
        Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
        Debug.Log("[WaypointUISetup] EventSystem created.");
    }

    // ── Utility helpers ──────────────────────────────────────────────────────

    /// <summary>Creates a child GameObject with a RectTransform.</summary>
    private static GameObject CreateUIElement(string name, GameObject parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    /// <summary>Stretches a RectTransform to fill its parent.</summary>
    private static void StretchFill(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
    }

    private static Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out Color c)) return c;
        Debug.LogWarning("[WaypointUISetup] Could not parse hex: " + hex);
        return Color.magenta;
    }
}
