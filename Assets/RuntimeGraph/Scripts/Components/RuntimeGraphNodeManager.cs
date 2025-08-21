using System;
using System.Collections.Generic;
using System.Linq;
using RuntimeGraph.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuntimeGraph
{
    /// <summary>
    /// Manages node creation, deletion, selection, and NodeView instances
    /// </summary>
    public class RuntimeGraphNodeManager
    {
        private readonly VisualElement _graphRoot;
        private readonly List<Node> _nodes;
        private readonly RuntimeGraphConnectionManager _connectionManager;
        private readonly Dictionary<string, NodeView> _nodeViews = new Dictionary<string, NodeView>();
        private RuntimeGraphToolbar _toolbar;
        private RuntimeGraphViewport _viewport;
        
        private string _selectedNodeId;
        private const float NodeSize = 120f;
        
        public event Action<string> NodeSelected;
        public event Action<Node> NodeCreated;
        public event Action<Node> NodeRemoved;
        
        public IReadOnlyList<Node> Nodes => _nodes;
        public string SelectedNodeId => _selectedNodeId;
        
        public RuntimeGraphNodeManager(VisualElement graphRoot, List<Node> nodes, RuntimeGraphConnectionManager connectionManager = null)
        {
            _graphRoot = graphRoot;
            _nodes = nodes;
            _connectionManager = connectionManager;
            EnsureUniqueNodeIds();
        }
        
        public void SetToolbar(RuntimeGraphToolbar toolbar)
        {
            _toolbar = toolbar;
        }
        
        public void SetViewport(RuntimeGraphViewport viewport)
        {
            _viewport = viewport;
        }
        
        /// <summary>
        /// Migration: ensure all existing nodes have unique IDs (older data may have empty ids)
        /// </summary>
        private void EnsureUniqueNodeIds()
        {
            var usedIds = new HashSet<string>();
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (string.IsNullOrEmpty(_nodes[i].id) || usedIds.Contains(_nodes[i].id))
                {
                    _nodes[i].id = Guid.NewGuid().ToString("N");
                }
                usedIds.Add(_nodes[i].id);
            }
        }
        
        /// <summary>
        /// Creates NodeView instances for all nodes and adds them to the graph
        /// </summary>
        public void CreateAllNodeViews()
        {
            foreach (var node in _nodes)
            {
                CreateNodeView(node);
            }
        }
        
        /// <summary>
        /// Creates a new node at the specified graph position
        /// </summary>
        public Node AddNode(Vector2 graphPos)
        {
            var node = new Node
            {
                id = Guid.NewGuid().ToString("N"),
                title = "New Node",
                graphRect = new Rect(graphPos.x, graphPos.y, NodeSize, NodeSize),
                metadata = new List<MetadataEntry>(),
                metadataFoldout = true
            };
            _nodes.Add(node);
            CreateNodeView(node);
            NodeCreated?.Invoke(node);
            return node;
        }
        
        /// <summary>
        /// Removes a node and its associated view
        /// </summary>
        public void RemoveNode(Node node)
        {
            if (_nodeViews.TryGetValue(node.id, out var view))
            {
                _graphRoot.Remove(view);
                _nodeViews.Remove(node.id);
            }
            _nodes.Remove(node);
            
            if (_selectedNodeId == node.id)
            {
                SetSelectedNode(null);
            }
            
            NodeRemoved?.Invoke(node);
        }
        
        /// <summary>
        /// Sets the selected node by ID
        /// </summary>
        public void SetSelectedNode(string nodeId)
        {
            // Deselect previous
            if (!string.IsNullOrEmpty(_selectedNodeId) && _nodeViews.TryGetValue(_selectedNodeId, out var prevView))
            {
                prevView.SetSelected(false);
            }
            
            _selectedNodeId = nodeId;
            
            // Select new
            if (!string.IsNullOrEmpty(_selectedNodeId) && _nodeViews.TryGetValue(_selectedNodeId, out var newView))
            {
                newView.SetSelected(true);
            }
            
            NodeSelected?.Invoke(_selectedNodeId);
        }
        
        /// <summary>
        /// Gets the selected node, if any
        /// </summary>
        public Node GetSelectedNode()
        {
            if (string.IsNullOrEmpty(_selectedNodeId))
                return null;
                
            return _nodes.FirstOrDefault(n => n.id == _selectedNodeId);
        }
        
        /// <summary>
        /// Tries to get a NodeView by node ID
        /// </summary>
        public bool TryGetNodeView(string nodeId, out NodeView view)
        {
            return _nodeViews.TryGetValue(nodeId, out view);
        }
        
        /// <summary>
        /// Gets a node by ID
        /// </summary>
        public Node GetNode(string nodeId)
        {
            return _nodes.FirstOrDefault(n => n.id == nodeId);
        }
        
        /// <summary>
        /// Marks all node views for visual repaint
        /// </summary>
        public void MarkAllForRepaint()
        {
            foreach (var view in _nodeViews.Values)
            {
                view.MarkDirtyRepaint();
            }
        }
        
        /// <summary>
        /// Creates a NodeView for the given node and adds it to the graph
        /// </summary>
        private void CreateNodeView(Node node)
        {
            var view = new NodeView(this, _connectionManager, node, _toolbar, _viewport);
            _nodeViews[node.id] = view;
            _graphRoot.Add(view);
        }
        
        /// <summary>
        /// Called by NodeView when a node is selected via UI interaction
        /// </summary>
        internal void OnNodeSelected(string nodeId)
        {
            SetSelectedNode(nodeId);
        }
        
        /// <summary>
        /// Data model for graph nodes
        /// </summary>
        [System.Serializable]
        public class Node
        {
            public string id = "";
            public string title = "Node";
            public Rect graphRect = new Rect(0, 0, 180, 180);
            public List<MetadataEntry> metadata = new List<MetadataEntry>();
            public bool metadataFoldout = true;
        }
        
        /// <summary>
        /// Key-value metadata entry for nodes
        /// </summary>
        [System.Serializable]
        public class MetadataEntry
        {
            public string key = "";
            public string value = "";
        }
    }
}