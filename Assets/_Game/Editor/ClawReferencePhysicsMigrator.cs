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

        private const float ReferenceClawImportScale = 0.2227894217f;

        private static readonly Vector3 ReferenceHeadColliderCenter = new(0f, -0.195496231f, 0f);
        private const float ReferenceHeadColliderRadius = 0.11f;
        private const float ReferenceHeadColliderHeight = 0.513159454f;

        private static readonly Vector3[] ReferenceFingerPivotOffsets =
        {
            new(-0.0002702177f, -0.0906925201f, 0.0436868667f),
            new(0.0378040373f, -0.0906924009f, -0.0215678215f),
            new(-0.0377401411f, -0.0906636119f, -0.0219225884f)
        };

        private static readonly Vector3[] ReferenceFingerCapsulePositions =
        {
            new(-0.0741f, -0.0054f, 0f),
            new(-0.1877f, -0.0608f, 0f),
            new(-0.2605f, -0.1643f, 0f),
            new(-0.2923f, -0.2789f, 0f)
        };

        private static readonly Vector3[] ReferenceFingerCapsuleScales =
        {
            new(0.0381f, 0.0758f, 0.0381f),
            new(0.0381f, 0.0758f, 0.0381f),
            new(0.0381f, 0.0758f, 0.0381f),
            new(0.0381f, 0.0758f, 0.0381f)
        };

        private static readonly Vector3[] ReferenceFingerBoxPositions =
        {
            new(-0.2426f, -0.4408f, 0f),
            new(-0.2774f, -0.3633f, 0f)
        };

        private static readonly Vector3[] ReferenceFingerBoxScales =
        {
            new(0.0131f, 0.0896f, 0.0609f),
            new(0.0131f, 0.0896f, 0.0609f)
        };

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

            ApplyReferenceHeadCollider(hub);
            AlignReferenceFingerPivots(hub.transform, fingers);

            for (int fingerIndex = 0; fingerIndex < fingers.Length; ++fingerIndex)
            {
                ClawFinger finger = fingers[fingerIndex];
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
                    hinge.anchor = Vector3.zero;
                    hinge.autoConfigureConnectedAnchor = true;

                    JointLimits limits = hinge.limits;
                    limits.min = Mathf.Min(config.fingerOpenAngleDegrees, config.fingerClosedAngleDegrees);
                    limits.max = Mathf.Max(config.fingerOpenAngleDegrees, config.fingerClosedAngleDegrees);
                    limits.bounciness = 0f;
                    limits.contactDistance = config.fingerLimitContactDistance;
                    hinge.limits = limits;
                }

                ApplyReferenceFingerColliders(finger);
                finger.Configure(config);
            }

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

            IgnoreConnectedClawContacts(hub, fingers);

            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            Debug.Log(
                "Claw3D: source-semantic claw rig applied. " +
                $"Open {config.fingerOpenAngleDegrees:0.#}°, closed {config.fingerClosedAngleDegrees:0.#}°, " +
                $"rope {config.ropeSubsteps} substeps, head collider Capsule, finger colliders 4 Capsule + 2 Box each.");
        }

        private static void AlignReferenceFingerPivots(Transform hub, ClawFinger[] fingers)
        {
            int count = Mathf.Min(fingers.Length, ReferenceFingerPivotOffsets.Length);
            for (int i = 0; i < count; ++i)
            {
                Vector3 offset = ReferenceFingerPivotOffsets[i];
                fingers[i].transform.position = hub.position + hub.rotation * offset;

                Vector3 radial = new Vector3(offset.x, 0f, offset.z);
                if (radial.sqrMagnitude > 0.000001f)
                {
                    radial = hub.rotation * radial.normalized;
                    fingers[i].transform.rotation = Quaternion.LookRotation(radial, hub.up);
                }
            }
        }

        private static void ApplyReferenceHeadCollider(GameObject hub)
        {
            foreach (Collider existing in hub.GetComponents<Collider>())
                existing.enabled = false;

            Transform oldRoot = hub.transform.Find("ReferenceHeadCollider");
            if (oldRoot != null)
                Object.DestroyImmediate(oldRoot.gameObject);

            GameObject root = new("ReferenceHeadCollider");
            root.transform.SetParent(hub.transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            Vector3 parentScale = hub.transform.lossyScale;
            root.transform.localScale = new Vector3(
                SafeInverse(parentScale.x),
                SafeInverse(parentScale.y),
                SafeInverse(parentScale.z));

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.center = ReferenceHeadColliderCenter * ReferenceClawImportScale;
            capsule.radius = ReferenceHeadColliderRadius * ReferenceClawImportScale;
            capsule.height = ReferenceHeadColliderHeight * ReferenceClawImportScale;
        }

        private static void ApplyReferenceFingerColliders(ClawFinger finger)
        {
            Transform previous = finger.transform.Find("ReferenceColliders");
            if (previous != null)
                Object.DestroyImmediate(previous.gameObject);

            foreach (Collider existing in finger.GetComponentsInChildren<Collider>(true))
                existing.enabled = false;

            GameObject frame = new("ReferenceColliders");
            frame.transform.SetParent(finger.transform, false);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            frame.transform.localScale = Vector3.one * ReferenceClawImportScale;

            for (int i = 0; i < ReferenceFingerCapsulePositions.Length; ++i)
            {
                GameObject child = new($"Capsule_{i + 1}");
                child.transform.SetParent(frame.transform, false);
                child.transform.localPosition = ReferenceFingerCapsulePositions[i];
                child.transform.localRotation = Quaternion.identity;
                child.transform.localScale = ReferenceFingerCapsuleScales[i];

                CapsuleCollider collider = child.AddComponent<CapsuleCollider>();
                collider.center = Vector3.zero;
                collider.radius = 0.5f;
                collider.height = 2f;
                collider.direction = 1;
            }

            for (int i = 0; i < ReferenceFingerBoxPositions.Length; ++i)
            {
                GameObject child = new($"Box_{i + 1}");
                child.transform.SetParent(frame.transform, false);
                child.transform.localPosition = ReferenceFingerBoxPositions[i];
                child.transform.localRotation = Quaternion.identity;
                child.transform.localScale = ReferenceFingerBoxScales[i];

                BoxCollider collider = child.AddComponent<BoxCollider>();
                collider.center = Vector3.zero;
                collider.size = Vector3.one;
            }
        }

        private static void IgnoreConnectedClawContacts(GameObject hub, ClawFinger[] fingers)
        {
            Collider[] hubColliders = hub.GetComponentsInChildren<Collider>(true)
                .Where(c => c.enabled)
                .ToArray();

            foreach (ClawFinger finger in fingers)
            {
                Collider[] fingerColliders = finger.GetComponentsInChildren<Collider>(true)
                    .Where(c => c.enabled)
                    .ToArray();

                foreach (Collider hubCollider in hubColliders)
                foreach (Collider fingerCollider in fingerColliders)
                    UnityEngine.Physics.IgnoreCollision(hubCollider, fingerCollider, true);
            }
        }

        private static float SafeInverse(float value)
        {
            return Mathf.Abs(value) < 0.000001f ? 1f : 1f / value;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.IsValid() && scene.name == PrototypeSceneName)
                EditorApplication.delayCall += ApplyToActivePrototypeScene;
        }
    }
}
#endif
