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
        private const string StylizedMaterialFolder = "Assets/_Game/Materials/StylizedGenerated";

        private static readonly Color[] ToyPalette =
        {
            new(1.00f, 0.43f, 0.60f, 1f),
            new(0.34f, 0.76f, 0.94f, 1f),
            new(1.00f, 0.80f, 0.35f, 1f),
            new(0.52f, 0.86f, 0.52f, 1f),
            new(0.75f, 0.56f, 0.91f, 1f),
            new(1.00f, 0.58f, 0.40f, 1f)
        };

        static ClawToonPresentation()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += ApplyToActivePrototypeScene;
        }

        [MenuItem("Claw3D/Apply Cartoon Rendering")]
        [MenuItem("Claw3D/Apply Stylized Rendering")]
        public static void ApplyToActivePrototypeScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != PrototypeSceneName) return;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null)
            {
                Debug.LogWarning("URP/Lit shader is not available yet.");
                return;
            }

            // Rebuild presentation meshes first so cabinet/claw recover their intended source colours.
            ClawCabinetPresentation.ApplyToActivePrototypeScene();
            ClawMechanismPresentation.ApplyToActivePrototypeScene();
            EnsureFolders();

            foreach (GameObject root in scene.GetRootGameObjects())
                ApplyRecursive(root.transform, litShader);

            // Bright, neutral arcade lighting. The previous experimental toon shader created
            // oversized hard bands and made the prize bed look broken, so this pass deliberately
            // uses clean URP lighting with pastel materials instead of full-screen cel bands.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.52f, 0.52f, 0.55f);
            RenderSettings.reflectionIntensity = 0.20f;

            Light key = FindLight("Key Light");
            if (key != null)
            {
                key.color = new Color(1.0f, 0.98f, 0.95f);
                key.intensity = 1.05f;
                key.shadows = LightShadows.Soft;
                key.shadowStrength = 0.32f;
            }

            Light fill = FindLight("Fill Light");
            if (fill != null)
            {
                fill.color = new Color(0.90f, 0.94f, 1.0f);
                fill.intensity = 1.10f;
                fill.shadows = LightShadows.None;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.IsValid() && scene.name == PrototypeSceneName)
                EditorApplication.delayCall += ApplyToActivePrototypeScene;
        }

        private static void ApplyRecursive(Transform node, Shader litShader)
        {
            Renderer renderer = node.GetComponent<Renderer>();
            if (renderer != null && renderer.enabled)
                ApplyRenderer(renderer, litShader);

            for (int i = 0; i < node.childCount; i++)
                ApplyRecursive(node.GetChild(i), litShader);
        }

        private static void ApplyRenderer(Renderer renderer, Shader litShader)
        {
            Material[] sourceMaterials = renderer.sharedMaterials;
            if (sourceMaterials == null || sourceMaterials.Length == 0) return;

            string objectName = renderer.gameObject.name;
            bool isFloor = objectName.StartsWith("Floor", StringComparison.Ordinal);
            bool isToy = TryGetToyIndex(renderer.transform, out int toyIndex);
            bool isMetal = IsMetalPart(renderer.transform, objectName);

            Material[] replacements = new Material[sourceMaterials.Length];
            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                Color baseColor = ResolveBaseColor(renderer, sourceMaterials[i], isToy, toyIndex);
                replacements[i] = GetOrCreateStylizedMaterial(litShader, baseColor, isToy, isFloor, isMetal);
            }

            renderer.sharedMaterials = replacements;
            renderer.SetPropertyBlock(null);

            // A uniform prize bed reads much better from this camera. Keep object shadows on toys,
            // but do not let cabinet posts paint giant diagonal shapes over the entire floor.
            if (isFloor)
            {
                renderer.receiveShadows = false;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            else if (isToy)
            {
                renderer.receiveShadows = true;
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }

            EditorUtility.SetDirty(renderer);
        }

        private static Color ResolveBaseColor(Renderer renderer, Material sourceMaterial, bool isToy, int toyIndex)
        {
            if (isToy)
                return ToyPalette[(toyIndex - 1) % ToyPalette.Length];

            string n = renderer.gameObject.name;

            // Prize-bed surfaces should be quiet, warm-neutral colours so the plushies carry the scene.
            if (n.StartsWith("Floor", StringComparison.Ordinal)) return new Color(0.72f, 0.71f, 0.74f, 1f);
            if (n.StartsWith("Chute", StringComparison.Ordinal)) return new Color(0.50f, 0.49f, 0.54f, 1f);
            if (n == "RailX") return new Color(0.58f, 0.60f, 0.64f, 1f);
            if (n == "Cable") return new Color(0.10f, 0.10f, 0.12f, 1f);

            if (sourceMaterial != null && sourceMaterial.HasProperty("_BaseColor"))
                return sourceMaterial.GetColor("_BaseColor");
            if (sourceMaterial != null && sourceMaterial.HasProperty("_Color"))
                return sourceMaterial.GetColor("_Color");

            return new Color(0.75f, 0.75f, 0.78f, 1f);
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

        private static bool IsMetalPart(Transform transform, string objectName)
        {
            if (objectName.StartsWith("Blade_", StringComparison.Ordinal) ||
                objectName.Contains("Ring", StringComparison.Ordinal) ||
                objectName.Contains("Collar", StringComparison.Ordinal) ||
                objectName.Contains("Carriage", StringComparison.Ordinal) ||
                objectName.Contains("Roller", StringComparison.Ordinal) ||
                objectName.Contains("Hinge", StringComparison.Ordinal) ||
                objectName == "RailX")
                return true;

            Transform cursor = transform;
            while (cursor != null)
            {
                if (cursor.name == "ClawVisual_v1" || cursor.name == "TrolleyVisual_v1" || cursor.name == "FingerVisual_v1")
                    return true;
                cursor = cursor.parent;
            }

            return false;
        }

        private static Material GetOrCreateStylizedMaterial(
            Shader shader,
            Color baseColor,
            bool isToy,
            bool isFloor,
            bool isMetal)
        {
            Color32 c = baseColor;
            string kind = isToy ? "toy" : isFloor ? "floor" : isMetal ? "metal" : "mat";
            string key = $"{c.r:X2}{c.g:X2}{c.b:X2}_{kind}";
            string path = $"{StylizedMaterialFolder}/Stylized_{key}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(shader) { name = $"Stylized_{key}" };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", new Color(baseColor.r, baseColor.g, baseColor.b, 1f));
            material.SetFloat("_Metallic", isMetal ? 0.58f : 0f);
            material.SetFloat("_Smoothness", isToy ? 0.18f : isFloor ? 0.08f : isMetal ? 0.56f : 0.22f);
            if (material.HasProperty("_SpecularHighlights")) material.SetFloat("_SpecularHighlights", isToy ? 0f : 1f);
            if (material.HasProperty("_EnvironmentReflections")) material.SetFloat("_EnvironmentReflections", isMetal ? 1f : 0f);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game")) AssetDatabase.CreateFolder("Assets", "_Game");
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/_Game", "Materials");
            if (!AssetDatabase.IsValidFolder(StylizedMaterialFolder)) AssetDatabase.CreateFolder(MaterialFolder, "StylizedGenerated");
        }

        private static Light FindLight(string name)
        {
            GameObject go = GameObject.Find(name);
            return go == null ? null : go.GetComponent<Light>();
        }
    }
}
#endif
