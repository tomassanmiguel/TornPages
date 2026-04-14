# Torn Pages — Implementation & Technology Specification

*Derived from the Torn Pages Game Design Document. This spec describes what must be built, not how to build it.*

---

## Table of Contents

1. [Technology Stack](#1-technology-stack)
2. [Architecture Overview](#2-architecture-overview)
3. [Data Layer — Game Data](#3-data-layer--game-data)
4. [State Layer — Run State](#4-state-layer--run-state)
5. [Engine Layer](#5-engine-layer)
6. [UI Layer — React Frontend](#6-ui-layer--react-frontend)
7. [Persistence Layer](#7-persistence-layer)
8. [Engine — Data Models in Detail](#8-engine--data-models-in-detail)
9. [Engine — Game Loop & Page State Machine](#9-engine--game-loop--page-state-machine)
10. [Engine — Combat Resolution](#10-engine--combat-resolution)
11. [Engine — Formation System](#11-engine--formation-system)
12. [Engine — Crew & Effect Level](#12-engine--crew--effect-level)
13. [Engine — Event System](#13-engine--event-system)
14. [Engine — Shop System](#14-engine--shop-system)
15. [Engine — Mod & Glitch System](#15-engine--mod--glitch-system)
16. [Engine — Metaprogression & Profiles](#16-engine--metaprogression--profiles)
17. [UI — Component Inventory](#17-ui--component-inventory)
18. [UI — Left Page](#18-ui--left-page)
19. [UI — Right Page Variants](#19-ui--right-page-variants)
20. [UI — Overlays & Modals](#20-ui--overlays--modals)
21. [UI — Inspector & Glossary](#21-ui--inspector--glossary)
22. [UI — Page Viewer & Tearing](#22-ui--page-viewer--tearing)
23. [Text & Localisation System](#23-text--localisation-system)
24. [Audio](#24-audio)
25. [Testing Agent](#25-testing-agent)

---

## 1. Technology Stack

### Runtime
- **Game Engine (C#, headless):** A fully self-contained .NET class library that implements all game rules, state transitions, and data. It has no UI dependency whatsoever. All game logic lives here. This library must be reusable in a future Unity integration without modification.
- **Frontend (React + TypeScript):** A web application that communicates with the headless engine via a thin HTTP or in-process API. It reads state, renders UI, and submits player actions. It contains no game logic.

### API Bridge
The C# engine is hosted as a local ASP.NET Core minimal API. The React frontend communicates with it over HTTP. All endpoints are scoped by profile ID so multiple players can use the server simultaneously with independent state.

**Profile management:**
- `GET /profiles` — list all profiles `[{ profileId, name, hasActiveRun }]`
- `POST /profiles` — create a new profile → `{ profileId }`
- `DELETE /profiles/{profileId}` — delete profile and all its saved data

**Per-profile game endpoints:**
- `GET /profiles/{profileId}/state` — returns the full current `RenderState` for this profile
- `POST /profiles/{profileId}/action` — submits a `PlayerAction`; engine validates, applies, returns updated `RenderState`
- `GET /profiles/{profileId}/history/{pageIndex}` — returns the frozen `RenderState` for a historical page

All communication is JSON. The engine must never accept illegal moves — every action endpoint validates preconditions and returns a structured error if invalid.

**Server-side profile storage:** Each profile is persisted as a JSON file at `profiles/{profileId}.json` relative to the server binary. The API keeps a `Dictionary<string, TornPagesEngine>` in memory, loading from disk on first access and auto-saving after every committed action. Profile IDs are GUIDs.

**Client-side:** The selected `profileId` is stored in `localStorage`. On load, if no profile is stored or the stored ID no longer exists on the server, the client redirects to the profile selection screen.

**No authentication.** Any client that knows a profile ID can access it. This is intentional — the game is designed for shared-machine or small-group play.

### Target Environments
- Dev: engine hosted locally on developer machine; React dev server at `localhost:5173` connects to `localhost:5000`
- Playtest: React app on Vercel (`torn-pages.vercel.app`); engine runs locally exposed via Cloudflare Tunnel (`api.torn-pages.com`)

---

## 2. Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│                  GAME DATA (static)                 │
│  Formation definitions, mod definitions, weapon     │
│  definitions, ability definitions, trait pools,     │
│  name pools, lore strings — all defined in C# code  │
└────────────────────┬────────────────────────────────┘
                     │ read-only
┌────────────────────▼────────────────────────────────┐
│                  ENGINE (C#)                        │
│  Holds RunState, validates actions, applies rules,  │
│  generates RenderState snapshots                    │
└────────┬───────────────────────┬────────────────────┘
         │ RenderState (JSON)    │ PlayerAction (JSON)
┌────────▼───────┐    ┌──────────▼──────────┐
│  GET /state    │    │  POST /action        │
│  GET /history  │    │  (validated by eng.) │
└────────┬───────┘    └──────────────────────┘
         │
┌────────▼───────────────────────────────────────────┐
│                  REACT FRONTEND                    │
│  Reads RenderState, renders UI, emits actions      │
│  Contains NO game logic                            │
└────────────────────────────────────────────────────┘
```

**Critical constraint: The UI layer must never mutate state directly.** All state changes flow through `POST /action`. The UI may compute display-only derived values (e.g. colour thresholds) from `RenderState` but must not infer or guess game state.

---

## 3. Data Layer — Game Data

All game data is defined as static C# classes/records. No JSON files, no scriptable objects, no external data. This enables straightforward LLM-assisted edits to game data.

### 3.1 Definitions Required

#### `FormationDefinition`
- `string Id`
- `string Name`
- `FormationRarity Rarity` — `Starting | Common | Uncommon | Rare`
- `List<SlotDefinition> Slots`
- `FormationPassive? Passive` — nullable; encodes passives like lock-on reduction, no panic, Graveyard bonus, etc.

#### `SlotDefinition`
- `string? ChainId` — links chained slots visually; no mechanical effect currently
- `SlotType Type` — `Empty | Boosted | Pain | Heal | Growth | Fatigue | MinusFatigue | Kill`
- `List<Lane> ConnectedLanes` — `Left | Center | Right`; empty list = no lane (personal effect only)
- `bool IsDouble` — corresponds to `(2x)` notation

#### `WeaponDefinition`
- `string Id`, `string Name`
- `List<WeaponEffect> LaneEffects` — one per lane; effects are typed (Threat, Burn, LockOn, Hack, ChargeDelta, Panic, Morale, etc.)
- `bool RandomiseLanes` — if true, lane assignments are shuffled each time the weapon is used
- `string? SpecialNote` — e.g. "shield doubly effective", "piercing"

#### `EnemyAbilityDefinition`
- `string Id`, `string Name`, `string Description`
- `EnemyAbilityTrigger Trigger` — `Passive | OnExhaust | OnCharge | OnPage | etc.`
- `EnemyAbilityEffect Effect` — typed payload

#### `ModDefinition`
- `string Id`, `string Name`, `string Description`
- `SystemType System` — `Hull | Engine | Computers | Cabin`
- `ModSize Size` — `S | M | L` (S=1 slot, M=2, L=3)
- `bool IsGlitch`
- `ModEffect Effect` — typed effect descriptor

#### `TraitDefinition`
- `string Id`, `string Name`, `string Description`
- `bool IsNegative`
- `TraitEffect Effect` — typed effect descriptor

#### `AbilityDefinition`
- `string Id`
- `AbilityType Type` — `Shield | Hack | Charge | Boost | Barrier | Heal | Fusion`
- `AbilityRarity Rarity` — `Common | Rare`

#### `RaceDefinition`
- `string Id`, `string Name`
- `int BaseHp`
- `StatBlock BonusStats` — flat stat additions
- `RacePassive? Passive` — e.g. Replicant revive, Silent boost, Dwarven nothing (just end bonus)

#### `DeployableDefinition`
- `string Id`, `string Name`, `string Description`
- `DeployableEffect Effect`

#### `EventDefinition`
- `string Id`, `string Name`
- `EventStyle Style` — `Announcement | Option | Shop`
- `int DramaticTension`
- `bool TearingAllowed`
- `bool AlwaysInPool`
- `int? MaxPerChapter`
- `EventEffect Effect`
- `EventConstraint? Constraint` — e.g. requires 2+ crew, requires glitch equipped

#### `LocationDefinition`
- `string Id`, `string Name`
- `string LoreDescription` (TODO for most)
- `EventDefinition? SpecialEvent`

#### `DeployableInstance`
- `string DeployableId` — references a `DeployableDefinition`
- Deployables are **once-ever consumables**. There is no "used" flag on the instance — once a deployable is expended, the instance is removed entirely from `Deployables` in the next committed `LeftPageState`. The delta-safe pattern applies: during a page, `CombatRightPage.DeployablesUsedThisPage` records the ids of expended deployables, and the effective held list is derived by filtering `LeftPageState.Deployables` against that list. On page commit the used entries are permanently dropped.

---

## 4. State Layer — Run State

The run state is the complete, serialisable representation of an in-progress run. It is the ground truth for everything.

### 4.1 `RunState`
Top-level object. Contains:
- `List<PageState> Pages` — all pages played so far, in order
- `RunMetadata Metadata` — run-level flags, chapters removed from pool, difficulty level, RNG seed
- `int CurrentPageIndex`

`RunState` does **not** contain player stats directly — those are derived from the most recent `LeftPageState`.

### 4.2 `PageState`
- `LeftPageState Left`
- `RightPageState Right` — abstract; concrete subtypes below

### 4.3 `LeftPageState`
The complete persistent game state at the moment this page was entered. Immutable once the page is committed.
- `int HullCurrent`, `int HullMax`
- `int Coin`
- `int Morale` (0–100, min 20)
- `List<CrewMemberState> Crew`
- `FormationArchive FormationArchive`
- `ShipSystems Systems`
- `List<ModInstance> EquippedMods` — per system
- `List<ModInstance> ModStorage`
- `List<DeployableInstance> Deployables`
- `int PagesRemaining`
- `int ChapterNumber`, `int ActNumber`
- `RunStats Stats` — cumulative charge, hack, shield generated

### 4.4 `CrewMemberState`
- `string Name`, `string Race`, `string Backstory`
- `StatBlock Stats` — `int Resolve, Intelligence, Charisma, Endurance`
- `int HpCurrent`, `int HpMax`
- `List<TraitInstance> Traits`
- `List<AbilityInstance> Abilities` — 1 or 2
- `int PermanentEffectLevelBonus` — accumulated from Growth slots, Ascension, etc.
- `bool IsDead`

### 4.5 `AbilityInstance`
- `AbilityDefinition Definition`
- `bool IsDual` — if true, raw output halved
- `float EffectMultiplier` — 1.0f normally; 0.5f if dual

### 4.6 `FormationArchive`
- `List<FormationInstance> Formations`
- `string? LastUsedFormationId` — for consecutive-draw exclusion

### 4.7 `FormationInstance`
- `FormationDefinition Definition`
- `List<SlotInstance> Slots` — mirrors definition but with current (possibly modified) slot types
- `List<SlotInstance> OriginalSlots` — frozen copy of the definition slots; used to reset after combat

### 4.8 `ShipSystems`
- `SystemState Engine`, `SystemState Cabin`, `SystemState Hull`, `SystemState Computers`

### 4.9 `SystemState`
- `int Level` (0–3)
- `int ModSlots` — 3 + Level
- `List<ModInstance> InstalledMods`

### 4.10 `RightPageState` subtypes

Each concrete subtype holds:
1. All **generated options** (what was randomly produced for this page)
2. All **player decisions** made so far on this page
3. A `bool IsCommitted` flag set when the player readys/confirms/proceeds

**Critical architectural principle: every state change that occurs on a page must be recorded on that page's `RightPageState`.** This is not limited to the primary gameplay interaction — it includes any secondary action that modifies `LeftPageState` mid-page. The engine derives deltas by comparing the current `LeftPageState` with the cumulative effect of all decisions recorded on `RightPageState`. If a state change is not recorded on `RightPageState`, it cannot be:
- Reflected as a delta in the UI
- Correctly replayed when the player navigates to a historical page
- Correctly reversed when a page is torn

Examples of secondary actions that must be fully captured on `RightPageState`:
- **Crew dismissal** — which crew member was dismissed (triggered by over-capacity from a crew gain event)
- **Deployable use** — which deployable was expended and on which page
- **Mod storage moves** — mods dragged to/from system slots while the mod overlay is open
- **Crew slotting during combat** — which crew is in which slot each page
- **Silent racial passive** — which slot was temporarily boosted (for accurate delta rendering)
- **Awakener passive** — which slot was converted from Fatigue to Boosted
- Any other mid-page sub-action that changes any field on `LeftPageState`

The test for whether something belongs on `RightPageState`: *if this page were rewound and re-rendered from scratch, would leaving out this record produce a different-looking left page?* If yes, it must be recorded.

Subtypes:
- `LocationSelectRightPage` — two `LocationDefinition` options; selected index
- `BuildUpRightPage` — generated event pool; selected event ids
- `EventRightPage` — which event; generated sub-options (coin amount, trait drawn, etc.); player choice; any secondary action (e.g. crew dismissal triggered by gaining a crew member)
- `EnemySelectRightPage` — two locked weapons; three weapon options for choice; three ability options; player selections
- `CombatRightPage` — `CombatState` (see §10); per-page record of crew slot assignments, deployables used, silent passive slot, awakener conversions
- `CombatRewardRightPage` — formation options; selected formation; coin amount; any secondary mod storage moves made while reward overlay is open
- `ChapterConclusionRightPage` — no decisions; just committed flag
- `PrologueBoonRightPage` — three mod options; three formation options; three deployable options; selections; any crew dismissal triggered by starting crew exceeding cabin capacity
- `EpilogueRightPage` — stat snapshot

In general, `RightPageState` subtypes should be designed to be **append-only during a page** — new decisions and sub-actions are added as the player makes them, and the full sequence is preserved. The engine always reconstructs the expected `LeftPageState` for a given page by replaying all decisions on `RightPageState` from the prior page's `LeftPageState`.

---

## 5. Engine Layer

The engine is a C# class library exposing a single entry point: `TornPagesEngine`.

### 5.1 Public Interface

```csharp
public class TornPagesEngine
{
    // Initialise a new run
    public RunState NewRun(int difficultyLevel);

    // Load an existing run
    public RunState LoadRun(string serialisedJson);

    // Serialise current run for saving
    public string SaveRun(RunState state);

    // Get the full render state for the current page
    public RenderState GetRenderState(RunState state);

    // Get the frozen render state for a historical page
    public RenderState GetHistoricalRenderState(RunState state, int pageIndex);

    // Submit a player action; returns updated RunState or throws EngineException
    public RunState ApplyAction(RunState state, PlayerAction action);
}
```

`PlayerAction` is a discriminated union / tagged type with a string `ActionType` and a JSON payload. The engine validates all preconditions before applying. Illegal actions throw `EngineException` with a reason code — the API layer translates these to HTTP 400 responses.

### 5.2 `RenderState`

`RenderState` is the complete view model for the current page. The React frontend renders exclusively from this. It contains:
- `LeftPageRenderModel Left` — current hull, coin, morale, crew cards, systems, mods, formations, page count
- `RightPageRenderModel Right` — typed by current page variant
- `List<DeltaValue> Deltas` — all forecasted changes keyed by field path (e.g. `"hull.current": -5`); used by UI to show flashing delta indicators
- `bool IsCurrentPage` — if false, UI renders in read-only historical mode with solid (non-flashing) deltas
- `List<AuthorInterjection> PendingInterjections` — queue of author speech bubbles to display

### 5.3 Action Types

All player interactions map to one of these action types:

| Action | Payload |
|--------|---------|
| `SelectLocation` | `locationId` |
| `SelectEvent` | `eventId` (toggle) |
| `ReadyBuildUp` | — |
| `EventOptionSelect` | `optionIndex` |
| `EventConfirm` | — |
| `EventContinue` | — |
| `SelectWeapon` | `weaponId` |
| `SelectEnemyAbility` | `abilityId` (toggle) |
| `ConfirmEnemy` | — |
| `SelectFormation` | `formationId` |
| `SlotCrew` | `crewId`, `slotIndex` |
| `UnslotCrew` | `slotIndex` |
| `ReadyUp` | — |
| `UseDeployable` | `deployableId` |
| `SelectReward` | `formationId` |
| `ConfirmReward` | — |
| `Tear` | — |
| `Proceed` | — (chapter conclusion, boss select proceed) |
| `BoonSelect` | `modId`, `formationId`, `deployableId` |
| `SelectBossOption` | `optionIndex` |
| `PrologueCrewSelect` | `crewAttributeType`, `optionIndex` |
| `DismissCrewMember` | `crewId` |
| `MoveModToStorage` | `modId` |
| `MoveModFromStorage` | `modId`, `systemType`, `slotIndex` |
| `SubmitHighScore` | `playerName` |

---

## 6. UI Layer — React Frontend

### 6.1 Core Principles
- Renders exclusively from `RenderState`. No local game logic.
- All user interactions dispatch to `POST /action`.
- Optimistic UI is acceptable for low-stakes interactions (hover effects) but all state changes must await engine confirmation.
- Drag-and-drop (crew to slots, mods to systems) uses the HTML5 drag API or a React DnD library.

### 6.2 Global Layout
The game renders as a two-page book spread. The **left page** is always the persistent state view. The **right page** changes based on the current page type. Below both pages is the **Page Viewer** widget.

```
┌─────────────────┬─────────────────┐
│                 │                 │
│   LEFT PAGE     │   RIGHT PAGE    │
│   (persistent)  │   (page type)   │
│                 │                 │
└─────────────────┴─────────────────┘
│         PAGE VIEWER WIDGET        │
└───────────────────────────────────┘
```

### 6.3 Delta System
`RenderState.Deltas` provides a map of field → delta value. The UI:
- Renders the current value normally
- Overlays the delta value (e.g. `−5` in red, `+8` in green) next to or over the relevant UI element
- Flashes deltas when `IsCurrentPage = true`; renders them solid when `IsCurrentPage = false`
- Deltas update live during the planning phase as crew are slotted/unslotted

---

## 7. Persistence Layer

### 7.1 Save Data Structure
Each save profile contains:
- `RunState? ActiveRun` — null if no run in progress
- `MetaprogressionState Meta` — difficulty level, unlocked modifiers, achievements
- `List<CompletedRunRecord> RunHistory` — lightweight summaries for the bookshelf UI
- `List<HighScoreEntry>? HighScores` — list per difficulty level
- `ProfileSettings Settings` — audio levels, text speed, etc.

**Demo profile policy:** For the demo, any number of profiles may be created (no fixed cap). There is no authentication — any player can select and play as any profile. Profile management (create, select, delete) is accessible from the main menu.

### 7.2 Serialisation
`RunState` serialises to JSON via `System.Text.Json`. The engine exposes `SaveRun`/`LoadRun` methods. The API layer handles file I/O. Save files are written to a local path on the hosting machine.

### 7.3 Auto-save
The engine auto-saves after every committed page transition. The save file is always a valid `RunState` that can be resumed.

---

## 8. Engine — Data Models in Detail

### 8.1 `CombatState`
Contained within `CombatRightPage`. Mutates as the player takes actions during combat.

```
CombatState
├── int PageNumber (within this combat)
├── int ChargeAccumulated
├── int[3] LockOn  (Left, Center, Right)
├── int[3] BurnStacks
├── int[3] BarrierStacks
├── int[3] FusionStacks
├── int[3] PersistentBoostModifier  (for Act III boss debuff etc.)
├── EnemyState Enemy
├── FormationInstance? ActiveFormation
├── string? LastFormationId  (for consecutive draw exclusion this combat)
├── List<SlottedCrew>[3] SlottedByLane
├── List<CombatModifier> ActiveModifiers
├── bool IsPhantomPage
├── bool IsInvulnerablePage
├── List<CombatEvent> EventLog  (for display)
└── bool IsComplete
```

### 8.2 `EnemyState`
- `string Name`
- `List<WeaponDefinition> Weapons` — exactly 3; randomised lane assignments computed at generation
- `List<EnemyAbilityDefinition> ActiveAbilities`
- `bool IsBoss`
- `int? SuperShieldCurrent` (Final Boss only)
- `int FadingCanonUseCount` (for Fading Canon weapon)
- Per-ability state tracking (e.g. Act III boss panic counter)

### 8.3 `SlottedCrew`
- `CrewMemberState Crew`
- `SlotInstance Slot`
- `int SlotIndex`

### 8.4 `CombatModifier`
Represents an active combat boost/penalty for this combat. **Multiple modifiers can be active simultaneously** — one may come from an event, another from a Sabotage Shop purchase, another from an enemy special ability. All are accumulated in `List<CombatModifier> ActiveModifiers` and applied additively or as independent effects as specified by type.
- `CombatModifierType Type`
- `int? Value`
- `bool IsPositive`

The `CombatModifiers` panel on the Combat Page UI must display all currently active modifiers, not just one. The enemy select screen shows how many the player has opted into for this encounter.

---

## 9. Engine — Game Loop & Page State Machine

The run is a linear sequence of pages. The engine maintains a page-type state machine. The current page type determines which actions are valid.

### 9.1 Chapter Structure (pages consumed per chapter)

| Phase | Pages Consumed |
|-------|----------------|
| Location Select | 1 |
| Build Up | 1 |
| Each Event | 1 each |
| Enemy Select | 1 |
| Each Combat Page | 1 each |
| Chapter Conclusion | 1 |

Each tear costs an additional page and must guarantee a different outcome from the torn page.

### 9.2 Page Type Transition Logic

```
START
  └─► PrologueCrewCreation (×3 crew, sequential)
        └─► PrologueBoon
              └─► ChapterLoop (chapters 1–10)
                    └─► LocationSelect
                          └─► BuildUp
                                └─► Events (0–N pages)
                                      └─► EnemySelect
                                            └─► Combat (N pages)
                                                  └─► CombatReward
                                                        └─► ChapterConclusion
                                                              ├─► [if chapter < 10] → LocationSelect (next chapter)
                                                              └─► [if chapter 10 complete] → Epilogue
```

**Run end conditions:**
- `PagesRemaining == 0` → Author interjection ("Oh, dear.") → RunFailed, return to Main Menu
- `Hull == 0` → RunFailed, return to Main Menu
- Epilogue reached → RunSuccess → HighScore prompt → Main Menu

### 9.3 Tear Mechanics
When the player tears a page:
1. Consume 1 page from `PagesRemaining`
2. Re-generate the current page's content with a new RNG seed
3. Guarantee the result differs from the torn result (re-roll if identical, up to N attempts)
4. Replace the current `RightPageState` with the new generation
5. Preserve `LeftPageState` unchanged
6. Reset any planning-phase decisions (unslot crew, deselect formation)

Tearing is disabled on: Chapter Conclusion, Gain/Lose Morale event, Upgrade a System event, Gain/Lose Max HP event.

### 9.4 Location Selection
- Generate 2 random `LocationDefinition` entries from the full pool
- Both options presented; player selects one
- Selected location is stored on `LeftPageState` for the chapter; used to enable location special events

### 9.5 Build Up Generation
Generate the event pool:
1. Always include "Gain a Crew Member" (−1 DT) and "Visit a Shop" (0 DT)
2. Draw additional events from the neutral pool; apply chapter/act/location/condition filters
3. Apply DT pool constraint: for every event E in the pool, `sum(DT of all other events that are positive) >= 4 - DT(E)` must hold. Re-generate until satisfied.
4. Total events = 5 + floor(totalChaRisma / 3), capped at 15
5. Events are shown on the BuildUp page; the player selects a combination reaching DT ≥ 4

### 9.6 Combat Continuation
Combat continues across multiple pages until:
- `ChargeAccumulated >= ChargeThreshold` (success → CombatReward)
- `Hull <= 0` (failure → RunFailed)
- `PagesRemaining <= 0` (failure → RunFailed)

---

## 10. Engine — Combat Resolution

### 10.1 Page Resolution Order

Combat page resolution is split into two phases: **start-of-page** (which runs before the player makes any decisions) and **on-ready-up** (which runs when the player confirms).

**Start-of-page** (runs immediately when a new combat page is entered, before the Planning Phase UI is shown):
1. Apply persistent stack contributions: Burn → adds Threat to each lane; Barrier → adds Shield to each lane; Fusion → adds Charge to each lane
2. Apply Passive Charge: `1 + sum(Intelligence of all living crew)`
3. Apply Silent race passive: randomly boost one slot in the active formation for this page
4. Apply start-of-page combat modifiers (e.g. "random crew member panics")
5. Apply enemy start-of-page abilities
6. Generate enemy intents for this page (see §10.6)

These effects are **committed before the player acts** and are reflected in the initial state of the Planning Phase UI. They cannot be altered by tearing (tearing re-runs all of the above with a new RNG seed).

**Planning Phase** — player slots crew and selects formation. The UI forecasts the outcome of readying up in real time. Some effects are **continuous** — they respond live to the player's current planning choices and update the forecast instantly:
- Counterblaster Threat (scales with forecasted Charge)
- "Generating charge also generates Threat" enemy ability (scales per lane with forecasted Charge)
- Morale multiplier (applies to all forecasted outputs)
- Any combat modifier or passive that reacts to what is slotted (e.g. "Empty slots shield 1" — updates as slots are filled or emptied)

The Planning Phase forecast must clearly distinguish:
- Effects already committed (start-of-page, shown as settled values)
- Effects that will resolve on ready-up (shown as forecasted deltas, flashing)
- Continuous effects whose values depend on current planning choices (updated live)

**On ReadyUp** (resolves in this order):
1. **Shield step** — sum all Shield from crew abilities per lane; subtract from incoming Threat; store remaining Threat
2. **Threat step** — for each lane with remaining unmitigated Threat:
   - Hull takes damage equal to remaining Threat in that lane (clamped by relevant mods/traits)
   - **Every crew member with at least one slot connected to that lane takes 1 damage per slot connected to that lane** (not just the connected slot — all slots the crew member occupies that connect to the overflowed lane each deal 1 damage)
   - Check hull for death
3. **Hack step** — sum all Hack from crew abilities per lane; add to Coin
4. **Charge step** — for each lane: sum all Charge inputs and subtractions; the **per-lane net can go negative**, but only the amount above zero contributes to `ChargeAccumulated`. Specifically: `ChargeAccumulated += max(0, laneChargeNet)`. The running `ChargeAccumulated` total itself is never reduced by the Charge step — negative lane results simply contribute zero. (Separate effects like the Repulsor weapon reduce `ChargeAccumulated` directly, outside of this step.)
5. **Post-page effects** — Echo passive, Awakener passive, Growth slot permanence, fatigue recovery (−1 use-count this page per crew member)

6. **Combat end check** — if `ChargeAccumulated >= ChargeThreshold` → combat ends, proceed to CombatReward; else generate next page

### 10.2 Ability Output Calculation
For a crew member C slotted in slot S connected to lane L:

```
BaseOutput = AbilityBaseMultiplier × EffectLevel(C)
  where AbilityBaseMultiplier = 2 for Common, 1 for Rare
  and EffectLevel(C) = 1 + Resolve + PermanentBonus + TraitModifiers + FormationModifiers + CombatModifiers

if IsDual: BaseOutput = floor(BaseOutput / 2)
if IsDouble slot: fire twice (apply full calculation twice)

LaneBoost = sum of all boost % in lane L (from formation passive, lane-wide boosts, etc.)
SlotBoost = slot boost % (50% if Boosted slot, 0 otherwise)
TotalBoost = LaneBoost + SlotBoost  // additive

FinalOutput = BaseOutput × (1 + TotalBoost / 100)
FinalOutput = FinalOutput × (Morale / 100)  // morale as final multiplier

if CrewTrait "Unaffected by Morale": skip morale multiplier
```

### 10.3 Lock-On Modifier Application
Enemy weapon effects in a lane are multiplied by `(1 + LockOn × 0.2)`. LockOn is clamped 0–5 (default cap). This applies to all per-lane values: Threat amounts, Burn stacks added, even the lock-on increment itself.

### 10.4 Boost Persistence
Lane boost (from crew abilities) does **not** persist between pages. Slot boost (from slot type) also does not persist. The Act III boss's permanent boost debuff is stored in `CombatState.PersistentBoostModifier[lane]` and subtracts from total boost additively. It accumulates with no floor.

### 10.5 Charge Handling
Per-lane Charge net can go negative (e.g. from Repulsor or enemy abilities reducing Charge in a lane). However, only positive lane results contribute to `ChargeAccumulated` — negative lane results contribute zero to the total. The running `ChargeAccumulated` total is only ever reduced by effects that directly target it (e.g. the Repulsor weapon applies `−15/30/45` directly to `ChargeAccumulated`). The "Negative charge disabled in lanes" Engine mod prevents any per-lane Charge reduction from applying, meaning lane Charge cannot go below zero for that combat.

### 10.6 Enemy Intent Generation
At the start of each combat page, the engine:
1. Selects one weapon intent per lane from the enemy's weapon set (no repeats across pages, including after tears)
2. If the weapon has `RandomiseLanes = true`, shuffle the lane assignments
3. Apply lock-on scaling to all effect values
4. Store on `CombatRightPage` as `GeneratedIntents`

Tearing re-runs steps 1–4 and guarantees a different set of intents.

### 10.7 Boss-Specific Logic

**Act I Boss:**
- Track `FullLockOnReached` flag
- Each page: +1 lock-on all lanes; if lock-on reaches 5 anywhere → `FullLockOnReached = true`
- Before full lock-on: NO CHARGE generated by player (engine discards charge step output); M Threat + PAIN in randomised lanes
- PAIN override: all slots connected to affected lanes become Pain type for this page
- At full lock-on: L Threat all lanes (standard)

**Act II Boss:**
- Track `ChargeThreshold50Crossed` flag
- Phase 1 (> 50% threshold remaining): 3 possible intents cycling — Burn 7 one lane + lock-on others; M damage two lanes + lock-on last; −12 Charge all lanes
- Phase 2 (≤ 50%): special scripted sequence (first page idle; subsequent pages: convert Burn to 3× Threat, fill empty lanes with Burn 7, repeat; then alternating XXL / L / Burn 7 pattern)

**Act III Boss:**
- Track `PanicAbilityUseCount` and `PageNumber`
- Odd pages (1, 3, 5…): panic X crew where X = PanicAbilityUseCount++; then M/L/XL Threat in lanes
- Even pages: +1 lock-on + −10% permanent boost modifier per lane (stored in `PersistentBoostModifier`)
- At 50% charge remaining: invulnerable next page

**Final Boss:**
- `SuperShieldCurrent` starts at 100
- While SuperShield > 0: Charge from all sources is redirected to reduce SuperShield; Hack also reduces SuperShield (1:1)
- When SuperShield hits 0: that page generates Charge normally; enemy just does +1 lock-on all lanes
- SuperShield can be repaired by "Repair Super Shield 30" attack
- Attacks: choose one per lane, no repeats per page, from the 8-item attack pool
- Charge threshold: 400 (accumulated only during pages where SuperShield is down)

---

## 11. Engine — Formation System

### 11.1 Formation Archive
- Stored as `List<FormationInstance>` on `LeftPageState`
- Starts with the 4 Starting Formations
- No maximum size
- Each `FormationInstance` carries a `List<SlotInstance>` representing current slot state, and `OriginalSlots` for reset after combat

### 11.2 Drawing Formations for Combat
Each combat page, draw N formations from the archive (N = Computer system level: 2/3/4/5):
1. Exclude the formation used on the immediately preceding page (unless it's the only option)
2. Randomly select N from remaining formations
3. Guarantee drawn set differs from previous page's drawn set (re-roll once if identical)

Formation selected by player is stored in `CombatState.ActiveFormation`.

### 11.3 Slot Modification
- Temporary in-combat modifications: stored on `FormationInstance.Slots`; reset to `OriginalSlots` at combat end
- Permanent modifications (from events): stored on `FormationInstance.Slots`; `OriginalSlots` updated to match

"Adding a slot" = find a qualifying slot (Empty or negative for upgrades; Empty or positive for downgrades) and change its `SlotType`. Only one slot type per slot.

### 11.4 Formation Passives
Formation passives are evaluated before/during ability resolution. They are implemented as typed `FormationPassive` objects on `FormationDefinition`:

| Passive | When Applied |
|---------|-------------|
| `LockOnReduction(lane, amount)` | Start of page, before enemy effects |
| `NoPanic` | Start of page |
| `GraveyardBonus(percent)` | Applied as post-morale multiplier on exhausted crew |
| `CoreBonus(abilityType, percent)` | Applied as additional boost to matching ability type |
| `EchoEffect(chance)` | End of page, schedules partial repeat next page |
| `FullHousePassive` | Empty slots produce Threat per execution pass |
| `PowerColumnBoost` | Boost effectiveness multiplier for this formation |
| `BrainformOverride` | Use Int instead of Res for all slotted crew this page |
| `SmileformOverride` | Use Cha instead of Res for all slotted crew this page |
| `UnifyBonus` | +1 EL per same-race slotted crew |
| `ChampionBonus` | +1 EL per distinct race among slotted crew |
| `AwakenPassive` | Replaces F slots with B slots after each page |
| `PhalanxPassive` | Lock-on cannot increase this page |
| `PhantomPassive` | No damage taken this page; Threat persists |
| `NecropassPassive` | Exhausted slotted crew Hack 15 + Charge 3 per connected lane |
| `MedbayPassive` | H slots boosted for full-health crew |
| `SacrificePassive` | K slot kills crew before abilities fire; others get +5 EL |

---

## 12. Engine — Crew & Effect Level

### 12.1 Effect Level Computation
```
EffectLevel(crew, context) =
  1
  + crew.Stats.Resolve              // base
  + crew.PermanentEffectLevelBonus  // from Growth slots, Ascension
  + TraitModifiers(crew, context)   // see trait list
  + FormationModifiers(context)     // Brainform, Smileform overrides replace Resolve with Int or Cha
  + CombatModifiers(context)        // +1 EL from mods, abilities, start-of-combat bonuses
  + SituationalModifiers(context)   // +EL for low hull, lane, lock-on, etc.
```

If `BrainformOverride` is active: replace `Resolve` term with `Intelligence` in the formula.
If `SmileformOverride` is active: replace `Resolve` with `Charisma`.
If crew has trait "Charisma instead of Resolve determines effect level": replace Resolve with Charisma.

### 12.2 Delta-Safe State Management

**The engine must never mutate `LeftPageState` directly during a planning phase or mid-page interaction.** Any value that could desync between the UI forecast and the committed state must be computed from the recorded decisions on `RightPageState`, not stored as a mutable field that gets incremented or decremented in place.

The general pattern: **instead of mutating a value, record the action that would change it, then derive the current value by replaying actions against the baseline `LeftPageState`.**

Examples of this pattern applied:

**Fatigue:**
- `LeftPageState` stores `CrewMemberState.FatigueBaseline` — the fatigue at the start of the current page (after the previous page's −1 recovery was applied)
- `CombatRightPage` stores `Dictionary<crewId, int> SlotUseCountThisPage` — how many times each crew member has been slotted on this page so far
- Effective current fatigue = `FatigueBaseline + SlotUseCountThisPage[crewId]`
- On ReadyUp, the committed fatigue is written into the next page's `LeftPageState`
- This means the UI can show accurate fatigue and exhaust forecasts live during planning with no risk of desync

**Coin (shops):**
- `LeftPageState` stores `Coin` — the coin total at the start of this page
- `ShopRightPage` stores `List<Purchase> PurchasesThisPage` — each item bought with its cost
- Effective current coin = `Coin - sum(PurchasesThisPage.Select(p => p.Cost))`
- On page commit, the net coin value is written into the next `LeftPageState`

**Deployables:**
- `LeftPageState` stores `List<DeployableInstance> Deployables` — held deployables at the start of this page
- `CombatRightPage` stores `List<string> DeployablesUsedThisPage` — ids of deployables expended
- Effective current deployables = `Deployables where id not in DeployablesUsedThisPage`
- Deployables are **once-ever** — once used, they are gone permanently; they are removed from `Deployables` in the next `LeftPageState`

**Any other value that changes mid-page** (hull, coin, morale, crew HP, etc.) must follow the same pattern: record the action, derive the current value, commit on page transition. No in-place mutation of `LeftPageState` during a page.

### 12.3 Fatigue & Exhaust
- Effective fatigue for crew member C on page P = `C.FatigueBaseline + SlotUseCountThisPage[C.id]`
- C is exhausted if effective fatigue > C.Stats.Endurance
- Exhausted crew cannot be slotted; attempting to do so is an illegal action the engine rejects
- At end of page: new `FatigueBaseline` = `max(0, effectiveFatigue - 1)` (i.e. −1 recovery applied after use-count is locked in)
- "Exhausted crew members recover after 2 pages" Cabin Mod: track pages since exhaust; auto-zero `FatigueBaseline` when threshold reached

### 12.3 Panic
- At start of page: evaluate all panic sources (enemy abilities, traits, combat modifiers)
- Panicked crew: set `IsPanicked = true` for this page only
- Panicked crew cannot be placed in slots
- Panic clears automatically at start of next page

### 12.4 Stat Block Generation (Crew Creation)
For a crew member at chapter C:
- `Level = 8 + C`
- Distribute `Level` points randomly across Int, Cha, Res (minimum 0 per stat, no explicit maximum)
- Apply race stat bonuses on top
- Endurance is set separately (base 0, modified only by race and traits)

### 12.5 Dual Ability Handling
If `IsDual = true`: `BaseOutput = floor(BaseOutput / 2)` applied before any other modifiers.

### 12.6 Charisma Shop Discount
`FinalPrice = BasePrice × (0.99 ^ totalTeamCharisma)` — applied consecutively, floors at 1 coin minimum.

---

## 13. Engine — Event System

### 13.1 Event Pool Generation
1. Start with guaranteed events: "Gain a Crew Member", "Visit a Shop"
2. Add "Visit a Shop" second time if the first generates a different shop type — both shops added as separate entries
3. Draw from neutral event pool filtering by:
   - Act restrictions (none currently defined but framework must support)
   - Condition restrictions (e.g. "requires glitch equipped" for Merkaj)
   - Per-chapter limits (e.g. "Gain/Lose Morale" max 1)
4. Include location special event if defined for the selected location and conditions are met
5. Apply DT pool constraint (see §9.5)
6. Total count: 5 + floor(totalCha / 3), max 15

### 13.2 Event Execution
Each selected event plays out on its own page in the order selected. The engine:
1. Generates sub-options for the event (coin tier, specific mod, trait, formation options, etc.)
2. Presents to UI via `RenderState`
3. Waits for player action(s)
4. On confirm/continue, applies the effect and advances to the next event page (or to EnemySelect after all events)

### 13.3 Event Types — Full Enumeration

Each row below is a **distinct event type** in the `EventDefinition` pool. Flavors that differ only in magnitude or direction are separate types with their own DT values and generation constraints. This table is the authoritative list for implementation.

| Event ID | DT | Style | Effect | Constraints |
|----------|----|-------|--------|-------------|
| `COIN_GAIN_XL` | −2 | Announcement | Add XL coin (110–150) | — |
| `COIN_GAIN_L` | −1 | Announcement | Add L coin (80–110) | — |
| `COIN_GAIN_M` | 0 | Announcement | Add M coin (50–80) | — |
| `COIN_GAIN_XL_COSTLY` | +1 | Announcement | Add XL coin (110–150) | — |
| `COIN_LOSE_S` | +2 | Announcement | Remove S coin (30–50) | Requires coin ≥ 50 |
| `COIN_LOSE_M` | +3 | Announcement | Remove M coin (50–80) | Requires coin ≥ 80 |
| `CREW_GAIN` | −1 | Crew Creation UI | Add one crew member | Always in pool; max once per chapter |
| `CREW_DIES` | +5 | Option | Remove chosen crew member | Requires 2+ crew |
| `SHOP_MOD` | 0 | Shop | Mod shop (6 mods, all systems) | Always in pool |
| `SHOP_DEPLOYABLE` | 0 | Shop | Deployable shop (6 deployables) | — |
| `SHOP_HEALING` | 0 | Shop | Healing shop (hull/morale items) | — |
| `SHOP_MERCENARY` | 0 | Shop | Mercenary shop (6 generated crew) | — |
| `SHOP_SABOTAGE` | 0 | Shop | Sabotage shop (6 combat modifiers) | — |
| `SHOP_UPGRADE` | 0 | Shop | System upgrade shop (per-system costs) | Not offered if all systems max |
| `COMBAT_MODIFIER_POSITIVE` | 0 | Announcement | Apply one positive combat modifier to next combat | Max 1 per chapter |
| `COMBAT_MODIFIER_NEGATIVE` | +2 | Announcement | Apply one negative combat modifier to next combat | Max 1 per chapter |
| `FORMATION_REMOVE` | −1 | Option | Remove one formation from archive | Requires >1 formation |
| `DEPLOYABLE_GAIN` | −1 | Announcement | Add one random deployable | — |
| `SYSTEM_UPGRADE_ENGINE` | −2 | Announcement | Upgrade Engine system | Tearing disabled; max twice |
| `SYSTEM_UPGRADE_HULL` | −2 | Announcement | Upgrade Hull system | Tearing disabled; max twice |
| `SYSTEM_UPGRADE_CABIN` | −2 | Announcement | Upgrade Cabin system | Tearing disabled; max twice |
| `SYSTEM_UPGRADE_COMPUTERS` | −2 | Announcement | Upgrade Computers system | Tearing disabled; max twice |
| `TRAINING_INT` | 0 | Option | +1 Int to chosen crew | Max 3 total Training events per chapter |
| `TRAINING_CHA` | 0 | Option | +1 Cha to chosen crew | Max 3 total Training events per chapter |
| `TRAINING_RES` | 0 | Option | +1 Res to chosen crew | Max 3 total Training events per chapter |
| `TRAINING_END` | −2 | Option | +1 End to chosen crew | Max 3 total Training events per chapter |
| `TRAIT_NEGATIVE` | +3 | Option (on event page) | Assign random negative trait to chosen crew | — |
| `TRAIT_POSITIVE` | −1 | Option (on event page) | Assign random positive trait to chosen crew | — |
| `MORALE_RESTORE_FULL` | −3 | Announcement | Restore morale to 100% | Max 1 morale event per chapter; tearing disabled |
| `MORALE_RESTORE_25` | −1 | Announcement | Restore 25% morale | Max 1 morale event per chapter; tearing disabled |
| `MORALE_LOSE_10` | +1 | Announcement | Reduce morale by 10% | Max 1 morale event per chapter; tearing disabled |
| `MORALE_LOSE_25` | +3 | Announcement | Reduce morale by 25% | Max 1 morale event per chapter; tearing disabled |
| `MOD_GAIN` | −2 | Announcement | Gain one random mod (system-flavored) | Max 1 mod + 1 glitch per chapter |
| `GLITCH_GAIN` | +4 | Announcement | Gain one random glitch (system-flavored) | Max 1 mod + 1 glitch per chapter |
| `FORMATION_UPGRADE` | −1 | Option | Upgrade one slot in chosen formation | Requires eligible formation; max 1 per chapter |
| `FORMATION_DOWNGRADE` | +1 | Option | Downgrade one slot in chosen formation | Requires eligible formation; max 1 per chapter |
| `HULL_MAX_GAIN` | +1 | Announcement | +10 max HP (and current HP) | Max 1 per chapter; tearing disabled |
| `HULL_MAX_LOSE` | −2 | Announcement | −10 max HP | Max 1 per chapter; tearing disabled; requires max HP ≥ 11 |
| `DAMAGE_S` | +1 | Announcement | Deal S damage (4–9) to Hull | Max 1 take damage/heal per chapter |
| `DAMAGE_M` | +2 | Announcement | Deal M damage (10–13) to Hull | Max 1 take damage/heal per chapter |
| `DAMAGE_L` | +3 | Announcement | Deal L damage (14–19) to Hull | Max 1 take damage/heal per chapter |
| `DAMAGE_XL` | +4 | Announcement | Deal XL damage (20–25) to Hull | Max 1 take damage/heal per chapter |
| `HEAL_S` | 0 | Announcement | Heal S HP (4–9) | Max 1 take damage/heal per chapter |
| `HEAL_M` | −1 | Announcement | Heal M HP (10–13) | Max 1 take damage/heal per chapter |
| `HEAL_L` | −2 | Announcement | Heal L HP (14–19) | Max 1 take damage/heal per chapter |
| `HEAL_XL` | −3 | Announcement | Heal XL HP (20–25) | Max 1 take damage/heal per chapter |

**Location special events** (only available at matching location; not drawn from general pool):

| Event ID | Location | DT | Style | Effect |
|----------|----------|----|-------|--------|
| `LOC_DRIFT_BRITTLEBONE_CREW` | The Drift | −1 | Crew Creation UI | Gain Brittlebone crew |
| `LOC_JENSONIA_STAT_COPY` | Jensonia | 0 | Option | Copy one crew member's stats to all others |
| `LOC_MYCELIA_SILENT_CREW` | Mycelia | −1 | Crew Creation UI | Gain Silent crew |
| `LOC_SIGG_STEELBORN_CREW` | Sigg | −1 | Crew Creation UI | Gain Steelborn crew |
| `LOC_MERKAJ_CLEAR_GLITCH` | Merkaj | −2 | Announcement | Clear one random equipped Glitch | Requires ≥1 glitch equipped |
| `LOC_ENJUKAN_REPLICANT_CREW` | Enjukan | −1 | Crew Creation UI | Gain Replicant crew |
| `LOC_ENJUKAN_CONVERT_REPLICANTS` | Enjukan | 0 | Announcement | Convert all crew to Replicants |
| `LOC_VOIDWATCH_RANDOMISE_ABILITIES` | Voidwatch | 0 | Announcement | Reassign all crew abilities randomly |
| `LOC_KHORUM_DWARF_CREW` | Khorum | −1 | Crew Creation UI | Gain Dwarf crew |
| `LOC_KRAETHOS_SYMBIOTE_CREW` | Kraethos | −1 | Crew Creation UI | Gain Symbiote crew |

### 13.4 Location Special Event Effects
- **Gain [Race] crew** — create one crew member with fixed race; all other attributes normally generated; cabin capacity check applies
- **Copy stats (Jensonia)** — option style; player selects source crew; all stats (Int, Cha, Res, End) copied to all other living crew
- **Clear Glitch (Merkaj)** — randomly select one equipped Glitch; remove it; free up its slots
- **Convert to Replicants (Enjukan)** — for each crew: change race to Replicant; remove old racial bonuses; apply Replicant bonuses; update HP max based on race change
- **Randomise Abilities (Voidwatch)** — for each crew: generate new ability as if creating fresh crew (respecting 75/25 common/rare, 70/30 single/dual)

---

## 14. Engine — Shop System

### 14.1 Shop Types

| Shop Type | Inventory Generation |
|-----------|---------------------|
| Mod Shop | 6 mods randomly drawn from all systems and sizes |
| Deployable Shop | 6 deployables randomly drawn from pool, no repeats |
| Healing Shop | Fixed: Heal 1 (8), Heal 5 (35), Heal 15 (75), Heal 30 (140), Heal Full (250), Morale items |
| Mercenary Shop | 6 randomly generated crew members at current chapter level |
| Sabotage Shop | 6 combat modifiers (positive and negative), randomly selected |
| System Upgrade Shop | Up to 4 items: one per system not yet at max level, at randomised costs within range |

### 14.2 Purchase Validation & Delta-Safe Coin Handling
Shop purchases follow the delta-safe state pattern. `LeftPageState.Coin` is the coin total at the start of the page. `ShopRightPage.PurchasesThisPage` records each purchase as `{ itemId, cost }`. Effective current coin at any moment = `LeftPageState.Coin - sum(PurchasesThisPage.Select(p => p.Cost))`. On page commit, this derived value becomes the `Coin` in the next `LeftPageState`.

This means:
- The UI can show an accurate live coin delta as the player browses items
- The "sufficient coin" check uses the derived effective coin, not `LeftPageState.Coin`
- Rewinding to a historical shop page shows the exact coin state before and after each purchase
- Tearing a shop page correctly restores the pre-purchase coin without any mutation

Additional validation rules:
- Apply Charisma discount to displayed and actual cost before checking sufficiency
- Crew dismissal modal if Mercenary purchase exceeds cabin capacity
- Mod auto-storage if mod purchase and no slot available

### 14.3 Pricing
All prices are randomised within the ranges in the Numerical Reference on shop generation. Prices are fixed for the duration of that shop instance. Charisma discount is applied on top.

---

## 15. Engine — Mod & Glitch System

### 15.1 Mod Installation
- Each system has `Level + 3` slots (3 at Lvl 0, up to 5 at Lvl 3)
- S mods occupy 1 slot, M mods occupy 2 slots, L mods occupy 3 slots
- A mod may only be installed if sufficient contiguous (or any) free slots exist

### 15.2 Glitch Force-Equip
When a Glitch is acquired:
1. If target system has free slots: install immediately
2. If no free slots: displace the first non-Glitch mod (send to storage); install Glitch
3. If all slots occupied by Glitches: send new Glitch to mod storage

### 15.3 Mod Effect Application
Mods are passive effects evaluated at the relevant trigger point. They are stored as `ModEffect` typed objects and evaluated by the engine during resolution:
- **Constant passives** (e.g. reduce damage by 1): applied every applicable step
- **Trigger passives** (e.g. "on exhaust, charge 2"): registered as event listeners in the combat resolution pipeline
- **Threshold passives** (e.g. "<25% hull, +1 EL"): checked each page start

---

## 16. Engine — Metaprogression & Profiles

### 16.1 Profile Structure
Three independent save profiles per installation, each with:
- `MetaprogressionState` — current difficulty level, all cumulative modifiers active
- `List<CompletedRunRecord>` — for run history / bookshelf
- `List<HighScoreEntry>` — per difficulty level
- `AchievementState` — which of the 10 achievements have been unlocked
- `ProfileSettings` — audio, text speed, etc.

### 16.2 Difficulty Levels
- Difficulty 0 = baseline
- Each level adds: `ChargeThreshold × 1.10` (cumulative per level) on top of base thresholds
- Difficulty level is selected on a pre-run screen (only levels already unlocked are available)
- Completing a run on level N unlocks level N+1

### 16.3 Achievement Unlock
After a successful run, check: if run difficulty = N and achievement for level N not yet unlocked → unlock it.

### 16.4 High Scores
- Score = pages remaining at run end
- Tracked per difficulty level
- After successful run: prompt for player name; store `{ playerName, score, runDate }`

---

## 17. UI — Component Inventory

The following React components are required:

### Global / Layout
- `BookSpread` — two-page layout container
- `PageViewerWidget` — bottom bar; PrevPage, NextPage, CurrentPage, Tear buttons + page counter
- `AuthorInterjectionOverlay` — speech bubble overlay; renders from `PendingInterjections` queue
- `InspectorOverlay` — right-click modal with name, description, scroll, back button
- `GlossaryChain` — handles chained glossary navigation inside Inspector
- `TooltipWrapper` — hover tooltip for all interactive elements

### Main Menu
- `MainMenuScreen` — title, start, profile selector, settings, run history, stats, achievements
- `ProfileSelector` — toggle/create/delete profiles
- `SettingsOverlay` — tabbed settings panel
- `RunHistoryBookshelf` — scrollable list of completed run summaries
- `AchievementsPanel`
- `StatsPanel` — runs played/won/lost

### Crew
- `CrewCard` — name, effect text, stats (Res/Int/Cha), HP bar (6 rectangles), fatigue/endurance, skull overlay. The effect text must be **dynamically generated** from the crew member's current state and context — it is never a static string. See §18 for rendering rules.
- `CrewCreationUI` — multi-step attribute selection (name, race, backstory, ability, statblock, trait); 3 options each

### Left Page
- `LeftPage` — hull bar, coin, morale, systems (engine/cabin), crew grid, formation viewer
- `HullBar` — colour-coded health bar with delta overlay
- `SystemDisplay` — engine charge value, cabin capacity, computers level, hull max
- `ModSlotRail` — shows installed mods per system; hover to enlarge
- `FormationViewer` — scrollable list of formations in archive; click to select; shows slot diagram
- `FormationDiagram` — renders slots and connections from `FormationInstance`
- `DeployableButton` — opens deployable inventory; shows count of held deployables; visually reflects how many have been used this page via delta indicator
- `ModStoreButton` — opens mod storage overlay

### Right Page Variants
- `LocationSelectPage`
- `BuildUpPage` — event pool display, DT tracker, Ready button
- `EventCard` — shows event name, DT value, summary effect
- `EventAnnouncementPage`
- `EventOptionPage`
- `ShopPage`
- `EnemySelectPage` — two locked weapon cards, one selectable, ability selection panel
- `BossSelectPage` — two boss portrait cards, typewriter passage
- `CombatPage` — enemy panel, lane display, crew slots, formation toggle, Ready button
- `LaneDisplay` — shows incoming effects, lock-on indicator, slot grid, charge/shield/threat values
- `CombatRewardPage` — coin gained, formation options with previews
- `ChapterConclusionPage` — lore text
- `PrologueBoonPage` — mod/formation/deployable selection grids
- `EpilogueScreen` — stats + message of hope
- `HighScoreSubmitScreen`

### Overlays / Modals
- `CrewDismissalModal` — mandatory drag-to-dismiss interface
- `ModStorageOverlay` — scrollable list of stored mods; drag to/from system slots
- `DeployableInventoryOverlay` — list of held deployables with use buttons
- `ConfirmationDialog` — generic yes/no for destructive actions if needed

---

## 18. UI — Left Page

The left page is always visible. It renders from `RenderState.Left` and shows deltas from `RenderState.Deltas`.

### Key Rendering Rules
- **Hull bar**: green > 50%, yellow 25–50%, red < 25%; shows `current/max`; delta flashes as overlay
- **Coin**: numeric display; delta shows as `+N` or `−N` in contrasting colour
- **Morale**: not shown directly on left page (accessible via tooltip or inspector); affects displayed effect values
- **Crew grid**: shows crew cards 2-wide; empty slots labelled "Empty"; slots beyond cabin capacity are blacked out
- **Crew card ability text**: the effect description shown on each crew card must reflect the crew member's **current effective state**, not a static base value. Specifically:
  - Outside combat: show base effect computed from current Effect Level (1 + Res + permanent bonus + traits), including any trait modifiers that are always active (e.g. "+1 effect level for every 100 coin you have" should show the current computed value, not a formula)
  - During combat planning: show the effect as it would resolve *in the current context* — accounting for the active formation's slot type (Boosted slot → show boosted output), lane connectivity (multi-lane slot → note it fires into N lanes), Brainform/Smileform overrides, and any active combat modifiers. This is the value the player should expect to see resolve when they ready up.
  - The `TextManager` (§23) is responsible for generating this string from the ability instance + current modifiers + context. It must be recomputed whenever any modifier changes (slotting into a different slot, switching formation, morale change, etc.)
  - Fatigue and exhaust state are also shown on the card: effective fatigue = `FatigueBaseline + SlotUseCountThisPage`, displayed as `X/Endurance`; exhausted crew show an `X` with a flash if this page's slot choices would cause exhaust
- **Formation viewer**: scrollable; formations are displayed as miniature diagrams; greyed out if used last page; selected one has a visible highlight box
- **Systems**: show numeric values (Engine: starting charge; Cabin: X/Y capacity; Computers: draw count; Hull: max HP); hover for tooltip
- **Mod slots**: rendered as mod card icons within each system row; hover enlarges; empty slots shown as empty rectangles
- **Deployable button**: shows held deployable count (e.g. `[3]`). If any deployables have been used on the current page, a delta indicator shows the reduction (e.g. `−1` flashing in red). When viewing a historical page, the count and any delta render as solid, reflecting the state at that point in time. The button is only interactive on the current page.

---

## 19. UI — Right Page Variants

### 19.1 Location Select
- Two location cards, each showing name + lore blurb (TODO text)
- Selecting one highlights it; enables the Proceed button

### 19.2 Build Up
- Dramatic Tension tracker: `0 / 4` format
- Event cards in a loose grid; colour coded (green = positive DT, yellow = mixed, red = negative)
- Each card shows event name and DT contribution
- Selected events are highlighted with a backdrop
- Ready button greyed while DT < 4

### 19.3 Event Pages
**Announcement:** typewriter lore text; event effect banner at top; Continue button.

**Option:** crew cards if relevant; up to 3 option buttons; lore revealed after selection; Confirm button. Trait events: crew option shown on this page; trait revealed after selection.

**Shop:** grid of 6 items; each item shows name, description (tooltip), cost (with Cha discount applied); Buy button; Exit button. Items bought are greyed out.

### 19.4 Enemy Select
- Two locked weapon cards (fully visible, with names)
- One hidden weapon slot showing `?`; clicking one of 3 option buttons reveals name and locks selection
- Weapon hover: enlarge + tooltip; right-click: Inspector
- Ability selection: 3 ability cards, player toggles 1–3; more selected = more rewards + harder fight
- Attenuation Threshold displayed prominently
- Confirm button after weapon and at least 1 ability selected

### 19.5 Boss Select
- Two boss portrait cards with nameplates
- Selecting one typewriters in a descriptive passage
- Proceed button appears after selection

### 19.6 Combat Page
- **Top**: enemy name, HP equivalent (charge progress bar), current charge vs threshold
- **Enemy intent area**: 3 weapon cards for current page; current weapon highlighted; lock-on indicators per lane (flashing delta)
- **Lane grid** (3 lanes × N slots): crew slots drag-target; slot type visualised; lane-level effects shown as text
- **Crew roster** (on left page during combat): crew cards are draggable to slots
- **Formation toggle**: drawn formation cards at bottom; click to switch active formation
- **Bonus formation effect**: shown if active formation has a passive
- **Ready button**: always visible; becomes active once player has made choices (or immediately if they want to pass)
- **Forecast**: all delta values update live as player slots crew

### 19.7 Combat Reward
- Coin gained displayed at top
- 3 formation cards (or fewer based on enemy abilities selected and Computer mods)
- Clicking a card shows its diagram as preview below
- Select button; confirms on click; unclickable if none selected

### 19.8 Chapter Conclusion
- Lore text (TODO); untearable; single Continue/Proceed button

---

## 20. UI — Overlays & Modals

### 20.1 Crew Dismissal Modal
Triggered automatically when roster exceeds cabin capacity. Blocks interaction with everything else.
- Shows current cabin capacity
- Shows the new (over-capacity) crew card
- A dismiss drop-zone; player drags an existing crew card here
- Confirm button; disabled until a crew member has been dragged to the zone

### 20.2 Mod Storage Overlay
- Full list of stored mods as mod cards; scrollable
- Player can drag mods to/from system mod slots while overlay is open
- Close button

### 20.3 Deployable Inventory Overlay

The deployable inventory is accessible from the `DeployableButton` on the left page. It is an overlay that renders on top of the current right page.

**Display:**
- Up to 3 deployable slots shown (filled or empty)
- Each filled slot shows the deployable's name, a short description, and a Use button
- Each slot also shows its **current state** relative to this page:
  - **Available** — held and not yet used this page; Use button active
  - **Used this page** — the deployable was expended on the current page; slot rendered greyed out with a `USED` indicator (flashing if on the current page, solid if viewing historical); the name and description remain visible for reference
  - **Empty** — slot was already empty at the start of this page; rendered as an empty slot

**Delta behaviour:**
- The `RenderState.Deltas` map includes a delta entry for each deployable expended on the current page (e.g. `"deployables[1]": "used"`)
- The overlay reads these deltas to determine which slots show the `USED` state
- On the current page, used slots flash to signal the pending state change
- On a historical page, used slots render solid — they reflect the committed state at that point

**Use button:**
- Dispatches `UseDeployable` action with the deployable's id
- Only enabled when: on the current page AND within a combat page AND the deployable has not already been used this page AND the "Deployables cannot be used" combat modifier is not active
- After use, the slot immediately transitions to the `USED` state in the overlay; the effect is applied and the `RenderState` updates accordingly
- The Use button is not present when viewing historical pages

**Restrictions:**
- A given deployable can only be used once per combat (enforced by the engine; the Use button is disabled if already used)
- The overlay may be opened at any time on the current combat page, including during the Planning Phase before readying up; using a deployable during planning updates the forecast immediately

**State recording:**
- Every `UseDeployable` action is recorded on the current page's `CombatRightPage` (as part of the append-only decision log — see §4.10) so that rewinding and tearing correctly restores the pre-use state

---

## 21. UI — Inspector & Glossary

### Inspector
Right-clicking any rendered game object opens the Inspector overlay:
- Title (object name)
- Long description (scrollable if needed)
- For Crew: also shows full stat block, all traits, current ability with computed effect values, HP, fatigue
- For Mods/Glitches: shows full effect description and size
- For Weapons: shows full per-lane effects
- Dismiss button (X)
- Back button (←) — only visible if reached via a glossary chain; returns to previous Inspector entry

### Glossary
- A static map of `term → description` maintained in `GameData`
- Terms appearing in any descriptive text are highlighted/underlined
- Right-clicking a highlighted term opens the Inspector showing the glossary entry
- If a glossary entry mentions another term, clicking it opens that term's entry and queues the previous one for back-navigation
- This system must be applied to: event descriptions, mod descriptions, ability descriptions, tooltip text

---

## 22. UI — Page Viewer & Tearing

### Page Viewer Widget
Persistent bottom bar showing:
- `PreviousPage` button — greyed if on page 0
- `NextPage` button — greyed if on most recent page
- `CurrentPage` button — greyed if already on current page (jumps to latest)
- `TEAR` button — only enabled if on current page AND tearing is allowed for this page type
- Page counter: e.g. `37 of 100` (pages used of total)

When viewing a historical page:
- Right page and left page render from the frozen historical `RenderState`
- All deltas render as solid (non-flashing)
- All interactive elements are disabled
- A subtle visual indicator shows "reading historical page" state

### Tear Action
1. Dispatch `Tear` action to engine
2. Engine validates (not on historical page; tearing allowed; pages remaining > 0)
3. Engine decrements pages, re-generates page content, returns new `RenderState`
4. UI re-renders; page counter updates

---

## 23. Text & Localisation System

A `TextManager` class (C# engine side, mirrored in React) handles:
- **Ability description generation** — converts an `AbilityInstance` + current stats + traits into a human-readable effect description (e.g. "Shield 8 — boosted to Shield 10 by trait")
- **Glossary term detection** — scans any rendered string for known glossary terms; returns a list of spans with term links
- **Typewriter sequencing** — timing data for the React typewriter animation (character delay configurable via text speed setting)
- **Lore string lookup** — maps event IDs / location IDs to their lore text variants
- **Icon substitution** — maps inline icon tags (e.g. `[COIN]`, `[CHARGE]`) to rendered icon components in React

Language support: English only for initial implementation. The framework should support future localisation (all strings keyed, no hardcoded UI text).

---

## 24. Audio

Sound effects are triggered by UI events, not by the engine. The React layer fires SFX on:
- Button hover
- Button click (mouse down)
- Crew member slotted
- Crew member unslotted
- Taking damage
- Victory in combat (escaping)
- Crew member death
- Tearing a page
- Shop purchase

Background music:
- Main menu: title music
- In-game: per-act soundtrack (Act I, II, III, IV — four tracks)
- Boss encounters: boss music track (acts as overlay or replacement)
- Final boss: dedicated final boss track
- Settings menu open: apply muted filter to all currently playing music

Volume controls: Master, Music, SFX — stored in `ProfileSettings`. All audio volume is the product of master × category volume.

---

## 25. Testing Agent

A simple automated agent that can play the game end-to-end for testing purposes:

### Interface
The agent interacts exclusively through `POST /action` — the same interface as the real player. It never bypasses the engine.

### Behaviour
- **Action selection:** random valid action selection at each step. The agent asks the engine for the current `RenderState`, identifies all legal actions from the rendered options, and picks one at random.
- **Goal:** complete a run without causing engine exceptions or invalid states. It will "lose" frequently; that's expected.
- **Tear budget:** agent tears pages rarely (e.g. 10% chance when available) to exercise that code path without depleting pages too fast.

### Validation
The agent run is considered successful if:
- No `EngineException` is thrown for any action the agent submits
- The run reaches a terminal state (win or loss) without hanging
- The final `RenderState` is consistent with the terminal condition

The agent can be run in headless mode (no UI) by calling the engine library directly without the HTTP layer.

---

*End of Implementation Specification*

---

## 26. Preliminary Content & Lore Text

This section defines the minimum textual content required for a functional, communicative demo. All text is derived from the Torn Pages Lore Bible. Content is intentionally sparse — sufficient to make the game legible and atmospheric, not exhaustive. Placeholder markers indicate where future writing will expand coverage.

---

### 26.1 Name Pools

The following names are drawn from lore-appropriate conventions for each race. Each pool should contain at least 20 names to avoid obvious repetition across crew generation.

**Human names** (Nomad / GU / Earth stock — warm, diverse, cross-cultural):
Amara, Kesolo, Tivin, Dharue, Orenne, Kava, Pela, Adaeze, Ferrus, Nael, Thord, Brek, Vars, Kraen, Aethis, Silon, Dravmir, Volush, Kesola, Maethis

**Symbiote names** (geological, physical, short and hard):
Kraen, Volush, Dravmir, Aethis, Silon, Greth, Maex, Thoral, Brevin, Kaeld, Oreth, Vulsh, Naemis, Draven, Rethis, Skael, Morven, Braith, Soleth, Kavor

**Replicant names** (abstract concepts, phenomena, qualities — player-chosen identity):
Solace, Meridian, Variance, Constancy, Threshold, Verdant, Axiom, Sequence, Cadence, Lattice, Theorem, Stratum, Inflection, Margin, Liminal, Cipher, Fulcrum, Resonance, Gradient, Manifold

**Brittlebone names** (compound personal + lineage: use first-name only in UI):
Maren, Fen, Sael, Kora, Hollbreck, Duskwall, Ironvault, Dunns, Coldgraft, Gable, Rimvault, Ashframe, Trussdawn, Holleck, Sparwall, Brace, Ferrule, Keyston, Gantry, Corbel

**Dwarf / Builder names** (short, hard consonants):
Thord, Brek, Grukk, Nael, Vars, Ferrus, Drumn, Keld, Stonn, Brak, Gorvek, Hult, Neld, Drav, Torsk, Veld, Krunn, Haeld, Bolten, Grimvek

**Steelborn names** (reference endurance, cold, survival):
Ferrus, Coldhand, Radmark, Flintborn, Steelwick, Greyframe, Frostmark, Irenholm, Hardveld, Coldrun, Rimborn, Ironweld, Greyflint, Steelrun, Frostwall, Coldvault, Hardmark, Flintveld, Greywood, Ironmark

**Silent names** (long, flowing, soft sounds — used in written form only):
Aelomyn, Savaethis, Torvanel, Elovyn, Myrethas, Seloryn, Velomyn, Thaeris, Alorvyn, Savethis, Mylorin, Torvaen, Elaomyn, Selomyn, Vaeloris, Myrevyn, Thovelys, Aelothis, Savomyn, Vorelyn

---

### 26.2 Backstory Pool

Each backstory is one or two sentences. These appear on crew cards and in the inspector. At least one backstory per race is required. Aim for 3–5 per race at minimum.

**Human:**
- "A former GU logistics officer who witnessed the fall of Melflux firsthand. She doesn't talk about what she saw, but she shipped out the next morning."
- "Grew up on a Nomad trade vessel and has visited more systems than most people know exist. The war turned all of his trade routes into warzones."
- "A schoolteacher from Terra who joined the resistance when the Macedonians burned her school. She's better at this than she expected."
- "Ex-Union Navy, dishonorably discharged for refusing a fire order. The officer who gave that order is dead now."
- "A graveyard anchor maintenance tech who spent three years alone with the Anchormaids before a passing ship offered him a better story."

**Symbiote:**
- "From an occupied Kraethos district. She refused the Macedonian recruitment and hasn't been able to go back since."
- "A competition champion who walked away from his title when Macedon bought the league. He has nothing to prove and everything to destroy."
- "One of the few Symbiotes to hold a GU commission. The rest of his unit thinks he's showing off. He is."

**Replicant:**
- "A Wanderer who left Eden nine years ago. Her backup hasn't been refreshed since then. She considers this living dangerously."
- "He worked cybercrime enforcement for the GU until the Macedonian invasion collapsed his department. Now he freelances — same work, no paperwork."
- "She initialized herself with a name meaning 'resolve' and has spent the years since wondering if she chose right."

**Brittlebone:**
- "A structural engineer who evacuated three arcologies before the Battle of the Drift. She made it. Most of her team didn't."
- "Built ships for forty years before deciding to ride one into something dangerous. He's very aware that his bones don't take impacts well."
- "Exiled from the Drift for insubordination. She filed the corrective maintenance report anyway. She'd do it again."

**Dwarf / Builder:**
- "Extraction Company Seven, Sub-unit Fourteen. Currently on an indefinite sabbatical from the company. He finds combat structurally interesting."
- "Defense Company, attached to GU special operations. She has never failed to hold a position that needed holding."
- "He calculated the exact probability of surviving this mission before volunteering. He considers the number acceptable."

**Steelborn:**
- "Her frame carries cold-world modifications that make her visibly uncomfortable in warmer environments. She's never mentioned it once."
- "A former strategic analyst who got tired of watching other people act on his recommendations. He upgraded his combat chassis accordingly."
- "The third Steelborn to serve aboard the Apogee. The other two are gone. She doesn't discuss them."

**Silent:**
- "She communicates through MSL and a small printed notepad for people who don't know the signs. Her handwriting is extraordinary."
- "Left Mdorom after his community decided not to act. He disagreed. That was three years and four ships ago."
- "Nobody on the crew can tell when she's scared. She finds this useful."

---

### 26.3 Location Descriptions

Each location presents two short lines on the selection page — a name and a one-sentence flavour description. These are the minimum required for the location selection UI to be legible.

| Location | Description |
|----------|-------------|
| **Desolate Anchor 003** | A graveyard anchor in deep space, far from any star. Staffed by Anchormaids serving penance. Useful for those who'd rather not be found. |
| **The Drift** | The Brittlebone arcology cluster — an engineering marvel and a war wound. Several arcologies are gone. The rest hold. |
| **Jensonia** | A volcanic world where lava is art and art is everything. The Jensons trade freely with anyone who doesn't bore them. |
| **Mycelia** | The Silent homeworld — dense fungal forests and total quiet, to outside ears. The most well-coordinated people in the galaxy never say a word. |
| **Merkaj** | A busy Macedonian-controlled trade system. This is where the Hyperion devices were built. Nobody here looks comfortable. |
| **Odi** | An asteroid mining operation founded by Replicants, Dwarves, and Steelborn. Efficient, unglamorous, and apparently indestructible. |
| **Jaguan** | A Symbiote colony with a taste for bloodsport. The arena bookings are current. The GU presence is not. |
| **Melflux** | The galaxy's greatest free city, now under occupation. Canal taxis still run. Surveillance drones run with them. |
| **Hermestris** | The Nomad homeworld, amber-skied and occupied. The government cooperates. Most of its people despise them for it. |
| **Sigg** | The Steelborn arctic homeworld — cold, mineral-rich, and quietly ferocious. The Roboticist's Union runs from here. Macedon hasn't tried yet. |
| **Enjukan** | The Replicant homeworld on a rogue planet, hollowed out with server farms. Under Macedonian occupation. The backups are leverage. |
| **Voidwatch** | A research anchor built in orbit of a black hole. The Anchormaids stationed here volunteered for this one. Nobody's sure why. |
| **Dunson 4** | An outer-system way station. Nobody remarkable lives here, which makes it popular with people who'd rather not be remarkable right now. |
| **Khorum** | The Dwarf homeworld — severe, productive, and underground. If it needs building, it was probably built here first. |
| **Macedon** | The empire's heart. Arid, oppressive, and absolute. The Apogee has no business here and every reason to keep moving. |
| **Kraethos** | The Symbiote mountain world. Part free, part occupied, entirely proud. The resistance here has teeth. |
| **Kaimitrio** | A Macedonian server farm system housing the Elysium simulation. The dead are here — sort of. |
| **Luxion** | A dead world. The Star Virus still saturates the surface. The anchor still works. Nobody uses it twice. |
| **Sol** | The fallen heart of the Galactic Union. Earth is rubble. The Hyperion Shield orbits above it all. This is where it ends. |

---

### 26.4 Event Lore Variants

Most events should have 2–3 lore-text variants that vary the narrative framing without changing the mechanical effect. These are the text that typewriters onto the screen in Announcement Style events. Below are minimum viable variants per event.

**Gain XL Coin (−2 DT):**
- *"A salvage signal leads the Apogee to a derelict Nomad freighter. The manifest is unsigned. You pocket the contents and write nothing down."*
- *"An anonymous transfer arrives from a GU sympathiser in occupied Melflux. The amount is larger than expected. You don't ask where it came from."*

**Gain L Coin (−1 DT):**
- *"A graveyard anchor Clerk waves you through without logging the transit — for a fee. You pay it. It's cheaper than paperwork."*
- *"A side job materialises out of nowhere. The work is unpleasant. The coin is satisfactory."*

**Gain M Coin (+0 DT):**
- *"The Apogee's hack team turns a brief window of Macedonian network access into a modest but useful liquidity event."*
- *"A debt comes due. Someone pays it. You note this and move on."*

**Lose S Coin (+2 DT):**
- *"Docking fees. Fuel. The quiet, grinding cost of being in motion. The Apogee was never cheap to run."*
- *"A bribe to a Macedonian checkpoint officer. He takes it. You don't begrudge him."*

**Lose M Coin (+3 DT):**
- *"A supply chain that someone in the crew swore was reliable turns out not to be. The Apogee pays the premium price for last-minute resupply."*
- *"The anchor fee here is extortionate. The Anchormaids explain, without apology, that graveyard anchor operations are expensive."*

**Gain a Crew Member (−1 DT):**
- *"Someone new finds their way to the Apogee. You're not entirely sure how they heard about the mission. You're not sure it matters."*
- *"A distress signal. A survivor. The Apogee changes course."*

**Crew Member Dies (+5 DT):**
- *"Not every chapter ends the way you wrote it. Sometimes the story takes a crew member instead."*

**Combat Modifier — Positive (+0 DT):**
- *"The Apogee's crew runs the numbers one more time. They find an angle the enemy won't expect."*

**Combat Modifier — Negative (+2 DT):**
- *"Something in this system is off. The enemy has found a way to complicate the next engagement."*

**Upgrade System — Engine:**
- *"An engineering team works through the night. The Apogee's engines run quieter now, and faster."*

**Upgrade System — Hull:**
- *"New plating, reinforced bulkheads. The Apogee feels heavier and safer in equal measure."*

**Upgrade System — Cabin:**
- *"The crew quarters are expanded. There's room now for the people who didn't fit before."*

**Upgrade System — Computers:**
- *"Updated targeting algorithms, improved formation analysis. The tactical picture gets clearer."*

**Gain Positive Trait (−1 DT):**
- *"Something changes. A crew member has grown into something they weren't before."*

**Gain Negative Trait (+3 DT):**
- *"The mission takes its toll. A crew member returns from the encounter different — not better."*

**Lose 25% Morale (+3 DT):**
- *"A loss that can't be framed as anything else. The crew knows it. They fly on anyway, because there's nothing else to do."*

**Restore All Morale (−3 DT):**
- *"Something goes right. Not everything — but enough. For a moment, the crew remembers why they're doing this."*

---

### 26.5 Chapter Conclusion Lore

A short paragraph for the Chapter Conclusion page. One variant per chapter is sufficient at this stage. These are untearable and serve as narrative punctuation between chapters.

- **Ch. 1:** *"The Apogee clears the anchor field and the system falls behind them. The next transit window is already calculated. The crew doesn't talk much tonight."*
- **Ch. 2:** *"Another system cleared. Another entry in the log that nobody reads but the author. The Hyperion Shield is still up. The Apogee is still moving."*
- **Ch. 3 (Boss):** *"The encounter is over. The ship needs a moment — so does the crew. Then the next chapter."*
- **Ch. 4–8:** *"The chapter closes. The Apogee moves on. The story is still being written."* *(Placeholder — expand later)*
- **Ch. 9 (Boss):** *"That was the hardest thing some of them have done. Not the last. Sol is close now."*
- **Ch. 10 / Final:** *"The Shield is ahead. The crew is ready. The author writes the last line of the draft."*

---

### 26.6 Enemy Names & Descriptions

Non-boss enemies are modular and unnamed in the abstract — they are described by the author's framing on the Enemy Select page. Weapon and ability tooltips must be readable and lore-grounded.

**Generic enemy descriptors** (used on Enemy Select page, author-voice framing):
- *"A Macedonian patrol vessel — standard complement, standard weapons. Standard doesn't mean safe."*
- *"A hunter-killer frigate running dark. Two locked weapons. The third is your choice."*
- *"A repurposed Nomad trade vessel under Macedonian command. The conversion was hasty. The crew is not."*
- *"A border enforcement unit. It has been told the Apogee does not get through."*
- *"A Macedonian destroyer outrunning its fleet. Alone, but armed for company."*

**Weapon descriptions** (shown in Inspector / tooltip):

| Weapon | Short Description | Long Description |
|--------|------------------|-----------------|
| Left Beam | A directed-energy emitter targeting the ship's port approach. | Fires a sustained beam into the left lane, generating heavy threat and increasing targeting lock-on in flanking lanes. |
| Middle Beam | A spine-mounted cannon aimed directly at the Apogee's core. | Channels maximum threat into the center lane while increasing lock-on on both flanks. |
| Right Beam | A starboard-facing emitter designed to punish evasive manoeuvres. | Mirror of the left beam. Threat right, lock-on flanks. |
| Broad Burn | An incendiary dispersal system. Burns everything. | Applies burn stacks to all three lanes simultaneously. The fire persists. |
| Lock On | A targeting computer building a firing solution across all vectors. | Incrementally increases lock-on across all lanes. At maximum, converts to a devastating all-lane threat burst. |
| Hack | Electronic warfare suite targeting the Apogee's financial systems. | Generates hack effects in one lane while threatening another. Lane assignments vary. |
| Broad Beam | A scatter-fire system covering all approach angles. | Randomised threat distribution across all lanes — unpredictable but consistently dangerous. |
| Panic Ray | A psychological warfare emitter. | Induces panic in crew in the primary lane. Applies lock-on and burn in flanking lanes. Assignments randomised. |
| Repulsor | A fifth-dimensional charge suppressor. | Actively reduces the Apogee's accumulated charge while generating threat. Lane assignments randomised. |
| Shield Piercer | A penetrating munition that bypasses defensive plating. | Applies piercing threat that cannot be shielded in one lane. Standard threat and lock-on in others. |
| Predictable Blast | A high-yield explosive on a clearly telegraphed trajectory. | Enormous threat in the center lane — but the firing solution is known. Shield is twice as effective against it. |
| Counterblaster | A reactive weapons system that turns the Apogee's own output against her. | Generates threat proportional to charge generated this page. Rewards restraint. |
| Long Term Threatener | A slow, methodical system designed for attrition. | Applies burn, builds lock-on, and degrades morale for each crew member caught in its lane. |
| Incinerator | A high-temperature burn weapon. | Stacks significant burn in one lane and lock-on in others. Assignments randomised. |
| Fading Canon | An aging weapon system losing efficiency with each use. | Heavy threat across two lanes on first use. Weakens by 3 threat each subsequent time it fires. |

---

### 26.7 Boss Descriptions

**Act I Boss — "The Gate Warden"**
*Enemy Select description:* "A Macedonian command vessel stationed at the system's anchor approach. It has been here long enough to know every angle of attack. It is waiting."
*Nameplate:* Gate Warden
*Combat text:* "The Gate Warden is building a targeting lock across all vectors. It will not let the Apogee charge until it is sure the fight is already won."

**Act II Boss — "The Patience Engine"**
*Enemy Select description:* "A slow, methodical destroyer running a burn-and-attrition doctrine. It has fought longer battles than this one. It is not in a hurry."
*Nameplate:* Patience Engine
*Combat text (Phase 1):* "The enemy is applying pressure gradually. Fire, lock-on, charge suppression — it builds a situation rather than forcing one."
*Combat text (Phase 2):* "It has decided to finish this. The burn converts. The attacks escalate."

**Act III Boss — "The Collector"**
*Enemy Select description:* "An experimental vessel running an experimental doctrine. It has been collecting data on the Apogee since before this engagement began."
*Nameplate:* The Collector
*Combat text:* "Every crew member who panics makes the next panic worse. The Collector is learning the Apogee's weaknesses in real time."

**Final Boss — "The Hyperion Shield"**
*Enemy Select / Boss Select description:* "This is the end of the draft. The Shield is ahead. Its super-shield prevents charge generation until it is brought down. Hack and charge both reduce it. Then the real fight begins."
*Nameplate:* Hyperion Shield Defense System
*Combat text (Shield up):* "The super-shield is holding. Charge and hack both reduce it. Nothing else matters yet."
*Combat text (Shield down):* "The super-shield is down. Charge now. Everything the defense system has is about to fire."

---

### 26.8 Glossary Terms

The following terms must have glossary entries for the Inspector system. These cover all key mechanics referenced in event text, ability descriptions, and tooltips.

| Term | Entry |
|------|-------|
| Charge | The Apogee's attenuation progress. Accumulate enough to escape through the nearby Anchor. Persists between pages. Cannot go below zero per lane. |
| Threat | Incoming fire. If not fully mitigated by Shield, the Hull takes damage and slotted crew take 1 damage per connected slot. Resets each page unless persistent effects are active. |
| Shield | Crew-generated protection that absorbs Threat before it reaches the Hull. Does not persist between pages. |
| Hack | Electronic intercept of enemy systems. Converts directly to Coin at 1:1. |
| Boost | A percentage increase applied to final effect output. Multiple sources stack additively. Does not persist between pages in lanes. |
| Burn | Stacks applied to lanes that generate 1 Threat per stack at the start of each page. Persists in-lane until cleared. |
| Barrier | Stacks that generate 1 Shield per stack at the start of each page. Persists in-lane. |
| Fusion | Stacks that generate 1 Charge per stack at the start of each page. Persists in-lane. |
| Lock-On | Enemy targeting focus on a lane. Increases the magnitude of all enemy effects in that lane. Caps at 5 by default. |
| Fatigue | Accumulated strain from repeated use in combat. If fatigue exceeds Endurance, the crew member exhausts. |
| Exhaust | A crew member who has exceeded their Endurance limit. Unavailable for the rest of the combat. |
| Panic | A crew member who cannot be slotted this page. Clears at the start of the next page. |
| Morale | The crew's collective resolve, expressed as a percentage. Multiplies all effect output. Minimum 20%. |
| Effect Level | A crew member's combat effectiveness. Equals 1 + Resolve by default. Modified by many sources. |
| Formation | A combat arrangement that determines slot configuration and connectivity. Selected from the Formation Archive each page. |
| Slot | A position in a Formation. Determines which lanes a crew member's ability reaches. |
| Dramatic Tension | A value representing narrative stakes. The Build Up requires at least 4 to proceed. Higher means harder, more interesting events. |
| Tearing | Destroying the current page to rewrite it with different options. Costs one page. The result is always different from what was torn. |
| Anchor | A transit device linking star systems. The Apogee must attenuate to the nearby Anchor to escape combat and move on. |
| The Hyperion Shield | A Macedonian superweapon that blocks Anchor transit. Destroying it is the mission. |
| Coin | Standard galactic currency. Used in shops and for various expenses throughout the run. |
| Deployable | A one-time-use combat resource. Expending it produces an immediate powerful effect. Up to 3 may be carried. |

---

### 26.9 Author Interjections (Extended List)

Beyond the three listed in the design doc, the following interjections cover common gameplay moments. Each is a short, dry, authorial aside in a speech bubble overlay.

| Trigger | Interjection |
|---------|-------------|
| Impossible action (exhausted crew) | *"That won't work."* |
| Apogee facing lethal damage | *"Uh oh."* |
| Crew member dies | *"This will be a gut punch."* |
| Pages drop below 20 | *"We're running out of pages."* |
| Player tries to tear an untearable page | *"This one stays as written."* |
| Tearing a page | *"Let's try that again."* |
| Running out of pages (failure) | *"Oh, dear."* |
| Hull reaches 0 (failure) | *"The Apogee is gone."* |
| Winning a combat | *"That'll do."* |
| Full lock-on reached | *"They have us."* |
| Crew member exhausts | *"They've given everything they have."* |
| Deployable used | *"Emergency measures."* |
| Gaining a new crew member | *"Someone new joins the story."* |
| Entering the final boss | *"This is the last chapter."* |

