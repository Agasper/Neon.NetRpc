using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor.SearchService;
using Scene = UnityEngine.SceneManagement.Scene;


[InitializeOnLoad]
public static class LoadMainScene
{
    static LoadMainScene()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Scene currentScene = EditorSceneManager.GetActiveScene();
            if (!EditorBuildSettings.scenes.Any(o => o.path == currentScene.path))
                return;


            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorPrefs.SetString("active_scene", currentScene.path);
                EditorSceneManager.OpenScene("Assets/Scenes/init.unity");
                EditorApplication.isPlaying = true;
            }
            else
            {
                EditorApplication.isPlaying = false;
            }
        }

        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Scene currentScene = EditorSceneManager.GetActiveScene();
            //Debug.LogWarning(EditorBuildSettings.scenes.Select(o => o.path).ToStringCustom());
            if (!EditorBuildSettings.scenes.Any(o => o.path == currentScene.path))
                return;

            string scene = EditorPrefs.GetString("active_scene", currentScene.path);

            EditorApplication.isPlaying = false;
            if (!string.IsNullOrEmpty(scene))
                EditorSceneManager.OpenScene(scene);
        }
    }
}
