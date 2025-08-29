using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.IO;

namespace RuntimeGraph.Sprite
{
    [System.Serializable]
    public class Instrument
    {
        public int BankNum;
        public int PresetNum;
        public string Name;
        public int BoundChannel;

        public override string ToString()
        {
            return Name;
        }
    }

    [System.Serializable]
    public class InstrumentList
    {
        public List<Instrument> List = new List<Instrument>();
    }

    /// <summary>
    /// Screen space UI for node configuration, docked on the right side of the screen
    /// </summary>
    public class SpriteNodeConfigUI : MonoBehaviour
    {
        [Header("UI Configuration")]
        public Vector2 panelSize = new Vector2(250, 400);
        public Vector2 screenOffset = new Vector2(-20, 0); // Offset from right edge
        public Color backgroundColor = new Color(0.2f, 0.2f, 0.3f, 0.9f);
        public Color fieldBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        
        [Header("Layout")]
        public float fieldSpacing = 5f;
        public float fieldHeight = 25f;
        public RectOffset padding;
        
        private Canvas configCanvas;
        private GameObject configPanel;
        private SpriteNode currentNode;
        private bool isVisible = false;
        
        // UI Elements
        private Toggle isStartToggle;
        private InputField durationField;
        private InputField noteField;
        private InputField channelField;
        private InputField rotationField;
        private Dropdown instrumentDropdown;
        
        // Instrument data
        private List<Instrument> availableInstruments = new List<Instrument>();
        
        private void Awake()
        {
            padding = new RectOffset(15, 15, 15, 15);
        }
        
        public void Initialize()
        {
            LoadInstruments();
            CreateScreenSpaceUI();
            SetVisible(false);
        }
        
        private void LoadInstruments()
        {
            try
            {
                string jsonPath = Path.Combine(Application.dataPath, "instruments.json");
                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    InstrumentList instrumentList = JsonUtility.FromJson<InstrumentList>(jsonContent);
                    
                    if (instrumentList != null && instrumentList.List != null)
                    {
                        // Filter to only use the 16 selected chiptune instruments
                        FilterToChiptuneInstruments(instrumentList);
                        Debug.Log($"Filtered to {availableInstruments.Count} chiptune instruments from JSON for UI");
                    }
                    else
                    {
                        Debug.LogWarning("instruments.json contains null data, using default instruments");
                        CreateDefaultInstruments();
                    }
                }
                else
                {
                    Debug.LogWarning("instruments.json file not found, using default instruments");
                    CreateDefaultInstruments();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load instruments.json: {ex.Message}");
                CreateDefaultInstruments();
            }
        }

        private void FilterToChiptuneInstruments(InstrumentList fullInstrumentList)
        {
            // Define the 16 chiptune instruments we want to use (same as sequencer)
            var selectedPresets = new System.Collections.Generic.Dictionary<int, (string Name, int Channel)>
            {
                { 1, ("100 Square", 0) },
                { 8, ("100 25% Pulse", 1) },
                { 60, ("048 Pulse 50%", 2) },
                { 9, ("100 12.5% Pulse", 3) },
                { 10, ("100 75% Pulse", 4) },
                { 11, ("100 PWM", 5) },
                { 12, ("100 Triangle", 6) },
                { 6, ("100 Saw Wave", 7) },
                { 13, ("100 Noise", 8) },
                { 4, ("100 Buzzy", 11) },
                { 14, ("100 Sub Bass", 12) },
                { 15, ("100 Lead", 13) },
                { 16, ("100 Arp", 14) },
                { 17, ("100 Pad", 15) }
            };

            // Drum sets (bank 128)
            var selectedDrumPresets = new System.Collections.Generic.Dictionary<int, (string Name, int Channel)>
            {
                { 0, ("Standard Drums", 9) },
                { 16, ("059 Drumkit", 10) }
            };

            availableInstruments = new System.Collections.Generic.List<Instrument>();

            // Find and add matching instruments from the full list
            foreach (var instrument in fullInstrumentList.List)
            {
                // Check regular instruments (bank 0)
                if (instrument.BankNum == 0 && selectedPresets.ContainsKey(instrument.PresetNum))
                {
                    var selected = selectedPresets[instrument.PresetNum];
                    availableInstruments.Add(new Instrument
                    {
                        BankNum = instrument.BankNum,
                        PresetNum = instrument.PresetNum,
                        Name = selected.Name,
                        BoundChannel = selected.Channel
                    });
                }
                // Check drum sets (bank 128)
                else if (instrument.BankNum == 128 && selectedDrumPresets.ContainsKey(instrument.PresetNum))
                {
                    var selected = selectedDrumPresets[instrument.PresetNum];
                    availableInstruments.Add(new Instrument
                    {
                        BankNum = instrument.BankNum,
                        PresetNum = instrument.PresetNum,
                        Name = selected.Name,
                        BoundChannel = selected.Channel
                    });
                }
            }

            // If we couldn't find all instruments in the JSON, fall back to defaults
            if (availableInstruments.Count < 16)
            {
                Debug.LogWarning($"Only found {availableInstruments.Count} of 16 expected chiptune instruments in JSON, using defaults");
                CreateDefaultInstruments();
            }
        }
        
