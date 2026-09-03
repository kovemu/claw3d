#if UNITY_EDITOR
using System;
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
        private const string MaterialFolder = "Assets/_Game/Materials";
        private const string ToonMaterialFolder = "Assets/_Game/Materials/ToonGenerated";

        private static readonly Color[] ToyPalette =
        {
            new(1.00f, 0.42f, 0.62f, 1f),
            new(0.37f, 0.78f, 0.95f, 1f),
            new(1.00f, 0.82f, 0.40f, 1f),
            new(0.55f, 0.88f, 0.55f, 1f),
            new(0.78f, 0.57f, 0.92f, 1f),
            new(1.00f, 0.60f, 0.44f, 1f)
        };

        static ClawToonPresentation()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
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
                Debug.LogWarning("Claw3D/Toon shader is not imported yet. Apply Cartoon Rendering again after Unity finishes compiling shaders.");
                return;
            }

            // Rebuild presentation-only meshes first. This restores their intended source
            // colours even if a previous experimental toon pass replaced the materials.
            ClawCabinetPresentation.ApplyToActivePrototypeScene();
            ClawMechanismPresentation.ApplyToActivePrototypeScene();

            EnsureFolders();

            foreach (GameObject root in scene.GetRootGameObjects())
                ApplyRecursive(root.transform, toonShader);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.30f, 0.30f, 0.34f);
            RenderSettings.reflectionIntensity = 0.15f;

            Light key = FindLight("Key Light");
            if (key != null)
            {
                key.color = new Color(1.0f, 0.96f, 0.92f);
                key.intensity = 1.1f;
                key.shadows = LightShadows.Soft;
            }

            Light fill = FindLight("Fill Light");
            if (fill != null)
            {
                fill.color = new Color(0.88f, 0.92f, 1.0f);
                fill.intensity = 0.45f;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.IsValid() && scene.name == PrototypeSceneName)
                EditorApplication.delayCall += ApplyToActivePrototypeScene;
        }

        private static void ApplyRecursive(Transform node, Shader toonShader)
        {
            Renderer renderer = node.GetComponent<Renderer>();
            if (renderer != null && renderer.enabled)
                ApplyRenderer(renderer, toonShader);

            for (int i = 0; i < node.childCount; i++)
                ApplyRecursive(node.GetChild(i), toonShader);
        }

        private static void ApplyRenderer(Renderer renderer, Shader toonShader)
        {
            Material[] sourceMaterials = renderer.sharedMaterials;
            if (sourceMaterials == null || sourceMaterials.Length == 0) return;

            Material[] replacements = new Material[sourceMaterials.Length];
            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                Color baseColor = ResolveBaseColor(renderer, sourceMaterials[i], toonShader);
                string widthClass = GetWidthClass(renderer);
                float outlineWidth = GetOutlineWidth(renderer);
                replacements[i] = GetOrCreateToonMaterial(toonShader, baseColor, outlineWidth, widthClass);
            }

            renderer.sharedMaterials = replacements;
            renderer.SetPropertyBlock(null);
            EditorUtility.SetDirty(renderer);
        }

        private static Color ResolveBaseColor(Renderer renderer, Material sourceMaterial, Shader toonShader)
        {
            if (TryGetToyIndex(renderer.transform, out int toyIndex))
                return ToyPalette[(toyIndex - 1) % ToyPalette.Length];

            string n = renderer.gameObject.name;

            // Physics-bed materials were temporary scene materials in the original builder,
            // so a previous toon pass can erase their colour reference. Give them an explicit
            // stable arcade palette instead of falling back to white.
            if (n.StartsWith("Floor", StringComparison.Ordinal)) return new Color(0.33f, 0.37f, 0.49f, 1f);
            if (n.StartsWith("Chute", StringComparison.Ordinal)) return new Color(0.19f, 0.21f, 0.29f, 1f);
            if (n == "RailX") return new Color(0.46f, 0.50f, 0.58f, 1f);
            if (n == "Cable") return new Color(0.08f, 0.085f, 0.11f, 1f);

            if (sourceMaterial != null && sourceMaterial.shader != toonShader)
                return ReadMaterialColor(sourceMaterial);

            // Already-toon objects keep the colour stored in their generated material.
            if (sourceMaterial != null && sourceMaterial.HasProperty("_BaseColor"))
                return sourceMaterial.GetColor("_BaseColor");

            return Color.white;
        }

        private static bool TryGetToyIndex(Transform node, out int toyIndex)
        {
            Transform cursor = node;
            while (cursor != null)
            {
                if (cursor.name.StartsWith("Toy_", StringComparison.Ordinal) &&
                    int.TryParse(cursor.name.Substring(4), out toyIndex))
                    return true;
                cursor = cursor.parent;
            }

            toyIndex = 0;
            return false;
        }

        private static string GetWidthClass(Renderer renderer)
        {
            string n = renderer.gameObject.name;
            if (n.StartsWith("Blade_", StringComparison.Ordinal) || n == "Cable") return "thin";
            if (TryGetToyIndex(renderer.transform, out _)) return "toy";
            return "standard";
        }

        private static float GetOutlineWidth(Renderer renderer)
        {
            string n = renderer.gameObject.name;
            if (n.StartsWith("Blade_", StringComparison.Ordinal) || n == "Cable") return 0.0009f;
            if (TryGetToyIndex(renderer.transform, out _)) return 0.0018f;
            return 0.00125f;
        }

        private static Material GetOrCreateToonMaterial(Shader shader, Color baseColor, float outlineWidth, string widthClass)
        {
            Color32 c = baseColor;
            string key = $"{c.r:X2}{c.g:X2}{c.b:X2}_{widthClass}";
            string path = $"{ToonMaterialFolder}/Toon_{key}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(shader) { name = $"Toon_{key}" };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            Color shadow = new(
                Mathf.Clamp01(baseColor.r * 0.56f),
                Mathf.Clamp01(baseColor.g * 0.56f),
                Mathf.Clamp01(baseColor.b * 0.60f),
                1f);
            Color rim = Color.Lerp(baseColor, Color.white, 0.30f);

            material.SetColor("_BaseColor", new Color(baseColor.r, baseColor.g, baseColor.b, 1f));
            material.SetColor("_ShadowColor", shadow);
            material.SetColor("_RimColor", rim);
            material.SetColor("_OutlineColor", new Color(0.035f, 0.030f, 0.050f, 1f));
            material.SetFloat("_RimPower", 4.2f);
            material.SetFloat("_OutlineWidth", outlineWidth);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Color ReadMaterialColor(Material material)
        {
            if (material == null) return Color.white;
            if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
            if (material.HasProperty("_Color")) return material.GetColor("_Color");
            return Color.white;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game")) AssetDatabase.CreateFolder("Assets", "_Game");
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/_Game", "Materials");
            if (!AssetDatabase.IsValidFolder(ToonMaterialFolder)) AssetDatabase.CreateFolder(MaterialFolder, "ToonGenerated");
        }

        private static Light FindLight(string name)
        {
            GameObject go = GameObject.Find(name);
            return go == null ? null : go.GetComponent<Light>();
        }
    }
}
#endif
