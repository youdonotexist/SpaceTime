using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Commonwealth.Script.Ship.Monitors
{
    public class ShipStatsUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject statItemPrefab;
        [SerializeField] private Transform statsContainer;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button toggleButton;
        [SerializeField] private TextMeshProUGUI headerText;
        
        [Header("Display Settings")]
        [SerializeField] private int maxVisibleStats = 10;
        [SerializeField] private bool showOnlyProblems = false;
        [SerializeField] private bool autoSort = true;
        [SerializeField] private float refreshRate = 0.5f;
        
        [Header("Colors")]
        [SerializeField] private Color goodColor = Color.green;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.7f);
        
        private ShipStatsManager statsManager;
        private Dictionary<string, ShipStatUIItem> uiItems;
        private List<ShipStatUIItem> activeItems;
        private float lastRefreshTime;
        private bool isVisible = true;
        
        void Awake()
        {
            uiItems = new Dictionary<string, ShipStatUIItem>();
            activeItems = new List<ShipStatUIItem>();
            
            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleVisibility);
        }
        
        void Start()
        {
            statsManager = FindObjectOfType<ShipStatsManager>();
            if (statsManager == null)
            {
                Debug.LogError("ShipStatsUI: No ShipStatsManager found in scene!");
                return;
            }
            
            InitializeUI();
            SubscribeToEvents();
        }
        
        void Update()
        {
            if (Time.time - lastRefreshTime >= refreshRate)
            {
                RefreshDisplay();
                lastRefreshTime = Time.time;
            }
        }
        
        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void InitializeUI()
        {
            CreateStatItems();
            RefreshDisplay();
            UpdateHeaderText();
        }
        
        private void CreateStatItems()
        {
            if (statItemPrefab == null || statsContainer == null)
            {
                Debug.LogError("ShipStatsUI: Missing prefab or container references!");
                return;
            }
            
            foreach (var stat in statsManager.AllStats)
            {
                GameObject itemObj = Instantiate(statItemPrefab, statsContainer);
                ShipStatUIItem uiItem = itemObj.GetComponent<ShipStatUIItem>();
                
                if (uiItem == null)
                {
                    uiItem = itemObj.AddComponent<ShipStatUIItem>();
                }
                
                uiItem.Initialize(stat);
                uiItems[stat.statName] = uiItem;
                
                // Initially hide all items
                itemObj.SetActive(false);
            }
        }
        
        private void RefreshDisplay()
        {
            if (statsManager == null) return;
            
            // Get stats to display based on settings
            List<ShipStat> statsToShow = GetStatsToDisplay();
            
            // Sort by urgency if auto-sort is enabled
            if (autoSort)
            {
                statsToShow = statsToShow.OrderByDescending(s => s.GetUrgencyScore()).ToList();
            }
            
            // Limit to max visible stats
            if (maxVisibleStats > 0)
            {
                statsToShow = statsToShow.Take(maxVisibleStats).ToList();
            }
            
            // Update active items
            UpdateActiveItems(statsToShow);
            
            // Update header
            UpdateHeaderText();
        }
        
        private List<ShipStat> GetStatsToDisplay()
        {
            if (showOnlyProblems)
            {
                return statsManager.AllStats
                    .Where(s => s.CurrentState != ShipStatState.Good)
                    .ToList();
            }
            
            return statsManager.AllStats.ToList();
        }
        
        private void UpdateActiveItems(List<ShipStat> statsToShow)
        {
            // Hide all items first
            foreach (var item in activeItems)
            {
                item.gameObject.SetActive(false);
            }
            activeItems.Clear();
            
            // Show and update selected items
            for (int i = 0; i < statsToShow.Count; i++)
            {
                var stat = statsToShow[i];
                if (uiItems.TryGetValue(stat.statName, out var uiItem))
                {
                    uiItem.gameObject.SetActive(true);
                    uiItem.transform.SetSiblingIndex(i);
                    uiItem.UpdateDisplay();
                    activeItems.Add(uiItem);
                }
            }
        }
        
        private void UpdateHeaderText()
        {
            if (headerText == null) return;
            
            int criticalCount = statsManager.CriticalStats.Count;
            int warningCount = statsManager.WarningStats.Count;
            int totalCount = statsManager.AllStats.Count;
            
            if (criticalCount > 0)
            {
                headerText.text = $"SHIP SYSTEMS - {criticalCount} CRITICAL";
                headerText.color = criticalColor;
            }
            else if (warningCount > 0)
            {
                headerText.text = $"SHIP SYSTEMS - {warningCount} WARNING";
                headerText.color = warningColor;
            }
            else
            {
                headerText.text = "SHIP SYSTEMS - ALL NOMINAL";
                headerText.color = goodColor;
            }
        }
        
        private void SubscribeToEvents()
        {
            if (statsManager != null)
            {
                statsManager.OnStatChanged += OnStatChanged;
                statsManager.OnStatsUpdated += OnStatsUpdated;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (statsManager != null)
            {
                statsManager.OnStatChanged -= OnStatChanged;
                statsManager.OnStatsUpdated -= OnStatsUpdated;
            }
        }
        
        private void OnStatChanged(ShipStat stat)
        {
            if (uiItems.TryGetValue(stat.statName, out var uiItem))
            {
                uiItem.UpdateDisplay();
            }
        }
        
        private void OnStatsUpdated()
        {
            // Refresh display will be called in Update loop
        }
        
        public void ToggleVisibility()
        {
            isVisible = !isVisible;
            gameObject.SetActive(isVisible);
        }
        
        public void SetShowOnlyProblems(bool showOnly)
        {
            showOnlyProblems = showOnly;
            RefreshDisplay();
        }
        
        public void SetMaxVisibleStats(int maxStats)
        {
            maxVisibleStats = maxStats;
            RefreshDisplay();
        }
        
        public void SetAutoSort(bool autoSort)
        {
            this.autoSort = autoSort;
            RefreshDisplay();
        }
    }
}