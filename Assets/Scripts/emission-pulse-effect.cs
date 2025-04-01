using UnityEngine;
using System.Collections;

/// <summary>
/// Optimized emission effect using Material Property Blocks
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
    private Coroutine activeEmissionPulse;
    
    private void Awake()
    {
        // Initialize property block once
        propertyBlock = new MaterialPropertyBlock();
        
        // Cache renderers for better performance
        renderers = GetComponentsInChildren<Renderer>();
        
        // Ensure emission is enabled on all materials
        foreach (Renderer renderer in renderers)
        {
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_EmissionColor"))
            {
                renderer.sharedMaterial.EnableKeyword("_EMISSION");
            }
        }
    }
    
    /// <summary>
    /// Starts emission pulse effect using efficient Material Property Blocks
    /// </summary>
    public void PlayEmission()
    {
        DebugLog("PlayEmission called");
        
        // Stop any current emission pulse
        if (activeEmissionPulse != null)
        {
            StopCoroutine(activeEmissionPulse);
            activeEmissionPulse = null;
        }
        
        // Start emission pulse
        activeEmissionPulse = StartCoroutine(PulseEmissionCoroutine());
    }
    
    private IEnumerator PulseEmissionCoroutine()
    {
        // Store original emission values
        Color[] originalEmissions = new Color[renderers.Length];
        bool[] hasEmission = new bool[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].sharedMaterial != null && renderers[i].sharedMaterial.HasProperty("_EmissionColor"))
            {
                // Get current property block values
                renderers[i].GetPropertyBlock(propertyBlock);
                
                // Check if emission color is already set in the property block
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
        
        // Perform pulse animation
        float startTime = Time.time;
        
        while (Time.time < startTime + emissionDuration)
        {
            float progress = (Time.time - startTime) / emissionDuration;
            float curveValue = emissionCurve.Evaluate(progress);
            
            // Calculate emission color
            Color newEmission = emissionColor * curveValue * emissionIntensity;
            
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
            
            yield return null;
        }
        
        // Reset to original values
        for (int i = 0; i < renderers.Length; i++)
        {
            if (hasEmission[i])
            {
                renderers[i].GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_EmissionColor", originalEmissions[i]);
                renderers[i].SetPropertyBlock(propertyBlock);
            }
        }
        
        DebugLog("Emission pulse complete");
        activeEmissionPulse = null;
    }
    
    private void OnDestroy()
    {
        // Clean up coroutine if active
        if (activeEmissionPulse != null)
        {
            StopCoroutine(activeEmissionPulse);
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