using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrivateIsland.Editor
{
    [InitializeOnLoad]
    internal static class EnsureSampleSceneOpen
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        static EnsureSampleSceneOpen()
        {
            EditorApplication.delayCall += EnsureSceneReady;
        }

        private static void EnsureSceneReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            SceneAsset sampleSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(SampleScenePath);
            if (sampleSceneAsset != null)
            {
                EditorSceneManager.playModeStartScene = sampleSceneAsset;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (!ShouldOpenSampleScene(activeScene))
            {
                return;
            }

            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(SampleScenePath))
            {
                Debug.LogWarning($"Could not find required scene at '{SampleScenePath}'.");
                return;
            }

            EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        }

        private static bool ShouldOpenSampleScene(Scene activeScene)
        {
            if (!activeScene.IsValid())
            {
                return true;
            }

            if (activeScene.path == SampleScenePath)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(activeScene.path))
            {
                return false;
            }

            if (activeScene.isDirty)
            {
                return false;
            }

            GameObject[] roots = activeScene.GetRootGameObjects();
            if (roots == null || roots.Length == 0)
            {
                return true;
            }

            string[] defaultRootNames = { "Main Camera", "Directional Light", "DontDestroyOnLoad" };
            return roots.All(root => defaultRootNames.Contains(root.name));
        }
    }
}
