using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the VR campus tour waypoint sequence.
/// Works with OVRCameraRig (VR headset) and a standard
/// CharacterController (desktop keyboard testing).
///
/// Setup:
///   1. Attach this script to an empty GameObject (e.g. "WaypointManager").
///   2. Assign the 5 waypoint GameObjects to the Waypoints list (in order).
///   3. Assign WaypointInfoCanvas to the Info Panel field.
///   4. The waypoint spheres must have SphereColliders with Is Trigger = true.
///   5. The player object must have a Rigidbody (Is Kinematic = true) and be
///      tagged "Player" so OnTriggerEnter fires and WaypointTriggerRelay
///      identifies it correctly.
/// </summary>
public class WaypointManager : MonoBehaviour
{
    [Header("Waypoints")]
    public List<GameObject> waypoints = new List<GameObject>();

    [Header("UI")]
    public GameObject infoPanel;

    [Header("Labels")]
    public string[] waypointLabels = new string[]
    {
        "Welcome to Michigan! Start your tour here.",
        "Library \u2014 UMSI Collection & Study Spaces",
        "Science Hall \u2014 Research & Laboratories",
        "Student Union \u2014 Dining & Student Life",
        "Gymnasium \u2014 Sports & Recreation. Tour Complete!"
    };

    // ── runtime state ────────────────────────────────────────────────────────
    private int      currentIndex = 0;
    private bool     tourComplete = false;
    private TMP_Text mainLabelText;   // "WaypointLabel" TMP child
    private TMP_Text stepLabelText;   // "StepLabel"     TMP child

    // ────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        ConfigureInfoPanelCanvas();   // fix render mode + layout before caching text
        CacheTextComponents();
        ResetTour();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Canvas configuration
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Forces the info panel Canvas to Screen Space – Overlay and anchors all
    /// direct children (Background, WaypointLabel, StepLabel) to the bottom
    /// centre of the screen. Called once in Start() so it works regardless of
    /// how the canvas was created in the Editor.
    /// </summary>
    private void ConfigureInfoPanelCanvas()
    {
        if (infoPanel == null)
        {
            Debug.LogWarning("[WaypointManager] infoPanel is not assigned in the Inspector.");
            return;
        }

        // ── Render mode ──────────────────────────────────────────────────────
        Canvas canvas = infoPanel.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[WaypointManager] infoPanel has no Canvas component.");
            return;
        }

        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        // CanvasScaler should be set to Constant Pixel Size for overlay panels.
        CanvasScaler scaler = infoPanel.GetComponent<CanvasScaler>();
        if (scaler != null)
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        // ── Re-anchor children to bottom centre ──────────────────────────────
        // In Screen Space – Overlay the canvas fills the screen; children are
        // positioned relative to it via anchors. The World Space scale (0.01)
        // set by WaypointUISetup must also be reset to (1,1,1).
        infoPanel.transform.localScale = Vector3.one;

        foreach (Transform child in infoPanel.transform)
        {
            RectTransform rt = child.GetComponent<RectTransform>();
            if (rt == null) continue;

            // Anchor point: bottom centre of the screen canvas
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);

            switch (child.name)
            {
                case "Background":
                    rt.sizeDelta        = new Vector2(640f, 110f);
                    rt.anchoredPosition = new Vector2(0f,    10f);
                    break;

                case "WaypointLabel":
                    rt.sizeDelta        = new Vector2(600f,  65f);
                    rt.anchoredPosition = new Vector2(0f,    44f);
                    break;

                case "StepLabel":
                    rt.sizeDelta        = new Vector2(600f,  28f);
                    rt.anchoredPosition = new Vector2(0f,    14f);
                    break;
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Text component caching
    // ────────────────────────────────────────────────────────────────────────

    private void CacheTextComponents()
    {
        if (infoPanel == null) return;

        // includeInactive: true — panel is hidden at Start so children are
        // technically inactive; we still need to cache the references.
        TMP_Text[] texts = infoPanel.GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text t in texts)
        {
            if (t.gameObject.name == "WaypointLabel") mainLabelText = t;
            if (t.gameObject.name == "StepLabel")     stepLabelText = t;
        }

        if (mainLabelText == null)
            Debug.LogWarning(
                "[WaypointManager] No TMP_Text child named 'WaypointLabel' found in infoPanel. " +
                "Make sure WaypointUISetup created the canvas and the name matches.");

        if (stepLabelText == null)
            Debug.LogWarning(
                "[WaypointManager] No TMP_Text child named 'StepLabel' found in infoPanel.");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Trigger detection
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by WaypointTriggerRelay when the player enters a waypoint sphere.
    /// </summary>
    public void OnWaypointReached(GameObject waypointObject)
    {
        int index = waypoints.IndexOf(waypointObject);
        if (index < 0 || index != currentIndex)
            return;   // not the active waypoint — ignore stale calls

        HandleWaypointReached(index);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Core logic
    // ────────────────────────────────────────────────────────────────────────

    private void HandleWaypointReached(int index)
    {
        bool   isLast = (index == waypoints.Count - 1);
        string label  = GetLabel(index);

        Debug.Log($"[WaypointManager] Reached waypoint {index + 1}/{waypoints.Count}: {label}");

        string displayText = isLast ? "Tour Complete!  " + label : label;
        ShowInfoPanel(displayText, index + 1);

        SetWaypointVisible(index, false);

        if (isLast)
        {
            tourComplete = true;
            Debug.Log("[WaypointManager] Tour complete!");
        }
        else
        {
            currentIndex = index + 1;
            SetWaypointVisible(currentIndex, true);
            Debug.Log($"[WaypointManager] Next waypoint: {GetWaypointName(currentIndex)}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Public API
    // ────────────────────────────────────────────────────────────────────────

    public void ResetTour()
    {
        tourComplete = false;
        currentIndex = 0;

        for (int i = 0; i < waypoints.Count; i++)
            SetWaypointVisible(i, false);

        if (waypoints.Count > 0)
            SetWaypointVisible(0, true);

        HideInfoPanel();

        Debug.Log("[WaypointManager] Tour reset. Starting at Waypoint_1.");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────────

    private void ShowInfoPanel(string message, int stepNumber)
    {
        if (infoPanel == null) return;

        infoPanel.SetActive(true);

        if (mainLabelText != null)
            mainLabelText.text = message;

        if (stepLabelText != null)
            stepLabelText.text = $"Step {stepNumber} of {waypoints.Count}";
    }

    private void HideInfoPanel()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    private void SetWaypointVisible(int index, bool visible)
    {
        if (!IsValidIndex(index)) return;
        if (waypoints[index] != null)
            waypoints[index].SetActive(visible);
    }

    private string GetLabel(int index)
    {
        if (waypointLabels != null && index < waypointLabels.Length)
            return waypointLabels[index];
        return $"Waypoint {index + 1}";
    }

    private string GetWaypointName(int index) =>
        IsValidIndex(index) && waypoints[index] != null
            ? waypoints[index].name
            : $"Waypoint_{index + 1}";

    private bool IsValidIndex(int index) =>
        index >= 0 && index < waypoints.Count;
}
