using UnityEngine;
using DG.Tweening;

/// <summary>
/// Optimized emission effect using Material Property Blocks and DOTween
/// </summary>
public class EmissionPulseEffect : MonoBehaviour
{
    [Header("Emission Settings")]
    [SerializeField] private AnimationCurve emissionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Range(0.1f, 5f)] private float emissionDuration = 1f;
    [SerializeField] private Color emissionColor = Color.white;
    [SerializeField, Range(0f, 10f)] private float emissionIntensity = 1f;
    [SerializeField] private bool debugMode = false;

    private MaterialPropertyBlock propertyBlock;
    private Renderer[] renderers;
    private Tween activeEmissionTween;

    // Store original emission values
    private Color[] originalEmissions;
    private bool[] hasEmission;

    // Value that will be tweened by DOTween
    private float currentEmissionValue = 0f;

    private void Awake()
    {
        // Initialize property block once
        propertyBlock = new MaterialPropertyBlock();

        // Cache renderers for better performance
        renderers = GetComponentsInChildren<Renderer>();

        // Initialize arrays for storing original values
        originalEmissions = new Color[renderers.Length];
        hasEmission = new bool[renderers.Length];

        // Ensure emission is enabled on all materials and cache original values
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].sharedMaterial != null && renderers[i].sharedMaterial.HasProperty("_EmissionColor"))
            {
                renderers[i].sharedMaterial.EnableKeyword("_EMISSION");

                // Get current property block values
                renderers[i].GetPropertyBlock(propertyBlock);

                // Cache the original emission value
                if (propertyBlock.HasColor("_EmissionColor"))
                {
                    originalEmissions[i] = propertyBlock.GetColor("_EmissionColor");
                }
                else
                {
                    // If not set in property block, get from shared material
                    originalEmissions[i] = renderers[i].sharedMaterial.GetColor("_EmissionColor");
                }

                hasEmission[i] = true;
                DebugLog($"Found emission-capable material on {renderers[i].name}");
            }
            else
            {
                hasEmission[i] = false;
            }
        }
    }

    /// <summary>
    /// Starts emission pulse effect using DOTween and efficient Material Property Blocks
    /// </summary>
    public void PlayEmission()
    {
        DebugLog("PlayEmission called");

        // Stop any current emission tween
        if (activeEmissionTween != null && activeEmissionTween.IsActive())
        {
            DebugLog("Interrupting current emission pulse");
            activeEmissionTween.Kill();
            activeEmissionTween = null;

            // Reset emission values to original immediately
            ResetEmissionToOriginal();
        }

        // Reset current emission value
        currentEmissionValue = 0f;

        // Create a new tween for the emission pulse
        activeEmissionTween = DOTween.To(
            () => currentEmissionValue,             // Getter
            x => {                                  // Setter
                currentEmissionValue = x;
                UpdateEmissionValue(x);
            },
            1f,                                     // Target value (full emission)
            emissionDuration                        // Duration
        )
        .SetEase(emissionCurve)                     // Use the custom animation curve
        .OnComplete(() => {
            DebugLog("Emission pulse complete");
            ResetEmissionToOriginal();
            activeEmissionTween = null;
        });
    }

    /// <summary>
    /// Updates all renderer emission values based on the current emission value
    /// </summary>
    private void UpdateEmissionValue(float value)
    {
        // Calculate emission color
        Color newEmission = emissionColor * value * emissionIntensity;

        // Update all renderers with property block
        for (int i = 0; i < renderers.Length; i++)
        {
            if (hasEmission[i])
            {
                renderers[i].GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_EmissionColor", newEmission);
                renderers[i].SetPropertyBlock(propertyBlock);
            }
        }
    }

    /// <summary>
    /// Resets emission values to their original state
    /// </summary>
    private void ResetEmissionToOriginal()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (hasEmission[i])
            {
                renderers[i].GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_EmissionColor", originalEmissions[i]);
                renderers[i].SetPropertyBlock(propertyBlock);
            }
        }

        DebugLog("Reset emission to original values");
    }

    /// <summary>
    /// Stops any active emission pulse and resets to original values
    /// </summary>
    public void StopEmission()
    {
        if (activeEmissionTween != null && activeEmissionTween.IsActive())
        {
            DebugLog("Stopping emission pulse");
            activeEmissionTween.Kill();
            activeEmissionTween = null;

            // Reset emission values to original
            ResetEmissionToOriginal();
        }
    }

    private void OnDestroy()
    {
        // Clean up tween if active
        if (activeEmissionTween != null && activeEmissionTween.IsActive())
        {
            activeEmissionTween.Kill();
            activeEmissionTween = null;
        }
    }

    // Helper method for debug logging
    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[EmissionPulseEffect] {message}");
        }
    }
}