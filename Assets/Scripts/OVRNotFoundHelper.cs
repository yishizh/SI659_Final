using UnityEngine;

/// <summary>
/// Displays a prominent amber warning overlay in the Game view when
/// OVRCameraRig is not present in the scene.
///
/// • Safe to leave in every scene — it hides itself when the rig IS found.
/// • Destroys itself in non-Editor (production) builds automatically.
/// • Compiles with or without the Meta XR SDK because all OVR type
///   references use string-based component lookup.
///
/// VRRigSetup.cs adds this to the scene automatically. You can also
/// drag it onto any persistent GameObject manually.
/// </summary>
public class OVRNotFoundHelper : MonoBehaviour
{
    // Cached state — evaluated once in Start so OnGUI stays allocation-free.
    private bool      _showWarning;
    private GUIStyle  _boxStyle;
    private GUIStyle  _labelStyle;
    private GUIStyle  _hintStyle;

    // ────────────────────────────────────────────────────────────────────────

    private void Start()
    {
#if !UNITY_EDITOR
        // This helper is only useful while developing inside the Editor.
        Destroy(gameObject);
        return;
#endif
        EvaluateState();
    }

    // Allows VRRigSetup (or any other script) to force a re-check after
    // instantiating or destroying the rig at runtime.
    public void Refresh() => EvaluateState();

    // ────────────────────────────────────────────────────────────────────────

    private void EvaluateState()
    {
        bool ovrPresent     = HasOVRCameraRigInScene();
        bool desktopMode    = GameObject.Find("DesktopPlayer") != null;

        // Show the warning only if OVR is absent AND we are not intentionally
        // running in keyboard / desktop-test mode.
        _showWarning = !ovrPresent && !desktopMode;

        if (!_showWarning)
        {
            // Nothing to display — disable rendering overhead entirely.
            enabled = false;
        }
    }

    private void OnGUI()
    {
        if (!_showWarning) return;

        BuildStylesIfNeeded();

        const float w = 500f;
        const float h = 160f;
        float x = (Screen.width  - w) * 0.5f;
        float y = 16f;

        // Dark semi-transparent backdrop
        GUI.Box(new Rect(x - 12f, y - 12f, w + 24f, h + 24f),
                GUIContent.none, _boxStyle);

        // Main warning text
        string warning =
            "OVRCameraRig NOT found in this scene\n\n" +
            "Run:   CampusTour  \u25b6  Setup VR Rig (OVRCameraRig)\n\n" +
            "If the menu item says the prefab is missing,\n" +
            "install the Meta XR Core SDK first via Package Manager.";

        GUI.Label(new Rect(x, y, w, h - 28f), warning, _labelStyle);

        // Smaller hint at the bottom of the box
        GUI.Label(new Rect(x, y + h - 28f, w, 24f),
                  "For keyboard testing use:  CampusTour  \u25b6  Setup Desktop Player",
                  _hintStyle);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Searches active MonoBehaviours by type name so the check compiles
    /// whether or not the Oculus/Meta SDK assemblies are present.
    /// </summary>
    private static bool HasOVRCameraRigInScene()
    {
        foreach (MonoBehaviour mb in FindObjectsOfType<MonoBehaviour>())
        {
            if (mb != null && mb.GetType().Name == "OVRCameraRig")
                return true;
        }
        return false;
    }

    private void BuildStylesIfNeeded()
    {
        if (_boxStyle != null) return;   // built once, reused every frame

        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeSolidTex(new Color(0.05f, 0.05f, 0.05f, 0.88f)) }
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 17,
            fontStyle = FontStyle.Bold,
            wordWrap  = true,
            alignment = TextAnchor.UpperLeft,
            normal    = { textColor = new Color(1f, 0.84f, 0.18f) }  // amber
        };

        _hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 12,
            fontStyle = FontStyle.Italic,
            wordWrap  = true,
            alignment = TextAnchor.UpperLeft,
            normal    = { textColor = new Color(0.75f, 0.75f, 0.75f) }  // light gray
        };
    }

    /// <summary>Creates a 2×2 solid-colour Texture2D for GUI backgrounds.</summary>
    private static Texture2D MakeSolidTex(Color color)
    {
        var tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { color, color, color, color });
        tex.Apply();
        return tex;
    }
}
