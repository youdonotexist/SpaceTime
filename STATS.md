### Power & Energy

1. **Reactor Output** — Current generation vs. demand (MW)
2. **Capacitor Charge** — Short-term power buffer (% full)
3. **Power Bus Utilization** — Load vs. rated capacity (%)
4. **Blackout Risk Index** — Probability of bus collapse (0–1)
5. **Battery Health (SoH)** — Long-term storage condition (%)
6. **Overclock Thermal Penalty** — Added temp from overclock (°C)
7. **Energy Reserve Hours** — Hours at current draw (hrs)

### Thermal & Coolant

8. **Coolant Loop Pressure** — System pressure (kPa)
9. **Coolant ΔT** — Inlet–outlet temperature delta (°C)
10. **Radiator Efficiency** — Heat rejection vs. spec (%)
11. **Heat Sink Saturation** — Remaining thermal capacity (%)
12. **Thermal Hotspot Count** — Zones over safe threshold (#)

### Atmosphere & Life Support

13. **O₂ Partial Pressure** — Breathable oxygen (kPa)
14. **CO₂ Concentration** — Carbon dioxide (ppm)
15. **Humidity Level** — Relative humidity (%)
16. **Airflow Rate** — Circulation volume (m³/s)
17. **Filter Saturation** — HEPA/particulate load (%)
18. **Scrubber Throughput** — CO₂ removal vs. demand (%)
19. **Biohazard Risk Score** — Contamination likelihood (0–100)

### Structural & Hull

20. **Hull Stress** — Peak stress vs. yield (%)
21. **Microfracture Index** — Microcracks per area (#/m²)
22. **Bulkhead Integrity** — Structural health of partitions (%)
23. **Seal Leakage Rate** — Pressure loss rate (Pa/s)
24. **Vibration RMS** — Structural vibration magnitude (g RMS)
25. **Micrometeor Impact Rate** — Detected strikes (#/hr)

### Propulsion & Maneuvering

26. **Thrust Availability** — Usable thrust vs. nominal (%)
27. **Reaction Mass Reserve** — Propellant remaining (% or kg)
28. **Thruster Alignment Error** — Vector deviation (deg)
29. **Drive Core Stability** — Core variance from spec (%)
30. **Propulsion Redundancy Score** — N+X coverage (index)
31. **FTL Spool Readiness** — Jump/warp charge state (%)

### Navigation, Comms & Sensors

32. **Nav Solution Confidence** — Certainty of plotted course (%)
33. **Sensor SNR** — Signal-to-noise for key bands (dB)
34. **Comms Uptime (24h)** — External link availability (%)
35. **External Packet Loss** — Data loss to external endpoints (%)
36. **Command Bus Latency** — Control network latency (ms)
37. **Array Calibration Drift** — Sensor/antenna drift (ppm)

### Data, Control & Security

38. **Control Bus Integrity** — Error-free frames (%)
39. **Routing Table Health** — Reachable nodes (%)
40. **Cyber Intrusion Risk** — Anomaly/attack likelihood (0–100)
41. **Command Queue Backlog** — Pending ops (count)

### Manufacturing, Inventory & Logistics

42. **Fabricator Queue Time** — Time to next part (min)
43. **Blueprint Coverage** — Critical parts with recipes (%)
44. **Spare Parts Stock** — On-hand vs. par (%)
45. **Conduit Spool Stock** — Buildable length (meters)
46. **Salvage Yield Rate** — Usable scrap per mission (% of intake)

### Defense & Shielding

47. **Shield Charge** — Field energy reserve (%)
48. **Armor Ablation** — Cumulative armor wear (% used)

### Crew Health & Ops

49. **Crew Fatigue Index** — Physiological/shift fatigue (0–100)
50. **Crew Morale Index** — Cohesion/resolve (0–100)

If you want, I can turn these into a **data table (CSV/JSON)** with min/max thresholds, alert bands, and which **node types** feed each stat—handy for driving UI heatmaps and incident generation.
