#!/usr/bin/env python3
"""
Script to match ship parts between code (EnginePartCatalog) and JSON file.
Finds exact matches and similar matches using string similarity.
"""

import json
import re
from difflib import SequenceMatcher

# Ship parts from EnginePartCatalog.cs (extracted from the code)
CODE_SHIP_PARTS = [
    # Power & Energy (7 parts)
    "Fusion Reactor Core",
    "Auxiliary Microreactor", 
    "Capacitor Bank",
    "Battery Rack",
    "Power Inverter/Rectifier Unit",
    "Overclock Controller (Clock Module)",
    "Bus Tie Breaker / ATS",
    
    # Thermal & Coolant (6 parts)
    "Coolant Pump Assembly",
    "Heat Exchanger",
    "Radiator Panel Array",
    "Heat Sink Block",
    "Phase-Change Reservoir (PCM Tank)",
    "Thermal Bypass/Control Valve",
    
    # Atmosphere & Life Support (8 parts)
    "O₂ Generator (Electrolyzer)",
    "CO₂ Scrubber Cartridge",
    "Air Circulation Blower",
    "HEPA Filter Cassette",
    "Dehumidifier Unit",
    "Humidifier Vaporizer",
    "Pressure Regulator / Relief Valve",
    "UV Sterilizer / Biofilter",
    
    # Structural & Hull (6 parts)
    "Hull Plate Segment (Composite Tile)",
    "Bulkhead Door Actuator",
    "Seal/Gasket Ring Set",
    "Vibration Dampener/Isolator",
    "Microfracture Sensor Mesh",
    "Micrometeor Shield Tile (Whipple)",
    
    # Propulsion & Maneuvering (6 parts)
    "Main Thruster Nozzle",
    "Reaction Mass Tank",
    "Thrust Vector Gimbal",
    "Drive Core Field Coil",
    "FTL Spool/Condenser",
    "RCS Thruster Quad",
    
    # Navigation, Comms & Sensors (7 parts)
    "Navigation Computer",
    "Star Tracker Camera",
    "Inertial Measurement Unit (IMU)",
    "Sensor Array Receiver (Multi-band)",
    "High-Gain Antenna",
    "Quantum/Ka-Band Transceiver",
    "Signal Processing DSP Module",
    
    # Data, Control & Security (5 parts)
    "Control Router / PLC Backplane",
    "Error-Correcting Memory Bank",
    "Security Firewall Appliance",
    "Intrusion Detection Node (IDS)",
    "Redundant Controller (Hot-Standby PLC)",
    
    # Manufacturing, Inventory & Logistics (3 parts)
    "Fabricator (Multi-Material Printer)",
    "Smart Parts Locker (RFID Inventory)",
    "Salvage Drone (Autonomous EVA)",
    
    # Defense & Shielding (2 parts)
    "Shield Emitter Array",
    "Shield Capacitor Bank"
]

def load_json_parts(filename):
    """Load ship part names from JSON file."""
    try:
        with open(filename, 'r', encoding='utf-8') as f:
            data = json.load(f)
        return [part['name'] for part in data.get('parts', [])]
    except Exception as e:
        print(f"Error loading JSON: {e}")
        return []

def similarity(a, b):
    """Calculate similarity ratio between two strings."""
    return SequenceMatcher(None, a.lower(), b.lower()).ratio()

def find_best_match(target, candidates, threshold=0.6):
    """Find the best matching candidate for target string."""
    best_match = None
    best_score = 0
    
    for candidate in candidates:
        score = similarity(target, candidate)
        if score > best_score and score >= threshold:
            best_score = score
            best_match = candidate
            
    return best_match, best_score

def main():
    # Load JSON parts
    json_filename = "Assets/ship_parts_block_layouts.json"
    json_parts = load_json_parts(json_filename)
    
    if not json_parts:
        print("Failed to load JSON parts")
        return
    
    print("=== SHIP PARTS MATCHING ANALYSIS ===\n")
    print(f"Code parts: {len(CODE_SHIP_PARTS)}")
    print(f"JSON parts: {len(json_parts)}")
    print()
    
    exact_matches = []
    similar_matches = []
    no_matches = []
    
    # Find matches for each code part
    for code_part in CODE_SHIP_PARTS:
        if code_part in json_parts:
            exact_matches.append((code_part, code_part))
        else:
            # Look for similar match
            best_match, score = find_best_match(code_part, json_parts)
            if best_match:
                similar_matches.append((code_part, best_match, score))
            else:
                no_matches.append(code_part)
    
    # Display results
    print("=== EXACT MATCHES ===")
    for code_part, json_part in exact_matches:
        print(f"✓ {code_part}")
    
    print(f"\n=== SIMILAR MATCHES (similarity >= 0.6) ===")
    for code_part, json_part, score in similar_matches:
        print(f"~ {code_part} → {json_part} ({score:.2f})")
    
    print(f"\n=== NO MATCHES FOUND ===")
    for code_part in no_matches:
        print(f"✗ {code_part}")
    
    # Show JSON parts that don't match any code parts
    matched_json_parts = set([match[1] for match in exact_matches] + [match[1] for match in similar_matches])
    unmatched_json_parts = set(json_parts) - matched_json_parts
    
    print(f"\n=== JSON PARTS WITH NO CODE EQUIVALENT ===")
    for json_part in sorted(unmatched_json_parts):
        print(f"? {json_part}")
    
    print(f"\n=== SUMMARY ===")
    print(f"Exact matches: {len(exact_matches)}")
    print(f"Similar matches: {len(similar_matches)}")
    print(f"Code parts with no match: {len(no_matches)}")
    print(f"JSON parts with no code equivalent: {len(unmatched_json_parts)}")
    
    # Generate rename suggestions
    if similar_matches:
        print(f"\n=== RECOMMENDED RENAMES FOR JSON FILE ===")
        for code_part, json_part, score in similar_matches:
            print(f'Rename "{json_part}" → "{code_part}" (similarity: {score:.2f})')

if __name__ == "__main__":
    main()