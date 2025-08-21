using System;
using UnityEngine;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// Sprite-based connection component for rendering lines between nodes
    /// </summary>
    public class SpriteConnection : MonoBehaviour
    {
        [System.Serializable]
        public class ConnectionData
        {
            public string id = "";
            public string fromNodeId = "";
            public int fromAnchorIndex = 0;
            public string toNodeId = "";
            public int toAnchorIndex = 0;
            
            // Path behavior properties
            [Range(0.1f, 10f)]
            public float weight = 1f; // For weighted random behavior
            public int creationOrder = 0; // For sequential behavior
            
            // Path orientation preference to prevent flipping during zoom/animation
            public bool preferHorizontalFirst = true; // Store initial orientation preference
            private bool orientationInitialized = false; // Track if orientation has been set
            
            public void InitializeOrientation(Vector3 fromPos, Vector3 toPos)
            {
                if (!orientationInitialized)
                {
                    float deltaX = Mathf.Abs(toPos.x - fromPos.x);
                    float deltaY = Mathf.Abs(toPos.y - fromPos.y);
                    preferHorizontalFirst = deltaX > deltaY;
                    orientationInitialized = true;
                }
            }
        }
        
        [Header("Visual Settings")]
        public Color connectionColor = new Color(0.3f, 0.8f, 1f, 0.9f);
        public Color pendingConnectionColor = new Color(1f, 0.8f, 0.2f, 0.9f);
        public float lineWidth = 0.1f;
        public Material lineMaterial;
        
        [Header("Arrowhead Settings")]
        public float arrowheadSize = 0.3f;
        public Color arrowheadColor = Color.white;
        
        private SpriteRuntimeGraph graph;
        private ConnectionData connectionData;
        private LineRenderer lineRenderer;
        private SpriteRenderer arrowheadRenderer;
        private bool isPendingConnection;
        private Vector3 pendingEndPoint;
        
        public ConnectionData ConnectionDataInstance => connectionData;
        public bool IsPending => isPendingConnection;
        
        public void Initialize(SpriteRuntimeGraph graph, ConnectionData data)
        {
            this.graph = graph;
            this.connectionData = data;
            
            SetupLineRenderer();
            UpdateConnection();
        }
        
        public void InitializeAsPending(SpriteRuntimeGraph graph, string fromNodeId, int fromAnchorIndex)
        {
            this.graph = graph;
            this.connectionData = new ConnectionData
            {
                id = "pending",
                fromNodeId = fromNodeId,
                fromAnchorIndex = fromAnchorIndex
            };
            this.isPendingConnection = true;
            
            SetupLineRenderer();
            UpdatePendingConnection();
        }
        
        private void SetupLineRenderer()
        {
            // Get or create line renderer
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
            
            // Configure line renderer
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.sortingOrder = 5; // Below nodes but above grid
            
            // Set material
            if (lineMaterial != null)
            {
                lineRenderer.material = lineMaterial;
            }
            else
            {
                // Create default material
                lineRenderer.material = CreateDefaultMaterial();
            }
            
            // Set color
            Color targetColor = isPendingConnection ? pendingConnectionColor : connectionColor;
            lineRenderer.startColor = targetColor;
            lineRenderer.endColor = targetColor;
            
            // Set name
            gameObject.name = isPendingConnection ? "PendingConnection" : $"Connection ({connectionData.fromNodeId} -> {connectionData.toNodeId})";
        }
        
        private Material CreateDefaultMaterial()
        {
            // Create a simple unlit material for the line
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = Color.white;
            return mat;
        }
        
        private void Update()
        {
            if (isPendingConnection)
            {
                UpdatePendingConnection();
            }
            else
            {
                UpdateConnection();
            }
        }
        
        private void UpdateConnection()
        {
            if (graph == null || connectionData == null) return;
            
            // Get nodes
            var fromNode = graph.GetNode(connectionData.fromNodeId);
            var toNode = graph.GetNode(connectionData.toNodeId);
            
            if (fromNode == null || toNode == null)
            {
                // Hide line if nodes don't exist
                lineRenderer.enabled = false;
                return;
            }
            
            // Get anchor positions
            Vector3 fromPos = fromNode.GetAnchorWorldPosition(connectionData.fromAnchorIndex);
            Vector3 toPos = toNode.GetAnchorWorldPosition(connectionData.toAnchorIndex);
            
            // Calculate right-angle path points
            Vector3[] pathPoints = CalculateRightAnglePath(fromPos, toPos);
            
            // Update line positions with right-angle path
            lineRenderer.enabled = true;
            lineRenderer.positionCount = pathPoints.Length;
            for (int i = 0; i < pathPoints.Length; i++)
            {
                lineRenderer.SetPosition(i, pathPoints[i]);
            }
        }
        
        private void UpdatePendingConnection()
        {
            if (graph == null || connectionData == null) return;
            
            // Get from node
            var fromNode = graph.GetNode(connectionData.fromNodeId);
            if (fromNode == null)
            {
                lineRenderer.enabled = false;
                return;
            }
            
            // Get from position
            Vector3 fromPos = fromNode.GetAnchorWorldPosition(connectionData.fromAnchorIndex);
            
            // Use pending end point or mouse position
            Vector3 toPos = pendingEndPoint;
            if (toPos == Vector3.zero)
            {
                toPos = graph.ScreenToWorld(Input.mousePosition);
            }
            
            // Calculate right-angle path points
            Vector3[] pathPoints = CalculateRightAnglePath(fromPos, toPos);
            
            // Update line positions with right-angle path
            lineRenderer.enabled = true;
            lineRenderer.positionCount = pathPoints.Length;
            for (int i = 0; i < pathPoints.Length; i++)
            {
                lineRenderer.SetPosition(i, pathPoints[i]);
            }
        }
        
        public void SetPendingEndPoint(Vector3 worldPos)
        {
            pendingEndPoint = worldPos;
        }
        
        /// <summary>
        /// Calculates a path using only straight lines and right angles (no diagonals)
        /// </summary>
        private Vector3[] CalculateRightAnglePath(Vector3 fromPos, Vector3 toPos)
        {
            // If points are already aligned horizontally or vertically, use direct path
            if (Mathf.Approximately(fromPos.x, toPos.x) || Mathf.Approximately(fromPos.y, toPos.y))
            {
                return new Vector3[] { fromPos, toPos };
            }
            
            // Initialize orientation preference if this is a permanent connection
            if (!isPendingConnection && connectionData != null)
            {
                connectionData.InitializeOrientation(fromPos, toPos);
            }
            
            Vector3 intermediatePoint;
            
            // For pending connections, use dynamic calculation
            if (isPendingConnection)
            {
                // Choose the intermediate point based on which direction requires less travel
                float deltaX = Mathf.Abs(toPos.x - fromPos.x);
                float deltaY = Mathf.Abs(toPos.y - fromPos.y);
                
                if (deltaX > deltaY)
                {
                    intermediatePoint = new Vector3(toPos.x, fromPos.y, fromPos.z);
                }
                else
                {
                    intermediatePoint = new Vector3(fromPos.x, toPos.y, fromPos.z);
                }
            }
            else
            {
                // For permanent connections, use stored orientation preference to prevent flipping
                if (connectionData.preferHorizontalFirst)
                {
                    intermediatePoint = new Vector3(toPos.x, fromPos.y, fromPos.z);
                }
                else
                {
                    intermediatePoint = new Vector3(fromPos.x, toPos.y, fromPos.z);
                }
            }
            
            return new Vector3[] { fromPos, intermediatePoint, toPos };
        }
        
        public void ConvertToPermanent(string toNodeId, int toAnchorIndex)
        {
            if (!isPendingConnection) return;
            
            connectionData.id = Guid.NewGuid().ToString("N");
            connectionData.toNodeId = toNodeId;
            connectionData.toAnchorIndex = toAnchorIndex;
            isPendingConnection = false;
            
            // Update visual appearance
            Color targetColor = connectionColor;
            lineRenderer.startColor = targetColor;
            lineRenderer.endColor = targetColor;
            
            gameObject.name = $"Connection ({connectionData.fromNodeId} -> {connectionData.toNodeId})";
            
            UpdateConnection();
        }
        
        public void UpdateVisualStyle(bool isHighlighted = false)
        {
            if (lineRenderer == null) return;
            
            Color targetColor = connectionColor;
            float targetWidth = lineWidth;
            
            if (isPendingConnection)
            {
                targetColor = pendingConnectionColor;
                targetWidth = lineWidth * 1.5f;
            }
            else if (isHighlighted)
            {
                targetColor = Color.yellow;
                targetWidth = lineWidth * 1.2f;
            }
            
            lineRenderer.startColor = targetColor;
            lineRenderer.endColor = targetColor;
            lineRenderer.startWidth = targetWidth;
            lineRenderer.endWidth = targetWidth;
        }
        
        public float GetLength()
        {
            if (lineRenderer == null || lineRenderer.positionCount < 2) return 0f;
            
            // Calculate total path length including all segments
            float totalLength = 0f;
            for (int i = 0; i < lineRenderer.positionCount - 1; i++)
            {
                Vector3 segmentStart = lineRenderer.GetPosition(i);
                Vector3 segmentEnd = lineRenderer.GetPosition(i + 1);
                totalLength += Vector3.Distance(segmentStart, segmentEnd);
            }
            return totalLength;
        }
        
        public float GetLengthInGridTiles(float gridSpacing = 5f)
        {
            return GetLength() / gridSpacing;
        }
        
        public Vector3 GetMidpoint()
        {
            if (lineRenderer == null || lineRenderer.positionCount < 2) return Vector3.zero;
            
            Vector3 start = lineRenderer.GetPosition(0);
            Vector3 end = lineRenderer.GetPosition(1);
            return (start + end) * 0.5f;
        }
        
        public bool IsPointOnLine(Vector3 worldPoint, float threshold = 0.2f)
        {
            if (lineRenderer == null || lineRenderer.positionCount < 2) return false;
            
            Vector3 start = lineRenderer.GetPosition(0);
            Vector3 end = lineRenderer.GetPosition(1);
            
            // Calculate distance from point to line segment
            Vector3 lineDirection = (end - start).normalized;
            Vector3 pointDirection = worldPoint - start;
            
            float projectionLength = Vector3.Dot(pointDirection, lineDirection);
            projectionLength = Mathf.Clamp(projectionLength, 0f, Vector3.Distance(start, end));
            
            Vector3 closestPoint = start + lineDirection * projectionLength;
            float distanceToLine = Vector3.Distance(worldPoint, closestPoint);
            
            return distanceToLine <= threshold;
        }
        
        public void SetInteractable(bool interactable)
        {
            // Add collider for interaction if needed
            var collider = GetComponent<EdgeCollider2D>();
            
            if (interactable && collider == null)
            {
                collider = gameObject.AddComponent<EdgeCollider2D>();
                UpdateCollider();
            }
            else if (!interactable && collider != null)
            {
                DestroyImmediate(collider);
            }
        }
        
        private void UpdateCollider()
        {
            var collider = GetComponent<EdgeCollider2D>();
            if (collider == null || lineRenderer == null) return;
            
            if (lineRenderer.positionCount >= 2)
            {
                Vector2[] points = new Vector2[lineRenderer.positionCount];
                for (int i = 0; i < lineRenderer.positionCount; i++)
                {
                    Vector3 worldPos = lineRenderer.GetPosition(i);
                    points[i] = new Vector2(worldPos.x, worldPos.y);
                }
                collider.points = points;
            }
        }
        
        // Mouse interaction for connection selection/deletion
        private void OnMouseDown()
        {
            if (isPendingConnection) return;
            
            // Handle connection interaction
            Debug.Log($"Connection clicked: {connectionData.fromNodeId} -> {connectionData.toNodeId}");
            
            // Could implement connection selection, deletion, or property editing here
            if (Input.GetKey(KeyCode.Delete))
            {
                RequestDeletion();
            }
        }
        
        private void OnMouseEnter()
        {
            if (!isPendingConnection)
            {
                UpdateVisualStyle(true); // Highlight on hover
            }
        }
        
        private void OnMouseExit()
        {
            if (!isPendingConnection)
            {
                UpdateVisualStyle(false); // Remove highlight
            }
        }
        
        private void RequestDeletion()
        {
            // Request deletion from the graph
            if (graph != null)
            {
                // The graph should handle the actual deletion
                Debug.Log($"Requesting deletion of connection: {connectionData.id}");
            }
        }
        
        private void OnDestroy()
        {
            // Clean up material if we created it
            if (lineRenderer != null && lineRenderer.material != null && 
                lineRenderer.material.name.Contains("(Instance)"))
            {
                DestroyImmediate(lineRenderer.material);
            }
        }
        
        // Debug visualization
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || lineRenderer == null) return;
            
            if (isPendingConnection)
            {
                Gizmos.color = pendingConnectionColor;
            }
            else
            {
                Gizmos.color = connectionColor;
            }
            
            if (lineRenderer.positionCount >= 2)
            {
                Vector3 start = lineRenderer.GetPosition(0);
                Vector3 end = lineRenderer.GetPosition(1);
                Gizmos.DrawLine(start, end);
                
                // Draw direction arrow
                Vector3 direction = (end - start).normalized;
                Vector3 midpoint = (start + end) * 0.5f;
                Gizmos.DrawRay(midpoint, direction * 0.3f);
                
                // Draw anchor points
                Gizmos.DrawWireSphere(start, 0.05f);
                Gizmos.DrawWireSphere(end, 0.05f);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (lineRenderer == null) return;
            
            // Show connection info when selected
            Gizmos.color = Color.white;
            
            if (lineRenderer.positionCount >= 2)
            {
                Vector3 start = lineRenderer.GetPosition(0);
                Vector3 end = lineRenderer.GetPosition(1);
                Vector3 midpoint = (start + end) * 0.5f;
                
                // Draw connection bounds
                Gizmos.DrawWireSphere(midpoint, GetLength() * 0.5f);
            }
        }
    }
}