using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// Sprite-based RuntimeGraph system for world space usage
    /// Replaces UI Toolkit-based system to work in game world
    /// </summary>
    public class SpriteRuntimeGraph : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject nodePrefab;
        public GameObject connectionPrefab;
        
        [Header("Settings")]
        public float nodeSize = 5f;
        public Color gridColor = new Color(1f, 1f, 1f, 0.1f);
        public float gridSpacing = 5f;
        public LayerMask nodeLayer = -1;
        
        [Header("Camera")]
        public Camera graphCamera;
        public float zoomMin = 0.5f;
        public float zoomMax = 50f; // Increased for much greater zoom out capability
        public float panSpeed = 10f;
        public float zoomSpeed = 2f;
        
        [Header("Audio")]
        public AudioSource nodeBeepSource;
        public AudioClip nodeBeepClip;
        public float nodeBeepVolume = 0.5f;
        
        // Graph data
        [SerializeField] private List<SpriteNode.NodeData> nodes = new List<SpriteNode.NodeData>();
        [SerializeField] private List<SpriteConnection.ConnectionData> connections = new List<SpriteConnection.ConnectionData>();
        
        // Runtime components
        private Dictionary<string, SpriteNode> nodeInstances = new Dictionary<string, SpriteNode>();
        private Dictionary<string, SpriteConnection> connectionInstances = new Dictionary<string, SpriteConnection>();
        private SpriteGraphGrid gridRenderer;
        private SpriteGraphToolbar toolbar;
        private SpritePlaybackController playbackController;
        private SpriteNodeConfigUI nodeConfigUI;
        private SpriteNodePalette nodePalette;
        private Transform nodeContainer;
        
        // Interaction state
        public enum InteractionMode { Select, Node, Connect, Play }
        public InteractionMode currentMode = InteractionMode.Select;
        
        private string selectedNodeId;
        private string pendingFromNodeId;
        private int pendingFromAnchorIndex = -1;
        private SpriteNode draggedNode;
        private Vector3 dragOffset;
        
        // Input state
        private bool isPanning;
        private Vector3 lastMouseWorldPos;
        private bool rightMouseDown;
        private Vector3 rightMouseDownPos;
        private const float panThreshold = 0.1f;
        
        // Smooth panning
        private Vector3 targetCameraPos;
        private bool smoothPanning = false;
        private float panSmoothSpeed = 20f;
        
        public event Action<string> NodeSelected;
        public event Action<SpriteNode.NodeData> NodeCreated;
        public event Action<SpriteConnection.ConnectionData> ConnectionCreated;
        public System.Action<SpriteNode.NodeData> OnNodeActivated;
        
        private void Awake()
        {
            InitializeCamera();
            InitializeNodeContainer();
            InitializeGrid();
            InitializeToolbar();
            InitializePlaybackController();
            InitializeNodeConfigUI();
            InitializeNodePalette();
            InitializeAudio();
            InitializeStatsUI();
            EnsureNodeIds();
            CreateNodeInstances();
            CreateConnectionInstances();
        }
        
        private void InitializeCamera()
        {
            if (graphCamera == null)
            {
                graphCamera = Camera.main;
                if (graphCamera == null)
                {
                    var cameraGO = new GameObject("SpriteGraphCamera");
                    graphCamera = cameraGO.AddComponent<Camera>();
                    graphCamera.orthographic = true;
                    graphCamera.orthographicSize = 10f;
                    graphCamera.transform.position = new Vector3(0, 0, -10f); // Position to see grid
                    graphCamera.backgroundColor = new Color(0.11f, 0.12f, 0.13f); // Match UI background
                }
            }
            
            // Ensure camera is positioned to see the grid
            if (graphCamera.transform.position.z >= 0)
            {
                graphCamera.transform.position = new Vector3(graphCamera.transform.position.x, graphCamera.transform.position.y, -10f);
            }
            
            // Ensure AudioListener is present for audio playback
            if (graphCamera.GetComponent<AudioListener>() == null)
            {
                graphCamera.gameObject.AddComponent<AudioListener>();
            }
        }
        
        private void InitializeNodeContainer()
        {
            var containerGO = new GameObject("NodeContainer");
            containerGO.transform.SetParent(transform);
            containerGO.transform.position = Vector3.zero;
            nodeContainer = containerGO.transform;
        }
        
        private void InitializeGrid()
        {
            var gridGO = new GameObject("GraphGrid");
            gridGO.transform.SetParent(transform);
            gridRenderer = gridGO.AddComponent<SpriteGraphGrid>();
            gridRenderer.Initialize(this);
        }
        
        private void InitializeToolbar()
        {
            var toolbarGO = new GameObject("GraphToolbar");
            toolbarGO.transform.SetParent(transform);
            toolbar = toolbarGO.AddComponent<SpriteGraphToolbar>();
            toolbar.Initialize(this);
            toolbar.ModeChanged += OnModeChanged;
        }
        
        private void InitializePlaybackController()
        {
            var playbackGO = new GameObject("PlaybackController");
            playbackGO.transform.SetParent(transform);
            playbackController = playbackGO.AddComponent<SpritePlaybackController>();
            playbackController.Initialize(this);
        }
        
        private void InitializeNodeConfigUI()
        {
            var nodeConfigGO = new GameObject("NodeConfigUI");
            nodeConfigGO.transform.SetParent(transform);
            nodeConfigUI = nodeConfigGO.AddComponent<SpriteNodeConfigUI>();
            nodeConfigUI.Initialize();
        }
        
        private void InitializeNodePalette()
        {
            var nodePaletteGO = new GameObject("NodePalette");
            nodePaletteGO.transform.SetParent(transform);
            nodePalette = nodePaletteGO.AddComponent<SpriteNodePalette>();
            nodePalette.Initialize(this);
            nodePalette.SetVisible(false); // Hidden by default, only show in Node mode
        }
        
        private void InitializeAudio()
        {
            // Create audio source for node beep sounds
            var audioGO = new GameObject("NodeBeepAudio");
            audioGO.transform.SetParent(transform);
            
            nodeBeepSource = audioGO.AddComponent<AudioSource>();
            nodeBeepSource.playOnAwake = false;
            nodeBeepSource.volume = nodeBeepVolume;
            nodeBeepSource.pitch = 1f;
            
            // Create node beep sound if none provided
            if (nodeBeepClip == null)
            {
                nodeBeepClip = CreateNodeBeepSound();
            }
        }
        
        private void InitializeStatsUI()
        {
            // Create ship stats UI integration
            var statsGO = new GameObject("ShipStatsUI");
            statsGO.transform.SetParent(transform);
            var statsIntegration = statsGO.AddComponent<Commonwealth.Script.Ship.Monitors.SpriteRuntimeGraphStatsIntegration>();
        }
        
        private AudioClip CreateNodeBeepSound()
        {
            // Create a different beep sound from the metronome (higher frequency, shorter duration)
            int sampleRate = 44100;
            float duration = 0.08f; // Shorter than metronome click
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] audioData = new float[samples];
            
            float frequency = 1200f; // Higher frequency than metronome (800Hz)
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 15f); // Slightly softer decay
                audioData[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * 0.4f;
            }
            
            AudioClip clip = AudioClip.Create("NodeBeepSound", samples, 1, sampleRate, false);
            clip.SetData(audioData, 0);
            return clip;
        }
        
        private void EnsureNodeIds()
        {
            var usedIds = new HashSet<string>();
            for (int i = 0; i < nodes.Count; i++)
            {
                if (string.IsNullOrEmpty(nodes[i].id) || usedIds.Contains(nodes[i].id))
                {
                    nodes[i].id = Guid.NewGuid().ToString("N");
                }
                usedIds.Add(nodes[i].id);
            }
        }
        
        private void CreateNodeInstances()
        {
            foreach (var nodeData in nodes)
            {
                CreateNodeInstance(nodeData);
            }
        }
        
        private void CreateConnectionInstances()
        {
            foreach (var connectionData in connections)
            {
                CreateConnectionInstance(connectionData);
            }
        }
        
        private void Update()
        {
            HandleInput();
            HandleSmoothPanning();
        }
        
        private void HandleSmoothPanning()
        {
            if (smoothPanning)
            {
                Vector3 previousCameraPos = graphCamera.transform.position;
                graphCamera.transform.position = Vector3.Lerp(graphCamera.transform.position, targetCameraPos, Time.deltaTime * panSmoothSpeed);
                
                // Move node container opposite to camera movement to keep nodes in view
                Vector3 cameraDelta = graphCamera.transform.position - previousCameraPos;
                nodeContainer.position -= cameraDelta;
                
                // Stop smooth panning when close enough
                if (Vector3.Distance(graphCamera.transform.position, targetCameraPos) < 0.01f)
                {
                    graphCamera.transform.position = targetCameraPos;
                    smoothPanning = false;
                }
                
                // Update grid less frequently during smooth panning to reduce jitter
                if (Time.frameCount % 3 == 0)
                {
                    gridRenderer?.UpdateGrid();
                }
            }
        }
        
        private void HandleInput()
        {
            HandleMouseInput();
            HandleKeyboardInput();
        }
        
        private void HandleMouseInput()
        {
            Vector3 mouseWorldPos = graphCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            
            // Right mouse button for panning
            if (Input.GetMouseButtonDown(1))
            {
                rightMouseDown = true;
                rightMouseDownPos = mouseWorldPos;
            }
            
            if (rightMouseDown && Input.GetMouseButton(1))
            {
                float distance = Vector3.Distance(mouseWorldPos, rightMouseDownPos);
                if (distance > panThreshold && !isPanning)
                {
                    isPanning = true;
                }
            }
            
            if (Input.GetMouseButtonUp(1))
            {
                if (!isPanning && rightMouseDown)
                {
                    // Right click context menu
                    HandleRightClick(mouseWorldPos);
                }
                rightMouseDown = false;
                isPanning = false;
            }
            
            // Handle panning
            if (isPanning)
            {
                Vector3 deltaWorld = mouseWorldPos - lastMouseWorldPos;
                targetCameraPos = graphCamera.transform.position - deltaWorld;
                
                // Enable smooth panning for jitter-free movement
                smoothPanning = true;
            }
            
            // Left mouse button for selection and interaction
            if (Input.GetMouseButtonDown(0))
            {
                HandleLeftClick(mouseWorldPos);
            }
            
            // Mouse wheel for zooming
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                HandleZoom(scroll, mouseWorldPos);
            }
            
            // Handle dynamic connection slot selection
            if (!string.IsNullOrEmpty(pendingFromNodeId) && currentMode == InteractionMode.Connect)
            {
                UpdateDynamicConnectionSlot(mouseWorldPos);
            }
            
            lastMouseWorldPos = mouseWorldPos;
        }
        
        private void HandleKeyboardInput()
        {
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                DeleteSelectedNode();
            }
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClearSelection();
            }
        }
        
        private void HandleRightClick(Vector3 worldPos)
        {
            if (currentMode == InteractionMode.Node)
            {
                CreateNode(worldPos);
            }
            else
            {
                ClearSelection();
            }
        }
        
        private void HandleLeftClick(Vector3 worldPos)
        {
            // Don't process clicks if the mouse is over UI elements
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            
            // Check if clicking on a node
            var hitNode = GetNodeAtPosition(worldPos);
            
            if (hitNode != null)
            {
                switch (currentMode)
                {
                    case InteractionMode.Select:
                        SelectNode(hitNode);
                        break;
                    case InteractionMode.Connect:
                        HandleConnectionClick(hitNode, worldPos);
                        break;
                }
            }
            else if (currentMode == InteractionMode.Node)
            {
                CreateNode(worldPos);
            }
            else
            {
                ClearSelection();
            }
        }
        
        private void HandleZoom(float scrollDelta, Vector3 mouseWorldPos)
        {
            float currentSize = graphCamera.orthographicSize;
            float newSize = currentSize * (1f - scrollDelta * zoomSpeed);
            newSize = Mathf.Clamp(newSize, zoomMin, zoomMax);
            
            // Zoom towards mouse position
            Vector3 worldPointBeforeZoom = graphCamera.ScreenToWorldPoint(Input.mousePosition);
            graphCamera.orthographicSize = newSize;
            Vector3 worldPointAfterZoom = graphCamera.ScreenToWorldPoint(Input.mousePosition);
            graphCamera.transform.position += worldPointBeforeZoom - worldPointAfterZoom;
            
            gridRenderer?.UpdateGrid();
            
            // Re-snap all nodes to maintain grid alignment after zoom
            ResnapAllNodesToGrid();
        }
        
        private void ResnapAllNodesToGrid()
        {
            if (gridRenderer == null) return;
            
            // Re-snap all existing nodes to maintain grid alignment
            foreach (var nodeInstance in nodeInstances.Values)
            {
                if (nodeInstance != null)
                {
                    Vector3 currentPos = nodeInstance.transform.position;
                    Vector3 snappedPos = gridRenderer.SnapToGrid(currentPos, true);
                    
                    // Update both the transform and the node data
                    nodeInstance.transform.position = snappedPos;
                    nodeInstance.NodeDataInstance.worldPosition = snappedPos;
                }
            }
            
            // Also re-snap all existing engine components
            ResnapAllEnginesToGrid();
        }
        
        private void ResnapAllEnginesToGrid()
        {
            // Engine nodes are now handled by ResnapAllNodesToGrid() since they are regular nodes
            // This method is kept for compatibility but does nothing
        }
        
        public SpriteNode.NodeData CreateNode(Vector3 worldPos)
        {
            // Snap the position to the nearest grid intersection
            Vector3 snappedPos = gridRenderer?.SnapToGrid(worldPos, true) ?? worldPos;
            
            // Check if selected type is an engine component
            if (nodePalette != null && nodePalette.SelectedNodeType != null && 
                nodePalette.SelectedNodeType.category == "Engines" && currentMode == InteractionMode.Node)
            {
                CreateEngineFromNodeType(snappedPos, nodePalette.SelectedNodeType);
                return null; // Return null since we created an engine, not a node
            }
            
            var nodeData = new SpriteNode.NodeData
            {
                id = Guid.NewGuid().ToString("N"),
                title = "New Node",
                worldPosition = snappedPos,
                metadata = new List<SpriteNode.MetadataEntry>(),
                isStart = nodes.Count == 0 // First node placed is automatically a start node
            };
            
            // Apply selected node type properties if available
            if (nodePalette != null && nodePalette.SelectedNodeType != null && currentMode == InteractionMode.Node)
            {
                var selectedType = nodePalette.SelectedNodeType;
                nodeData.title = selectedType.name;
                nodeData.color = selectedType.color;
                nodeData.note = selectedType.note;
                nodeData.velocity = selectedType.velocity;
                nodeData.channel = selectedType.channel;
                nodeData.duration = selectedType.duration;
                
                // Add metadata for node type info
                nodeData.metadata.Clear();
                nodeData.metadata.Add(new SpriteNode.MetadataEntry { key = "Type", value = selectedType.name });
                nodeData.metadata.Add(new SpriteNode.MetadataEntry { key = "Category", value = selectedType.category });
                nodeData.metadata.Add(new SpriteNode.MetadataEntry { key = "Description", value = selectedType.description });
            }
            
            nodes.Add(nodeData);
            CreateNodeInstance(nodeData);
            NodeCreated?.Invoke(nodeData);
            
            return nodeData;
        }
        
        private void CreateEngineFromNodeType(Vector3 worldPos, SpriteNodePalette.NodeTypeData nodeType)
        {
            // Create a regular SpriteNode with engine properties
            SpriteNode.EngineType engineType = nodeType.name switch
            {
                "Main Engine" => SpriteNode.EngineType.MainEngine,
                "Thruster" => SpriteNode.EngineType.Thruster,
                "Retro Engine" => SpriteNode.EngineType.RetroEngine,
                "Stability Engine" => SpriteNode.EngineType.StabilityEngine,
                _ => SpriteNode.EngineType.MainEngine
            };
            
            var nodeData = new SpriteNode.NodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                title = nodeType.name,
                worldPosition = worldPos,
                color = nodeType.color,
                note = nodeType.note,
                velocity = nodeType.velocity,
                channel = nodeType.channel,
                duration = nodeType.duration,
                metadata = new List<SpriteNode.MetadataEntry>(),
                
                // Engine-specific properties
                isEngine = true,
                engineType = engineType,
                thrust = GetEngineTypeThrust(engineType),
                efficiency = 0.8f,
                showThrustEffect = true,
                thrustColor = Color.cyan,
                gridWidth = GetEngineTypeGridDimensions(engineType).x,
                gridHeight = GetEngineTypeGridDimensions(engineType).y,
                connectedNodeIds = new List<string>()
            };
            
            // Add metadata for engine info
            nodeData.metadata.Add(new SpriteNode.MetadataEntry { key = "Type", value = nodeType.name });
            nodeData.metadata.Add(new SpriteNode.MetadataEntry { key = "Category", value = "Engine" });
            nodeData.metadata.Add(new SpriteNode.MetadataEntry { key = "Description", value = nodeType.description });
            
            nodes.Add(nodeData);
            CreateNodeInstance(nodeData);
            NodeCreated?.Invoke(nodeData);
        }
        
        private SpriteNode CreateNodeInstance(SpriteNode.NodeData nodeData)
        {
            GameObject nodeGO;
            
            if (nodePrefab != null)
            {
                nodeGO = Instantiate(nodePrefab, nodeData.worldPosition, Quaternion.identity, nodeContainer);
            }
            else
            {
                // Create default node GameObject
                nodeGO = CreateDefaultNode();
                nodeGO.transform.position = nodeData.worldPosition;
                nodeGO.transform.SetParent(nodeContainer);
            }
            
            var spriteNode = nodeGO.GetComponent<SpriteNode>();
            if (spriteNode == null)
            {
                spriteNode = nodeGO.AddComponent<SpriteNode>();
            }
            
            spriteNode.Initialize(this, nodeData);
            nodeInstances[nodeData.id] = spriteNode;
            
            return spriteNode;
        }
        
        private GameObject CreateDefaultNode()
        {
            var nodeGO = new GameObject("GraphNode");
            
            // Add sprite renderer
            var spriteRenderer = nodeGO.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateNodeSprite();
            spriteRenderer.color = Color.white;
            
            // Add collider for interaction
            var collider = nodeGO.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one * nodeSize;
            
            return nodeGO;
        }
        
        private UnityEngine.Sprite CreateNodeSprite()
        {
            // Create a simple square sprite
            var texture = new Texture2D(64, 64);
            var colors = new Color32[64 * 64];
            
            // Fill with white center and black border
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
            
            return UnityEngine.Sprite.Create(texture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 64f / nodeSize);
        }
        
        private void CreateConnectionInstance(SpriteConnection.ConnectionData connectionData)
        {
            GameObject connectionGO;
            
            if (connectionPrefab != null)
            {
                connectionGO = Instantiate(connectionPrefab, transform);
            }
            else
            {
                connectionGO = CreateDefaultConnection();
                connectionGO.transform.SetParent(transform);
            }
            
            var spriteConnection = connectionGO.GetComponent<SpriteConnection>();
            if (spriteConnection == null)
            {
                spriteConnection = connectionGO.AddComponent<SpriteConnection>();
            }
            
            spriteConnection.Initialize(this, connectionData);
            connectionInstances[connectionData.id] = spriteConnection;
        }
        
        private GameObject CreateDefaultConnection()
        {
            var connectionGO = new GameObject("GraphConnection");
            connectionGO.AddComponent<LineRenderer>();
            return connectionGO;
        }
        
        private SpriteNode GetNodeAtPosition(Vector3 worldPos)
        {
            var collider = Physics2D.OverlapPoint(worldPos, nodeLayer);
            if (collider != null)
            {
                return collider.GetComponent<SpriteNode>();
            }
            return null;
        }
        
        public void SelectNode(SpriteNode node)
        {
            // Deselect previous
            if (!string.IsNullOrEmpty(selectedNodeId) && nodeInstances.TryGetValue(selectedNodeId, out var prevNode))
            {
                prevNode.SetSelected(false);
            }
            
            // Hide configuration UI if no node or different node selected
            if (node == null || selectedNodeId != node?.NodeDataInstance.id)
            {
                nodeConfigUI?.HideNodeConfiguration();
            }
            
            selectedNodeId = node?.NodeDataInstance.id;
            
            // Select new
            if (node != null)
            {
                node.SetSelected(true);
                // Show screen space configuration UI
                nodeConfigUI?.ShowNodeConfiguration(node);
            }
            
            NodeSelected?.Invoke(selectedNodeId);
        }
        
        public void ClearSelection()
        {
            SelectNode(null);
            ClearPendingConnection();
        }
        
        public void ClearPendingConnection()
        {
            if (!string.IsNullOrEmpty(pendingFromNodeId) && nodeInstances.TryGetValue(pendingFromNodeId, out var node))
            {
                node.SetPendingHighlight(false);
            }
            
            pendingFromNodeId = null;
            pendingFromAnchorIndex = -1;
        }
        
        private void HandleConnectionClick(SpriteNode node, Vector3 worldPos)
        {
            int anchorIndex = node.GetNearestAvailableAnchorIndex(worldPos);
            if (anchorIndex == -1) return; // No available anchors
            
            if (string.IsNullOrEmpty(pendingFromNodeId))
            {
                // Start new connection
                pendingFromNodeId = node.NodeDataInstance.id;
                pendingFromAnchorIndex = anchorIndex;
                node.SetPendingHighlight(true);
            }
            else if (pendingFromNodeId != node.NodeDataInstance.id)
            {
                // Complete connection
                nodeInstances.TryGetValue(pendingFromNodeId, out var fromNode);
                if (fromNode)
                {
                    int anchorIndexFrom = node.GetNearestAvailableAnchorIndex(fromNode.GetAnchorWorldPosition(pendingFromAnchorIndex));
                
                    CreateConnection(pendingFromNodeId, pendingFromAnchorIndex, node.NodeDataInstance.id, anchorIndexFrom);
                }

                ClearPendingConnection();
            }
        }
        
        public void CreateConnection(string fromNodeId, int fromAnchor, string toNodeId, int toAnchor)
        {
            var connectionData = new SpriteConnection.ConnectionData
            {
                id = Guid.NewGuid().ToString("N"),
                fromNodeId = fromNodeId,
                fromAnchorIndex = fromAnchor,
                toNodeId = toNodeId,
                toAnchorIndex = toAnchor
            };
            
            connections.Add(connectionData);
            CreateConnectionInstance(connectionData);
            ConnectionCreated?.Invoke(connectionData);
        }
        
        public void StartConnection(string nodeId, int anchorIndex)
        {
            // Clear previous pending connection
            ClearPendingConnection();
            
            // Set new pending connection
            pendingFromNodeId = nodeId;
            pendingFromAnchorIndex = anchorIndex;
            
            if (nodeInstances.TryGetValue(nodeId, out var node))
            {
                node.SetPendingHighlight(true);
            }
        }
        
        public void OnNodeMoved(SpriteNode node)
        {
            // Update connections when a node is moved
            foreach (var connection in connectionInstances.Values)
            {
                if (connection.ConnectionDataInstance.fromNodeId == node.NodeDataInstance.id || 
                    connection.ConnectionDataInstance.toNodeId == node.NodeDataInstance.id)
                {
                    // Connection will update itself in its Update method
                }
            }
        }
        
        private void UpdateDynamicConnectionSlot(Vector3 mouseWorldPos)
        {
            // Get the starting node for the pending connection
            if (nodeInstances.TryGetValue(pendingFromNodeId, out var fromNode))
            {
                // Find the nearest available anchor on the starting node based on mouse position
                int newAnchorIndex = fromNode.GetNearestAvailableAnchorIndex(mouseWorldPos);
                
                // Update the anchor if it's different and available
                if (newAnchorIndex != -1 && newAnchorIndex != pendingFromAnchorIndex)
                {
                    pendingFromAnchorIndex = newAnchorIndex;
                }
            }
        }
        
        private void DeleteSelectedNode()
        {
            if (string.IsNullOrEmpty(selectedNodeId)) return;
            
            // Remove connections
            var connectionsToRemove = connections.FindAll(c => 
                c.fromNodeId == selectedNodeId || c.toNodeId == selectedNodeId);
            
            foreach (var conn in connectionsToRemove)
            {
                if (connectionInstances.TryGetValue(conn.id, out var connInstance))
                {
                    DestroyImmediate(connInstance.gameObject);
                    connectionInstances.Remove(conn.id);
                }
                connections.Remove(conn);
            }
            
            // Remove node
            if (nodeInstances.TryGetValue(selectedNodeId, out var nodeInstance))
            {
                var nodeData = nodeInstance.NodeDataInstance;
                DestroyImmediate(nodeInstance.gameObject);
                nodeInstances.Remove(selectedNodeId);
                nodes.Remove(nodeData);
            }
            
            selectedNodeId = null;
        }
        
        private void OnModeChanged(InteractionMode newMode)
        {
            currentMode = newMode;
            if (newMode != InteractionMode.Connect)
            {
                ClearPendingConnection();
            }
            
            // Show/hide node palette based on interaction mode
            if (nodePalette != null)
            {
                nodePalette.SetVisible(newMode == InteractionMode.Node);
            }
        }
        
        public bool IsAnchorAvailable(string nodeId, int anchorIndex)
        {
            return !connections.Exists(c => 
                (c.fromNodeId == nodeId && c.fromAnchorIndex == anchorIndex) ||
                (c.toNodeId == nodeId && c.toAnchorIndex == anchorIndex));
        }
        
        public SpriteNode GetNode(string nodeId)
        {
            nodeInstances.TryGetValue(nodeId, out var node);
            return node;
        }
        
        public SpriteConnection GetConnectionInstance(string connectionId)
        {
            connectionInstances.TryGetValue(connectionId, out var connection);
            return connection;
        }
        
        public Vector3 WorldToScreen(Vector3 worldPos)
        {
            return graphCamera.WorldToScreenPoint(worldPos);
        }
        
        public Vector3 ScreenToWorld(Vector3 screenPos)
        {
            var worldPos = graphCamera.ScreenToWorldPoint(screenPos);
            worldPos.z = 0;
            return worldPos;
        }
        
        // Integration methods for traveler system
        public float GetCurrentTempoMultiplier()
        {
            return playbackController?.CurrentTempoMultiplier ?? 1f;
        }
        
        public float GetQuarterNoteInterval()
        {
            return playbackController?.QuarterNoteInterval ?? 0.5f;
        }
        
        public float CalculateTravelTimeFromDistance(SpriteConnection connection)
        {
            if (connection == null || gridRenderer == null) return 0.5f; // Default fallback
            
            // Get connection length in grid tiles (1 grid tile = 1 quarter note)
            float distanceInGridTiles = connection.GetLengthInGridTiles(gridRenderer.gridSpacing);
            
            // Convert to time using quarter note interval
            float quarterNoteInterval = GetQuarterNoteInterval();
            return distanceInGridTiles * quarterNoteInterval;
        }
        
        public void OnTravelerArrivedAtNode(SpriteTraveler traveler, SpriteNode node)
        {
            // Play beep sound when traveler arrives at node
            if (nodeBeepSource != null && nodeBeepClip != null)
            {
                
                //nodeBeepSource.PlayOneShot(nodeBeepClip, nodeBeepVolume);
            }

            OnNodeActivated(node.NodeDataInstance);
            
            // Hook for additional processing when travelers arrive at nodes
            // Can be extended for music/MIDI event triggering, etc.
            
            // Find outgoing connections from this node
            var outgoingConnections = connections.FindAll(c => c.fromNodeId == node.NodeDataInstance.id);
            
            if (outgoingConnections.Count > 0)
            {
                // For now, take the first available connection (could be enhanced with path behavior logic)
                var nextConnection = outgoingConnections[0];
                var nextNode = GetNode(nextConnection.toNodeId);
                
                if (nextNode != null)
                {
                    // Get the connection instance to pass path information
                    SpriteConnection connectionInstance = null;
                    if (connectionInstances.TryGetValue(nextConnection.id, out connectionInstance))
                    {
                        // Calculate travel time based on connection distance in grid tiles
                        float travelTime = CalculateTravelTimeFromDistance(connectionInstance);
                        // Continue movement following the connection path
                        traveler.StartMovementWithConnection(nextConnection.toNodeId, travelTime, connectionInstance);
                    }
                    else
                    {
                        // Fallback to direct movement with default time if connection instance not found
                        traveler.StartMovement(nextConnection.toNodeId, GetQuarterNoteInterval());
                    }
                }
            }
            else
            {
                // No outgoing connections - traveler stops here and becomes inactive
                traveler.SetActive(false);
            }
        }
        
        // Properties for external access
        public InteractionMode CurrentMode => currentMode;
        public string SelectedNodeId => selectedNodeId;
        public string PendingFromNodeId => pendingFromNodeId;
        public int PendingFromAnchorIndex => pendingFromAnchorIndex;
        public Camera GraphCamera => graphCamera;
        public List<SpriteNode.NodeData> Nodes => nodes;
        public List<SpriteConnection.ConnectionData> Connections => connections;
        public SpritePlaybackController PlaybackController => playbackController;
        
        // Engine nodes are now handled as regular nodes - no separate engine management needed
        
        // Engine creation methods removed - engines are now created as regular nodes with engine properties
        
        private float GetEngineTypeThrust(SpriteNode.EngineType engineType)
        {
            return engineType switch
            {
                SpriteNode.EngineType.MainEngine => 200f,
                SpriteNode.EngineType.Thruster => 50f,
                SpriteNode.EngineType.RetroEngine => 150f,
                SpriteNode.EngineType.StabilityEngine => 30f,
                _ => 100f
            };
        }
        
        private Vector2Int GetEngineTypeGridDimensions(SpriteNode.EngineType engineType)
        {
            return engineType switch
            {
                SpriteNode.EngineType.MainEngine => new Vector2Int(3, 2), // Large main engine: 3x2 grid cells
                SpriteNode.EngineType.Thruster => new Vector2Int(1, 1), // Small thruster: 1x1 grid cell
                SpriteNode.EngineType.RetroEngine => new Vector2Int(2, 2), // Medium retro engine: 2x2 grid cells
                SpriteNode.EngineType.StabilityEngine => new Vector2Int(1, 2), // Tall stability engine: 1x2 grid cells
                _ => new Vector2Int(1, 1)
            };
        }
    }
}