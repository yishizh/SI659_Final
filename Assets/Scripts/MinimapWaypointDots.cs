using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Placed on MinimapPanel by MinimapUISetup.
/// Each LateUpdate it reads WaypointManager.waypoints, shows a dot only
/// for the currently-active waypoint, and repositions every dot to match
/// its world-space X/Z offset from the player — exactly as seen by the
/// top-down MinimapCamera (camera right = world +X, camera up = world +Z).
/// </summary>
[AddComponentMenu("CampusTour/Minimap Waypoint Dots")]
public class MinimapWaypointDots : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Camera / Panel Settings")]
    [Tooltip("Must match MinimapCamera.orthographicSize (default 40).")]
    public float orthographicSize = 40f;

    [Tooltip("Half the pixel width/height of MinimapMask (192 ÷ 2 = 96).")]
    public float panelHalfSize = 96f;

    [Header("Scene References")]
    [Tooltip("Auto-found via FindObjectOfType if left empty.")]
    public WaypointManager waypointManager;

    [Tooltip("Auto-found via 'Player' tag if left empty.")]
    public Transform playerTransform;

    [Header("Dot Roots (WaypointDot_1 … WaypointDot_5 inside MinimapMask)")]
    public RectTransform[] waypointDotRoots = new RectTransform[5];

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
                playerTransform = p.transform;
            else
                Debug.LogWarning("[MinimapWaypointDots] No GameObject tagged 'Player' found.");
        }

        if (waypointManager == null)
        {
            waypointManager = FindObjectOfType<WaypointManager>();
            if (waypointManager == null)
                Debug.LogWarning("[MinimapWaypointDots] WaypointManager not found in scene.");
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null || waypointManager == null) return;

        // pixels-per-world-unit: panelHalfSize maps to orthographicSize world units
        float scale = panelHalfSize / orthographicSize;

        var waypoints = waypointManager.waypoints;

        for (int i = 0; i < waypointDotRoots.Length; i++)
        {
            RectTransform dotRoot = waypointDotRoots[i];
            if (dotRoot == null) continue;

            bool hasSlot    = i < waypoints.Count && waypoints[i] != null;
            bool isActive   = hasSlot && waypoints[i].activeInHierarchy;

            dotRoot.gameObject.SetActive(isActive);

            if (!isActive) continue;

            // World-space offset from the player (the map is always player-centred)
            Vector3 wpos = waypoints[i].transform.position;
            float   dx   = wpos.x - playerTransform.position.x;
            float   dz   = wpos.z - playerTransform.position.z;

            // Camera Euler(90,0,0): screen-right = world +X, screen-up = world +Z
            dotRoot.anchoredPosition = new Vector2(dx * scale, dz * scale);
        }
    }
}