        private void CreateDefaultInstruments()
        {
            // Create curated list of 16 chiptune instruments for MIDI channels 0-15 (same as sequencer)
            availableInstruments = new List<Instrument>
            {
                // Required chiptune waveforms
                new Instrument { BankNum = 0, PresetNum = 1, Name = "100 Square", BoundChannel = 0 },
                new Instrument { BankNum = 0, PresetNum = 8, Name = "100 25% Pulse", BoundChannel = 1 },
                new Instrument { BankNum = 0, PresetNum = 60, Name = "048 Pulse 50%", BoundChannel = 2 },
                new Instrument { BankNum = 0, PresetNum = 9, Name = "100 12.5% Pulse", BoundChannel = 3 },
                new Instrument { BankNum = 0, PresetNum = 10, Name = "100 75% Pulse", BoundChannel = 4 },
                new Instrument { BankNum = 0, PresetNum = 11, Name = "100 PWM", BoundChannel = 5 }, // PWM for chorusy motion
                new Instrument { BankNum = 0, PresetNum = 12, Name = "100 Triangle", BoundChannel = 6 },
                new Instrument { BankNum = 0, PresetNum = 6, Name = "100 Saw Wave", BoundChannel = 7 },
                new Instrument { BankNum = 0, PresetNum = 13, Name = "100 Noise", BoundChannel = 8 },
                
                // Drum sets
                new Instrument { BankNum = 128, PresetNum = 0, Name = "Standard Drums", BoundChannel = 9 },
                new Instrument { BankNum = 128, PresetNum = 16, Name = "059 Drumkit", BoundChannel = 10 },
                
                // Additional chiptune-aesthetic instruments
                new Instrument { BankNum = 0, PresetNum = 4, Name = "100 Buzzy", BoundChannel = 11 },
                new Instrument { BankNum = 0, PresetNum = 14, Name = "100 Sub Bass", BoundChannel = 12 },
                new Instrument { BankNum = 0, PresetNum = 15, Name = "100 Lead", BoundChannel = 13 },
                new Instrument { BankNum = 0, PresetNum = 16, Name = "100 Arp", BoundChannel = 14 },
                new Instrument { BankNum = 0, PresetNum = 17, Name = "100 Pad", BoundChannel = 15 }
            };
        }
        
        private void CreateScreenSpaceUI()
        {
            // Create screen space canvas for configuration UI
            var canvasGO = new GameObject("NodeConfigCanvas");
            canvasGO.transform.SetParent(transform);
            
            configCanvas = canvasGO.AddComponent<Canvas>();
            configCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            configCanvas.sortingOrder = 999; // Below toolbar but above other UI
            
            // Add Canvas Scaler for consistent sizing
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            // Add GraphicRaycaster for interaction
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Create configuration panel docked to right side
            CreateConfigPanel();
        }
        
