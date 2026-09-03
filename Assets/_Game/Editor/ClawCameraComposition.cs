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

            // The reference game's coordinate convention treats +Z as the cabinet front.
            // Keep the camera centered on X so the player sees the machine straight-on,
            // with only a small downward pitch to expose the prize bed.
            cameraObject.transform.position = new Vector3(0f, 0.74f, 2.20f);
            cameraObject.transform.LookAt(new Vector3(0f, 0.47f, 0.03f), Vector3.up);
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.03f;

            EditorUtility.SetDirty(cameraObject.transform);
            EditorUtility.SetDirty(camera);
            SceneView.RepaintAll();
        }
    }
}
#endif
