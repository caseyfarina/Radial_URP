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