        private void CreateConfigPanel()
        {
            configPanel = new GameObject("NodeConfigPanel");
            configPanel.transform.SetParent(configCanvas.transform, false);
            
            // Setup RectTransform for right-side docking
            var rectTransform = configPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1, 0.5f); // Right side, center vertically
            rectTransform.anchorMax = new Vector2(1, 0.5f);
            rectTransform.pivot = new Vector2(1, 0.5f); // Pivot at right edge
            rectTransform.anchoredPosition = screenOffset; // Offset from right edge
            rectTransform.sizeDelta = panelSize;
            
            // Add background image
            var backgroundImage = configPanel.AddComponent<Image>();
            backgroundImage.color = backgroundColor;
            
            // Add vertical layout group
            var layoutGroup = configPanel.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = padding;
            layoutGroup.spacing = fieldSpacing;
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandWidth = true;
            
            // Add content size fitter
            var contentSizeFitter = configPanel.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Create header
            CreateHeader();
            
            // Create configuration fields
            CreateConfigFields();
        }
        
        private void CreateHeader()
        {
            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(configPanel.transform, false);
            
            var headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(0, 30);
            
            var headerText = headerGO.AddComponent<Text>();
            headerText.text = "Node Configuration";
            headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headerText.fontSize = 16;
            headerText.color = Color.white;
            headerText.alignment = TextAnchor.MiddleCenter;
            headerText.fontStyle = FontStyle.Bold;
        }
        
        private void CreateConfigFields()
        {
            // IsStart toggle
            isStartToggle = CreateToggleField("Is Start", false);
            
            // Duration field
            durationField = CreateFloatField("Duration", 0.5f);
            
            // Note field
            noteField = CreateIntField("Note", 0);
            
            // Channel field
            channelField = CreateIntField("Channel", 1);
            
            // Rotation field
            rotationField = CreateFloatField("Rotation", 0f);
            
            // Instrument dropdown
            instrumentDropdown = CreateDropdownField("Instrument", 0);
            PopulateInstrumentDropdown();
        }
        
        private Toggle CreateToggleField(string label, bool initialValue)
        {
            var fieldGO = new GameObject($"Field_{label}");
            fieldGO.transform.SetParent(configPanel.transform, false);
            
            var fieldRect = fieldGO.AddComponent<RectTransform>();
            fieldRect.sizeDelta = new Vector2(0, fieldHeight);
            
            // Create horizontal layout
            var horizontalLayout = fieldGO.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.childControlWidth = true;
            horizontalLayout.childControlHeight = true;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childForceExpandHeight = false;
            horizontalLayout.spacing = 10f;
            
            // Create toggle
            var toggleGO = new GameObject("Toggle");
            toggleGO.transform.SetParent(fieldGO.transform, false);
            
            var toggleRect = toggleGO.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(20, 20);
            
            var toggle = toggleGO.AddComponent<Toggle>();
            toggle.isOn = initialValue;
            
            // Add toggle background
            var toggleBg = toggleGO.AddComponent<Image>();
            toggleBg.color = fieldBackgroundColor;
            toggle.targetGraphic = toggleBg;
            
            // Add layout element to ensure proper sizing
            var toggleLayout = toggleGO.AddComponent<LayoutElement>();
            toggleLayout.preferredWidth = 20f;
            toggleLayout.preferredHeight = 20f;
            
            // Create checkmark
            var checkmarkGO = new GameObject("Checkmark");
            checkmarkGO.transform.SetParent(toggleGO.transform, false);
            
            var checkmarkRect = checkmarkGO.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.offsetMin = Vector2.zero;
            checkmarkRect.offsetMax = Vector2.zero;
            
            var checkmarkImage = checkmarkGO.AddComponent<Image>();
            checkmarkImage.color = Color.white;
            checkmarkImage.sprite = CreateCheckmarkSprite();
            toggle.graphic = checkmarkImage;
            
            // Create label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(fieldGO.transform, false);
            
            var labelText = labelGO.AddComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 12;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            
            var layoutElement = labelGO.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1f;
            
            return toggle;
        }
        
