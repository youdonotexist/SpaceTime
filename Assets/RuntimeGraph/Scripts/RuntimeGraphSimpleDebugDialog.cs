using UnityEngine;
using UnityEngine.UIElements;

namespace RuntimeGraph
{
    /// <summary>
    /// Simple debug dialog that displays coordinate information directly from RuntimeGraphUI
    /// </summary>
    public class RuntimeGraphSimpleDebugDialog
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
        
        private bool _isVisible = false;
        
        public bool IsVisible => _isVisible;
        
        public RuntimeGraphSimpleDebugDialog(VisualElement parent)
        {
            _parent = parent;
            BuildDebugDialog();
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
        /// Updates debug information from RuntimeGraphUI instance
        /// </summary>
        public void UpdateDebugInfo(RuntimeGraphUI_Refactored graphUI, Vector2 mouseScreenPos, Vector2 mouseGraphPos)
        {
            if (!_isVisible || graphUI == null) return;
            
            // Mouse positions
            _mouseScreenLabel.text = $"Mouse Screen: {mouseScreenPos:F2}";
            _mouseGraphLabel.text = $"Mouse Graph: {mouseGraphPos:F2}";
            
            // Viewport info
            _zoomLabel.text = $"Zoom: {graphUI.Zoom:F3}";
            _panLabel.text = $"Pan: {graphUI.Pan:F2}";
            
            // Selected node info
            var selectedNode = graphUI.NodeManager.GetSelectedNode();
            if (selectedNode != null)
            {
                _selectedNodeLabel.text = $"Selected Node: {selectedNode.title} at {selectedNode.graphRect.position:F2} size {selectedNode.graphRect.size:F2}";
                
                // Show slot information for selected node
                UpdateNodeSlotInfo(graphUI, selectedNode, mouseScreenPos);
            }
            else
            {
                _selectedNodeLabel.text = "Selected Node: None";
                _nodeSlotInfoLabel.text = "Node Slots: N/A";
            }
            
            // Connection info
            var pendingFrom = graphUI.ConnectionManager.PendingFromNodeId;
            var pendingAnchor = graphUI.ConnectionManager.PendingFromAnchorIndex;
            if (!string.IsNullOrEmpty(pendingFrom))
            {
                _connectionInfoLabel.text = $"Pending Connection: {pendingFrom} anchor {pendingAnchor}";
            }
            else
            {
                _connectionInfoLabel.text = "Pending Connection: None";
            }
            
            // Additional debug info
            UpdateAdditionalDebugInfo(graphUI);
        }
        
        private void UpdateNodeSlotInfo(RuntimeGraphUI_Refactored graphUI, RuntimeGraphNodeManager.Node selectedNode, Vector2 mouseScreenPos)
        {
            if (selectedNode == null) return;
            
            if (graphUI.NodeManager.TryGetNodeView(selectedNode.id, out var nodeView))
            {
                // Find nearest slot to mouse
                int nearestSlot = nodeView.GetNearestAnchorIndexFromLocal(nodeView.WorldToLocal(_parent.LocalToWorld(mouseScreenPos)));
                Vector2 nearestSlotWorld = nodeView.GetAnchorWorldPosition(nearestSlot);
                Vector2 nearestSlotScreen = _parent.WorldToLocal(nearestSlotWorld);
                
                // Check availability
                bool isAvailable = graphUI.ConnectionManager.IsAnchorAvailable(selectedNode.id, nearestSlot);
                
                _nodeSlotInfoLabel.text = $"Nearest Slot: {nearestSlot} at world {nearestSlotWorld:F2} screen {nearestSlotScreen:F2} available: {isAvailable}";
            }
            else
            {
                _nodeSlotInfoLabel.text = "Node Slots: NodeView not found";
            }
        }
        
        private void UpdateAdditionalDebugInfo(RuntimeGraphUI_Refactored graphUI)
        {
            var debugInfo = "";
            
            // Add coordinate transformation test
            var testGraphPos = new Vector2(100, 100);
            var testScreenPos = graphUI.GraphToScreen(testGraphPos);
            var testBackToGraph = graphUI.ScreenToGraph(testScreenPos);
            debugInfo += $"Coord Test: Graph(100,100) -> Screen({testScreenPos:F2}) -> Graph({testBackToGraph:F2})\n";
            
            // Add connection count
            debugInfo += $"Connections: {graphUI.connections.Count}\n";
            
            // Add node count
            debugInfo += $"Nodes: {graphUI.nodes.Count}";
            
            _debugInfoLabel.text = debugInfo;
        }
        
        private void BuildDebugDialog()
        {
            _debugPanel = new VisualElement();
            _debugPanel.style.position = Position.Absolute;
            _debugPanel.style.right = 10;
            _debugPanel.style.top = 50;
            _debugPanel.style.width = 350;
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
            var title = new Label("RuntimeGraph Debug Info (Press D to toggle)");
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