using System.Collections.Generic;
using UnityEngine;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// Data structure for engine part nodes that affect ship systems
    /// </summary>
    [System.Serializable]
    public class EnginePartNodeData
    {
        public string name;
        public string category;
        public Color color;
        public string description;
        public UnityEngine.Sprite icon;
        public List<string> affectedStats; // Stats from ship monitoring system
        
        // MIDI drum integration properties
        public bool usesDrumChannel = false; // Whether this component uses drum channel 9
        public int drumNote = 36; // MIDI note for drum sound (36 = kick drum by default)
        public string drumSoundName = ""; // Human-readable name of the drum sound
        
        public EnginePartNodeData(string name, string category, Color color, string description, params string[] stats)
        {
            this.name = name;
            this.category = category;
            this.color = color;
            this.description = description;
            this.affectedStats = new List<string>(stats);
            
            // Set drum channel usage for propulsion and energy components
            this.usesDrumChannel = category == "Propulsion & Maneuvering" || category == "Power & Energy";
        }
        
        public EnginePartNodeData(string name, string category, Color color, string description, int drumNote, string drumSoundName, params string[] stats)
        {
            this.name = name;
            this.category = category;
            this.color = color;
            this.description = description;
            this.affectedStats = new List<string>(stats);
            this.usesDrumChannel = category == "Propulsion & Maneuvering" || category == "Power & Energy";
            this.drumNote = drumNote;
            this.drumSoundName = drumSoundName;
        }
    }
    
    /// <summary>
    /// Static data provider for all 50 ship engine parts
    /// </summary>
    public static class EnginePartCatalog
    {
        // Category colors
        private static readonly Color PowerEnergyColor = new Color(1f, 0.8f, 0.2f, 1f); // Golden yellow
        private static readonly Color ThermalCoolantColor = new Color(0.2f, 0.6f, 1f, 1f); // Cool blue
        private static readonly Color LifeSupportColor = new Color(0.4f, 0.9f, 0.4f, 1f); // Life green
        private static readonly Color StructuralHullColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Steel gray
        private static readonly Color PropulsionColor = new Color(1f, 0.4f, 0.2f, 1f); // Engine orange
        private static readonly Color NavCommsColor = new Color(0.6f, 0.4f, 1f, 1f); // Tech purple
        private static readonly Color DataControlColor = new Color(0.9f, 0.2f, 0.9f, 1f); // Cyber magenta
        private static readonly Color ManufacturingColor = new Color(0.8f, 0.6f, 0.2f, 1f); // Industrial bronze
        private static readonly Color DefenseColor = new Color(1f, 0.2f, 0.2f, 1f); // Shield red
        
        public static List<EnginePartNodeData> GetAllEngineParts()
        {
            var parts = new List<EnginePartNodeData>();
            
            // Power & Energy (7 parts) - Mapped to drum sounds
            parts.AddRange(new[]
            {
                new EnginePartNodeData("Fusion Reactor Core", "Power & Energy", PowerEnergyColor, 
                    "Main power generation unit", 36, "Kick Drum", "Reactor Output", "Blackout Risk Index", "Energy Reserve Hours"),
                new EnginePartNodeData("Auxiliary Microreactor", "Power & Energy", PowerEnergyColor, 
                    "Backup power generation", 38, "Snare Drum", "Reactor Output", "Propulsion Redundancy Score", "Blackout Risk Index"),
                new EnginePartNodeData("Capacitor Bank", "Power & Energy", PowerEnergyColor, 
                    "Short-term power storage", 42, "Closed Hi-Hat", "Capacitor Charge", "Power Bus Utilization", "Shield Charge"),
                new EnginePartNodeData("Battery Rack", "Power & Energy", PowerEnergyColor, 
                    "Long-term energy storage", 46, "Open Hi-Hat", "Battery Health (SoH)", "Energy Reserve Hours", "Blackout Risk Index"),
                new EnginePartNodeData("Power Inverter/Rectifier Unit", "Power & Energy", PowerEnergyColor, 
                    "Power conditioning system", 49, "Crash Cymbal", "Power Bus Utilization", "Control Bus Integrity", "Blackout Risk Index"),
                new EnginePartNodeData("Overclock Controller (Clock Module)", "Power & Energy", PowerEnergyColor, 
                    "Performance enhancement system", 51, "Ride Cymbal", "Overclock Thermal Penalty", "Reactor Output", "Drive Core Stability"),
                new EnginePartNodeData("Bus Tie Breaker / ATS", "Power & Energy", PowerEnergyColor, 
                    "Power distribution switch", 47, "Low Mid Tom", "Blackout Risk Index", "Power Bus Utilization", "Comms Uptime (24h)")
            });
            
            // Thermal & Coolant (6 parts)
            parts.AddRange(new[]
            {
                new EnginePartNodeData("Coolant Pump Assembly", "Thermal & Coolant", ThermalCoolantColor, 
                    "Coolant circulation system", "Coolant Loop Pressure", "Thermal Hotspot Count", "Coolant ΔT"),
                new EnginePartNodeData("Heat Exchanger", "Thermal & Coolant", ThermalCoolantColor, 
                    "Heat transfer component", "Coolant ΔT", "Radiator Efficiency", "Heat Sink Saturation"),
                new EnginePartNodeData("Radiator Panel Array", "Thermal & Coolant", ThermalCoolantColor, 
                    "Heat dissipation system", "Radiator Efficiency", "Thermal Hotspot Count", "Overclock Thermal Penalty"),
                new EnginePartNodeData("Heat Sink Block", "Thermal & Coolant", ThermalCoolantColor, 
                    "Thermal mass component", "Heat Sink Saturation", "Thermal Hotspot Count", "Hull Stress"),
                new EnginePartNodeData("Phase-Change Reservoir (PCM Tank)", "Thermal & Coolant", ThermalCoolantColor, 
                    "Thermal storage system", "Radiator Efficiency", "Heat Sink Saturation", "Overclock Thermal Penalty"),
                new EnginePartNodeData("Thermal Bypass/Control Valve", "Thermal & Coolant", ThermalCoolantColor, 
                    "Temperature regulation", "Coolant Loop Pressure", "Coolant ΔT", "Thermal Hotspot Count")
            });
            
            // Atmosphere & Life Support (8 parts)
            parts.AddRange(new[]
            {
                new EnginePartNodeData("O₂ Generator (Electrolyzer)", "Atmosphere & Life Support", LifeSupportColor, 
                    "Oxygen production system", "O₂ Partial Pressure", "Energy Reserve Hours", "Crew Morale Index"),
                new EnginePartNodeData("CO₂ Scrubber Cartridge", "Atmosphere & Life Support", LifeSupportColor, 
                    "Carbon dioxide removal", "CO₂ Concentration", "Scrubber Throughput", "Biohazard Risk Score"),
                new EnginePartNodeData("Air Circulation Blower", "Atmosphere & Life Support", LifeSupportColor, 
                    "Atmosphere circulation", "Airflow Rate", "Humidity Level", "Biohazard Risk Score"),
                new EnginePartNodeData("HEPA Filter Cassette", "Atmosphere & Life Support", LifeSupportColor, 
                    "Air filtration system", "Filter Saturation", "Biohazard Risk Score", "Airflow Rate"),
                new EnginePartNodeData("Dehumidifier Unit", "Atmosphere & Life Support", LifeSupportColor, 
                    "Humidity control", "Humidity Level", "Biohazard Risk Score", "Energy Reserve Hours"),
                new EnginePartNodeData("Humidifier Vaporizer", "Atmosphere & Life Support", LifeSupportColor, 
                    "Humidity regulation", "Humidity Level", "Crew Morale Index", "Biohazard Risk Score"),
                new EnginePartNodeData("Pressure Regulator / Relief Valve", "Atmosphere & Life Support", LifeSupportColor, 
                    "Pressure management", "Seal Leakage Rate", "O₂ Partial Pressure", "Bulkhead Integrity"),
                new EnginePartNodeData("UV Sterilizer / Biofilter", "Atmosphere & Life Support", LifeSupportColor, 
                    "Biological contamination control", "Biohazard Risk Score", "Comms Uptime (24h)", "Crew Morale Index")
            });
            
            // Structural & Hull (6 parts)
            parts.AddRange(new[]
            {
                new EnginePartNodeData("Hull Plate Segment (Composite Tile)", "Structural & Hull", StructuralHullColor, 
                    "Primary hull armor", "Hull Stress", "Armor Ablation", "Micrometeor Impact Rate"),
                new EnginePartNodeData("Bulkhead Door Actuator", "Structural & Hull", StructuralHullColor, 
                    "Compartment isolation", "Bulkhead Integrity", "Seal Leakage Rate", "Crew Fatigue Index"),
                new EnginePartNodeData("Seal/Gasket Ring Set", "Structural & Hull", StructuralHullColor, 
                    "Pressure sealing system", "Seal Leakage Rate", "Bulkhead Integrity", "Biohazard Risk Score"),
                new EnginePartNodeData("Vibration Dampener/Isolator", "Structural & Hull", StructuralHullColor, 
                    "Structural vibration control", "Vibration RMS", "Microfracture Index", "Sensor SNR"),
                new EnginePartNodeData("Microfracture Sensor Mesh", "Structural & Hull", StructuralHullColor, 
                    "Hull integrity monitoring", "Microfracture Index", "Hull Stress", "Crew Fatigue Index"),
                new EnginePartNodeData("Micrometeor Shield Tile (Whipple)", "Structural & Hull", StructuralHullColor, 
                    "Impact protection system", "Micrometeor Impact Rate", "Armor Ablation", "Hull Stress")
            });
            
            // Propulsion & Maneuvering (6 parts) - Mapped to drum sounds
            parts.AddRange(new[]
            {
                new EnginePartNodeData("Main Thruster Nozzle", "Propulsion & Maneuvering", PropulsionColor, 
                    "Primary propulsion system", 35, "Acoustic Bass Drum", "Thrust Availability", "Thruster Alignment Error", "Drive Core Stability"),
                new EnginePartNodeData("Reaction Mass Tank", "Propulsion & Maneuvering", PropulsionColor, 
                    "Propellant storage", 40, "Electric Snare", "Reaction Mass Reserve", "Thrust Availability", "FTL Spool Readiness"),
                new EnginePartNodeData("Thrust Vector Gimbal", "Propulsion & Maneuvering", PropulsionColor, 
                    "Directional control", 41, "Low Floor Tom", "Thruster Alignment Error", "Vibration RMS", "Propulsion Redundancy Score"),
                new EnginePartNodeData("Drive Core Field Coil", "Propulsion & Maneuvering", PropulsionColor, 
                    "FTL drive component", 43, "High Floor Tom", "Drive Core Stability", "Overclock Thermal Penalty", "FTL Spool Readiness"),
                new EnginePartNodeData("FTL Spool/Condenser", "Propulsion & Maneuvering", PropulsionColor, 
                    "Jump preparation system", 45, "Low Tom", "FTL Spool Readiness", "Drive Core Stability", "Energy Reserve Hours"),
                new EnginePartNodeData("RCS Thruster Quad", "Propulsion & Maneuvering", PropulsionColor, 
                    "Attitude control system", 48, "Hi Mid Tom", "Thrust Availability", "Propulsion Redundancy Score", "Nav Solution Confidence")
            });
            
            // Navigation, Comms & Sensors (7 parts)
            parts.AddRange(new[]
            {
                new EnginePartNodeData("Navigation Computer", "Navigation, Comms & Sensors", NavCommsColor, 
                    "Navigation processing", "Nav Solution Confidence", "Command Bus Latency", "Routing Table Health"),
                new EnginePartNodeData("Star Tracker Camera", "Navigation, Comms & Sensors", NavCommsColor, 
                    "Stellar navigation sensor", "Nav Solution Confidence", "Array Calibration Drift", "Sensor SNR"),
                new EnginePartNodeData("Inertial Measurement Unit (IMU)", "Navigation, Comms & Sensors", NavCommsColor, 
                    "Motion sensing system", "Nav Solution Confidence", "Array Calibration Drift", "Command Bus Latency"),
                new EnginePartNodeData("Sensor Array Receiver (Multi-band)", "Navigation, Comms & Sensors", NavCommsColor, 
                    "Multi-spectrum sensing", "Sensor SNR", "Array Calibration Drift", "Cyber Intrusion Risk"),
                new EnginePartNodeData("High-Gain Antenna", "Navigation, Comms & Sensors", NavCommsColor, 
                    "Long-range communications", "Comms Uptime (24h)", "External Packet Loss", "Sensor SNR"),
                new EnginePartNodeData("Quantum/Ka-Band Transceiver", "Navigation, Comms & Sensors", NavCommsColor, 
                    "Quantum communication system", "Comms Uptime (24h)", "External Packet Loss", "Cyber Intrusion Risk"),
                new EnginePartNodeData("Signal Processing DSP Module", "Navigation, Comms & Sensors", NavCommsColor, 
                    "Signal processing unit", "Sensor SNR", "External Packet Loss", "Command Bus Latency")
            });
            
            // Data, Control & Security (5 parts)
            parts.AddRange(new[]
            {
                new EnginePartNodeData("Control Router / PLC Backplane", "Data, Control & Security", DataControlColor, 
                    "Control system backbone", "Control Bus Integrity", "Command Bus Latency", "Routing Table Health"),
                new EnginePartNodeData("Error-Correcting Memory Bank", "Data, Control & Security", DataControlColor, 
                    "Fault-tolerant storage", "Control Bus Integrity", "Routing Table Health", "Command Queue Backlog"),
                new EnginePartNodeData("Security Firewall Appliance", "Data, Control & Security", DataControlColor, 
                    "Network security system", "Cyber Intrusion Risk", "Comms Uptime (24h)", "Command Bus Latency"),
                new EnginePartNodeData("Intrusion Detection Node (IDS)", "Data, Control & Security", DataControlColor, 
                    "Threat monitoring system", "Cyber Intrusion Risk", "Control Bus Integrity", "Command Queue Backlog"),
                new EnginePartNodeData("Redundant Controller (Hot-Standby PLC)", "Data, Control & Security", DataControlColor, 
                    "Backup control system", "Control Bus Integrity", "Propulsion Redundancy Score", "Blackout Risk Index")
            });
            
            // Manufacturing, Inventory & Logistics (3 parts)
            parts.AddRange(new[]
            {
                new EnginePartNodeData("Fabricator (Multi-Material Printer)", "Manufacturing, Inventory & Logistics", ManufacturingColor, 
                    "On-demand manufacturing", "Fabricator Queue Time", "Blueprint Coverage", "Spare Parts Stock"),
                new EnginePartNodeData("Smart Parts Locker (RFID Inventory)", "Manufacturing, Inventory & Logistics", ManufacturingColor, 
                    "Automated inventory system", "Spare Parts Stock", "Blueprint Coverage", "Fabricator Queue Time"),
                new EnginePartNodeData("Salvage Drone (Autonomous EVA)", "Manufacturing, Inventory & Logistics", ManufacturingColor, 
                    "Material recovery system", "Salvage Yield Rate", "Spare Parts Stock", "Biohazard Risk Score")
            });
            
            // Defense & Shielding (2 parts)
            parts.AddRange(new[]
            {
                new EnginePartNodeData("Shield Emitter Array", "Defense & Shielding", DefenseColor, 
                    "Energy shield projection", "Shield Charge", "Armor Ablation", "Micrometeor Impact Rate"),
                new EnginePartNodeData("Shield Capacitor Bank", "Defense & Shielding", DefenseColor, 
                    "Shield power storage", "Shield Charge", "Capacitor Charge", "Blackout Risk Index")
            });
            
            return parts;
        }
    }
}