        private InputField CreateFloatField(string label, float initialValue)
        {
            return CreateInputField(label, initialValue.ToString("F2"));
        }
        
        private InputField CreateIntField(string label, int initialValue)
        {
            return CreateInputField(label, initialValue.ToString());
        }
        
        private Dropdown CreateDropdownField(string label, int initialValue)
        {
            var fieldGO = new GameObject($"Field_{label}");
            fieldGO.transform.SetParent(configPanel.transform, false);
            
            var fieldRect = fieldGO.AddComponent<RectTransform>();
            fieldRect.sizeDelta = new Vector2(0, fieldHeight);
            
            // Create horizontal layout
            var horizontalLayout = fieldGO.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.childControlWidth = true;
            horizontalLayout.childControlHeight = true;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childForceExpandHeight = false;
            horizontalLayout.spacing = 10f;
            
            // Create label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(fieldGO.transform, false);
            
            var labelText = labelGO.AddComponent<Text>();
            labelText.text = $"{label}:";
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 12;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            
            var labelLayout = labelGO.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 80f;
            labelLayout.preferredHeight = fieldHeight;
            
            // Create dropdown
            var dropdownGO = new GameObject("Dropdown");
            dropdownGO.transform.SetParent(fieldGO.transform, false);
            
            var dropdownRect = dropdownGO.AddComponent<RectTransform>();
            dropdownRect.sizeDelta = new Vector2(0, fieldHeight);
            
            var dropdown = dropdownGO.AddComponent<Dropdown>();
            dropdown.value = initialValue;
            
            // Add background image for dropdown
            var dropdownImage = dropdownGO.AddComponent<Image>();
            dropdownImage.color = fieldBackgroundColor;
            dropdown.targetGraphic = dropdownImage;
            
            // Create label for dropdown (shows selected text)
            var dropdownLabelGO = new GameObject("Label");
            dropdownLabelGO.transform.SetParent(dropdownGO.transform, false);
            
            var dropdownLabelRect = dropdownLabelGO.AddComponent<RectTransform>();
            dropdownLabelRect.anchorMin = Vector2.zero;
            dropdownLabelRect.anchorMax = Vector2.one;
            dropdownLabelRect.offsetMin = new Vector2(10, 0);
            dropdownLabelRect.offsetMax = new Vector2(-25, 0);
            
            var dropdownLabelText = dropdownLabelGO.AddComponent<Text>();
            dropdownLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dropdownLabelText.fontSize = 12;
            dropdownLabelText.color = Color.white;
            dropdownLabelText.alignment = TextAnchor.MiddleLeft;
            
            dropdown.captionText = dropdownLabelText;
            
            // Create arrow
            var arrowGO = new GameObject("Arrow");
            arrowGO.transform.SetParent(dropdownGO.transform, false);
            
            var arrowRect = arrowGO.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.pivot = new Vector2(1, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-10, 0);
            arrowRect.sizeDelta = new Vector2(10, 10);
            
            var arrowImage = arrowGO.AddComponent<Image>();
            arrowImage.color = Color.white;
            arrowImage.sprite = CreateArrowSprite();
            
            // Create template (dropdown list)
            var templateGO = new GameObject("Template");
            templateGO.transform.SetParent(dropdownGO.transform, false);
            templateGO.SetActive(false);
            
            var templateRect = templateGO.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = new Vector2(0, 2);
            templateRect.sizeDelta = new Vector2(0, 150);
            
            // Add LayoutElement to ensure template has proper height
            var templateLayoutElement = templateGO.AddComponent<LayoutElement>();
            templateLayoutElement.preferredHeight = 150f;
            templateLayoutElement.flexibleHeight = 0f;
            
            var templateImage = templateGO.AddComponent<Image>();
            templateImage.color = fieldBackgroundColor;
            
            // Create scrollbar
            var scrollbarGO = new GameObject("Scrollbar");
            scrollbarGO.transform.SetParent(templateGO.transform, false);
            
            var scrollbarRect = scrollbarGO.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 1);
            scrollbarRect.anchoredPosition = new Vector2(0, 0);
            scrollbarRect.sizeDelta = new Vector2(20, 0);
            
