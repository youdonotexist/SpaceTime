using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Commonwealth.Script.Ship.Resources
{
    public class ResourceUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform resourceContainer;
        [SerializeField] private GameObject resourceItemPrefab;
        [SerializeField] private Canvas resourceCanvas;
        
        [Header("Resource Manager")]
        [SerializeField] private ResourceManager resourceManager;
        
        private Dictionary<ResourceType, ResourceUIItem> resourceItems = new Dictionary<ResourceType, ResourceUIItem>();
        
        [System.Serializable]
        public class ResourceUIItem
        {
            public GameObject itemGameObject;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI amountText;
            public TextMeshProUGUI statusText;
            public Slider amountSlider;
            public Image statusIndicator;
        }
        
        private void Start()
        {
            // Find ResourceManager if not assigned
            if (resourceManager == null)
            {
                resourceManager = FindObjectOfType<ResourceManager>();
                if (resourceManager == null)
                {
                    Debug.LogWarning("ResourceManager not found. Creating one...");
                    var resourceManagerGO = new GameObject("ResourceManager");
                    resourceManager = resourceManagerGO.AddComponent<ResourceManager>();
                }
            }
            
            SetupUI();
            SubscribeToEvents();
            UpdateAllResourceUI();
        }
        
        private void SetupUI()
        {
            // Create canvas if not assigned
            if (resourceCanvas == null)
            {
                CreateResourceCanvas();
            }
            
            // Create container if not assigned
            if (resourceContainer == null)
            {
                CreateResourceContainer();
            }
            
            // Create UI items for each resource
            foreach (var resource in resourceManager.AllResources)
            {
                CreateResourceUIItem(resource);
            }
        }
        
        private void CreateResourceCanvas()
        {
            var canvasGO = new GameObject("Resource UI Canvas");
            canvasGO.transform.SetParent(transform);
            resourceCanvas = canvasGO.AddComponent<Canvas>();
            resourceCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            resourceCanvas.sortingOrder = 10;
            
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        private void CreateResourceContainer()
        {
            var containerGO = new GameObject("Resource Container");
            containerGO.transform.SetParent(resourceCanvas.transform);
            resourceContainer = containerGO.transform;
            
            var rectTransform = containerGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.anchoredPosition = new Vector2(-300, -50);
            rectTransform.sizeDelta = new Vector2(280, 600);
            
            // Add background panel
            var image = containerGO.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            // Add vertical layout
            var layoutGroup = containerGO.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 5f;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
        }
        
        private void CreateResourceUIItem(Resource resource)
        {
            var itemGO = new GameObject($"Resource_{resource.resourceType}");
            itemGO.transform.SetParent(resourceContainer);
            
            var rectTransform = itemGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(260, 80);
            
            // Background
            var bg = itemGO.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            
            var uiItem = new ResourceUIItem();
            uiItem.itemGameObject = itemGO;
            
            // Resource name
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(itemGO.transform);
            uiItem.nameText = nameGO.AddComponent<TextMeshProUGUI>();
            var nameRect = uiItem.nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0, 0.6f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.anchoredPosition = Vector2.zero;
            nameRect.sizeDelta = Vector2.zero;
            uiItem.nameText.text = resource.DisplayName;
            uiItem.nameText.fontSize = 14;
            uiItem.nameText.fontStyle = FontStyles.Bold;
            uiItem.nameText.color = Color.white;
            uiItem.nameText.alignment = TextAlignmentOptions.Left;
            
            // Amount text
            var amountGO = new GameObject("Amount");
            amountGO.transform.SetParent(itemGO.transform);
            uiItem.amountText = amountGO.AddComponent<TextMeshProUGUI>();
            var amountRect = uiItem.amountText.rectTransform;
            amountRect.anchorMin = new Vector2(0, 0.3f);
            amountRect.anchorMax = new Vector2(0.7f, 0.6f);
            amountRect.anchoredPosition = Vector2.zero;
            amountRect.sizeDelta = Vector2.zero;
            uiItem.amountText.fontSize = 12;
            uiItem.amountText.color = Color.white;
            uiItem.amountText.alignment = TextAlignmentOptions.Left;
            
            // Status text
            var statusGO = new GameObject("Status");
            statusGO.transform.SetParent(itemGO.transform);
            uiItem.statusText = statusGO.AddComponent<TextMeshProUGUI>();
            var statusRect = uiItem.statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0.7f, 0.3f);
            statusRect.anchorMax = new Vector2(1, 0.6f);
            statusRect.anchoredPosition = Vector2.zero;
            statusRect.sizeDelta = Vector2.zero;
            uiItem.statusText.fontSize = 10;
            uiItem.statusText.alignment = TextAlignmentOptions.Right;
            
            // Progress bar
            var sliderGO = new GameObject("Slider");
            sliderGO.transform.SetParent(itemGO.transform);
            uiItem.amountSlider = sliderGO.AddComponent<Slider>();
            var sliderRect = uiItem.amountSlider.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0);
            sliderRect.anchorMax = new Vector2(1, 0.3f);
            sliderRect.anchoredPosition = Vector2.zero;
            sliderRect.sizeDelta = Vector2.zero;
            
            // Slider background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(sliderGO.transform);
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            var bgRect = bgImage.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            
            // Slider fill
            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform);
            var fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;
            fillAreaRect.anchoredPosition = Vector2.zero;
            
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform);
            var fillImage = fillGO.AddComponent<Image>();
            var fillRect = fillImage.rectTransform;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            
            uiItem.amountSlider.fillRect = fillRect;
            uiItem.amountSlider.targetGraphic = fillImage;
            uiItem.statusIndicator = fillImage;
            
            resourceItems[resource.resourceType] = uiItem;
        }
        
        private void SubscribeToEvents()
        {
            if (resourceManager != null)
            {
                resourceManager.OnResourceChanged += UpdateResourceUI;
                resourceManager.OnResourceStateChanged += OnResourceStateChanged;
            }
        }
        
        private void UpdateResourceUI(Resource resource)
        {
            if (resourceItems.TryGetValue(resource.resourceType, out var uiItem))
            {
                uiItem.amountText.text = $"{resource.currentAmount:F0}/{resource.maxCapacity:F0} {resource.Unit}";
                uiItem.amountSlider.value = resource.FillPercentage;
                
                // Update colors based on state
                Color statusColor = resource.CurrentState switch
                {
                    ResourceState.Good => Color.green,
                    ResourceState.Warning => Color.yellow,
                    ResourceState.Critical => Color.red,
                    _ => Color.white
                };
                
                uiItem.statusText.text = resource.CurrentState.ToString();
                uiItem.statusText.color = statusColor;
                uiItem.statusIndicator.color = statusColor;
            }
        }
        
        private void OnResourceStateChanged(Resource resource, ResourceState newState)
        {
            Debug.Log($"Resource {resource.DisplayName} state changed to: {newState}");
        }
        
        private void UpdateAllResourceUI()
        {
            if (resourceManager != null)
            {
                foreach (var resource in resourceManager.AllResources)
                {
                    UpdateResourceUI(resource);
                }
            }
        }
        
        private void OnDestroy()
        {
            if (resourceManager != null)
            {
                resourceManager.OnResourceChanged -= UpdateResourceUI;
                resourceManager.OnResourceStateChanged -= OnResourceStateChanged;
            }
        }
    }
}