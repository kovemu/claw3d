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

            ClawFinger[] fingers = Object.FindObjectsByType<ClawFinger>(FindObjectsSortMode.None)
                .OrderBy(f => f.name)
                .ToArray();

            Time.fixedDeltaTime = config.fixedTimestep;

            trolleyBody.mass = 1f;
            trolleyBody.isKinematic = true;
            trolleyBody.useGravity = false;
            trolleyBody.linearDamping = 0f;
            trolleyBody.angularDamping = 0.05f;
            trolleyBody.interpolation = RigidbodyInterpolation.Interpolate;

            hubBody.mass = config.hubMass;
            hubBody.linearDamping = config.hubLinearDamping;
            hubBody.angularDamping = config.hubAngularDamping;
            hubBody.useGravity = true;
            hubBody.isKinematic = false;
            hubBody.interpolation = RigidbodyInterpolation.Interpolate;
            hubBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            hubBody.solverIterations = config.solverIterations;
            hubBody.solverVelocityIterations = config.solverVelocityIterations;

            foreach (ConfigurableJoint joint in hub.GetComponents<ConfigurableJoint>())
                Object.DestroyImmediate(joint);

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

                    // The source mesh uses local Z. The temporary capsule prototype was authored around
                    // local X, so preserve its equivalent axis until the extracted-dimension arm rig replaces it.
                    hinge.axis = -Vector3.right;

                    JointLimits limits = hinge.limits;
                    limits.min = config.fingerClosedAngleDegrees;
                    limits.max = config.fingerOpenAngleDegrees;
                    limits.bounciness = 0f;
                    limits.contactDistance = config.fingerLimitContactDistance;
                    hinge.limits = limits;
                }

                finger.Configure(config);
            }

            // The old prototype placed the head cableLength (0.24 m) below the trolley. The extracted
            // source rope begins at only 0.027136756 m of rest length and its MOVER attachment is offset.
            // Align the temporary geometry to this verified rope geometry before initializing the pool,
            // otherwise the hard zero-compliance constraints start heavily stretched and explode.
            if (!Application.isPlaying)
            {
                Vector3 topAttachment = trolleyBody.position + trolleyBody.rotation * config.ropeTopAttachmentOffset;
                Vector3 currentHeadAttachment = hubBody.position + hubBody.rotation * config.ropeHeadAttachmentOffset;
                Vector3 desiredHeadAttachment = topAttachment + Vector3.down * config.ropeInitialRestLength;
                Vector3 rigDelta = desiredHeadAttachment - currentHeadAttachment;

                if (rigDelta.sqrMagnitude > 0.00000001f)
                {
                    hubBody.position += rigDelta;
                    foreach (ClawFinger finger in fingers)
                    {
                        Rigidbody fingerBody = finger.GetComponent<Rigidbody>();
                        if (fingerBody != null)
                            fingerBody.position += rigDelta;
                    }
                }

                UnityEngine.Physics.SyncTransforms();
            }

            ClawRopeConstraint rope = trolley.GetComponent<ClawRopeConstraint>();
            if (rope == null) rope = trolley.AddComponent<ClawRopeConstraint>();
            rope.Configure(config, trolleyBody, hubBody);

            ClawController claw = trolley.GetComponent<ClawController>();
            if (claw != null)
                claw.Configure(config, trolleyBody, hubBody, fingers, rope);

            GameObject cable = GameObject.Find("Cable");
            if (cable != null)
            {
                ClawCableVisual cableVisual = cable.GetComponent<ClawCableVisual>();
                if (cableVisual != null) cableVisual.SetRope(rope);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            Debug.Log(
                "Claw3D: pooled reference rope rig applied. " +
                $"Unity {1f / config.fixedTimestep:0} Hz, rope {config.ropeSubsteps} substeps, " +
                $"initial/pool particles {config.ropeActiveParticles}/{config.ropeParticlePoolCapacity}, " +
                $"initial rest {config.ropeInitialRestLength:0.000000} m, " +
                $"claw/finger mass {config.hubMass:0.##}/{config.fingerMass:0.##} kg.");
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.IsValid() && scene.name == PrototypeSceneName)
                EditorApplication.delayCall += ApplyToActivePrototypeScene;
        }
    }
}
#endif
