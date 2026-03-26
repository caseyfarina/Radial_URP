using UnityEngine;
using System.Collections;

public class RandomRotationAnimator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField, Range(0f, 360f)] private float minRotationDegrees = 100f;
    [SerializeField, Range(0f, 360f)] private float maxRotationDegrees = 300f;
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 4f;
    [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Axis Settings")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.X;
    
    private bool isAnimating = false;
    
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }
    
    /// <summary>
    /// Starts a random rotation animation based on the configured settings
    /// </summary>
    public void RotateRandomly()
    {
        if (isAnimating)
        {
            Debug.LogWarning("Rotation animation already in progress!");
            return;
        }
        
        // Generate random rotation angle within the specified range
        float rotationAmount = Random.Range(minRotationDegrees, maxRotationDegrees);
        
        // Start the rotation coroutine
        StartCoroutine(AnimateRotation(rotationAmount));
    }
    
    /// <summary>
    /// Starts a rotation animation with a specific rotation amount
    /// </summary>
    /// <param name="rotationAmount">The amount to rotate in degrees</param>
    public void RotateSpecificAmount(float rotationAmount)
    {
        if (isAnimating)
        {
            Debug.LogWarning("Rotation animation already in progress!");
            return;
        }
        
        StartCoroutine(AnimateRotation(rotationAmount));
    }
    
    private IEnumerator AnimateRotation(float rotationAmount)
    {
        isAnimating = true;
        
        // Store the starting rotation
        Quaternion startRotation = transform.rotation;
        
        // Create target rotation based on the selected axis
        Vector3 rotationVector = Vector3.zero;
        
        switch (rotationAxis)
        {
            case RotationAxis.X:
                rotationVector = new Vector3(rotationAmount, 0f, 0f);
                break;
            case RotationAxis.Y:
                rotationVector = new Vector3(0f, rotationAmount, 0f);
                break;
            case RotationAxis.Z:
                rotationVector = new Vector3(0f, 0f, rotationAmount);
                break;
        }
        
        // Calculate target rotation
        Quaternion targetRotation = startRotation * Quaternion.Euler(rotationVector);
        
        float elapsedTime = 0f;
        
        // Animate the rotation over time
        while (elapsedTime < animationDuration)
        {
            // Calculate normalized time and apply easing curve
            float normalizedTime = elapsedTime / animationDuration;
            float curveValue = easingCurve.Evaluate(normalizedTime);
            
            // Interpolate between start and target rotations
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, curveValue);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end at exactly the target rotation
        transform.rotation = targetRotation;
        
        isAnimating = false;
    }
    
    /// <summary>
    /// Checks if a rotation animation is currently in progress
    /// </summary>
    public bool IsAnimating()
    {
        return isAnimating;
    }
}
