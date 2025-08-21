using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuntimeGraph.Components
{
    /// <summary>
    /// Manages the toolbar UI for switching between interaction modes
    /// </summary>
    public class RuntimeGraphToolbar
    {
        private readonly VisualElement _parent;
        private VisualElement _toolbar;
        private Button _btnSelect;
        private Button _btnNode;
        private Button _btnConnect;
        
        private InteractionMode _mode = InteractionMode.Select;
        
        public event Action<InteractionMode> ModeChanged;
        public InteractionMode Mode => _mode;
        
        public RuntimeGraphToolbar(VisualElement parent)
        {
            _parent = parent;
            BuildToolbar();
        }
        
        /// <summary>
        /// Sets the current interaction mode and updates toolbar visuals
        /// </summary>
        public void SetMode(InteractionMode mode)
        {
            _mode = mode;
            UpdateToolbarVisuals();
            ModeChanged?.Invoke(_mode);
        }
        
        /// <summary>
        /// Builds the toolbar UI with mode selection buttons
        /// </summary>
        private void BuildToolbar()
        {
            _toolbar = new VisualElement();
            _toolbar.style.position = Position.Absolute;
            _toolbar.style.left = 10; _toolbar.style.top = 10;
            _toolbar.style.flexDirection = FlexDirection.Row;
            _toolbar.style.backgroundColor = new Color(0.15f, 0.15f, 0.17f, 0.9f);
            _toolbar.style.paddingLeft = 6; _toolbar.style.paddingRight = 6; 
            _toolbar.style.paddingTop = 4; _toolbar.style.paddingBottom = 4;
            _toolbar.style.borderBottomLeftRadius = 4; _toolbar.style.borderBottomRightRadius = 4; 
            _toolbar.style.borderTopLeftRadius = 4; _toolbar.style.borderTopRightRadius = 4;
            _toolbar.style.height = 32; // Explicit height to prevent collapse
            _toolbar.style.alignItems = Align.Center;
            _parent.Add(_toolbar);

            _btnSelect = new Button(() => SetMode(InteractionMode.Select)) { text = "Select" };
            _btnNode = new Button(() => SetMode(InteractionMode.Node)) { text = "Node" };
            _btnConnect = new Button(() => SetMode(InteractionMode.Connect)) { text = "Connect" };

            _toolbar.Add(_btnSelect);
            _toolbar.Add(_btnNode);
            _toolbar.Add(_btnConnect);

            StyleToolbarButton(_btnSelect);
            StyleToolbarButton(_btnNode);
            StyleToolbarButton(_btnConnect);
            
            UpdateToolbarVisuals();
        }
        
        /// <summary>
        /// Applies consistent styling to toolbar buttons
        /// </summary>
        private void StyleToolbarButton(Button b)
        {
            b.style.marginLeft = 2; b.style.marginRight = 2;
            b.style.unityTextAlign = TextAnchor.MiddleCenter;
            b.style.minWidth = 70;
            b.style.height = 24; // Explicit height for proper visibility
            b.style.fontSize = 12; // Ensure text is properly sized
        }
        
        /// <summary>
        /// Updates the visual appearance of toolbar buttons based on current mode
        /// </summary>
        private void UpdateToolbarVisuals()
        {
            var activeColor = new Color(0.3f, 0.5f, 0.8f);
            var inactiveColor = new Color(0.25f, 0.25f, 0.27f);
            
            _btnSelect.style.backgroundColor = _mode == InteractionMode.Select ? activeColor : inactiveColor;
            _btnNode.style.backgroundColor = _mode == InteractionMode.Node ? activeColor : inactiveColor;
            _btnConnect.style.backgroundColor = _mode == InteractionMode.Connect ? activeColor : inactiveColor;
        }
        
        /// <summary>
        /// Removes the toolbar from its parent
        /// </summary>
        public void Destroy()
        {
            _parent?.Remove(_toolbar);
        }
        
        /// <summary>
        /// Interaction modes for the graph editor
        /// </summary>
        public enum InteractionMode
        {
            Select,   // Select and move nodes
            Node,     // Create new nodes
            Connect   // Create connections between nodes
        }
    }
}