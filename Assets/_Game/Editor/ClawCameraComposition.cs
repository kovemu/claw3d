#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Claw3D.Editor
{
    [InitializeOnLoad]
    public static class ClawCameraComposition
    {
        private const string PrototypeSceneName = "ClawPrototype";

        static ClawCameraComposition()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorApplication.delayCall += ApplyToActivePrototypeScene;
        }

        [MenuItem("Claw3D/Set Front Camera Composition")]
        public static void ApplyToActivePrototypeScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!IsPrototypeScene(scene)) return;
            Apply();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (IsPrototypeScene(scene)) Apply();
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            if (scene.name == PrototypeSceneName || path.EndsWith("/ClawPrototype.unity")) Apply();
        }

        private static bool IsPrototypeScene(Scene scene)
        {
            return scene.IsValid() && scene.name == PrototypeSceneName;
        }

        private static void Apply()
        {
            GameObject cameraObject = GameObject.FindGameObjectWithTag("MainCamera");
            if (cameraObject == null) return;

            Camera camera = cameraObject.GetComponent<Camera>();
            if (camera == null) return;

            // Gameplay view, not a showroom shot: the player's face is close to the glass.
            // The eye sits just above the play field and looks down roughly 24 degrees,
            // enough to read toy depth without turning the scene into a top-down view.
            cameraObject.transform.position = new Vector3(0f, 1.00f, 1.32f);
            cameraObject.transform.LookAt(new Vector3(0f, 0.42f, 0.00f), Vector3.up);
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 0.015f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.045f, 0.075f);

            EditorUtility.SetDirty(cameraObject.transform);
            EditorUtility.SetDirty(camera);
            SceneView.RepaintAll();
        }
    }
}
#endif
