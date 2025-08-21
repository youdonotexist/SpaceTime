using UnityEngine;
using UnityEngine.UIElements;

namespace RuntimeGraph.Components
{
    /// <summary>
    /// Debug dialog that displays coordinate information and other debugging data for the RuntimeGraph system
    /// </summary>
    public class RuntimeGraphDebugDialog
    {
        private readonly VisualElement _parent;
        private VisualElement _debugPanel;
        private Label _mouseScreenLabel;
        private Label _mouseGraphLabel;
        private Label _zoomLabel;
        private Label _panLabel;
        private Label _selectedNodeLabel;
        private Label _nodeSlotInfoLabel;
        private Label _connectionInfoLabel;
        private Label _debugInfoLabel;
        
        private RuntimeGraphViewport _viewport;
        private RuntimeGraphNodeManager _nodeManager;
        private RuntimeGraphConnectionManager _connectionManager;
        
        private bool _isVisible = false;
        
        public bool IsVisible => _isVisible;
        
        public RuntimeGraphDebugDialog(VisualElement parent)
        {
            _parent = parent;
            BuildDebugDialog();
        }
        
        public void SetDependencies(RuntimeGraphViewport viewport, RuntimeGraphNodeManager nodeManager, RuntimeGraphConnectionManager connectionManager)
        {
            _viewport = viewport;
            _nodeManager = nodeManager;
            _connectionManager = connectionManager;
        }
        
