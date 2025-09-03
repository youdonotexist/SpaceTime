using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using Newtonsoft.Json;
using RuntimeGraph.Sprite;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// Floating tooltip that displays node information on mouse hover
    /// </summary>
    public class NodeInfoTooltip : MonoBehaviour
    {
        [System.Serializable]
        public class ShipPartData
        {
            public string name;
            public string category;
            public List<string> ship_stats_impact;
            public string part_flow;
        }

        [System.Serializable]
        public class ShipPartsData
        {
            public List<ShipPartData> parts;
        }

        [Header("UI Components")]
        [SerializeField] private Canvas tooltipCanvas;
        [SerializeField] private RectTransform tooltipPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI categoryText;
        [SerializeField] private TextMeshProUGUI flowText;
        [SerializeField] private TextMeshProUGUI statsHeaderText;
        [SerializeField] private TextMeshProUGUI statsListText;

        private static NodeInfoTooltip instance;
        private Camera uiCamera;
        private Dictionary<string, ShipPartData> shipPartsLookup = new Dictionary<string, ShipPartData>();
        private bool isVisible = false;

        public static NodeInfoTooltip Instance => instance;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            // Create UI if not already set up
            if (tooltipCanvas == null)
            {
                SetupUI();
            }

            uiCamera = Camera.main;
            if (uiCamera == null)
            {
                uiCamera = FindObjectOfType<Camera>();
            }

            LoadShipPartsData();
            Hide();
        }

        private void SetupUI()
        {
            // Create canvas
            GameObject canvasGO = new GameObject("NodeInfoTooltip Canvas");
            canvasGO.transform.SetParent(transform);
            tooltipCanvas = canvasGO.AddComponent<Canvas>();
            tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tooltipCanvas.sortingOrder = 1000; // Very high to appear above everything

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Create tooltip panel
            GameObject panelGO = new GameObject("Tooltip Panel");
            panelGO.transform.SetParent(canvasGO.transform);
            tooltipPanel = panelGO.AddComponent<RectTransform>();
            
            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // Dark semi-transparent background

            // Set panel size and position
            tooltipPanel.sizeDelta = new Vector2(300, 200);
            tooltipPanel.anchorMin = Vector2.zero;
            tooltipPanel.anchorMax = Vector2.zero;

            // Add border
            Outline outline = panelGO.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(1, -1);

            // Create title text
            CreateText("Title", tooltipPanel, new Vector2(0, 80), "Node Name", 16, FontStyles.Bold, out titleText);
            
            // Create type text
            CreateText("Type", tooltipPanel, new Vector2(0, 60), "Type: Ship Part", 12, FontStyles.Normal, out typeText);
            
            // Create category text
            CreateText("Category", tooltipPanel, new Vector2(0, 40), "Category: Power", 12, FontStyles.Normal, out categoryText);
            
            // Create flow text
            CreateText("Flow", tooltipPanel, new Vector2(0, 20), "Flow: Start", 12, FontStyles.Normal, out flowText);
            
            // Create stats header
            CreateText("StatsHeader", tooltipPanel, new Vector2(0, -5), "Ship Statistics Impact:", 12, FontStyles.Bold, out statsHeaderText);
            
            // Create stats list
            CreateText("StatsList", tooltipPanel, new Vector2(0, -40), "• Reactor Output\n• Energy Reserve Hours", 11, FontStyles.Normal, out statsListText);
            statsListText.rectTransform.sizeDelta = new Vector2(280, 100);
        }

        private void CreateText(string name, RectTransform parent, Vector2 position, string text, int fontSize, FontStyles fontStyle, out TextMeshProUGUI textComponent)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent);
            textComponent = textGO.AddComponent<TextMeshProUGUI>();
            
            RectTransform textRect = textComponent.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = position;
            textRect.sizeDelta = new Vector2(280, 20);

            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = fontStyle;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Left;
            textComponent.overflowMode = TextOverflowModes.Overflow;
        }

        private void LoadShipPartsData()
        {
            try
            {
                string jsonPath = Path.Combine(Application.dataPath, "ship_parts_block_layouts.json");
                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    var data = JsonConvert.DeserializeObject<ShipPartsData>(jsonContent);
                    
                    if (data?.parts != null)
                    {
                        shipPartsLookup.Clear();
                        foreach (var part in data.parts)
                        {
                            if (!string.IsNullOrEmpty(part.name))
                            {
                                shipPartsLookup[part.name] = part;
                            }
                        }
                        Debug.Log($"Loaded {shipPartsLookup.Count} ship parts for tooltip system");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load ship parts data for tooltip: {ex.Message}");
            }
        }

        public void ShowTooltip(SpriteNode node, Vector3 worldPosition)
        {
            if (node == null || tooltipPanel == null) return;

            var nodeData = node.NodeDataInstance;
            if (nodeData == null) return;

            // Update content
            string nodeName = nodeData.title;
            titleText.text = nodeName;

            bool isShipPart = node.IsShipPart();
            typeText.text = $"Type: {(isShipPart ? "Ship Part" : "Graph Node")}";

            if (isShipPart && shipPartsLookup.TryGetValue(nodeName, out var partData))
            {
                // Use data from JSON
                categoryText.text = $"Category: {FormatCategory(partData.category)}";
                flowText.text = $"Flow: {FormatPartFlow(partData.part_flow)}";
                
                if (partData.ship_stats_impact != null && partData.ship_stats_impact.Count > 0)
                {
                    statsHeaderText.text = "Ship Statistics Impact:";
                    statsListText.text = "• " + string.Join("\n• ", partData.ship_stats_impact);
                }
                else
                {
                    statsHeaderText.text = "Ship Statistics Impact:";
                    statsListText.text = "None (transforms/routes data)";
                }
            }
            else
            {
                // Fallback for non-ship parts or missing data
                categoryText.text = $"Category: {(isShipPart ? "Ship Part" : "Graph Node")}";
                flowText.text = "Flow: N/A";
                statsHeaderText.text = "Ship Statistics Impact:";
                statsListText.text = "N/A";
            }

            // Position tooltip above the node
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPosition);
            
            // Get screen dimensions
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            Vector2 tooltipSize = tooltipPanel.sizeDelta;
            
            // Position tooltip above the node with some padding
            float yOffset = tooltipSize.y / 2f + 50f; // Position above node with 50px padding
            Vector2 tooltipScreenPos = new Vector2(screenPoint.x, screenPoint.y + yOffset);
            
            // Keep tooltip on screen horizontally
            tooltipScreenPos.x = Mathf.Clamp(tooltipScreenPos.x, tooltipSize.x / 2f, screenWidth - tooltipSize.x / 2f);
            
            // If tooltip would go off the top of screen, position it below the node instead
            if (tooltipScreenPos.y + tooltipSize.y / 2f > screenHeight)
            {
                tooltipScreenPos.y = screenPoint.y - yOffset;
            }
            
            // Ensure tooltip doesn't go off bottom of screen
            tooltipScreenPos.y = Mathf.Max(tooltipScreenPos.y, tooltipSize.y / 2f);
            
            // Convert screen position to anchored position for the tooltip panel
            tooltipPanel.position = tooltipScreenPos;
            
            // Show tooltip
            tooltipCanvas.gameObject.SetActive(true);
            isVisible = true;
        }

        public void Hide()
        {
            if (tooltipCanvas != null)
            {
                tooltipCanvas.gameObject.SetActive(false);
            }
            isVisible = false;
        }

        private string FormatCategory(string category)
        {
            return category switch
            {
                "power" => "Power & Energy",
                "thermal" => "Thermal & Coolant", 
                "atmosphere" => "Atmosphere & Life Support",
                "structural" => "Structural & Hull",
                "propulsion" => "Propulsion & Maneuvering",
                "navigation" => "Navigation, Comms & Sensors",
                "data" => "Data, Control & Security",
                "manufacturing" => "Manufacturing, Inventory & Logistics",
                "defense" => "Defense & Shielding",
                _ => category
            };
        }

        private string FormatPartFlow(string partFlow)
        {
            return partFlow switch
            {
                "start" => "Start (Source)",
                "middle" => "Middle (Processor)",
                "end" => "End (Consumer)",
                _ => partFlow ?? "Unknown"
            };
        }

        public bool IsVisible => isVisible;

        private void Update()
        {
            // Hide tooltip if mouse moves away or ESC is pressed
            if (isVisible && (Input.GetKeyDown(KeyCode.Escape)))
            {
                Hide();
            }
        }
    }
}