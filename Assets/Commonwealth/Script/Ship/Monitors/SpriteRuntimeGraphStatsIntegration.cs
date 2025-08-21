using UnityEngine;
using UnityEngine.UI;

namespace Commonwealth.Script.Ship.Monitors
{
    public class SpriteRuntimeGraphStatsIntegration : MonoBehaviour
    {
        [Header("Integration Settings")]
        [SerializeField] private Canvas statsCanvas;
        [SerializeField] private GameObject statsUIPrefab;
        [SerializeField] private RectTransform statsUIParent;
        
        [Header("Positioning")]
        [SerializeField] private Vector2 anchorMin = new Vector2(1f, 1f);
        [SerializeField] private Vector2 anchorMax = new Vector2(1f, 1f);
        [SerializeField] private Vector2 anchoredPosition = new Vector2(-20f, -20f);
        [SerializeField] private Vector2 sizeDelta = new Vector2(300f, 400f);
        
        private ShipStatsUI statsUI;
        private ShipStatsManager statsManager;
        private GameObject statsUIInstance;
        
        void Start()
        {
            InitializeStatsSystem();
        }
        
        private void InitializeStatsSystem()
        {
            // Find or create stats manager
            statsManager = FindObjectOfType<ShipStatsManager>();
            if (statsManager == null)
            {
                GameObject managerObj = new GameObject("ShipStatsManager");
                statsManager = managerObj.AddComponent<ShipStatsManager>();
                Debug.Log("Created ShipStatsManager");
            }
            
            // Create stats UI
            CreateStatsUI();
            
            // Setup canvas if not provided
            if (statsCanvas == null)
            {
                SetupCanvas();
            }
        }
        
        private void CreateStatsUI()
        {
            if (statsUIPrefab != null)
            {
                // Use provided prefab
                statsUIInstance = Instantiate(statsUIPrefab, GetUIParent());
                statsUI = statsUIInstance.GetComponent<ShipStatsUI>();
            }
            else
            {
                // Create UI programmatically
                CreateStatsUIProgrammatically();
            }
            
            if (statsUI != null)
            {
                ConfigureStatsUI();
            }
        }
        
        private void CreateStatsUIProgrammatically()
        {
            // Create main UI object
            statsUIInstance = new GameObject("ShipStatsUI");
            statsUIInstance.transform.SetParent(GetUIParent());
            
            // Add RectTransform and configure
            RectTransform rectTransform = statsUIInstance.AddComponent<RectTransform>();
            ConfigureRectTransform(rectTransform);
            
            // Add background panel
            Image backgroundPanel = statsUIInstance.AddComponent<Image>();
            backgroundPanel.color = new Color(0, 0, 0, 0.8f);
            
            // Create header
            GameObject header = CreateHeader();
            
            // Create scroll view for stats
            GameObject scrollView = CreateScrollView();
            
            // Create stat item prefab if not provided
            GameObject statItemPrefab = CreateStatItemPrefab();
            
            // Add ShipStatsUI component
            statsUI = statsUIInstance.AddComponent<ShipStatsUI>();
            
            // Configure references using reflection or public fields
            ConfigureStatsUIReferences(scrollView, statItemPrefab, header);
        }
        
        private GameObject CreateHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(statsUIInstance.transform);
            
            RectTransform headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.anchoredPosition = new Vector2(0, -15);
            headerRect.sizeDelta = new Vector2(0, 30);
            
            // Add text component
            var headerText = header.AddComponent<UnityEngine.UI.Text>();
            headerText.text = "SHIP SYSTEMS";
            headerText.fontSize = 14;
            headerText.fontStyle = FontStyle.Bold;
            headerText.color = Color.white;
            headerText.alignment = TextAnchor.MiddleCenter;
            
            // Try to use TextMeshPro if available
            try
            {
                var tmpText = header.AddComponent<TMPro.TextMeshProUGUI>();
                tmpText.text = "SHIP SYSTEMS";
                tmpText.fontSize = 14;
                tmpText.fontStyle = TMPro.FontStyles.Bold;
                tmpText.color = Color.white;
                tmpText.alignment = TMPro.TextAlignmentOptions.Center;
                DestroyImmediate(headerText); // Remove regular text component
            }
            catch
            {
                // TextMeshPro not available, use regular text
            }
            
