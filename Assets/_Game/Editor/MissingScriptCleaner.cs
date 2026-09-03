#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Claw3D.Editor
{
    [InitializeOnLoad]
    public static class MissingScriptCleaner
    {
        private const string PrototypeSceneName = "ClawPrototype";

        static MissingScriptCleaner()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += CleanActivePrototypeScene;
        }

        [MenuItem("Claw3D/Clean Missing Scripts")]
        public static void CleanActivePrototypeScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != PrototypeSceneName) return;

            int removed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                removed += CleanRecursive(root.transform);

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"Claw3D: removed {removed} missing script component(s) from {PrototypeSceneName}.");
            }
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.IsValid() && scene.name == PrototypeSceneName)
                EditorApplication.delayCall += CleanActivePrototypeScene;
        }

        private static int CleanRecursive(Transform node)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(node.gameObject);
            for (int i = 0; i < node.childCount; i++)
                removed += CleanRecursive(node.GetChild(i));
            return removed;
        }
    }
}
#endif
