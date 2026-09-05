#if UNITY_EDITOR
using System.Linq;
using Claw3D.Claw;
using Claw3D.Physics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Claw3D.Editor
{
    [InitializeOnLoad]
    public static class ClawReferencePhysicsMigrator
    {
        private const string PrototypeSceneName = "ClawPrototype";
        private const string ConfigPath = "Assets/_Game/Config/ClawPhysicsConfig.asset";

        static ClawReferencePhysicsMigrator()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += ApplyToActivePrototypeScene;
        }

        [MenuItem("Claw3D/Apply Claw Machine Sim Physics Rig")]
        public static void ApplyToActivePrototypeScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != PrototypeSceneName) return;

            ClawPhysicsConfig config = AssetDatabase.LoadAssetAtPath<ClawPhysicsConfig>(ConfigPath);
            GameObject trolley = GameObject.Find("PhysicsTrolley");
            GameObject hub = GameObject.Find("ClawHub");
            if (config == null || trolley == null || hub == null) return;

            Rigidbody trolleyBody = trolley.GetComponent<Rigidbody>();
            Rigidbody hubBody = hub.GetComponent<Rigidbody>();
            if (trolleyBody == null || hubBody == null) return;

            Time.fixedDeltaTime = config.fixedTimestep;

            trolleyBody.isKinematic = true;
            trolleyBody.useGravity = false;
            trolleyBody.interpolation = RigidbodyInterpolation.Interpolate;

            hubBody.mass = config.hubMass;
            hubBody.linearDamping = config.hubLinearDamping;
            hubBody.angularDamping = config.hubAngularDamping;
            hubBody.useGravity = true;
            hubBody.isKinematic = false;
            hubBody.interpolation = RigidbodyInterpolation.Interpolate;
            hubBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            hubBody.solverIterations = config.solverIterations;
            hubBody.solverVelocityIterations = config.solverVelocityIterations;

            // Remove the old fixed-offset pendulum. The reference changes rope rest length,
            // so the replacement is a unilateral variable-length rope constraint.
            foreach (ConfigurableJoint joint in hub.GetComponents<ConfigurableJoint>())
                Object.DestroyImmediate(joint);

            ClawRopeConstraint rope = trolley.GetComponent<ClawRopeConstraint>();
            if (rope == null) rope = trolley.AddComponent<ClawRopeConstraint>();
            rope.Configure(config, trolleyBody, hubBody);

            ClawFinger[] fingers = Object.FindObjectsByType<ClawFinger>(FindObjectsSortMode.None)
                .OrderBy(f => f.name)
                .ToArray();

            foreach (ClawFinger finger in fingers)
            {
                Rigidbody body = finger.GetComponent<Rigidbody>();
                if (body != null)
                {
                    body.mass = config.fingerMass;
                    body.linearDamping = config.fingerIdleLinearDamping;
                    body.angularDamping = config.fingerIdleAngularDamping;
                    body.useGravity = true;
                    body.isKinematic = false;
                    body.interpolation = RigidbodyInterpolation.Interpolate;
                    body.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    body.solverIterations = config.solverIterations;
                    body.solverVelocityIterations = config.solverVelocityIterations;
                }

                HingeJoint hinge = finger.GetComponent<HingeJoint>();
                if (hinge != null)
                {
                    hinge.useSpring = false;
                    hinge.useMotor = false;
                    hinge.useLimits = true;
                    hinge.enableCollision = false;
                    hinge.enablePreprocessing = true;
                    hinge.axis = -Vector3.right;
                    JointLimits limits = hinge.limits;
                    limits.min = config.fingerClosedAngleDegrees;
                    limits.max = config.fingerOpenAngleDegrees;
                    limits.bounciness = 0f;
                    limits.contactDistance = 0f;
                    hinge.limits = limits;
                }

                finger.Configure(config);
            }

            ClawController claw = trolley.GetComponent<ClawController>();
            if (claw != null)
                claw.Configure(config, trolleyBody, hubBody, fingers, rope);

            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            Debug.Log("Claw3D: reference physics rig applied (variable rope, direct carriage step, free 0..45 hinges). Save the scene after testing.");
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.IsValid() && scene.name == PrototypeSceneName)
                EditorApplication.delayCall += ApplyToActivePrototypeScene;
        }
    }
}
#endif
