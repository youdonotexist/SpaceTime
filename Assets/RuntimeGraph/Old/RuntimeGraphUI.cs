using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RuntimeGraph.Components;

namespace RuntimeGraph
{
    [DefaultExecutionOrder(-2000)]
    [System.Obsolete("This class is deprecated. Use RuntimeGraphUI_Refactored instead.")]

    
    public class RuntimeGraphUI : MonoBehaviour
    {
        public enum InteractionMode { Select, Node, Connect }
        [Serializable]
        public class MetadataEntry { public string key; public string value; }

        [Serializable]
        public class Node
        {
            public string id;
            public string title = "Node";
            public Rect graphRect = new Rect(0, 0, NodeSize, NodeSize); // square node by default
            public List<MetadataEntry> metadata = new List<MetadataEntry>();
            [NonSerialized] public bool metadataFoldout = true;
        }

        [Serializable]
        public class Connection { public string fromNodeId; public int fromAnchorIndex; public string toNodeId; public int toAnchorIndex; }

        // Graph data
        public List<Node> nodes = new List<Node>();
        public List<Connection> connections = new List<Connection>();

        // View state
        [SerializeField] private Vector2 _pan = Vector2.zero; // in panel pixels
        [SerializeField] private float _zoom = 1.0f;
        [SerializeField] private float _minZoom = 0.25f;
        [SerializeField] private float _maxZoom = 2.5f;

        // Colors
        private readonly Color _gridSmallColor = new Color(1f, 1f, 1f, 0.06f);
        private readonly Color _gridLargeColor = new Color(1f, 1f, 1f, 0.12f);
        private readonly Color _connectionColor = new Color(0.3f, 0.8f, 1f, 0.9f);
        private readonly Color _pendingConnectionColor = new Color(1f, 0.8f, 0.2f, 0.9f);

        // UI Toolkit
        private UIDocument _uiDoc;
        private PanelSettings _panelSettings;
        private VisualElement _root;
        private VisualElement _graphRoot; // contains grid, connections, content
        private GridLayer _gridLayer;
        private ConnectionLayer _connectionLayer;
        private VisualElement _content; // transformed by pan/zoom
        private VisualElement _nodesLayer;
        private VisualElement _toolbar;
        private Button _btnSelect, _btnNode, _btnConnect;
        private RuntimeGraphSimpleDebugDialog _debugDialog;

        private readonly Dictionary<string, NodeView> _nodeViews = new Dictionary<string, NodeView>();

        // Interaction state
        private bool _panning;
        private Vector2 _panStart;
        private Vector2 _panOrigin;
        private string _pendingFromNodeId; // for connections
        private int _pendingFromAnchorIndex = -1;
        private Vector2 _lastMouseLocal; // in graphRoot local coords
        private InteractionMode _mode = InteractionMode.Select;
        private string _selectedNodeId;

        // Right mouse tracking for pan vs context click
        private bool _rightButtonDown;
        private Vector2 _rightDownPos;
        private const float _panStartThreshold = 3f;

        private const float GridSmall = 32f;
        private const float GridLarge = 160f;
        private const float NodeSize = 120f;

        private void Awake()
        {
            EnsureUIDocument();
            BuildUI();

            // Migration: ensure all existing nodes have unique IDs (older data may have empty ids)
            var usedIds = new HashSet<string>();
            for (int i = 0; i < nodes.Count; i++)
            {
                if (string.IsNullOrEmpty(nodes[i].id) || usedIds.Contains(nodes[i].id))
                {
                    nodes[i].id = Guid.NewGuid().ToString("N");
                }
                usedIds.Add(nodes[i].id);
            }

           

            // Build views for existing nodes
            foreach (var n in nodes) CreateNodeView(n);
            MarkAllForRepaint();

            // Debug dialog will be updated through direct method calls instead of dependencies
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
                _panelSettings.match = 1;
                _uiDoc.panelSettings = _panelSettings;
            }
        }

