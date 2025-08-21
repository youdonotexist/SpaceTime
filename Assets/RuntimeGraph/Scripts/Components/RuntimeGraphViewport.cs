using UnityEngine;
using UnityEngine.UIElements;

namespace RuntimeGraph
{
    /// <summary>
    /// Handles viewport operations: panning, zooming, coordinate transformations, and grid rendering
    /// </summary>
    public class RuntimeGraphViewport
    {
        private readonly VisualElement _graphRoot;
        private readonly GridLayer _gridLayer;
        
        // Pan and zoom state
        private Vector2 _pan = Vector2.zero;
        private float _zoom = 1f;
        private bool _panning;
        private Vector2 _panStart;
        private Vector2 _panOrigin;
        
        // Right mouse tracking for pan vs context click
        private bool _rightButtonDown;
        private Vector2 _rightDownPos;
        private const float _panStartThreshold = 3f;
        
        // Grid constants
        private const float GridSmall = 32f;
        private const float GridLarge = 160f;
        
        public Vector2 Pan => _pan;
        public float Zoom => _zoom;
        public bool IsPanning => _panning;
        
        public RuntimeGraphViewport(VisualElement graphRoot)
        {
            _graphRoot = graphRoot;
            _gridLayer = new GridLayer();
            _gridLayer.SetViewport(this);
            _graphRoot.Add(_gridLayer);
        }
        
        public void RegisterEvents()
        {
            _graphRoot.RegisterCallback<WheelEvent>(OnWheel);
            _graphRoot.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _graphRoot.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _graphRoot.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }
        
        public void UnregisterEvents()
        {
            _graphRoot.UnregisterCallback<WheelEvent>(OnWheel);
            _graphRoot.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _graphRoot.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _graphRoot.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }
        
        private void OnWheel(WheelEvent evt)
        {
            Vector2 mouseLocal = _graphRoot.WorldToLocal(evt.mousePosition);
            Vector2 graphPosBefore = ScreenToGraph(mouseLocal);
            float zoomDelta = -evt.delta.y * 0.001f;
            _zoom = Mathf.Clamp(_zoom + zoomDelta, 0.1f, 3f);
            Vector2 graphPosAfter = ScreenToGraph(mouseLocal);
            _pan += (graphPosBefore - graphPosAfter) * _zoom;
            ApplyPanZoom();
            evt.StopPropagation();
        }
        
        public bool HandlePointerDown(PointerDownEvent evt)
        {
            if (evt.button == 1) // RMB: prepare for possible pan
            {
                _rightButtonDown = true;
                _rightDownPos = _graphRoot.WorldToLocal(evt.position);
                return false; // Don't consume event, allow ContextClick to fire if no drag
            }
            if (evt.button == 2) // MMB: start panning immediately
            {
                _panning = true;
                _panStart = _graphRoot.WorldToLocal(evt.position);
                _panOrigin = _pan;
                _graphRoot.CapturePointer(evt.pointerId);
                return true; // Consume event
            }
            return false;
        }
        
        private void OnPointerDown(PointerDownEvent evt)
        {
            HandlePointerDown(evt);
        }
        
        public bool HandlePointerMove(PointerMoveEvent evt)
        {
            if (_panning)
            {
                Vector2 cur = _graphRoot.WorldToLocal(evt.position);
                Vector2 delta = cur - _panStart;
                _pan = _panOrigin + delta;
                ApplyPanZoom();
                return true; // Consume event
            }
            
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
                    return true; // Consume event
                }
            }
            
            return false;
        }
        
        private void OnPointerMove(PointerMoveEvent evt)
        {
            HandlePointerMove(evt);
        }
        
        public bool HandlePointerUp(PointerUpEvent evt)
        {
            if (evt.button == 2 && _panning) // MMB release
            {
                _panning = false;
                _graphRoot.ReleasePointer(evt.pointerId);
                return true; // Consume event
            }
            if (evt.button == 1) // RMB release
            {
                _rightButtonDown = false;
                return false; // Don't consume, allow context menu if no pan occurred
            }
            return false;
        }
        
        private void OnPointerUp(PointerUpEvent evt)
        {
            HandlePointerUp(evt);
        }
        
        private void ApplyPanZoom()
        {
            _graphRoot.style.translate = new Translate(_pan.x, _pan.y);
            _graphRoot.style.scale = new Scale(Vector2.one * _zoom);
            _gridLayer.MarkDirtyRepaint();
        }
        
        public Vector2 ScreenToGraph(Vector2 screenLocal)
        {
            // Since we apply transforms via CSS (translate/scale), we need to account for them properly
            // The _graphRoot has transforms applied, so convert through the transform system
            Vector2 worldPos = _graphRoot.parent.LocalToWorld(screenLocal);
            Vector2 graphLocalPos = _graphRoot.WorldToLocal(worldPos);
            return graphLocalPos;
        }
        
        public Vector2 GraphToScreen(Vector2 graphPoint)
        {
            return graphPoint * _zoom + _pan;
        }
        
        /// <summary>
        /// Grid rendering layer
        /// </summary>
        private class GridLayer : VisualElement
        {
            private RuntimeGraphViewport _viewport;
            
            public GridLayer()
            {
                generateVisualContent += OnGenerate;
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.left = 0; style.top = 0; style.right = 0; style.bottom = 0;
            }
            
            public void SetViewport(RuntimeGraphViewport viewport)
            {
                _viewport = viewport;
            }
            
            private void OnGenerate(MeshGenerationContext ctx)
            {
                var p = ctx.painter2D;
                p.strokeColor = new Color(1, 1, 1, 0.1f);
                p.lineWidth = 1.0f;
                // Use exact colors matching original RuntimeGraphUI
                DrawGrid(p, new Color(1f, 1f, 1f, 0.06f), GridSmall);  // _gridSmallColor
                DrawGrid(p, new Color(1f, 1f, 1f, 0.12f), GridLarge); // _gridLargeColor
            }
            
            private void DrawGrid(Painter2D p, Color color, float spacing)
            {
                if (_viewport == null) return;
                
                // Scale the grid with zoom to match node coordinate system
                float scaledSpacing = spacing * _viewport.Zoom;
                if (scaledSpacing < 8f) return; // Only hide when too small to see
                
                float left = contentRect.xMin, right = contentRect.xMax, top = contentRect.yMin, bottom = contentRect.yMax;
                float xOffset = Mathf.Repeat(_viewport.Pan.x, scaledSpacing);
                float yOffset = Mathf.Repeat(_viewport.Pan.y, scaledSpacing);

                p.lineWidth = 1f;
                p.strokeColor = color;

                // Vertical lines
                for (float x = left + xOffset; x < right; x += scaledSpacing)
                {
                    p.BeginPath(); 
                    p.MoveTo(new Vector2(x, top)); 
                    p.LineTo(new Vector2(x, bottom)); 
                    p.Stroke();
                }
                // Horizontal lines
                for (float y = top + yOffset; y < bottom; y += scaledSpacing)
                {
                    p.BeginPath(); 
                    p.MoveTo(new Vector2(left, y)); 
                    p.LineTo(new Vector2(right, y)); 
                    p.Stroke();
                }
            }
        }
    }
}