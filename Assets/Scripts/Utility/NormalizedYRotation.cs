using UnityEngine;

public class NormalizedYRotation : MonoBehaviour
{
    [Tooltip("Multiplies the normalized input value (0-1) before mapping to rotation")]
    [Range(0f, 10f)]
    public float multiplier = 1.0f;

    [Tooltip("Controls the smoothness of rotation lerping (higher = smoother but slower)")]
    [Range(0.1f, 20f)]
    public float smoothness = 5.0f;

    // Target rotation we're lerping towards
    private Quaternion targetRotation;
    // Flag to indicate if we should be lerping
    private bool isLerping = false;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize target rotation to current rotation
        targetRotation = transform.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (isLerping)
        {
            // Smoothly interpolate current rotation towards target
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                targetRotation,
                Time.deltaTime * smoothness
            );

            // Check if we're close enough to consider it complete
            if (Quaternion.Angle(transform.localRotation, targetRotation) < 0.1f)
            {
                transform.localRotation = targetRotation;
                isLerping = false;
            }
        }
    }

    /// <summary>
    /// Sets the local Y rotation based on a normalized value (0-1) with multiplier
    /// </summary>
    /// <param name="normalizedValue">Normalized value between 0 and 1</param>
    public void SetNormalizedYRotation(float normalizedValue)
    {
        // Clamp the input to ensure it's between 0 and 1
        normalizedValue = Mathf.Clamp01(normalizedValue);

        // Apply multiplier (clamped result may exceed 1.0 which is fine)
        normalizedValue *= multiplier;

        // Map the normalized value to rotation degrees, wrapping around 360
        float rotationDegrees = (normalizedValue * 360f) % 360f;

        // Create a new rotation with the calculated Y value
        targetRotation = Quaternion.Euler(
            transform.localEulerAngles.x,
            rotationDegrees,
            transform.localEulerAngles.z
        );

        // Activate lerping
        isLerping = true;
    }
}