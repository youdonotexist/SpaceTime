using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Commonwealth.Script.Ship.Monitors
{
    public class ShipStatsManager : MonoBehaviour
    {
        [Header("Update Settings")]
        [SerializeField] private float updateInterval = 1.0f;
        [SerializeField] private bool enableRandomSimulation = true;
        
        [Header("Stats")]
        [SerializeField] private List<ShipStat> allStats;
        
        private Dictionary<string, ShipStat> statLookup;
        private float lastUpdateTime;
        
        public event Action<ShipStat> OnStatChanged;
        public event Action OnStatsUpdated;
        
        public List<ShipStat> AllStats => allStats;
        public List<ShipStat> CriticalStats => allStats.Where(s => s.CurrentState == ShipStatState.Critical).ToList();
        public List<ShipStat> WarningStats => allStats.Where(s => s.CurrentState == ShipStatState.Warning).ToList();
        
        void Awake()
        {
            InitializeStats();
            BuildStatLookup();
        }
        
        void Update()
        {
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateAllStats();
                lastUpdateTime = Time.time;
            }
        }
        
        private void InitializeStats()
        {
            allStats = new List<ShipStat>();
            
            // Power & Energy Stats
            allStats.Add(new ShipStat("Reactor Output", "MW", "Power & Energy", 0, 1000, 200, 100, true));
            allStats.Add(new ShipStat("Capacitor Charge", "%", "Power & Energy", 0, 100, 30, 15, true));
            allStats.Add(new ShipStat("Power Bus Utilization", "%", "Power & Energy", 0, 100, 85, 95, false));
            allStats.Add(new ShipStat("Blackout Risk Index", "", "Power & Energy", 0, 1, 0.7f, 0.9f, false));
            allStats.Add(new ShipStat("Battery Health (SoH)", "%", "Power & Energy", 0, 100, 40, 20, true));
            allStats.Add(new ShipStat("Overclock Thermal Penalty", "°C", "Power & Energy", 0, 200, 150, 180, false));
            allStats.Add(new ShipStat("Energy Reserve Hours", "hrs", "Power & Energy", 0, 72, 12, 6, true));
            
            // Thermal & Coolant Stats
            allStats.Add(new ShipStat("Coolant Loop Pressure", "kPa", "Thermal & Coolant", 0, 500, 100, 50, true));
            allStats.Add(new ShipStat("Coolant ΔT", "°C", "Thermal & Coolant", 0, 100, 80, 90, false));
            allStats.Add(new ShipStat("Radiator Efficiency", "%", "Thermal & Coolant", 0, 100, 60, 40, true));
            allStats.Add(new ShipStat("Heat Sink Saturation", "%", "Thermal & Coolant", 0, 100, 80, 90, false));
            allStats.Add(new ShipStat("Thermal Hotspot Count", "#", "Thermal & Coolant", 0, 20, 10, 15, false));
            
            // Atmosphere & Life Support Stats
            allStats.Add(new ShipStat("O₂ Partial Pressure", "kPa", "Atmosphere & Life Support", 0, 50, 16, 12, true));
            allStats.Add(new ShipStat("CO₂ Concentration", "ppm", "Atmosphere & Life Support", 0, 5000, 1000, 2000, false));
            allStats.Add(new ShipStat("Humidity Level", "%", "Atmosphere & Life Support", 0, 100, 70, 80, false));
            allStats.Add(new ShipStat("Airflow Rate", "m³/s", "Atmosphere & Life Support", 0, 100, 20, 10, true));
            allStats.Add(new ShipStat("Filter Saturation", "%", "Atmosphere & Life Support", 0, 100, 75, 90, false));
            allStats.Add(new ShipStat("Scrubber Throughput", "%", "Atmosphere & Life Support", 0, 100, 60, 40, true));
            allStats.Add(new ShipStat("Biohazard Risk Score", "", "Atmosphere & Life Support", 0, 100, 60, 80, false));
            
            // Structural & Hull Stats
            allStats.Add(new ShipStat("Hull Stress", "%", "Structural & Hull", 0, 100, 70, 85, false));
            allStats.Add(new ShipStat("Microfracture Index", "#/m²", "Structural & Hull", 0, 1000, 500, 750, false));
            allStats.Add(new ShipStat("Bulkhead Integrity", "%", "Structural & Hull", 0, 100, 60, 40, true));
            allStats.Add(new ShipStat("Seal Leakage Rate", "Pa/s", "Structural & Hull", 0, 100, 50, 80, false));
            allStats.Add(new ShipStat("Vibration RMS", "g RMS", "Structural & Hull", 0, 10, 7, 9, false));
            allStats.Add(new ShipStat("Micrometeor Impact Rate", "#/hr", "Structural & Hull", 0, 50, 20, 35, false));
            
            // Propulsion & Maneuvering Stats
            allStats.Add(new ShipStat("Thrust Availability", "%", "Propulsion & Maneuvering", 0, 100, 60, 40, true));
            allStats.Add(new ShipStat("Reaction Mass Reserve", "%", "Propulsion & Maneuvering", 0, 100, 30, 15, true));
            allStats.Add(new ShipStat("Thruster Alignment Error", "deg", "Propulsion & Maneuvering", 0, 10, 5, 8, false));
            allStats.Add(new ShipStat("Drive Core Stability", "%", "Propulsion & Maneuvering", 0, 100, 70, 50, true));
            allStats.Add(new ShipStat("Propulsion Redundancy Score", "", "Propulsion & Maneuvering", 0, 10, 3, 1, true));
            allStats.Add(new ShipStat("FTL Spool Readiness", "%", "Propulsion & Maneuvering", 0, 100, 60, 40, true));
            
            // Navigation, Comms & Sensors Stats
            allStats.Add(new ShipStat("Nav Solution Confidence", "%", "Navigation, Comms & Sensors", 0, 100, 70, 50, true));
            allStats.Add(new ShipStat("Sensor SNR", "dB", "Navigation, Comms & Sensors", 0, 50, 20, 10, true));
            allStats.Add(new ShipStat("Comms Uptime (24h)", "%", "Navigation, Comms & Sensors", 0, 100, 80, 60, true));
            allStats.Add(new ShipStat("External Packet Loss", "%", "Navigation, Comms & Sensors", 0, 100, 20, 40, false));
            allStats.Add(new ShipStat("Command Bus Latency", "ms", "Navigation, Comms & Sensors", 0, 1000, 500, 800, false));
            allStats.Add(new ShipStat("Array Calibration Drift", "ppm", "Navigation, Comms & Sensors", 0, 1000, 500, 800, false));
            
            // Data, Control & Security Stats
            allStats.Add(new ShipStat("Control Bus Integrity", "%", "Data, Control & Security", 0, 100, 90, 80, true));
            allStats.Add(new ShipStat("Routing Table Health", "%", "Data, Control & Security", 0, 100, 80, 60, true));
            allStats.Add(new ShipStat("Cyber Intrusion Risk", "", "Data, Control & Security", 0, 100, 40, 70, false));
            allStats.Add(new ShipStat("Command Queue Backlog", "count", "Data, Control & Security", 0, 1000, 500, 800, false));
            
            // Manufacturing, Inventory & Logistics Stats
            allStats.Add(new ShipStat("Fabricator Queue Time", "min", "Manufacturing, Inventory & Logistics", 0, 1440, 720, 1080, false));
            allStats.Add(new ShipStat("Blueprint Coverage", "%", "Manufacturing, Inventory & Logistics", 0, 100, 60, 40, true));
            allStats.Add(new ShipStat("Spare Parts Stock", "%", "Manufacturing, Inventory & Logistics", 0, 100, 40, 20, true));
            allStats.Add(new ShipStat("Conduit Spool Stock", "meters", "Manufacturing, Inventory & Logistics", 0, 10000, 2000, 1000, true));
            allStats.Add(new ShipStat("Salvage Yield Rate", "%", "Manufacturing, Inventory & Logistics", 0, 100, 60, 40, true));
            
            // Defense & Shielding Stats
            allStats.Add(new ShipStat("Shield Charge", "%", "Defense & Shielding", 0, 100, 40, 20, true));
            allStats.Add(new ShipStat("Armor Ablation", "%", "Defense & Shielding", 0, 100, 60, 80, false));
            
            // Crew Health & Ops Stats
            allStats.Add(new ShipStat("Crew Fatigue Index", "", "Crew Health & Ops", 0, 100, 60, 80, false));
            allStats.Add(new ShipStat("Crew Morale Index", "", "Crew Health & Ops", 0, 100, 40, 20, true));
        }
        
        private void BuildStatLookup()
        {
            statLookup = new Dictionary<string, ShipStat>();
            foreach (var stat in allStats)
            {
                statLookup[stat.statName] = stat;
            }
        }
        
        private void UpdateAllStats()
        {
            if (!enableRandomSimulation) return;
            
            foreach (var stat in allStats)
            {
                // Simple simulation - add some random variation
                float variation = UnityEngine.Random.Range(-0.05f, 0.05f);
                float range = stat.maxValue - stat.minValue;
                float change = variation * range;
                
                stat.UpdateValue(stat.currentValue + change);
                OnStatChanged?.Invoke(stat);
            }
            
            OnStatsUpdated?.Invoke();
        }
        
        public ShipStat GetStat(string statName)
        {
            return statLookup.TryGetValue(statName, out var stat) ? stat : null;
        }
        
        public void UpdateStat(string statName, float newValue)
        {
            var stat = GetStat(statName);
            if (stat != null)
            {
                stat.UpdateValue(newValue);
                OnStatChanged?.Invoke(stat);
            }
        }
        
        public List<ShipStat> GetStatsByCategory(string category)
        {
            return allStats.Where(s => s.category == category).ToList();
        }
        
        public List<ShipStat> GetSortedStatsByUrgency()
        {
            return allStats.OrderByDescending(s => s.GetUrgencyScore()).ToList();
        }
        
        public List<string> GetAllCategories()
        {
            return allStats.Select(s => s.category).Distinct().ToList();
        }
    }
}