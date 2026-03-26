using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simple rotation controller with configurable axes.
/// </summary>
public class RotationController : MonoBehaviour
{
    // Which axes to rotate around
    public bool rotateX = false;
    public bool rotateY = true;
    public bool rotateZ = false;

    [Tooltip("Whether to automatically start rotating when the game begins")]
    public bool autoRotate = false;

    [Tooltip("Rotation speed in degrees per second when auto-rotating")]
    public float autoRotationSpeed = 30f;

    [Tooltip("Multiplier to adjust the rotation speed")]
    [Range(0.1f, 500f)]
    public float speedMultiplier = 1f;

    [Tooltip("Minimum value for random speed multiplier")]
    public float minRandomMultiplier = 0.5f;

    [Tooltip("Maximum value for random speed multiplier")]
    public float maxRandomMultiplier = 2.0f;

    // Current rotation speed in degrees per second
    private float rotationSpeed = 0f;

    // Cached transform for better performance
    private Transform cachedTransform;
    private Vector3 rotationVector = Vector3.zero;

    private void Awake()
    {
        cachedTransform = transform;
        UpdateRotationVector();
    }

    private void Start()
    {
        // Initialize auto rotation if enabled
        if (autoRotate)
        {
            rotationSpeed = autoRotationSpeed;
        }
    }

    private void Update()
    {
        // Apply rotation if there's any speed
        if (rotationSpeed != 0f)
        {
            cachedTransform.Rotate(rotationVector * (rotationSpeed * speedMultiplier * Time.deltaTime));
        }
    }

    /// <summary>
    /// Updates the rotation vector based on the selected axes.
    /// </summary>
    private void UpdateRotationVector()
    {
        rotationVector.x = rotateX ? 1f : 0f;
        rotationVector.y = rotateY ? 1f : 0f;
        rotationVector.z = rotateZ ? 1f : 0f;

        // Normalize if more than one axis is selected
        if (rotationVector.sqrMagnitude > 1.1f)
        {
            rotationVector.Normalize();
        }
    }

    /// <summary>
    /// Set clockwise rotation speed (positive value).
    /// This can be connected to Unity events that pass a float parameter.
    /// </summary>
    /// <param name="speed">Rotation speed in degrees per second</param>
    public void SetClockwiseSpeed(float speed)
    {
        rotationSpeed = speed;
    }

    /// <summary>
    /// Set counter-clockwise rotation speed (positive value).
    /// This can be connected to Unity events that pass a float parameter.
    /// </summary>
    /// <param name="speed">Rotation speed in degrees per second</param>
    public void SetCounterClockwiseSpeed(float speed)
    {
        rotationSpeed = -speed;
    }

    /// <summary>
    /// Set the speed multiplier value.
    /// This can be connected to Unity events that pass a float parameter.
    /// </summary>
    /// <param name="multiplier">Speed multiplier value</param>
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0.1f, multiplier);
    }

    /// <summary>
    /// Randomizes the speed multiplier between the min and max values.
    /// This can be connected to Unity events.
    /// </summary>
    public void RandomizeSpeedMultiplier()
    {
        speedMultiplier = Random.Range(minRandomMultiplier, maxRandomMultiplier);
    }

    /// <summary>
    /// Toggle auto rotation on or off.
    /// </summary>
    /// <param name="enableAutoRotate">Whether auto rotation should be enabled</param>
    public void SetAutoRotate(bool enableAutoRotate)
    {
        autoRotate = enableAutoRotate;

        if (autoRotate)
        {
            rotationSpeed = autoRotationSpeed;
        }
        else if (rotationSpeed == autoRotationSpeed)
        {
            // Only reset rotation speed if it's the auto rotation speed
            // (to avoid interfering with speeds set through other methods)
            rotationSpeed = 0f;
        }
    }

    /// <summary>
    /// Increment the rotation speed by the specified amount.
    /// </summary>
    /// <param name="increment">Amount to increase the rotation speed by</param>
    public void IncrementRotationSpeed(float increment = 5f)
    {
        rotationSpeed += increment;
    }

    /// <summary>
    /// Decrement the rotation speed by the specified amount.
    /// </summary>
    /// <param name="decrement">Amount to decrease the rotation speed by</param>
    public void DecrementRotationSpeed(float decrement = 5f)
    {
        rotationSpeed -= decrement;
    }

    /// <summary>
    /// Reverses the current rotation direction by multiplying the rotation speed by -1.
    /// </summary>
    public void ReverseRotationDirection()
    {
        rotationSpeed = -rotationSpeed;
    }

    /// <summary>
    /// Called when component values are changed in the Inspector.
    /// </summary>
    private void OnValidate()
    {
        UpdateRotationVector();

        // Ensure min is not greater than max for random values
        if (minRandomMultiplier > maxRandomMultiplier)
        {
            minRandomMultiplier = maxRandomMultiplier;
        }
    }
}