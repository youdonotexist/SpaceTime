using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

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
        public float itemHeight = 60f;
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
            // Create sample node categories with random MIDI values and colors
            
            // Parts category
            var partsCategory = new NodeCategory
            {
                categoryName = "Parts",
                categoryColor = new Color(0.4f, 0.6f, 0.8f, 1f)
            };
            
            partsCategory.nodeTypes.AddRange(new[]
            {
                new NodeTypeData
                {
                    name = "Kick",
                    category = "Parts",
                    color = new Color(0.8f, 0.3f, 0.3f, 1f),
                    note = 36, // C2 - typical kick drum
                    velocity = UnityEngine.Random.Range(80, 127),
                    channel = 10, // Drum channel
                    duration = 0.04f,
                    description = "Bass drum sound"
                },
                new NodeTypeData
                {
                    name = "Snare",
                    category = "Parts",
                    color = new Color(0.9f, 0.6f, 0.2f, 1f),
                    note = 38, // D2 - typical snare
                    velocity = UnityEngine.Random.Range(70, 110),
                    channel = 10,
                    duration = 0.04f,
                    description = "Snare drum sound"
                },
                new NodeTypeData
                {
                    name = "Hi-Hat",
                    category = "Parts",
                    color = new Color(0.7f, 0.7f, 0.3f, 1f),
                    note = 42, // F#2 - closed hi-hat
                    velocity = UnityEngine.Random.Range(50, 90),
                    channel = 10,
                    duration = 0.04f,
                    description = "Hi-hat cymbal"
                },
                new NodeTypeData
                {
                    name = "Bass",
                    category = "Parts",
                    color = new Color(0.4f, 0.2f, 0.8f, 1f),
                    note = UnityEngine.Random.Range(24, 48),
                    velocity = UnityEngine.Random.Range(80, 120),
                    channel = 3,
                    duration = 0.04f,
                    description = "Bass line element"
                }
            });

            // Elements category
            var elementsCategory = new NodeCategory
            {
                categoryName = "Elements",
                categoryColor = new Color(0.6f, 0.8f, 0.4f, 1f)
            };
            
            elementsCategory.nodeTypes.AddRange(new[]
            {
                new NodeTypeData
                {
                    name = "Melody",
                    category = "Elements",
                    color = new Color(0.2f, 0.8f, 0.6f, 1f),
                    note = UnityEngine.Random.Range(60, 84), // C4 to C6
                    velocity = UnityEngine.Random.Range(60, 100),
                    channel = 1,
                    duration = 0.04f,
                    description = "Melodic phrase"
                },
                new NodeTypeData
                {
                    name = "Chord",
                    category = "Elements",
                    color = new Color(0.6f, 0.4f, 0.9f, 1f),
                    note = UnityEngine.Random.Range(48, 72), // C3 to C5
                    velocity = UnityEngine.Random.Range(70, 110),
                    channel = 2,
                    duration = 0.04f,
                    description = "Harmonic chord"
                },
                new NodeTypeData
                {
                    name = "Arp",
                    category = "Elements",
                    color = new Color(0.9f, 0.4f, 0.7f, 1f),
                    note = UnityEngine.Random.Range(60, 96),
                    velocity = UnityEngine.Random.Range(50, 90),
                    channel = 4,
                    duration = 0.04f,
                    description = "Arpeggio pattern"
                },
                new NodeTypeData
                {
                    name = "Lead",
                    category = "Elements",
                    color = new Color(1f, 0.7f, 0.2f, 1f),
                    note = UnityEngine.Random.Range(72, 108),
                    velocity = UnityEngine.Random.Range(90, 127),
                    channel = 5,
                    duration = 0.04f,
                    description = "Lead synthesizer"
                }
            });

            // Resources category
            var resourcesCategory = new NodeCategory
            {
                categoryName = "Resources",
                categoryColor = new Color(0.8f, 0.6f, 0.4f, 1f)
            };
            
            resourcesCategory.nodeTypes.AddRange(new[]
            {
                new NodeTypeData
                {
                    name = "Pad",
                    category = "Resources",
                    color = new Color(0.5f, 0.6f, 0.9f, 1f),
                    note = UnityEngine.Random.Range(36, 60),
                    velocity = UnityEngine.Random.Range(40, 80),
                    channel = 6,
                    duration = 0.04f,
                    description = "Atmospheric pad"
                },
                new NodeTypeData
                {
                    name = "FX",
                    category = "Resources",
                    color = new Color(0.8f, 0.8f, 0.5f, 1f),
                    note = UnityEngine.Random.Range(96, 120),
                    velocity = UnityEngine.Random.Range(30, 70),
                    channel = 7,
                    duration = 0.04f,
                    description = "Sound effect"
                },
                new NodeTypeData
                {
                    name = "Ambient",
                    category = "Resources",
                    color = new Color(0.6f, 0.9f, 0.7f, 1f),
                    note = UnityEngine.Random.Range(24, 48),
                    velocity = UnityEngine.Random.Range(20, 60),
                    channel = 8,
                    duration = 0.04f,
                    description = "Ambient texture"
                },
                new NodeTypeData
                {
                    name = "Percussion",
                    category = "Resources",
                    color = new Color(0.9f, 0.5f, 0.4f, 1f),
                    note = UnityEngine.Random.Range(60, 84),
                    velocity = UnityEngine.Random.Range(60, 100),
                    channel = 10,
                    duration = 0.04f,
                    description = "Auxiliary percussion"
                }
            });

            // Engines category
            var enginesCategory = new NodeCategory
            {
                categoryName = "Engines",
                categoryColor = new Color(0.8f, 0.4f, 0.2f, 1f)
            };
            
            enginesCategory.nodeTypes.AddRange(new[]
            {
                new NodeTypeData
                {
                    name = "Main Engine",
                    category = "Engines",
                    color = new Color(0.2f, 0.4f, 0.8f, 1f),
                    note = 60, // C4
                    velocity = UnityEngine.Random.Range(100, 127),
                    channel = 9,
                    duration = 0.04f,
                    description = "Primary propulsion system"
                },
                new NodeTypeData
                {
                    name = "Thruster",
                    category = "Engines",
                    color = new Color(0.8f, 0.4f, 0.2f, 1f),
                    note = 72, // C5
                    velocity = UnityEngine.Random.Range(80, 110),
                    channel = 9,
                    duration = 0.04f,
                    description = "Maneuvering thruster"
                },
                new NodeTypeData
                {
                    name = "Retro Engine",
                    category = "Engines",
                    color = new Color(0.6f, 0.2f, 0.8f, 1f),
                    note = 48, // C3
                    velocity = UnityEngine.Random.Range(90, 120),
                    channel = 9,
                    duration = 0.04f,
                    description = "Reverse thrust system"
                },
                new NodeTypeData
                {
                    name = "Stability Engine",
                    category = "Engines",
                    color = new Color(0.2f, 0.8f, 0.4f, 1f),
                    note = 84, // C6
                    velocity = UnityEngine.Random.Range(60, 90),
                    channel = 9,
                    duration = 0.04f,
                    description = "Attitude control system"
                }
            });

            nodeCategories.Add(partsCategory);
            nodeCategories.Add(elementsCategory);
            nodeCategories.Add(resourcesCategory);
            nodeCategories.Add(enginesCategory);
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
            
            // Add button background
            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = nodeType.color;
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
            
            // Add node name text
            var nameTextGO = new GameObject("NameText");
            nameTextGO.transform.SetParent(contentGO.transform, false);
            
            var nameText = nameTextGO.AddComponent<Text>();
            nameText.text = nodeType.name;
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 12;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.fontStyle = FontStyle.Bold;
            
            // Add info text
            var infoTextGO = new GameObject("InfoText");
            infoTextGO.transform.SetParent(contentGO.transform, false);
            
            var infoText = infoTextGO.AddComponent<Text>();
            infoText.text = $"Note: {nodeType.note}\nCh: {nodeType.channel}";
            infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            infoText.fontSize = 8;
            infoText.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
            infoText.alignment = TextAnchor.MiddleCenter;
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