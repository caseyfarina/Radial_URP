using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Static registry for tracking Entity GameObjects without FindGameObjectsWithTag.
/// Attach this to any GameObject with the "Entity" tag to auto-register/unregister.
/// </summary>
public class EntityRegistry : MonoBehaviour
{
    private static readonly List<GameObject> entities = new List<GameObject>();

    public static IReadOnlyList<GameObject> Entities => entities;
    public static int Count => entities.Count;

    private void OnEnable()
    {
        entities.Add(gameObject);
    }

    private void OnDisable()
    {
        entities.Remove(gameObject);
    }
}