            return header;
        }
        
        private GameObject CreateScrollView()
        {
            GameObject scrollView = new GameObject("StatsScrollView");
            scrollView.transform.SetParent(statsUIInstance.transform);
            
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(5, 5);
            scrollRect.offsetMax = new Vector2(-5, -40); // Leave space for header
            
            // Add ScrollRect component
            ScrollRect scrollComponent = scrollView.AddComponent<ScrollRect>();
            scrollComponent.horizontal = false;
            scrollComponent.vertical = true;
            
            // Create viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform);
            
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;
            
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            
            // Create content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform);
            
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            contentRect.anchoredPosition = Vector2.zero;
            
            // Add VerticalLayoutGroup
            VerticalLayoutGroup layoutGroup = content.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 2;
            layoutGroup.padding = new RectOffset(5, 5, 5, 5);
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = false;
            
            // Add ContentSizeFitter
            ContentSizeFitter sizeFitter = content.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Connect scroll rect
            scrollComponent.viewport = viewportRect;
            scrollComponent.content = contentRect;
            
            return scrollView;
        }
        
        private GameObject CreateStatItemPrefab()
        {
            GameObject prefab = new GameObject("StatItemPrefab");
            
            RectTransform rect = prefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280, 25);
            
            // Background
            Image bg = prefab.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // Add ShipStatUIItem component
            prefab.AddComponent<ShipStatUIItem>();
            
            // Create text elements
            GameObject nameText = CreateTextElement("StatName", prefab.transform);
            ConfigureTextElement(nameText, new Vector2(0, 0.5f), new Vector2(0.6f, 0.5f), 
                                new Vector2(5, 0), "Stat Name", TextAnchor.MiddleLeft);
            
            GameObject valueText = CreateTextElement("StatValue", prefab.transform);
            ConfigureTextElement(valueText, new Vector2(0.6f, 0.5f), new Vector2(0.9f, 0.5f), 
                                new Vector2(0, 0), "100", TextAnchor.MiddleRight);
            
            GameObject unitText = CreateTextElement("StatUnit", prefab.transform);
            ConfigureTextElement(unitText, new Vector2(0.9f, 0.5f), new Vector2(1, 0.5f), 
                                new Vector2(-5, 0), "(%)", TextAnchor.MiddleLeft);
            
            return prefab;
        }
        
        private GameObject CreateTextElement(string name, Transform parent)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent);
            return textObj;
        }
        
        private void ConfigureTextElement(GameObject textObj, Vector2 anchorMin, Vector2 anchorMax, 
                                        Vector2 anchoredPos, string text, TextAnchor alignment)
        {
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = Vector2.zero;
            
            var textComponent = textObj.AddComponent<UnityEngine.UI.Text>();
            textComponent.text = text;
            textComponent.fontSize = 10;
            textComponent.color = Color.white;
            textComponent.alignment = alignment;
        }
        
        private void ConfigureStatsUIReferences(GameObject scrollView, GameObject statItemPrefab, GameObject header)
        {
            // This would need reflection or public fields to set private serialized fields
            // For now, we'll use the component as-is and let it find references
        }
        
        private void ConfigureStatsUI()
        {
            if (statsUI != null)
            {
                // Set initial configuration
                statsUI.SetMaxVisibleStats(8);
                statsUI.SetShowOnlyProblems(false);
                statsUI.SetAutoSort(true);
            }
        }
        
        private void ConfigureRectTransform(RectTransform rectTransform)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }
        
        private Transform GetUIParent()
        {
            if (statsUIParent != null)
                return statsUIParent;
            
            if (statsCanvas != null)
                return statsCanvas.transform;
            
            return transform;
        }
        
        private void SetupCanvas()
        {
            GameObject canvasObj = new GameObject("StatsCanvas");
            canvasObj.transform.SetParent(transform);
            
            statsCanvas = canvasObj.AddComponent<Canvas>();
            statsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            statsCanvas.sortingOrder = 100; // High sorting order to appear on top
            
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        public void ToggleStatsVisibility()
        {
            if (statsUI != null)
            {
                statsUI.ToggleVisibility();
            }
        }
        
        public void SetShowOnlyProblems(bool showOnly)
        {
            if (statsUI != null)
            {
                statsUI.SetShowOnlyProblems(showOnly);
            }
        }
    }
}