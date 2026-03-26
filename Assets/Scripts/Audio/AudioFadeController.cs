using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Reflection;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

/// <summary>
/// Controls audio volume fading with smoothing on binary input values (0 or 1)
/// Can also control post-processing parameters with the same smoothed value
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioFadeController : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField, Range(0.01f, 10f)] private float smoothingStrength = 2f;
    [SerializeField, Range(0f, 1f)] private float minVolumeThreshold = 0.01f;
    [SerializeField] private bool playOnAwake = false;
    [SerializeField, Range(0f, 1f)] private float maxVolume = 1f;
    [SerializeField, Tooltip("Animation curve that maps the smooth value to volume. Default is exponential.")]
    private AnimationCurve volumeCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 1f),
        new Keyframe(1f, 1f, 2f, 0f)
    );

    [Header("Post-Processing Mapping")]
    [SerializeField] private bool mapToPostProcessing = false;
    [SerializeField] private Volume postProcessingVolume;
    [SerializeField] private string selectedEffectType = "";
    [SerializeField] private string selectedParameterName = "";
    [SerializeField, Tooltip("Animation curve that maps the smooth value to the post-processing parameter.")]
    private AnimationCurve postProcessingCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 1f, 1f),
        new Keyframe(1f, 1f, 1f, 0f)
    );
    [SerializeField, Tooltip("Minimum value of the post-processing parameter.")]
    private float minParameterValue = 0f;
    [SerializeField, Tooltip("Maximum value of the post-processing parameter.")]
    private float maxParameterValue = 1f;
    [SerializeField, Tooltip("Invert the mapping (1->min, 0->max)")]
    private bool invertParameterMapping = false;

    [Header("Debug Settings")]
    [SerializeField] private bool debugLogging = false;

    // Internal state tracking
    private AudioSource audioSource;
    private float currentSmoothValue = 0f;
    private float targetValue = 0f;
    private bool wasPlaying = false;

    // Post-processing related variables
    private object postProcessingEffect;
    private PropertyInfo postProcessingProperty;
    private VolumeProfile volumeProfile;
    private List<string> availableEffectTypes = new List<string>();
    private Dictionary<string, List<string>> effectParameters = new Dictionary<string, List<string>>();

    private void Awake()
    {
        // Get reference to the AudioSource component
        audioSource = GetComponent<AudioSource>();

        // Set initial volume to 0 to prevent audio pop on startup
        audioSource.volume = 0f;

        // Disable playOnAwake on the AudioSource to manage playback ourselves
        audioSource.playOnAwake = false;

        // Initialize state
        currentSmoothValue = playOnAwake ? maxVolume : 0f;
        targetValue = playOnAwake ? 1f : 0f;

        // Start audio if playOnAwake is enabled
        if (playOnAwake)
        {
            audioSource.Play();
            wasPlaying = true;
        }

        // Initialize post-processing
        InitializePostProcessingProfile();
    }

    private void InitializePostProcessingProfile()
    {
        if (!mapToPostProcessing || postProcessingVolume == null) return;

        volumeProfile = postProcessingVolume.profile;
        if (volumeProfile == null)
        {
            Debug.LogWarning("[AudioFadeController] Post-processing volume has no profile assigned.");
            return;
        }

        // Try to get the currently selected effect
        if (!string.IsNullOrEmpty(selectedEffectType) && !string.IsNullOrEmpty(selectedParameterName))
        {
            FindEffectAndParameter();
        }
    }

    private void FindEffectAndParameter()
    {
        if (volumeProfile == null) return;

        // Reset references
        postProcessingEffect = null;

        // Try to find the selected effect in the profile
        System.Type effectType = System.Type.GetType(selectedEffectType);
        if (effectType == null)
        {
            Debug.LogWarning($"[AudioFadeController] Effect type {selectedEffectType} not found.");
            return;
        }

        var effectFound = false;
        object effect = null;

        // Manually look through components to find the effect
        foreach (var component in volumeProfile.components)
        {
            if (component.GetType().AssemblyQualifiedName == selectedEffectType)
            {
                effectFound = true;
                effect = component;
                break;
            }
        }

        if (effectFound && effect != null)
        {
            postProcessingEffect = effect;

            if (debugLogging)
            {
                Debug.Log($"[AudioFadeController] Found effect {effectType.Name}");
            }
        }
        else
        {
            Debug.LogWarning($"[AudioFadeController] Effect {effectType.Name} not found in profile");
        }
    }

    private void Update()
    {
        // Smoothly interpolate toward the target value
        float smoothDelta = (targetValue - currentSmoothValue) * smoothingStrength * Time.deltaTime;
        currentSmoothValue += smoothDelta;

        // Clamp to valid range
        currentSmoothValue = Mathf.Clamp01(currentSmoothValue);

        // Apply volume using the animation curve for non-linear mapping
        float curveValue = volumeCurve.Evaluate(currentSmoothValue);
        float newVolume = curveValue * maxVolume;
        audioSource.volume = newVolume;

        // Update post-processing parameter if enabled
        UpdatePostProcessingParameter(currentSmoothValue);

        // Check if we should start or stop the audio
        if (!wasPlaying && currentSmoothValue > minVolumeThreshold)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                wasPlaying = true;

                if (debugLogging)
                {
                    Debug.Log($"[AudioFadeController] Started playback. Volume: {newVolume:F3}");
                }
            }
        }
        else if (wasPlaying && currentSmoothValue <= minVolumeThreshold)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
                wasPlaying = false;

                if (debugLogging)
                {
                    Debug.Log($"[AudioFadeController] Stopped playback. Volume below threshold: {minVolumeThreshold:F3}");
                }
            }
        }
    }

    private void UpdatePostProcessingParameter(float smoothValue)
    {
        if (!mapToPostProcessing || postProcessingEffect == null) return;

        // Apply curve to the smoothed value
        float curveValue = postProcessingCurve.Evaluate(smoothValue);

        // Invert if requested
        if (invertParameterMapping)
        {
            curveValue = 1f - curveValue;
        }

        // Map from 0-1 to min-max range
        float parameterValue = Mathf.Lerp(minParameterValue, maxParameterValue, curveValue);

        try
        {
            // Try to find the selected parameter as a field first (most common in URP)
            System.Reflection.FieldInfo field = postProcessingEffect.GetType().GetField(selectedParameterName);
            if (field != null)
            {
                // Get the parameter object
                object parameterObj = field.GetValue(postProcessingEffect);
                if (parameterObj != null)
                {
                    // Try to find the 'value' field or property on the parameter
                    var valueField = parameterObj.GetType().GetField("value");
                    var valueProperty = parameterObj.GetType().GetProperty("value");

                    if (valueField != null)
                    {
                        // Convert value to the right type and set it
                        Type valueType = valueField.FieldType;
                        object convertedValue = ConvertToType(parameterValue, valueType);
                        valueField.SetValue(parameterObj, convertedValue);

                        if (debugLogging)
                        {
                            Debug.Log($"[AudioFadeController] Updated parameter field {selectedParameterName} to {parameterValue}");
                        }
                    }
                    else if (valueProperty != null)
                    {
                        // Convert value to the right type and set it
                        Type valueType = valueProperty.PropertyType;
                        object convertedValue = ConvertToType(parameterValue, valueType);
                        valueProperty.SetValue(parameterObj, convertedValue);

                        if (debugLogging)
                        {
                            Debug.Log($"[AudioFadeController] Updated parameter property {selectedParameterName} to {parameterValue}");
                        }
                    }
                }
            }
            else
            {
                // Try as property (fallback)
                PropertyInfo property = postProcessingEffect.GetType().GetProperty(selectedParameterName);
                if (property != null && property.CanWrite)
                {
                    object parameterObj = property.GetValue(postProcessingEffect);
                    if (parameterObj != null)
                    {
                        // Try to find the 'value' property on the parameter
                        PropertyInfo valueProperty = parameterObj.GetType().GetProperty("value");
                        if (valueProperty != null)
                        {
                            // Convert value to the right type and set it
                            Type valueType = valueProperty.PropertyType;
                            object convertedValue = ConvertToType(parameterValue, valueType);
                            valueProperty.SetValue(parameterObj, convertedValue);

                            if (debugLogging)
                            {
                                Debug.Log($"[AudioFadeController] Updated parameter property {selectedParameterName} to {parameterValue}");
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            if (debugLogging)
            {
                Debug.LogError($"[AudioFadeController] Error updating post-processing parameter: {e.Message}");
            }
        }
    }

    private object ConvertToType(float value, System.Type targetType)
    {
        // Handle various common parameter types
        if (targetType == typeof(float))
        {
            return value;
        }
        else if (targetType == typeof(int))
        {
            return (int)value;
        }
        else if (targetType == typeof(bool))
        {
            return value > 0.5f;
        }
        else if (targetType == typeof(Color))
        {
            return new Color(value, value, value, 1f);
        }
        else if (targetType == typeof(Vector2))
        {
            return new Vector2(value, value);
        }
        else if (targetType == typeof(Vector3))
        {
            return new Vector3(value, value, value);
        }
        else if (targetType == typeof(Vector4))
        {
            return new Vector4(value, value, value, value);
        }

        // Default case - try to convert to float
        return value;
    }

    /// <summary>
    /// Set the target value for volume fading (0 = off, 1 = on)
    /// </summary>
    /// <param name="value">Target value (0 or 1)</param>
    public void SetFadeTarget(float value)
    {
        // Normalize input to 0 or 1 (any non-zero value becomes 1)
        targetValue = value > 0 ? 1f : 0f;

        if (debugLogging)
        {
            Debug.Log($"[AudioFadeController] SetFadeTarget: {targetValue}");
        }
    }

    /// <summary>
    /// Change the smoothing strength at runtime
    /// </summary>
    /// <param name="strength">New smoothing strength value</param>
    public void SetSmoothingStrength(float strength)
    {
        smoothingStrength = Mathf.Max(0.01f, strength);
    }

    /// <summary>
    /// Change the maximum volume at runtime
    /// </summary>
    /// <param name="volume">New maximum volume (0-1)</param>
    public void SetMaxVolume(float volume)
    {
        maxVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Set a new animation curve for volume mapping
    /// </summary>
    /// <param name="curve">New animation curve</param>
    public void SetVolumeCurve(AnimationCurve curve)
    {
        if (curve != null && curve.length >= 2)
        {
            volumeCurve = curve;
        }
        else
        {
            Debug.LogWarning("[AudioFadeController] Invalid curve provided. Using current curve.");
        }
    }

    /// <summary>
    /// Set up post-processing mapping
    /// </summary>
    /// <param name="volume">Post-processing volume</param>
    /// <param name="effectType">Type name of the effect</param>
    /// <param name="parameterName">Name of the parameter</param>
    public void SetupPostProcessingMapping(Volume volume, string effectType, string parameterName)
    {
        postProcessingVolume = volume;
        selectedEffectType = effectType;
        selectedParameterName = parameterName;
        mapToPostProcessing = true;

        InitializePostProcessingProfile();
    }

    /// <summary>
    /// Set post-processing parameter range
    /// </summary>
    /// <param name="min">Minimum parameter value</param>
    /// <param name="max">Maximum parameter value</param>
    /// <param name="invert">Whether to invert the mapping</param>
    public void SetPostProcessingRange(float min, float max, bool invert = false)
    {
        minParameterValue = min;
        maxParameterValue = max;
        invertParameterMapping = invert;
    }

    /// <summary>
    /// Set post-processing curve
    /// </summary>
    /// <param name="curve">New animation curve for post-processing</param>
    public void SetPostProcessingCurve(AnimationCurve curve)
    {
        if (curve != null && curve.length >= 2)
        {
            postProcessingCurve = curve;
        }
    }

    /// <summary>
    /// Enable or disable post-processing mapping
    /// </summary>
    /// <param name="enable">Whether to enable post-processing mapping</param>
    public void EnablePostProcessingMapping(bool enable)
    {
        mapToPostProcessing = enable;
    }

    /// <summary>
    /// Get the current smoothed value
    /// </summary>
    /// <returns>Current smooth value (0-1)</returns>
    public float GetCurrentValue()
    {
        return currentSmoothValue;
    }

    /// <summary>
    /// Immediately set volume without smoothing
    /// </summary>
    /// <param name="volume">Volume value (0-1)</param>
    public void SetVolumeImmediate(float volume)
    {
        float normalizedVolume = Mathf.Clamp01(volume);
        currentSmoothValue = normalizedVolume;
        targetValue = normalizedVolume > 0 ? 1f : 0f;
        float curveValue = volumeCurve.Evaluate(normalizedVolume);
        audioSource.volume = curveValue * maxVolume;

        // Handle playback state
        if (normalizedVolume > minVolumeThreshold)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                wasPlaying = true;
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
                wasPlaying = false;
            }
        }
    }
}
