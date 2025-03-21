using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple script that quits the application when a configurable key is pressed
/// and reloads the current scene when another key is pressed.
/// </summary>
public class QuitApplication : MonoBehaviour
{
    [Header("Quit Application Settings")]
    [Tooltip("Key that triggers application quit")]
    [SerializeField] private KeyCode quitKey = KeyCode.Escape;

    [Tooltip("Whether to quit in the editor too (otherwise just stops play mode)")]
    [SerializeField] private bool quitInEditor = false;

    [Header("Scene Reload Settings")]
    [Tooltip("Key that triggers scene reload")]
    [SerializeField] private KeyCode reloadKey = KeyCode.R;

    private void Update()
    {
        // Check for quit key press
        if (Input.GetKeyDown(quitKey))
        {
            QuitGame();
        }

        // Check for reload key press
        if (Input.GetKeyDown(reloadKey))
        {
            ReloadCurrentScene();
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        if (quitInEditor)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
#else
        // Actual build - quit the application
        Application.Quit();
#endif
    }

    private void ReloadCurrentScene()
    {
        // Get the current scene and reload it
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    /// <summary>
    /// Public method to programmatically quit the application
    /// </summary>
    public void QuitApplicationImmediately()
    {
        QuitGame();
    }

    /// <summary>
    /// Public method to programmatically reload the current scene
    /// </summary>
    public void ReloadSceneImmediately()
    {
        ReloadCurrentScene();
    }
}

