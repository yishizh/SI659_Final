using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor utility: CampusTour ▶ Setup Minimap UI
///
/// Builds the complete minimap overlay in one click:
///
///   MinimapCanvas  (Screen Space – Overlay, sort 20)
///   └─ MinimapPanel  (200×200, top-right, navy #1A2C4E)
///       ├─ MinimapLabel  "CAMPUS MAP"  [TMP, above panel]
///       └─ MinimapMask   (192×192, circle sprite, Mask)
///           ├─ MinimapImage   RawImage → MinimapRT
///           ├─ PlayerDot      blue #3498DB, 10×10, centred
///           └─ WaypointDot_1 … WaypointDot_5
///               ├─ Dot        yellow #F1C40F, 8×8
///               └─ WPLabel   "1" … "5" [TMP]
///
/// MinimapPanel also receives MinimapWaypointDots (runtime component
/// that repositions/shows waypoint dots each frame).
///
/// Re-running the menu item destroys and recreates MinimapCanvas — safe.
/// </summary>
public static class MinimapUISetup
{
    const string MENU    = "CampusTour/Setup Minimap UI";
    const string RT_PATH = "Assets/MinimapRT.renderTexture";
    const string SP_PATH = "Assets/MinimapCircle.png";

    static readonly Color32 C_NAVY     = new Color32(0x1A, 0x2C, 0x4E, 220);
    static readonly Color32 C_PLAYER   = new Color32(0x34, 0x98, 0xDB, 255);  // #3498DB
    static readonly Color32 C_WAYPOINT = new Color32(0xF1, 0xC4, 0x0F, 255);  // #F1C40F

    // ── Menu ──────────────────────────────────────────────────────────────────

