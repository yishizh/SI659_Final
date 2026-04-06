using UnityEngine;
using UnityEditor;

public class CampusSetup
{
    [MenuItem("CampusTour/Setup Campus Scene")]
    public static void SetupCampus()
    {
        CreateGround();

        GameObject campus = new GameObject("Campus");
        Undo.RegisterCreatedObjectUndo(campus, "Create Campus");

        CreateBuilding("Building_Library", new Vector3(10, 3, 15),   new Vector3(8, 6, 10),  "#C0392B", campus);
        CreateBuilding("Building_Science", new Vector3(-15, 4, 10),  new Vector3(10, 8, 8),  "#2980B9", campus);
        CreateBuilding("Building_Union",   new Vector3(0, 2.5f, -20), new Vector3(12, 5, 10), "#F39C12", campus);
        CreateBuilding("Building_Admin",   new Vector3(-5, 7, 5),    new Vector3(5, 14, 5),  "#7F8C8D", campus);
        CreateBuilding("Building_Gym",     new Vector3(20, 3, -10),  new Vector3(12, 6, 15), "#27AE60", campus);

        Selection.activeGameObject = campus;
        Debug.Log("[CampusTour] Campus scene setup complete.");
    }

    static void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(10, 1, 10);

        Material mat = CreateMaterial("Mat_Ground", "#4A7C3F");
        ground.GetComponent<Renderer>().material = mat;
    }

    static void CreateBuilding(string name, Vector3 position, Vector3 scale, string hexColor, GameObject parent)
    {
        GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(building, "Create " + name);
        building.name = name;
        building.transform.position = position;
        building.transform.localScale = scale;
        building.transform.SetParent(parent.transform, true);

        // Ensure a BoxCollider is present (Cube primitives include one by default,
        // but we add explicitly in case the primitive type ever changes).
        if (building.GetComponent<BoxCollider>() == null)
            building.AddComponent<BoxCollider>();

        Material mat = CreateMaterial("Mat_" + name, hexColor);
        building.GetComponent<Renderer>().material = mat;
    }

    static Material CreateMaterial(string matName, string hexColor)
    {
        // Resolve shader before constructing Material — Shader.Find returns null if not found
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard")
                     ?? Shader.Find("Diffuse");

        if (shader == null)
        {
            Debug.LogError("[CampusTour] Could not find a usable shader. Material will be magenta.");
            shader = Shader.Find("Hidden/InternalErrorShader");
        }

        Material mat = new Material(shader);

        if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
            mat.color = color;
        else
            Debug.LogWarning($"[CampusTour] Could not parse color '{hexColor}' for {matName}.");

        // Save the material as an asset so it persists after play mode
        string dir = "Assets/Materials/CampusTour";
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Materials", "CampusTour");

        string path = $"{dir}/{matName}.mat";
        AssetDatabase.CreateAsset(mat, path);

        return mat;
    }

    [MenuItem("CampusTour/Clear Campus Scene")]
    public static void ClearCampus()
    {
        GameObject campus = GameObject.Find("Campus");
        if (campus != null)
        {
            Undo.DestroyObjectImmediate(campus);
            Debug.Log("[CampusTour] 'Campus' GameObject removed.");
        }
        else
        {
            Debug.Log("[CampusTour] No 'Campus' GameObject found in scene.");
        }

        GameObject ground = GameObject.Find("Ground");
        if (ground != null)
        {
            Undo.DestroyObjectImmediate(ground);
            Debug.Log("[CampusTour] 'Ground' GameObject removed.");
        }
    }
}