            var scrollbar = scrollbarGO.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            
            // Create viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(templateGO.transform, false);
            
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-20, 0);
            
            var viewportMask = viewportGO.AddComponent<Mask>();
            viewportMask.showMaskGraphic = true;
            
            var viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = new Color(0.1f, 0.1f, 0.1f, 1f); // Solid background for proper masking
            
            // Add ScrollRect component for proper scrolling
            var scrollRect = templateGO.AddComponent<ScrollRect>();
            scrollRect.content = null; // Will be set after content creation
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.verticalScrollbar = scrollbar;
            
            // Create content
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);
            
            // Add ContentSizeFitter to automatically resize content based on children
            var contentSizeFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Add VerticalLayoutGroup to properly arrange dropdown items
            var verticalLayoutGroup = contentGO.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childControlHeight = true;
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.spacing = 0f;
            
            // Set ScrollRect content
            scrollRect.content = contentRect;
            
            // Create item
            var itemGO = new GameObject("Item");
            itemGO.transform.SetParent(contentGO.transform, false);
            
            var itemRect = itemGO.AddComponent<RectTransform>();
            itemRect.anchorMin = Vector2.zero;
            itemRect.anchorMax = Vector2.one;
            itemRect.offsetMin = Vector2.zero;
            itemRect.offsetMax = Vector2.zero;
            
            // Add LayoutElement to control item height
            var itemLayoutElement = itemGO.AddComponent<LayoutElement>();
            itemLayoutElement.preferredHeight = 20f;
            itemLayoutElement.flexibleHeight = 0f;
            
            var itemToggle = itemGO.AddComponent<Toggle>();
            itemToggle.isOn = false;
            
            var itemBg = itemGO.AddComponent<Image>();
            itemBg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            itemToggle.targetGraphic = itemBg;
            
            // Create item label
            var itemLabelGO = new GameObject("Item Label");
            itemLabelGO.transform.SetParent(itemGO.transform, false);
            
            var itemLabelRect = itemLabelGO.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(10, 1);
            itemLabelRect.offsetMax = new Vector2(-10, -2);
            
            var itemLabelText = itemLabelGO.AddComponent<Text>();
            itemLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemLabelText.fontSize = 12;
            itemLabelText.color = Color.white;
            itemLabelText.alignment = TextAnchor.MiddleLeft;
            
            dropdown.template = templateRect;
            dropdown.captionText = dropdownLabelText;
            dropdown.itemText = itemLabelText;
            
            var dropdownLayout = dropdownGO.AddComponent<LayoutElement>();
            dropdownLayout.flexibleWidth = 1f;
            dropdownLayout.preferredWidth = 120f;
            dropdownLayout.preferredHeight = fieldHeight;
            
            return dropdown;
        }
        
        private void PopulateInstrumentDropdown()
        {
            if (instrumentDropdown == null || availableInstruments == null) return;
            
            instrumentDropdown.ClearOptions();
            
            var options = new List<Dropdown.OptionData>();
            foreach (var instrument in availableInstruments)
            {
                options.Add(new Dropdown.OptionData(instrument.Name));
            }
            
            instrumentDropdown.AddOptions(options);
        }
        
        private InputField CreateInputField(string label, string initialValue)
        {
            var fieldGO = new GameObject($"Field_{label}");
            fieldGO.transform.SetParent(configPanel.transform, false);
            
            var fieldRect = fieldGO.AddComponent<RectTransform>();
            fieldRect.sizeDelta = new Vector2(0, fieldHeight);
            
            // Create horizontal layout
            var horizontalLayout = fieldGO.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.childControlWidth = true;
            horizontalLayout.childControlHeight = true;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childForceExpandHeight = false;
            horizontalLayout.spacing = 10f;
            
            // Create label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(fieldGO.transform, false);
            
            var labelText = labelGO.AddComponent<Text>();
            labelText.text = $"{label}:";
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 12;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            
            var labelLayout = labelGO.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 80f;
            labelLayout.preferredHeight = fieldHeight;
            
            // Create input field
            var inputGO = new GameObject("InputField");
            inputGO.transform.SetParent(fieldGO.transform, false);
            
            var inputRect = inputGO.AddComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(0, fieldHeight);
            
            var inputField = inputGO.AddComponent<InputField>();
            inputField.text = initialValue;
            
            // Add background image for input field
            var inputImage = inputGO.AddComponent<Image>();
            inputImage.color = fieldBackgroundColor;
            inputField.targetGraphic = inputImage;
            
            // Create text component for input field
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(inputGO.transform, false);
            
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 0);
            textRect.offsetMax = new Vector2(-5, 0);
            
            var inputText = textGO.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inputText.fontSize = 12;
            inputText.color = Color.white;
            inputText.alignment = TextAnchor.MiddleLeft;
            
            inputField.textComponent = inputText;
            
            var inputLayout = inputGO.AddComponent<LayoutElement>();
            inputLayout.flexibleWidth = 1f;
            inputLayout.preferredWidth = 100f;
            inputLayout.preferredHeight = fieldHeight;
            
            return inputField;
        }
        
        public void ShowNodeConfiguration(SpriteNode node)
        {
            currentNode = node;
            if (node?.NodeDataInstance != null)
            {
                // Update UI fields with node data
                isStartToggle.isOn = node.NodeDataInstance.isStart;
                durationField.text = node.NodeDataInstance.duration.ToString("F2");
                noteField.text = node.NodeDataInstance.note.ToString();
                channelField.text = node.NodeDataInstance.channel.ToString();
                rotationField.text = node.NodeDataInstance.rotation.ToString("F0");
                instrumentDropdown.value = node.NodeDataInstance.selectedInstrumentIndex;
                
                // Setup event listeners
                SetupEventListeners();
                
                SetVisible(true);
            }
        }
        
        public void HideNodeConfiguration()
        {
            ClearEventListeners();
            currentNode = null;
            SetVisible(false);
        }
        
        private void SetupEventListeners()
        {
            if (currentNode?.NodeDataInstance == null) return;
            
            // Clear existing listeners
            ClearEventListeners();
            
            // IsStart toggle
            isStartToggle.onValueChanged.AddListener((value) => {
                if (currentNode?.NodeDataInstance != null)
                    currentNode.NodeDataInstance.isStart = value;
            });
            
            // Duration field
            durationField.onEndEdit.AddListener((value) => {
                if (currentNode?.NodeDataInstance != null && float.TryParse(value, out float result))
                    currentNode.NodeDataInstance.duration = Mathf.Max(0.1f, result);
            });
            
            // Note field
            noteField.onEndEdit.AddListener((value) => {
                if (currentNode?.NodeDataInstance != null && int.TryParse(value, out int result))
                    currentNode.NodeDataInstance.note = Mathf.Clamp(result, 0, 127);
            });
            
            // Channel field
            channelField.onEndEdit.AddListener((value) => {
                if (currentNode?.NodeDataInstance != null && int.TryParse(value, out int result))
                    currentNode.NodeDataInstance.channel = Mathf.Clamp(result, 0, 16);
            });
            
            // Rotation field
            rotationField.onEndEdit.AddListener((value) => {
                if (currentNode?.NodeDataInstance != null && float.TryParse(value, out float result))
                {
                    // Clamp rotation to valid 90-degree increments (0, 90, 180, 270)
                    float clampedRotation = Mathf.Round(result / 90f) * 90f;
                    clampedRotation = clampedRotation % 360f;
                    if (clampedRotation < 0f) clampedRotation += 360f;
                    
                    currentNode.NodeDataInstance.rotation = clampedRotation;
                    rotationField.text = clampedRotation.ToString("F0");
                    currentNode.UpdateVisuals();
                }
            });
            
            // Instrument dropdown
            instrumentDropdown.onValueChanged.AddListener((value) => {
                if (currentNode?.NodeDataInstance != null && value >= 0 && value < availableInstruments.Count)
                {
                    currentNode.NodeDataInstance.selectedInstrumentIndex = value;
                    currentNode.NodeDataInstance.instrumentName = availableInstruments[value].Name;
                }
            });
        }
        
        private void ClearEventListeners()
        {
            isStartToggle?.onValueChanged.RemoveAllListeners();
            durationField?.onEndEdit.RemoveAllListeners();
            noteField?.onEndEdit.RemoveAllListeners();
            channelField?.onEndEdit.RemoveAllListeners();
            rotationField?.onEndEdit.RemoveAllListeners();
            instrumentDropdown?.onValueChanged.RemoveAllListeners();
        }
        
        public void SetVisible(bool visible)
        {
            isVisible = visible;
            if (configCanvas != null)
                configCanvas.gameObject.SetActive(visible);
        }
        
        public bool IsVisible => isVisible;
        
        private UnityEngine.Sprite CreateCheckmarkSprite()
        {
            // Create a simple checkmark texture
            int size = 16;
            var texture = new Texture2D(size, size);
            var colors = new Color32[size * size];
            
            // Clear background
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }
            
            // Draw simple checkmark
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Simple checkmark pattern
                    bool isCheckmark = false;
                    
                    // Left part of checkmark (going down-right)
                    if (x >= 2 && x <= 6 && y >= 6 && y <= 10)
                    {
                        if (Mathf.Abs((x - 2) - (y - 6)) <= 1)
                            isCheckmark = true;
                    }
                    
                    // Right part of checkmark (going up-right)  
                    if (x >= 6 && x <= 13 && y >= 3 && y <= 10)
                    {
                        if (Mathf.Abs((13 - x) - (y - 3)) <= 1)
                            isCheckmark = true;
                    }
                    
                    if (isCheckmark)
                        colors[y * size + x] = Color.white;
                }
            }
            
            texture.SetPixels32(colors);
            texture.Apply();
            
            return UnityEngine.Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f);
        }
        
        private UnityEngine.Sprite CreateArrowSprite()
        {
            // Create a simple down arrow texture
            int size = 12;
            var texture = new Texture2D(size, size);
            var colors = new Color32[size * size];
            
            // Clear background
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }
            
            // Draw simple down arrow
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Simple down arrow pattern
                    bool isArrow = false;
                    
                    // Top horizontal line
                    if (y >= 3 && y <= 4 && x >= 2 && x <= 9)
                    {
                        isArrow = true;
                    }
                    // Middle lines getting narrower
                    else if (y >= 5 && y <= 6 && x >= 3 && x <= 8)
                    {
                        isArrow = true;
                    }
                    else if (y >= 7 && y <= 8 && x >= 4 && x <= 7)
                    {
                        isArrow = true;
                    }
                    // Bottom point
                    else if (y >= 9 && y <= 10 && x >= 5 && x <= 6)
                    {
                        isArrow = true;
                    }
                    
                    if (isArrow)
                        colors[y * size + x] = Color.white;
                }
            }
            
            texture.SetPixels32(colors);
            texture.Apply();
            
            return UnityEngine.Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f);
        }
        
        private void OnDestroy()
        {
            ClearEventListeners();
        }
    }
}