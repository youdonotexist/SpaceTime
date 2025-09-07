using System;
using System.Collections.Generic;
using System.Linq;
using Commonwealth.Script.Ship.Monitors;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// Sprite-based RuntimeGraph system for world space usage
    /// Replaces UI Toolkit-based system to work in game world
    /// </summary>
    public class SpriteRuntimeGraph : MonoBehaviour, ProceduralShipLayoutGenerator.IShipLayoutTarget
    {
        [Header("Prefabs")]
        public GameObject nodePrefab;
        public GameObject connectionPrefab;
        public ShipStatsUI shipStatsUIPrefab;
        
        [Header("Settings")]
        public float nodeSize = 5f;
        public Color gridColor = new Color(1f, 1f, 1f, 0.1f);
        public float gridSpacing = 5f;
        public LayerMask nodeLayerMask = -1;
        public int nodeBlockLayer;
        public int nodeLayer;
        
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
        
        [Header("UI")]
        public NodeInfoTooltip nodeTooltip;
        
        [Header("Procedural Generation")]
        public ProceduralShipLayoutGenerator.GeneratorSettings generatorSettings;
        
        // Graph data
        [SerializeField] private List<SpriteNode.NodeData> nodes = new List<SpriteNode.NodeData>();
        [SerializeField] private List<SpriteConnection.ConnectionData> connections = new List<SpriteConnection.ConnectionData>();
        
        // Runtime components
        private Dictionary<string, SpriteNode> nodeInstances = new Dictionary<string, SpriteNode>();
        private Dictionary<string, SpriteConnection> connectionInstances = new Dictionary<string, SpriteConnection>();
        private ProceduralShipLayoutGenerator proceduralGenerator;
        private SpriteConnection pendingConnectionInstance;
        private SpriteGraphGrid gridRenderer;
        private SpriteGraphToolbar toolbar;
        private SpritePlaybackController playbackController;
        private SpriteNodeConfigUI nodeConfigUI;
        private SpriteConnectionConfigUI connectionConfigUI;
        private SpriteNodePalette nodePalette;
        private ShipPartCategoryUI categoryUI;
        private Transform nodeContainer;
        
        // Interaction state
        public enum InteractionMode { Select, Node, Connect, Play }
        public InteractionMode currentMode = InteractionMode.Select;
        
        private string selectedNodeId;
        private string selectedConnectionId;
        private string pendingFromNodeId;
        private int pendingFromAnchorIndex = -1;
        private SpriteNode draggedNode;
        private Vector3 dragOffset;
        
        // Ghost part state for temporary placement preview
        private GameObject ghostPart;
        private SpriteNodePalette.NodeTypeData ghostNodeType;
        private bool isGhostActive = false;
        
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
        public event Action<string> ConnectionSelected;
        public event Action<SpriteNode.NodeData> NodeCreated;
        public event Action<SpriteConnection.ConnectionData> ConnectionCreated;
        public System.Action<SpriteNode.NodeData> OnNodeActivated;
        
        private void Awake()
        {
            nodeBlockLayer = LayerMask.NameToLayer("PartNodeBlocks");
            nodeLayer = LayerMask.NameToLayer("PartNodes");
            
            InitializeCamera();
            InitializeNodeContainer();
            InitializeGrid();
            InitializeToolbar();
            InitializePlaybackController();
            InitializeNodeConfigUI();
            proceduralGenerator = new ProceduralShipLayoutGenerator(generatorSettings);
            InitializeConnectionConfigUI();
            InitializeNodePalette();
            InitializeCategoryUI();
            InitializeAudio();
            InitializeStatsUI();
            InitializeTooltip();
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
        
        private void InitializeConnectionConfigUI()
        {
            var connectionConfigGO = new GameObject("ConnectionConfigUI");
            connectionConfigGO.transform.SetParent(transform);
            connectionConfigUI = connectionConfigGO.AddComponent<SpriteConnectionConfigUI>();
            connectionConfigUI.Initialize(this);
            
            // Subscribe to events
            connectionConfigUI.OnConnectionDeleted += OnConnectionDeleted;
            connectionConfigUI.OnConnectionDirectionChanged += OnConnectionDirectionChanged;
        }
        
        private void InitializeNodePalette()
        {
            var nodePaletteGO = new GameObject("NodePalette");
            nodePaletteGO.transform.SetParent(transform);
            nodePalette = nodePaletteGO.AddComponent<SpriteNodePalette>();
            nodePalette.Initialize(this);
            nodePalette.SetVisible(false); // Hidden by default, only show in Node mode
        }
        
        private void InitializeCategoryUI()
        {
            var categoryUIGO = new GameObject("ShipPartCategoryUI");
            categoryUIGO.transform.SetParent(transform);
            categoryUI = categoryUIGO.AddComponent<ShipPartCategoryUI>();
            categoryUI.Initialize(nodePalette);
            
            // Subscribe to node selection events
            categoryUI.OnNodeTypeSelected += OnCategoryNodeTypeSelected;
        }
        
        private void OnCategoryNodeTypeSelected(SpriteNodePalette.NodeTypeData nodeType)
        {
            // Set the selected node type in the original palette system for compatibility
            if (nodePalette != null)
            {
                // Access the palette's selection mechanism
                nodePalette.OnNodeTypeSelected?.Invoke(nodeType);
            }
            
            // Switch to node placement mode
            currentMode = InteractionMode.Node;
            
            // Create ghost part for visual feedback
            CreateGhostPart(nodeType);
            
            Debug.Log($"Category UI selected: {nodeType.name} from {nodeType.category}");
        }
        
        private void CreateGhostPart(SpriteNodePalette.NodeTypeData nodeType)
        {
            // Clear any existing ghost part
            ClearGhostPart();
            
            // Store the node type data
            ghostNodeType = nodeType;
            
            // Create a temporary node data for the ghost part
            var tempNodeData = new SpriteNode.NodeData
            {
                id = "ghost_temp",
                title = nodeType.name,
                worldPosition = Vector3.zero,
                color = new Color(nodeType.color.r, nodeType.color.g, nodeType.color.b, 0.5f), // Semi-transparent
                metadata = new List<SpriteNode.MetadataEntry>
                {
                    new SpriteNode.MetadataEntry { key = "Type", value = nodeType.name },
                    new SpriteNode.MetadataEntry { key = "Category", value = nodeType.category },
                    new SpriteNode.MetadataEntry { key = "Description", value = nodeType.description }
                },
                note = nodeType.note,
                velocity = nodeType.velocity,
                channel = nodeType.channel,
                duration = nodeType.duration,
                icon = nodeType.icon,
                isEngine = nodeType.category == "Propulsion & Maneuvering"
            };
            
            // Create the ghost GameObject
            ghostPart = new GameObject("GhostPart");
            ghostPart.transform.SetParent(nodeContainer, false);
            
            // Add SpriteNode component and initialize it
            var ghostSpriteNode = ghostPart.AddComponent<SpriteNode>();
            ghostSpriteNode.Initialize(this, tempNodeData, partBlockLayer: nodeBlockLayer);
            ghostPart.layer = nodeLayer;
            
            
            // Make it semi-transparent and disable collider
            var renderers = ghostPart.GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in renderers)
            {
                var color = renderer.color;
                color.a = 0.5f;
                renderer.color = color;
            }
            
            var colliders = ghostPart.GetComponentsInChildren<Collider2D>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }
            
            isGhostActive = true;
        }
        
        private void ClearGhostPart()
        {
            if (ghostPart != null)
            {
                DestroyImmediate(ghostPart);
                ghostPart = null;
            }
            ghostNodeType = null;
            isGhostActive = false;
        }
        
        private void PlaceGhostPart(Vector3 worldPos)
        {
            if (!isGhostActive || ghostNodeType == null) return;
            
            // Snap the position to grid
            Vector3 snappedPos = gridRenderer?.SnapToGrid(worldPos, true) ?? worldPos;
            
            // Check for collision with existing nodes
            if (IsPositionOccupied(snappedPos))
            {
                Debug.LogWarning("Cannot place part: Position is already occupied!");
                return;
            }
            
            // Create the actual node from ghost data
            var nodeData = new SpriteNode.NodeData
            {
                id = Guid.NewGuid().ToString("N"),
                title = ghostNodeType.name,
                worldPosition = snappedPos,
                color = ghostNodeType.color,
                metadata = new List<SpriteNode.MetadataEntry>
                {
                    new SpriteNode.MetadataEntry { key = "Type", value = ghostNodeType.name },
                    new SpriteNode.MetadataEntry { key = "Category", value = ghostNodeType.category },
                    new SpriteNode.MetadataEntry { key = "Description", value = ghostNodeType.description }
                },
                note = ghostNodeType.note,
                velocity = ghostNodeType.velocity,
                channel = ghostNodeType.channel,
                duration = ghostNodeType.duration,
                icon = ghostNodeType.icon,
                isEngine = ghostNodeType.category == "Propulsion & Maneuvering"
            };
            
            // Add to nodes list and create instance
            nodes.Add(nodeData);
            CreateNodeInstance(nodeData);
            NodeCreated?.Invoke(nodeData);
            
            // Clear the ghost part after placement
            ClearGhostPart();
            
            if (ghostNodeType != null)
                Debug.Log($"Placed ship part: {ghostNodeType.name} at {snappedPos}");
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
            // Disabled complex ship stats UI - replaced with simplified dashboard
            // Instantiate(shipStatsUIPrefab, FindFirstObjectByType<Canvas>().transform);
            
            // Create simplified ship dashboard
            CreateSimplifiedShipDashboard();
        }
        
        private void CreateSimplifiedShipDashboard()
        {
            // Create a dedicated canvas for the ship dashboard to ensure it stays visible across all modes
            var dashboardCanvasGO = new GameObject("ShipDashboard Canvas");
            var dashboardCanvas = dashboardCanvasGO.AddComponent<Canvas>();
            dashboardCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dashboardCanvas.sortingOrder = 100; // High sorting order to appear above other UI
            
            var canvasScaler = dashboardCanvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            dashboardCanvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Create dashboard container
            var dashboardGO = new GameObject("SimplifiedShipDashboard");
            dashboardGO.transform.SetParent(dashboardCanvas.transform);
            
            var dashboardComponent = dashboardGO.AddComponent<SimplifiedShipDashboard>();
            dashboardComponent.Initialize(this);
        }
        
        private void InitializeTooltip()
        {
            // Create or find the NodeInfoTooltip instance
            if (nodeTooltip == null)
            {
                nodeTooltip = FindObjectOfType<NodeInfoTooltip>();
                
                if (nodeTooltip == null)
                {
                    var tooltipGO = new GameObject("NodeInfoTooltip");
                    nodeTooltip = tooltipGO.AddComponent<NodeInfoTooltip>();
                    Debug.Log("Created NodeInfoTooltip system");
                }
            }
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
            HandleGhostPart();
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
                    
                    // Update all connection colliders after panning completes
                    UpdateAllConnectionColliders();
                }
                
                // Update grid less frequently during smooth panning to reduce jitter
                if (Time.frameCount % 3 == 0)
                {
                    gridRenderer?.UpdateGrid();
                }
            }
        }
        
        private void HandleGhostPart()
        {
            if (!isGhostActive || ghostPart == null) return;
            
            // Get mouse world position
            Vector3 mouseWorldPos = graphCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            
            // Snap to grid
            Vector3 snappedPos = gridRenderer?.SnapToGrid(mouseWorldPos, true) ?? mouseWorldPos;
            
            // Update ghost part position
            ghostPart.transform.position = snappedPos;
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
                // Clear ghost part first, then selection
                if (isGhostActive)
                {
                    ClearGhostPart();
                }
                else
                {
                    ClearSelection();
                }
            }
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                RotateSelectedNode();
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                RotateSelectedNode();
            }
        }
        
        private void RotateSelectedNode()
        {
            if (string.IsNullOrEmpty(selectedNodeId)) return;
            
            var selectedNode = GetNode(selectedNodeId);
            if (selectedNode == null) return;
            
            var nodeData = selectedNode.NodeDataInstance;
            
            // Rotate by 90 degrees, snapping to grid-aligned angles (0, 90, 180, 270)
            nodeData.rotation += 90f;
            if (nodeData.rotation >= 360f)
            {
                nodeData.rotation = 0f;
            }
            
            // Update the visual representation
            selectedNode.UpdateVisuals();
            
            Debug.Log($"Rotated node '{nodeData.title}' to {nodeData.rotation} degrees");
        }
        
        private void HandleRightClick(Vector3 worldPos)
        {
            // Cancel any pending connection on right-click
            if (!string.IsNullOrEmpty(pendingFromNodeId))
            {
                ClearPendingConnection();
                return;
            }
            
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
                // Handle ghost part placement
                if (isGhostActive && ghostNodeType != null)
                {
                    PlaceGhostPart(worldPos);
                }
                else
                {
                    CreateNode(worldPos);
                }
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
            
            // Update all connection colliders after nodes have been repositioned
            UpdateAllConnectionColliders();
        }
        
        private void ResnapAllEnginesToGrid()
        {
            // Engine nodes are now handled by ResnapAllNodesToGrid() since they are regular nodes
            // This method is kept for compatibility but does nothing
        }
        
        private void UpdateAllConnectionColliders()
        {
            // Update all connection colliders after nodes have been repositioned during zoom
            foreach (var connectionInstance in connectionInstances.Values)
            {
                if (connectionInstance != null)
                {
                    connectionInstance.RefreshCollider();
                }
            }
        }
        
        public SpriteNode.NodeData CreateNode(Vector3 worldPos)
        {
            // Snap the position to the nearest grid intersection
            Vector3 snappedPos = gridRenderer?.SnapToGrid(worldPos, true) ?? worldPos;
            
            // Check for collision with existing nodes
            if (IsPositionOccupied(snappedPos))
            {
                Debug.LogWarning("Cannot place node: Position is already occupied!");
                return null; // Prevent placement on occupied positions
            }
            
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
                nodeData.icon = selectedType.icon;
                
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
            // Check for collision with existing nodes
            if (IsPositionOccupied(worldPos))
            {
                Debug.LogWarning("Cannot place engine node: Position is already occupied!");
                return; // Prevent placement on occupied positions
            }
            
            // Determine engine type from node type name
            var engineType = DetermineEngineTypeFromName(nodeType.name);
            
            var nodeData = new SpriteNode.NodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                title = nodeType.name,
                worldPosition = worldPos,
                color = nodeType.color,
                note = nodeType.note,
                velocity = nodeType.velocity,
                channel = GetEngineTypeChannel(engineType), // Each engine category uses its own channel
                duration = nodeType.duration,
                metadata = new List<SpriteNode.MetadataEntry>(),
                
                // Set as engine part
                isEngine = true,
                engineType = engineType,
                thrust = 1.0f,
                efficiency = 0.8f,
                showThrustEffect = DetermineShowThrustEffect(nodeType.name),
                thrustColor = nodeType.color,
                gridWidth = 1,
                gridHeight = 1,
                connectedNodeIds = new List<string>()
            };
            
            // Add metadata for engine part info
            nodeData.metadata.Add(new SpriteNode.MetadataEntry { key = "Type", value = nodeType.name });
            nodeData.metadata.Add(new SpriteNode.MetadataEntry { key = "Category", value = nodeType.category });
            nodeData.metadata.Add(new SpriteNode.MetadataEntry { key = "Description", value = nodeType.description });
            
            // Connect to ship stats system
            ConnectNodeToShipStats(nodeData, nodeType);
            
            nodes.Add(nodeData);
            CreateNodeInstance(nodeData);
            NodeCreated?.Invoke(nodeData);
        }
        
        private bool DetermineShowThrustEffect(string partName)
        {
            return partName.Contains("Thruster") || partName.Contains("Engine") || 
                   partName.Contains("Propulsion") || partName.Contains("Drive");
        }
        
        private SpriteNode.EngineType DetermineEngineTypeFromName(string partName)
        {
            string lowerName = partName.ToLowerInvariant();
            
            // Check for thruster keywords
            if (lowerName.Contains("thruster") || lowerName.Contains("rcs") || 
                lowerName.Contains("maneuvering") || lowerName.Contains("attitude"))
            {
                return SpriteNode.EngineType.Thruster;
            }
            
            // Check for retro engine keywords
            if (lowerName.Contains("retro") || lowerName.Contains("reverse") || 
                lowerName.Contains("brake") || lowerName.Contains("deceleration"))
            {
                return SpriteNode.EngineType.RetroEngine;
            }
            
            // Check for stability engine keywords
            if (lowerName.Contains("stability") || lowerName.Contains("stabilizer") || 
                lowerName.Contains("gyro") || lowerName.Contains("control"))
            {
                return SpriteNode.EngineType.StabilityEngine;
            }
            
            // Default to main engine for anything else with engine-like keywords
            if (lowerName.Contains("engine") || lowerName.Contains("propulsion") || 
                lowerName.Contains("drive") || lowerName.Contains("motor"))
            {
                return SpriteNode.EngineType.MainEngine;
            }
            
            // Fallback to main engine
            return SpriteNode.EngineType.MainEngine;
        }
        
        private void ConnectNodeToShipStats(SpriteNode.NodeData nodeData, SpriteNodePalette.NodeTypeData nodeType)
        {
            // Find the ship stats manager
            var statsManager = FindObjectOfType<Commonwealth.Script.Ship.Monitors.ShipStatsManager>();
            if (statsManager == null) return;
            
            // Store affected stats in metadata for the node
            var enginePart = RuntimeGraph.Sprite.EnginePartCatalog.GetAllEngineParts()
                .FirstOrDefault(p => p.name == nodeType.name);
            
            if (enginePart != null && enginePart.affectedStats != null)
            {
                foreach (var statName in enginePart.affectedStats)
                {
                    nodeData.metadata.Add(new SpriteNode.MetadataEntry 
                    { 
                        key = "AffectedStat", 
                        value = statName 
                    });
                }
            }
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
            
            nodeGO.layer = nodeLayer;
            
            var spriteNode = nodeGO.GetComponent<SpriteNode>();
            if (spriteNode == null)
            {
                spriteNode = nodeGO.AddComponent<SpriteNode>();
            }
            
            spriteNode.Initialize(this, nodeData, nodeBlockLayer);
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
                connectionGO.transform.position = Vector3.zero;
            }
            else
            {
                connectionGO = CreateDefaultConnection();
                connectionGO.transform.SetParent(transform);
                connectionGO.transform.position = Vector3.zero;
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
            var collider = Physics2D.OverlapPoint(worldPos, nodeLayerMask);
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
        
        public void SelectConnection(SpriteConnection connection)
        {
            // Clear node selection
            if (!string.IsNullOrEmpty(selectedNodeId) && nodeInstances.TryGetValue(selectedNodeId, out var prevNode))
            {
                prevNode.SetSelected(false);
            }
            selectedNodeId = null;
            nodeConfigUI?.HideNodeConfiguration();
            
            // Deselect previous connection
            if (!string.IsNullOrEmpty(selectedConnectionId) && connectionInstances.TryGetValue(selectedConnectionId, out var prevConnection))
            {
                prevConnection.UpdateVisualStyle(false);
            }
            
            selectedConnectionId = connection?.ConnectionDataInstance?.id;
            
            // Select new connection
            if (connection != null && currentMode == InteractionMode.Select)
            {
                connection.UpdateVisualStyle(true);
                // Show connection configuration UI
                connectionConfigUI?.ShowConnectionConfiguration(connection);
            }
            
            ConnectionSelected?.Invoke(selectedConnectionId);
        }
        
        public void ClearSelection()
        {
            SelectNode(null);
            SelectConnection(null);
            ClearPendingConnection();
        }
        
        public void ClearPendingConnection()
        {
            if (!string.IsNullOrEmpty(pendingFromNodeId) && nodeInstances.TryGetValue(pendingFromNodeId, out var node))
            {
                node.SetPendingHighlight(false);
            }
            
            // Destroy visual pending connection
            if (pendingConnectionInstance != null)
            {
                DestroyImmediate(pendingConnectionInstance.gameObject);
                pendingConnectionInstance = null;
            }
            
            pendingFromNodeId = null;
            pendingFromAnchorIndex = -1;
        }
        
        private void HandleConnectionClick(SpriteNode node, Vector3 worldPos)
        {
            // Only allow connections when clicking on port blocks (new approach) for ship parts
            if (!node.IsPositionOnPortBlock(worldPos))
                return; // Click is not on a port block, ignore
                
            int anchorIndex = node.GetNearestAvailableAnchorIndex(worldPos);
            if (anchorIndex == -1) return; // No available anchors
            
            if (string.IsNullOrEmpty(pendingFromNodeId))
            {
                // Start new connection
                pendingFromNodeId = node.NodeDataInstance.id;
                pendingFromAnchorIndex = anchorIndex;
                node.SetPendingHighlight(true);
                
                // Create visual pending connection
                CreatePendingConnection(pendingFromNodeId, pendingFromAnchorIndex);
            }
            else if (pendingFromNodeId != node.NodeDataInstance.id)
            {
                // Complete connection - also check that the target position is on a port block
                nodeInstances.TryGetValue(pendingFromNodeId, out var fromNode);
                if (fromNode)
                {
                    int anchorIndexTo = node.GetNearestAvailableAnchorIndex(worldPos);
                    CreateConnection(pendingFromNodeId, pendingFromAnchorIndex, node.NodeDataInstance.id, anchorIndexTo);
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
            
            // Create visual pending connection
            CreatePendingConnection(nodeId, anchorIndex);
        }
        
        private void CreatePendingConnection(string fromNodeId, int fromAnchorIndex)
        {
            // Clear any existing pending connection first
            if (pendingConnectionInstance != null)
            {
                DestroyImmediate(pendingConnectionInstance.gameObject);
            }
            
            // Create pending connection data
            var pendingConnectionData = new SpriteConnection.ConnectionData
            {
                id = "pending",
                fromNodeId = fromNodeId,
                fromAnchorIndex = fromAnchorIndex,
                toNodeId = "",
                toAnchorIndex = -1
            };
            
            // Create connection GameObject
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
            
            // Setup pending connection
            var spriteConnection = connectionGO.GetComponent<SpriteConnection>();
            if (spriteConnection == null)
            {
                spriteConnection = connectionGO.AddComponent<SpriteConnection>();
            }
            
            spriteConnection.InitializeAsPending(this, pendingConnectionData);
            pendingConnectionInstance = spriteConnection;
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
            
            // Clear any pending connection when switching away from Connect mode
            if (newMode != InteractionMode.Connect)
            {
                ClearPendingConnection();
            }
            
            // Show/hide node palette based on interaction mode
            if (nodePalette != null)
            {
                nodePalette.SetVisible(false); // Always hide the node palette
            }
            
            // Show/hide ship part category UI based on interaction mode
            if (categoryUI != null)
            {
                categoryUI.SetVisible(newMode == InteractionMode.Node);
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
            
            // Get grid-based node-to-node distance (1 grid tile = 1 quarter note)
            // This ensures travel time is based on node positions, not port mount points
            float distanceInGridTiles = connection.GetGridDistance(gridRenderer.gridSpacing);
            
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

            OnNodeActivated?.Invoke(node.NodeDataInstance);
            
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
        public string SelectedConnectionId => selectedConnectionId;
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
        
        private int GetEngineTypeChannel(SpriteNode.EngineType engineType)
        {
            return engineType switch
            {
                SpriteNode.EngineType.MainEngine => 9,      // Channel 0 for MainEngine
                SpriteNode.EngineType.Thruster => 1,        // Channel 1 for Thruster
                SpriteNode.EngineType.RetroEngine => 2,     // Channel 2 for RetroEngine
                SpriteNode.EngineType.StabilityEngine => 3, // Channel 3 for StabilityEngine
                _ => 0
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
        
        // Procedural Ship Layout Generation
        public void GenerateRandomShipLayout()
        {
            proceduralGenerator?.GenerateRandomShipLayout(this);
        }
        
        public void ClearAllNodesAndConnections()
        {
            // Clear connections first to avoid orphaned references
            var connectionInstancesCopy = new List<SpriteConnection>(connectionInstances.Values);
            foreach (var connection in connectionInstancesCopy)
            {
                if (connection != null)
                {
                    DestroyImmediate(connection.gameObject);
                }
            }
            connectionInstances.Clear();
            connections.Clear();
            
            // Clear nodes
            var nodeInstancesCopy = new List<SpriteNode>(nodeInstances.Values);
            foreach (var node in nodeInstancesCopy)
            {
                if (node != null)
                {
                    DestroyImmediate(node.gameObject);
                }
            }
            nodeInstances.Clear();
            nodes.Clear();
            
            // Clear selection
            ClearSelection();
        }
        
        
        public SpriteNode.NodeData CreateShipPartNode(EnginePartNodeData part, Vector3 position)
        {
            return CreateShipPartNode(part, position, false);
        }

        public SpriteNode.NodeData CreateShipPartNode(EnginePartNodeData part, Vector3 position, bool isStartNode)
        {
            // Determine engine type from part name
            var engineType = DetermineEngineTypeFromName(part.name);
            
            var nodeData = new SpriteNode.NodeData
            {
                id = System.Guid.NewGuid().ToString("N"),
                title = part.name,
                worldPosition = position,
                color = part.color,
                metadata = new List<SpriteNode.MetadataEntry>(),
                isStart = isStartNode, // Set the start node status
                
                // Set as engine part
                isEngine = true,
                engineType = engineType,
                thrust = UnityEngine.Random.Range(0.5f, 2.0f),
                efficiency = UnityEngine.Random.Range(0.6f, 0.9f),
                showThrustEffect = DetermineShowThrustEffect(part.name),
                thrustColor = part.color,
                gridWidth = 1,
                gridHeight = 1,
                rotation = UnityEngine.Random.Range(0, 4) * 90f, // Random rotation (0, 90, 180, or 270 degrees)
                connectedNodeIds = new List<string>(),
                
                // MIDI properties - Use drum channel and specific notes for propulsion/energy parts
                note = part.usesDrumChannel ? part.drumNote : UnityEngine.Random.Range(36, 84),
                velocity = UnityEngine.Random.Range(60, 100),
                channel = part.usesDrumChannel ? 9 : GetEngineTypeChannel(engineType), // Channel 9 for drums, others use engine type channel
                duration = 0.08f,
                icon = EnginePartIconGenerator.GenerateIconForPart(part)
            };
            
            // Add metadata
            nodeData.metadata.Add(new SpriteNode.MetadataEntry { key = "Type", value = part.name });
            nodeData.metadata.Add(new SpriteNode.MetadataEntry { key = "Category", value = part.category });
            nodeData.metadata.Add(new SpriteNode.MetadataEntry { key = "Description", value = part.description });
            
            // Add drum-specific metadata for propulsion and energy parts
            if (part.usesDrumChannel)
            {
                nodeData.metadata.Add(new SpriteNode.MetadataEntry { key = "MIDI Channel", value = "9 (Drum Channel)" });
                nodeData.metadata.Add(new SpriteNode.MetadataEntry { key = "Drum Sound", value = part.drumSoundName });
                nodeData.metadata.Add(new SpriteNode.MetadataEntry { key = "MIDI Note", value = part.drumNote.ToString() });
            }
            
            nodes.Add(nodeData);
            CreateNodeInstance(nodeData);
            NodeCreated?.Invoke(nodeData);
            
            return nodeData;
        }
        
        public void CreateAutoConnection(string fromNodeId, string toNodeId)
        {
            var connectionData = new SpriteConnection.ConnectionData
            {
                id = System.Guid.NewGuid().ToString("N"),
                fromNodeId = fromNodeId,
                toNodeId = toNodeId,
                fromAnchorIndex = 0, // Use first available anchor
                toAnchorIndex = 0,
                weight = UnityEngine.Random.Range(0.5f, 2.0f),
                creationOrder = connections.Count
            };
            
            connections.Add(connectionData);
            CreateConnectionInstance(connectionData);
        }

        // IShipLayoutTarget interface implementation
        public Vector3 SnapToGrid(Vector3 position)
        {
            return gridRenderer?.SnapToGrid(position, true) ?? position;
        }

        public int GetCurrentNodeCount()
        {
            return nodes.Count;
        }

        public bool IsPositionOccupied(Vector3 position, float tolerance = 1f)
        {
            // For composite ship parts, use shape-based collision detection
            foreach (var nodeInstance in nodeInstances.Values)
            {
                if (nodeInstance == null) continue;
                
                // Check if this is a composite ship part
                var compositeRenderer = nodeInstance.GetComponent<CompositeShipPartRenderer>();
                if (compositeRenderer != null)
                {
                    // Use composite part collision detection
                    var occupiedPositions = compositeRenderer.GetOccupiedGridPositions();
                    foreach (var occupiedPos in occupiedPositions)
                    {
                        if (Vector3.Distance(occupiedPos, position) < tolerance)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    // Fallback to simple distance check for non-composite parts
                    float distance = Vector3.Distance(nodeInstance.transform.position, position);
                    if (distance < tolerance)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if placing a composite ship part at the given position would overlap with existing parts
        /// </summary>
        public bool WouldCompositePartOverlap(CompositeShipPartRenderer newPart, Vector3 proposedPosition, float rotationDegrees = 0f)
        {
            if (newPart == null) return false;
            
            // Get the positions the new part would occupy
            var proposedPositions = newPart.GetProposedOccupiedPositions(proposedPosition, rotationDegrees);
            
            // Check against all existing composite parts
            foreach (var nodeInstance in nodeInstances.Values)
            {
                if (nodeInstance == null) continue;
                
                var existingComposite = nodeInstance.GetComponent<CompositeShipPartRenderer>();
                if (existingComposite != null && existingComposite != newPart)
                {
                    // Check if any of our proposed positions would overlap with this existing part
                    if (existingComposite.OccupiesAnyPosition(proposedPositions))
                    {
                        return true;
                    }
                }
                else if (existingComposite == null)
                {
                    // Check against non-composite parts using simple distance check
                    var nodePos = nodeInstance.transform.position;
                    foreach (var proposedPos in proposedPositions)
                    {
                        if (Vector3.Distance(nodePos, proposedPos) < 1f)
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        // Connection configuration event handlers
        private void OnConnectionDeleted(SpriteConnection connection)
        {
            if (connection == null || connection.ConnectionDataInstance == null) return;
            
            string connectionId = connection.ConnectionDataInstance.id;
            
            // Remove from data structures
            connections.RemoveAll(c => c.id == connectionId);
            
            if (connectionInstances.TryGetValue(connectionId, out var connectionInstance))
            {
                connectionInstances.Remove(connectionId);
                DestroyImmediate(connectionInstance.gameObject);
            }
            
            // Clear selection
            SelectConnection(null);
        }
        
        private void OnConnectionDirectionChanged(SpriteConnection connection, bool shouldReverse)
        {
            if (connection == null || connection.ConnectionDataInstance == null) return;
            
            var connectionData = connection.ConnectionDataInstance;
            
            if (shouldReverse)
            {
                // Swap from and to nodes
                string tempNodeId = connectionData.fromNodeId;
                int tempAnchorIndex = connectionData.fromAnchorIndex;
                
                connectionData.fromNodeId = connectionData.toNodeId;
                connectionData.fromAnchorIndex = connectionData.toAnchorIndex;
                connectionData.toNodeId = tempNodeId;
                connectionData.toAnchorIndex = tempAnchorIndex;
                
                //TODO: Update is called on a framebasis, so I don't think we need to call this
                // Update the connection display
                //connection.UpdateConnection();
                
                
                // Update the configuration UI to reflect the change
                connectionConfigUI?.ShowConnectionConfiguration(connection);
            }
        }
    }
}