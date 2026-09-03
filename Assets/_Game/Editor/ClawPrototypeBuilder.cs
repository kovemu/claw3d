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

        [MenuItem("Claw3D/Build Day 1 Prototype")]
        public static void Build()
        {
            EnsureFolders();
            ClawPhysicsConfig config = GetOrCreateConfig();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateLight();
            CreateFloor();
            CreateFrame();

            GameObject trolley = Cube("Trolley", config.homePosition, new Vector3(0.8f, 0.25f, 0.8f));
            Rigidbody trolleyBody = trolley.AddComponent<Rigidbody>();
            trolleyBody.isKinematic = true;
            trolleyBody.interpolation = RigidbodyInterpolation.Interpolate;

            GameObject hub = Sphere("ClawHub", config.homePosition + Vector3.down * config.topCableLength, 0.42f);
            Rigidbody hubBody = hub.AddComponent<Rigidbody>();
            hubBody.mass = config.clawMass;
            hubBody.linearDamping = config.swingDrag;
            hubBody.angularDamping = config.angularDrag;
            hubBody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            ConfigurableJoint cableJoint = hub.AddComponent<ConfigurableJoint>();
            cableJoint.connectedBody = trolleyBody;
            cableJoint.autoConfigureConnectedAnchor = false;
            cableJoint.anchor = Vector3.zero;
            cableJoint.connectedAnchor = new Vector3(0f, -config.topCableLength, 0f);
            cableJoint.xMotion = ConfigurableJointMotion.Locked;
            cableJoint.yMotion = ConfigurableJointMotion.Locked;
            cableJoint.zMotion = ConfigurableJointMotion.Locked;
            cableJoint.angularXMotion = ConfigurableJointMotion.Free;
            cableJoint.angularYMotion = ConfigurableJointMotion.Free;
            cableJoint.angularZMotion = ConfigurableJointMotion.Free;

            GameObject cableVisual = Cylinder("CableVisual", config.homePosition + Vector3.down * (config.topCableLength * 0.5f), new Vector3(0.035f, config.topCableLength * 0.5f, 0.035f));
            cableVisual.transform.SetParent(trolley.transform, true);
            Object.DestroyImmediate(cableVisual.GetComponent<Collider>());

            List<ClawFinger> fingers = new();
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f;
                Vector3 radial = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 fingerPos = hub.transform.position + radial * 0.42f + Vector3.down * 0.42f;
                GameObject finger = Cube($"Finger_{i + 1}", fingerPos, new Vector3(0.12f, 0.8f, 0.12f));
                finger.transform.rotation = Quaternion.LookRotation(radial, Vector3.up) * Quaternion.Euler(24f, 0f, 0f);

                Rigidbody fingerBody = finger.AddComponent<Rigidbody>();
                fingerBody.mass = config.fingerMass;
                fingerBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                fingerBody.interpolation = RigidbodyInterpolation.Interpolate;

                HingeJoint hinge = finger.AddComponent<HingeJoint>();
                hinge.connectedBody = hubBody;
                hinge.axis = Vector3.right;
                hinge.autoConfigureConnectedAnchor = true;

                ClawFinger controller = finger.AddComponent<ClawFinger>();
                controller.Configure(config, false);
                fingers.Add(controller);
            }

            ClawController claw = trolley.AddComponent<ClawController>();
            claw.Configure(config, trolleyBody, cableJoint, cableVisual.transform, fingers.ToArray());

            ClawInput input = trolley.AddComponent<ClawInput>();
            MachineController machine = trolley.AddComponent<MachineController>();
            machine.Configure(input, claw, config);

            CreateToys();
            CreatePrizeChute();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = trolley;
            Debug.Log("Claw3D prototype rebuilt. WASD/arrows aim. Space starts a full drop -> grip -> lift -> return -> release cycle.");
        }

        private static void CreateToys()
        {
            Vector3[] positions =
            {
                new(-0.9f, 0.65f, 0.35f), new(-0.25f, 0.68f, 0.05f), new(0.45f, 0.66f, 0.4f),
                new(1.05f, 0.65f, 0.05f), new(-0.65f, 0.68f, -0.55f), new(0.05f, 0.65f, -0.55f), new(0.8f, 0.67f, -0.65f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject toy = Sphere($"Toy_{i + 1}", positions[i], 0.75f + (i % 3) * 0.08f);
                Rigidbody rb = toy.AddComponent<Rigidbody>();
                rb.mass = 0.55f + (i % 3) * 0.2f;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                toy.AddComponent<ToyPhysics>();
            }
        }

        private static void CreatePrizeChute()
        {
            Cube("PrizeChuteBase", new Vector3(-2.35f, 0.45f, -1.55f), new Vector3(1.25f, 0.25f, 1.25f));
            Cube("PrizeChuteBack", new Vector3(-2.35f, 0.9f, -2.1f), new Vector3(1.25f, 0.9f, 0.12f));
            Cube("PrizeChuteLeft", new Vector3(-2.9f, 0.9f, -1.55f), new Vector3(0.12f, 0.9f, 1.25f));
            Cube("PrizeChuteRight", new Vector3(-1.8f, 0.9f, -1.55f), new Vector3(0.12f, 0.9f, 1.25f));
        }

        private static void CreateFloor()
        {
            GameObject floor = Cube("Floor", new Vector3(0f, 0f, 0f), new Vector3(8f, 0.3f, 6f));
            floor.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.16f, 0.18f, 0.22f));
        }

        private static void CreateFrame()
        {
            Vector3[] posts = { new(-3.8f, 2.7f, -2.8f), new(3.8f, 2.7f, -2.8f), new(-3.8f, 2.7f, 2.8f), new(3.8f, 2.7f, 2.8f) };
            foreach (Vector3 p in posts) Cube("FramePost", p, new Vector3(0.16f, 5.4f, 0.16f));
            Cube("TopBeamFront", new Vector3(0f, 5.35f, -2.8f), new Vector3(7.7f, 0.18f, 0.18f));
            Cube("TopBeamBack", new Vector3(0f, 5.35f, 2.8f), new Vector3(7.7f, 0.18f, 0.18f));
        }

        private static void CreateCamera()
        {
            GameObject go = new("Main Camera");
            Camera camera = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            go.transform.position = new Vector3(7.3f, 6.0f, -8.3f);
            go.transform.LookAt(new Vector3(0f, 2.2f, 0f));
            camera.fieldOfView = 46f;
        }

        private static void CreateLight()
        {
            GameObject go = new("Directional Light");
            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            go.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
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
            if (config != null) return config;
            config = ScriptableObject.CreateInstance<ClawPhysicsConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
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
