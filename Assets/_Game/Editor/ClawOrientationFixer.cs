#if UNITY_EDITOR
using Claw3D.Claw;
using Claw3D.Physics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Claw3D.Editor
{
    [InitializeOnLoad]
    public static class ClawOrientationFixer
    {
        private const string PrototypeSceneName = "ClawPrototype";
        private const string ConfigPath = "Assets/_Game/Config/ClawPhysicsConfig.asset";

        static ClawOrientationFixer()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorApplication.delayCall += ApplyToActivePrototypeScene;
        }

        [MenuItem("Claw3D/Fix Claw Orientation")]
        public static void ApplyToActivePrototypeScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != PrototypeSceneName) return;
            Apply();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.IsValid() && scene.name == PrototypeSceneName) Apply();
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            if (scene.name == PrototypeSceneName || path.EndsWith("/ClawPrototype.unity")) Apply();
        }

        private static void Apply()
        {
            ClawPhysicsConfig config = AssetDatabase.LoadAssetAtPath<ClawPhysicsConfig>(ConfigPath);
            if (config == null) return;

            // Existing assets may still contain the old orientation and shallow drop depth.
            config.openAngleDegrees = -48.7f;
            config.closedAngleDegrees = 0f;
            config.bottomY = 0.36f;
            EditorUtility.SetDirty(config);

            for (int i = 1; i <= config.fingerCount; i++)
            {
                GameObject fingerObject = GameObject.Find($"Finger_{i}");
                if (fingerObject == null) continue;

                RebuildSegmentTransforms(fingerObject.transform, config);

                ClawFinger finger = fingerObject.GetComponent<ClawFinger>();
                if (finger != null) finger.Configure(config);
            }

            // Rebuild the decorative claw skin after the physics segment transforms move.
            ClawMechanismPresentation.ApplyToActivePrototypeScene();
            SceneView.RepaintAll();
        }

        private static void RebuildSegmentTransforms(Transform fingerRoot, ClawPhysicsConfig config)
        {
            float[] lengths =
            {
                config.fingerSegmentLengths.x,
                config.fingerSegmentLengths.y,
                config.fingerSegmentLengths.z
            };
            float[] curves =
            {
                config.fingerSegmentCurvesRadians.x,
                config.fingerSegmentCurvesRadians.y,
                config.fingerSegmentCurvesRadians.z
            };

            Vector3 cursor = Vector3.zero;
            for (int s = 0; s < 3; s++)
            {
                Transform segment = fingerRoot.Find($"Segment_{s + 1}");
                if (segment == null) continue;

                // Local -Z is toward the claw center for every radially mounted finger.
                // Positive rotation around local X bends the hanging segment inward.
                float degrees = curves[s] * Mathf.Rad2Deg;
                Vector3 direction = Quaternion.AngleAxis(degrees, Vector3.right) * Vector3.down;
                Vector3 end = cursor + direction * lengths[s];

                segment.localPosition = (cursor + end) * 0.5f;
                segment.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
                cursor = end;
                EditorUtility.SetDirty(segment);
            }
        }
    }
}
#endif
