using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// Sprite-based node component for world space graph nodes
    /// </summary>
    public class SpriteNode : MonoBehaviour
    {
        [System.Serializable]
        public class NodeData
        {
            public string id = "";
            public string title = "Node";
            public Vector3 worldPosition = Vector3.zero;
            public List<MetadataEntry> metadata = new List<MetadataEntry>();
            public bool metadataFoldout = true;
            
            // Point properties from specification
            public Color color = Color.white;
            public bool mute = false;
            [Range(0f, 100f)]
            public float probability = 100f;
            public int repeatCount = 1;
            public float duration = 0.01f; // in quarter notes/grid squares
            public bool isStart = false; // whether this node can spawn travelers
            
            // Music-aware properties
            [Range(0, 127)]
            public int note = 64; // MIDI note
            [Range(0, 127)]
            public int velocity = 5;
            [Range(1, 16)]
            public int channel = 1;
            public bool forceToProjectScale = false;
            
            // Instrument selection
            public int selectedInstrumentIndex = 0; // Index into available instruments list
            public string instrumentName = "Piano"; // Display name of selected instrument
            
            // CC (Control Change) properties
            public bool isCCPoint = false;
            [Range(0, 127)]
            public int ccParam = 1;
            [Range(0, 127)]
            public int ccAmount = 64;
            public float ccSlew = 0f;
            [Range(0, 127)]
            public int ccEndAmount = 64;
            public float ccSlewDuration = 1f;
            
            // Logic Point properties
            public bool isLogicPoint = false;
            public LogicType logicType = LogicType.AND;
            
            // Path behavior
            public PathBehavior pathBehavior = PathBehavior.Sequential;
        
            // Engine properties (when this node is an engine component)
            public bool isEngine = false;
            public EngineType engineType = EngineType.MainEngine;
            public float thrust = 100f;
            public float efficiency = 0.8f;
            public bool showThrustEffect = true;
            public Color thrustColor = Color.cyan;
            public int gridWidth = 1; // Width in grid cells
            public int gridHeight = 1; // Height in grid cells
            public float rotation = 0f; // Rotation angle in degrees (0, 90, 180, 270)
            public List<string> connectedNodeIds = new List<string>();
            public UnityEngine.Sprite icon;
        }
        
        [System.Serializable]
        public enum LogicType
        {
            AND,
            OR,
            NOT,
            Toggle
        }
        
        [System.Serializable]
        public enum PathBehavior
        {
            Sequential,    // Follow paths in creation/order list
            WeightedRandom, // Choose an outgoing path by weight
            Split,         // Fan out to all outgoing paths
            Instant        // Jump to target with no travel time
        }
        
        [System.Serializable]
        public enum EngineType
        {
            MainEngine,      // Primary propulsion
            Thruster,        // Maneuvering thrusters
            RetroEngine,     // Reverse thrust
            StabilityEngine  // Attitude control
        }
        
        [System.Serializable]
        public class MetadataEntry
        {
            public string key = "";
            public string value = "";
        }
        
        [Header("Visual Settings")]
        public Color defaultColor = new Color(0.2f, 0.2f, 0.25f, 1f);
        public Color selectedColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        public Color pendingColor = new Color(1f, 0.95f, 0.3f, 1.0f);
        public float borderWidth = 0.1f;
        
        private SpriteRuntimeGraph graph;
        private NodeData nodeData;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer startSymbolRenderer;
        private BoxCollider2D nodeCollider;
        private CompositeEngineRenderer compositeEngineRenderer;
        private CompositeShipPartRenderer compositeShipPartRenderer;
        private bool isSelected;
        private bool isPendingHighlight;
        private bool isDragging;
        private Vector3 dragStartPos;
        private Vector3 mouseOffset;
        
        // Connection anchors (3 per side = 12 total)
        private const int AnchorsPerSide = 3;
        private const int TotalAnchors = AnchorsPerSide * 4;
        
        
        public NodeData NodeDataInstance => nodeData;
        public bool IsSelected => isSelected;
        public bool IsDragging => isDragging;
        
        public void Initialize(SpriteRuntimeGraph graph, NodeData data)
        {
            this.graph = graph;
            this.nodeData = data;
            
            SetupComponents();
            UpdatePosition();
            UpdateVisuals();
        }
        
        private void SetupComponents()
        {
            // Get or create sprite renderer
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            
            // Get or create collider
            nodeCollider = GetComponent<BoxCollider2D>();
            if (nodeCollider == null)
            {
                nodeCollider = gameObject.AddComponent<BoxCollider2D>();
            }
            
            // Set sprite based on node type - use composite rendering for ALL ship parts
            bool isShipPart = IsShipPart();
            if (isShipPart)
            {
                // Use composite ship part renderer for all ship parts
                SetupCompositeShipPartRenderer();
            }
            else
            {
                spriteRenderer.sprite = CreateNodeSprite();
                // Standard node collider size
                nodeCollider.size = Vector2.one;
            }
            
            // Create start symbol overlay
            CreateStartSymbol();
            
            // Set name
            gameObject.name = isShipPart ? $"ShipPartNode ({nodeData.title})" : $"GraphNode ({nodeData.title})";
            
            // Ensure proper layer and sorting
            gameObject.layer = LayerMask.NameToLayer("Default");
            spriteRenderer.sortingOrder = 10; // Above connections but below UI
        }
        
        private void UpdatePosition()
        {
            transform.position = nodeData.worldPosition;
        }
        
        public void UpdateVisuals()
        {
            Color targetColor = nodeData.color;
            
            if (isPendingHighlight)
                targetColor = pendingColor;
            else if (isSelected)
                targetColor = selectedColor;
            
            // Update visuals based on node type
            if (compositeShipPartRenderer != null)
            {
                // Update universal composite ship part renderer
                compositeShipPartRenderer.UpdateBlockColors(targetColor);
                compositeShipPartRenderer.ApplyRotation(nodeData.rotation);
            }
            else if (nodeData.isEngine && compositeEngineRenderer != null)
            {
                // Update legacy composite engine renderer (fallback)
                compositeEngineRenderer.UpdateBlockColors(targetColor);
                compositeEngineRenderer.ApplyRotation(nodeData.rotation);
            }
            else
            {
                // Update regular sprite renderer for non-ship parts
                spriteRenderer.color = targetColor;
                transform.rotation = Quaternion.Euler(0, 0, nodeData.rotation);
            }
        }
        
        private void CreateStartSymbol()
        {
            // Create child GameObject for start symbol overlay
            var symbolGO = new GameObject("StartSymbol");
            symbolGO.transform.SetParent(transform);
            symbolGO.transform.localPosition = Vector3.zero;
            symbolGO.transform.localScale = Vector3.one;
            
            // Add sprite renderer for the start symbol
            startSymbolRenderer = symbolGO.AddComponent<SpriteRenderer>();
            //startSymbolRenderer.sprite = nodeData.icon;
            startSymbolRenderer.color = Color.white;
            startSymbolRenderer.sortingOrder = 11; // Above the main node sprite
            
            // Initially hidden - will be shown based on isStart in UpdateVisuals
            //startSymbolRenderer.enabled = false;
        }
        
        private UnityEngine.Sprite CreateNodeSprite()
        {
            // Create a simple square node sprite
            var texture = new Texture2D(64, 64);
            var colors = new Color32[64 * 64];
            
            // Fill with node color and black border
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    if (x == 0 || x == 63 || y == 0 || y == 63)
                        colors[y * 64 + x] = Color.black;
                    else
                        colors[y * 64 + x] = new Color(0.2f, 0.2f, 0.25f, 0.9f);
                }
            }
            
            texture.SetPixels32(colors);
            texture.Apply();
            
            return UnityEngine.Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        }
        
        private bool IsShipPart()
        {
            // Check if this node is a ship part based on metadata or engine flag
            if (nodeData.isEngine) return true;
            
            // Check metadata for ship part categories
            foreach (var metadata in nodeData.metadata)
            {
                if (metadata.key == "Category")
                {
                    string category = metadata.value;
                    return category == "Power & Energy" ||
                           category == "Thermal & Coolant" ||
                           category == "Atmosphere & Life Support" ||
                           category == "Structural & Hull" ||
                           category == "Propulsion & Maneuvering" ||
                           category == "Navigation, Comms & Sensors" ||
                           category == "Data, Control & Security" ||
                           category == "Manufacturing, Inventory & Logistics" ||
                           category == "Defense & Shielding";
                }
            }
            
            // Fallback: check part name for ship part keywords
            string partName = nodeData.title.ToLowerInvariant();
            return partName.Contains("reactor") || partName.Contains("power") || partName.Contains("battery") ||
                   partName.Contains("coolant") || partName.Contains("heat") || partName.Contains("thermal") ||
                   partName.Contains("life") || partName.Contains("atmosphere") || partName.Contains("filter") ||
                   partName.Contains("hull") || partName.Contains("structural") || partName.Contains("armor") ||
                   partName.Contains("engine") || partName.Contains("thruster") || partName.Contains("propulsion") ||
                   partName.Contains("navigation") || partName.Contains("sensor") || partName.Contains("antenna") ||
                   partName.Contains("control") || partName.Contains("security") || partName.Contains("memory") ||
                   partName.Contains("fabricator") || partName.Contains("manufacturing") || partName.Contains("inventory") ||
                   partName.Contains("shield") || partName.Contains("defense");
        }
        
        private void SetupCompositeShipPartRenderer()
        {
            // Hide the main sprite renderer for ship parts - we'll use composite blocks instead
            spriteRenderer.sprite = null;
            spriteRenderer.color = Color.clear;
            
            // Get or create the composite ship part renderer component
            compositeShipPartRenderer = GetComponent<CompositeShipPartRenderer>();
            if (compositeShipPartRenderer == null)
            {
                compositeShipPartRenderer = gameObject.AddComponent<CompositeShipPartRenderer>();
            }
            
            // Initialize the composite renderer with this node
            compositeShipPartRenderer.Initialize(this);
        }
        
        private void SetupCompositeEngineRenderer()
        {
            // Hide the main sprite renderer for engine parts - we'll use composite blocks instead
            spriteRenderer.sprite = null;
            spriteRenderer.color = Color.clear;
            
            // Get or create the composite engine renderer component
            compositeEngineRenderer = GetComponent<CompositeEngineRenderer>();
            if (compositeEngineRenderer == null)
            {
                compositeEngineRenderer = gameObject.AddComponent<CompositeEngineRenderer>();
            }
            
            // Initialize the composite renderer with this node
            compositeEngineRenderer.Initialize(this);
        }
        
        private UnityEngine.Sprite CreateEngineSprite()
        {
            // Create sprite based on engine grid dimensions
            int pixelsPerGridCell = 64; // Resolution per grid cell
            int textureWidth = nodeData.gridWidth * pixelsPerGridCell;
            int textureHeight = nodeData.gridHeight * pixelsPerGridCell;
            
            var texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            var colors = new Color[textureWidth * textureHeight];
            
            float centerX = textureWidth * 0.5f;
            float centerY = textureHeight * 0.5f;
            
            // Fill with engine shape based on type and size
            for (int y = 0; y < textureHeight; y++)
            {
                for (int x = 0; x < textureWidth; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    Color pixelColor = Color.clear;
                    
                    switch (nodeData.engineType)
                    {
                        case EngineType.MainEngine:
                            // Rectangular main engine scaled to size
                            float marginX = textureWidth * 0.1f;
                            float marginY = textureHeight * 0.2f;
                            if (x >= marginX && x <= textureWidth - marginX && 
                                y >= marginY && y <= textureHeight - marginY)
                                pixelColor = nodeData.color;
                            break;
                            
                        case EngineType.Thruster:
                            // Circular thruster
                            float maxRadius = Mathf.Min(textureWidth, textureHeight) * 0.4f;
                            if (distance <= maxRadius)
                                pixelColor = nodeData.color;
                            break;
                            
                        case EngineType.RetroEngine:
                            // Diamond-shaped retro engine
                            float diamondSize = Mathf.Min(textureWidth, textureHeight) * 0.4f;
                            if (Mathf.Abs(x - centerX) + Mathf.Abs(y - centerY) <= diamondSize)
                                pixelColor = nodeData.color;
                            break;
                            
                        case EngineType.StabilityEngine:
                            // Cross-shaped stability engine
                            float crossThickness = textureWidth * 0.25f;
                            if ((x >= centerX - crossThickness && x <= centerX + crossThickness) ||
                                (y >= centerY - crossThickness && y <= centerY + crossThickness))
                                pixelColor = nodeData.color;
                            break;
                    }
                    
                    colors[y * textureWidth + x] = pixelColor;
                }
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            
            return UnityEngine.Sprite.Create(texture, new Rect(0, 0, textureWidth, textureHeight), 
                new Vector2(0.5f, 0.5f), pixelsPerGridCell);
        }
        
        private UnityEngine.Sprite CreateStarSprite()
        {
            // Create a star-shaped sprite
            int size = 32;
            var texture = new Texture2D(size, size);
            var colors = new Color32[size * size];
            
            // Clear background
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }
            
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outerRadius = size * 0.4f;
            float innerRadius = size * 0.2f;
            
            // Draw a simple 5-pointed star
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    Vector2 dir = pos - center;
                    float distance = dir.magnitude;
                    float angle = Mathf.Atan2(dir.y, dir.x);
                    
                    // Convert angle to 0-10 range (5 points * 2 for inner/outer)
                    float normalizedAngle = (angle + Mathf.PI) / (2 * Mathf.PI) * 10;
                    int starSegment = Mathf.FloorToInt(normalizedAngle) % 10;
                    float segmentProgress = normalizedAngle - Mathf.Floor(normalizedAngle);
                    
                    // Determine if this is an outer point (even segments) or inner point (odd segments)
                    bool isOuterPoint = (starSegment % 2) == 0;
                    float targetRadius = isOuterPoint ? outerRadius : innerRadius;
                    
                    // Interpolate radius for smooth star shape
                    float nextRadius = ((starSegment + 1) % 2 == 0) ? outerRadius : innerRadius;
                    float currentRadius = Mathf.Lerp(targetRadius, nextRadius, segmentProgress);
                    
                    // Fill pixels that are within the star shape
                    if (distance <= currentRadius && distance >= 2f)
                    {
                        colors[y * size + x] = new Color32(255, 255, 0, 255); // Yellow star
                    }
                }
            }
            
            texture.SetPixels32(colors);
            texture.Apply();
            
            return UnityEngine.Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size * 2f);
        }
        
        private void Update()
        {
            HandleDragging();
        }
        
        private void HandleDragging()
        {
            if (!isDragging) return;
            
            if (Input.GetMouseButton(0))
            {
                Vector3 mouseWorldPos = graph.ScreenToWorld(Input.mousePosition);
                Vector3 newPos = mouseWorldPos + mouseOffset;
                
                // Always snap to grid during dragging to maintain alignment
                var gridRenderer = graph.GetComponent<SpriteRuntimeGraph>()?.GetComponentInChildren<SpriteGraphGrid>();
                if (gridRenderer != null)
                {
                    newPos = gridRenderer.SnapToGrid(newPos, true);
                }
                
                transform.position = newPos;
                nodeData.worldPosition = newPos;
                
                // Notify graph of position change for connection updates
                graph.GetComponent<SpriteRuntimeGraph>()?.OnNodeMoved(this);
            }
            else
            {
                StopDragging();
            }
        }
        
        private void OnMouseDown()
        {
            if (graph == null) return;
            
            Vector3 mouseWorldPos = graph.ScreenToWorld(Input.mousePosition);
            
            switch (graph.CurrentMode)
            {
                case SpriteRuntimeGraph.InteractionMode.Select:
                    HandleSelectMode(mouseWorldPos);
                    break;
                case SpriteRuntimeGraph.InteractionMode.Connect:
                    HandleConnectMode(mouseWorldPos);
                    break;
            }
        }
        
        private void HandleSelectMode(Vector3 mouseWorldPos)
        {
            graph.SelectNode(this);
            
            // Start dragging if not already selected
            if (graph.SelectedNodeId == nodeData.id)
            {
                StartDragging(mouseWorldPos);
            }
        }
        
        private void HandleConnectMode(Vector3 mouseWorldPos)
        {
            // Connection handling is now managed entirely by SpriteRuntimeGraph.HandleConnectionClick
            // to prevent duplicate connection creation. This method is left empty to avoid
            // processing the same mouse click twice.
        }
        
        private void StartDragging(Vector3 mouseWorldPos)
        {
            isDragging = true;
            dragStartPos = transform.position;
            mouseOffset = transform.position - mouseWorldPos;
        }
        
        private void StopDragging()
        {
            isDragging = false;
            
            // Snap to grid when dragging ends
            if (graph != null)
            {
                var gridRenderer = graph.GetComponent<SpriteRuntimeGraph>()?.GetComponentInChildren<SpriteGraphGrid>();
                if (gridRenderer != null)
                {
                    Vector3 snappedPos = gridRenderer.SnapToGrid(transform.position, true);
                    transform.position = snappedPos;
                    nodeData.worldPosition = snappedPos;
                    
                    // Notify graph of position change for connection updates
                    graph.GetComponent<SpriteRuntimeGraph>()?.OnNodeMoved(this);
                }
            }
        }
        
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateVisuals();
            // World-space UI removed - node selection now handled by screen-space UI
        }
        
        public void SetPendingHighlight(bool highlight)
        {
            isPendingHighlight = highlight;
            UpdateVisuals();
        }
        
        public Vector3 GetAnchorWorldPosition(int anchorIndex)
        {
            // For composite ship parts, use block-specific anchors
            if (compositeShipPartRenderer != null)
            {
                return GetCompositeShipPartAnchorPosition(anchorIndex);
            }
            // Legacy: For composite engine parts, use block-specific anchors
            else if (nodeData.isEngine && compositeEngineRenderer != null)
            {
                return GetCompositeEngineAnchorPosition(anchorIndex);
            }
            
            // Standard node anchor calculation
            if (anchorIndex < 0 || anchorIndex >= TotalAnchors)
                return transform.position;
            
            // Get node bounds
            Bounds bounds = spriteRenderer.bounds;
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;
            
            // Calculate anchor position around the perimeter
            int side = anchorIndex / AnchorsPerSide;
            int indexOnSide = anchorIndex % AnchorsPerSide;
            float t = (indexOnSide + 1f) / (AnchorsPerSide + 1f);
            
            Vector3 anchorPos;
            switch (side)
            {
                case 0: // Left edge
                    anchorPos = new Vector3(center.x - size.x * 0.5f, center.y + (t - 0.5f) * size.y, center.z);
                    break;
                case 1: // Right edge
                    anchorPos = new Vector3(center.x + size.x * 0.5f, center.y + (t - 0.5f) * size.y, center.z);
                    break;
                case 2: // Top edge
                    anchorPos = new Vector3(center.x + (t - 0.5f) * size.x, center.y + size.y * 0.5f, center.z);
                    break;
                case 3: // Bottom edge
                    anchorPos = new Vector3(center.x + (t - 0.5f) * size.x, center.y - size.y * 0.5f, center.z);
                    break;
                default:
                    anchorPos = center;
                    break;
            }
            
            return anchorPos;
        }

        private Vector3 GetCompositeShipPartAnchorPosition(int anchorIndex)
        {
            var partBlocks = compositeShipPartRenderer.PartBlocks;
            
            // Find which block this anchor belongs to
            foreach (var block in partBlocks)
            {
                if (block.availableAnchorIndices.Contains(anchorIndex))
                {
                    // Get anchor positions for this specific block
                    var blockAnchorPositions = compositeShipPartRenderer.GetBlockAnchorPositions(block);
                    
                    // Find the local index within the block's anchors
                    int localAnchorIndex = block.availableAnchorIndices.IndexOf(anchorIndex);
                    
                    if (localAnchorIndex >= 0 && localAnchorIndex < blockAnchorPositions.Length)
                    {
                        return blockAnchorPositions[localAnchorIndex];
                    }
                }
            }
            
            // Fallback to center position if anchor not found
            return transform.position;
        }
        
        private Vector3 GetCompositeEngineAnchorPosition(int anchorIndex)
        {
            var engineBlocks = compositeEngineRenderer.EngineBlocks;
            
            // Find which block this anchor belongs to
            foreach (var block in engineBlocks)
            {
                if (block.availableAnchorIndices.Contains(anchorIndex))
                {
                    // Get anchor positions for this specific block
                    var blockAnchorPositions = compositeEngineRenderer.GetBlockAnchorPositions(block);
                    
                    // Find the local index within the block's anchors
                    int localAnchorIndex = block.availableAnchorIndices.IndexOf(anchorIndex);
                    
                    if (localAnchorIndex >= 0 && localAnchorIndex < blockAnchorPositions.Length)
                    {
                        return blockAnchorPositions[localAnchorIndex];
                    }
                }
            }
            
            // Fallback to center position if anchor not found
            return transform.position;
        }
        
        public int GetNearestAnchorIndex(Vector3 worldPos)
        {
            float bestDistance = float.MaxValue;
            int bestIndex = 0;
            
            for (int i = 0; i < TotalAnchors; i++)
            {
                Vector3 anchorPos = GetAnchorWorldPosition(i);
                float distance = Vector3.Distance(worldPos, anchorPos);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }
            
            return bestIndex;
        }
        
        public int GetNearestAvailableAnchorIndex(Vector3 worldPos)
        {
            float bestDistance = float.MaxValue;
            int bestIndex = -1;
            
            for (int i = 0; i < TotalAnchors; i++)
            {
                if (!graph.IsAnchorAvailable(nodeData.id, i))
                    continue;
                
                Vector3 anchorPos = GetAnchorWorldPosition(i);
                float distance = Vector3.Distance(worldPos, anchorPos);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }
            
            return bestIndex;
        }
        
        public void UpdateTitle(string newTitle)
        {
            nodeData.title = newTitle;
            gameObject.name = $"GraphNode ({nodeData.title})";
        }
        
        public void AddMetadata(string key, string value)
        {
            nodeData.metadata.Add(new MetadataEntry { key = key, value = value });
        }
        
        public void RemoveMetadata(int index)
        {
            if (index >= 0 && index < nodeData.metadata.Count)
            {
                nodeData.metadata.RemoveAt(index);
            }
        }
        
        private void OnDestroy()
        {
            // Clean up any references
            // World-space UI removed - no UI cleanup needed
        }
        
        // Debug visualization
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            
            // Draw anchor points when selected
            if (isSelected || isPendingHighlight)
            {
                Gizmos.color = isPendingHighlight ? pendingColor : Color.yellow;
                
                for (int i = 0; i < TotalAnchors; i++)
                {
                    Vector3 anchorPos = GetAnchorWorldPosition(i);
                    bool isAvailable = graph?.IsAnchorAvailable(nodeData.id, i) ?? true;
                    
                    Gizmos.color = isAvailable ? Color.green : Color.red;
                    Gizmos.DrawWireSphere(anchorPos, 0.1f);
                }
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Always show anchor points when selected in editor
            Gizmos.color = Color.cyan;
            
            if (spriteRenderer != null)
            {
                for (int i = 0; i < TotalAnchors; i++)
                {
                    Vector3 anchorPos = GetAnchorWorldPosition(i);
                    Gizmos.DrawWireSphere(anchorPos, 0.05f);
                    Gizmos.DrawLine(transform.position, anchorPos);
                }
            }
        }
    }
    
}