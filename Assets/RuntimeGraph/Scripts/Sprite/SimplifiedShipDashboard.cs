using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// Simplified ship dashboard showing key ship systems: thrust/speed, shields, and energy
    /// </summary>
    public class SimplifiedShipDashboard : MonoBehaviour
    {
        [Header("Dashboard UI")]
        [SerializeField] private RectTransform dashboardPanel;
        [SerializeField] private TextMeshProUGUI thrustSpeedText;
        [SerializeField] private TextMeshProUGUI shieldsText;
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private Image thrustSpeedBar;
        [SerializeField] private Image shieldsBar;
        [SerializeField] private Image energyBar;
        
        private SpriteRuntimeGraph runtimeGraph;
        private float updateInterval = 0.5f;
        private float lastUpdateTime;
        
        // Ship system values
        private float currentThrust = 0f;
        private float currentSpeed = 0f;
        private float shieldLevel = 100f;
        private float energyLevel = 100f;
        
        public void Initialize(SpriteRuntimeGraph graph)
        {
            runtimeGraph = graph;
            SetupUI();
        }
        
        private void SetupUI()
        {
            // Create dashboard panel
            var panelGO = new GameObject("DashboardPanel");
            panelGO.transform.SetParent(transform);
            dashboardPanel = panelGO.AddComponent<RectTransform>();
            
            // Position panel at top-left corner
            dashboardPanel.anchorMin = new Vector2(0, 1);
            dashboardPanel.anchorMax = new Vector2(0, 1);
            dashboardPanel.anchoredPosition = new Vector2(20, -20);
            dashboardPanel.sizeDelta = new Vector2(300, 120);
            
            // Add background
            var bgImage = panelGO.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            // Add border
            var outline = panelGO.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(1, -1);
            
            // Create system displays
            CreateSystemDisplay("Thrust & Speed", 0, out thrustSpeedText, out thrustSpeedBar);
            CreateSystemDisplay("Deflector Shields", 1, out shieldsText, out shieldsBar);
            CreateSystemDisplay("Energy Systems", 2, out energyText, out energyBar);
            
            // Initialize display
            UpdateDisplay();
        }
        
        private void CreateSystemDisplay(string systemName, int index, out TextMeshProUGUI textComponent, out Image barComponent)
        {
            float yPos = -10 - (index * 35);
            
            // Create text label
            var textGO = new GameObject($"{systemName}_Text");
            textGO.transform.SetParent(dashboardPanel);
            textComponent = textGO.AddComponent<TextMeshProUGUI>();
            
            var textRect = textComponent.rectTransform;
            textRect.anchorMin = new Vector2(0, 1);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.anchoredPosition = new Vector2(10, yPos);
            textRect.sizeDelta = new Vector2(-20, 20);
            
            textComponent.text = $"{systemName}: --";
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Left;
            
            // Create status bar background
            var barBgGO = new GameObject($"{systemName}_BarBG");
            barBgGO.transform.SetParent(dashboardPanel);
            var barBgRect = barBgGO.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0, 1);
            barBgRect.anchorMax = new Vector2(1, 1);
            barBgRect.anchoredPosition = new Vector2(10, yPos - 18);
            barBgRect.sizeDelta = new Vector2(-20, 8);
            
            var barBgImage = barBgGO.AddComponent<Image>();
            barBgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            
            // Create status bar fill
            var barFillGO = new GameObject($"{systemName}_BarFill");
            barFillGO.transform.SetParent(barBgGO.transform);
            barComponent = barFillGO.AddComponent<Image>();
            
            var barFillRect = barComponent.rectTransform;
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = new Vector2(1, 1);
            barFillRect.anchoredPosition = Vector2.zero;
            barFillRect.sizeDelta = Vector2.zero;
            
            // Set initial color based on system type
            Color barColor = systemName switch
            {
                "Thrust & Speed" => new Color(1f, 0.5f, 0f, 0.8f), // Orange
                "Deflector Shields" => new Color(0f, 0.5f, 1f, 0.8f), // Blue
                "Energy Systems" => new Color(1f, 0.8f, 0f, 0.8f), // Yellow
                _ => Color.green
            };
            barComponent.color = barColor;
        }
        
        private void Update()
        {
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateSystemValues();
                UpdateDisplay();
                lastUpdateTime = Time.time;
            }
        }
        
        private void UpdateSystemValues()
        {
            // Calculate thrust and speed based on connected engine nodes
            CalculateThrustAndSpeed();
            
            // Update shield level (simulate degradation/regeneration)
            UpdateShieldLevel();
            
            // Update energy level (simulate consumption/generation)
            UpdateEnergyLevel();
        }
        
        private void CalculateThrustAndSpeed()
        {
            if (runtimeGraph == null)
            {
                currentThrust = 0f;
                currentSpeed = 0f;
                return;
            }
            
            // Find connected engine chains (start to end nodes with active connections)
            var engineChains = FindConnectedEngineChains();
            
            // Calculate total thrust from active engine chains
            float totalThrust = 0f;
            foreach (var chain in engineChains)
            {
                // Each complete chain contributes thrust based on its efficiency
                totalThrust += CalculateChainThrust(chain);
            }
            
            currentThrust = totalThrust;
            // Speed is proportional to thrust (simplified physics)
            currentSpeed = totalThrust * 2.5f; // Arbitrary multiplier for display
        }
        
        private List<List<SpriteNode.NodeData>> FindConnectedEngineChains()
        {
            var chains = new List<List<SpriteNode.NodeData>>();
            
            if (runtimeGraph?.Nodes == null) return chains;
            
            // Find start nodes (engine begin nodes)
            var startNodes = runtimeGraph.Nodes.FindAll(n => IsEngineStartNode(n));
            
            foreach (var startNode in startNodes)
            {
                var chain = TraceEngineChain(startNode);
                if (chain.Count > 1 && IsEngineEndNode(chain[chain.Count - 1]))
                {
                    chains.Add(chain);
                }
            }
            
            return chains;
        }
        
        private bool IsEngineStartNode(SpriteNode.NodeData node)
        {
            // Check if node is an engine start node (has "start" part_flow or is a power source)
            return node.isEngine || 
                   node.title.Contains("Engine") || 
                   node.title.Contains("Reactor") ||
                   node.title.Contains("Thruster");
        }
        
        private bool IsEngineEndNode(SpriteNode.NodeData node)
        {
            // Check if node is an engine end node (thruster, nozzle, etc.)
            return node.title.Contains("Thruster") ||
                   node.title.Contains("Nozzle") ||
                   node.title.Contains("Drive");
        }
        
        private List<SpriteNode.NodeData> TraceEngineChain(SpriteNode.NodeData startNode)
        {
            var chain = new List<SpriteNode.NodeData> { startNode };
            var visited = new HashSet<string> { startNode.id };
            
            var currentNode = startNode;
            
            // Follow connections from start to end
            while (true)
            {
                var nextConnection = runtimeGraph.Connections?.Find(c => c.fromNodeId == currentNode.id);
                if (nextConnection == null) break;
                
                var nextNode = runtimeGraph.Nodes?.Find(n => n.id == nextConnection.toNodeId);
                if (nextNode == null || visited.Contains(nextNode.id)) break;
                
                chain.Add(nextNode);
                visited.Add(nextNode.id);
                currentNode = nextNode;
            }
            
            return chain;
        }
        
        private float CalculateChainThrust(List<SpriteNode.NodeData> chain)
        {
            // Base thrust per complete chain
            float baseThrust = 50f;
            
            // Efficiency based on chain length and node types
            float efficiency = Mathf.Clamp01(1.0f - (chain.Count - 2) * 0.1f);
            
            return baseThrust * efficiency;
        }
        
        private void UpdateShieldLevel()
        {
            // Simulate shield regeneration/degradation
            // For now, just maintain at 100% - can be enhanced with actual shield systems
            shieldLevel = Mathf.Clamp(shieldLevel + Random.Range(-1f, 2f), 0f, 100f);
        }
        
        private void UpdateEnergyLevel()
        {
            // Simulate energy consumption/generation
            // For now, fluctuate around 85% - can be enhanced with actual energy systems
            energyLevel = Mathf.Clamp(energyLevel + Random.Range(-2f, 1f), 0f, 100f);
        }
        
        private void UpdateDisplay()
        {
            // Update thrust & speed display
            if (thrustSpeedText != null)
            {
                thrustSpeedText.text = $"Thrust & Speed: {currentThrust:F0}N / {currentSpeed:F0}m/s";
            }
            if (thrustSpeedBar != null)
            {
                thrustSpeedBar.fillAmount = Mathf.Clamp01(currentThrust / 200f); // Max 200N for display
            }
            
            // Update shields display
            if (shieldsText != null)
            {
                shieldsText.text = $"Deflector Shields: {shieldLevel:F0}%";
            }
            if (shieldsBar != null)
            {
                shieldsBar.fillAmount = shieldLevel / 100f;
                // Change color based on shield level
                Color shieldColor = shieldLevel > 50f ? Color.cyan : 
                                   shieldLevel > 25f ? Color.yellow : Color.red;
                shieldsBar.color = new Color(shieldColor.r, shieldColor.g, shieldColor.b, 0.8f);
            }
            
            // Update energy display
            if (energyText != null)
            {
                energyText.text = $"Energy Systems: {energyLevel:F0}%";
            }
            if (energyBar != null)
            {
                energyBar.fillAmount = energyLevel / 100f;
                // Change color based on energy level
                Color energyColor = energyLevel > 50f ? Color.green : 
                                   energyLevel > 25f ? Color.yellow : Color.red;
                energyBar.color = new Color(energyColor.r, energyColor.g, energyColor.b, 0.8f);
            }
        }
    }
}