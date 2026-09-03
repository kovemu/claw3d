#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Claw3D.Editor
{
    [InitializeOnLoad]
    public static class ClawToonPresentation
    {
        private const string PrototypeSceneName = "ClawPrototype";
        private const string ToonMaterialPath = "Assets/_Game/Materials/ClawToonBase.mat";

        static ClawToonPresentation()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorApplication.delayCall += ApplyToActivePrototypeScene;
        }

        [MenuItem("Claw3D/Apply Cartoon Rendering")]
        public static void ApplyToActivePrototypeScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != PrototypeSceneName) return;

            Shader toonShader = Shader.Find("Claw3D/Toon");
            if (toonShader == null)
            {
                Debug.LogWarning("Claw3D/Toon shader is not imported yet. Try Apply Cartoon Rendering again after Unity finishes compiling shaders.");
                return;
            }

            Material toonMaterial = GetOrCreateToonMaterial(toonShader);
            if (toonMaterial == null) return;

            foreach (GameObject root in scene.GetRootGameObjects())
                ApplyRecursive(root.transform, toonMaterial);

            // Flat ambient light keeps the stepped shading readable and prevents the
            // simple prototype geometry from looking like glossy default URP primitives.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.19f, 0.27f);
            RenderSettings.reflectionIntensity = 0.25f;

            Light key = FindLight("Key Light");
            if (key != null)
            {
                key.intensity = 1.15f;
                key.shadows = LightShadows.Soft;
            }

            Light fill = FindLight("Fill Light");
            if (fill != null) fill.intensity = 0.8f;

            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.IsValid() && scene.name == PrototypeSceneName)
                EditorApplication.delayCall += ApplyToActivePrototypeScene;
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            if (scene.name == PrototypeSceneName || path.EndsWith("/ClawPrototype.unity"))
                ApplyToActivePrototypeScene();
        }

        private static void ApplyRecursive(Transform node, Material toonMaterial)
        {
            Renderer renderer = node.GetComponent<Renderer>();
            if (renderer != null && renderer.enabled)
                ApplyRenderer(renderer, toonMaterial);

            for (int i = 0; i < node.childCount; i++)
                ApplyRecursive(node.GetChild(i), toonMaterial);
        }

        private static void ApplyRenderer(Renderer renderer, Material toonMaterial)
        {
            Material[] sourceMaterials = renderer.sharedMaterials;
            if (sourceMaterials == null || sourceMaterials.Length == 0) return;

            bool alreadyToon = true;
            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                if (sourceMaterials[i] != toonMaterial)
                {
                    alreadyToon = false;
                    break;
                }
            }
            if (alreadyToon) return;

            Color[] sourceColors = new Color[sourceMaterials.Length];
            for (int i = 0; i < sourceMaterials.Length; i++)
                sourceColors[i] = ReadMaterialColor(sourceMaterials[i]);

            Material[] replacements = new Material[sourceMaterials.Length];
            for (int i = 0; i < replacements.Length; i++) replacements[i] = toonMaterial;
            renderer.sharedMaterials = replacements;

            for (int i = 0; i < sourceColors.Length; i++)
            {
                Color baseColor = sourceColors[i];
                Color shadow = new(
                    Mathf.Clamp01(baseColor.r * 0.43f),
                    Mathf.Clamp01(baseColor.g * 0.43f),
                    Mathf.Clamp01(baseColor.b * 0.50f),
                    baseColor.a);
                Color rim = Color.Lerp(baseColor, Color.white, 0.58f);

                MaterialPropertyBlock block = new();
                block.SetColor("_BaseColor", baseColor);
                block.SetColor("_ShadowColor", shadow);
                block.SetColor("_RimColor", rim);
                block.SetColor("_OutlineColor", new Color(0.045f, 0.035f, 0.065f, 1f));
                block.SetFloat("_Steps", 3f);
                block.SetFloat("_RimPower", 3.6f);
                block.SetFloat("_OutlineWidth", GetOutlineWidth(renderer));
                renderer.SetPropertyBlock(block, i);
            }
        }

        private static float GetOutlineWidth(Renderer renderer)
        {
            string n = renderer.gameObject.name;
            if (n.StartsWith("Blade_") || n == "Cable") return 0.0012f;
            if (n.Contains("Toy") || n == "Body" || n.StartsWith("Ear")) return 0.0032f;
            return 0.0023f;
        }

        private static Color ReadMaterialColor(Material material)
        {
            if (material == null) return Color.white;
            if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
            if (material.HasProperty("_Color")) return material.GetColor("_Color");
            return Color.white;
        }

        private static Material GetOrCreateToonMaterial(Shader shader)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ToonMaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "ClawToonBase"
                };
                AssetDatabase.CreateAsset(material, ToonMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_ShadowColor", new Color(0.34f, 0.30f, 0.40f, 1f));
            material.SetColor("_RimColor", new Color(1f, 0.90f, 0.97f, 1f));
            material.SetColor("_OutlineColor", new Color(0.045f, 0.035f, 0.065f, 1f));
            material.SetFloat("_Steps", 3f);
            material.SetFloat("_RimPower", 3.6f);
            material.SetFloat("_OutlineWidth", 0.0023f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Light FindLight(string name)
        {
            GameObject go = GameObject.Find(name);
            return go == null ? null : go.GetComponent<Light>();
        }
    }
}
#endif
