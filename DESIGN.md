# Massive‑Ship Engineer — One‑Pager

## Premise & Player Fantasy

Be the ship’s systems engineer on a colossal vessel. Triage failures, cannibalize parts, reroute conduits, and keep mission‑critical systems stable. After every few successful repairs, the narrative advances (events, destinations, new ship areas).

## Platforms & Camera (baseline)

PC/Console. 2D/3D hybrid: strategic “Ship Grid” view + contextual first‑person/over‑the‑shoulder for repair microgames.

## Core Pillars

* **Systems Stewardship:** The ship is a living grid of **nodes** connected by **conduits**; both are resources.
* **Hard Choices:** Parts are scarce; scavenging from one system risks another.
* **Sonic Telemetry:** Each node has a unique **tone**; **degradation lowers pitch**. **Down/overclocking** shifts pitch for tuning & throughput.
* **Cadenced Story:** Complete a set of tasks → unlock the next narrative beat/region/mechanic.

## Player Verbs

Inspect • Trace • Isolate • Reroute • Cannibalize • Replace • Fabricate • Patch • Tune (clock) • Test • Log

## Core Loop

1. **Alert** → 2) **Diagnose** (grid + audio cues) → 3) **Plan** (what to cannibalize/fabricate) → 4) **Acquire** parts → 5) **Execute** swap/reroute → 6) **Tune** clocks & balance loads → 7) **Validate** (metrics + tone) → 8) **Log** & **Advance** story counter.

```
[Alert]→[Diagnose]→[Plan]→[Acquire]→[Execute]→[Tune]→[Validate]→[Story+]
       ↑                                                           ↓
       └───────────────────────────────[Incidents / Decay]─────────┘
```

## Ship Grid Model

* **Nodes (examples):** Reactor, Capacitor, Pump, Scrubber (O₂/CO₂), Radiator, Router (data), Fabricator, Relay, Hydroponics, Shield Emitter.
* **Conduits (types):** Power bus, Coolant line, Data fiber, Atmos duct. Length/grade = capacity & loss. Conduits are craftable/repurposable assets.
* **Constraints:** Distance budgets, heat/pressure, impedance; cross‑talk if routes are too dense.
* **Health & Load:** Each node has **Health**, **Clock/Throughput**, **Temp/Stress**, **Tone** (Hz). Overclock ↑ throughput & pitch; accelerates wear/heat.

## Audio System (Side Mechanic → Core UX)

* **Per‑Node Timbre:** Reactor=low drone, Pump=steady thump, Router=chatter, Radiator=whisper, Scrubber=breathy hiss, Capacitor=pulsed hum.
* **Degradation → Detune:** Wear drifts pitch downward; distortion/noise indicates specific faults.
* **Tuning Microgame:** Match target Hz range; align harmonics across a subnet to reduce losses (set bonuses when subnet is “in key”).
* **Accessibility:** Visual oscilloscope + color bands mirror pitch for non‑audio play.

## Progression & Narrative Cadence

* **Acts by Deck/Domain:** Power → Life Support → Propulsion → Comms/Navigation → Defense → Exotic Systems.
* **Beat Gate:** Every N resolved tasks (or a boss‑scale incident) unlocks new sectors, node types, and ship logs/story scenes.
* **Incidents:** Micrometeor breach, coolant leak cascade, parasitic vine in hydroponics, data loop storm, overclock tax.

## Economy & Resources

* **Parts:** Fuses, Coils, Pumps, Filters, Relays, Heat Sinks, Fiber Spools, Duct Sections, Clock Crystals.
* **Currencies:** Scrap, Fabricator Time, Crew Favors/Access, Energy Credits.
* **Ratings:** Speed, Stability Margin, Collateral Damage, Efficiency (reroute cost). Ratings grant XP/Blueprints/Access Keys.

## Risk & Failure

* **Local vs Global Risk:** Cannibalize to fix now → increases latent failure risk elsewhere.
* **Thresholds:** If any core metric (Power/Life Support/Propulsion/Comms/Shield) drops below X% for Y seconds → **Emergency Mode**; time‑boxed triage tasks spawn.
* **Cascades:** Heat/pressure overloads propagate along conduits; poor tuning amplifies losses.

## Tools & UI

* **Grid HUD:** Heatmap overlays for power/flow/data/atmo; pathfinder suggesting routes with costs.
* **Audio Panel:** Tuner (Hz), spectrogram, harmonic lock indicators, “in‑key” subnet buff.
* **Trace & Tag:** Right‑click trace path; tag nodes for watchlists & alerts.
* **Workbench:** Fabricator queue with recipe dependencies & time.

## Mission Template (Generator‑friendly)

**Cause** (hidden root) → **Effect** (system state change) → **Symptoms** (alerts/tones) → **Constraints** (time, access, scarcity) → **Options** (reroute/cannibalize/fabricate) → **Twist** (secondary failure or narrative beat) → **Resolution** (tuning + validation).

## Difficulty Curve

* Start with single‑system linear fixes → introduce cross‑system trades → multi‑incident juggling → cascading failures with harmonic subnet tuning challenges.

## Session Structure

15–30 min sessions = 2–4 tasks + 1 incident; every 2–3 sessions = major story beat/unlock.

## Differentiators

* **Audio‑as‑Truth:** Read the ship by ear; music theory as systems design.
* **Everything is a Resource:** Even conduits are inventory and map the play space.
* **Meaningful Scarcity:** Cannibalization as core narrative & mechanical choice.

## Stretch Goals / Hooks

Co‑op (comms vs field engineer), Roguelite “Derelict Mode,” Daily seeded ships, Community‑made node types/tones.