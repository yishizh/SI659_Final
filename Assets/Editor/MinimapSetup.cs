using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility: CampusTour ▶ Setup Minimap
///
/// Creates (or repairs) the minimap rig:
///   MinimapAnchor  (MinimapFollow component — follows Player X/Z)
///   └─ MinimapCamera  (orthographic, depth 1, renders to MinimapRT 256×256)
///
/// Run this once per scene. Re-running it is safe; existing objects are reused.
/// </summary>
public static class MinimapSetup
{
    private const string RT_PATH      = "Assets/MinimapRT.renderTexture";
    private const string MENU_ITEM    = "CampusTour/Setup Minimap";
    private const int    RT_SIZE      = 256;
    private const float  CAM_HEIGHT   = 50f;
    private const float  ORTHO_SIZE   = 40f;
    private const int    CAM_DEPTH    = 1;

    // Background colour #1A2C4E
    private static readonly Color BG_COLOR = new Color(0x1A / 255f, 0x2C / 255f, 0x4E / 255f, 1f);

    [MenuItem(MENU_ITEM)]
    public static void SetupMinimap()
    {
        // ── 1. RenderTexture ────────────────────────────────────────────────
        RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RT_PATH);
        if (rt == null)
        {
            rt = new RenderTexture(RT_SIZE, RT_SIZE, 16, RenderTextureFormat.ARGB32)
            {
                name            = "MinimapRT",
                antiAliasing    = 1,
                filterMode      = FilterMode.Bilinear,
                wrapMode        = TextureWrapMode.Clamp
            };
            rt.Create();
            AssetDatabase.CreateAsset(rt, RT_PATH);
            AssetDatabase.SaveAssets();
            Debug.Log($"[MinimapSetup] Created RenderTexture at {RT_PATH}");
        }
        else
        {
            Debug.Log($"[MinimapSetup] Reusing existing RenderTexture at {RT_PATH}");
        }

        // ── 2. MinimapAnchor (carries MinimapFollow) ────────────────────────
        GameObject anchor = GameObject.Find("MinimapAnchor");
        if (anchor == null)
        {
            anchor = new GameObject("MinimapAnchor");
            anchor.transform.position = Vector3.zero;
            Debug.Log("[MinimapSetup] Created MinimapAnchor.");
        }
        else
        {
            Debug.Log("[MinimapSetup] Reusing existing MinimapAnchor.");
        }

        if (anchor.GetComponent<MinimapFollow>() == null)
        {
            anchor.AddComponent<MinimapFollow>();
            Debug.Log("[MinimapSetup] Added MinimapFollow to MinimapAnchor.");
        }

        // ── 3. MinimapCamera ────────────────────────────────────────────────
        // Locate or create; always (re)parent to the anchor.
        GameObject camObj = GameObject.Find("MinimapCamera");
        if (camObj == null)
        {
            camObj = new GameObject("MinimapCamera");
            Debug.Log("[MinimapSetup] Created MinimapCamera.");
        }
        else
        {
            Debug.Log("[MinimapSetup] Reusing existing MinimapCamera.");
        }

        // Parent BEFORE setting local transform so values are in anchor space.
        camObj.transform.SetParent(anchor.transform, worldPositionStays: false);
        camObj.transform.localPosition = new Vector3(0f, CAM_HEIGHT, 0f);
        camObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // look straight down

        Camera cam = camObj.GetComponent<Camera>();
        if (cam == null)
            cam = camObj.AddComponent<Camera>();

        cam.orthographic     = true;
        cam.orthographicSize = ORTHO_SIZE;
        cam.cullingMask      = -1;                       // Everything
        cam.depth            = CAM_DEPTH;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = BG_COLOR;
        cam.targetTexture    = rt;

        // Disable audio listener on minimap cam to avoid the "two listeners" warning.
        AudioListener al = camObj.GetComponent<AudioListener>();
        if (al != null)
            Object.DestroyImmediate(al);

        // ── 4. Finalise ─────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[MinimapSetup] Done. Hierarchy:\n" +
                  "  MinimapAnchor  [MinimapFollow]\n" +
                  "  └─ MinimapCamera  [Camera → MinimapRT 256×256, ortho=40, depth=1]");
    }

    // Grey out the menu item when not in Edit mode (e.g. during Play).
    [MenuItem(MENU_ITEM, true)]
    private static bool ValidateSetupMinimap()
    {
        return !Application.isPlaying;
    }
}
