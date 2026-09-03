#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Claw3D.Editor
{
    [InitializeOnLoad]
    public static class ClawCabinetPresentation
    {
        private const string PrototypeSceneName = "ClawPrototype";
        private const string RootName = "CabinetPresentation_v1";
        private const string MaterialFolder = "Assets/_Game/Materials";

        static ClawCabinetPresentation()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += ApplyToActivePrototypeScene;
        }

        [MenuItem("Claw3D/Apply Cabinet Presentation")]
        public static void ApplyToActivePrototypeScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != PrototypeSceneName) return;
            Apply();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.IsValid() && scene.name == PrototypeSceneName)
                Apply();
        }

        private static void Apply()
        {
            RemoveOldPrototypeFrame();

            GameObject existing = GameObject.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing);

            EnsureMaterialFolder();
            Material shell = GetOrCreateMaterial("CabinetShell.mat", new Color(0.92f, 0.16f, 0.44f), 0.05f, 0.42f);
            Material trim = GetOrCreateMaterial("CabinetTrim.mat", new Color(1.00f, 0.50f, 0.68f), 0.10f, 0.60f);
            Material dark = GetOrCreateMaterial("CabinetDark.mat", new Color(0.07f, 0.06f, 0.10f), 0.00f, 0.25f);
            Material metal = GetOrCreateMaterial("CabinetMetal.mat", new Color(0.58f, 0.62f, 0.70f), 0.65f, 0.72f);
            Material glow = GetOrCreateMaterial("CabinetGlow.mat", new Color(1.00f, 0.78f, 0.90f), 0.00f, 0.75f);

            GameObject root = new(RootName);

            const float hx = 0.455f;
            const float hzFront = 0.455f;
            const float topY = 1.11f;

            // Four clean cabinet posts. Front posts are deliberately thicker because
            // the game is composed from straight ahead, not a three-quarter view.
            VisualCube("FrontPost_L", root.transform, new Vector3(-hx, 0.55f, hzFront), new Vector3(0.055f, 1.10f, 0.055f), shell);
            VisualCube("FrontPost_R", root.transform, new Vector3(hx, 0.55f, hzFront), new Vector3(0.055f, 1.10f, 0.055f), shell);
            VisualCube("BackPost_L", root.transform, new Vector3(-hx, 0.55f, -0.455f), new Vector3(0.035f, 1.10f, 0.035f), trim);
            VisualCube("BackPost_R", root.transform, new Vector3(hx, 0.55f, -0.455f), new Vector3(0.035f, 1.10f, 0.035f), trim);

            VisualCube("TopFrontBeam", root.transform, new Vector3(0f, 1.02f, hzFront), new Vector3(0.91f, 0.075f, 0.055f), shell);
            VisualCube("BottomFrontBeam", root.transform, new Vector3(0f, 0.015f, hzFront), new Vector3(0.91f, 0.055f, 0.055f), shell);
            VisualCube("TopCap", root.transform, new Vector3(0f, topY, 0f), new Vector3(0.96f, 0.11f, 0.96f), shell);

            // Marquee gives the cabinet a readable silhouette even before final art.
            VisualCube("Marquee", root.transform, new Vector3(0f, 1.055f, 0.485f), new Vector3(0.78f, 0.12f, 0.045f), dark);
            VisualCube("MarqueeInset", root.transform, new Vector3(0f, 1.055f, 0.512f), new Vector3(0.65f, 0.065f, 0.012f), glow);

            // Lower body. Pure presentation geometry: physics floor/chute remain untouched.
            VisualCube("LowerCabinet", root.transform, new Vector3(0f, -0.19f, 0.16f), new Vector3(0.92f, 0.38f, 0.60f), shell);
            VisualCube("LowerFront", root.transform, new Vector3(0f, -0.19f, 0.485f), new Vector3(0.84f, 0.31f, 0.055f), trim);
            VisualCube("PrizeWindow", root.transform, new Vector3(-0.25f, -0.19f, 0.520f), new Vector3(0.26f, 0.15f, 0.018f), dark);
            VisualCube("PrizeWindowInner", root.transform, new Vector3(-0.25f, -0.19f, 0.532f), new Vector3(0.205f, 0.102f, 0.010f), new ColorMaterial(dark, new Color(0.025f, 0.022f, 0.035f)));

            // Sloped control deck, centered and readable from the fixed front camera.
            GameObject deck = VisualCube("ControlDeck", root.transform, new Vector3(0f, 0.015f, 0.545f), new Vector3(0.72f, 0.10f, 0.20f), dark);
            deck.transform.rotation = Quaternion.Euler(-12f, 0f, 0f);

            GameObject stickBase = VisualCylinder("StickBase", root.transform, new Vector3(0.18f, 0.078f, 0.575f), new Vector3(0.040f, 0.012f, 0.040f), metal);
            stickBase.transform.rotation = Quaternion.Euler(-12f, 0f, 0f);
            VisualCylinder("Stick", root.transform, new Vector3(0.18f, 0.135f, 0.565f), new Vector3(0.012f, 0.055f, 0.012f), metal);
            VisualSphere("StickBall", root.transform, new Vector3(0.18f, 0.195f, 0.552f), 0.050f, shell);

            GameObject button = VisualCylinder("DropButton", root.transform, new Vector3(-0.17f, 0.090f, 0.575f), new Vector3(0.050f, 0.018f, 0.050f), glow);
            button.transform.rotation = Quaternion.Euler(-12f, 0f, 0f);

            // Small chrome rail housing makes the top mechanism visually connected.
            VisualCube("RailHousing", root.transform, new Vector3(0f, 0.965f, 0f), new Vector3(0.68f, 0.035f, 0.07f), metal);

            EditorUtility.SetDirty(root);
            SceneView.RepaintAll();
        }

        private static void RemoveOldPrototypeFrame()
        {
            string[] oldNames = { "FrameFL", "FrameFR", "FrameBL", "FrameBR", "TopFront", "TopBack" };
            foreach (string name in oldNames)
            {
                GameObject go = GameObject.Find(name);
                if (go != null) Object.DestroyImmediate(go);
            }
        }

        private static GameObject VisualCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SetupVisual(go, name, parent, position, scale, material);
            return go;
        }

        private static GameObject VisualCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            SetupVisual(go, name, parent, position, scale, material);
            return go;
        }

        private static GameObject VisualSphere(string name, Transform parent, Vector3 position, float diameter, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            SetupVisual(go, name, parent, position, Vector3.one * diameter, material);
            return go;
        }

        private static void SetupVisual(GameObject go, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.transform.localScale = scale;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }

        private static void EnsureMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game")) AssetDatabase.CreateFolder("Assets", "_Game");
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/_Game", "Materials");
        }

        private static Material GetOrCreateMaterial(string fileName, Color color, float metallic, float smoothness)
        {
            string path = $"{MaterialFolder}/{fileName}";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material new ColorMaterial(Material source, Color color)
        {
            Material copy = new(source.shader);
            copy.color = color;
            if (copy.HasProperty("_BaseColor")) copy.SetColor("_BaseColor", color);
            return copy;
        }
    }
}
#endif
