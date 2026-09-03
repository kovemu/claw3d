#if UNITY_EDITOR
using System.Collections.Generic;
using Claw3D.Claw;
using Claw3D.Input;
using Claw3D.Machine;
using Claw3D.Physics;
using Claw3D.Toys;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Claw3D.Editor
{
    public static class ClawPrototypeBuilder
    {
        private const string ConfigPath = "Assets/_Game/Config/ClawPhysicsConfig.asset";
        private const string ScenePath = "Assets/_Game/Scenes/ClawPrototype.unity";

        [MenuItem("Claw3D/Build Reference Physics Prototype")]
        public static void Build()
        {
            EnsureFolders();
            ClawPhysicsConfig config = GetOrCreateConfig();
            ResetConfigToCurrentDefaults(config);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateEnvironment(config);
            CreateCamera();
            CreateLights();

            Vector3 trolleyStart = new(config.homeXZ.x, config.topY, config.homeXZ.y);
            GameObject trolley = Cube("PhysicsTrolley", trolleyStart, new Vector3(0.08f, 0.025f, 0.08f));
            Rigidbody trolleyBody = trolley.AddComponent<Rigidbody>();
            trolleyBody.isKinematic = true;
            trolleyBody.interpolation = RigidbodyInterpolation.Interpolate;

            Vector3 hubStart = trolleyStart + Vector3.down * config.cableLength;
            GameObject hub = Sphere("ClawHub", hubStart, config.hubRadius * 2f);
            Rigidbody hubBody = hub.AddComponent<Rigidbody>();
            hubBody.mass = config.hubMass;
            hubBody.linearDamping = config.hubLinearDamping;
            hubBody.angularDamping = config.hubAngularDamping;
            hubBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            hubBody.interpolation = RigidbodyInterpolation.Interpolate;

            ConfigurableJoint pendulum = hub.AddComponent<ConfigurableJoint>();
            pendulum.connectedBody = trolleyBody;
            pendulum.autoConfigureConnectedAnchor = false;
            pendulum.anchor = Vector3.up * config.cableLength;
            pendulum.connectedAnchor = Vector3.zero;
            pendulum.xMotion = ConfigurableJointMotion.Locked;
            pendulum.yMotion = ConfigurableJointMotion.Locked;
            pendulum.zMotion = ConfigurableJointMotion.Locked;
            pendulum.angularXMotion = ConfigurableJointMotion.Free;
            pendulum.angularYMotion = ConfigurableJointMotion.Free;
            pendulum.angularZMotion = ConfigurableJointMotion.Free;
            pendulum.enableCollision = false;

            GameObject cableMesh = Cylinder("Cable", Vector3.zero, Vector3.one);
            Object.DestroyImmediate(cableMesh.GetComponent<Collider>());
            ClawCableVisual cableVisual = cableMesh.AddComponent<ClawCableVisual>();
            cableVisual.Configure(trolley.transform, hub.transform, cableMesh.transform, 0.004f);

            List<ClawFinger> fingers = BuildFingers(config, hubBody, hub.transform);

            ClawController claw = trolley.AddComponent<ClawController>();
            claw.Configure(config, trolleyBody, hubBody, fingers.ToArray());
            ClawInput input = trolley.AddComponent<ClawInput>();
            MachineController machine = trolley.AddComponent<MachineController>();
            machine.Configure(input, claw, config);

            CreateToys(config);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = trolley;
            Debug.Log("Reference-physics prototype built. Space=start, WASD/arrows=aim, Space=drop. Physics follows the RiwRiwara architecture, reimplemented for Unity PhysX.");
        }

        private static List<ClawFinger> BuildFingers(ClawPhysicsConfig config, Rigidbody hubBody, Transform hub)
        {
            List<ClawFinger> result = new();
            PhysicMaterial fingerMaterial = new("FingerGrip")
            {
                dynamicFriction = config.fingerFriction,
                staticFriction = config.fingerFriction,
                frictionCombine = PhysicMaterialCombine.Maximum,
                bounciness = 0f
            };

            for (int i = 0; i < config.fingerCount; i++)
            {
                float theta = i * 360f / config.fingerCount;
                Quaternion radialRotation = Quaternion.Euler(0f, theta, 0f);
                Vector3 radial = radialRotation * Vector3.forward;
                Vector3 anchorHubWorld = hub.position + radial * config.fingerMountRadius + Vector3.up * config.fingerMountY;

                GameObject root = new($"Finger_{i + 1}");
                root.transform.position = anchorHubWorld;
                root.transform.rotation = Quaternion.LookRotation(radial, Vector3.up);

                Rigidbody body = root.AddComponent<Rigidbody>();
                body.mass = config.fingerMass;
                body.angularDamping = config.fingerAngularDamping;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                body.interpolation = RigidbodyInterpolation.Interpolate;

                HingeJoint hinge = root.AddComponent<HingeJoint>();
                hinge.connectedBody = hubBody;
                hinge.autoConfigureConnectedAnchor = false;
                hinge.anchor = Vector3.zero;
                hinge.connectedAnchor = hub.InverseTransformPoint(anchorHubWorld);
                hinge.axis = Vector3.right;
                hinge.enableCollision = false;

                BuildFingerSegments(root.transform, config, fingerMaterial);

                ClawFinger finger = root.AddComponent<ClawFinger>();
                finger.Configure(config);
                result.Add(finger);
            }
            return result;
        }

        private static void BuildFingerSegments(Transform root, ClawPhysicsConfig config, PhysicMaterial material)
        {
            float[] lengths = { config.fingerSegmentLengths.x, config.fingerSegmentLengths.y, config.fingerSegmentLengths.z };
            float[] curves = { config.fingerSegmentCurvesRadians.x, config.fingerSegmentCurvesRadians.y, config.fingerSegmentCurvesRadians.z };
            float[] radii = { config.fingerSegmentRadii.x, config.fingerSegmentRadii.y, config.fingerSegmentRadii.z };
            Vector3 cursor = Vector3.zero;

            for (int s = 0; s < 3; s++)
            {
                float degrees = curves[s] * Mathf.Rad2Deg;
                Vector3 direction = Quaternion.AngleAxis(degrees, Vector3.right) * Vector3.down;
                Vector3 end = cursor + direction * lengths[s];
                Vector3 center = (cursor + end) * 0.5f;

                GameObject segment = Capsule($"Segment_{s + 1}", root, center, radii[s], lengths[s]);
                segment.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
                CapsuleCollider collider = segment.GetComponent<CapsuleCollider>();
                collider.material = material;
                cursor = end;
            }
        }

        private static void CreateEnvironment(ClawPhysicsConfig config)
        {
            float hx = config.cabinetHalfX;
            float hz = config.cabinetHalfZ;
            float h = config.cabinetHeight;
            float shell = config.shellThickness;
            float chuteHalf = 0.11f;
            float cx = config.homeXZ.x;
            float cz = config.homeXZ.y;

            // Floor strips leave a real square prize hole under the home position.
            CreateBox("FloorBack", new Vector3(0f, -0.02f, (cz - chuteHalf - hz) * 0.5f), new Vector3(hx * 2f, 0.04f, cz - chuteHalf + hz));
            CreateBox("FloorFront", new Vector3(0f, -0.02f, (cz + chuteHalf + hz) * 0.5f), new Vector3(hx * 2f, 0.04f, hz - (cz + chuteHalf)));
            CreateBox("FloorLeft", new Vector3((-hx + cx - chuteHalf) * 0.5f, -0.02f, cz), new Vector3(cx - chuteHalf + hx, 0.04f, chuteHalf * 2f));
            CreateBox("FloorRight", new Vector3((cx + chuteHalf + hx) * 0.5f, -0.02f, cz), new Vector3(hx - (cx + chuteHalf), 0.04f, chuteHalf * 2f));

            CreateBox("WallLeft", new Vector3(-hx, h * 0.5f, 0f), new Vector3(shell, h, hz * 2f));
            CreateBox("WallRight", new Vector3(hx, h * 0.5f, 0f), new Vector3(shell, h, hz * 2f));
            CreateBox("WallBack", new Vector3(0f, h * 0.5f, -hz), new Vector3(hx * 2f, h, shell));
            CreateBox("WallFront", new Vector3(0f, h * 0.5f, hz), new Vector3(hx * 2f, h, shell));

            // Simple shaft/catch tray so released toys visibly fall through.
            CreateBox("ChuteBack", new Vector3(cx, -0.17f, cz - chuteHalf - 0.01f), new Vector3(chuteHalf * 2f, 0.34f, 0.02f));
            CreateBox("ChuteLeft", new Vector3(cx - chuteHalf - 0.01f, -0.17f, cz), new Vector3(0.02f, 0.34f, chuteHalf * 2f));
            CreateBox("ChuteRight", new Vector3(cx + chuteHalf + 0.01f, -0.17f, cz), new Vector3(0.02f, 0.34f, chuteHalf * 2f));
            CreateBox("ChuteTray", new Vector3(cx, -0.35f, cz), new Vector3(chuteHalf * 2f, 0.04f, chuteHalf * 2f));

            // Visible rail.
            GameObject railX = Cube("RailX", new Vector3(0f, config.railY, 0f), new Vector3(0.62f, 0.018f, 0.018f));
            Object.DestroyImmediate(railX.GetComponent<Collider>());
        }

        private static void CreateToys(ClawPhysicsConfig config)
        {
            PhysicMaterial toyMaterial = new("ToyFriction")
            {
                dynamicFriction = config.toyFriction,
                staticFriction = config.toyFriction,
                frictionCombine = PhysicMaterialCombine.Maximum,
                bounciness = 0.02f
            };

            Vector3[] positions =
            {
                new(-0.10f, 0.10f, -0.08f), new(0.02f, 0.13f, -0.02f), new(0.15f, 0.10f, 0.06f),
                new(-0.16f, 0.10f, 0.10f), new(0.10f, 0.18f, 0.15f), new(0.22f, 0.11f, -0.13f),
                new(-0.02f, 0.22f, 0.20f), new(0.20f, 0.20f, 0.20f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                float radius = Mathf.Lerp(config.toyMinRadius, config.toyMaxRadius, (i % 3) / 2f);
                GameObject toy = Sphere($"Toy_{i + 1}", positions[i], radius * 2f);
                Collider collider = toy.GetComponent<Collider>();
                collider.material = toyMaterial;
                Rigidbody body = toy.AddComponent<Rigidbody>();
                body.mass = config.toyMass * (0.85f + (i % 3) * 0.15f);
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                toy.AddComponent<ToyPhysics>();
            }
        }

        private static void CreateCamera()
        {
            GameObject go = new("Main Camera");
            Camera camera = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            go.transform.position = new Vector3(1.15f, 1.22f, -1.35f);
            go.transform.LookAt(new Vector3(0f, 0.48f, 0f));
            camera.fieldOfView = 38f;
        }

        private static void CreateLights()
        {
            GameObject key = new("Key Light");
            Light light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            key.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            GameObject fill = new("Fill Light");
            Light fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.range = 3f;
            fillLight.intensity = 4f;
            fill.transform.position = new Vector3(0f, 1.1f, -0.25f);
        }

        private static GameObject CreateBox(string name, Vector3 position, Vector3 scale)
        {
            GameObject go = Cube(name, position, scale);
            go.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.17f, 0.18f, 0.22f));
            return go;
        }

        private static GameObject Cube(string name, Vector3 position, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            return go;
        }

        private static GameObject Sphere(string name, Vector3 position, float diameter)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = Vector3.one * diameter;
            return go;
        }

        private static GameObject Capsule(string name, Transform parent, Vector3 localPosition, float radius, float length)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            // Unity capsule primitive is 2 units tall and 1 unit wide.
            go.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
            return go;
        }

        private static GameObject Cylinder(string name, Vector3 position, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            return go;
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { color = color };
            return material;
        }

        private static ClawPhysicsConfig GetOrCreateConfig()
        {
            ClawPhysicsConfig config = AssetDatabase.LoadAssetAtPath<ClawPhysicsConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ClawPhysicsConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            return config;
        }

        private static void ResetConfigToCurrentDefaults(ClawPhysicsConfig config)
        {
            // Existing ScriptableObject assets preserve removed/old serialized values.
            // Copy fresh defaults so rebuilding the prototype always uses the current tuning model.
            ClawPhysicsConfig defaults = ScriptableObject.CreateInstance<ClawPhysicsConfig>();
            EditorUtility.CopySerialized(defaults, config);
            Object.DestroyImmediate(defaults);
            EditorUtility.SetDirty(config);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "_Game");
            EnsureFolder("Assets/_Game", "Config");
            EnsureFolder("Assets/_Game", "Scenes");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
