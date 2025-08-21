using System;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// World space toolbar for the sprite-based graph system
    /// Provides mode switching UI that can be integrated into the game world
    /// </summary>
    public class SpriteGraphToolbar : MonoBehaviour
    {
        [Header("UI Settings")]
        public Vector3 screenPosition = new Vector3(50, 0, 0);
        public float toolbarScale = 1f;
        public Color backgroundColor = new Color(0.15f, 0.15f, 0.17f, 0.9f);
        public Color activeButtonColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        public Color inactiveButtonColor = new Color(0.25f, 0.25f, 0.27f, 1f);
        
        [Header("Button Settings")]
        public float buttonWidth = 70f;
        public float buttonHeight = 32f;
        public float buttonSpacing = 4f;
        
        private SpriteRuntimeGraph graph;
        private Canvas toolbarCanvas;
        private GameObject toolbarPanel;
        private Button selectButton;
        private Button nodeButton;
        private Button connectButton;
        private Button playButton;
        
        private SpriteRuntimeGraph.InteractionMode currentMode = SpriteRuntimeGraph.InteractionMode.Select;
        
        public event Action<SpriteRuntimeGraph.InteractionMode> ModeChanged;
        public SpriteRuntimeGraph.InteractionMode Mode => currentMode;
        
        public void Initialize(SpriteRuntimeGraph graph)
        {
            this.graph = graph;
            CreateToolbarUI();
            UpdateButtonVisuals();
            UpdatePlayButtonText();
        }
        
        private void CreateToolbarUI()
        {
            // Ensure EventSystem exists for UI interaction
            EnsureEventSystem();
            
            // Create screen space canvas for toolbar
            var canvasGO = new GameObject("ToolbarCanvas");
            canvasGO.transform.SetParent(transform);
            
            toolbarCanvas = canvasGO.AddComponent<Canvas>();
            toolbarCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            toolbarCanvas.sortingOrder = 1000; // High priority to render on top
            
            // Add Canvas Scaler for consistent sizing
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            // Add GraphicRaycaster for button interaction
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Create toolbar panel
            CreateToolbarPanel();
            
            // Create buttons
            CreateButtons();
        }
        
        private void EnsureEventSystem()
        {
            // Check if EventSystem exists, create one if it doesn't
            var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
        
        private void CreateToolbarPanel()
        {
            toolbarPanel = new GameObject("ToolbarPanel");
            toolbarPanel.transform.SetParent(toolbarCanvas.transform, false);
            
            // Add RectTransform
            var rectTransform = toolbarPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = screenPosition;
            rectTransform.sizeDelta = new Vector2(buttonWidth * 4 + buttonSpacing * 5, buttonHeight + buttonSpacing * 2);
            
            // Add background image
            var backgroundImage = toolbarPanel.AddComponent<Image>();
            backgroundImage.color = backgroundColor;
            backgroundImage.type = Image.Type.Sliced;
            
            // Create rounded corners using a simple sprite
            backgroundImage.sprite = CreateRoundedRectSprite();
        }
        
        private void CreateButtons()
        {
            // Select Button
            selectButton = CreateButton("Select", 0, () => SetMode(SpriteRuntimeGraph.InteractionMode.Select));
            
            // Node Button
            nodeButton = CreateButton("Node", 1, () => SetMode(SpriteRuntimeGraph.InteractionMode.Node));
            
            // Connect Button
            connectButton = CreateButton("Connect", 2, () => SetMode(SpriteRuntimeGraph.InteractionMode.Connect));
            
            // Play Button - Toggle playback instead of setting mode
            playButton = CreateButton("Play", 3, () => TogglePlayback());
        }
        
        private Button CreateButton(string text, int index, System.Action onClick)
        {
            var buttonGO = new GameObject($"Button_{text}");
            buttonGO.transform.SetParent(toolbarPanel.transform, false);
            
            // Setup RectTransform
            var rectTransform = buttonGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0, 0.5f);
            rectTransform.pivot = new Vector2(0, 0.5f);
            rectTransform.anchoredPosition = new Vector2(buttonSpacing + index * (buttonWidth + buttonSpacing), 0);
            rectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            
            // Add Button component
            var button = buttonGO.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            
            // Add Image for button background
            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = inactiveButtonColor;
            button.targetGraphic = buttonImage;
            
            // Create button sprite
            buttonImage.sprite = CreateButtonSprite();
            buttonImage.type = Image.Type.Sliced;
            
            // Add Text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            var textRectTransform = textGO.AddComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.offsetMin = Vector2.zero;
            textRectTransform.offsetMax = Vector2.zero;
            
            var textComponent = textGO.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 16;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            
            return button;
        }
        
        private UnityEngine.Sprite CreateRoundedRectSprite()
        {
            // Create a simple rounded rectangle texture
            int width = 32, height = 32;
            var texture = new Texture2D(width, height);
            var colors = new Color32[width * height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Create rounded corners
                    bool isCorner = (x < 4 || x >= width - 4) && (y < 4 || y >= height - 4);
                    bool isBorder = x == 0 || x == width - 1 || y == 0 || y == height - 1;
                    
                    if (isCorner)
                    {
                        // Calculate distance from corner
                        int cornerX = (x < width / 2) ? 4 : width - 5;
                        int cornerY = (y < height / 2) ? 4 : height - 5;
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cornerX, cornerY));
                        
                        if (dist > 4)
                            colors[y * width + x] = Color.clear;
                        else if (dist > 3)
                            colors[y * width + x] = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundColor.a * 0.5f);
                        else
                            colors[y * width + x] = backgroundColor;
                    }
                    else if (isBorder)
                    {
                        colors[y * width + x] = new Color(0, 0, 0, 0.4f); // Border
                    }
                    else
                    {
                        colors[y * width + x] = backgroundColor;
                    }
                }
            }
            
            texture.SetPixels32(colors);
            texture.Apply();
            
            return UnityEngine.Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * 0.5f, 100f, 0, SpriteMeshType.FullRect, Vector4.one * 8f);
        }
        
        private UnityEngine.Sprite CreateButtonSprite()
        {
            // Create a simple button texture
            int width = 16, height = 16;
            var texture = new Texture2D(width, height);
            var colors = new Color32[width * height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = x == 0 || x == width - 1 || y == 0 || y == height - 1;
                    
                    if (isBorder)
                        colors[y * width + x] = new Color(0, 0, 0, 0.3f);
                    else
                        colors[y * width + x] = Color.white;
                }
            }
            
            texture.SetPixels32(colors);
            texture.Apply();
            
            return UnityEngine.Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * 0.5f, 100f, 0, SpriteMeshType.FullRect, Vector4.one * 2f);
        }
        
        public void SetMode(SpriteRuntimeGraph.InteractionMode mode)
        {
            if (currentMode == mode) return;
            
            currentMode = mode;
            UpdateButtonVisuals();
            ModeChanged?.Invoke(currentMode);
        }
        
        private void TogglePlayback()
        {
            // Call the playback controller through the runtime graph
            if (graph?.PlaybackController != null)
            {
                graph.PlaybackController.TogglePlayback();
                UpdatePlayButtonText();
            }
        }
        
        private void UpdatePlayButtonText()
        {
            if (playButton == null || graph?.PlaybackController == null) return;
            
            // Get the Text component from the play button
            var textComponent = playButton.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                // Update text based on playback state
                bool isPlaying = graph.PlaybackController.settings.isPlaying;
                textComponent.text = isPlaying ? "Stop" : "Play";
            }
        }
        
        private void UpdateButtonVisuals()
        {
            // Update button colors based on current mode
            selectButton.GetComponent<Image>().color = 
                currentMode == SpriteRuntimeGraph.InteractionMode.Select ? activeButtonColor : inactiveButtonColor;
            
            nodeButton.GetComponent<Image>().color = 
                currentMode == SpriteRuntimeGraph.InteractionMode.Node ? activeButtonColor : inactiveButtonColor;
            
            connectButton.GetComponent<Image>().color = 
                currentMode == SpriteRuntimeGraph.InteractionMode.Connect ? activeButtonColor : inactiveButtonColor;
            
            playButton.GetComponent<Image>().color = 
                currentMode == SpriteRuntimeGraph.InteractionMode.Play ? activeButtonColor : inactiveButtonColor;
        }
        
        public void SetVisible(bool visible)
        {
            toolbarCanvas.gameObject.SetActive(visible);
        }
        
        public void SetPosition(Vector3 screenPos)
        {
            screenPosition = screenPos;
            if (toolbarPanel != null)
            {
                toolbarPanel.GetComponent<RectTransform>().anchoredPosition = screenPos;
            }
        }
        
        public void SetScale(float scale)
        {
            toolbarScale = scale;
            if (toolbarCanvas != null)
            {
                toolbarCanvas.transform.localScale = Vector3.one * scale;
            }
        }
        
        public void SetColors(Color background, Color active, Color inactive)
        {
            backgroundColor = background;
            activeButtonColor = active;
            inactiveButtonColor = inactive;
            
            // Update existing visuals
            if (toolbarPanel != null)
            {
                toolbarPanel.GetComponent<Image>().color = backgroundColor;
            }
            
            UpdateButtonVisuals();
        }
        
        // Update method to handle dynamic positioning or other updates
        private void Update()
        {
            // Could add dynamic positioning based on screen size changes
            // or other runtime updates here if needed
        }
        
        private void OnDestroy()
        {
            // Clean up created sprites and textures
            if (toolbarPanel != null)
            {
                var image = toolbarPanel.GetComponent<Image>();
                if (image != null && image.sprite != null)
                {
                    var texture = image.sprite.texture;
                    if (texture.name.Contains("(Clone)"))
                    {
                        DestroyImmediate(texture);
                    }
                    DestroyImmediate(image.sprite);
                }
            }
            
            // Clean up button sprites
            CleanupButtonSprite(selectButton);
            CleanupButtonSprite(nodeButton);
            CleanupButtonSprite(connectButton);
            CleanupButtonSprite(playButton);
        }
        
        private void CleanupButtonSprite(Button button)
        {
            if (button == null) return;
            
            var image = button.GetComponent<Image>();
            if (image != null && image.sprite != null)
            {
                var texture = image.sprite.texture;
                if (texture.name.Contains("(Clone)"))
                {
                    DestroyImmediate(texture);
                }
                DestroyImmediate(image.sprite);
            }
        }
        
        // Alternative world-space toolbar implementation
        public void ConvertToWorldSpace(Camera worldCamera, Vector3 worldPosition)
        {
            if (toolbarCanvas == null) return;
            
            // Convert to world space canvas
            toolbarCanvas.renderMode = RenderMode.WorldSpace;
            toolbarCanvas.worldCamera = worldCamera;
            
            // Position in world space
            transform.position = worldPosition;
            transform.localScale = Vector3.one * 0.01f; // Scale for world space
            
            // Adjust canvas settings for world space
            var rectTransform = toolbarCanvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300, 50); // Appropriate size for world space
        }
        
        // Debug support
        private void OnDrawGizmos()
        {
            if (toolbarCanvas != null && toolbarCanvas.renderMode == RenderMode.WorldSpace)
            {
                // Draw toolbar bounds in world space
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(transform.position, transform.localScale * 3f);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Show toolbar configuration
            Gizmos.color = backgroundColor;
            if (toolbarCanvas != null && toolbarCanvas.renderMode == RenderMode.WorldSpace)
            {
                var bounds = new Vector3(3f, 0.5f, 0.1f);
                bounds.Scale(transform.localScale);
                Gizmos.DrawCube(transform.position, bounds);
            }
        }
    }
}