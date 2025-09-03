#!/usr/bin/env python3
"""
Script to update ship_parts_block_layouts.json with:
1. Port blocks for parts that are missing them
2. Ship statistics impact rules for each part
3. Part flow classifications (start/middle/end)
"""

import json
import sys
import os

def get_ship_stats_for_category(category):
    """Map part categories to ship statistics they impact"""
    category_stats_map = {
        "power": [
            "Reactor Output", "Capacitor Charge", "Power Bus Utilization", 
            "Blackout Risk Index", "Battery Health (SoH)", "Energy Reserve Hours"
        ],
        "thermal": [
            "Coolant Loop Pressure", "Coolant ΔT", "Radiator Efficiency", 
            "Heat Sink Saturation", "Thermal Hotspot Count"
        ],
        "atmosphere": [
            "O₂ Partial Pressure", "CO₂ Concentration", "Humidity Level", 
            "Airflow Rate", "Filter Saturation", "Scrubber Throughput", "Biohazard Risk Score"
        ],
        "structural": [
            "Hull Stress", "Microfracture Index", "Bulkhead Integrity", 
            "Seal Leakage Rate", "Vibration RMS", "Micrometeor Impact Rate"
        ],
        "propulsion": [
            "Thrust Availability", "Reaction Mass Reserve", "Thruster Alignment Error", 
            "Drive Core Stability", "Propulsion Redundancy Score", "FTL Spool Readiness"
        ],
        "navigation": [
            "Nav Solution Confidence", "Sensor SNR", "Comms Uptime (24h)", 
            "External Packet Loss", "Command Bus Latency", "Array Calibration Drift"
        ],
        "data": [
            "Control Bus Integrity", "Routing Table Health", "Cyber Intrusion Risk", 
            "Command Queue Backlog"
        ],
        "manufacturing": [
            "Fabricator Queue Time", "Blueprint Coverage", "Spare Parts Stock", 
            "Conduit Spool Stock", "Salvage Yield Rate"
        ],
        "defense": [
            "Shield Charge", "Armor Ablation"
        ]
    }
    
    return category_stats_map.get(category, [])

def get_part_flow_classification(part_name, category):
    """Classify parts as start, middle, or end based on their function"""
    
    # Start parts (sources/generators)
    start_parts = [
        "Fusion Reactor Core", "Auxiliary Microreactor", "O2 Generator (Electrolyzer)",
        "Fabricator (Multi-Material Printer)", "Salvage Drone (Autonomous EVA)",
        "Navigation Computer", "Star Tracker Camera", "Sensor Array Receiver (Multi-band)"
    ]
    
    # End parts (consumers/outputs)
    end_parts = [
        "Radiator Panel Array", "Heat Sink Block", "CO2 Scrubber Cartridge",
        "HEPA Filter Cassette", "Dehumidifier Unit", "UV Sterilizer / Biofilter",
        "Main Thruster Nozzle", "RCS Thruster Quad", "Shield Emitter Array"
    ]
    
    if part_name in start_parts:
        return "start"
    elif part_name in end_parts:
        return "end"
    else:
        return "middle"

def add_default_ports_if_missing(part):
    """Add default port blocks if a part has empty or missing ports"""
    if not part.get("ports") or len(part.get("ports", [])) == 0:
        coords = part.get("coords", [])
        if not coords:
            return
            
        # Find edge coordinates for port placement
        ports = []
        coords_set = set(tuple(coord) for coord in coords)
        
        for coord in coords:
            x, y = coord[0], coord[1]
            # Check if this coordinate is on the edge (has at least one empty adjacent cell)
            adjacent = [(x-1, y), (x+1, y), (x, y-1), (x, y+1)]
            is_edge = any(adj not in coords_set for adj in adjacent)
            
            if is_edge and len(ports) < 4:  # Limit to 4 ports max
                ports.append([x, y])
        
        # If no edge ports found or very few, add some from the existing coords
        if len(ports) < 2:
            ports = coords[:min(4, len(coords))]
        
        part["ports"] = ports
        part["port_count"] = len(ports)

def update_ship_parts_json():
    """Main function to update the JSON file"""
    
    json_path = "Assets/ship_parts_block_layouts.json"
    
    if not os.path.exists(json_path):
        print(f"Error: {json_path} not found")
        return False
    
    try:
        with open(json_path, 'r', encoding='utf-8') as f:
            data = json.load(f)
    except Exception as e:
        print(f"Error reading JSON: {e}")
        return False
    
    if "parts" not in data:
        print("Error: 'parts' key not found in JSON")
        return False
    
    parts_updated = 0
    
    for part in data["parts"]:
        part_name = part.get("name", "")
        category = part.get("category", "")
        
        print(f"Processing: {part_name}")
        
        # Add missing port blocks
        add_default_ports_if_missing(part)
        
        # Add ship statistics impact if missing
        if "ship_stats_impact" not in part:
            stats = get_ship_stats_for_category(category)
            
            # Customize stats based on specific part function
            if "Reactor" in part_name or "Microreactor" in part_name:
                stats = ["Reactor Output", "Energy Reserve Hours", "Blackout Risk Index"]
            elif "Capacitor" in part_name or "Battery" in part_name:
                stats = ["Capacitor Charge", "Battery Health (SoH)", "Energy Reserve Hours"]
            elif "Coolant" in part_name or "Heat" in part_name or "Radiator" in part_name:
                stats = ["Coolant Loop Pressure", "Coolant ΔT", "Radiator Efficiency", "Heat Sink Saturation"]
            elif "O2" in part_name or "CO2" in part_name or "Air" in part_name:
                stats = ["O₂ Partial Pressure", "CO₂ Concentration", "Airflow Rate"]
            elif "Hull" in part_name or "Bulkhead" in part_name or "Shield" in part_name:
                stats = ["Hull Stress", "Bulkhead Integrity", "Microfracture Index"]
            elif "Thruster" in part_name or "Drive" in part_name or "FTL" in part_name:
                stats = ["Thrust Availability", "Drive Core Stability", "FTL Spool Readiness"]
            elif "Navigation" in part_name or "Sensor" in part_name or "Antenna" in part_name:
                stats = ["Nav Solution Confidence", "Sensor SNR", "Comms Uptime (24h)"]
            elif "Control" in part_name or "Router" in part_name or "Security" in part_name:
                stats = ["Control Bus Integrity", "Routing Table Health", "Cyber Intrusion Risk"]
            elif "Fabricator" in part_name or "Parts" in part_name or "Salvage" in part_name:
                stats = ["Fabricator Queue Time", "Spare Parts Stock", "Salvage Yield Rate"]
            elif "Shield" in part_name and "defense" in category:
                stats = ["Shield Charge", "Armor Ablation"]
            
            part["ship_stats_impact"] = stats
        
        # Add part flow classification if missing
        if "part_flow" not in part:
            part["part_flow"] = get_part_flow_classification(part_name, category)
        
        parts_updated += 1
    
    # Save updated JSON
    try:
        with open(json_path, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
        
        print(f"\nSuccessfully updated {parts_updated} parts in {json_path}")
        return True
        
    except Exception as e:
        print(f"Error writing JSON: {e}")
        return False

if __name__ == "__main__":
    if update_ship_parts_json():
        print("Ship parts JSON update completed successfully!")
    else:
        print("Ship parts JSON update failed!")
        sys.exit(1)