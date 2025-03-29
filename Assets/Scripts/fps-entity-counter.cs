using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Displays the current FPS and Entity count in TextMeshPro objects.
/// Entity count refreshes every half-second and counts all GameObjects with the "Entity" tag.
/// </summary>
public class FPSAndEntityCounter : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private TextMeshProUGUI fpsDisplay;
    [SerializeField] private TextMeshProUGUI entityCountDisplay;
    
    [Header("FPS Settings")]
    [SerializeField] private float fpsRefreshRate = 0.1f;
    [SerializeField] private string fpsFormat = "FPS: {0}";
    [SerializeField] private Color goodFpsColor = Color.green;
    [SerializeField] private Color warningFpsColor = Color.yellow;
    [SerializeField] private Color badFpsColor = Color.red;
    [SerializeField] private int goodFpsThreshold = 60;
    [SerializeField] private int warningFpsThreshold = 30;
    
    [Header("Entity Counter Settings")]
    [SerializeField] private float entityRefreshRate = 0.5f;
    [SerializeField] private string entityFormat = "Entities: {0}";
    
    private float fpsCounter;
    private float fpsTimer;
    private int frameCount;
    
    private float entityTimer;
    private int entityCount;
    
    private void Start()
    {
        // Validate that text displays are assigned
        if (fpsDisplay == null)
        {
            Debug.LogError("FPS Display TextMeshPro is not assigned!");
        }
        
        if (entityCountDisplay == null)
        {
            Debug.LogError("Entity Count Display TextMeshPro is not assigned!");
        }
        
        // Initialize timers
        fpsTimer = 0f;
        entityTimer = 0f;
        
        // Initial entity count
        RefreshEntityCount();
    }
    
    private void Update()
    {
        CalculateFPS();
        
        // Check if it's time to refresh entity count
        entityTimer += Time.deltaTime;
        if (entityTimer >= entityRefreshRate)
        {
            RefreshEntityCount();
            entityTimer = 0f;
        }
    }
    
    private void CalculateFPS()
    {
        // Increment frame count
        frameCount++;
        
        // Add time to the timer
        fpsTimer += Time.deltaTime;
        
        // When we reach the refresh rate, calculate FPS and reset
        if (fpsTimer >= fpsRefreshRate)
        {
            // Calculate frames per second
            fpsCounter = frameCount / fpsTimer;
            
            // Round to nearest integer
            int fps = Mathf.RoundToInt(fpsCounter);
            
            // Update the display if assigned
            if (fpsDisplay != null)
            {
                fpsDisplay.text = string.Format(fpsFormat, fps);
                
                // Set color based on performance thresholds
                if (fps >= goodFpsThreshold)
                {
                    fpsDisplay.color = goodFpsColor;
                }
                else if (fps >= warningFpsThreshold)
                {
                    fpsDisplay.color = warningFpsColor;
                }
                else
                {
                    fpsDisplay.color = badFpsColor;
                }
            }
            
            // Reset counters
            frameCount = 0;
            fpsTimer = 0f;
        }
    }
    
    private void RefreshEntityCount()
    {
        // Find all objects with the "Entity" tag
        GameObject[] entities = GameObject.FindGameObjectsWithTag("Entity");
        entityCount = entities.Length;
        
        // Update the display if assigned
        if (entityCountDisplay != null)
        {
            entityCountDisplay.text = string.Format(entityFormat, entityCount);
        }
    }
    
    /// <summary>
    /// Gets the current FPS value
    /// </summary>
    public int GetCurrentFPS()
    {
        return Mathf.RoundToInt(fpsCounter);
    }
    
    /// <summary>
    /// Gets the current entity count
    /// </summary>
    public int GetEntityCount()
    {
        return entityCount;
    }
    
    /// <summary>
    /// Force a refresh of the entity count
    /// </summary>
    public void ForceEntityCountRefresh()
    {
        RefreshEntityCount();
    }
}
