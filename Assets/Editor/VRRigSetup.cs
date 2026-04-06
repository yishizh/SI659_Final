using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// CampusTour > Setup VR Rig (OVRCameraRig)
///
/// Finds the OVRCameraRig prefab via the AssetDatabase, instantiates it,
/// sets OVRManager.trackingOriginType = FloorLevel, and removes any
/// standalone Main Camera that is not inside the rig hierarchy.
///
/// Compiles and runs whether or not the Meta XR SDK is installed.
/// All OVR type references are string-based so there is no compile-time
/// dependency on the Oculus assemblies.
/// </summary>
public static class VRRigSetup
{
    // ── OVRManager.TrackingOrigin enum values (from Meta XR SDK source) ──────
    // EyeLevel  = 0
    // FloorLevel = 1   ← we want this
    // Stage      = 2
    private const int TrackingOrigin_FloorLevel = 1;

    // ────────────────────────────────────────────────────────────────────────
    [MenuItem("CampusTour/Setup VR Rig (OVRCameraRig)")]
    public static void SetupVRRig()
    {
        // ── 1. Search project prefab database for OVRCameraRig ───────────────
        string[] guids = AssetDatabase.FindAssets("OVRCameraRig t:Prefab");

        if (guids.Length == 0)
        {
            Debug.LogWarning(
                "[VRRigSetup] OVRCameraRig prefab not found in this project.\n" +
                "To fix this:\n" +
                "  1. Open Window > Package Manager\n" +
                "  2. Search for 'Meta XR Core SDK' and install it, OR\n" +
                "  3. Import the Meta XR All-in-One SDK from the Asset Store.\n" +
                "Then re-run CampusTour > Setup VR Rig (OVRCameraRig).");
            return;
        }

        // Prefer an exact filename match; fall back to the first result.
        string prefabPath = null;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == "OVRCameraRig")
            {
                prefabPath = path;
                break;
            }
        }
        prefabPath ??= AssetDatabase.GUIDToAssetPath(guids[0]);

        // ── 2. Remove any existing OVRCameraRig in the scene ─────────────────
        // Use type-name check so this compiles without the OVR assemblies.
        foreach (MonoBehaviour mb in Object.FindObjectsOfType<MonoBehaviour>())
        {
            if (mb == null) continue;
            if (mb.GetType().Name == "OVRCameraRig")
            {
                Debug.Log("[VRRigSetup] Removing existing OVRCameraRig from scene.");
                Undo.DestroyObjectImmediate(mb.gameObject);
                break;
            }
        }

        // ── 3. Instantiate at origin ──────────────────────────────────────────
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[VRRigSetup] Failed to load prefab at path: {prefabPath}");
            return;
        }

        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(rig, "Create OVRCameraRig");
        rig.transform.position = Vector3.zero;

        // ── 4. Set OVRManager.trackingOriginType = FloorLevel ─────────────────
        // Walk all components on the rig and its children; match by type name
        // so there is no compile-time dependency on the Oculus assembly.
        bool managerConfigured = false;
        foreach (Component comp in rig.GetComponentsInChildren<Component>(includeInactive: true))
        {
            if (comp == null) continue;
            if (comp.GetType().Name != "OVRManager") continue;

            // Use SerializedObject so the value is properly recorded by Unity's
            // undo system and written back to the prefab instance.
            SerializedObject   so   = new SerializedObject(comp);
            SerializedProperty prop = so.FindProperty("trackingOriginType");

            if (prop != null)
            {
                prop.enumValueIndex = TrackingOrigin_FloorLevel;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(comp);
                Debug.Log("[VRRigSetup] OVRManager.trackingOriginType set to FloorLevel (1).");
                managerConfigured = true;
            }
            else
            {
                Debug.LogWarning(
                    "[VRRigSetup] Could not find 'trackingOriginType' property on OVRManager. " +
                    "The SDK version may use a different field name. Set it manually in the Inspector.");
            }
            break;
        }

        if (!managerConfigured)
            Debug.LogWarning("[VRRigSetup] OVRManager component not found on the instantiated rig.");

        // ── 5. Delete any standalone Main Camera outside the OVR hierarchy ────
        foreach (Camera cam in Object.FindObjectsOfType<Camera>())
        {
            if (cam.transform.IsChildOf(rig.transform)) continue;

            if (cam.CompareTag("MainCamera") || cam.name == "Main Camera")
            {
                Debug.Log($"[VRRigSetup] Removing standalone camera '{cam.name}'.");
                Undo.DestroyObjectImmediate(cam.gameObject);
            }
        }

        // ── 6. Add OVRNotFoundHelper if not already present ───────────────────
        if (Object.FindObjectOfType<OVRNotFoundHelper>() == null)
        {
            GameObject helperGO = new GameObject("OVRNotFoundHelper");
            Undo.RegisterCreatedObjectUndo(helperGO, "Create OVRNotFoundHelper");
            helperGO.AddComponent<OVRNotFoundHelper>();
        }

        Selection.activeGameObject = rig;
        Debug.Log($"[VRRigSetup] OVRCameraRig instantiated from: {prefabPath}");
    }

    // ────────────────────────────────────────────────────────────────────────
    [MenuItem("CampusTour/Setup Desktop Player")]
    public static void SetupDesktopPlayer()
    {
        // Remove stale instance
        GameObject stale = GameObject.Find("DesktopPlayer");
        if (stale != null)
        {
            Undo.DestroyObjectImmediate(stale);
            Debug.Log("[VRRigSetup] Removed existing DesktopPlayer.");
        }

        // ── Create DesktopPlayer root ────────────────────────────────────────
        GameObject player = new GameObject("DesktopPlayer");
        Undo.RegisterCreatedObjectUndo(player, "Create DesktopPlayer");
        player.transform.position = Vector3.zero;

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.radius = 0.3f;

        // ── Child camera at eye height ───────────────────────────────────────
        GameObject camGO = new GameObject("PlayerCamera");
        camGO.transform.SetParent(player.transform, false);
        camGO.transform.localPosition = new Vector3(0f, 1.7f, 0f);
        Undo.RegisterCreatedObjectUndo(camGO, "Create PlayerCamera");

        camGO.AddComponent<Camera>();
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        player.AddComponent<DesktopTestController>();

        // Remove any pre-existing standalone Main Camera
        foreach (Camera c in Object.FindObjectsOfType<Camera>())
        {
            if (c.gameObject == camGO) continue;
            if (c.name == "Main Camera")
            {
                Undo.DestroyObjectImmediate(c.gameObject);
                Debug.Log("[VRRigSetup] Removed old 'Main Camera'.");
            }
        }

        Selection.activeGameObject = player;
        Debug.Log("[VRRigSetup] DesktopPlayer created. Press Play and click the Game view to lock the cursor.");
    }
}
