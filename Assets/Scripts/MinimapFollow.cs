using UnityEngine;

/// <summary>
/// Attached to MinimapAnchor. Keeps the anchor (and its child MinimapCamera)
/// centred on the Player's X/Z position every frame so the minimap scrolls
/// with the player.
/// </summary>
[AddComponentMenu("CampusTour/Minimap Follow")]
public class MinimapFollow : MonoBehaviour
{
    private Transform _player;

    void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("[MinimapFollow] No GameObject tagged 'Player' found. " +
                             "Minimap will not follow until one is present.");
        }
    }

    void LateUpdate()
    {
        if (_player == null)
        {
            // Retry each frame in case the player spawns after Start.
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                _player = playerObj.transform;
            return;
        }

        Vector3 pos = transform.position;
        pos.x = _player.position.x;
        pos.z = _player.position.z;
        // Y is intentionally unchanged: the anchor stays at its original height
        // so the camera (child at localPosition.y = 50) never drifts vertically.
        transform.position = pos;
    }
}
