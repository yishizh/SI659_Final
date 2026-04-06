using UnityEngine;

/// <summary>
/// Animates a waypoint sphere with three simultaneous effects:
///   • Scale pulse  — sine-wave breathe between 1.2 and 1.8 (base 1.5 ± 0.3)
///   • Y-axis bob   — gentle float up and down (± 0.3 units at 1 Hz)
///   • Y-axis spin  — continuous 45 °/s rotation
///
/// The animation only runs while the GameObject is active, so
/// WaypointManager's SetActive(false) calls automatically freeze
/// inactive waypoints at their rest state without extra logic here.
///
/// Each waypoint gets a randomised phase offset so they do not all
/// pulse in perfect unison when multiple are briefly visible.
/// </summary>
public class WaypointPulse : MonoBehaviour
{
    [Header("Scale Pulse")]
    public float baseScale   = 1.5f;
    public float pulseRange  = 0.3f;
    public float pulseSpeed  = 1.5f;

    [Header("Bob")]
    public float bobRange    = 0.3f;
    public float bobSpeed    = 1.0f;

    [Header("Rotation")]
    public float spinSpeed   = 45f;   // degrees per second

    // ── runtime state ────────────────────────────────────────────────────────
    private Vector3 originPosition;   // world position set at spawn time
    private float   phaseOffset;      // randomise so waypoints feel independent

    // ────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        originPosition = transform.position;

        // Spread the phase so all 5 waypoints don't breathe in sync.
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void OnEnable()
    {
        // Snap to rest state whenever the waypoint is (re-)activated so there
        // is no visual "jump" from wherever the animation left off.
        transform.localScale = Vector3.one * baseScale;
        transform.position   = originPosition;
    }

    private void Update()
    {
        float t = Time.time;

        // ── 1. Scale pulse (uniform — keeps the sphere spherical) ─────────
        float pulseSin  = Mathf.Sin(t * pulseSpeed * Mathf.PI * 2f + phaseOffset);
        float newScale  = baseScale + pulseSin * pulseRange;
        transform.localScale = Vector3.one * newScale;

        // ── 2. Y-axis bob ────────────────────────────────────────────────
        float bobSin    = Mathf.Sin(t * bobSpeed  * Mathf.PI * 2f + phaseOffset);
        Vector3 pos     = originPosition;
        pos.y          += bobSin * bobRange;
        transform.position = pos;

        // ── 3. Y-axis spin ───────────────────────────────────────────────
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }
}