        private void BuildUI()
        {
            _root = _uiDoc.rootVisualElement;
            _root.style.flexGrow = 1;
            _root.style.backgroundColor = new Color(0.11f, 0.12f, 0.13f);

            _graphRoot = new VisualElement { name = "GraphRoot" };
            _graphRoot.style.flexGrow = 1;
            _graphRoot.style.position = Position.Relative;
            _graphRoot.pickingMode = PickingMode.Position;
            _root.Add(_graphRoot);

            // Grid layer
            _gridLayer = new GridLayer(this) { name = "GridLayer" };
            _gridLayer.style.position = Position.Absolute;
            _gridLayer.style.left = 0; _gridLayer.style.top = 0; _gridLayer.style.right = 0; _gridLayer.style.bottom = 0;
            _gridLayer.pickingMode = PickingMode.Ignore;
            _graphRoot.Add(_gridLayer);

            // Content (pan/zoom) with nodes inside
            _content = new VisualElement { name = "Content" };
            _content.style.position = Position.Absolute;
            _content.style.left = 0; _content.style.top = 0; _content.style.right = 0; _content.style.bottom = 0;
            _content.pickingMode = PickingMode.Ignore; // background events handled by GraphRoot
            _graphRoot.Add(_content);

            _nodesLayer = new VisualElement { name = "NodesLayer" };
            _nodesLayer.style.position = Position.Absolute;
            _nodesLayer.style.left = 0; _nodesLayer.style.top = 0; _nodesLayer.style.right = 0; _nodesLayer.style.bottom = 0;
            _content.Add(_nodesLayer);

            // Connections layer (drawn ABOVE nodes for clarity)
            _connectionLayer = new ConnectionLayer(this) { name = "ConnectionLayer" };
            _connectionLayer.style.position = Position.Absolute;
            _connectionLayer.style.left = 0; _connectionLayer.style.top = 0; _connectionLayer.style.right = 0; _connectionLayer.style.bottom = 0;
            _connectionLayer.pickingMode = PickingMode.Ignore;
            _graphRoot.Add(_connectionLayer);

            // Toolbar (top-left)
            BuildToolbar();

            // Debug dialog (top-right)
            _debugDialog = new RuntimeGraphSimpleDebugDialog(_root);

            // Input handlers on graphRoot
            _graphRoot.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            _graphRoot.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            _graphRoot.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            _graphRoot.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            _graphRoot.RegisterCallback<ContextClickEvent>(OnContextClick);
            
            // Keyboard input for debug dialog
            _root.RegisterCallback<KeyDownEvent>(OnKeyDown);

            ApplyPanZoom();
        }

        private void BuildToolbar()
        {
            _toolbar = new VisualElement { name = "Toolbar" };
            _toolbar.style.position = Position.Absolute;
            _toolbar.style.left = 8; _toolbar.style.top = 8;
            _toolbar.style.flexDirection = FlexDirection.Row;
            _toolbar.style.backgroundColor = new Color(0.15f, 0.15f, 0.17f, 0.9f);
            _toolbar.style.paddingLeft = 6; _toolbar.style.paddingRight = 6; _toolbar.style.paddingTop = 4; _toolbar.style.paddingBottom = 4;
            _toolbar.style.borderBottomLeftRadius = 4; _toolbar.style.borderBottomRightRadius = 4; _toolbar.style.borderTopLeftRadius = 4; _toolbar.style.borderTopRightRadius = 4;
            _toolbar.style.height = 32; // Explicit height to prevent collapse
            _toolbar.style.alignItems = Align.Center;
            _graphRoot.Add(_toolbar);

            _btnSelect = new Button(() => SetMode(InteractionMode.Select)) { text = "Select" };
            _btnNode = new Button(() => SetMode(InteractionMode.Node)) { text = "Node" };
            _btnConnect = new Button(() => SetMode(InteractionMode.Connect)) { text = "Connect" };
            StyleToolbarButton(_btnSelect); StyleToolbarButton(_btnNode); StyleToolbarButton(_btnConnect);
            _toolbar.Add(_btnSelect); _toolbar.Add(_btnNode); _toolbar.Add(_btnConnect);
            UpdateToolbarVisuals();
        }

        private void StyleToolbarButton(Button b)
        {
            b.style.marginLeft = 2; b.style.marginRight = 2;
            b.style.unityTextAlign = TextAnchor.MiddleCenter;
            b.style.minWidth = 70;
            b.style.height = 24; // Explicit height for proper visibility
            b.style.fontSize = 12; // Ensure text is properly sized
        }

