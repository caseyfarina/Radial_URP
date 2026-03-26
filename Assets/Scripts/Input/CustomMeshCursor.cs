using UnityEngine;

/// <summary>
/// Replaces the standard mouse cursor with a custom 3D mesh
/// </summary>
public class CustomMeshCursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private bool hideCursorOnStart = true;
    [SerializeField] private Vector3 cursorOffset = Vector3.zero;
    [SerializeField] private Vector3 customRotation = Vector3.zero;
    [SerializeField] private float meshScale = 1.0f;
    
    [Header("Visual Options")]
    [SerializeField] private Material cursorMaterial;
    [SerializeField] private Color cursorColor = Color.white;
    [SerializeField] private bool useCustomMaterial = false;
    
    [Header("Animation Options")]
    [SerializeField] private bool enableRotation = false;
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 90, 0);
    [SerializeField] private bool enableHoverScale = false;
    [SerializeField] private float hoverScaleFactor = 1.2f;
    [SerializeField] private float hoverScaleSpeed = 5f;
    
    // Runtime variables
    private Camera mainCamera;
    private Vector3 originalScale;
    private bool isScalingUp = false;
    private float currentHoverScale = 1.0f;
    
    private void Awake()
    {
        Initialize();
    }
    
    private void Start()
    {
        // Hide standard cursor if needed
        if (hideCursorOnStart)
        {
            Cursor.visible = false;
        }
        
        // Store reference to the camera and original scale
        mainCamera = Camera.main;
        originalScale = transform.localScale;
        
        // Apply custom material if needed
        if (useCustomMaterial && cursorMaterial != null && meshRenderer != null)
        {
            meshRenderer.material = cursorMaterial;
            meshRenderer.material.color = cursorColor;
        }
        else if (meshRenderer != null)
        {
            // Just apply color to existing material
            meshRenderer.material.color = cursorColor;
        }
    }
    
    private void Update()
    {
        UpdateCursorPosition();
        
        if (enableRotation)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
        
        if (enableHoverScale)
        {
            UpdateHoverScaleEffect();
        }
    }
    
    private void Initialize()
    {
        // Try to get components if not assigned
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
            
            // If still null, try to find in children
            if (meshFilter == null)
                meshFilter = GetComponentInChildren<MeshFilter>();
            
            // If still null, add a new one with default mesh
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
                meshFilter.mesh = CreateDefaultCursorMesh();
            }
        }
        
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            
            // If still null, try to find in children
            if (meshRenderer == null)
                meshRenderer = GetComponentInChildren<MeshRenderer>();
            
            // If still null, add a new one
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
                meshRenderer.material = new Material(Shader.Find("Standard"));
            }
        }
        
        // Apply scale
        transform.localScale = Vector3.one * meshScale;
        
        // Apply custom rotation
        transform.rotation = Quaternion.Euler(customRotation);
    }
    
    private void UpdateCursorPosition()
    {
        // Get mouse position in screen space
        Vector3 mousePos = Input.mousePosition;
        
        // Convert to world space
        if (mainCamera != null)
        {
            // Set z position for correct world space conversion (distance from camera)
            mousePos.z = 10f;
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
            
            // Apply position with offset
            transform.position = worldPos + cursorOffset;
        }
    }
    
    private void UpdateHoverScaleEffect()
    {
        // Toggle scaling direction on click
        if (Input.GetMouseButtonDown(0))
        {
            isScalingUp = !isScalingUp;
        }
        
        // Calculate target scale
        float targetScale = isScalingUp ? hoverScaleFactor : 1.0f;
        
        // Smoothly animate current scale factor
        currentHoverScale = Mathf.Lerp(currentHoverScale, targetScale, Time.deltaTime * hoverScaleSpeed);
        
        // Apply scale
        transform.localScale = originalScale * currentHoverScale;
    }
    
    private Mesh CreateDefaultCursorMesh()
    {
        // Create a simple arrow-like cursor mesh if none is provided
        Mesh mesh = new Mesh();
        
        // Create an arrow shape pointing up-right
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 0, 0),      // 0: origin
            new Vector3(0.5f, 0.5f, 0), // 1: arrow tip
            new Vector3(0, 0.4f, 0),    // 2: top barb
            new Vector3(0.4f, 0, 0),    // 3: right barb
            new Vector3(0.25f, 0.25f, 0) // 4: center
        };
        
        // Define triangles
        int[] triangles = new int[]
        {
            0, 1, 4, // Main arrow body triangle 1
            1, 2, 4, // Upper triangle
            4, 3, 0  // Lower triangle
        };
        
        // Set vertices and triangles
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        
        // Generate normals
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    /// <summary>
    /// Sets a new mesh for the cursor
    /// </summary>
    /// <param name="newMesh">The mesh to use as cursor</param>
    public void SetCursorMesh(Mesh newMesh)
    {
        if (meshFilter != null && newMesh != null)
        {
            meshFilter.mesh = newMesh;
        }
    }
    
    /// <summary>
    /// Sets a new material for the cursor
    /// </summary>
    /// <param name="newMaterial">The material to use</param>
    public void SetCursorMaterial(Material newMaterial)
    {
        if (meshRenderer != null && newMaterial != null)
        {
            meshRenderer.material = newMaterial;
        }
    }
    
    /// <summary>
    /// Sets cursor color
    /// </summary>
    /// <param name="newColor">Color to apply</param>
    public void SetCursorColor(Color newColor)
    {
        cursorColor = newColor;
        
        if (meshRenderer != null)
        {
            meshRenderer.material.color = newColor;
        }
    }
    
    /// <summary>
    /// Toggle standard cursor visibility
    /// </summary>
    /// <param name="visible">Whether system cursor should be visible</param>
    public void SetSystemCursorVisibility(bool visible)
    {
        Cursor.visible = visible;
    }
    
    /// <summary>
    /// Sets custom offset for the mesh cursor
    /// </summary>
    /// <param name="offset">Offset from mouse position</param>
    public void SetCursorOffset(Vector3 offset)
    {
        cursorOffset = offset;
    }
}
