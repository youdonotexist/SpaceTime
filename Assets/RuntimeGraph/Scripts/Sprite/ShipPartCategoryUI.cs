using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// UI system for ship part category selection with buttons along the bottom left of the screen
    /// </summary>
    public class ShipPartCategoryUI : MonoBehaviour
    {
        [Header("UI Configuration")]
        public Vector2 buttonSize = new Vector2(40, 40);
        public float buttonSpacing = 5f;
        public Vector2 bottomLeftOffset = new Vector2(20, 20);
        public Color categoryButtonColor = new Color(0.2f, 0.2f, 0.3f, 0.9f);
        public Color categoryButtonHighlightColor = new Color(0.3f, 0.5f, 0.8f, 0.9f);
        
        [Header("Part List Configuration")]
        public Vector2 partListSize = new Vector2(200, 300);
        public Color partListBackgroundColor = new Color(0.1f, 0.1f, 0.2f, 0.95f);
        public Color partButtonColor = new Color(0.15f, 0.15f, 0.25f, 0.9f);
        public Color partButtonHighlightColor = new Color(0.25f, 0.4f, 0.7f, 0.9f);
        
        private Canvas categoryCanvas;
        private GameObject categoryButtonsContainer;
        private GameObject currentPartList;
        private SpriteNodePalette.NodeTypeData selectedNodeType;
        private string currentSelectedCategory;
        
        // Category data
        private List<CategoryData> categories = new List<CategoryData>();
        private SpriteNodePalette originalPalette;
        
        [System.Serializable]
        public class CategoryData
        {
            public string categoryName;
            public Color categoryColor;
            public List<SpriteNodePalette.NodeTypeData> nodeTypes = new List<SpriteNodePalette.NodeTypeData>();
            public Button categoryButton;
        }
        
        // Events
        public System.Action<SpriteNodePalette.NodeTypeData> OnNodeTypeSelected;
        
        public void Initialize(SpriteNodePalette palette)
        {
            originalPalette = palette;
            LoadCategoryData();
            CreateCategoryUI();
            
            // Hide the original palette by default
            if (originalPalette != null)
            {
                originalPalette.SetVisible(false);
            }
        }
        
        private void LoadCategoryData()
        {
            // Get all engine parts and group by category
            var allParts = EnginePartCatalog.GetAllEngineParts();
            var categoryGroups = new Dictionary<string, List<EnginePartNodeData>>();
            
            foreach (var part in allParts)
            {
                if (!categoryGroups.ContainsKey(part.category))
                {
                    categoryGroups[part.category] = new List<EnginePartNodeData>();
                }
                categoryGroups[part.category].Add(part);
            }
            
            // Create category data with node types
            foreach (var categoryGroup in categoryGroups)
            {
                var categoryData = new CategoryData
                {
                    categoryName = categoryGroup.Key,
                    categoryColor = categoryGroup.Value.Count > 0 ? categoryGroup.Value[0].color : Color.white
                };
                
                // Convert engine parts to node types
                foreach (var part in categoryGroup.Value)
                {
                    // Determine engine type from part name for consistent channel assignment
                    var engineType = DetermineEngineTypeFromName(part.name);
                    
                    var nodeType = new SpriteNodePalette.NodeTypeData
                    {
                        name = part.name,
                        category = part.category,
                        color = part.color,
                        description = part.description,
                        // Default MIDI values for compatibility
                        note = UnityEngine.Random.Range(36, 84),
                        velocity = UnityEngine.Random.Range(60, 100),
                        channel = GetEngineTypeChannel(engineType),
                        duration = 0.08f,
                        // Generate procedural icon with error handling
                        icon = null
                    };
                    
                    // Try to generate icon, but don't fail if it doesn't work
                    try
                    {
                        nodeType.icon = EnginePartIconGenerator.GenerateIconForPart(part);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[DEBUG_LOG] Failed to generate icon for part {part.name}: {ex.Message}");
                        nodeType.icon = null;
                    }
                    
                    categoryData.nodeTypes.Add(nodeType);
                }
                
                categories.Add(categoryData);
            }
        }
        
        private SpriteNode.EngineType DetermineEngineTypeFromName(string partName)
        {
            string lowerName = partName.ToLowerInvariant();
            
            // Check for thruster keywords
            if (lowerName.Contains("thruster") || lowerName.Contains("rcs") || 
                lowerName.Contains("maneuvering") || lowerName.Contains("attitude"))
            {
                return SpriteNode.EngineType.Thruster;
            }
            
            // Check for retro engine keywords
            if (lowerName.Contains("retro") || lowerName.Contains("reverse") || 
                lowerName.Contains("brake") || lowerName.Contains("deceleration"))
            {
                return SpriteNode.EngineType.RetroEngine;
            }
            
            // Check for stability engine keywords
            if (lowerName.Contains("stability") || lowerName.Contains("stabilizer") || 
                lowerName.Contains("gyro") || lowerName.Contains("control"))
            {
                return SpriteNode.EngineType.StabilityEngine;
            }
            
            // Default to main engine for anything else with engine-like keywords
            if (lowerName.Contains("engine") || lowerName.Contains("propulsion") || 
                lowerName.Contains("drive") || lowerName.Contains("motor"))
            {
                return SpriteNode.EngineType.MainEngine;
            }
            
            // Fallback to main engine
            return SpriteNode.EngineType.MainEngine;
        }
        
        private int GetEngineTypeChannel(SpriteNode.EngineType engineType)
        {
            return engineType switch
            {
                SpriteNode.EngineType.MainEngine => 0,
                SpriteNode.EngineType.Thruster => 1,
                SpriteNode.EngineType.RetroEngine => 2,
                SpriteNode.EngineType.StabilityEngine => 3,
                _ => 0
            };
        }
        
        private void CreateCategoryUI()
        {
            // Create screen space canvas for category UI
            var canvasGO = new GameObject("ShipPartCategoryCanvas");
            canvasGO.transform.SetParent(transform);
            
            categoryCanvas = canvasGO.AddComponent<Canvas>();
            categoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            categoryCanvas.sortingOrder = 997; // Below palette and config UI
            
            // Add Canvas Scaler for consistent sizing
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            // Add GraphicRaycaster for interaction
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Create category buttons container
            CreateCategoryButtons();
        }
        
        private void CreateCategoryButtons()
        {
            categoryButtonsContainer = new GameObject("CategoryButtonsContainer");
            categoryButtonsContainer.transform.SetParent(categoryCanvas.transform, false);
            
            // Setup RectTransform for bottom-left positioning
            var containerRect = categoryButtonsContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0); // Bottom-left anchor
            containerRect.anchorMax = new Vector2(0, 0);
            containerRect.pivot = new Vector2(0, 0); // Pivot at bottom-left
            containerRect.anchoredPosition = bottomLeftOffset;
            
            // Add horizontal layout group for buttons
            var layoutGroup = categoryButtonsContainer.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = buttonSpacing;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            
            // Create category buttons
            for (int i = 0; i < categories.Count; i++)
            {
                CreateCategoryButton(categories[i], i);
            }
        }
        
        private void CreateCategoryButton(CategoryData categoryData, int index)
        {
            // Create button GameObject
            var buttonGO = new GameObject($"CategoryButton_{categoryData.categoryName}");
            buttonGO.transform.SetParent(categoryButtonsContainer.transform, false);
            
            // Setup button RectTransform
            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.sizeDelta = buttonSize;
            
            // Add button component
            var button = buttonGO.AddComponent<Button>();
            categoryData.categoryButton = button;
            
            // Add button background image
            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = categoryButtonColor;
            button.targetGraphic = buttonImage;
            
            // Create button text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 0);
            textRect.offsetMax = new Vector2(-5, 0);
            
            var buttonText = textGO.AddComponent<Text>();
            buttonText.text = GetShortCategoryName(categoryData.categoryName);
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 10;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.fontStyle = FontStyle.Bold;
            
            // Setup button colors
            var colors = button.colors;
            colors.normalColor = categoryButtonColor;
            colors.highlightedColor = categoryButtonHighlightColor;
            colors.pressedColor = categoryButtonHighlightColor;
            colors.selectedColor = categoryButtonHighlightColor;
            button.colors = colors;
            
            // Add click event
            button.onClick.AddListener(() => OnCategoryButtonClicked(categoryData));
        }
        
        private string GetShortCategoryName(string categoryName)
        {
            // Create shorter names for buttons
            return categoryName switch
            {
                "Power & Energy" => "Power",
                "Thermal & Coolant" => "Thermal",
                "Atmosphere & Life Support" => "Life Support",
                "Structural & Hull" => "Structure",
                "Propulsion & Maneuvering" => "Propulsion",
                "Navigation, Comms & Sensors" => "Nav/Comms",
                "Data, Control & Security" => "Control",
                "Manufacturing, Inventory & Logistics" => "Manufacturing",
                "Defense & Shielding" => "Defense",
                _ => categoryName
            };
        }
        
        private void OnCategoryButtonClicked(CategoryData categoryData)
        {
            // Close current part list if it's the same category
            if (currentSelectedCategory == categoryData.categoryName && currentPartList != null)
            {
                HidePartList();
                return;
            }
            
            // Hide current part list if showing
            HidePartList();
            
            // Show new part list
            ShowPartList(categoryData);
            currentSelectedCategory = categoryData.categoryName;
            
            Debug.Log($"Selected category: {categoryData.categoryName} with {categoryData.nodeTypes.Count} parts");
        }
        
        private void ShowPartList(CategoryData categoryData)
        {
            // Create part list container
            currentPartList = new GameObject($"PartList_{categoryData.categoryName}");
            currentPartList.transform.SetParent(categoryCanvas.transform, false);
            
            // Position part list above the category button
            var listRect = currentPartList.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0);
            listRect.anchorMax = new Vector2(0, 0);
            listRect.pivot = new Vector2(0, 0);
            listRect.anchoredPosition = new Vector2(bottomLeftOffset.x, bottomLeftOffset.y + buttonSize.y + buttonSpacing);
            listRect.sizeDelta = partListSize;
            
            // Add background image
            var listImage = currentPartList.AddComponent<Image>();
            listImage.color = partListBackgroundColor;
            
            // Add scroll view for parts
            CreatePartScrollView(categoryData);
        }
        
        private void CreatePartScrollView(CategoryData categoryData)
        {
            // Create scroll view
            var scrollViewGO = new GameObject("ScrollView");
            scrollViewGO.transform.SetParent(currentPartList.transform, false);
            
            var scrollRect = scrollViewGO.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(5, 5);
            scrollRect.offsetMax = new Vector2(-5, -5);
            
            var scrollView = scrollViewGO.AddComponent<ScrollRect>();
            
            // Create viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollViewGO.transform, false);
            
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            var viewportMask = viewportGO.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            
            var viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = Color.white;
            
            // Create content container
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.anchoredPosition = Vector2.zero;
            
            // Add vertical layout group
            var layoutGroup = contentGO.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 2f;
            layoutGroup.padding = new RectOffset(5, 5, 5, 5);
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            
            // Add content size fitter
            var contentSizeFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Setup scroll view
            scrollView.content = contentRect;
            scrollView.viewport = viewportRect;
            scrollView.vertical = true;
            scrollView.horizontal = false;
            
            // Create part buttons
            CreatePartButtons(categoryData, contentGO);
        }
        
        private void CreatePartButtons(CategoryData categoryData, GameObject contentContainer)
        {
            Debug.Log($"[DEBUG_LOG] Creating {categoryData.nodeTypes.Count} part buttons for category: {categoryData.categoryName}");
            foreach (var nodeType in categoryData.nodeTypes)
            {
                CreatePartButton(nodeType, contentContainer);
                Debug.Log($"[DEBUG_LOG] Created part button: {nodeType.name}");
            }
        }
        
        private void CreatePartButton(SpriteNodePalette.NodeTypeData nodeType, GameObject parent)
        {
            // Create button GameObject
            var buttonGO = new GameObject($"PartButton_{nodeType.name}");
            buttonGO.transform.SetParent(parent.transform, false);
            
            // Setup button RectTransform
            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(0, 30); // Height only, width controlled by layout
            
            // Add layout element
            var layoutElement = buttonGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 30;
            
            // Add button component
            var button = buttonGO.AddComponent<Button>();
            
            // Add button background image
            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = partButtonColor;
            button.targetGraphic = buttonImage;
            
            // Create button text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 0);
            textRect.offsetMax = new Vector2(-5, 0);
            
            var buttonText = textGO.AddComponent<Text>();
            buttonText.text = nodeType.name;
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 9;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleLeft;
            
            // Setup button colors
            var colors = button.colors;
            colors.normalColor = partButtonColor;
            colors.highlightedColor = partButtonHighlightColor;
            colors.pressedColor = partButtonHighlightColor;
            colors.selectedColor = partButtonHighlightColor;
            button.colors = colors;
            
            // Add click event
            button.onClick.AddListener(() => OnPartButtonClicked(nodeType));
        }
        
        private void OnPartButtonClicked(SpriteNodePalette.NodeTypeData nodeType)
        {
            selectedNodeType = nodeType;
            OnNodeTypeSelected?.Invoke(nodeType);
            
            // Hide the part list after selection
            HidePartList();
            
            Debug.Log($"Selected ship part: {nodeType.name} from category {nodeType.category}");
        }
        
        private void HidePartList()
        {
            if (currentPartList != null)
            {
                DestroyImmediate(currentPartList);
                currentPartList = null;
            }
            currentSelectedCategory = null;
        }
        
        public void SetVisible(bool visible)
        {
            if (categoryCanvas != null)
                categoryCanvas.gameObject.SetActive(visible);
        }
        
        public void ToggleVisibility()
        {
            if (categoryCanvas != null)
                categoryCanvas.gameObject.SetActive(!categoryCanvas.gameObject.activeSelf);
        }
        
        public bool IsVisible => categoryCanvas != null && categoryCanvas.gameObject.activeSelf;
        public SpriteNodePalette.NodeTypeData SelectedNodeType => selectedNodeType;
        
        public void ShowOriginalPalette()
        {
            if (originalPalette != null)
            {
                originalPalette.SetVisible(true);
            }
        }
        
        public void HideOriginalPalette()
        {
            if (originalPalette != null)
            {
                originalPalette.SetVisible(false);
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup
            HidePartList();
        }
    }
}