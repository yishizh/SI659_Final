using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class InfoPanelSetup
{
    struct BuildingData
    {
        public string objectName;
        public string panelName;
        public string displayName;
        public string description;

        public BuildingData(string obj, string panel, string name, string desc)
        {
            objectName  = obj;
            panelName   = panel;
            displayName = name;
            description = desc;
        }
    }

    static readonly BuildingData[] Buildings =
    {
        new BuildingData("Building_Library", "InfoPanel_Library", "Library",      "UMSI Collection & Study Spaces"),
        new BuildingData("Building_Science", "InfoPanel_Science", "Science Hall",  "Research & Laboratories"),
        new BuildingData("Building_Union",   "InfoPanel_Union",   "Student Union", "Dining & Student Life"),
        new BuildingData("Building_Admin",   "InfoPanel_Admin",   "Admin Tower",   "Offices & Registration"),
        new BuildingData("Building_Gym",     "InfoPanel_Gym",     "Gymnasium",     "Sports & Recreation"),
    };

    // -------------------------------------------------------------------------
    [MenuItem("CampusTour/Setup Info Panels")]
    public static void SetupInfoPanels()
    {
        int created = 0;

        foreach (BuildingData data in Buildings)
        {
            GameObject building = GameObject.Find(data.objectName);
            if (building == null)
            {
                Debug.LogWarning(
                    $"[InfoPanelSetup] '{data.objectName}' not found. " +
                    "Run CampusTour > Setup Campus Scene first, then re-run this.");
                continue;
            }

            // Remove any stale panel from a previous run
            GameObject stale = GameObject.Find(data.panelName);
            if (stale != null)
                Undo.DestroyObjectImmediate(stale);

            // Place panel at eye level (1.7m) on the side of the building facing the origin
            Vector3 toOrigin     = (Vector3.zero - building.transform.position).normalized;
            float   halfDepth    = building.transform.localScale.z * 0.5f;
            Vector3 panelPos     = building.transform.position
                                 + toOrigin * (halfDepth + 0.1f);  // just in front of the face
            panelPos.y           = 1.7f;                           // eye level
            GameObject canvas = CreateInfoCanvas(data.panelName, panelPos);

            // Attach BuildingInfoTrigger to the building
            BuildingInfoTrigger trigger = building.GetComponent<BuildingInfoTrigger>();
            if (trigger == null)
                trigger = Undo.AddComponent<BuildingInfoTrigger>(building);

            trigger.infoPanel           = canvas;
            trigger.buildingName        = data.displayName;
            trigger.buildingDescription = data.description;
            trigger.triggerDistance     = 8f;

            EditorUtility.SetDirty(building);
            created++;
        }

        Debug.Log($"[InfoPanelSetup] Done — {created}/5 info panels created.");
    }

    // -------------------------------------------------------------------------
    static GameObject CreateInfoCanvas(string panelName, Vector3 worldPosition)
    {
        // ── Root Canvas ──────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject(panelName);
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create " + panelName);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta   = new Vector2(200f, 120f);
        canvasRect.localScale  = new Vector3(0.01f, 0.01f, 0.01f);
        canvasGO.transform.position = worldPosition;

        // ── Dark semi-transparent background ─────────────────────────────────
        GameObject bgGO = new GameObject("Background");
        Undo.RegisterCreatedObjectUndo(bgGO, "Create Background");
        bgGO.transform.SetParent(canvasGO.transform, false);

        Image bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);

        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // ── Title (building name) ─────────────────────────────────────────────
        GameObject titleGO = new GameObject("Title");
        Undo.RegisterCreatedObjectUndo(titleGO, "Create Title");
        titleGO.transform.SetParent(canvasGO.transform, false);

        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "Building Name";
        titleTMP.fontSize  = 14f;
        titleTMP.color     = Color.white;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.fontStyle = FontStyles.Bold;

        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.52f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(10f,  6f);
        titleRect.offsetMax = new Vector2(-10f, -6f);

        // ── Description ───────────────────────────────────────────────────────
        GameObject descGO = new GameObject("Description");
        Undo.RegisterCreatedObjectUndo(descGO, "Create Description");
        descGO.transform.SetParent(canvasGO.transform, false);

        TextMeshProUGUI descTMP = descGO.AddComponent<TextMeshProUGUI>();
        descTMP.text      = "Building description.";
        descTMP.fontSize  = 11f;
        descTMP.color     = new Color(0.88f, 0.88f, 0.88f, 1f);
        descTMP.alignment = TextAlignmentOptions.Center;

        RectTransform descRect = descGO.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0f, 0f);
        descRect.anchorMax = new Vector2(1f, 0.52f);
        descRect.offsetMin = new Vector2(10f,  6f);
        descRect.offsetMax = new Vector2(-10f, -6f);

        return canvasGO;
    }

    // -------------------------------------------------------------------------
    [MenuItem("CampusTour/Clear Info Panels")]
    public static void ClearInfoPanels()
    {
        string[] names = { "InfoPanel_Library", "InfoPanel_Science",
                           "InfoPanel_Union",   "InfoPanel_Admin", "InfoPanel_Gym" };
        int removed = 0;
        foreach (string n in names)
        {
            GameObject go = GameObject.Find(n);
            if (go != null) { Undo.DestroyObjectImmediate(go); removed++; }
        }
        Debug.Log($"[InfoPanelSetup] Cleared {removed} info panel(s).");
    }
}
