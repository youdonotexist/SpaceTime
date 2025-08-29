### Power & Energy

1. **Fusion Reactor Core** — Reactor Output, Blackout Risk Index, Energy Reserve Hours
2. **Auxiliary Microreactor** — Reactor Output, Redundancy Score, Blackout Risk Index
3. **Capacitor Bank** — Capacitor Charge, Power Bus Utilization, Shield Charge
4. **Battery Rack** — Battery Health (SoH), Energy Reserve Hours, Blackout Risk Index
5. **Power Inverter/Rectifier Unit** — Power Bus Utilization, Command Bus Integrity, Blackout Risk Index
6. **Overclock Controller (Clock Module)** — Overclock Thermal Penalty, Reactor Output, Drive Core Stability
7. **Bus Tie Breaker / ATS** — Blackout Risk Index, Power Bus Utilization, Comms Uptime

### Thermal & Coolant

8. **Coolant Pump Assembly** — Coolant Loop Pressure, Thermal Hotspot Count, Coolant ΔT
9. **Heat Exchanger** — Coolant ΔT, Radiator Efficiency, Heat Sink Saturation
10. **Radiator Panel Array** — Radiator Efficiency, Thermal Hotspot Count, Overclock Thermal Penalty
11. **Heat Sink Block** — Heat Sink Saturation, Thermal Hotspot Count, Hull Stress
12. **Phase-Change Reservoir (PCM Tank)** — Radiator Efficiency, Heat Sink Saturation, Overclock Thermal Penalty
13. **Thermal Bypass/Control Valve** — Coolant Loop Pressure, Coolant ΔT, Thermal Hotspot Count

### Atmosphere & Life Support

14. **O₂ Generator (Electrolyzer)** — O₂ Partial Pressure, Energy Reserve Hours, Crew Morale Index
15. **CO₂ Scrubber Cartridge** — CO₂ Concentration, Scrubber Throughput, Biohazard Risk Score
16. **Air Circulation Blower** — Airflow Rate, Humidity Level, Biohazard Risk Score
17. **HEPA Filter Cassette** — Filter Saturation, Biohazard Risk Score, Airflow Rate
18. **Dehumidifier Unit** — Humidity Level, Biohazard Risk Score, Energy Reserve Hours
19. **Humidifier Vaporizer** — Humidity Level, Crew Morale Index, Biohazard Risk Score
20. **Pressure Regulator / Relief Valve** — Seal Leakage Rate, O₂ Partial Pressure, Bulkhead Integrity
21. **UV Sterilizer / Biofilter** — Biohazard Risk Score, Comms Uptime (crew availability), Crew Morale Index

### Structural & Hull

22. **Hull Plate Segment (Composite Tile)** — Hull Stress, Armor Ablation, Micrometeor Impact Rate
23. **Bulkhead Door Actuator** — Bulkhead Integrity, Seal Leakage Rate, Emergency Mode likelihood
24. **Seal/Gasket Ring Set** — Seal Leakage Rate, Bulkhead Integrity, Biohazard Risk Score
25. **Vibration Dampener/Isolator** — Vibration RMS, Microfracture Index, Sensor SNR
26. **Microfracture Sensor Mesh** — Microfracture Index, Hull Stress, Emergency Mode likelihood
27. **Micrometeor Shield Tile (Whipple)** — Micrometeor Impact Rate, Armor Ablation, Hull Stress

### Propulsion & Maneuvering

28. **Main Thruster Nozzle** — Thrust Availability, Thruster Alignment Error, Drive Core Stability
29. **Reaction Mass Tank** — Reaction Mass Reserve, Thrust Availability, FTL Spool Readiness
30. **Thrust Vector Gimbal** — Thruster Alignment Error, Vibration RMS, Propulsion Redundancy Score
31. **Drive Core Field Coil** — Drive Core Stability, Overclock Thermal Penalty, FTL Spool Readiness
32. **FTL Spool/Condenser** — FTL Spool Readiness, Drive Core Stability, Energy Reserve Hours
33. **RCS Thruster Quad** — Thrust Availability, Propulsion Redundancy Score, Nav Solution Confidence

### Navigation, Comms & Sensors

34. **Navigation Computer** — Nav Solution Confidence, Command Bus Latency, Routing Table Health
35. **Star Tracker Camera** — Nav Solution Confidence, Array Calibration Drift, Sensor SNR
36. **Inertial Measurement Unit (IMU)** — Nav Solution Confidence, Array Calibration Drift, Command Bus Latency
37. **Sensor Array Receiver (Multi-band)** — Sensor SNR, Array Calibration Drift, Cyber Intrusion Risk
38. **High-Gain Antenna** — Comms Uptime (24h), External Packet Loss, Sensor SNR
39. **Quantum/Ka-Band Transceiver** — Comms Uptime (24h), External Packet Loss, Cyber Intrusion Risk
40. **Signal Processing DSP Module** — Sensor SNR, External Packet Loss, Command Bus Latency

### Data, Control & Security

41. **Control Router / PLC Backplane** — Control Bus Integrity, Command Bus Latency, Routing Table Health
42. **Error-Correcting Memory Bank** — Control Bus Integrity, Routing Table Health, Command Queue Backlog
43. **Security Firewall Appliance** — Cyber Intrusion Risk, Comms Uptime (24h), Command Bus Latency
44. **Intrusion Detection Node (IDS)** — Cyber Intrusion Risk, Control Bus Integrity, Command Queue Backlog
45. **Redundant Controller (Hot-Standby PLC)** — Control Bus Integrity, Propulsion Redundancy Score, Blackout Risk Index

### Manufacturing, Inventory & Logistics

46. **Fabricator (Multi-Material Printer)** — Fabricator Queue Time, Blueprint Coverage, Spare Parts Stock
47. **Smart Parts Locker (RFID Inventory)** — Spare Parts Stock, Blueprint Coverage, Fabricator Queue Time
48. **Salvage Drone (Autonomous EVA)** — Salvage Yield Rate, Spare Parts Stock, Biohazard Risk Score

### Defense & Shielding

49. **Shield Emitter Array** — Shield Charge, Armor Ablation, Micrometeor Impact Rate
50. **Shield Capacitor Bank** — Shield Charge, Capacitor Charge, Blackout Risk Index
