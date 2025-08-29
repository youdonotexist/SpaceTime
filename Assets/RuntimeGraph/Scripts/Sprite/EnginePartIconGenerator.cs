using UnityEngine;
using System.Collections.Generic;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// Loads sprite icons for engine parts from Assets/Commonwealth/Art/ShipParts
    /// </summary>
    public static class EnginePartIconGenerator
    {
        private const int ICON_SIZE = 64;
        private const int CENTER_X = ICON_SIZE / 2;
        private const int CENTER_Y = ICON_SIZE / 2;
        // Mapping of engine part names to their corresponding sprite file names
        private static readonly Dictionary<string, string> PartNameToFilename = new Dictionary<string, string>
        {
            // Power & Energy
            { "Fusion Reactor Core", "fusion_reactor_core" },
            { "Auxiliary Microreactor", "auxiliary_microreactor" },
            { "Capacitor Bank", "capacitor_bank" },
            { "Battery Rack", "battery_rack" },
            { "Power Inverter/Rectifier Unit", "power_inverter_rectifier_unit" },
            { "Overclock Controller (Clock Module)", "overclock_controller" },
            { "Bus Tie Breaker / ATS", "bus_tie_breaker_ats" },
            
            // Thermal & Coolant
            { "Coolant Pump Assembly", "coolant_pump_assembly" },
            { "Heat Exchanger", "heat_exchanger" },
            { "Radiator Panel Array", "radiator_panel_array" },
            { "Heat Sink Block", "heat_sink_block" },
            { "Phase-Change Reservoir (PCM Tank)", "phase_change_reservoir" },
            { "Thermal Bypass/Control Valve", "thermal_control_valve" },
            
            // Atmosphere & Life Support
            { "O₂ Generator (Electrolyzer)", "o2_generator_electrolyzer" },
            { "CO₂ Scrubber Cartridge", "co2_scrubber_cartridge" },
            { "Air Circulation Blower", "air_circulation_blower" },
            { "HEPA Filter Cassette", "hepa_filter_cassette" },
            { "Dehumidifier Unit", "dehumidifier_unit" },
            { "Humidifier Vaporizer", "humidifier_vaporizer" },
            { "Pressure Regulator / Relief Valve", "pressure_regulator_relief_valve" },
            { "UV Sterilizer / Biofilter", "uv_sterilizer_biofilter" },
            
            // Structural & Hull
            { "Hull Plate Segment (Composite Tile)", "hull_plate_segment_composite_tile" },
            { "Bulkhead Door Actuator", "bulkhead_door_actuator" },
            { "Seal/Gasket Ring Set", "seal_gasket_ring_set" },
            { "Vibration Dampener/Isolator", "vibration_dampener_isolator" },
            { "Microfracture Sensor Mesh", "microfracture_sensor_mesh" },
            { "Micrometeor Shield Tile (Whipple)", "micrometeor_shield_tile_whipple" },
            
            // Propulsion & Maneuvering
            { "Main Thruster Nozzle", "main_thruster_nozzle" },
            { "Reaction Mass Tank", "reaction_mass_tank" },
            { "Thrust Vector Gimbal", "thrust_vector_gimbal" },
            { "Drive Core Field Coil", "drive_core_field_coil" },
            { "FTL Spool/Condenser", "ftl_spool_condenser" },
            { "RCS Thruster Quad", "rcs_thruster_quad" },
            
            // Navigation, Comms & Sensors
            { "Navigation Computer", "navigation_computer" },
            { "Star Tracker Camera", "star_tracker_camera" },
            { "Inertial Measurement Unit (IMU)", "inertial_measurement_unit_imu" },
            { "Sensor Array Receiver (Multi-band)", "sensor_array_receiver_multi_band" },
            { "High-Gain Antenna", "high_gain_antenna" },
            { "Quantum/Ka-Band Transceiver", "quantum_ka_band_transceiver" },
            { "Signal Processing DSP Module", "signal_processing_dsp_module" },
            
            // Data, Control & Security
            { "Control Router / PLC Backplane", "control_router_plc_backplane" },
            { "Error-Correcting Memory Bank", "error_correcting_memory_bank" },
            { "Security Firewall Appliance", "security_firewall_appliance" },
            { "Intrusion Detection Node (IDS)", "intrusion_detection_node_ids" },
            { "Redundant Controller (Hot-Standby PLC)", "redundant_controller_hot_standby_plc" },
            
            // Manufacturing, Inventory & Logistics
            { "Fabricator (Multi-Material Printer)", "fabricator_multi_material_printer" },
            { "Smart Parts Locker (RFID Inventory)", "smart_parts_locker_rfid" },
            { "Salvage Drone (Autonomous EVA)", "salvage_drone_autonomous_eva" },
            
            // Defense & Shielding
            { "Shield Emitter Array", "shield_emitter_array" },
            { "Shield Capacitor Bank", "shield_capacitor_bank" }
        };
        
        public static UnityEngine.Sprite GenerateIconForPart(EnginePartNodeData partData)
        {
            // Try to load sprite from Resources first
            if (PartNameToFilename.TryGetValue(partData.name, out string filename))
            {
                string resourcePath = $"Commonwealth/Art/ShipParts/{filename}";
                UnityEngine.Sprite sprite = Resources.Load<UnityEngine.Sprite>(resourcePath);
                
                if (sprite != null)
                {
                    return sprite;
                }
                
                // If Resources.Load fails, try loading from Assets folder using Resources.LoadAssetAtPath
                string assetPath = $"Assets/Commonwealth/Art/ShipParts/{filename}.png";
                Texture2D texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                
                if (texture != null)
                {
                    return UnityEngine.Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
                
                Debug.LogWarning($"Could not load sprite for {partData.name} at path: {resourcePath} or {assetPath}");
            }
            else
            {
                Debug.LogWarning($"No filename mapping found for engine part: {partData.name}");
            }
            
            // Fallback to procedural generation if sprite loading fails
            return CreateFallbackSprite(partData);
        }
        
        private static UnityEngine.Sprite CreateFallbackSprite(EnginePartNodeData partData)
        {
            // Simple fallback sprite - colored circle with border
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.4f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    
                    if (distance <= radius)
                    {
                        if (distance >= radius - 2) // Border
                        {
                            pixels[y * size + x] = Color.black;
                        }
                        else
                        {
                            pixels[y * size + x] = partData.color;
                        }
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return UnityEngine.Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
        
        private static void DrawPowerIcon(Color[] pixels, EnginePartNodeData partData)
        {
            Color baseColor = partData.color;
            Color accentColor = Color.Lerp(baseColor, Color.white, 0.3f);
            
            if (partData.name.Contains("Reactor"))
            {
                // Draw reactor core - circle with radiating lines
                DrawCircle(pixels, CENTER_X, CENTER_Y, 12, baseColor);
                DrawCircle(pixels, CENTER_X, CENTER_Y, 8, accentColor);
                for (int angle = 0; angle < 360; angle += 45)
                {
                    DrawLine(pixels, CENTER_X, CENTER_Y, 
                            CENTER_X + (int)(18 * Mathf.Cos(angle * Mathf.Deg2Rad)), 
                            CENTER_Y + (int)(18 * Mathf.Sin(angle * Mathf.Deg2Rad)), baseColor);
                }
            }
            else if (partData.name.Contains("Capacitor") || partData.name.Contains("Battery"))
            {
                // Draw battery/capacitor - rectangles with terminals
                DrawRectangle(pixels, CENTER_X - 15, CENTER_Y - 8, 30, 16, baseColor);
                DrawRectangle(pixels, CENTER_X - 12, CENTER_Y - 5, 24, 10, accentColor);
                DrawRectangle(pixels, CENTER_X - 18, CENTER_Y - 3, 4, 6, baseColor); // Terminal
                DrawRectangle(pixels, CENTER_X + 14, CENTER_Y - 3, 4, 6, baseColor); // Terminal
            }
            else
            {
                // Generic power component - lightning bolt
                DrawLightningBolt(pixels, CENTER_X, CENTER_Y, baseColor);
            }
        }
        
        private static void DrawThermalIcon(Color[] pixels, EnginePartNodeData partData)
        {
            Color baseColor = partData.color;
            Color hotColor = Color.Lerp(baseColor, Color.red, 0.4f);
            Color coldColor = Color.Lerp(baseColor, Color.cyan, 0.4f);
            
            if (partData.name.Contains("Radiator"))
            {
                // Draw radiator - parallel lines with heat waves
                for (int i = 0; i < 5; i++)
                {
                    int x = CENTER_X - 20 + i * 10;
                    DrawLine(pixels, x, CENTER_Y - 15, x, CENTER_Y + 15, baseColor);
                }
                // Heat waves
                for (int i = 0; i < 3; i++)
                {
                    DrawWavyLine(pixels, CENTER_X - 15, CENTER_Y - 20 + i * 8, 30, hotColor);
                }
            }
            else if (partData.name.Contains("Heat"))
            {
                // Draw heat sink - stepped blocks
                for (int i = 0; i < 4; i++)
                {
                    int height = 8 + i * 4;
                    DrawRectangle(pixels, CENTER_X - 15 + i * 8, CENTER_Y - height/2, 6, height, baseColor);
                }
            }
            else if (partData.name.Contains("Coolant") || partData.name.Contains("Pump"))
            {
                // Draw pump/coolant - circular with flow arrows
                DrawCircle(pixels, CENTER_X, CENTER_Y, 15, baseColor);
                DrawArrow(pixels, CENTER_X - 8, CENTER_Y, CENTER_X + 8, CENTER_Y, coldColor);
            }
            else
            {
                // Generic thermal - temperature symbol
                DrawThermometer(pixels, CENTER_X, CENTER_Y, baseColor, hotColor, coldColor);
            }
        }
        
        private static void DrawLifeSupportIcon(Color[] pixels, EnginePartNodeData partData)
        {
            Color baseColor = partData.color;
            Color accentColor = Color.Lerp(baseColor, Color.white, 0.2f);
            
            if (partData.name.Contains("O₂") || partData.name.Contains("Oxygen"))
            {
                // Draw oxygen symbol - circular with O2
                DrawCircle(pixels, CENTER_X, CENTER_Y, 18, baseColor);
                DrawCircle(pixels, CENTER_X, CENTER_Y, 15, accentColor);
                // Draw "O2" text representation with pixels
                DrawO2Symbol(pixels, CENTER_X, CENTER_Y, Color.black);
            }
            else if (partData.name.Contains("CO₂") || partData.name.Contains("Scrubber"))
            {
                // Draw scrubber - filter pattern
                for (int y = 0; y < 20; y += 4)
                {
                    for (int x = 0; x < 20; x += 4)
                    {
                        DrawRectangle(pixels, CENTER_X - 10 + x, CENTER_Y - 10 + y, 2, 2, baseColor);
                    }
                }
                DrawRectangle(pixels, CENTER_X - 12, CENTER_Y - 12, 24, 24, Color.clear);
                DrawRectangleOutline(pixels, CENTER_X - 12, CENTER_Y - 12, 24, 24, baseColor);
            }
            else if (partData.name.Contains("Filter"))
            {
                // Draw filter - mesh pattern
                DrawMeshPattern(pixels, CENTER_X, CENTER_Y, 20, baseColor);
            }
            else
            {
                // Generic life support - leaf/life symbol
                DrawLeafSymbol(pixels, CENTER_X, CENTER_Y, baseColor);
            }
        }
        
        private static void DrawStructuralIcon(Color[] pixels, EnginePartNodeData partData)
        {
            Color baseColor = partData.color;
            Color accentColor = Color.Lerp(baseColor, Color.white, 0.3f);
            
            if (partData.name.Contains("Hull"))
            {
                // Draw hull plating - hexagonal pattern
                DrawHexagon(pixels, CENTER_X, CENTER_Y, 18, baseColor);
                DrawHexagon(pixels, CENTER_X, CENTER_Y, 15, accentColor);
            }
            else if (partData.name.Contains("Bulkhead"))
            {
                // Draw bulkhead - thick vertical line with brackets
                DrawRectangle(pixels, CENTER_X - 2, CENTER_Y - 20, 4, 40, baseColor);
                DrawRectangle(pixels, CENTER_X - 8, CENTER_Y - 20, 6, 4, baseColor);
                DrawRectangle(pixels, CENTER_X - 8, CENTER_Y + 16, 6, 4, baseColor);
            }
            else if (partData.name.Contains("Vibration") || partData.name.Contains("Dampener"))
            {
                // Draw dampener - spring pattern
                DrawSpringPattern(pixels, CENTER_X, CENTER_Y, baseColor);
            }
            else
            {
                // Generic structural - beam cross
                DrawCross(pixels, CENTER_X, CENTER_Y, 20, 4, baseColor);
            }
        }
        
        private static void DrawPropulsionIcon(Color[] pixels, EnginePartNodeData partData)
        {
            Color baseColor = partData.color;
            Color flameColor = Color.Lerp(baseColor, Color.yellow, 0.6f);
            
            if (partData.name.Contains("Thruster") || partData.name.Contains("Nozzle"))
            {
                // Draw thruster nozzle with flame
                DrawRectangle(pixels, CENTER_X - 8, CENTER_Y - 15, 16, 30, baseColor);
                DrawTriangle(pixels, CENTER_X, CENTER_Y + 15, CENTER_X - 6, CENTER_Y + 25, CENTER_X + 6, CENTER_Y + 25, flameColor);
            }
            else if (partData.name.Contains("FTL") || partData.name.Contains("Drive"))
            {
                // Draw FTL drive - ring with energy
                DrawRing(pixels, CENTER_X, CENTER_Y, 18, 12, baseColor);
                for (int angle = 0; angle < 360; angle += 30)
                {
                    int x = CENTER_X + (int)(15 * Mathf.Cos(angle * Mathf.Deg2Rad));
                    int y = CENTER_Y + (int)(15 * Mathf.Sin(angle * Mathf.Deg2Rad));
                    DrawCircle(pixels, x, y, 2, flameColor);
                }
            }
            else if (partData.name.Contains("Tank"))
            {
                // Draw fuel tank - cylinder
                DrawRectangle(pixels, CENTER_X - 10, CENTER_Y - 18, 20, 36, baseColor);
                DrawRectangleOutline(pixels, CENTER_X - 10, CENTER_Y - 18, 20, 36, Color.Lerp(baseColor, Color.black, 0.3f));
            }
            else
            {
                // Generic propulsion - arrow
                DrawArrow(pixels, CENTER_X - 15, CENTER_Y, CENTER_X + 15, CENTER_Y, baseColor);
            }
        }
        
        private static void DrawNavCommsIcon(Color[] pixels, EnginePartNodeData partData)
        {
            Color baseColor = partData.color;
            Color signalColor = Color.Lerp(baseColor, Color.cyan, 0.4f);
            
            if (partData.name.Contains("Antenna"))
            {
                // Draw antenna - vertical line with radiating signals
                DrawLine(pixels, CENTER_X, CENTER_Y - 20, CENTER_X, CENTER_Y + 20, baseColor);
                for (int i = 1; i <= 3; i++)
                {
                    DrawRing(pixels, CENTER_X, CENTER_Y, i * 8, i * 8 - 2, signalColor);
                }
            }
            else if (partData.name.Contains("Sensor") || partData.name.Contains("Array"))
            {
                // Draw sensor array - grid of dots
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        DrawCircle(pixels, CENTER_X + x * 6, CENTER_Y + y * 6, 2, baseColor);
                    }
                }
            }
            else if (partData.name.Contains("Navigation") || partData.name.Contains("Star"))
            {
                // Draw navigation - compass rose
                DrawCompassRose(pixels, CENTER_X, CENTER_Y, baseColor);
            }
            else
            {
                // Generic nav/comms - radar sweep
                DrawCircle(pixels, CENTER_X, CENTER_Y, 18, baseColor);
                DrawLine(pixels, CENTER_X, CENTER_Y, CENTER_X + 15, CENTER_Y - 8, signalColor);
            }
        }
        
        private static void DrawDataControlIcon(Color[] pixels, EnginePartNodeData partData)
        {
            Color baseColor = partData.color;
            Color dataColor = Color.Lerp(baseColor, Color.green, 0.4f);
            
            if (partData.name.Contains("Memory") || partData.name.Contains("Storage"))
            {
                // Draw memory bank - stacked rectangles
                for (int i = 0; i < 4; i++)
                {
                    DrawRectangle(pixels, CENTER_X - 15, CENTER_Y - 12 + i * 6, 30, 4, baseColor);
                    DrawRectangle(pixels, CENTER_X - 12, CENTER_Y - 11 + i * 6, 24, 2, dataColor);
                }
            }
            else if (partData.name.Contains("Router") || partData.name.Contains("Network"))
            {
                // Draw network - connected nodes
                DrawCircle(pixels, CENTER_X, CENTER_Y, 6, baseColor);
                DrawCircle(pixels, CENTER_X - 15, CENTER_Y - 10, 4, baseColor);
                DrawCircle(pixels, CENTER_X + 15, CENTER_Y - 10, 4, baseColor);
                DrawCircle(pixels, CENTER_X - 15, CENTER_Y + 10, 4, baseColor);
                DrawCircle(pixels, CENTER_X + 15, CENTER_Y + 10, 4, baseColor);
                // Connections
                DrawLine(pixels, CENTER_X, CENTER_Y, CENTER_X - 15, CENTER_Y - 10, dataColor);
                DrawLine(pixels, CENTER_X, CENTER_Y, CENTER_X + 15, CENTER_Y - 10, dataColor);
                DrawLine(pixels, CENTER_X, CENTER_Y, CENTER_X - 15, CENTER_Y + 10, dataColor);
                DrawLine(pixels, CENTER_X, CENTER_Y, CENTER_X + 15, CENTER_Y + 10, dataColor);
            }
            else if (partData.name.Contains("Security") || partData.name.Contains("Firewall"))
            {
                // Draw security - shield with lock
                DrawShieldShape(pixels, CENTER_X, CENTER_Y, baseColor);
                DrawLockSymbol(pixels, CENTER_X, CENTER_Y, dataColor);
            }
            else
            {
                // Generic data - circuit pattern
                DrawCircuitPattern(pixels, CENTER_X, CENTER_Y, baseColor);
            }
        }
        
        private static void DrawManufacturingIcon(Color[] pixels, EnginePartNodeData partData)
        {
            Color baseColor = partData.color;
            Color accentColor = Color.Lerp(baseColor, Color.white, 0.3f);
            
            if (partData.name.Contains("Fabricator"))
            {
                // Draw 3D printer - box with nozzle
                DrawRectangle(pixels, CENTER_X - 15, CENTER_Y - 10, 30, 20, baseColor);
                DrawRectangle(pixels, CENTER_X - 2, CENTER_Y - 18, 4, 8, baseColor);
                DrawRectangle(pixels, CENTER_X - 8, CENTER_Y + 5, 16, 3, accentColor);
            }
            else if (partData.name.Contains("Drone") || partData.name.Contains("Salvage"))
            {
                // Draw drone - small ship shape
                DrawTriangle(pixels, CENTER_X, CENTER_Y - 12, CENTER_X - 10, CENTER_Y + 8, CENTER_X + 10, CENTER_Y + 8, baseColor);
                DrawRectangle(pixels, CENTER_X - 15, CENTER_Y + 3, 8, 3, accentColor);
                DrawRectangle(pixels, CENTER_X + 7, CENTER_Y + 3, 8, 3, accentColor);
            }
            else
            {
                // Generic manufacturing - gear
                DrawGear(pixels, CENTER_X, CENTER_Y, 16, 8, baseColor);
            }
        }
        
        private static void DrawDefenseIcon(Color[] pixels, EnginePartNodeData partData)
        {
            Color baseColor = partData.color;
            Color energyColor = Color.Lerp(baseColor, Color.cyan, 0.5f);
            
            if (partData.name.Contains("Shield"))
            {
                // Draw shield - energy barrier
                DrawHexagon(pixels, CENTER_X, CENTER_Y, 20, baseColor);
                DrawHexagon(pixels, CENTER_X, CENTER_Y, 16, energyColor);
                DrawHexagon(pixels, CENTER_X, CENTER_Y, 12, Color.Lerp(energyColor, Color.white, 0.3f));
            }
            else
            {
                // Generic defense - armor plates
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        DrawRectangle(pixels, CENTER_X - 12 + j * 8, CENTER_Y - 12 + i * 8, 6, 6, baseColor);
                    }
                }
            }
        }
        
        private static void DrawDefaultIcon(Color[] pixels, Color color)
        {
            // Default fallback icon - simple circle
            DrawCircle(pixels, CENTER_X, CENTER_Y, 16, color);
            DrawCircle(pixels, CENTER_X, CENTER_Y, 12, Color.Lerp(color, Color.white, 0.3f));
        }
        
        // Helper drawing methods
        private static void DrawCircle(Color[] pixels, int centerX, int centerY, int radius, Color color)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        SetPixel(pixels, centerX + x, centerY + y, color);
                    }
                }
            }
        }
        
        private static void DrawRectangle(Color[] pixels, int x, int y, int width, int height, Color color)
        {
            for (int px = x; px < x + width; px++)
            {
                for (int py = y; py < y + height; py++)
                {
                    SetPixel(pixels, px, py, color);
                }
            }
        }
        
        private static void DrawRectangleOutline(Color[] pixels, int x, int y, int width, int height, Color color)
        {
            // Top and bottom
            for (int px = x; px < x + width; px++)
            {
                SetPixel(pixels, px, y, color);
                SetPixel(pixels, px, y + height - 1, color);
            }
            // Left and right
            for (int py = y; py < y + height; py++)
            {
                SetPixel(pixels, x, py, color);
                SetPixel(pixels, x + width - 1, py, color);
            }
        }
        
        private static void DrawLine(Color[] pixels, int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int x = x0;
            int y = y0;
            int n = 1 + dx + dy;
            int x_inc = (x1 > x0) ? 1 : -1;
            int y_inc = (y1 > y0) ? 1 : -1;
            int error = dx - dy;
            
            dx *= 2;
            dy *= 2;
            
            for (; n > 0; --n)
            {
                SetPixel(pixels, x, y, color);
                
                if (error > 0)
                {
                    x += x_inc;
                    error -= dy;
                }
                else
                {
                    y += y_inc;
                    error += dx;
                }
            }
        }
        
        // Additional helper methods for complex shapes
        private static void DrawTriangle(Color[] pixels, int x1, int y1, int x2, int y2, int x3, int y3, Color color)
        {
            DrawLine(pixels, x1, y1, x2, y2, color);
            DrawLine(pixels, x2, y2, x3, y3, color);
            DrawLine(pixels, x3, y3, x1, y1, color);
        }
        
        private static void DrawHexagon(Color[] pixels, int centerX, int centerY, int radius, Color color)
        {
            for (int angle = 0; angle < 6; angle++)
            {
                int x1 = centerX + (int)(radius * Mathf.Cos(angle * 60 * Mathf.Deg2Rad));
                int y1 = centerY + (int)(radius * Mathf.Sin(angle * 60 * Mathf.Deg2Rad));
                int x2 = centerX + (int)(radius * Mathf.Cos((angle + 1) * 60 * Mathf.Deg2Rad));
                int y2 = centerY + (int)(radius * Mathf.Sin((angle + 1) * 60 * Mathf.Deg2Rad));
                DrawLine(pixels, x1, y1, x2, y2, color);
            }
        }
        
        private static void DrawRing(Color[] pixels, int centerX, int centerY, int outerRadius, int innerRadius, Color color)
        {
            for (int x = -outerRadius; x <= outerRadius; x++)
            {
                for (int y = -outerRadius; y <= outerRadius; y++)
                {
                    int distSq = x * x + y * y;
                    if (distSq <= outerRadius * outerRadius && distSq >= innerRadius * innerRadius)
                    {
                        SetPixel(pixels, centerX + x, centerY + y, color);
                    }
                }
            }
        }
        
        // Placeholder methods for complex shapes (simplified implementations)
        private static void DrawLightningBolt(Color[] pixels, int centerX, int centerY, Color color)
        {
            DrawLine(pixels, centerX - 5, centerY - 15, centerX + 3, centerY - 5, color);
            DrawLine(pixels, centerX - 2, centerY - 5, centerX + 5, centerY + 15, color);
            DrawLine(pixels, centerX - 8, centerY, centerX + 2, centerY, color);
        }
        
        private static void DrawArrow(Color[] pixels, int x1, int y1, int x2, int y2, Color color)
        {
            DrawLine(pixels, x1, y1, x2, y2, color);
            DrawLine(pixels, x2 - 5, y2 - 3, x2, y2, color);
            DrawLine(pixels, x2 - 5, y2 + 3, x2, y2, color);
        }
        
        private static void DrawThermometer(Color[] pixels, int centerX, int centerY, Color baseColor, Color hotColor, Color coldColor)
        {
            DrawRectangle(pixels, centerX - 2, centerY - 15, 4, 25, baseColor);
            DrawCircle(pixels, centerX, centerY + 12, 6, hotColor);
        }
        
        private static void DrawO2Symbol(Color[] pixels, int centerX, int centerY, Color color)
        {
            // Simplified O2 representation
            DrawRing(pixels, centerX - 4, centerY, 6, 3, color);
            DrawRectangle(pixels, centerX + 2, centerY + 3, 6, 2, color);
            DrawRectangle(pixels, centerX + 2, centerY - 3, 6, 2, color);
            DrawRectangle(pixels, centerX + 4, centerY - 1, 2, 2, color);
        }
        
        private static void DrawMeshPattern(Color[] pixels, int centerX, int centerY, int size, Color color)
        {
            for (int x = -size/2; x <= size/2; x += 4)
            {
                DrawLine(pixels, centerX + x, centerY - size/2, centerX + x, centerY + size/2, color);
            }
            for (int y = -size/2; y <= size/2; y += 4)
            {
                DrawLine(pixels, centerX - size/2, centerY + y, centerX + size/2, centerY + y, color);
            }
        }
        
        private static void DrawLeafSymbol(Color[] pixels, int centerX, int centerY, Color color)
        {
            // Simplified leaf shape
            for (int y = -10; y <= 10; y++)
            {
                int width = 8 - Mathf.Abs(y) / 2;
                DrawLine(pixels, centerX - width/2, centerY + y, centerX + width/2, centerY + y, color);
            }
            DrawLine(pixels, centerX, centerY - 10, centerX, centerY + 10, Color.Lerp(color, Color.black, 0.3f));
        }
        
        private static void DrawCross(Color[] pixels, int centerX, int centerY, int size, int thickness, Color color)
        {
            DrawRectangle(pixels, centerX - size/2, centerY - thickness/2, size, thickness, color);
            DrawRectangle(pixels, centerX - thickness/2, centerY - size/2, thickness, size, color);
        }
        
        private static void DrawSpringPattern(Color[] pixels, int centerX, int centerY, Color color)
        {
            for (int i = 0; i < 6; i++)
            {
                int x = centerX + (i % 2 == 0 ? -5 : 5);
                int y = centerY - 15 + i * 5;
                DrawLine(pixels, x - 3, y, x + 3, y, color);
            }
            DrawLine(pixels, centerX - 5, centerY - 15, centerX + 5, centerY + 15, color);
        }
        
        private static void DrawCompassRose(Color[] pixels, int centerX, int centerY, Color color)
        {
            for (int angle = 0; angle < 360; angle += 45)
            {
                int x = centerX + (int)(15 * Mathf.Cos(angle * Mathf.Deg2Rad));
                int y = centerY + (int)(15 * Mathf.Sin(angle * Mathf.Deg2Rad));
                DrawLine(pixels, centerX, centerY, x, y, color);
            }
            DrawCircle(pixels, centerX, centerY, 3, color);
        }
        
        private static void DrawShieldShape(Color[] pixels, int centerX, int centerY, Color color)
        {
            // Simple shield outline
            DrawLine(pixels, centerX, centerY - 15, centerX - 10, centerY - 5, color);
            DrawLine(pixels, centerX - 10, centerY - 5, centerX - 10, centerY + 5, color);
            DrawLine(pixels, centerX - 10, centerY + 5, centerX, centerY + 15, color);
            DrawLine(pixels, centerX, centerY + 15, centerX + 10, centerY + 5, color);
            DrawLine(pixels, centerX + 10, centerY + 5, centerX + 10, centerY - 5, color);
            DrawLine(pixels, centerX + 10, centerY - 5, centerX, centerY - 15, color);
        }
        
        private static void DrawLockSymbol(Color[] pixels, int centerX, int centerY, Color color)
        {
            DrawRectangle(pixels, centerX - 4, centerY + 2, 8, 6, color);
            DrawRing(pixels, centerX, centerY - 2, 5, 3, color);
        }
        
        private static void DrawCircuitPattern(Color[] pixels, int centerX, int centerY, Color color)
        {
            // Simple circuit lines
            DrawLine(pixels, centerX - 15, centerY, centerX + 15, centerY, color);
            DrawLine(pixels, centerX, centerY - 15, centerX, centerY + 15, color);
            DrawRectangle(pixels, centerX - 3, centerY - 3, 6, 6, color);
        }
        
        private static void DrawGear(Color[] pixels, int centerX, int centerY, int outerRadius, int innerRadius, Color color)
        {
            DrawCircle(pixels, centerX, centerY, innerRadius, color);
            for (int angle = 0; angle < 360; angle += 45)
            {
                int x = centerX + (int)(outerRadius * Mathf.Cos(angle * Mathf.Deg2Rad));
                int y = centerY + (int)(outerRadius * Mathf.Sin(angle * Mathf.Deg2Rad));
                DrawRectangle(pixels, x - 2, y - 2, 4, 4, color);
            }
        }
        
        private static void DrawWavyLine(Color[] pixels, int startX, int startY, int length, Color color)
        {
            for (int x = 0; x < length; x++)
            {
                int y = startY + (int)(3 * Mathf.Sin(x * 0.5f));
                SetPixel(pixels, startX + x, y, color);
            }
        }
        
        private static void SetPixel(Color[] pixels, int x, int y, Color color)
        {
            if (x >= 0 && x < ICON_SIZE && y >= 0 && y < ICON_SIZE)
            {
                pixels[y * ICON_SIZE + x] = color;
            }
        }
    }
}