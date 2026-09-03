#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Claw3D.Editor
{
    [InitializeOnLoad]
    public static class ClawMechanismPresentation
    {
        private const string PrototypeSceneName = "ClawPrototype";
        private const string MaterialFolder = "Assets/_Game/Materials";
        private const string HubVisualRoot = "ClawVisual_v1";
        private const string TrolleyVisualRoot = "TrolleyVisual_v1";
        private const string FingerVisualRoot = "FingerVisual_v1";

        static ClawMechanismPresentation()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += ApplyToActivePrototypeScene;
        }

        [MenuItem("Claw3D/Apply Claw Mechanism Presentation")]
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
            GameObject hub = GameObject.Find("ClawHub");
            GameObject trolley = GameObject.Find("PhysicsTrolley");
            if (hub == null || trolley == null) return;

            EnsureMaterialFolder();
            Material chrome = GetOrCreateMaterial("ClawChrome.mat", new Color(0.72f, 0.76f, 0.82f), 0.82f, 0.88f);
            Material darkMetal = GetOrCreateMaterial("ClawDarkMetal.mat", new Color(0.09f, 0.10f, 0.13f), 0.72f, 0.64f);
            Material accent = GetOrCreateMaterial("ClawAccent.mat", new Color(0.95f, 0.20f, 0.48f), 0.18f, 0.62f);
            Material rubber = GetOrCreateMaterial("ClawRubber.mat", new Color(0.08f, 0.08f, 0.09f), 0.02f, 0.28f);

            BuildTrolleyVisual(trolley, chrome, darkMetal, accent);
            BuildHubVisual(hub, chrome, darkMetal, accent);

            for (int i = 1; i <= 3; i++)
            {
                GameObject finger = GameObject.Find($"Finger_{i}");
                if (finger != null)
                    BuildFingerVisual(finger, chrome, darkMetal, rubber);
            }

            SceneView.RepaintAll();
        }

        private static void BuildTrolleyVisual(GameObject trolley, Material chrome, Material darkMetal, Material accent)
        {
            Renderer original = trolley.GetComponent<Renderer>();
            if (original != null) original.enabled = false;

            Transform old = trolley.transform.Find(TrolleyVisualRoot);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            GameObject root = new(TrolleyVisualRoot);
            root.transform.SetParent(trolley.transform, false);

            VisualCube("CarriageBody", root.transform, new Vector3(0f, 0f, 0f), new Vector3(0.11f, 0.045f, 0.085f), darkMetal);
            VisualCube("CarriageTop", root.transform, new Vector3(0f, 0.026f, 0f), new Vector3(0.085f, 0.018f, 0.070f), chrome);
            VisualCube("CarriageAccent", root.transform, new Vector3(0f, -0.008f, 0.046f), new Vector3(0.068f, 0.020f, 0.010f), accent);

            VisualCylinder("RollerL", root.transform, new Vector3(-0.047f, 0.008f, 0f), new Vector3(0.015f, 0.010f, 0.015f), chrome, Quaternion.Euler(0f, 0f, 90f));
            VisualCylinder("RollerR", root.transform, new Vector3(0.047f, 0.008f, 0f), new Vector3(0.015f, 0.010f, 0.015f), chrome, Quaternion.Euler(0f, 0f, 90f));
        }

        private static void BuildHubVisual(GameObject hub, Material chrome, Material darkMetal, Material accent)
        {
            Renderer original = hub.GetComponent<Renderer>();
            if (original != null) original.enabled = false;

            Transform old = hub.transform.Find(HubVisualRoot);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            GameObject root = new(HubVisualRoot);
            root.transform.SetParent(hub.transform, false);

            // Flattened dome + lower ring reads much closer to a real arcade claw head
            // while the original sphere collider remains untouched underneath.
            VisualSphere("Dome", root.transform, new Vector3(0f, 0.006f, 0f), new Vector3(0.105f, 0.072f, 0.105f), chrome);
            VisualCylinder("LowerRing", root.transform, new Vector3(0f, -0.031f, 0f), new Vector3(0.054f, 0.010f, 0.054f), darkMetal, Quaternion.identity);
            VisualCylinder("CableCollar", root.transform, new Vector3(0f, 0.050f, 0f), new Vector3(0.024f, 0.022f, 0.024f), darkMetal, Quaternion.identity);
            VisualCylinder("AccentBand", root.transform, new Vector3(0f, -0.018f, 0f), new Vector3(0.056f, 0.006f, 0.056f), accent, Quaternion.identity);
        }

        private static void BuildFingerVisual(GameObject finger, Material chrome, Material darkMetal, Material rubber)
        {
            Transform old = finger.transform.Find(FingerVisualRoot);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            GameObject root = new(FingerVisualRoot);
            root.transform.SetParent(finger.transform, false);

            // Keep the physics capsules, but hide their gray-box renderers.
            // Presentation capsules copy those exact transforms so visuals stay glued to physics.
            for (int segmentIndex = 1; segmentIndex <= 3; segmentIndex++)
            {
                Transform source = finger.transform.Find($"Segment_{segmentIndex}");
                if (source == null) continue;

                Renderer sourceRenderer = source.GetComponent<Renderer>();
                if (sourceRenderer != null) sourceRenderer.enabled = false;

                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = $"Blade_{segmentIndex}";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = source.localPosition;
                visual.transform.localRotation = source.localRotation;
                visual.transform.localScale = new Vector3(
                    source.localScale.x * 0.78f,
                    source.localScale.y * 0.985f,
                    source.localScale.z * 0.78f);
                RemoveCollider(visual);
                visual.GetComponent<Renderer>().sharedMaterial = chrome;

                Vector3 lowerEnd = source.localPosition + source.localRotation * Vector3.up * source.localScale.y;
                float jointSize = segmentIndex == 3 ? 0.013f : 0.017f;
                VisualSphere($"Joint_{segmentIndex}", root.transform, lowerEnd, Vector3.one * jointSize, darkMetal);

                if (segmentIndex == 3)
                {
                    GameObject pad = VisualSphere("GripPad", root.transform, lowerEnd, new Vector3(0.024f, 0.014f, 0.030f), rubber);
                    pad.transform.localRotation = source.localRotation;
                }
            }

            // Horizontal hinge pin at the root hides the obvious procedural joint seam.
            VisualCylinder(
                "HingePin",
                root.transform,
                Vector3.zero,
                new Vector3(0.014f, 0.021f, 0.014f),
                darkMetal,
                Quaternion.Euler(0f, 0f, 90f));
        }

        private static GameObject VisualCube(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SetupVisual(go, name, parent, localPosition, localScale, Quaternion.identity, material);
            return go;
        }

        private static GameObject VisualCylinder(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, Quaternion localRotation)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            SetupVisual(go, name, parent, localPosition, localScale, localRotation, material);
            return go;
        }

        private static GameObject VisualSphere(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            SetupVisual(go, name, parent, localPosition, localScale, Quaternion.identity, material);
            return go;
        }

        private static void SetupVisual(
            GameObject go,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale;
            RemoveCollider(go);
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }

        private static void RemoveCollider(GameObject go)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
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
    }
}
#endif
