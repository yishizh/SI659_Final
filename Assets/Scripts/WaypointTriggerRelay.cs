using UnityEngine;

/// <summary>
/// Attach one instance of this component to each waypoint sphere.
/// It detects when the player enters the trigger collider and notifies
/// WaypointManager which waypoint was reached.
///
/// Player detection accepts any of:
///   • GameObject tagged "Player"
///   • GameObject (or any ancestor) that carries a CharacterController
///   • GameObject (or any ancestor) that carries an OVRCameraRig
///     (checked via string-based lookup so the script compiles with
///      or without the Meta/Oculus SDK installed)
///
/// Setup:
///   1. Add this component to each waypoint sphere.
///   2. Drag the WaypointManager GameObject into the Manager field.
///   3. Ensure the sphere's SphereCollider has Is Trigger = true.
///   4. Ensure the player has a Rigidbody (Is Kinematic = true) so
///      Unity fires OnTriggerEnter.
/// </summary>
public class WaypointTriggerRelay : MonoBehaviour
{
    [Tooltip("Drag the GameObject that holds WaypointManager here.")]
    public WaypointManager manager;

    // ────────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (manager == null)
        {
            Debug.LogWarning($"[WaypointTriggerRelay] '{name}' has no WaypointManager assigned.");
            return;
        }

        if (IsPlayer(other))
            manager.OnWaypointReached(gameObject);
    }

    // ── Player detection ─────────────────────────────────────────────────────

    private static bool IsPlayer(Collider other)
    {
        // 1. Unity tag — works for both OVRCameraRig and CharacterController rigs
        //    as long as the player root is tagged "Player".
        if (other.CompareTag("Player"))
            return true;

        // 2. CharacterController anywhere in the entering object's hierarchy
        //    (the collider might be on a child bone, not the root).
        if (HasComponentInHierarchy<CharacterController>(other.transform))
            return true;

        // 3. OVRCameraRig anywhere in the hierarchy — string-based lookup so
        //    this compiles without the Meta/Oculus SDK present in the project.
        if (HasOVRCameraRigInHierarchy(other.transform))
            return true;

        return false;
    }

    /// <summary>
    /// Walks up the transform hierarchy looking for a component of type T.
    /// Searches the entering transform AND all of its parents.
    /// </summary>
    private static bool HasComponentInHierarchy<T>(Transform start) where T : Component
    {
        Transform t = start;
        while (t != null)
        {
            if (t.GetComponent<T>() != null)
                return true;
            t = t.parent;
        }
        return false;
    }

    /// <summary>
    /// Same walk, but uses the string overload of GetComponent so the code
    /// compiles whether or not the Oculus/Meta SDK is installed.
    /// </summary>
    private static bool HasOVRCameraRigInHierarchy(Transform start)
    {
        Transform t = start;
        while (t != null)
        {
            if (t.GetComponent("OVRCameraRig") != null)
                return true;
            t = t.parent;
        }
        return false;
    }
}
