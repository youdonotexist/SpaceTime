using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace RuntimeGraph
{
    /// <summary>
    /// Manages connections between nodes, including creation, validation, and visual rendering
    /// </summary>
    public class RuntimeGraphConnectionManager
    {
        private readonly VisualElement _graphRoot;
        private RuntimeGraphNodeManager _nodeManager;
        private readonly List<Connection> _connections;
        private readonly ConnectionLayer _connectionLayer;
        
        // Pending connection state
        private string _pendingFromNodeId;
        private int _pendingFromAnchorIndex = -1;
        
        public event Action<Connection> ConnectionCreated;
        public event Action<Connection> ConnectionRemoved;
        
        public IReadOnlyList<Connection> Connections => _connections;
        public string PendingFromNodeId => _pendingFromNodeId;
        public int PendingFromAnchorIndex => _pendingFromAnchorIndex;
        
        public RuntimeGraphConnectionManager(VisualElement graphRoot, List<Connection> connections)
        {
            _graphRoot = graphRoot;
            _connections = connections;
            
            _connectionLayer = new ConnectionLayer(this);
            _graphRoot.Insert(0, _connectionLayer); // Behind nodes
        }
        
        public void SetNodeManager(RuntimeGraphNodeManager nodeManager)
        {
            _nodeManager = nodeManager;
        }
        
        /// <summary>
        /// Creates a connection between two nodes at specific anchor points
        /// </summary>
        public Connection AddConnection(RuntimeGraphNodeManager.Node fromNode, int fromAnchor, RuntimeGraphNodeManager.Node toNode, int toAnchor)
        {
            // Validate that anchors are available
            if (!IsAnchorAvailable(fromNode.id, fromAnchor) || !IsAnchorAvailable(toNode.id, toAnchor))
            {
                return null; // Cannot create connection on occupied slot
            }
            
            var connection = new Connection
            {
                fromNodeId = fromNode.id,
                fromAnchorIndex = fromAnchor,
                toNodeId = toNode.id,
                toAnchorIndex = toAnchor
            };
            
            _connections.Add(connection);
            _connectionLayer.MarkDirtyRepaint();
            ConnectionCreated?.Invoke(connection);
            return connection;
        }
        
        /// <summary>
        /// Removes a connection
        /// </summary>
        public void RemoveConnection(Connection connection)
        {
            if (_connections.Remove(connection))
            {
                _connectionLayer.MarkDirtyRepaint();
                ConnectionRemoved?.Invoke(connection);
            }
        }
        
        /// <summary>
        /// Removes all connections involving a specific node
        /// </summary>
        public void RemoveConnectionsForNode(string nodeId)
        {
            var toRemove = _connections.Where(c => c.fromNodeId == nodeId || c.toNodeId == nodeId).ToList();
            foreach (var connection in toRemove)
            {
                RemoveConnection(connection);
            }
        }
        
        /// <summary>
        /// Checks if a specific anchor on a node is available (not already connected)
        /// </summary>
        public bool IsAnchorAvailable(string nodeId, int anchorIndex)
        {
            return !_connections.Exists(c => 
                (c.fromNodeId == nodeId && c.fromAnchorIndex == anchorIndex) ||
                (c.toNodeId == nodeId && c.toAnchorIndex == anchorIndex));
        }
        
        /// <summary>
        /// Sets pending connection state (for interactive connection creation)
        /// </summary>
        public void SetPendingFrom(string nodeId, int anchorIndex)
        {
            // Clear previous highlighting
            if (!string.IsNullOrEmpty(_pendingFromNodeId) && _nodeManager.TryGetNodeView(_pendingFromNodeId, out var prevView))
            {
                prevView.SetPendingHighlight(false);
            }
            
            _pendingFromNodeId = nodeId;
            _pendingFromAnchorIndex = anchorIndex;
            
            // Apply new highlighting
            if (!string.IsNullOrEmpty(_pendingFromNodeId) && _nodeManager.TryGetNodeView(_pendingFromNodeId, out var newView))
            {
                newView.SetPendingHighlight(true);
            }
            
            _connectionLayer.MarkDirtyRepaint();
        }
        
        private Vector2 _previewEndpoint = Vector2.zero;
        
        /// <summary>
        /// Sets the world position of a connection preview endpoint (mouse position)
        /// </summary>
        public void SetPreviewEndpoint(Vector2 worldPos)
        {
            _previewEndpoint = worldPos;
            _connectionLayer.MarkDirtyRepaint();
        }
        
        /// <summary>
        /// Gets the world position of a connection preview endpoint (mouse position)
        /// </summary>
        public Vector2 GetPreviewEndpoint()
        {
            return _previewEndpoint;
        }
        
        /// <summary>
        /// Marks the connection layer for repaint
        /// </summary>
        public void MarkDirtyRepaint()
        {
            _connectionLayer.MarkDirtyRepaint();
        }
        
        /// <summary>
        /// Data model for connections between nodes
        /// </summary>
        [System.Serializable]
        public class Connection
        {
            public string fromNodeId = "";
            public int fromAnchorIndex;
            public string toNodeId = "";
            public int toAnchorIndex;
        }
        
        /// <summary>
        /// Visual layer that renders all connections
        /// </summary>
        private class ConnectionLayer : VisualElement
        {
            private readonly RuntimeGraphConnectionManager _owner;
            
            public ConnectionLayer(RuntimeGraphConnectionManager owner)
            {
                _owner = owner;
                generateVisualContent += OnGenerate;
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.left = 0; style.top = 0; style.right = 0; style.bottom = 0;
            }
            
            private void OnGenerate(MeshGenerationContext ctx)
            {
                var p = ctx.painter2D;
                
                // Use colors matching original RuntimeGraphUI
                var connectionColor = new Color(0.3f, 0.8f, 1f, 0.9f);
                var pendingConnectionColor = new Color(1f, 0.8f, 0.2f, 0.9f);
                
                // Draw existing connections
                p.strokeColor = connectionColor;
                p.lineWidth = 2.0f;
                foreach (var conn in _owner._connections)
                {
                    if (_owner._nodeManager.TryGetNodeView(conn.fromNodeId, out var fromView) &&
                        _owner._nodeManager.TryGetNodeView(conn.toNodeId, out var toView))
                    {
                        Vector2 fromPos = WorldToLocalCenter(fromView.GetAnchorWorldPosition(conn.fromAnchorIndex));
                        Vector2 toPos = WorldToLocalCenter(toView.GetAnchorWorldPosition(conn.toAnchorIndex));
                        DrawLine(p, fromPos, toPos);
                    }
                }
                
                // Draw pending connection preview
                if (!string.IsNullOrEmpty(_owner._pendingFromNodeId) && 
                    _owner._nodeManager.TryGetNodeView(_owner._pendingFromNodeId, out var pendingView))
                {
                    Vector2 fromPos = WorldToLocalCenter(pendingView.GetAnchorWorldPosition(_owner._pendingFromAnchorIndex));
                    Vector2 toPos = WorldToLocalCenter(_owner.GetPreviewEndpoint());
                    p.strokeColor = pendingConnectionColor;
                    p.lineWidth = 3.5f; // Thicker line for pending connection, matching original
                    DrawLine(p, fromPos, toPos);
                }
            }
            
            private Vector2 WorldToLocalCenter(Vector2 worldPos)
            {
                return this.WorldToLocal(worldPos);
            }
            
            private void DrawLine(Painter2D p, Vector2 a, Vector2 b)
            {
                p.BeginPath();
                p.MoveTo(a);
                p.LineTo(b);
                p.Stroke();
            }
        }
    }
}