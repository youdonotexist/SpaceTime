using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// UI component for configuring connection properties (direction, weight, etc.)
    /// </summary>
    public class SpriteConnectionConfigUI : MonoBehaviour
    {
        [Header("UI References")]
        private Canvas configCanvas;
        private GameObject configPanel;
        private TextMeshProUGUI headerText;
        private Dropdown directionDropdown;
        private Slider weightSlider;
        private TextMeshProUGUI weightValueText;
        private Button deleteButton;
        private Button closeButton;
        
        [Header("Configuration")]
        [SerializeField] private Vector2 panelSize = new Vector2(300, 200);
        [SerializeField] private Vector2 screenOffset = new Vector2(-320, -20);
        [SerializeField] private Color backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        [SerializeField] private Color headerColor = new Color(0.25f, 0.25f, 0.35f, 1f);
        
        private SpriteConnection currentConnection;
        private SpriteRuntimeGraph graph;
        private bool isVisible = false;
        
        public bool IsVisible => isVisible;
        public event Action<SpriteConnection> OnConnectionDeleted;
        public event Action<SpriteConnection, bool> OnConnectionDirectionChanged;
        
        void Awake()
        {
            CreateScreenSpaceUI();
        }
        
        public void Initialize(SpriteRuntimeGraph graph)
        {
            this.graph = graph;
            SetVisible(false);
        }
        
        private void CreateScreenSpaceUI()
        {
            // Create screen space canvas for connection config
            var canvasGO = new GameObject("ConnectionConfigCanvas");
            canvasGO.transform.SetParent(transform);
            
            configCanvas = canvasGO.AddComponent<Canvas>();
            configCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            configCanvas.sortingOrder = 1000; // Above other UI
            
            // Add Canvas Scaler for consistent sizing
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            // Add GraphicRaycaster for interaction
            canvasGO.AddComponent<GraphicRaycaster>();
            
            CreateConfigPanel();
        }
        
        private void CreateConfigPanel()
        {
            configPanel = new GameObject("ConnectionConfigPanel");
            configPanel.transform.SetParent(configCanvas.transform, false);
            
            // Setup RectTransform for right-side docking
            var rectTransform = configPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 1f); // Top-right corner
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f); // Pivot at top-right
            rectTransform.anchoredPosition = screenOffset;
            rectTransform.sizeDelta = panelSize;
            
            // Add background image
            var backgroundImage = configPanel.AddComponent<Image>();
            backgroundImage.color = backgroundColor;
            
            CreateHeader();
            CreateConfigFields();
        }
        
        private void CreateHeader()
        {
            // Create header container
            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(configPanel.transform, false);
            
            var headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 30);
            
            // Create background GameObject
            var headerBgGO = new GameObject("HeaderBackground");
            headerBgGO.transform.SetParent(headerGO.transform, false);
            
            var headerBgRect = headerBgGO.AddComponent<RectTransform>();
            headerBgRect.anchorMin = Vector2.zero;
            headerBgRect.anchorMax = Vector2.one;
            headerBgRect.offsetMin = Vector2.zero;
            headerBgRect.offsetMax = Vector2.zero;
            
            var headerBg = headerBgGO.AddComponent<Image>();
            headerBg.color = headerColor;
            
            // Create text GameObject
            var headerTextGO = new GameObject("HeaderText");
            headerTextGO.transform.SetParent(headerGO.transform, false);
            
            var headerTextRect = headerTextGO.AddComponent<RectTransform>();
            headerTextRect.anchorMin = Vector2.zero;
            headerTextRect.anchorMax = Vector2.one;
            headerTextRect.offsetMin = Vector2.zero;
            headerTextRect.offsetMax = Vector2.zero;
            
            headerText = headerTextGO.AddComponent<TextMeshProUGUI>();
            headerText.text = "CONNECTION CONFIG";
            headerText.fontSize = 14;
            headerText.color = Color.white;
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.fontStyle = FontStyles.Bold;
        }
        
        private void CreateConfigFields()
        {
            float yOffset = -40f;
            const float fieldHeight = 30f;
            const float fieldSpacing = 5f;
            
            // Direction Dropdown
            CreateLabel("Direction:", yOffset);
            yOffset -= 20f;
            directionDropdown = CreateDirectionDropdown(yOffset);
            yOffset -= fieldHeight + fieldSpacing;
            
            // Weight Slider
            CreateLabel("Weight:", yOffset);
            yOffset -= 20f;
            var weightContainer = CreateWeightSlider(yOffset);
            yOffset -= fieldHeight + fieldSpacing * 2;
            
            // Delete Button
            deleteButton = CreateButton("Delete Connection", yOffset, OnDeleteButtonClicked);
            yOffset -= fieldHeight + fieldSpacing;
            
            // Close Button
            closeButton = CreateButton("Close", yOffset, OnCloseButtonClicked);
        }
        
        private void CreateLabel(string text, float yOffset)
        {
            var labelGO = new GameObject($"Label_{text}");
            labelGO.transform.SetParent(configPanel.transform, false);
            
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.anchoredPosition = new Vector2(0, yOffset);
            labelRect.sizeDelta = new Vector2(-20, 20);
            
            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = text;
            labelText.fontSize = 12;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
        }
        
        private Dropdown CreateDirectionDropdown(float yOffset)
        {
            var dropdownGO = new GameObject("DirectionDropdown");
            dropdownGO.transform.SetParent(configPanel.transform, false);
            
            var dropdownRect = dropdownGO.AddComponent<RectTransform>();
            dropdownRect.anchorMin = new Vector2(0, 1);
            dropdownRect.anchorMax = new Vector2(1, 1);
            dropdownRect.anchoredPosition = new Vector2(0, yOffset);
            dropdownRect.sizeDelta = new Vector2(-20, 25);
            
            var dropdown = dropdownGO.AddComponent<Dropdown>();
            var dropdownImage = dropdownGO.AddComponent<Image>();
            dropdownImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            // Create and assign template
            var template = CreateDropdownTemplate(dropdownGO);
            dropdown.template = template;
            
            // Create caption text
            var captionGO = new GameObject("Label");
            captionGO.transform.SetParent(dropdownGO.transform, false);
            var captionRect = captionGO.AddComponent<RectTransform>();
            captionRect.anchorMin = new Vector2(0, 0);
            captionRect.anchorMax = new Vector2(1, 1);
            captionRect.offsetMin = new Vector2(10, 2);
            captionRect.offsetMax = new Vector2(-25, -2);
            
            var captionText = captionGO.AddComponent<Text>();
            captionText.text = "From → To";
            captionText.fontSize = 10;
            captionText.color = Color.white;
            captionText.alignment = TextAnchor.MiddleLeft;
            dropdown.captionText = captionText;
            
            // Create arrow
            var arrowGO = new GameObject("Arrow");
            arrowGO.transform.SetParent(dropdownGO.transform, false);
            var arrowRect = arrowGO.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(20, 20);
            arrowRect.anchoredPosition = new Vector2(-10, 0);
            
            var arrowImage = arrowGO.AddComponent<Image>();
            arrowImage.color = Color.white;
            // Create simple arrow sprite
            arrowImage.sprite = CreateArrowSprite();
            
            // Add dropdown options
            dropdown.options.Clear();
            dropdown.options.Add(new Dropdown.OptionData("From → To"));
            dropdown.options.Add(new Dropdown.OptionData("To ← From"));
            
            // Add event listener
            dropdown.onValueChanged.AddListener(OnDirectionChanged);
            
            return dropdown;
        }
        
        private RectTransform CreateDropdownTemplate(GameObject parentDropdown)
        {
            // Create template container
            var templateGO = new GameObject("Template");
            templateGO.transform.SetParent(parentDropdown.transform, false);
            var templateRect = templateGO.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.anchoredPosition = new Vector2(0, -2);
            templateRect.sizeDelta = new Vector2(0, 150);
            
            // Add ScrollRect for the dropdown list
            var scrollRect = templateGO.AddComponent<ScrollRect>();
            var templateImage = templateGO.AddComponent<Image>();
            templateImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            
            // Create viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(templateGO.transform, false);
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            var mask = viewportGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = Color.clear;
            
            // Create content container
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 25);
            
            // Create item template
            var itemGO = new GameObject("Item");
            itemGO.transform.SetParent(contentGO.transform, false);
            var itemRect = itemGO.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.anchoredPosition = Vector2.zero;
            itemRect.sizeDelta = new Vector2(0, 20);
            
            // Add Toggle component (REQUIRED!)
            var toggle = itemGO.AddComponent<Toggle>();
            var itemBg = itemGO.AddComponent<Image>();
            itemBg.color = new Color(0.25f, 0.25f, 0.25f, 0.8f);
            toggle.targetGraphic = itemBg;
            
            // Create item label
            var itemLabelGO = new GameObject("Item Label");
            itemLabelGO.transform.SetParent(itemGO.transform, false);
            var itemLabelRect = itemLabelGO.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(10, 1);
            itemLabelRect.offsetMax = new Vector2(-10, -1);
            
            var itemText = itemLabelGO.AddComponent<Text>();
            itemText.text = "Option";
            itemText.fontSize = 10;
            itemText.color = Color.white;
            itemText.alignment = TextAnchor.MiddleLeft;
            
            // Configure ScrollRect
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            
            // Set item text for dropdown
            var dropdown = parentDropdown.GetComponent<Dropdown>();
            dropdown.itemText = itemText;
            
            // Initially hide the template
            templateGO.SetActive(false);
            
            return templateRect;
        }
        
        private UnityEngine.Sprite CreateArrowSprite()
        {
            // Create a simple downward arrow sprite
            var texture = new Texture2D(16, 16);
            var colors = new Color32[16 * 16];
            
            // Fill with transparent
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }
            
            // Draw simple arrow shape
            for (int y = 6; y <= 10; y++)
            {
                int width = y - 5;
                int startX = 8 - width / 2;
                int endX = 8 + width / 2;
                
                for (int x = startX; x <= endX; x++)
                {
                    if (x >= 0 && x < 16)
                        colors[y * 16 + x] = Color.white;
                }
            }
            
            texture.SetPixels32(colors);
            texture.Apply();
            
            return UnityEngine.Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.one * 0.5f);
        }
        
        private GameObject CreateWeightSlider(float yOffset)
        {
            var containerGO = new GameObject("WeightContainer");
            containerGO.transform.SetParent(configPanel.transform, false);
            
            var containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.anchoredPosition = new Vector2(0, yOffset);
            containerRect.sizeDelta = new Vector2(-20, 25);
            
            // Weight slider
            var sliderGO = new GameObject("WeightSlider");
            sliderGO.transform.SetParent(containerGO.transform, false);
            
            var sliderRect = sliderGO.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0);
            sliderRect.anchorMax = new Vector2(0.8f, 1);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;
            
            weightSlider = sliderGO.AddComponent<Slider>();
            var sliderImage = sliderGO.AddComponent<Image>();
            sliderImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            weightSlider.minValue = 0.1f;
            weightSlider.maxValue = 10f;
            weightSlider.value = 1f;
            weightSlider.onValueChanged.AddListener(OnWeightChanged);
            
            // Weight value text
            var valueTextGO = new GameObject("WeightValue");
            valueTextGO.transform.SetParent(containerGO.transform, false);
            
            var valueRect = valueTextGO.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.85f, 0);
            valueRect.anchorMax = new Vector2(1, 1);
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;
            
            weightValueText = valueTextGO.AddComponent<TextMeshProUGUI>();
            weightValueText.text = "1.0";
            weightValueText.fontSize = 10;
            weightValueText.color = Color.white;
            weightValueText.alignment = TextAlignmentOptions.Center;
            
            return containerGO;
        }
        
        private Button CreateButton(string text, float yOffset, UnityEngine.Events.UnityAction onClick)
        {
            var buttonGO = new GameObject($"Button_{text}");
            buttonGO.transform.SetParent(configPanel.transform, false);
            
            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0, 1);
            buttonRect.anchorMax = new Vector2(1, 1);
            buttonRect.anchoredPosition = new Vector2(0, yOffset);
            buttonRect.sizeDelta = new Vector2(-20, 25);
            
            var button = buttonGO.AddComponent<Button>();
            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.4f, 1f);
            
            button.onClick.AddListener(onClick);
            
            // Button text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var buttonText = textGO.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 12;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            
            return button;
        }
        
        public void ShowConnectionConfiguration(SpriteConnection connection)
        {
            if (connection == null) return;
            
            currentConnection = connection;
            SetVisible(true);
            
            // Update UI values
            UpdateConfigUI();
        }
        
        public void HideConnectionConfiguration()
        {
            currentConnection = null;
            SetVisible(false);
        }
        
        private void UpdateConfigUI()
        {
            if (currentConnection == null) return;
            
            var connectionData = currentConnection.ConnectionDataInstance;
            
            // Update header with connection info
            headerText.text = $"CONNECTION: {connectionData.fromNodeId} → {connectionData.toNodeId}";
            
            // Update direction dropdown (default to forward)
            directionDropdown.value = 0;
            
            // Update weight slider
            weightSlider.value = connectionData.weight;
            weightValueText.text = connectionData.weight.ToString("F1");
        }
        
        private void OnDirectionChanged(int value)
        {
            if (currentConnection == null) return;
            
            bool shouldReverse = (value == 1);
            OnConnectionDirectionChanged?.Invoke(currentConnection, shouldReverse);
        }
        
        private void OnWeightChanged(float value)
        {
            if (currentConnection == null) return;
            
            currentConnection.ConnectionDataInstance.weight = value;
            weightValueText.text = value.ToString("F1");
        }
        
        private void OnDeleteButtonClicked()
        {
            if (currentConnection == null) return;
            
            OnConnectionDeleted?.Invoke(currentConnection);
            HideConnectionConfiguration();
        }
        
        private void OnCloseButtonClicked()
        {
            HideConnectionConfiguration();
        }
        
        public void SetVisible(bool visible)
        {
            isVisible = visible;
            if (configCanvas != null)
                configCanvas.gameObject.SetActive(visible);
        }
        
        private void OnDestroy()
        {
            // Cleanup event listeners
            if (directionDropdown != null)
                directionDropdown.onValueChanged.RemoveAllListeners();
            if (weightSlider != null)
                weightSlider.onValueChanged.RemoveAllListeners();
            if (deleteButton != null)
                deleteButton.onClick.RemoveAllListeners();
            if (closeButton != null)
                closeButton.onClick.RemoveAllListeners();
        }
    }
}