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

            GameObject trolley = Cube("Trolley", new Vector3(0f, 5.3f, 0f), new Vector3(0.7f, 0.25f, 0.7f));
            Rigidbody trolleyBody = trolley.AddComponent<Rigidbody>();
            trolleyBody.isKinematic = true;
            trolleyBody.interpolation = RigidbodyInterpolation.Interpolate;

            GameObject hub = Sphere("ClawHub", new Vector3(0f, 3.6f, 0f), 0.28f);
            Rigidbody hubBody = hub.AddComponent<Rigidbody>();
            hubBody.mass = config.clawMass;
            hubBody.linearDamping = config.swingDrag;
            hubBody.angularDamping = config.angularDrag;
            hubBody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            ConfigurableJoint cable = hub.AddComponent<ConfigurableJoint>();
            cable.connectedBody = trolleyBody;
            cable.autoConfigureConnectedAnchor = false;
            cable.anchor = Vector3.zero;
            cable.connectedAnchor = Vector3.zero;
            cable.xMotion = ConfigurableJointMotion.Locked;
            cable.yMotion = ConfigurableJointMotion.Locked;
            cable.zMotion = ConfigurableJointMotion.Locked;
            cable.angularXMotion = ConfigurableJointMotion.Free;
            cable.angularYMotion = ConfigurableJointMotion.Free;
            cable.angularZMotion = ConfigurableJointMotion.Free;

            GameObject cableVisual = Cylinder("CableVisual", new Vector3(0f, 4.45f, 0f), new Vector3(0.035f, 0.85f, 0.035f));
            cableVisual.transform.SetParent(trolley.transform, true);
            Object.DestroyImmediate(cableVisual.GetComponent<Collider>());

            List<ClawFinger> fingers = new();
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f;
                Vector3 radial = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                GameObject finger = Cube($"Finger_{i + 1}", hub.transform.position + radial * 0.38f + Vector3.down * 0.32f, new Vector3(0.16f, 0.75f, 0.16f));
                finger.transform.rotation = Quaternion.LookRotation(radial, Vector3.up) * Quaternion.Euler(20f, 0f, 0f);
                Rigidbody fingerBody = finger.AddComponent<Rigidbody>();
                fingerBody.mass = config.fingerMass;
                fingerBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                HingeJoint hinge = finger.AddComponent<HingeJoint>();
                hinge.connectedBody = hubBody;
                hinge.axis = Vector3.right;
                hinge.autoConfigureConnectedAnchor = true;
                ClawFinger controller = finger.AddComponent<ClawFinger>();
                controller.Configure(config, false);
                fingers.Add(controller);
            }

            ClawController claw = trolley.AddComponent<ClawController>();
            claw.Configure(config, trolleyBody, fingers.ToArray());
            ClawInput input = trolley.AddComponent<ClawInput>();
            MachineController machine = trolley.AddComponent<MachineController>();
            machine.Configure(input, claw);

            CreateToys();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = trolley;
            Debug.Log("Claw3D Day 1 prototype created. Open Assets/_Game/Scenes/ClawPrototype.unity and press Play. WASD/arrows move; Space toggles grip.");
        }

        private static void CreateToys()
        {
            Vector3[] positions =
            {
                new(-1.1f, 0.65f, 0.4f), new(-0.35f, 0.65f, 0.15f), new(0.45f, 0.65f, 0.45f),
                new(1.15f, 0.65f, 0.1f), new(-0.75f, 0.65f, -0.65f), new(0.15f, 0.65f, -0.55f), new(0.95f, 0.65f, -0.75f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject toy = Sphere($"Toy_{i + 1}", positions[i], 0.42f + (i % 3) * 0.05f);
                Rigidbody rb = toy.AddComponent<Rigidbody>();
                rb.mass = 0.55f + (i % 3) * 0.2f;
                toy.AddComponent<ToyPhysics>();
            }
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
        }

        private static void CreateCamera()
        {
            GameObject go = new("Main Camera");
            Camera camera = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            go.transform.position = new Vector3(7.5f, 6.2f, -8.5f);
            go.transform.LookAt(new Vector3(0f, 2.2f, 0f));
            camera.fieldOfView = 48f;
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