    [MenuItem(MENU)]
    public static void SetupMinimapUI()
    {
        Sprite        circle = GetOrCreateCircleSprite();
        RenderTexture rt     = AssetDatabase.LoadAssetAtPath<RenderTexture>(RT_PATH);

        if (rt == null)
            Debug.LogWarning($"[MinimapUISetup] MinimapRT not found at '{RT_PATH}'. " +
                              "Assign it manually to MinimapImage after setup.");

        // Replace any existing canvas
        GameObject old = GameObject.Find("MinimapCanvas");
        if (old != null)
        {
            Object.DestroyImmediate(old);
            Debug.Log("[MinimapUISetup] Replaced existing MinimapCanvas.");
        }

        // ── Canvas ────────────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("MinimapCanvas");

        Canvas canvas         = canvasGO.AddComponent<Canvas>();
        canvas.renderMode     = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder   = 20;

        CanvasScaler scaler           = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution    = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight     = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Panel (200×200, top-right corner, 20 px margin) ──────────────────
        RectTransform panel = MakeRect("MinimapPanel", canvasGO.transform);
        panel.anchorMin        = Vector2.one;
        panel.anchorMax        = Vector2.one;
        panel.pivot            = Vector2.one;
        panel.anchoredPosition = new Vector2(-20f, -20f);
        panel.sizeDelta        = new Vector2(200f, 200f);

        Image panelImg = panel.gameObject.AddComponent<Image>();
        panelImg.color = C_NAVY;

        // ── "CAMPUS MAP" label (4 px above panel top, stretches panel width) ──
        RectTransform labelRT   = MakeRect("MinimapLabel", panel);
        labelRT.anchorMin        = new Vector2(0f,   1f);
        labelRT.anchorMax        = new Vector2(1f,   1f);
        labelRT.pivot            = new Vector2(0.5f, 0f);
        labelRT.anchoredPosition = new Vector2(0f,   4f);
        labelRT.sizeDelta        = new Vector2(0f,   16f);  // 0 width = stretch

        TextMeshProUGUI labelTMP = labelRT.gameObject.AddComponent<TextMeshProUGUI>();
        labelTMP.text      = "CAMPUS MAP";
        labelTMP.fontSize  = 10f;
        labelTMP.fontStyle = FontStyles.Bold;
        labelTMP.alignment = TextAlignmentOptions.Center;
        labelTMP.color     = Color.white;

        // ── Circular mask (192×192 centred — 4 px navy border shows on panel) ─
        RectTransform maskRT   = MakeRect("MinimapMask", panel);
        maskRT.anchorMin        = new Vector2(0.5f, 0.5f);
        maskRT.anchorMax        = new Vector2(0.5f, 0.5f);
        maskRT.pivot            = new Vector2(0.5f, 0.5f);
        maskRT.anchoredPosition = Vector2.zero;
        maskRT.sizeDelta        = new Vector2(192f, 192f);

        Image maskImg   = maskRT.gameObject.AddComponent<Image>();
        maskImg.sprite  = circle;
        maskImg.color   = Color.white;
        maskImg.type    = Image.Type.Simple;

        Mask mask             = maskRT.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic  = false;  // hide circle graphic — use only for clipping

        // ── RawImage (fills mask, displays MinimapRT) ─────────────────────────
        RectTransform rawRT = MakeRect("MinimapImage", maskRT);
        rawRT.anchorMin = Vector2.zero;
        rawRT.anchorMax = Vector2.one;
        rawRT.offsetMin = Vector2.zero;
        rawRT.offsetMax = Vector2.zero;

        RawImage rawImg  = rawRT.gameObject.AddComponent<RawImage>();
        rawImg.texture   = rt;   // safely null if RT not found

        // ── Player dot (10×10 blue, always centred — camera follows player) ───
        RectTransform playerDotRT   = MakeRect("PlayerDot", maskRT);
        playerDotRT.anchorMin        = new Vector2(0.5f, 0.5f);
        playerDotRT.anchorMax        = new Vector2(0.5f, 0.5f);
        playerDotRT.pivot            = new Vector2(0.5f, 0.5f);
        playerDotRT.anchoredPosition = Vector2.zero;
        playerDotRT.sizeDelta        = new Vector2(10f, 10f);

        Image playerDotImg   = playerDotRT.gameObject.AddComponent<Image>();
        playerDotImg.sprite  = circle;
        playerDotImg.color   = C_PLAYER;

        // ── Waypoint dots 1-5 ─────────────────────────────────────────────────
        var dotRoots = new RectTransform[5];
        for (int i = 0; i < 5; i++)
            dotRoots[i] = CreateWaypointDot(maskRT, i + 1, circle, C_WAYPOINT);

        // ── MinimapWaypointDots runtime component (on panel) ─────────────────
        MinimapWaypointDots mwd = panel.gameObject.AddComponent<MinimapWaypointDots>();
        mwd.waypointDotRoots    = dotRoots;
        mwd.waypointManager     = Object.FindObjectOfType<WaypointManager>();

        if (mwd.waypointManager == null)
            Debug.LogWarning("[MinimapUISetup] WaypointManager not found. " +
                             "Assign it to MinimapPanel → MinimapWaypointDots.");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[MinimapUISetup] Done.\n" +
                  "  MinimapCanvas  (sort 20)\n" +
                  "  MinimapPanel   [navy bg + MinimapWaypointDots]\n" +
                  "    MinimapLabel [CAMPUS MAP]\n" +
                  "    MinimapMask  [Mask 192x192]\n" +
                  "      MinimapImage  [RawImage -> MinimapRT]\n" +
                  "      PlayerDot     [blue 10x10]\n" +
                  "      WaypointDot_1..5");
    }

    [MenuItem(MENU, true)]
    static bool ValidateSetupMinimapUI() => !Application.isPlaying;

    // ── Helpers ───────────────────────────────────────────────────────────────

    static RectTransform MakeRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static RectTransform CreateWaypointDot(Transform parent, int index,
                                           Sprite circle, Color32 color)
    {
        // Root GO: runtime script toggles active + sets anchoredPosition
        RectTransform rootRT   = MakeRect($"WaypointDot_{index}", parent);
        rootRT.anchorMin        = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax        = new Vector2(0.5f, 0.5f);
        rootRT.pivot            = new Vector2(0.5f, 0.5f);
        rootRT.anchoredPosition = Vector2.zero;   // updated at runtime
        rootRT.sizeDelta        = new Vector2(20f, 26f);
        rootRT.gameObject.SetActive(false);       // hidden until waypoint is active

        // Dot — 8×8 circle, bottom-centre of root
        RectTransform dotRT   = MakeRect("Dot", rootRT);
        dotRT.anchorMin        = new Vector2(0.5f, 0f);
        dotRT.anchorMax        = new Vector2(0.5f, 0f);
        dotRT.pivot            = new Vector2(0.5f, 0.5f);
        dotRT.anchoredPosition = new Vector2(0f, 5f);
        dotRT.sizeDelta        = new Vector2(8f, 8f);

        Image dotImg   = dotRT.gameObject.AddComponent<Image>();
        dotImg.sprite  = circle;
        dotImg.color   = color;

        // Number label — tiny TMP, top-centre of root
        RectTransform numRT   = MakeRect("WPLabel", rootRT);
        numRT.anchorMin        = new Vector2(0.5f, 1f);
        numRT.anchorMax        = new Vector2(0.5f, 1f);
        numRT.pivot            = new Vector2(0.5f, 1f);
        numRT.anchoredPosition = Vector2.zero;
        numRT.sizeDelta        = new Vector2(16f, 14f);

        TextMeshProUGUI numTMP = numRT.gameObject.AddComponent<TextMeshProUGUI>();
        numTMP.text      = index.ToString();
        numTMP.fontSize  = 9f;
        numTMP.fontStyle = FontStyles.Bold;
        numTMP.alignment = TextAlignmentOptions.Center;
        numTMP.color     = color;

        return rootRT;
    }

    /// <summary>
    /// Writes Assets/MinimapCircle.png (128×128 anti-aliased white circle on
    /// transparent background) and imports it as a Sprite. Reuses the asset
    /// if it already exists.
    /// </summary>
    static Sprite GetOrCreateCircleSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(SP_PATH);
        if (existing != null) return existing;

        const int size   = 128;
        var       tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float     centre = (size - 1) * 0.5f;
        float     radius = centre - 1f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx    = x - centre;
            float dy    = y - centre;
            float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 1f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
        tex.Apply();

        // Write PNG next to the other minimap assets
        File.WriteAllBytes(
            Path.Combine(Application.dataPath, "MinimapCircle.png"),
            tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(SP_PATH);

        TextureImporter imp = AssetImporter.GetAtPath(SP_PATH) as TextureImporter;
        if (imp != null)
        {
            imp.textureType         = TextureImporterType.Sprite;
            imp.spriteImportMode    = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled       = false;
            imp.SaveAndReimport();
        }

        Debug.Log($"[MinimapUISetup] Created circle sprite at '{SP_PATH}'.");
        return AssetDatabase.LoadAssetAtPath<Sprite>(SP_PATH);
    }
}
