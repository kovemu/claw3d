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
            Time.fixedDeltaTime = config.fixedTimestep;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateEnvironment(config);
            CreateCamera();
            CreateLights();

            Vector3 trolleyStart = new(config.homeXZ.x, config.topY, config.homeXZ.y);
            GameObject trolley = Cube("PhysicsTrolley", trolleyStart, new Vector3(0.075f, 0.025f, 0.075f));
            trolley.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.72f, 0.73f, 0.76f));
            Rigidbody trolleyBody = trolley.AddComponent<Rigidbody>();
            trolleyBody.isKinematic = true;
            trolleyBody.interpolation = RigidbodyInterpolation.Interpolate;

            Vector3 hubStart = trolleyStart + Vector3.down * config.cableLength;
            GameObject hub = Sphere("ClawHub", hubStart, config.hubRadius * 2f);
            hub.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.82f, 0.84f, 0.88f));
            Rigidbody hubBody = hub.AddComponent<Rigidbody>();
            hubBody.mass = config.hubMass;
            hubBody.linearDamping = config.hubLinearDamping;
            hubBody.angularDamping = config.hubAngularDamping;
            hubBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            hubBody.interpolation = RigidbodyInterpolation.Interpolate;
            hubBody.solverIterations = config.solverIterations;
            hubBody.solverVelocityIterations = config.solverVelocityIterations;

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
            cableMesh.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.10f, 0.10f, 0.12f));
            ClawCableVisual cableVisual = cableMesh.AddComponent<ClawCableVisual>();
            cableVisual.Configure(trolley.transform, hub.transform, cableMesh.transform, 0.004f);

            List<ClawFinger> fingers = BuildFingers(config, hubBody, hub.transform);
            IgnoreHubFingerContacts(hub.GetComponent<Collider>(), fingers);

            ClawController claw = trolley.AddComponent<ClawController>();
            claw.Configure(config, trolleyBody, hubBody, fingers.ToArray());
            ClawInput input = trolley.AddComponent<ClawInput>();
            MachineController machine = trolley.AddComponent<MachineController>();
            machine.Configure(input, claw, config);

            CreatePrizeSensor(config, machine);
            CreateToys(config);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = trolley;
            Debug.Log("Reference physics v2 built. Space=start, WASD/arrows=aim, Space=drop. Prize chute now scores physically fallen toys.");
        }

        private static List<ClawFinger> BuildFingers(ClawPhysicsConfig config, Rigidbody hubBody, Transform hub)
        {
            List<ClawFinger> result = new();
            PhysicsMaterial fingerMaterial = new("FingerGrip")
            {
                dynamicFriction = config.fingerFriction,
                staticFriction = config.fingerFriction,
                frictionCombine = PhysicsMaterialCombine.Maximum,
                bounciness = 0f
            };
            Material metal = CreateMaterial(new Color(0.80f, 0.82f, 0.86f));

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
                body.solverIterations = config.solverIterations;
                body.solverVelocityIterations = config.solverVelocityIterations;

                HingeJoint hinge = root.AddComponent<HingeJoint>();
                hinge.connectedBody = hubBody;
                hinge.autoConfigureConnectedAnchor = false;
                hinge.anchor = Vector3.zero;
                hinge.connectedAnchor = hub.InverseTransformPoint(anchorHubWorld);
                hinge.axis = Vector3.right;
                hinge.enableCollision = false;

                BuildFingerSegments(root.transform, config, fingerMaterial, metal);

                ClawFinger finger = root.AddComponent<ClawFinger>();
                finger.Configure(config);
                result.Add(finger);
            }
            return result;
        }

        private static void BuildFingerSegments(Transform root, ClawPhysicsConfig config, PhysicsMaterial material, Material visualMaterial)
        {
            float[] lengths = { config.fingerSegmentLengths.x, config.fingerSegmentLengths.y, config.fingerSegmentLengths.z };
            float[] curves = { config.fingerSegmentCurvesRadians.x, config.fingerSegmentCurvesRadians.y, config.fingerSegmentCurvesRadians.z };
            float[] radii = { config.fingerSegmentRadii.x, config.fingerSegmentRadii.y, config.fingerSegmentRadii.z };
            Vector3 cursor = Vector3.zero;

            for (int s = 0; s < 3; s++)
            {
                float degrees = curves[s] * Mathf.Rad2Deg;
                Vector3 direction = Quaternion.AngleAxis(-degrees, Vector3.right) * Vector3.down;
                Vector3 end = cursor + direction * lengths[s];
                Vector3 center = (cursor + end) * 0.5f;

                GameObject segment = Capsule($"Segment_{s + 1}", root, center, radii[s], lengths[s]);
                segment.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
                segment.GetComponent<Renderer>().sharedMaterial = visualMaterial;
                CapsuleCollider collider = segment.GetComponent<CapsuleCollider>();
                collider.material = material;
                cursor = end;
            }
        }

        private static void IgnoreHubFingerContacts(Collider hubCollider, List<ClawFinger> fingers)
        {
            if (hubCollider == null) return;
            foreach (ClawFinger finger in fingers)
            {
                foreach (Collider collider in finger.GetComponentsInChildren<Collider>())
                    UnityEngine.Physics.IgnoreCollision(hubCollider, collider, true);
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

            CreateBox("FloorBack", new Vector3(0f, -0.02f, (cz - chuteHalf - hz) * 0.5f), new Vector3(hx * 2f, 0.04f, cz - chuteHalf + hz));
            CreateBox("FloorFront", new Vector3(0f, -0.02f, (cz + chuteHalf + hz) * 0.5f), new Vector3(hx * 2f, 0.04f, hz - (cz + chuteHalf)));
            CreateBox("FloorLeft", new Vector3((-hx + cx - chuteHalf) * 0.5f, -0.02f, cz), new Vector3(cx - chuteHalf + hx, 0.04f, chuteHalf * 2f));
            CreateBox("FloorRight", new Vector3((cx + chuteHalf + hx) * 0.5f, -0.02f, cz), new Vector3(hx - (cx + chuteHalf), 0.04f, chuteHalf * 2f));

            CreateInvisibleCollider("GlassLeft", new Vector3(-hx, h * 0.5f, 0f), new Vector3(shell, h, hz * 2f));
            CreateInvisibleCollider("GlassRight", new Vector3(hx, h * 0.5f, 0f), new Vector3(shell, h, hz * 2f));
            CreateInvisibleCollider("GlassBack", new Vector3(0f, h * 0.5f, -hz), new Vector3(hx * 2f, h, shell));
            CreateInvisibleCollider("GlassFront", new Vector3(0f, h * 0.5f, hz), new Vector3(hx * 2f, h, shell));

            CreateBox("ChuteBack", new Vector3(cx, -0.17f, cz - chuteHalf - 0.01f), new Vector3(chuteHalf * 2f, 0.34f, 0.02f));
            CreateBox("ChuteLeft", new Vector3(cx - chuteHalf - 0.01f, -0.17f, cz), new Vector3(0.02f, 0.34f, chuteHalf * 2f));
            CreateBox("ChuteRight", new Vector3(cx + chuteHalf + 0.01f, -0.17f, cz), new Vector3(0.02f, 0.34f, chuteHalf * 2f));
            CreateBox("ChuteTray", new Vector3(cx, -0.35f, cz), new Vector3(chuteHalf * 2f, 0.04f, chuteHalf * 2f));

            Material frameMat = CreateMaterial(new Color(0.92f, 0.28f, 0.52f));
            CreateFramePost("FrameFL", new Vector3(-hx, h * 0.5f, hz), h, frameMat);
            CreateFramePost("FrameFR", new Vector3(hx, h * 0.5f, hz), h, frameMat);
            CreateFramePost("FrameBL", new Vector3(-hx, h * 0.5f, -hz), h, frameMat);
            CreateFramePost("FrameBR", new Vector3(hx, h * 0.5f, -hz), h, frameMat);
            GameObject topFront = Cube("TopFront", new Vector3(0f, h, hz), new Vector3(hx * 2f, 0.035f, 0.035f));
            topFront.GetComponent<Renderer>().sharedMaterial = frameMat;
            Object.DestroyImmediate(topFront.GetComponent<Collider>());
            GameObject topBack = Cube("TopBack", new Vector3(0f, h, -hz), new Vector3(hx * 2f, 0.035f, 0.035f));
            topBack.GetComponent<Renderer>().sharedMaterial = frameMat;
            Object.DestroyImmediate(topBack.GetComponent<Collider>());

            GameObject railX = Cube("RailX", new Vector3(0f, config.railY, 0f), new Vector3(0.62f, 0.018f, 0.018f));
            railX.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.40f, 0.42f, 0.46f));
            Object.DestroyImmediate(railX.GetComponent<Collider>());
        }

        private static void CreatePrizeSensor(ClawPhysicsConfig config, MachineController machine)
        {
            GameObject sensor = new("PrizeSensor");
            sensor.transform.position = new Vector3(config.homeXZ.x, -0.10f, config.homeXZ.y);
            BoxCollider trigger = sensor.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(0.20f, 0.16f, 0.20f);
            PrizeChuteSensor sensorScript = sensor.AddComponent<PrizeChuteSensor>();
            sensorScript.Configure(machine);
        }

        private static void CreateToys(ClawPhysicsConfig config)
        {
            PhysicsMaterial toyMaterial = new("ToyFriction")
            {
                dynamicFriction = config.toyFriction,
                staticFriction = config.toyFriction,
                frictionCombine = PhysicsMaterialCombine.Maximum,
                bounciness = 0.02f
            };

            Vector3[] positions =
            {
                new(-0.08f, 0.10f, -0.10f), new(0.04f, 0.12f, -0.03f), new(0.16f, 0.10f, 0.07f),
                new(-0.17f, 0.10f, 0.09f), new(0.08f, 0.19f, 0.15f), new(0.21f, 0.12f, -0.14f),
                new(-0.02f, 0.22f, 0.20f), new(0.19f, 0.21f, 0.20f)
            };

            Color[] colors =
            {
                new(1f, 0.42f, 0.62f), new(0.37f, 0.78f, 0.95f), new(1f, 0.82f, 0.40f),
                new(0.55f, 0.88f, 0.55f), new(0.78f, 0.57f, 0.92f), new(1f, 0.60f, 0.44f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                float radius = Mathf.Lerp(config.toyMinRadius, config.toyMaxRadius, (i % 3) / 2f);
                int variant = i % 3;
                CreateCompoundToy($"Toy_{i + 1}", positions[i], radius, variant, colors[i % colors.Length], toyMaterial, config);
            }
        }

        private static void CreateCompoundToy(string name, Vector3 position, float radius, int variant, Color color, PhysicsMaterial physicsMaterial, ClawPhysicsConfig config)
        {
            GameObject root = new(name);
            root.transform.position = position;
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = config.toyMass * (0.9f + variant * 0.12f);
            body.solverIterations = config.solverIterations;
            body.solverVelocityIterations = config.solverVelocityIterations;

            Material material = CreateMaterial(color);
            CreateToySphere("Body", root.transform, Vector3.zero, radius, material, physicsMaterial);

            if (variant == 0)
            {
                CreateToySphere("EarL", root.transform, new Vector3(radius * 0.50f, radius * 0.85f, 0f), radius * 0.34f, material, physicsMaterial);
                CreateToySphere("EarR", root.transform, new Vector3(-radius * 0.50f, radius * 0.85f, 0f), radius * 0.34f, material, physicsMaterial);
            }
            else if (variant == 1)
            {
                CreateToySphere("EarL", root.transform, new Vector3(radius * 0.60f, radius * 0.78f, 0f), radius * 0.42f, material, physicsMaterial);
                CreateToySphere("EarR", root.transform, new Vector3(-radius * 0.60f, radius * 0.78f, 0f), radius * 0.42f, material, physicsMaterial);
            }
            else
            {
                CreateToyEar("EarL", root.transform, new Vector3(radius * 0.35f, radius * 1.15f, 0f), radius, 0.20f, material, physicsMaterial);
                CreateToyEar("EarR", root.transform, new Vector3(-radius * 0.35f, radius * 1.15f, 0f), radius, -0.20f, material, physicsMaterial);
            }

            ToyPhysics toyPhysics = root.AddComponent<ToyPhysics>();
            toyPhysics.Configure(config.toyLinearDamping, config.toyAngularDamping, config.solverIterations, config.solverVelocityIterations);
        }

        private static void CreateToySphere(string name, Transform parent, Vector3 localPosition, float radius, Material material, PhysicsMaterial physicsMaterial)
        {
            GameObject part = Sphere(name, Vector3.zero, radius * 2f);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.GetComponent<Renderer>().sharedMaterial = material;
            part.GetComponent<SphereCollider>().material = physicsMaterial;
        }

        private static void CreateToyEar(string name, Transform parent, Vector3 localPosition, float radius, float tiltRadians, Material material, PhysicsMaterial physicsMaterial)
        {
            float earRadius = radius * 0.16f;
            float halfLen = radius * 0.35f;
            float fullHeight = (halfLen + earRadius) * 2f;
            GameObject ear = Capsule(name, parent, localPosition, earRadius, fullHeight);
            ear.transform.localRotation = Quaternion.Euler(0f, 0f, tiltRadians * Mathf.Rad2Deg);
            ear.GetComponent<Renderer>().sharedMaterial = material;
            ear.GetComponent<CapsuleCollider>().material = physicsMaterial;
        }

        private static void CreateFramePost(string name, Vector3 position, float height, Material material)
        {
            GameObject post = Cube(name, position, new Vector3(0.025f, height, 0.025f));
            post.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(post.GetComponent<Collider>());
        }

        private static void CreateInvisibleCollider(string name, Vector3 position, Vector3 scale)
        {
            GameObject go = Cube(name, position, scale);
            Object.DestroyImmediate(go.GetComponent<Renderer>());
        }

        private static void CreateCamera()
        {
            GameObject go = new("Main Camera");
            Camera camera = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            go.transform.position = new Vector3(1.15f, 1.18f, -1.35f);
            go.transform.LookAt(new Vector3(0f, 0.45f, 0f));
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
