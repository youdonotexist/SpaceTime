using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuntimeGraph.Components
{
    /// <summary>
    /// Visual representation of a graph node with interactive capabilities
    /// </summary>
    public class NodeView : VisualElement
    {
        private readonly RuntimeGraphNodeManager _nodeManager;
        private readonly RuntimeGraphConnectionManager _connectionManager;
        private readonly RuntimeGraphNodeManager.Node _model;
        private readonly RuntimeGraphToolbar _toolbar;
        private readonly RuntimeGraphViewport _viewport;
        
        private VisualElement _header;
        private TextField _titleLabel;
        private Foldout _metadataFoldout;
        private VisualElement _metadataContainer;
        private bool _isPendingFrom;
        private const int AnchorsPerSide = 3;

        private bool _dragging;
        private Vector2 _dragStartPanel; // pointer pos in panel space at start
        private Vector2 _dragStartGraphPos; // node's graph pos at start

        private bool _isSelected;
        private Color _selectedBg = new Color(0.3f, 0.5f, 0.8f, 0.3f);
        private Color _defaultBg = new Color(0.2f, 0.2f, 0.25f, 0.8f);

        public RuntimeGraphNodeManager.Node Model => _model;
        
        public NodeView(RuntimeGraphNodeManager nodeManager, RuntimeGraphConnectionManager connectionManager, RuntimeGraphNodeManager.Node model, RuntimeGraphToolbar toolbar = null, RuntimeGraphViewport viewport = null)
        {
            _nodeManager = nodeManager;
            _connectionManager = connectionManager;
            _model = model;
            _toolbar = toolbar;
            _viewport = viewport;
            
            BuildNodeUI();
            UpdateLayoutFromModel();
        }

        private void BuildNodeUI()
        {
            style.position = Position.Absolute;
            style.backgroundColor = _defaultBg;
            style.borderTopWidth = 1; style.borderBottomWidth = 1; style.borderLeftWidth = 1; style.borderRightWidth = 1;
            style.borderTopColor = new Color(0, 0, 0, 0.4f);
            style.borderBottomColor = new Color(0, 0, 0, 0.4f);
            style.borderLeftColor = new Color(0, 0, 0, 0.4f);
            style.borderRightColor = new Color(0, 0, 0, 0.4f);
            style.borderTopLeftRadius = 4; style.borderTopRightRadius = 4; style.borderBottomLeftRadius = 4; style.borderBottomRightRadius = 4;

            // Header with dragging
            _header = new VisualElement();
            _header.style.height = 24;
            _header.style.backgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.8f);
            _header.style.flexDirection = FlexDirection.Row;
            _header.style.alignItems = Align.Center;
            _header.style.paddingLeft = 6; _header.style.paddingRight = 6;
            _header.RegisterCallback<PointerDownEvent>(OnHeaderDown);
            _header.RegisterCallback<PointerMoveEvent>(OnHeaderMove);
            _header.RegisterCallback<PointerUpEvent>(OnHeaderUp);
            Add(_header);

            _titleLabel = new TextField { value = _model.title };
            _titleLabel.style.flexGrow = 1;
            _titleLabel.style.fontSize = 12;
            _titleLabel.style.marginTop = 0; _titleLabel.style.marginBottom = 0; _titleLabel.style.marginLeft = 0; _titleLabel.style.marginRight = 0;
            _titleLabel.RegisterValueChangedCallback(evt => _model.title = evt.newValue);
            _header.Add(_titleLabel);

            // Connection via invisible anchors (bidirectional)
            // Use trickle-down so selection works even when clicking on child controls (e.g., TextFields)
            RegisterCallback<PointerDownEvent>(OnNodePointerDown, TrickleDown.TrickleDown);

            // Metadata
            _metadataFoldout = new Foldout { text = "Metadata", value = _model.metadataFoldout };
            _metadataFoldout.style.fontSize = 11;
            _metadataFoldout.RegisterValueChangedCallback(evt => _model.metadataFoldout = evt.newValue);
            Add(_metadataFoldout);

            _metadataContainer = new VisualElement();
            _metadataFoldout.Add(_metadataContainer);
            RebuildMetadataUI();
        }

        private void OnNodePointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            var mode = _toolbar?.Mode ?? RuntimeGraphToolbar.InteractionMode.Select;

            if (mode == RuntimeGraphToolbar.InteractionMode.Select)
            {
                // Select the node but do not stop propagation so TextFields remain interactive
                _nodeManager.OnNodeSelected(_model.id);
                return;
            }
            if (mode != RuntimeGraphToolbar.InteractionMode.Connect)
                return;

            // Connect mode: compute nearest available anchor index from local position
            int toAnchor = GetNearestAvailableAnchorIndexFromLocal(evt.localPosition);
            if (toAnchor == -1)
            {
                // No available slots on this node, do nothing
                return;
            }
            
            if (string.IsNullOrEmpty(_connectionManager.PendingFromNodeId))
            {
                _connectionManager.SetPendingFrom(_model.id, toAnchor);
            }
            else
            {
                if (_nodeManager.TryGetNodeView(_connectionManager.PendingFromNodeId, out var fromView))
                {
                    int fromAnchor = _connectionManager.PendingFromAnchorIndex >= 0 ? _connectionManager.PendingFromAnchorIndex : 
                        fromView.GetNearestAvailableAnchorIndexFromLocal(fromView.WorldToLocal(evt.position));
                    
                    if (fromAnchor >= 0) // Only create connection if both slots are available
                    {
                        var fromNode = _nodeManager.GetNode(_connectionManager.PendingFromNodeId);
                        _connectionManager.AddConnection(fromNode, fromAnchor, _model, toAnchor);
                    }
                }
                _connectionManager.SetPendingFrom(null, -1);
            }
            evt.StopPropagation();
        }

        public int AnchorCount => AnchorsPerSide * 4;

        public Vector2 GetAnchorWorldPosition(int index)
        {
            // Use model dimensions multiplied by zoom to ensure slots align with node boundaries at all zoom levels
            float zoom = _viewport?.Zoom ?? 1f;
            float w = _model.graphRect.width * zoom;
            float h = _model.graphRect.height * zoom;
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
                bool isOccupied = !_connectionManager.IsAnchorAvailable(_model.id, i);
                
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
            
            var mode = _toolbar?.Mode ?? RuntimeGraphToolbar.InteractionMode.Select;
            
            if (mode != RuntimeGraphToolbar.InteractionMode.Select) return; // drag only in Select mode
            
            _nodeManager.SetSelectedNode(_model.id);
            _dragging = true;
            Vector2 panelPos = evt.position;
            _dragStartPanel = panelPos;
            _dragStartGraphPos = _model.graphRect.position;
            _header.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnHeaderMove(PointerMoveEvent evt)
        {
            if (!_dragging) return;
            
            // Use injected viewport for coordinate conversion
            if (_viewport == null) return;
            
            Vector2 panelPos = evt.position;
            Vector2 deltaPanel = panelPos - _dragStartPanel;
            Vector2 newGraphPos = _dragStartGraphPos + deltaPanel / Mathf.Max(0.0001f, _viewport.Zoom);
            _model.graphRect.position = newGraphPos;
            UpdateLayoutFromModel();
            
            // Mark connections for repaint
            _connectionManager.MarkDirtyRepaint();
            
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
            style.left = _model.graphRect.x;
            style.top = _model.graphRect.y;
            style.width = _model.graphRect.width;
            style.height = _model.graphRect.height;
        }

        private void RebuildMetadataUI()
        {
            _metadataContainer.Clear();
            foreach (var entry in _model.metadata)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.marginBottom = 2;

                var keyField = new TextField { value = entry.key };
                keyField.style.flexGrow = 1;
                keyField.style.fontSize = 10;
                keyField.RegisterValueChangedCallback(evt => entry.key = evt.newValue);

                var valueField = new TextField { value = entry.value };
                valueField.style.flexGrow = 1;
                valueField.style.fontSize = 10;
                valueField.RegisterValueChangedCallback(evt => entry.value = evt.newValue);

                row.Add(keyField);
                row.Add(valueField);
                _metadataContainer.Add(row);
            }

            var addBtn = new Button(() => 
            {
                _model.metadata.Add(new RuntimeGraphNodeManager.MetadataEntry());
                RebuildMetadataUI();
            }) { text = "+" };
            addBtn.style.fontSize = 10;
            _metadataContainer.Add(addBtn);
        }
    }
}