        /// <summary>
        /// Toggles the visibility of the debug dialog
        /// </summary>
        public void ToggleVisibility()
        {
            _isVisible = !_isVisible;
            _debugPanel.style.display = _isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        /// <summary>
        /// Shows the debug dialog
        /// </summary>
        public void Show()
        {
            _isVisible = true;
            _debugPanel.style.display = DisplayStyle.Flex;
        }
        
        /// <summary>
        /// Hides the debug dialog
        /// </summary>
        public void Hide()
        {
            _isVisible = false;
            _debugPanel.style.display = DisplayStyle.None;
        }
        
        /// <summary>
        /// Updates all debug information based on current mouse position and system state
        /// </summary>
        public void UpdateDebugInfo(Vector2 mouseScreenPos, Vector2 mouseGraphPos)
        {
            if (!_isVisible) return;
            
            // Mouse positions
            _mouseScreenLabel.text = $"Mouse Screen: {mouseScreenPos:F2}";
            _mouseGraphLabel.text = $"Mouse Graph: {mouseGraphPos:F2}";
            
            // Viewport info
            if (_viewport != null)
            {
                _zoomLabel.text = $"Zoom: {_viewport.Zoom:F3}";
                _panLabel.text = $"Pan: {_viewport.Pan:F2}";
            }
            
            // Selected node info
            if (_nodeManager != null)
            {
                var selectedNode = _nodeManager.GetSelectedNode();
                if (selectedNode != null)
                {
                    _selectedNodeLabel.text = $"Selected Node: {selectedNode.title} at {selectedNode.graphRect.position:F2} size {selectedNode.graphRect.size:F2}";
                    
                    // Show slot information for selected node
                    if (_nodeManager.TryGetNodeView(selectedNode.id, out var nodeView))
                    {
                        UpdateNodeSlotInfo(nodeView, mouseScreenPos);
                    }
                }
                else
                {
                    _selectedNodeLabel.text = "Selected Node: None";
                    _nodeSlotInfoLabel.text = "Node Slots: N/A";
                }
            }
            
            // Connection info
            if (_connectionManager != null)
            {
                var pendingFrom = _connectionManager.PendingFromNodeId;
                var pendingAnchor = _connectionManager.PendingFromAnchorIndex;
                if (!string.IsNullOrEmpty(pendingFrom))
                {
                    _connectionInfoLabel.text = $"Pending Connection: {pendingFrom} anchor {pendingAnchor}";
                }
                else
                {
                    _connectionInfoLabel.text = "Pending Connection: None";
                }
            }
            
            // Additional debug info
            UpdateAdditionalDebugInfo();
        }
        
        private void UpdateNodeSlotInfo(Components.NodeView nodeView, Vector2 mouseScreenPos)
        {
            if (nodeView == null) return;
            
            // Find nearest slot to mouse
            int nearestSlot = nodeView.GetNearestAnchorIndexFromLocal(nodeView.WorldToLocal(_parent.LocalToWorld(mouseScreenPos)));
            Vector2 nearestSlotWorld = nodeView.GetAnchorWorldPosition(nearestSlot);
            Vector2 nearestSlotScreen = _parent.WorldToLocal(nearestSlotWorld);
            
            // Check availability
            bool isAvailable = _connectionManager?.IsAnchorAvailable(nodeView.Model.id, nearestSlot) ?? true;
            
            _nodeSlotInfoLabel.text = $"Nearest Slot: {nearestSlot} at world {nearestSlotWorld:F2} screen {nearestSlotScreen:F2} available: {isAvailable}";
        }
        
        private void UpdateAdditionalDebugInfo()
        {
            var debugInfo = "";
            
            // Add coordinate transformation test
            if (_viewport != null)
            {
                var testGraphPos = new Vector2(100, 100);
                var testScreenPos = _viewport.GraphToScreen(testGraphPos);
                var testBackToGraph = _viewport.ScreenToGraph(testScreenPos);
                debugInfo += $"Coord Test: Graph(100,100) -> Screen({testScreenPos:F2}) -> Graph({testBackToGraph:F2})\n";
            }
            
            // Add connection count
            if (_connectionManager != null)
            {
                debugInfo += $"Connections: {_connectionManager.Connections.Count}\n";
            }
            
            // Add node count
            if (_nodeManager != null)
            {
                debugInfo += $"Nodes: {_nodeManager.Nodes.Count}";
            }
            
            _debugInfoLabel.text = debugInfo;
        }
        
        private void BuildDebugDialog()
        {
            _debugPanel = new VisualElement();
            _debugPanel.style.position = Position.Absolute;
            _debugPanel.style.right = 10;
            _debugPanel.style.top = 50;
            _debugPanel.style.width = 300;
            _debugPanel.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            _debugPanel.style.borderTopWidth = 1;
            _debugPanel.style.borderBottomWidth = 1;
            _debugPanel.style.borderLeftWidth = 1;
            _debugPanel.style.borderRightWidth = 1;
            _debugPanel.style.borderTopColor = Color.gray;
            _debugPanel.style.borderBottomColor = Color.gray;
            _debugPanel.style.borderLeftColor = Color.gray;
            _debugPanel.style.borderRightColor = Color.gray;
            _debugPanel.style.borderTopLeftRadius = 4;
            _debugPanel.style.borderTopRightRadius = 4;
            _debugPanel.style.borderBottomLeftRadius = 4;
            _debugPanel.style.borderBottomRightRadius = 4;
            _debugPanel.style.paddingTop = 8;
            _debugPanel.style.paddingBottom = 8;
            _debugPanel.style.paddingLeft = 8;
            _debugPanel.style.paddingRight = 8;
            _debugPanel.style.display = DisplayStyle.None; // Initially hidden
            _parent.Add(_debugPanel);
            
            // Title
            var title = new Label("Debug Info (Press D to toggle)");
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.marginBottom = 5;
            _debugPanel.Add(title);
            
            // Mouse positions
            _mouseScreenLabel = new Label("Mouse Screen: N/A");
            _mouseScreenLabel.style.fontSize = 10;
            _mouseScreenLabel.style.color = Color.white;
            _debugPanel.Add(_mouseScreenLabel);
            
            _mouseGraphLabel = new Label("Mouse Graph: N/A");
            _mouseGraphLabel.style.fontSize = 10;
            _mouseGraphLabel.style.color = Color.white;
            _debugPanel.Add(_mouseGraphLabel);
            
            // Viewport info
            _zoomLabel = new Label("Zoom: N/A");
            _zoomLabel.style.fontSize = 10;
            _zoomLabel.style.color = Color.white;
            _debugPanel.Add(_zoomLabel);
            
            _panLabel = new Label("Pan: N/A");
            _panLabel.style.fontSize = 10;
            _panLabel.style.color = Color.white;
            _debugPanel.Add(_panLabel);
            
            // Selected node info
            _selectedNodeLabel = new Label("Selected Node: N/A");
            _selectedNodeLabel.style.fontSize = 10;
            _selectedNodeLabel.style.color = Color.white;
            _selectedNodeLabel.style.whiteSpace = WhiteSpace.Normal; // Allow text wrapping
            _debugPanel.Add(_selectedNodeLabel);
            
            // Node slot info
            _nodeSlotInfoLabel = new Label("Node Slots: N/A");
            _nodeSlotInfoLabel.style.fontSize = 10;
            _nodeSlotInfoLabel.style.color = Color.white;
            _nodeSlotInfoLabel.style.whiteSpace = WhiteSpace.Normal;
            _debugPanel.Add(_nodeSlotInfoLabel);
            
            // Connection info
            _connectionInfoLabel = new Label("Pending Connection: N/A");
            _connectionInfoLabel.style.fontSize = 10;
            _connectionInfoLabel.style.color = Color.white;
            _connectionInfoLabel.style.whiteSpace = WhiteSpace.Normal;
            _debugPanel.Add(_connectionInfoLabel);
            
            // Additional debug info
            _debugInfoLabel = new Label("Debug Info: N/A");
            _debugInfoLabel.style.fontSize = 10;
            _debugInfoLabel.style.color = Color.white;
            _debugInfoLabel.style.whiteSpace = WhiteSpace.Normal;
            _debugInfoLabel.style.marginTop = 5;
            _debugPanel.Add(_debugInfoLabel);
        }
        
        /// <summary>
        /// Removes the debug dialog from its parent
        /// </summary>
        public void Destroy()
        {
            _parent?.Remove(_debugPanel);
        }
    }
}