using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// Left-side node palette for selecting different node types to create
    /// Features multi-column vertical scrolling with categorized node types
    /// </summary>
    public class SpriteNodePalette : MonoBehaviour
    {
        [System.Serializable]
        public class NodeTypeData
        {
            public string name;
            public string category;
            public Color color;
            public int note;
            public int velocity;
            public int channel;
            public float duration;
            public string description;
            public UnityEngine.Sprite icon;
        }

        [System.Serializable]
        public class NodeCategory
        {
            public string categoryName;
            public Color categoryColor;
            public List<NodeTypeData> nodeTypes = new List<NodeTypeData>();
        }

        [Header("Palette Configuration")]
        public Vector2 paletteSize = new Vector2(300, 600);
        public Vector2 screenOffset = new Vector2(20, 0); // Offset from left edge
        public Color backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        public Color categoryHeaderColor = new Color(0.25f, 0.25f, 0.35f, 1f);
        
        [Header("Layout")]
        public int columnsPerRow = 2;
        public float itemSpacing = 5f;
        public float categorySpacing = 10f;
        public float itemHeight = 120f;
        public float itemWidth = 120f;
        public RectOffset padding;

        private Canvas paletteCanvas;
        private GameObject palettePanel;
        private ScrollRect scrollRect;
        private Transform contentContainer;
        private SpriteRuntimeGraph graph;
        private bool isVisible = true;

        // Node type categories with sample data
        private List<NodeCategory> nodeCategories = new List<NodeCategory>();
        
        // Selected node type for creation
        private NodeTypeData selectedNodeType;
        
        // Events
        public System.Action<NodeTypeData> OnNodeTypeSelected;

        private void Awake()
        {
            padding = new RectOffset(15, 15, 15, 15);
            InitializeNodeCategories();
        }

        public void Initialize(SpriteRuntimeGraph graph)
        {
            this.graph = graph;
            CreatePaletteUI();
            SetVisible(true);
        }

        private void InitializeNodeCategories()
        {
            nodeCategories.Clear();
            
            // Get all engine parts from catalog
            var allParts = EnginePartCatalog.GetAllEngineParts();
            
            // Group parts by category
            var categoryGroups = new Dictionary<string, List<EnginePartNodeData>>();
            foreach (var part in allParts)
            {
                if (!categoryGroups.ContainsKey(part.category))
                {
                    categoryGroups[part.category] = new List<EnginePartNodeData>();
                }
                categoryGroups[part.category].Add(part);
            }
            
            // Create node categories for each engine part category
            foreach (var categoryGroup in categoryGroups)
            {
                var nodeCategory = new NodeCategory
                {
                    categoryName = categoryGroup.Key,
                    categoryColor = categoryGroup.Value.First().color
                };
                
                // Convert engine parts to node types
                foreach (var part in categoryGroup.Value)
                {
                    // Determine engine type from part name for consistent channel assignment
                    var engineType = DetermineEngineTypeFromName(part.name);
                    
                    var nodeType = new NodeTypeData
                    {
                        name = part.name,
                        category = part.category,
                        color = part.color,
                        description = part.description,
                        // Default MIDI values for compatibility
                        note = UnityEngine.Random.Range(36, 84),
                        velocity = UnityEngine.Random.Range(60, 100),
                        channel = GetEngineTypeChannel(engineType), // Each engine category uses its own channel
                        duration = 0.08f,
                        // Generate procedural icon
                        icon = EnginePartIconGenerator.GenerateIconForPart(part)
                    };
                    
                    nodeCategory.nodeTypes.Add(nodeType);
                }
                
                nodeCategories.Add(nodeCategory);
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
                SpriteNode.EngineType.MainEngine => 0,      // Channel 0 for MainEngine
                SpriteNode.EngineType.Thruster => 1,        // Channel 1 for Thruster
                SpriteNode.EngineType.RetroEngine => 2,     // Channel 2 for RetroEngine
                SpriteNode.EngineType.StabilityEngine => 3, // Channel 3 for StabilityEngine
                _ => 0
            };
        }

        private void CreatePaletteUI()
        {
            // Create screen space canvas for palette
            var canvasGO = new GameObject("NodePaletteCanvas");
            canvasGO.transform.SetParent(transform);
            
            paletteCanvas = canvasGO.AddComponent<Canvas>();
            paletteCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            paletteCanvas.sortingOrder = 998; // Below toolbar, above other UI
            
            // Add Canvas Scaler for consistent sizing
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            // Add GraphicRaycaster for interaction
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Create palette panel
            CreatePalettePanel();
            
            // Create scroll view
            CreateScrollView();
            
            // Populate with node types
            PopulateNodeTypes();
        }

        private void CreatePalettePanel()
        {
            palettePanel = new GameObject("NodePalettePanel");
            palettePanel.transform.SetParent(paletteCanvas.transform, false);
            
            // Setup RectTransform for left-side docking
            var rectTransform = palettePanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0.5f); // Left side, center vertically
            rectTransform.anchorMax = new Vector2(0, 0.5f);
            rectTransform.pivot = new Vector2(0, 0.5f); // Pivot at left edge
            rectTransform.anchoredPosition = screenOffset; // Offset from left edge
            rectTransform.sizeDelta = paletteSize;
            
            // Add background image
            var backgroundImage = palettePanel.AddComponent<Image>();
            backgroundImage.color = backgroundColor;
            
            // Add header
            CreatePaletteHeader();
        }

        private void CreatePaletteHeader()
        {
            var headerGO = new GameObject("PaletteHeader");
            headerGO.transform.SetParent(palettePanel.transform, false);
            
            var headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 40);
            
            //var headerBg = headerGO.AddComponent<Image>();
            //headerBg.color = categoryHeaderColor;
            
            var headerText = headerGO.AddComponent<Text>();
            headerText.text = "Node Types";
            headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headerText.fontSize = 18;
            headerText.color = Color.white;
            headerText.alignment = TextAnchor.MiddleCenter;
            headerText.fontStyle = FontStyle.Bold;
        }

        private void CreateScrollView()
        {
            var scrollViewGO = new GameObject("ScrollView");
            scrollViewGO.transform.SetParent(palettePanel.transform, false);
            
            var scrollViewRect = scrollViewGO.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = Vector2.zero;
            scrollViewRect.anchorMax = Vector2.one;
            scrollViewRect.offsetMin = new Vector2(0, 0);
            scrollViewRect.offsetMax = new Vector2(0, -40); // Account for header
            
            scrollRect = scrollViewGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            
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
            viewportImage.color = Color.gray;
            
            scrollRect.viewport = viewportRect;
            
            // Create content container
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            
            // Add ContentSizeFitter for dynamic sizing
            var contentSizeFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Add VerticalLayoutGroup for automatic arrangement
            var verticalLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            verticalLayout.childControlHeight = false;
            verticalLayout.childControlWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.spacing = categorySpacing;
            verticalLayout.padding = padding;
            
            scrollRect.content = contentRect;
            contentContainer = contentGO.transform;
        }

        private void PopulateNodeTypes()
        {
            foreach (var category in nodeCategories)
            {
                CreateCategorySection(category);
            }
        }

        private void CreateCategorySection(NodeCategory category)
        {
            // Create category header
            var categoryHeaderGO = new GameObject($"CategoryHeader_{category.categoryName}");
            categoryHeaderGO.transform.SetParent(contentContainer, false);
            
            var headerRect = categoryHeaderGO.AddComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(0, 30);
            
            //var headerBg = categoryHeaderGO.AddComponent<Image>();
            //headerBg.color = category.categoryColor;
            
            var headerText = categoryHeaderGO.AddComponent<Text>();
            headerText.text = category.categoryName.ToUpper();
            headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headerText.fontSize = 14;
            headerText.color = Color.white;
            headerText.alignment = TextAnchor.MiddleCenter;
            headerText.fontStyle = FontStyle.Bold;
            
            var headerLayout = categoryHeaderGO.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = 30;
            
            // Create grid container for node type items
            var gridContainerGO = new GameObject($"Grid_{category.categoryName}");
            gridContainerGO.transform.SetParent(contentContainer, false);
            
            var gridRect = gridContainerGO.AddComponent<RectTransform>();
            gridRect.sizeDelta = Vector2.zero;
            
            var gridLayout = gridContainerGO.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(itemWidth, itemHeight);
            gridLayout.spacing = new Vector2(itemSpacing, itemSpacing);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = columnsPerRow;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            
            var gridLayoutElement = gridContainerGO.AddComponent<LayoutElement>();
            int rowCount = Mathf.CeilToInt((float)category.nodeTypes.Count / columnsPerRow);
            float totalHeight = rowCount * itemHeight + (rowCount - 1) * itemSpacing;
            gridLayoutElement.preferredHeight = totalHeight;
            
            var gridContentSizeFitter = gridContainerGO.AddComponent<ContentSizeFitter>();
            gridContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Create node type buttons
            foreach (var nodeType in category.nodeTypes)
            {
                CreateNodeTypeButton(gridContainerGO.transform, nodeType);
            }
        }

        private void CreateNodeTypeButton(Transform parent, NodeTypeData nodeType)
        {
            var buttonGO = new GameObject($"NodeType_{nodeType.name}");
            buttonGO.transform.SetParent(parent, false);
            
            var button = buttonGO.AddComponent<Button>();
            button.onClick.AddListener(() => OnNodeTypeButtonClicked(nodeType));
            
            // Add button background (transparent for engine parts)
            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = Color.clear; // Remove background by making it transparent
            button.targetGraphic = buttonImage;
            
            // Create content layout
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(buttonGO.transform, false);
            
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(5, 5);
            contentRect.offsetMax = new Vector2(-5, -5);
            
            var contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.spacing = 2f;
            
            // Add icon image if available
            if (nodeType.icon != null)
            {
                var iconGO = new GameObject("Icon");
                iconGO.transform.SetParent(contentGO.transform, false);
                
                var iconImage = iconGO.AddComponent<Image>();
                iconImage.sprite = nodeType.icon;
                iconImage.preserveAspect = true;
                iconImage.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 80f); // Set size to match sprite size>()
                
                var iconLayoutElement = iconGO.AddComponent<LayoutElement>();
                iconLayoutElement.preferredHeight = 24f; // Reserve space for icon
                iconLayoutElement.flexibleHeight = 0;
            }
            
            // Add node name text
            var nameTextGO = new GameObject("NameText");
            nameTextGO.transform.SetParent(contentGO.transform, false);
            
            var nameText = nameTextGO.AddComponent<Text>();
            nameText.text = nodeType.name;
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 10; // Reduced font size to make room for icon
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.UpperCenter;
            nameText.fontStyle = FontStyle.Bold;
            
            // Add info text
            var infoTextGO = new GameObject("InfoText");
            infoTextGO.transform.SetParent(contentGO.transform, false);
            
            var infoText = infoTextGO.AddComponent<Text>();
            infoText.text = $"Note: {nodeType.note}\nCh: {nodeType.channel}";
            infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            infoText.fontSize = 8;
            infoText.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
            infoText.alignment = TextAnchor.LowerCenter;
        }

        private void OnNodeTypeButtonClicked(NodeTypeData nodeType)
        {
            selectedNodeType = nodeType;
            OnNodeTypeSelected?.Invoke(nodeType);
            
            Debug.Log($"Selected node type: {nodeType.name} (Note: {nodeType.note}, Channel: {nodeType.channel})");
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;
            if (paletteCanvas != null)
                paletteCanvas.gameObject.SetActive(visible);
        }

        public void ToggleVisibility()
        {
            SetVisible(!isVisible);
        }

        public bool IsVisible => isVisible;
        public NodeTypeData SelectedNodeType => selectedNodeType;
        
        public void RefreshNodeTypes()
        {
            // Clear existing content
            if (contentContainer != null)
            {
                foreach (Transform child in contentContainer)
                {
                    DestroyImmediate(child.gameObject);
                }
                
                // Repopulate
                PopulateNodeTypes();
            }
        }

        private void OnDestroy()
        {
            // Cleanup
        }
    }
}