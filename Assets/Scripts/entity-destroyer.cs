using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Gradually destroys all GameObjects with the "Entity" tag in the scene.
/// </summary>
public class EntityDestroyer : MonoBehaviour
{
    [Header("Destruction Settings")]
    [Tooltip("Time in seconds between each entity destruction")]
    [SerializeField] private float timeBetweenDestruction = 0.2f;

    [Tooltip("Whether to log destruction information to the console")]
    [SerializeField] private bool logDestruction = true;

    [Tooltip("Optional UnityEvent triggered when an entity is destroyed")]
    [SerializeField] private UnityEngine.Events.UnityEvent onEntityDestroyed;

    [Tooltip("Optional UnityEvent triggered when all entities are destroyed")]
    [SerializeField] private UnityEngine.Events.UnityEvent onAllEntitiesDestroyed;

    // Track if a destruction sequence is currently running
    private bool isDestroyingEntities = false;

    /// <summary>
    /// Start the destruction sequence, destroying one entity every [timeBetweenDestruction] seconds
    /// </summary>
    public void DestroyAllEntitiesGradually()
    {
        // Don't start a new sequence if one is already running
        if (isDestroyingEntities)
        {
            Debug.LogWarning("Entity destruction sequence already in progress!");
            return;
        }

        StartCoroutine(DestroyEntitiesCoroutine());
    }

    /// <summary>
    /// Coroutine that handles the gradual destruction of entities
    /// </summary>
    private IEnumerator DestroyEntitiesCoroutine()
    {
        isDestroyingEntities = true;
        
        // Get all entities with the specified tag
        GameObject[] entities = GameObject.FindGameObjectsWithTag("Entity");
        
        int totalCount = entities.Length;
        int destroyedCount = 0;
        
        if (logDestruction)
        {
            Debug.Log($"Found {totalCount} entities to destroy");
        }
        
        // If no entities found, skip the process
        if (totalCount == 0)
        {
            if (logDestruction)
            {
                Debug.Log("No entities found to destroy");
            }
            
            onAllEntitiesDestroyed?.Invoke();
            isDestroyingEntities = false;
            yield break;
        }
        
        // Destroy one entity at a time with a delay between each
        foreach (GameObject entity in entities)
        {
            // Skip null entities (in case some were destroyed by other means)
            if (entity == null) continue;
            
            // Destroy the entity
            Destroy(entity);
            destroyedCount++;
            
            if (logDestruction)
            {
                Debug.Log($"Destroyed entity {destroyedCount}/{totalCount}: {entity.name}");
            }
            
            // Trigger the entity destroyed event
            onEntityDestroyed?.Invoke();
            
            // Wait for the specified time before destroying the next entity
            yield return new WaitForSeconds(timeBetweenDestruction);
        }
        
        if (logDestruction)
        {
            Debug.Log($"Completed destroying {destroyedCount} entities");
        }
        
        // Trigger the all entities destroyed event
        onAllEntitiesDestroyed?.Invoke();
        
        isDestroyingEntities = false;
    }
    
    /// <summary>
    /// Immediately destroys all entities with the "Entity" tag
    /// </summary>
    public void DestroyAllEntitiesImmediately()
    {
        // Stop any running coroutine
        if (isDestroyingEntities)
        {
            StopAllCoroutines();
            isDestroyingEntities = false;
        }
        
        GameObject[] entities = GameObject.FindGameObjectsWithTag("Entity");
        int count = entities.Length;
        
        foreach (GameObject entity in entities)
        {
            if (entity != null)
            {
                Destroy(entity);
            }
        }
        
        if (logDestruction)
        {
            Debug.Log($"Immediately destroyed {count} entities");
        }
        
        // Trigger events
        for (int i = 0; i < count; i++)
        {
            onEntityDestroyed?.Invoke();
        }
        
        onAllEntitiesDestroyed?.Invoke();
    }
    
    /// <summary>
    /// Returns the current count of entities with the "Entity" tag in the scene
    /// </summary>
    /// <returns>Number of entities</returns>
    public int GetEntityCount()
    {
        return GameObject.FindGameObjectsWithTag("Entity").Length;
    }
    
    /// <summary>
    /// Check if the entity destruction sequence is currently in progress
    /// </summary>
    /// <returns>True if destroying entities, false otherwise</returns>
    public bool IsDestroyingEntities()
    {
        return isDestroyingEntities;
    }
    
    /// <summary>
    /// Set the time between entity destructions
    /// </summary>
    /// <param name="seconds">Time in seconds</param>
    public void SetTimeBetweenDestruction(float seconds)
    {
        timeBetweenDestruction = Mathf.Max(0.01f, seconds);
    }
}