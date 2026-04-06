using UnityEditor;
using UnityEngine;

public class WaypointSetup
{
    private struct WaypointData
    {
        public string name;
        public Vector3 position;

        public WaypointData(string name, Vector3 position)
        {
            this.name = name;
            this.position = position;
        }
    }

    [MenuItem("CampusTour/Create Waypoints")]
    public static void CreateWaypoints()
    {
        WaypointData[] waypoints = new WaypointData[]
        {
            new WaypointData("Waypoint_1", new Vector3(  0f, 0f,   0f)),  // Start/Entrance
            new WaypointData("Waypoint_2", new Vector3( 10f, 0f,   8f)),  // Near Library
            new WaypointData("Waypoint_3", new Vector3(-10f, 0f,   5f)),  // Near Science Hall
            new WaypointData("Waypoint_4", new Vector3(  0f, 0f, -15f)),  // Near Student Union
            new WaypointData("Waypoint_5", new Vector3( 18f, 0f,  -8f)),  // Near Gym
        };

        // Create the glowing yellow material once and share it across all waypoints
        Material waypointMaterial = new Material(Shader.Find("Standard"));
        Color glowColor = HexToColor("F1C40F");
        waypointMaterial.color = glowColor;
        waypointMaterial.SetColor("_EmissionColor", glowColor * 1.5f);
        waypointMaterial.EnableKeyword("_EMISSION");
        waypointMaterial.name = "WaypointGlowMaterial";

        // Create the WaypointSystem parent object
        GameObject waypointSystem = new GameObject("WaypointSystem");
        Undo.RegisterCreatedObjectUndo(waypointSystem, "Create WaypointSystem");

        foreach (WaypointData data in waypoints)
        {
            GameObject waypoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            waypoint.name = data.name;
            waypoint.transform.position = data.position;
            waypoint.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            waypoint.transform.SetParent(waypointSystem.transform);

            // Apply glowing yellow material
            waypoint.GetComponent<Renderer>().material = waypointMaterial;

            // Configure the Sphere Collider as a trigger with radius 3.
            // CreatePrimitive adds a SphereCollider with radius 0.5 in local space;
            // scaling to (1.5, 1.5, 1.5) makes the effective world radius 0.75,
            // so we override it to 3 here (world-space equivalent via localScale).
            SphereCollider col = waypoint.GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 3f / 1.5f; // 2f — accounts for the (1.5,1.5,1.5) scale

            // Add Point Light
            Light pointLight = waypoint.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = HexToColor("F1C40F");
            pointLight.intensity = 2f;
            pointLight.range = 8f;

            // Add pulse / bob / spin animation
            WaypointPulse pulse = waypoint.AddComponent<WaypointPulse>();
            pulse.baseScale  = 1.5f;
            pulse.pulseRange = 0.3f;
            pulse.pulseSpeed = 1.5f;
            pulse.bobRange   = 0.3f;
            pulse.bobSpeed   = 1.0f;
            pulse.spinSpeed  = 45f;

            Undo.RegisterCreatedObjectUndo(waypoint, "Create " + data.name);
        }

        // Select the parent in the hierarchy for immediate visibility
        Selection.activeGameObject = waypointSystem;

        Debug.Log("[WaypointSetup] Created 5 waypoints under WaypointSystem (WaypointPulse attached to each).");
    }

    private static Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
            return color;

        Debug.LogWarning("[WaypointSetup] Failed to parse hex color: " + hex);
        return Color.yellow;
    }
}
