using UnityEngine;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// Shader-based world space grid renderer for the sprite-based graph system
    /// </summary>
    public class SpriteGraphGrid : MonoBehaviour
    {
        [Header("Grid Settings")]
        public Color gridColor = new Color(1f, 1f, 1f, 0.12f);
        public Color dotGridColor = new Color(1f, 1f, 1f, 0.5f);
        public float gridSpacing = 5f;
        public float dotGridSpacing = 0.2f;
        public float lineWidth = 1f;
        public float dotSize = 1f;
        
        [Header("Shader Grid")]
        public Shader gridShader;
        
        private SpriteRuntimeGraph graph;
        private Camera targetCamera;
        private Material gridMaterial;
        private MeshRenderer gridRenderer;
        private GameObject gridQuad;
        
        // Grid bounds calculation
        private Bounds lastGridBounds;
        private float lastCameraSize;
        private bool needsUpdate = true;
        
        public void Initialize(SpriteRuntimeGraph graph)
        {
            this.graph = graph;
            this.targetCamera = graph.GraphCamera;
            
            SetupGridMaterial();
            CreateGridQuad();
            UpdateGrid();
        }
        
        private void SetupGridMaterial()
        {
            // Find or use provided grid shader
            if (gridShader == null)
            {
                gridShader = Shader.Find("RuntimeGraph/GridShader");
                if (gridShader == null)
                {
                    Debug.LogError("GridShader not found! Please ensure RuntimeGraph/GridShader is available.");
                    gridShader = Shader.Find("Sprites/Default"); // Fallback
                }
            }
            
            // Create material with the grid shader
            gridMaterial = new Material(gridShader);
            gridMaterial.SetColor("_GridColor", gridColor);
            gridMaterial.SetColor("_DotColor", dotGridColor);
            gridMaterial.SetFloat("_GridSpacing", gridSpacing);
            gridMaterial.SetFloat("_DotSpacing", dotGridSpacing);
            gridMaterial.SetFloat("_LineWidth", lineWidth);
            gridMaterial.SetFloat("_DotSize", dotSize);
        }
        
        private void CreateGridQuad()
        {
            // Create a large quad to cover the grid area
            gridQuad = new GameObject("GridQuad");
            gridQuad.transform.SetParent(transform);
            
            // Add mesh filter and renderer
            var meshFilter = gridQuad.AddComponent<MeshFilter>();
            gridRenderer = gridQuad.AddComponent<MeshRenderer>();
            
            // Create a simple quad mesh
            meshFilter.mesh = CreateQuadMesh();
            gridRenderer.material = gridMaterial;
            gridRenderer.sortingOrder = 0; // Behind everything
            
            // Position the quad at world origin
            gridQuad.transform.position = Vector3.zero;
            gridQuad.transform.rotation = Quaternion.identity;
        }
        
        private Mesh CreateQuadMesh()
        {
            var mesh = new Mesh();
            mesh.name = "GridQuad";
            
            // Create a large quad that covers a wide area
            float size = 10000f; // Very large to cover any reasonable camera view
            
            Vector3[] vertices = {
                new(-size, -size, 0),
                new(size, -size, 0),
                new(size, size, 0),
                new(-size, size, 0)
            };
            
            Vector2[] uv = {
                new(0, 0),
                new(1, 0),
                new(1, 1),
                new(0, 1)
            };
            
            int[] triangles = new int[6]
            {
                0, 2, 1,
                0, 3, 2
            };
            
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        
        private void Update()
        {
            if (ShouldUpdateGrid())
            {
                UpdateGrid();
            }
        }
        
        private bool ShouldUpdateGrid()
        {
            if (needsUpdate) return true;
            if (targetCamera == null) return false;
            
            // Check if camera has moved significantly or zoom changed
            float currentSize = targetCamera.orthographicSize;
            Vector3 currentPos = targetCamera.transform.position;
            
            if (Mathf.Abs(currentSize - lastCameraSize) > 0.1f)
            {
                lastCameraSize = currentSize;
                return true;
            }
            
            // Check if camera moved more than half a grid spacing
            float moveThreshold = gridSpacing * 0.5f;
            if (Vector3.Distance(currentPos, transform.position) > moveThreshold)
            {
                return true;
            }
            
            return false;
        }
        
        public void UpdateGrid()
        {
            if (targetCamera == null || gridMaterial == null) return;
            
            needsUpdate = false;
            
            // Update shader parameters
            float zoomLevel = 1f / targetCamera.orthographicSize;
            Vector3 cameraPos = targetCamera.transform.position;
            
            // Update shader properties
            gridMaterial.SetColor("_GridColor", gridColor);
            gridMaterial.SetColor("_DotColor", dotGridColor);
            gridMaterial.SetFloat("_GridSpacing", gridSpacing);
            gridMaterial.SetFloat("_DotSpacing", dotGridSpacing);
            gridMaterial.SetFloat("_LineWidth", lineWidth);
            gridMaterial.SetFloat("_DotSize", dotSize);
            gridMaterial.SetFloat("_ZoomLevel", zoomLevel);
            gridMaterial.SetVector("_CameraPosition", cameraPos);
            gridMaterial.SetFloat("_CameraSize", targetCamera.orthographicSize);
            
            // Update grid quad position to follow camera (optional optimization)
            if (gridQuad != null)
            {
                gridQuad.transform.position = new Vector3(cameraPos.x, cameraPos.y, 0);
            }
        }
        
        private Bounds GetVisibleBounds()
        {
            if (targetCamera == null)
            {
                return new Bounds(Vector3.zero, Vector3.one * 20f);
            }
            
            float cameraHeight = targetCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * targetCamera.aspect;
            
            Vector3 cameraPos = targetCamera.transform.position;
            Vector3 size = new Vector3(cameraWidth, cameraHeight, 0);
            
            // Add some padding for smooth scrolling
            float padding = gridSpacing * 2f;
            size += Vector3.one * padding;
            
            return new Bounds(cameraPos, size);
        }
        
        public void SetGridSpacing(float spacing)
        {
            gridSpacing = spacing;
            needsUpdate = true;
        }
        
        public void SetGridColors(Color color)
        {
            gridColor = color;
            
            // Update shader material color
            if (gridMaterial != null)
            {
                gridMaterial.SetColor("_GridColor", gridColor);
            }
        }
        
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
        
        public Vector3 SnapToGrid(Vector3 worldPosition, bool useSmallGrid = true)
        {
            float spacing = gridSpacing;
            
            // Use the same calculation as the shader's grid line positioning
            // Shader uses: frac(coord / spacing - 0.5) - 0.5 = 0 at grid lines
            // This means grid lines occur at positions where (coord / spacing - 0.5) is an integer
            // So coord = (integer + 0.5) * spacing, or coord = Round(coord / spacing) * spacing
            float snappedX = Mathf.Round(worldPosition.x / spacing) * spacing;
            float snappedY = Mathf.Round(worldPosition.y / spacing) * spacing;
            
            return new Vector3(snappedX, snappedY, worldPosition.z);
        }
        
        public bool IsPointOnGrid(Vector3 worldPosition, float tolerance = 0.1f, bool useSmallGrid = true)
        {
            float spacing = gridSpacing;
            
            float remainderX = Mathf.Abs(worldPosition.x % spacing);
            float remainderY = Mathf.Abs(worldPosition.y % spacing);
            
            return (remainderX < tolerance || remainderX > spacing - tolerance) &&
                   (remainderY < tolerance || remainderY > spacing - tolerance);
        }
        
        private void OnDestroy()
        {
            // Cleanup shader material and grid quad
            if (gridMaterial != null)
            {
                DestroyImmediate(gridMaterial);
            }
            
            if (gridQuad != null)
            {
                DestroyImmediate(gridQuad);
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!enabled) return;
            
            // Draw grid in scene view for debugging
            Gizmos.color = gridColor;
            
            Bounds bounds = GetVisibleBounds();
            float minX = Mathf.Floor(bounds.min.x / gridSpacing) * gridSpacing;
            float maxX = Mathf.Ceil(bounds.max.x / gridSpacing) * gridSpacing;
            float minY = Mathf.Floor(bounds.min.y / gridSpacing) * gridSpacing;
            float maxY = Mathf.Ceil(bounds.max.y / gridSpacing) * gridSpacing;
            
            // Draw vertical lines
            for (float x = minX; x <= maxX; x += gridSpacing)
            {
                Gizmos.DrawLine(new Vector3(x, bounds.min.y, 0), new Vector3(x, bounds.max.y, 0));
            }
            
            // Draw horizontal lines  
            for (float y = minY; y <= maxY; y += gridSpacing)
            {
                Gizmos.DrawLine(new Vector3(bounds.min.x, y, 0), new Vector3(bounds.max.x, y, 0));
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw additional debug info when selected
            if (targetCamera == null) return;
            
            Gizmos.color = Color.yellow;
            Bounds visibleBounds = GetVisibleBounds();
            Gizmos.DrawWireCube(visibleBounds.center, visibleBounds.size);
        }
    }
}