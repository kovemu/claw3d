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

            // +Z is the cabinet front. Keep X exactly centered: no three-quarter view.
            // Distance/FOV are chosen so the full 0.9m cabinet still fits a 9:16 portrait crop.
            cameraObject.transform.position = new Vector3(0f, 0.67f, 2.35f);
            cameraObject.transform.LookAt(new Vector3(0f, 0.44f, 0.03f), Vector3.up);
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.03f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.045f, 0.075f);

            EditorUtility.SetDirty(cameraObject.transform);
            EditorUtility.SetDirty(camera);
            SceneView.RepaintAll();
        }
    }
}
#endif
