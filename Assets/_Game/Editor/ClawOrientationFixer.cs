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
            if (scene.IsValid() && scene.name == PrototypeSceneName)
                EditorApplication.delayCall += ApplyToActivePrototypeScene;
        }

        private static void Apply()
        {
            ClawPhysicsConfig config = AssetDatabase.LoadAssetAtPath<ClawPhysicsConfig>(ConfigPath);
            if (config == null) return;

            for (int i = 1; i <= config.fingerCount; i++)
            {
                GameObject fingerObject = GameObject.Find($"Finger_{i}");
                if (fingerObject == null) continue;

                HingeJoint hinge = fingerObject.GetComponent<HingeJoint>();
                if (hinge != null)
                {
                    hinge.axis = -Vector3.right;
                    hinge.useSpring = false;
                    hinge.useMotor = false;
                    hinge.useLimits = true;
                    JointLimits limits = hinge.limits;
                    limits.min = config.fingerClosedAngleDegrees;
                    limits.max = config.fingerOpenAngleDegrees;
                    limits.bounciness = 0f;
                    limits.contactDistance = config.fingerLimitContactDistance;
                    hinge.limits = limits;
                    EditorUtility.SetDirty(hinge);
                }

                ClawFinger finger = fingerObject.GetComponent<ClawFinger>();
                if (finger != null)
                    finger.Configure(config);
            }

            SceneView.RepaintAll();
        }
    }
}
#endif
