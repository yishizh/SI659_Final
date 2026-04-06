using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to MinimapCanvas alongside MinimapTeleport.
///
/// Each Update it mirrors WaypointManager state onto the minimap:
///   - Shows a yellow dot only while its 3D sphere is active (current target).
///   - Converts the sphere's world X/Z into a minimap UI position via the
///     MinimapCamera orthographic projection and the RT pixel dimensions.
///   - Pulses the visible dot's scale (0.8 – 1.2) with a sine wave so the
///     player can find the next target at a glance.
/// </summary>
[AddComponentMenu("CampusTour/Minimap Waypoint Sync")]
public class MinimapWaypointSync : MonoBehaviour
{
    // ── Data ──────────────────────────────────────────────────────────────────

    [System.Serializable]
    public class MinimapDot
    {
        [Tooltip("The 3D waypoint sphere in the scene.")]
        public GameObject waypointObject;

        [Tooltip("The yellow dot RectTransform on the minimap (inside MinimapMask).")]
        public RectTransform dotUI;
    }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Waypoint / Dot Pairs")]
    public List<MinimapDot> dots = new List<MinimapDot>();

    [Header("Minimap Camera")]
    [Tooltip("Auto-found by name 'MinimapCamera' if left empty.")]
    public Camera minimapCamera;

    [Header("Render Texture")]
    [Tooltip("Width = Height of MinimapRT (256 by default).")]
    public int rtSize = 256;

    [Header("Pulse")]
    [Tooltip("Sine-wave frequency for the active-dot scale pulse.")]
    public float pulseSpeed = 2f;
    [Tooltip("Minimum scale factor during pulse.")]
    public float pulseMin   = 0.8f;
    [Tooltip("Maximum scale factor during pulse.")]
    public float pulseMax   = 1.2f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        if (minimapCamera != null) return;

        GameObject camGO = GameObject.Find("MinimapCamera");
        if (camGO != null)
            minimapCamera = camGO.GetComponent<Camera>();

        if (minimapCamera == null)
            Debug.LogWarning("[MinimapWaypointSync] MinimapCamera not found in scene. " +
                             "Assign it in the Inspector.");
    }

    void Update()
    {
        if (minimapCamera == null) return;

        // ── Projection constants (recalculated each frame so they survive
        //    runtime changes to ortho size, e.g. if you zoom the map) ─────────

        float orthoSize = minimapCamera.orthographicSize;   // half-height in world units
        float aspect    = minimapCamera.aspect;             // = 1 for a 256×256 RT

        // Total world extent covered by the RT
        float worldH = orthoSize * 2f;
        float worldW = worldH * aspect;

        // RT pixels per world unit on each axis
        float pxPerWorldX = rtSize / worldW;
        float pxPerWorldZ = rtSize / worldH;   // Z maps to screen-Y (cam Euler 90,0,0)

        // Half the RT size — used to scale RT-pixel offsets to UI-pixel offsets
        float rtHalf = rtSize * 0.5f;

        // Camera world position = map centre (MinimapAnchor follows the player)
        Vector3 camPos = minimapCamera.transform.position;

        // Pulse value for the active dot: maps sin [-1,1] → [pulseMin, pulseMax]
        float pulseScale = Mathf.Lerp(pulseMin, pulseMax,
                                      (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);

        // ── Per-dot update ────────────────────────────────────────────────────

        for (int i = 0; i < dots.Count; i++)
        {
            MinimapDot d = dots[i];
            if (d == null || d.waypointObject == null || d.dotUI == null) continue;

            bool active = d.waypointObject.activeInHierarchy;

            // Show / hide
            d.dotUI.gameObject.SetActive(active);

            if (!active)
            {
                // Reset scale so the dot looks correct if re-activated later
                d.dotUI.localScale = Vector3.one;
                continue;
            }

            // ── Step 1: world offset from the camera centre ───────────────────
            Vector3 wpos = d.waypointObject.transform.position;
            float dx = wpos.x - camPos.x;   // + right  on minimap
            float dz = wpos.z - camPos.z;   // + up     on minimap (world Z = screen Y)

            // ── Step 2: world offset → RT pixel offset from RT centre ─────────
            float rtOffsetX = dx * pxPerWorldX;
            float rtOffsetZ = dz * pxPerWorldZ;

            // ── Step 3: RT pixel offset → UI pixel offset ─────────────────────
            // The RawImage fills its parent container (MinimapMask).
            // Derive the UI scale from the container's actual rect so the script
            // works regardless of the panel size chosen in MinimapUISetup.
            RectTransform container = d.dotUI.parent as RectTransform;
            float uiHalfX = (container != null) ? container.rect.width  * 0.5f : rtHalf;
            float uiHalfY = (container != null) ? container.rect.height * 0.5f : rtHalf;

            d.dotUI.anchoredPosition = new Vector2(
                rtOffsetX * (uiHalfX / rtHalf),
                rtOffsetZ * (uiHalfY / rtHalf)
            );

            // ── Step 4: pulse the active (current target) dot ─────────────────
            d.dotUI.localScale = new Vector3(pulseScale, pulseScale, 1f);
        }
    }
}