        private void SetMode(InteractionMode mode)
        {
            if (_mode == mode) return;
            _mode = mode;
            if (_mode != InteractionMode.Connect)
            {
                SetPendingFrom(null, -1);
            }
            UpdateToolbarVisuals();
        }

        private void UpdateToolbarVisuals()
        {
            void Set(Button b, bool on)
            {
                b.SetEnabled(!on);
                b.style.backgroundColor = on ? new Color(0.25f, 0.5f, 0.9f, 1f) : new Color(0.2f, 0.2f, 0.22f, 1f);
                b.style.color = on ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f);
            }
            Set(_btnSelect, _mode == InteractionMode.Select);
            Set(_btnNode, _mode == InteractionMode.Node);
            Set(_btnConnect, _mode == InteractionMode.Connect);
        }

        private void SetSelectedNode(string id)
        {
            if (!string.IsNullOrEmpty(_selectedNodeId) && TryGetNodeView(_selectedNodeId, out var prev))
            {
                prev.SetSelected(false);
            }
            _selectedNodeId = id;
            if (!string.IsNullOrEmpty(_selectedNodeId) && TryGetNodeView(_selectedNodeId, out var view))
            {
                view.SetSelected(true);
            }
        }

        // ========== Public Model API ==========
        public Node AddNode(Vector2 graphPos)
        {
            var n = new Node
            {
                id = Guid.NewGuid().ToString("N"),
                title = "Node" + (nodes.Count + 1),
                graphRect = new Rect(graphPos.x, graphPos.y, NodeSize, NodeSize),
                metadata = new List<MetadataEntry> { new MetadataEntry { key = "tag", value = string.Empty } }
            };
            nodes.Add(n);
            CreateNodeView(n);
            MarkAllForRepaint();
            return n;
        }

        public void RemoveNode(Node n)
        {
            if (n == null) return;
            connections.RemoveAll(c => c.fromNodeId == n.id || c.toNodeId == n.id);
            if (_nodeViews.TryGetValue(n.id, out var view))
            {
                view.RemoveFromHierarchy();
                _nodeViews.Remove(n.id);
            }
            nodes.Remove(n);
            if (_pendingFromNodeId == n.id) _pendingFromNodeId = null;
            MarkAllForRepaint();
        }

        public void AddConnection(Node from, int fromAnchor, Node to, int toAnchor)
        {
            if (from == null || to == null || ReferenceEquals(from, to)) return;
            if (connections.Exists(c => c.fromNodeId == from.id && c.toNodeId == to.id && c.fromAnchorIndex == fromAnchor && c.toAnchorIndex == toAnchor)) return;
            connections.Add(new Connection { fromNodeId = from.id, fromAnchorIndex = fromAnchor, toNodeId = to.id, toAnchorIndex = toAnchor });
            _connectionLayer.MarkDirtyRepaint();
        }

        // ========== Event handlers ==========
        private void OnContextClick(ContextClickEvent evt)
        {
            if (evt.target != _graphRoot) return;
            Vector2 local = evt.localMousePosition; // relative to graphRoot
            if (_mode == InteractionMode.Node)
            {
                var g = ScreenToGraph(local);
                AddNode(g);
            }
            else
            {
                // In other modes, context-click clears selection/pending for clarity
                SetPendingFrom(null, -1);
                SetSelectedNode(null);
            }
        }

        private void OnWheel(WheelEvent evt)
        {
            _lastMouseLocal = evt.localMousePosition;
            float scroll = -evt.delta.y; // positive when scrolling up
            if (Mathf.Abs(scroll) < 0.0001f) return;

            Vector2 beforeGraph = ScreenToGraph(_lastMouseLocal);
            float zoomFactor = 1f + (scroll > 0 ? 0.1f : -0.1f);
            float newZoom = Mathf.Clamp(_zoom * zoomFactor, _minZoom, _maxZoom);
            _zoom = newZoom;

            // keep cursor-anchored
            Vector2 afterScreen = GraphToScreen(beforeGraph);
            Vector2 delta = _lastMouseLocal - afterScreen;
            _pan += delta;

            ApplyPanZoom();
            evt.StopPropagation();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            _lastMouseLocal = _graphRoot.WorldToLocal(evt.position);
            // Background interactions
            if (evt.target == _graphRoot)
            {
                if (evt.button == 0)
                {
                    if (_mode == InteractionMode.Select)
                    {
                        SetSelectedNode(null);
                        SetPendingFrom(null, -1);
                    }
                    else if (_mode == InteractionMode.Node)
                    {
                        var g = ScreenToGraph(_lastMouseLocal);
                        AddNode(g);
                    }
                    else if (_mode == InteractionMode.Connect)
                    {
                        SetPendingFrom(null, -1);
                    }
                    evt.StopPropagation();
                    return;
                }
                if (evt.button == 1) // RMB: prepare for possible pan; defer actions to ContextClick
                {
                    _rightButtonDown = true;
                    _rightDownPos = _graphRoot.WorldToLocal(evt.position);
                    // Do not stop propagation, so ContextClickEvent can fire if no drag happens
                    return;
                }
                if (evt.button == 2) // MMB
                {
                    _panning = true;
                    _panStart = _graphRoot.WorldToLocal(evt.position);
                    _panOrigin = _pan;
                    _graphRoot.CapturePointer(evt.pointerId);
                    evt.StopPropagation();
                    return;
                }
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            _lastMouseLocal = _graphRoot.WorldToLocal(evt.position);
            
            // Update debug dialog with current coordinate information
            UpdateDebugDialog();
            
            if (_panning)
            {
                Vector2 cur = _graphRoot.WorldToLocal(evt.position);
                Vector2 delta = cur - _panStart;
                _pan = _panOrigin + delta;
                ApplyPanZoom();
                evt.StopPropagation();
            }
            else
            {
                // If RMB is held and moved enough, start panning
                if (_rightButtonDown)
                {
                    Vector2 cur = _graphRoot.WorldToLocal(evt.position);
                    if ((cur - _rightDownPos).sqrMagnitude >= _panStartThreshold * _panStartThreshold)
                    {
                        _panning = true;
                        _panStart = _rightDownPos;
                        _panOrigin = _pan;
                        _graphRoot.CapturePointer(evt.pointerId);
                        evt.StopPropagation();
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(_pendingFromNodeId))
                {
                    // Dynamically update the starting connection slot based on mouse position
                    if (TryGetNodeView(_pendingFromNodeId, out var fromView))
                    {
                        Vector2 mouseWorldPos = _graphRoot.LocalToWorld(_lastMouseLocal);
                        Vector2 fromLocal = fromView.WorldToLocal(mouseWorldPos);
                        int newFromAnchor = fromView.GetNearestAvailableAnchorIndexFromLocal(fromLocal);
                        if (newFromAnchor >= 0 && newFromAnchor != _pendingFromAnchorIndex)
                        {
                            _pendingFromAnchorIndex = newFromAnchor;
                        }
                    }
                    _connectionLayer.MarkDirtyRepaint(); // update preview line
                }
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (_panning)
            {
                _panning = false;
                _graphRoot.ReleasePointer(evt.pointerId);
                evt.StopPropagation();
            }
            if (evt.button == 1)
            {
                _rightButtonDown = false;
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.D)
            {
                _debugDialog?.ToggleVisibility();
                evt.StopPropagation();
            }
        }

        private void UpdateDebugDialog()
        {
            if (_debugDialog != null && _debugDialog.IsVisible)
            {
                Vector2 mouseGraph = ScreenToGraph(_lastMouseLocal);
                //_debugDialog.UpdateDebugInfo(this, _lastMouseLocal, mouseGraph);
            }
        }

        // ========== Helpers ==========
        private void ApplyPanZoom()
        {
            // Apply to content transform
            _content.transform.position = new Vector3(_pan.x, _pan.y, 0);
            _content.transform.scale = new Vector3(_zoom, _zoom, 1);
            _gridLayer.MarkDirtyRepaint();
            _connectionLayer.MarkDirtyRepaint();
        }

        public Vector2 ScreenToGraph(Vector2 screenLocal)
        {
            // Convert from _graphRoot local space to _content local space (which is where nodes live)
            // Since _content has the pan/zoom transforms applied, we need to use WorldToLocal conversion
            Vector2 graphRootWorld = _graphRoot.LocalToWorld(screenLocal);
            Vector2 contentLocal = _content.WorldToLocal(graphRootWorld);
            return contentLocal;
        }

        public Vector2 GraphToScreen(Vector2 graphPoint)
        {
            // Convert from _content local space to _graphRoot local space
            Vector2 contentWorld = _content.LocalToWorld(graphPoint);
            Vector2 graphRootLocal = _graphRoot.WorldToLocal(contentWorld);
            return graphRootLocal;
        }

        private void CreateNodeView(Node n)
        {
            var view = new NodeView(this, n);
            _nodeViews[n.id] = view;
            _nodesLayer.Add(view);
            view.UpdateLayoutFromModel();
        }

        // Public helper methods for debug dialog access
        public bool TryGetNodeView(string id, out NodeView view) => _nodeViews.TryGetValue(id, out view);
        public Node GetSelectedNode() => string.IsNullOrEmpty(_selectedNodeId) ? null : nodes.Find(n => n.id == _selectedNodeId);
        public string GetPendingFromNodeId() => _pendingFromNodeId;
        public int GetPendingFromAnchorIndex() => _pendingFromAnchorIndex;
        public float Zoom => _zoom;
        public Vector2 Pan => _pan;
        
        public bool IsAnchorAvailable(string nodeId, int anchorIndex)
        {
            return !connections.Exists(c => 
                (c.fromNodeId == nodeId && c.fromAnchorIndex == anchorIndex) ||
                (c.toNodeId == nodeId && c.toAnchorIndex == anchorIndex));
        }

        private void MarkAllForRepaint()
        {
            _gridLayer?.MarkDirtyRepaint();
            _connectionLayer?.MarkDirtyRepaint();
            foreach (var kv in _nodeViews) kv.Value.MarkDirtyRepaint();
        }

        private void SetPendingFrom(string nodeId, int anchorIndex)
        {
            if (!string.IsNullOrEmpty(_pendingFromNodeId) && TryGetNodeView(_pendingFromNodeId, out var prev))
            {
                prev.SetPendingHighlight(false);
            }
            _pendingFromNodeId = nodeId;
            _pendingFromAnchorIndex = anchorIndex;
            if (!string.IsNullOrEmpty(_pendingFromNodeId) && TryGetNodeView(_pendingFromNodeId, out var view))
            {
                view.SetPendingHighlight(true);
            }
            _connectionLayer?.MarkDirtyRepaint();
        }

        // ========== Visual Layers ==========
        private class GridLayer : VisualElement
        {
            private readonly RuntimeGraphUI _owner;
            public GridLayer(RuntimeGraphUI owner) { _owner = owner; generateVisualContent += OnGenerate; }

            private void OnGenerate(MeshGenerationContext ctx)
            {
                var painter = ctx.painter2D;
                float width = contentRect.width;
                float height = contentRect.height;
                if (width <= 0 || height <= 0) return;

                // Small grid
                DrawGrid(painter, _owner._gridSmallColor, RuntimeGraphUI.GridSmall);
                // Large grid
                DrawGrid(painter, _owner._gridLargeColor, RuntimeGraphUI.GridLarge);
            }

            private void DrawGrid(Painter2D p, Color color, float spacing)
            {
                // Scale the grid with zoom to match node coordinate system
                float scaledSpacing = spacing * _owner._zoom;
                if (scaledSpacing < 8f) return; // Only hide when too small to see
                
                float left = contentRect.xMin, right = contentRect.xMax, top = contentRect.yMin, bottom = contentRect.yMax;
                float xOffset = Mathf.Repeat(_owner._pan.x, scaledSpacing);
                float yOffset = Mathf.Repeat(_owner._pan.y, scaledSpacing);

                p.lineWidth = 1f;
                p.strokeColor = color;

                // Vertical lines
                for (float x = left + xOffset; x < right; x += scaledSpacing)
                {
                    p.BeginPath(); p.MoveTo(new Vector2(x, top)); p.LineTo(new Vector2(x, bottom)); p.Stroke();
                }
                // Horizontal lines
                for (float y = top + yOffset; y < bottom; y += scaledSpacing)
                {
                    p.BeginPath(); p.MoveTo(new Vector2(left, y)); p.LineTo(new Vector2(right, y)); p.Stroke();
                }
            }
        }

        private class ConnectionLayer : VisualElement
        {
            private readonly RuntimeGraphUI _owner;
            public ConnectionLayer(RuntimeGraphUI owner) { _owner = owner; generateVisualContent += OnGenerate; }

            private void OnGenerate(MeshGenerationContext ctx)
            {
                var p = ctx.painter2D;
                p.lineWidth = 2f;
                p.strokeColor = _owner._connectionColor;

                // Draw existing connections
                foreach (var c in _owner.connections)
                {
                    if (!_owner.TryGetNodeView(c.fromNodeId, out var from) || !_owner.TryGetNodeView(c.toNodeId, out var to)) continue;
                    Vector2 fromWorld = from.GetAnchorWorldPosition(c.fromAnchorIndex);
                    Vector2 toWorld = to.GetAnchorWorldPosition(c.toAnchorIndex);
                    Vector2 a = this.WorldToLocal(fromWorld);
                    Vector2 b = this.WorldToLocal(toWorld);
                    DrawLine(p, a, b);
                }

                // Pending preview
                if (!string.IsNullOrEmpty(_owner._pendingFromNodeId) && _owner._pendingFromAnchorIndex >= 0 && _owner.TryGetNodeView(_owner._pendingFromNodeId, out var fromV))
                {
                    Vector2 fromWorld = fromV.GetAnchorWorldPosition(_owner._pendingFromAnchorIndex);
                    Vector2 a = this.WorldToLocal(fromWorld);
                    Vector2 b = _owner._lastMouseLocal; // already in graphRoot local
                    var oldColor = p.strokeColor;
                    var oldWidth = p.lineWidth;
                    p.strokeColor = _owner._pendingConnectionColor;
                    p.lineWidth = 3.5f; // thicker for clarity
                    DrawLine(p, a, b);
                    p.strokeColor = oldColor;
                    p.lineWidth = oldWidth;
                }
            }

            private Vector2 WorldToLocalCenter(VisualElement ve)
            {
                var world = ve.worldBound;
                var center = new Vector2(world.xMin + world.width * 0.5f, world.yMin + world.height * 0.5f);
                return this.WorldToLocal(center);
            }

            private void DrawLine(Painter2D p, Vector2 a, Vector2 b)
            {
                p.BeginPath();
                p.MoveTo(a); p.LineTo(b); p.Stroke();
            }
        }

        // ========== Node View ==========
        public class NodeView : VisualElement
        {
            private readonly RuntimeGraphUI _owner;
            public readonly Node Model;
            private Label _titleLabel;
            private VisualElement _header;
            private Foldout _metadataFoldout;
            private VisualElement _metadataContainer;
            private bool _isPendingFrom;
            private const int AnchorsPerSide = 3;

            private bool _dragging;
            private Vector2 _dragStartPanel; // pointer pos in panel space at start
            private Vector2 _dragStartGraphPos; // model.graphRect.position at start

            private bool _isSelected;
            private readonly Color _defaultBg = new Color(0.18f, 0.18f, 0.2f);
            private readonly Color _selectedBg = new Color(0.22f, 0.25f, 0.32f);

            public NodeView(RuntimeGraphUI owner, Node model)
            {
                _owner = owner; Model = model;
                name = $"Node_{model.id}";
                style.position = Position.Absolute;
                style.width = model.graphRect.width;
                style.backgroundColor = _defaultBg;
                style.borderTopLeftRadius = 4; style.borderTopRightRadius = 4; style.borderBottomLeftRadius = 4; style.borderBottomRightRadius = 4;
                style.borderBottomColor = new Color(0,0,0,0.4f); style.borderTopColor = style.borderBottomColor; style.borderLeftColor = style.borderBottomColor; style.borderRightColor = style.borderBottomColor;
                style.borderBottomWidth = 1; style.borderTopWidth = 1; style.borderLeftWidth = 1; style.borderRightWidth = 1;

                // Header
                _header = new VisualElement { name = "Header" };
                _header.style.height = 24;
                _header.style.backgroundColor = new Color(0.24f, 0.24f, 0.26f);
                _header.style.unityTextAlign = TextAnchor.MiddleLeft;
                _header.style.paddingLeft = 8; _header.style.paddingRight = 8;
                Add(_header);

                _titleLabel = new Label(model.title);
                _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                _titleLabel.style.color = Color.white;
                _header.Add(_titleLabel);

                // Connection via invisible anchors (bidirectional)
                // Use trickle-down so selection works even when clicking on child controls (e.g., TextFields)
                RegisterCallback<PointerDownEvent>(OnNodePointerDown, TrickleDown.TrickleDown);

                // Metadata
                _metadataFoldout = new Foldout { text = "Metadata", value = model.metadataFoldout };
                _metadataFoldout.RegisterValueChangedCallback(evt => { model.metadataFoldout = evt.newValue; });
                Add(_metadataFoldout);

                _metadataContainer = new VisualElement();
                _metadataContainer.style.flexDirection = FlexDirection.Column;
                _metadataFoldout.Add(_metadataContainer);
                RebuildMetadataUI();

                // Dragging on header
                _header.RegisterCallback<PointerDownEvent>(OnHeaderDown);
                _header.RegisterCallback<PointerMoveEvent>(OnHeaderMove);
                _header.RegisterCallback<PointerUpEvent>(OnHeaderUp);

                RegisterCallback<GeometryChangedEvent>(_ => { _owner._connectionLayer.MarkDirtyRepaint(); });
            }

            private void OnNodePointerDown(PointerDownEvent evt)
            {
                if (evt.button != 0) return;
                // Header is handled by header callbacks
                if (_header.worldBound.Contains(evt.position)) return;

                if (_owner._mode == InteractionMode.Select)
                {
                    // Select the node but do not stop propagation so TextFields remain interactive
                    _owner.SetSelectedNode(Model.id);
                    return;
                }
                if (_owner._mode != InteractionMode.Connect)
                {
                    // In Node mode, do nothing on node body
                    return;
                }

                // Connect mode: compute nearest available anchor index from local position
                int toAnchor = GetNearestAvailableAnchorIndexFromLocal(evt.localPosition);
                if (toAnchor == -1)
                {
                    // No available slots on this node, do nothing
                    return;
                }
                
                if (string.IsNullOrEmpty(_owner._pendingFromNodeId))
                {
                    _owner.SetPendingFrom(Model.id, toAnchor);
                }
                else
                {
                    if (_owner.TryGetNodeView(_owner._pendingFromNodeId, out var fromView))
                    {
                        int fromAnchor = _owner._pendingFromAnchorIndex >= 0 ? _owner._pendingFromAnchorIndex : 
                            fromView.GetNearestAvailableAnchorIndexFromLocal(fromView.WorldToLocal(evt.position));
                        
                        if (fromAnchor >= 0) // Only create connection if both slots are available
                        {
                            _owner.AddConnection(fromView.Model, fromAnchor, this.Model, toAnchor);
                        }
                    }
                    _owner.SetPendingFrom(null, -1);
                }
                evt.StopPropagation();
            }

            public int AnchorCount => AnchorsPerSide * 4;

            public Vector2 GetAnchorWorldPosition(int index)
            {
                // Use model dimensions multiplied by zoom to ensure slots align with node boundaries at all zoom levels
                float w = Model.graphRect.width * _owner._zoom;
                float h = Model.graphRect.height * _owner._zoom;
                int side = Mathf.FloorToInt(index / AnchorsPerSide);
                int idx = index % AnchorsPerSide;
                float t = (idx + 1f) / (AnchorsPerSide + 1f);
                Vector2 local;
                switch (side)
                {
                    case 0: // Left edge
                        local = new Vector2(0f, Mathf.Lerp(0f, h, t));
                        break;
                    case 1: // Right edge
                        local = new Vector2(w, Mathf.Lerp(0f, h, t));
                        break;
                    case 2: // Top edge
                        local = new Vector2(Mathf.Lerp(0f, w, t), 0f);
                        break;
                    default: // Bottom edge
                        local = new Vector2(Mathf.Lerp(0f, w, t), h);
                        break;
                }
                return this.LocalToWorld(local);
            }

            public int GetNearestAnchorIndexFromLocal(Vector2 local)
            {
                float best = float.MaxValue; int bestIdx = 0;
                for (int i = 0; i < AnchorCount; i++)
                {
                    Vector2 wp = GetAnchorWorldPosition(i);
                    Vector2 lp = this.WorldToLocal(wp);
                    float d = (lp - local).sqrMagnitude;
                    if (d < best) { best = d; bestIdx = i; }
                }
                return bestIdx;
            }

            public int GetNearestAvailableAnchorIndexFromLocal(Vector2 local)
            {
                float best = float.MaxValue; int bestIdx = -1;
                for (int i = 0; i < AnchorCount; i++)
                {
                    // Check if this anchor is already occupied
                    bool isOccupied = _owner.connections.Exists(c => 
                        (c.fromNodeId == Model.id && c.fromAnchorIndex == i) ||
                        (c.toNodeId == Model.id && c.toAnchorIndex == i));
                    
                    if (!isOccupied)
                    {
                        Vector2 wp = GetAnchorWorldPosition(i);
                        Vector2 lp = this.WorldToLocal(wp);
                        float d = (lp - local).sqrMagnitude;
                        if (d < best) { best = d; bestIdx = i; }
                    }
                }
                return bestIdx; // returns -1 if no available slots
            }

            public void SetPendingHighlight(bool on)
            {
                _isPendingFrom = on;
                var color = on ? new Color(1f, 0.95f, 0.3f) : new Color(0,0,0,0.4f);
                style.borderLeftColor = color;
                style.borderRightColor = color;
                style.borderTopColor = color;
                style.borderBottomColor = color;
                int w = on ? 2 : 1;
                style.borderLeftWidth = w; style.borderRightWidth = w; style.borderTopWidth = w; style.borderBottomWidth = w;
            }

            public void SetSelected(bool on)
            {
                _isSelected = on;
                style.backgroundColor = on ? _selectedBg : _defaultBg;
            }

            private void OnHeaderDown(PointerDownEvent evt)
            {
                if (evt.button != 0) return;
                if (_owner._mode != InteractionMode.Select) return; // drag only in Select mode
                _owner.SetSelectedNode(Model.id);
                _dragging = true;
                Vector2 panelPos = evt.position;
                _dragStartPanel = panelPos;
                _dragStartGraphPos = Model.graphRect.position;
                _header.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            }

            private void OnHeaderMove(PointerMoveEvent evt)
            {
                if (!_dragging) return;
                Vector2 panelPos = evt.position;
                Vector2 deltaPanel = panelPos - _dragStartPanel;
                Vector2 newGraphPos = _dragStartGraphPos + deltaPanel / Mathf.Max(0.0001f, _owner._zoom);
                Model.graphRect.position = newGraphPos;
                UpdateLayoutFromModel();
                _owner._connectionLayer.MarkDirtyRepaint();
                evt.StopPropagation();
            }

            private void OnHeaderUp(PointerUpEvent evt)
            {
                if (!_dragging) return;
                _dragging = false;
                _header.ReleasePointer(evt.pointerId);
                evt.StopPropagation();
            }

            public void UpdateLayoutFromModel()
            {
                style.left = Model.graphRect.x;
                style.top = Model.graphRect.y;
                style.width = Model.graphRect.width;
                style.height = Model.graphRect.height;
            }

            private void RebuildMetadataUI()
            {
                _metadataContainer.Clear();
                for (int i = 0; i < Model.metadata.Count; i++)
                {
                    int idx = i;
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                    var key = new TextField { value = Model.metadata[i].key, style = { width = 100 } };
                    var sep = new Label(":") { style = { width = 10, unityTextAlign = TextAnchor.MiddleCenter } };
                    var val = new TextField { value = Model.metadata[i].value, style = { flexGrow = 1 } };
                    var del = new Button(() => { Model.metadata.RemoveAt(idx); RebuildMetadataUI(); }) { text = "-" };
                    del.style.width = 24;
                    key.RegisterValueChangedCallback(e => Model.metadata[idx].key = e.newValue);
                    val.RegisterValueChangedCallback(e => Model.metadata[idx].value = e.newValue);
                    row.Add(key); row.Add(sep); row.Add(val); row.Add(del);
                    _metadataContainer.Add(row);
                }
                var addBtn = new Button(() => { Model.metadata.Add(new MetadataEntry { key = string.Empty, value = string.Empty }); RebuildMetadataUI(); }) { text = "Add Metadata" };
                _metadataContainer.Add(addBtn);
            }
        }
    }
}
