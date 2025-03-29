using UnityEngine;

/// <summary>
/// Switches between camera game objects using number keys 1-4.
/// Camera 1 is enabled by default.
/// </summary>
public class CameraSwitcher : MonoBehaviour
{
    [Header("Camera GameObjects")]
    [Tooltip("Camera GameObject that will be activated when pressing 1")]
    [SerializeField] private GameObject cameraObject1;

    [Tooltip("Camera GameObject that will be activated when pressing 2")]
    [SerializeField] private GameObject cameraObject2;

    [Tooltip("Camera GameObject that will be activated when pressing 3")]
    [SerializeField] private GameObject cameraObject3;

    [Tooltip("Camera GameObject that will be activated when pressing 4")]
    [SerializeField] private GameObject cameraObject4;

    [Header("Settings")]
    [Tooltip("Whether to enable the script on start")]
    [SerializeField] private bool enableOnStart = true;

    // Store reference to currently active camera object
    private GameObject activeCameraObject;

    private void Start()
    {
        if (enableOnStart && cameraObject1 != null)
        {
            // Set camera 1 as the default active camera
            SetActiveCameraObject(cameraObject1);
        }
    }

    private void Update()
    {
        // Check for number key inputs
        if (Input.GetKeyDown(KeyCode.Alpha1) && cameraObject1 != null)
        {
            SetActiveCameraObject(cameraObject1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && cameraObject2 != null)
        {
            SetActiveCameraObject(cameraObject2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && cameraObject3 != null)
        {
            SetActiveCameraObject(cameraObject3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) && cameraObject4 != null)
        {
            SetActiveCameraObject(cameraObject4);
        }
    }

    /// <summary>
    /// Sets the specified camera object as active and disables all others
    /// </summary>
    /// <param name="cameraObjectToActivate">The camera GameObject to enable</param>
    public void SetActiveCameraObject(GameObject cameraObjectToActivate)
    {
        if (cameraObjectToActivate == null)
        {
            Debug.LogWarning("CameraSwitcher: Attempted to activate null camera object");
            return;
        }

        // Disable all camera objects first
        DisableAllCameraObjects();

        // Enable the target camera object
        cameraObjectToActivate.SetActive(true);
        activeCameraObject = cameraObjectToActivate;

        Debug.Log($"Switched to camera: {cameraObjectToActivate.name}");
    }

    /// <summary>
    /// Disables all camera objects managed by this switcher
    /// </summary>
    private void DisableAllCameraObjects()
    {
        if (cameraObject1 != null) cameraObject1.SetActive(false);
        if (cameraObject2 != null) cameraObject2.SetActive(false);
        if (cameraObject3 != null) cameraObject3.SetActive(false);
        if (cameraObject4 != null) cameraObject4.SetActive(false);
    }

    /// <summary>
    /// Activates camera object by index (1-4)
    /// </summary>
    /// <param name="cameraIndex">Index of the camera to activate (1-4)</param>
    public void ActivateCameraByIndex(int cameraIndex)
    {
        switch (cameraIndex)
        {
            case 1:
                if (cameraObject1 != null) SetActiveCameraObject(cameraObject1);
                break;
            case 2:
                if (cameraObject2 != null) SetActiveCameraObject(cameraObject2);
                break;
            case 3:
                if (cameraObject3 != null) SetActiveCameraObject(cameraObject3);
                break;
            case 4:
                if (cameraObject4 != null) SetActiveCameraObject(cameraObject4);
                break;
            default:
                Debug.LogWarning($"CameraSwitcher: Invalid camera index: {cameraIndex}. Must be 1-4.");
                break;
        }
    }

    /// <summary>
    /// Gets the currently active camera object
    /// </summary>
    /// <returns>The active camera GameObject</returns>
    public GameObject GetActiveCameraObject()
    {
        return activeCameraObject;
    }
}