using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RuntimeGraph.Components;

namespace RuntimeGraph
{
    /// <summary>
    /// Refactored RuntimeGraphUI that acts as a coordinator orchestrating modular components
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    public class RuntimeGraphUI_Refactored : MonoBehaviour
    {
        // Graph data - now using types from components
        public List<RuntimeGraphNodeManager.Node> nodes = new List<RuntimeGraphNodeManager.Node>();
        public List<RuntimeGraphConnectionManager.Connection> connections = new List<RuntimeGraphConnectionManager.Connection>();

        // UI Toolkit infrastructure
        private UIDocument _uiDoc;
        private PanelSettings _panelSettings;
        private VisualElement _root;
        private VisualElement _graphRoot;

        // Modular components
        private RuntimeGraphViewport _viewport;
        private RuntimeGraphNodeManager _nodeManager;
        private RuntimeGraphConnectionManager _connectionManager;
        private RuntimeGraphToolbar _toolbar;

        // Mouse tracking for connection preview
        private Vector2 _lastMouseLocal;
        
        // Debug dialog
        private RuntimeGraphSimpleDebugDialog _debugDialog;

        private void Awake()
        {
            EnsureUIDocument();
            BuildUI();
            InitializeComponents();
            SetupEventHandlers();
        }

        private void EnsureUIDocument()
        {
            _uiDoc = gameObject.GetComponent<UIDocument>();
            if (_uiDoc == null) _uiDoc = gameObject.AddComponent<UIDocument>();

            // Create transient PanelSettings at runtime if none is set
            if (_uiDoc.panelSettings == null)
            {
                _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                _panelSettings.name = "RuntimeGraph PanelSettings (Runtime)";
                _panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
                _panelSettings.referenceDpi = 96;
                _panelSettings.referenceResolution = new Vector2Int(1920, 1080);
                _uiDoc.panelSettings = _panelSettings;
            }
        }

        private void BuildUI()
        {
            _root = _uiDoc.rootVisualElement;
            _root.Clear(); // Clear any existing content
            _root.style.flexGrow = 1;
            _root.style.backgroundColor = new Color(0.12f, 0.12f, 0.15f);
            
            _graphRoot = new VisualElement();
            _graphRoot.style.position = Position.Absolute;
            _graphRoot.style.left = 0; _graphRoot.style.top = 0; _graphRoot.style.right = 0; _graphRoot.style.bottom = 0;
            _graphRoot.style.overflow = Overflow.Hidden;
            _root.Add(_graphRoot);
        }

        private void InitializeComponents()
        {
            // Initialize viewport (handles pan, zoom, grid)
            _viewport = new RuntimeGraphViewport(_graphRoot);
            
            // Initialize connection manager first
            _connectionManager = new RuntimeGraphConnectionManager(_graphRoot, connections);
            
            // Initialize node manager with connection manager reference
            _nodeManager = new RuntimeGraphNodeManager(_graphRoot, nodes, _connectionManager);
            
            // Set node manager reference in connection manager
            _connectionManager.SetNodeManager(_nodeManager);
            
            // Initialize toolbar
            _toolbar = new RuntimeGraphToolbar(_root);
            
            // Initialize debug dialog
            _debugDialog = new RuntimeGraphSimpleDebugDialog(_root);
            
            // Set dependencies in node manager
            _nodeManager.SetToolbar(_toolbar);
            _nodeManager.SetViewport(_viewport);
            
            // Create all node views
            _nodeManager.CreateAllNodeViews();
        }

        private void Update()
        {
            // Handle keyboard input for debug dialog
            if (Input.GetKeyDown(KeyCode.D))
            {
                _debugDialog?.ToggleVisibility();
            }
            
            // Update debug dialog if visible
            if (_debugDialog != null && _debugDialog.IsVisible)
            {
                Vector2 mouseScreenPos = _lastMouseLocal;
                Vector2 mouseGraphPos = ScreenToGraph(mouseScreenPos);
                _debugDialog.UpdateDebugInfo(this, mouseScreenPos, mouseGraphPos);
            }
        }

        private void SetupEventHandlers()
        {
            // Register viewport events
            _viewport.RegisterEvents();
            
            // Handle context clicks for adding nodes or clearing selection
            _graphRoot.RegisterCallback<ContextClickEvent>(OnContextClick);
            
            // Track mouse position for connection previews
            _graphRoot.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            
            // Handle toolbar mode changes
            _toolbar.ModeChanged += OnModeChanged;
            
            // Handle node events
            _nodeManager.NodeRemoved += OnNodeRemoved;
        }

        private void OnContextClick(ContextClickEvent evt)
        {
            if (_toolbar.Mode == RuntimeGraphToolbar.InteractionMode.Node)
            {
                var graphPos = _viewport.ScreenToGraph(_graphRoot.WorldToLocal(evt.mousePosition));
                _nodeManager.AddNode(graphPos);
            }
            else
            {
                _nodeManager.SetSelectedNode(null);
                _connectionManager.SetPendingFrom(null, -1);
            }
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            _lastMouseLocal = _graphRoot.WorldToLocal(evt.position);
            
            // Update connection preview
            if (!string.IsNullOrEmpty(_connectionManager.PendingFromNodeId))
            {
                // Dynamically update the starting connection slot based on mouse position
                if (_nodeManager.TryGetNodeView(_connectionManager.PendingFromNodeId, out var fromView))
                {
                    Vector2 mouseWorldPos = _graphRoot.LocalToWorld(_lastMouseLocal);
                    Vector2 fromLocal = fromView.WorldToLocal(mouseWorldPos);
                    int newFromAnchor = fromView.GetNearestAvailableAnchorIndexFromLocal(fromLocal);
                    if (newFromAnchor >= 0 && newFromAnchor != _connectionManager.PendingFromAnchorIndex)
                    {
                        _connectionManager.SetPendingFrom(_connectionManager.PendingFromNodeId, newFromAnchor);
                    }
                }
                
                // Update preview endpoint for rendering
                Vector2 worldPos = _graphRoot.LocalToWorld(_lastMouseLocal);
                _connectionManager.SetPreviewEndpoint(worldPos);
            }
        }

        private void OnModeChanged(RuntimeGraphToolbar.InteractionMode newMode)
        {
            // Clear pending connections when switching modes
            if (newMode != RuntimeGraphToolbar.InteractionMode.Connect)
            {
                _connectionManager.SetPendingFrom(null, -1);
            }
        }

        private void OnNodeRemoved(RuntimeGraphNodeManager.Node node)
        {
            // Remove all connections involving this node
            _connectionManager.RemoveConnectionsForNode(node.id);
        }

        private void AddSampleNodes()
        {
            var node1 = _nodeManager.AddNode(new Vector2(100, 100));
            node1.title = "Start Node";
            node1.metadata.Add(new RuntimeGraphNodeManager.MetadataEntry { key = "Type", value = "Entry" });

            var node2 = _nodeManager.AddNode(new Vector2(400, 200));
            node2.title = "Process Node";
            node2.metadata.Add(new RuntimeGraphNodeManager.MetadataEntry { key = "Type", value = "Logic" });

            // Create a sample connection
            _connectionManager.AddConnection(node1, 0, node2, 6);
        }

        private void OnDestroy()
        {
            // Cleanup
            _viewport?.UnregisterEvents();
            _toolbar?.Destroy();
            _debugDialog?.Destroy();
            
            if (_panelSettings != null)
            {
                DestroyImmediate(_panelSettings);
            }
        }

        // Public API for external access
        public RuntimeGraphViewport Viewport => _viewport;
        public RuntimeGraphNodeManager NodeManager => _nodeManager;
        public RuntimeGraphConnectionManager ConnectionManager => _connectionManager;
        public RuntimeGraphToolbar Toolbar => _toolbar;

        // Backward compatibility properties
        public Vector2 Pan => _viewport?.Pan ?? Vector2.zero;
        public float Zoom => _viewport?.Zoom ?? 1f;
        public string SelectedNodeId => _nodeManager?.SelectedNodeId;

        // Helper methods for backward compatibility
        public Vector2 ScreenToGraph(Vector2 screenLocal) => _viewport?.ScreenToGraph(screenLocal) ?? screenLocal;
        public Vector2 GraphToScreen(Vector2 graphPoint) => _viewport?.GraphToScreen(graphPoint) ?? graphPoint;
        
        public RuntimeGraphNodeManager.Node AddNode(Vector2 graphPos) => _nodeManager?.AddNode(graphPos);
        public void RemoveNode(RuntimeGraphNodeManager.Node node) => _nodeManager?.RemoveNode(node);
        
        public RuntimeGraphConnectionManager.Connection AddConnection(RuntimeGraphNodeManager.Node from, int fromAnchor, RuntimeGraphNodeManager.Node to, int toAnchor)
            => _connectionManager?.AddConnection(from, fromAnchor, to, toAnchor);
        
        // Additional API methods for compatibility with original RuntimeGraphUI
        public bool TryGetNodeView(string id, out Components.NodeView view)
        {
            view = null;
            return _nodeManager?.TryGetNodeView(id, out view) ?? false;
        }
        public RuntimeGraphNodeManager.Node GetSelectedNode() => _nodeManager?.GetSelectedNode();
        public string GetPendingFromNodeId() => _connectionManager?.PendingFromNodeId;
        public int GetPendingFromAnchorIndex() => _connectionManager?.PendingFromAnchorIndex ?? -1;
        public bool IsAnchorAvailable(string nodeId, int anchorIndex) => _connectionManager?.IsAnchorAvailable(nodeId, anchorIndex) ?? true;
    }
}