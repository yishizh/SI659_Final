using UnityEngine;

[AddComponentMenu("CampusTour/Minimap Follow")]
public class MinimapFollow : MonoBehaviour
{
    private Transform _player;

    void LateUpdate()
    {
        // Retry every frame until player is found
        if (_player == null)
        {
            // Try by tag first
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                _player = playerObj.transform;
                Debug.Log("[MinimapFollow] Player found: " + playerObj.name);
                return;
            }

            // Fallback: find OVRCameraRig by type name
            foreach (MonoBehaviour mb in FindObjectsOfType<MonoBehaviour>())
            {
                if (mb.GetType().Name == "OVRCameraRig")
                {
                    _player = mb.transform;
                    Debug.Log("[MinimapFollow] OVRCameraRig found as fallback.");
                    return;
                }
            }
            return;
        }

        // Follow player X/Z only — keep Y fixed
        Vector3 pos = transform.position;
        pos.x = _player.position.x;
        pos.z = _player.position.z;
        transform.position = pos;
    }
}

