using UnityEngine;
using TMPro;

/// <summary>
/// Attach to a building GameObject. Shows an info panel when the player
/// (OVRCameraRig) comes within triggerDistance and keeps the panel
/// billboard-facing the player at all times.
/// </summary>
public class BuildingInfoTrigger : MonoBehaviour
{
    [Header("UI")]
    public GameObject infoPanel;

    [Header("Trigger")]
    public float triggerDistance = 8f;

    [Header("Building Info")]
    public string buildingName;
    [TextArea(2, 5)]
    public string buildingDescription;

    // Cached references
    Transform _playerTransform;
    TextMeshProUGUI _titleText;
    TextMeshProUGUI _descText;
    bool _playerFound;

    void Start()
    {
        // --- Locate player transform (OVRCameraRig or Main Camera fallback) ---
        _playerTransform = FindPlayerTransform();
        _playerFound = _playerTransform != null;

        // --- Cache TextMeshPro children ---
        if (infoPanel != null)
        {
            TextMeshProUGUI[] texts = infoPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI t in texts)
            {
                string tag = t.gameObject.name.ToLower();
                if (tag.Contains("title") || tag.Contains("name"))
                    _titleText = t;
                else if (tag.Contains("desc") || tag.Contains("body") || tag.Contains("info"))
                    _descText = t;
            }

            // Graceful fallback: assign by index if naming convention wasn't matched
            if (_titleText == null && texts.Length > 0) _titleText = texts[0];
            if (_descText  == null && texts.Length > 1) _descText  = texts[1];

            infoPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning(
                $"[BuildingInfoTrigger] ({gameObject.name}) 'infoPanel' is not assigned " +
                "in the Inspector. Drag a world-space Canvas or panel GameObject into that field.");
        }
    }

    void Update()
    {
        // Retry finding the player every frame until located
        if (!_playerFound)
        {
            _playerTransform = FindPlayerTransform();
            _playerFound = _playerTransform != null;
            if (!_playerFound) return;
        }

        // Guard: cached transform may have been destroyed (e.g. Main Camera replaced by OVRCameraRig)
        if (_playerTransform == null)
        {
            _playerFound = false;
            return;
        }

        if (infoPanel == null) return;

        float distance = Vector3.Distance(transform.position, _playerTransform.position);
        bool inRange = distance < triggerDistance;

        // --- Show / hide ---
        if (inRange && !infoPanel.activeSelf)
        {
            UpdatePanelText();
            infoPanel.SetActive(true);
        }
        else if (!inRange && infoPanel.activeSelf)
        {
            infoPanel.SetActive(false);
        }

        // --- Reposition + billboard ---
        // Move panel to whichever face of the building is closest to the player,
        // always at eye level (1.7m), so it never clips into the building.
        if (infoPanel.activeSelf)
        {
            Vector3 buildingToPlayer = _playerTransform.position - transform.position;
            buildingToPlayer.y = 0f;

            if (buildingToPlayer.sqrMagnitude > 0.001f)
            {
                Vector3 dir = buildingToPlayer.normalized;

                // Offset from building centre to its surface in the player's direction.
                // Use half the max horizontal extent + small gap so the panel clears the wall.
                float halfExtent = Mathf.Max(transform.localScale.x, transform.localScale.z) * 0.5f + 0.3f;

                infoPanel.transform.position = new Vector3(
                    transform.position.x + dir.x * halfExtent,
                    1.7f,
                    transform.position.z + dir.z * halfExtent
                );

                // Canvas readable side faces local -Z, so point +Z away from player
                infoPanel.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
            }
        }
    }

    void UpdatePanelText()
    {
        if (_titleText != null) _titleText.text = buildingName;
        if (_descText  != null) _descText.text  = buildingDescription;
    }

    // Finds OVRCameraRig first; falls back to Main Camera for desktop testing
    Transform FindPlayerTransform()
    {
        OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
        if (rig != null)
        {
            Debug.Log($"[BuildingInfoTrigger] ({gameObject.name}) Using OVRCameraRig.");
            return rig.centerEyeAnchor != null ? rig.centerEyeAnchor : rig.transform;
        }

        Camera main = Camera.main;
        if (main != null)
        {
            Debug.Log($"[BuildingInfoTrigger] ({gameObject.name}) OVRCameraRig not found — using Main Camera for desktop testing.");
            return main.transform;
        }

        Debug.LogWarning($"[BuildingInfoTrigger] ({gameObject.name}) No OVRCameraRig or Main Camera found in scene.");
        return null;
    }

    // Allow other scripts (e.g. a UI manager) to update content at runtime
    public void SetBuildingInfo(string newName, string newDescription)
    {
        buildingName        = newName;
        buildingDescription = newDescription;
        if (infoPanel != null && infoPanel.activeSelf)
            UpdatePanelText();
    }
}
