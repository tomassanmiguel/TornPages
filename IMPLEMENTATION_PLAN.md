# Torn Pages — Implementation Plan

> **Purpose:** This document is the authoritative implementation plan for the Torn Pages prototype. It is divided into three phases matching the project goals: (1) stack validation, (2) game build-out, and (3) testing infrastructure. Read the spec files in `spec/` for full design details; this plan focuses on *what to build, in what order, and why*.

---

## Guiding Principles (from spec)

- **UI contains zero game logic.** All state mutations flow through `POST /action`. The UI reads `RenderState` and emits `PlayerAction`.
- **Delta-safe state management.** Never mutate `LeftPageState` during planning phases.
- **Append-only decision logs.** `RightPageState` is an immutable record.
- **Validated action system.** Engine throws `EngineException` on illegal moves — never silently ignores.
- **Deterministic.** Same seed + same action sequence = identical run. Critical for testing and tearing.

---

## Phase 1 — Stack Validation ("Does it work?")

**Goal:** Get a minimal React + ASP.NET Core app hosted publicly. Validate round-trip latency, toolchain, and deployment pipeline. No game logic — just infrastructure confidence.

### 1.1 Repository & Project Structure

```
TornPages/
  TornPages.Engine/          # .NET class library — all game logic lives here
  TornPages.Api/             # ASP.NET Core minimal API
  TornPages.Engine.Tests/    # xUnit test project
  client/                    # React + TypeScript (Vite)
  spec/                      # Design documents (already present)
  IMPLEMENTATION_PLAN.md     # This file
```

**Decision rationale:**
- Engine as a separate class library enforces the "no UI dependency" constraint at the compiler level. The API project references it; nothing else can import UI code into it.
- Vite (not CRA) for fast dev server and clean production builds.
- xUnit for C# tests (standard .NET ecosystem choice).

**Steps:**
1. `dotnet new sln -n TornPages`
2. `dotnet new classlib -n TornPages.Engine`
3. `dotnet new webapi -n TornPages.Api --no-openapi` (minimal API template)
4. `dotnet new xunit -n TornPages.Engine.Tests`
5. `dotnet sln add` all three projects; add Engine reference to Api and Tests
6. `npm create vite@latest client -- --template react-ts` in `client/`

### 1.2 API Surface

All game endpoints are scoped under a profile ID. This is the final API surface:

```
GET  /profiles                              → list all profiles (id, name, hasActiveRun)
POST /profiles                              → create profile → { profileId }
DELETE /profiles/{profileId}               → delete profile and all its data

GET  /profiles/{profileId}/state            → RenderState for this profile
POST /profiles/{profileId}/action           → PlayerAction → RenderState
GET  /profiles/{profileId}/history/{n}      → frozen RenderState for page n
```

**Why profile-scoped URLs:** Multiple players can share the same hosted server simultaneously, each with independent run state and metaprogression. The client selects a profile at the main menu and includes the `profileId` in every subsequent request. No authentication — any client can access any profile by ID, which is intentional for this co-op/shared-machine use case.

**Server-side profile storage:** Each profile is a JSON file at `profiles/{profileId}.json` on the server. The API maintains a `Dictionary<string, TornPagesEngine>` in memory, loading from disk on first access and auto-saving after every committed action. Profile IDs are GUIDs generated at creation.

**Client-side:** Selected `profileId` stored in `localStorage`. On load, if no profile is stored or the stored ID no longer exists on the server, redirect to profile selection screen.

For Phase 1, stub the engine with a trivial "ping-pong" run:
- `GET /state` returns a hardcoded `RenderState` with a `message` field
- `POST /action` echoes back a RenderState with a counter increment
- `GET /history/{pageIndex}` returns "not implemented" 404

**Why:** Validates CORS, JSON serialization, routing, and deployment — before any game complexity.

### 1.3 Latency Measurement App (the actual Phase 1 UI)

This is a small React page that:

1. On load, calls `GET /state` and records round-trip time (`performance.now()` before/after `fetch`)
2. Displays current latency in ms prominently
3. Has a "Ping Again" button that fires `POST /action` with `{ type: "Ping" }` and measures response time
4. Tracks a rolling history of the last 10 pings: timestamp, duration, status
5. Displays min/max/average of the session
6. Has a **"Copy Results"** button that copies a formatted summary to clipboard:
   ```
   Torn Pages Latency Test — 2026-04-14 15:32
   Server: [hostname from env or config]
   Pings: 10
   Avg: 142ms | Min: 89ms | Max: 341ms
   Last 10: 341, 120, 89, 105, ...
   ```
7. Shows a color indicator: green (<150ms), yellow (150-400ms), red (>400ms)

**Why this specific design:** The user wants to send the link to a friend who can test and share results. Copy-to-clipboard shareable output achieves this without a backend leaderboard.

### 1.4 CORS & Environment Configuration

- API reads `AllowedOrigins` from `appsettings.json` — not hardcoded
- Client reads `VITE_API_URL` from `.env.local` (dev) and build env var (prod)
- Separate `appsettings.Development.json` and `appsettings.Production.json`

### 1.5 Hosting & Deployment

**Recommended stack (cost-conscious, simple):**
- **API:** Azure App Service (Free tier) or Railway.app (free tier, easier Docker)
- **Client:** Vercel or Netlify (free tier, zero-config for Vite)
- **CI:** GitHub Actions — on push to `main`: build + test C# → deploy API; build React → deploy client

**GitHub Actions workflows to write:**
1. `api-deploy.yml`: `dotnet build` → `dotnet test` → deploy to chosen host
2. `client-deploy.yml`: `npm ci` → `npm run build` → deploy to Vercel/Netlify

**Phase 1 exit criteria:**
- [ ] A friend navigates to the public URL
- [ ] Latency numbers appear and are shareable via copy button
- [ ] `POST /action` round-trip confirmed working
- [ ] All tests pass in CI

---

## Phase 2 — Game Build-Out (piece by piece)

**Strategy:** Build the engine bottom-up (data → state → actions → render), then connect the UI top-down (page shell → page variants → combat). Each milestone produces something playable in the browser.

### 2.1 Engine Foundation

#### Milestone 2.1.1 — Core Data Definitions

All game data as static C# records. No JSON files. No databases. Order:

1. **Enums:** `Race`, `AbilityType`, `SlotType`, `LaneId`, `SystemType`, `EventType`, `PageType`
2. **Ability definitions:** `AbilityDefinition` record (type, base multiplier, is-rare)
3. **Trait definitions:** `TraitDefinition` record — encode every one of the 50+ traits as a typed record with a discriminated union for effect type
4. **Slot definitions:** `SlotDefinition` record (type, connected lanes)
5. **Formation definitions:** `FormationDefinition` record (id, rarity, slots, passive, name)
   - All 35+ formations hard-coded
6. **Mod/Glitch definitions:** `ModDefinition` (system affinity, size, effect type, values)
7. **Deployable definitions:** `DeployableDefinition` (effect type, cost)
8. **Enemy weapon definitions:** `WeaponDefinition` (15 weapons)
9. **Enemy ability definitions:** `EnemyAbilityDefinition` (20+ abilities)
10. **Event definitions:** `EventDefinition` (type, DT value, lore variants, effect parameters)
11. **Location definitions:** `LocationDefinition` (name, special event if any)
12. **Race definitions:** `RaceDefinition` (base HP, stat bonuses, passive if any)

**Critical constraint:** All `*Definition` types are immutable records. They are never instantiated as state — they are looked up by ID from a static registry (`FormationRegistry.Get("Flat")`, etc.).

#### Milestone 2.1.2 — State Types

Implement the full state type hierarchy as defined in the spec:

```csharp
RunState
  └─ List<PageState>
       ├─ LeftPageState (immutable once committed)
       └─ RightPageState (abstract, subtypes per page variant)
            ├─ LocationSelectRightPage
            ├─ BuildUpRightPage
            ├─ EventRightPage
            ├─ EnemySelectRightPage
            ├─ CombatRightPage
            │    └─ CombatState (mutable during page)
            ├─ CombatRewardRightPage
            ├─ ChapterConclusionRightPage
            ├─ PrologueBoonRightPage
            └─ EpilogueRightPage
```

Also: `RenderState`, `RenderModel` subtypes, `DeltaValue`, `PlayerAction` (discriminated union).

**Serialization:** Use `System.Text.Json` with a custom `JsonPolymorphicAttribute` or manual converter for the `RightPageState` hierarchy. Test round-trip serialization of `RunState` as a unit test from day one.

#### Milestone 2.1.3 — Run Initialization

`RunFactory.CreateNewRun(seed, difficulty)`:
1. Generate prologue page
2. Create 3 crew members (sequential, race/stat/ability/trait generation)
3. Set starting values (Hull 80, Coin 100, Pages 200, Morale 100%)
4. Initialize Formation Archive with 4 starting formations
5. Apply seed to all RNG sources

Unit tests: verify stat totals, ability assignment probabilities (rough), race bonuses.

#### Milestone 2.1.4 — Action Dispatcher & Validation

`Engine.ApplyAction(RunState state, PlayerAction action) → RunState`:
- Validate action is legal for current page type and game phase
- Validate preconditions (e.g., `SelectEvent` checks DT constraint)
- Dispatch to handler
- Return new `RunState` (immutable — create new instance, don't mutate)
- Throw `EngineException` with reason code for illegal moves

The `PlayerAction` is a discriminated union. Each variant maps to exactly one handler method.

#### Milestone 2.1.5 — RenderState Generator

`RenderStateGenerator.Generate(RunState, pageIndex) → RenderState`:
- Snapshot `LeftPageState` at the given page index
- Generate appropriate `RightPageRenderModel` from `RightPageState` subtype
- Compute `Deltas` by diffing adjacent `LeftPageState` snapshots
- Mark `IsCurrentPage` flag

This is exercised by `GET /state` and `GET /history/{pageIndex}`.

### 2.2 Prologue

Implement in order:

1. **Crew Generation** — random name (race pool), random stats (Level = 8, distribute across Int/Cha/Res), random ability (70/30 single/dual, 75/25 common/rare), random traits (0-2), backstory lookup by race
2. **Prologue Boon Selection** — draw 1 mod + 1 formation + 1 deployable as options, player selects each via `BoonSelect` action
3. **`PrologueBoonRightPage`** — captures the draws and the selections

UI milestone: Player can complete the prologue and see their starting crew and ship state.

### 2.3 Chapter Structure (Build-Up through Chapter Conclusion)

Implement one chapter's full loop before any combat:

#### Location Select
- Draw 2 random locations from pool (exclude already-visited this run)
- `SelectLocation` action
- Special location events included in event pool if applicable

#### Build-Up Phase
- Generate event pool (5 + floor(Cha/3), capped at 15)
- Apply constraint: pool must allow ≥4 DT to be reached by any single selection path
- `SelectEvent` (toggle), `ReadyBuildUp` actions
- Render: DT tracker, event cards with DT values

#### Event Pages (one at a time)
- Each event type has a handler that:
  1. Resolves the event effect
  2. Modifies `LeftPageState` accordingly
  3. Records everything in `EventRightPage`
- Priority order for implementing events (most mechanically self-contained first):
  1. Coin events (Gain/Lose)
  2. Heal/Damage events
  3. Training events (+stat)
  4. Shop events (Mod Shop, Healing Shop — others follow)
  5. Formation events (Remove, Upgrade, Downgrade)
  6. Morale events
  7. Gain Crew (triggers dismissal if over capacity)
  8. Trait events
  9. System Upgrade events
  10. Deployable events
  11. Glitch events
  12. Combat modifier events
  13. Crew Dies event

**Dismissal Modal:** Triggered when `Crew.Count > Cabin.Capacity`. `DismissCrewMember` action.

#### Enemy Select
- Generate enemy: 3 weapons (2 locked, 1 from options), 1-3 special abilities selectable
- `SelectWeapon`, `SelectEnemyAbility`, `ConfirmEnemy` actions
- Bosses on chapters 3, 6, 9, 10 — skip weapon selection; show boss intro page

#### Chapter Conclusion
- Untearable narrative page
- `Proceed` action only

### 2.4 Combat System (most complex milestone)

Build in strict layers. Each layer is independently unit-tested before the next.

#### Layer 1: Effect Calculation
`EffectCalculator.Calculate(CrewMemberState, SlotInstance, CombatState) → EffectOutput`:
- `BaseOutput = multiplier × EffectLevel`
- EffectLevel = 1 + Resolve + PermanentBonus + TraitMods + FormationMods + CombatMods
- Dual halving, boost application, morale multiplier
- All 50+ trait interactions

Unit test: table-driven tests for every trait modifier combination.

#### Layer 2: Page Execution (deterministic order)
`CombatPageExecutor.Execute(CombatState, SlottedCrewByLane[]) → CombatState`:
1. Start-of-page contributions (Burn→Threat, Barrier→Shield, Fusion→Charge)
2. Passive Charge (1 + sum Int)
3. Silent race passive
4. Start-of-page modifiers
5. Enemy start-of-page abilities
6. Enemy intents generated
7. **Planning phase** (player actions, forecasting)
8. On `ReadyUp`: Shield → Threat → Hack → Charge steps, lane-by-lane
9. Post-page effects (Echo, Awakener, Growth, fatigue recovery −1)
10. Combat end check

#### Layer 3: Formation Drawing
`FormationDrawer.Draw(FormationArchive, int count, string? lastFormationId) → List<FormationInstance>`:
- Exclude `lastFormationId` unless only 1 formation exists
- Guarantee drawn set differs from previous page (re-roll once if identical)
- Tearing redraws, guaranteed different

#### Layer 4: Boss Implementations
Each boss is a separate class implementing `IBossStrategy`:
- `GateWarden` (Ch 3)
- `PatienceEngine` (Ch 6)
- `TheCollector` (Ch 9)
- `HyperionShield` (Ch 10)

Implement `GateWarden` first since it appears earliest.

#### Layer 5: Live Forecasting
`CombatForecaster.Forecast(CombatState, SlottedCrewByLane[]) → ForecastResult`:
- Runs the same calculation as execution but without side effects
- Returns `EffectsByLane`, `ProjectedDamage`, `ProjectedChargeGain`, `ProjectedCoinGain`
- Included in `RenderState` for UI display
- **Critical:** Must be identical to actual execution for delta system to work

#### Combat Reward
- Draw 3 formations (weighted: 70% Common, 20% Uncommon, 10% Rare)
- `SelectReward`, `ConfirmReward` actions
- Coin amount = fixed per act tier + formation rarity bonus

#### UI combat milestones (in order):
1. Static combat page: hardcoded enemy + formation, no drag
2. Formation cycling between pages
3. Crew drag-drop to slots
4. Live forecast overlay updating as crew is slotted
5. Deployable use during combat
6. Page history viewing (frozen past pages)
7. Boss encounters (GateWarden first)

### 2.5 Full Run Loop

With all above in place, a complete run from prologue to chapter 10 should be possible. Final work:

- Chapter progression (advance `ChapterNumber`, regenerate locations/events/enemies at correct level)
- Pages remaining tracking (consumed correctly per page type, +1 per tear)
- Hull-0 and Pages-0 loss conditions
- Win condition (survive Chapter 10 boss)
- Epilogue stats page
- Save/Load: serialize `RunState` to JSON, load from file

### 2.6 Metaprogression & UI Shell

- **Profiles:** 3 profiles per installation, stored in `localStorage` (client) + local file (server, keyed by profile id)
- **Difficulty selection:** pre-run screen, unlocked levels gated by completion
- **High scores:** stored per profile per difficulty; sort by pages remaining
- **Achievements:** unlock on run completion at required difficulty
- **Main menu:** New Run, Continue (if save exists), High Scores, Settings
- **Settings:** audio (placeholder), text speed (typewriter delay), display (placeholder)

### 2.7 Polish & Author Interjections

- Implement all 20+ `AuthorInterjection` triggers (hull critical, no pages left, first tear, etc.)
- Glossary: term detection in all displayed text → hyperlinked inspector entries
- Typewriter effect on narrative text (configurable speed)
- Delta flashing animations
- Color coding (hull bars, coin delta green/red)
- Responsive layout for the two-page book spread

---

## Phase 2 UI — Full Frontend Specification

> This section is the authoritative plan for every UI component. It is deliberately more detailed than the engine milestones above because the UI has many orthogonal concerns (drag-and-drop, live forecasting, delta rendering, overlays, typewriter, inspector chaining) that require careful ordering to avoid rework.

### UI Stack Decisions

- **Vite + React + TypeScript** — already established
- **State management:** React context + `useReducer` for the global `RenderState`. No Redux — the server is the source of truth. The client state is only: current `RenderState`, pending action in-flight flag, historical page index being viewed, and UI overlay state (which modal is open).
- **Drag-and-drop:** `@dnd-kit/core` (modern, accessible, no HTML5 DnD quirks). Used for: crew-to-slot in combat, crew dismiss modal, mod-to-system in mod storage overlay.
- **CSS:** CSS Modules per component. No global utility class framework — the book aesthetic requires tight visual control. Tailwind would work against the custom art direction.
- **Fonts:** Serif font for book content (lore text, right page narrative); monospace or sans for numeric UI. Both configurable via CSS variables.
- **Animations:** CSS transitions for delta flashes, keyframe animations for typewriter; no animation library needed.

---

### UI.1 — Global Architecture

#### File Structure

```
client/src/
  api/
    client.ts          # fetch wrapper: GET /state, POST /action, GET /history/{n}
    types.ts           # TypeScript mirrors of all RenderState types from C#
  components/
    layout/
      BookSpread.tsx
      LeftPage.tsx
      RightPage.tsx
      PageViewerWidget.tsx
    left/
      HullBar.tsx
      CoinDisplay.tsx
      MoraleDisplay.tsx
      SystemDisplay.tsx
      ModSlotRail.tsx
      FormationViewer.tsx
      FormationDiagram.tsx
      DeployableButton.tsx
      ModStoreButton.tsx
      CrewGrid.tsx
      CrewCard.tsx
    right/
      LocationSelectPage.tsx
      BuildUpPage.tsx
        EventCard.tsx
        DramaticTensionTracker.tsx
      EventAnnouncementPage.tsx
      EventOptionPage.tsx
      ShopPage.tsx
        ShopItemCard.tsx
      EnemySelectPage.tsx
        WeaponCard.tsx
        AbilityToggleCard.tsx
      BossSelectPage.tsx
      CombatPage.tsx
        EnemyPanel.tsx
        ChargeBar.tsx
        LaneDisplay.tsx
        SlotGrid.tsx
        SlotCell.tsx
        FormationToggleBar.tsx
        CombatModifiersPanel.tsx
      CombatRewardPage.tsx
      ChapterConclusionPage.tsx
      PrologueBoonPage.tsx
      CrewCreationUI.tsx
      EpilogueScreen.tsx
      HighScoreSubmitScreen.tsx
    overlays/
      AuthorInterjectionOverlay.tsx
      InspectorOverlay.tsx
      CrewDismissalModal.tsx
      ModStorageOverlay.tsx
      DeployableInventoryOverlay.tsx
      ConfirmationDialog.tsx
    menu/
      MainMenuScreen.tsx
      ProfileSelector.tsx
      SettingsOverlay.tsx
      RunHistoryBookshelf.tsx
      AchievementsPanel.tsx
  hooks/
    useGameState.ts      # central hook: holds RenderState, dispatch action, history nav
    useDelta.ts          # reads Deltas map, provides helpers for per-field delta rendering
    useTypewriter.ts     # animates a string character-by-character
    useInspector.ts      # inspector overlay state + chained navigation stack
  text/
    abilityText.ts       # generates human-readable ability description from AbilityInstance
    glossary.ts          # static term→description map; span detection
    iconSubstitution.tsx # replaces [COIN],[CHARGE] tags with icon components
  context/
    GameContext.tsx      # provides RenderState + dispatch to all components
    OverlayContext.tsx   # tracks which overlay is open; prevents multiple simultaneous
```

#### `useGameState` Hook — Central Data Flow

```typescript
// Pseudocode
function useGameState() {
  const [renderState, setRenderState] = useState<RenderState | null>(null);
  const [viewingPageIndex, setViewingPageIndex] = useState<number | null>(null);
  const [isInflight, setIsInflight] = useState(false);

  async function dispatch(action: PlayerAction) {
    setIsInflight(true);
    const next = await apiClient.postAction(action);
    setRenderState(next);
    setViewingPageIndex(null); // return to current page after any action
    setIsInflight(false);
  }

  async function viewPage(index: number) {
    const historical = await apiClient.getHistory(index);
    setRenderState(historical);
    setViewingPageIndex(index);
  }

  return { renderState, dispatch, viewPage, isInflight, isHistoricalView: viewingPageIndex !== null };
}
```

All components receive `dispatch` and `renderState` via context. No component ever calls `fetch` directly — all calls go through `useGameState`.

**Optimistic updates:** Only for drag-and-drop operations in combat (crew slot placement). The forecasted deltas come from `RenderState.Deltas` which updates on every server roundtrip — but for slot drag preview we can display local pending state. The moment the slot drag completes (drop), dispatch `SlotCrew` and the server response confirms or corrects. No other optimistic updates.

---

### UI.2 — Delta System (cross-cutting)

`RenderState.Deltas` is a `Record<string, number | string>` keyed by field path (e.g. `"hull.current": -5`, `"coin": +20`, `"crew[0].hp": -1`, `"deployables[1]": "used"`).

#### `useDelta` Hook

```typescript
function useDelta(fieldPath: string): { value: number | string | null; isFlashing: boolean } {
  const { renderState } = useGameState();
  const delta = renderState?.Deltas[fieldPath] ?? null;
  const isFlashing = renderState?.IsCurrentPage ?? false;
  return { value: delta, isFlashing };
}
```

#### Delta Rendering Rules

Every numeric UI element that can change mid-page uses a `DeltaOverlay` sub-component:

```
[Current Value]  [+8 ▲] (green, flashing)
[Current Value]  [−5 ▼] (red, flashing)
```

- Flashing = CSS keyframe `opacity: 1 → 0.4 → 1` at 1.5s period, while `isFlashing = true`
- Solid (historical page) = static render, no animation
- Zero delta = no overlay rendered
- String deltas (e.g. `"used"`) are handled per-component (deployable overlay shows `USED` badge)

**Fields with delta overlays:**
- Hull current
- Coin
- Pages remaining
- Each crew member HP
- Each crew member fatigue
- Per-lane: Threat, Charge, Shield, Hack, Burn stacks, Barrier stacks, Fusion stacks, Lock-On
- Charge accumulated
- Each deployable slot (used/available state)
- Each mod slot (when a mod is displaced by glitch install)

---

### UI.3 — Left Page

The left page is always visible and always reflects the current (or historical) persistent state.

#### HullBar

```
[████████████████░░░░░░░░]  64 / 80  [−12 ▼]
```

- Color: green (`> 50%`), yellow (`25-50%`), red (`< 25%`), computed from `hullCurrent / hullMax`
- Delta overlay from `Deltas["hull.current"]`
- Pulsing red border animation when hull < 25%

#### CoinDisplay

- Numeric; `+N` green / `−N` red delta overlay
- Shows icon (coin symbol defined in assets)

#### SystemDisplay (four rows)

Each system shows: system name, level pips (0-3 filled circles), and primary value:
- **Engine:** "Starting Charge: 0 / 15 / 40 / 75" based on level
- **Cabin:** "Crew: X / Y" (current count / max capacity)
- **Computers:** "Draw: N formations/page"
- **Hull:** "Max HP: N"

Hover on any system row opens a tooltip: full description of the system + all upgrade costs.

#### ModSlotRail

One row per system. Shows mod card icons in sequence:
- Filled slots: mod icon + abbreviated name; hover enlarges to full card with name + description + size
- Empty slots: hollow rectangle
- Glitch slots: red border around mod card
- Size 2 mod spans 2 slots visually; size 3 spans 3

Clicking the `ModStoreButton` opens `ModStorageOverlay`.

#### CrewGrid

2-column grid. Slots equal to cabin capacity. Over-capacity slots blacked out.

Each filled slot contains a `CrewCard`:

**CrewCard layout:**
```
┌─────────────────────────────────┐
│ [Name]           [Race icon]    │
│ [Ability text — dynamic]        │
│                                 │
│ Res:3  Int:2  Cha:1  End:2     │
│ [■■■■□□]  HP 4/6               │
│ Fatigue: 2/3                    │
│                           [△]   │ ← exhaust warning if forecasted
└─────────────────────────────────┘
```

**Ability text generation (most complex text in the game):**

The ability text is **never a static string**. It is generated by `abilityText.ts` at render time:

```typescript
function generateAbilityText(
  crew: CrewMemberState,
  context: AbilityTextContext // { phase, formation, slot, activeMods, combatState }
): string
```

Rules:
- **Outside combat** (any non-combat page): show base output from current Effect Level. e.g. `"Charge 10"` for a Charge crew with Res 4 (EL = 5, output = 10). If a trait always modifies EL (e.g. "+1 EL for every 100 coin" with 300 coin), compute and show the result: `"Charge 16"`.
- **Combat planning — not slotted:** show base output for the current formation's slot if one is implied, else base. Show fatigue indicator.
- **Combat planning — slotted into a slot:** Show the output as it will resolve in that specific slot. Example: slotted into a Boosted slot (`+50%`) in a lane with `+30%` lane boost, morale 80%: `"Charge 8 → 10.4 (×1.8 boost ×0.8 morale)"`. Show the final number prominently; show the modifier breakdown in smaller text below.
- **Multi-lane slot:** note each lane: `"Charge 10 → Left, Center"` (fires twice).
- **Dual ability:** show both abilities with halved values: `"Shield 4 / Charge 4"`.
- **Brainform/Smileform active:** replace Resolve with Int/Cha in EL calculation; show `"[BRAIN]"` indicator.
- **Trait modifiers that are dynamic** (e.g. "+1 EL per lock-on in lane"): during combat, use current lock-on value to compute; show `"Charge 12 (+2 from lock-on)"`.

This text must recompute any time: crew is slotted/unslotted, formation changes, morale changes, lock-on changes, combat modifier applied.

**HP bar:** 6 rectangle segments (max possible HP is 9 base + bonuses, but display as segments up to `hpMax`). Filled segments = `hpCurrent`. Delta overlay on `hpCurrent`.

**Fatigue display:** `Fatigue: X/Endurance`. Delta overlay from `Deltas["crew[i].fatigue"]`. If effective fatigue would reach `Endurance` from current slot choices, show `⚠ EXHAUST` flash.

**Panic state:** if `IsPanicked`, card shows a visual panic overlay (e.g. blurred or shaking outline) and is not draggable.

**Dead crew:** `IsDead = true` → greyed skull overlay; not shown in combat roster.

**Right-click on crew card:** opens `InspectorOverlay` with: full stat block, all traits with descriptions, current ability with full computed text, HP, fatigue/endurance, backstory.

#### FormationViewer

Scrollable panel below crew grid. Each formation shows a `FormationDiagram` (miniature).

Formations are not interactive on the left page — they are toggled via the `FormationToggleBar` within the `CombatPage` right page. The viewer here is for reference and history only.

Greyed out: the formation used on the immediately preceding page (cannot be drawn again unless only option).

---

### UI.4 — Right Page: Location Select

Two `LocationCard` components side by side:
- Name (large)
- Lore blurb (placeholder "TODO" text is fine)
- Special event badge if location has one (e.g. "Special: Gain Symbiote crew")

Selecting one highlights it with an inset shadow / border change. `Proceed` button appears after selection. Right-clicking a card opens Inspector with full location description.

---

### UI.5 — Right Page: Build Up

**DramaticTensionTracker:**
```
Dramatic Tension: 3 / 4 (minimum)    [READY ↗] ← greyed if <4
```
- DT count updates live as events are toggled
- Tracker turns green when threshold is met
- `Ready` button greyed while DT < 4

**Event pool grid:** flexible grid of `EventCard` components

Each `EventCard`:
- Event name
- DT value badge: green (negative DT = positive for player), red (positive DT = harder), grey (0)
- Summary effect: "Gain 80-110 coin", "Crew member dies", "+1 Int to chosen crew", etc. All effects are visible before selecting.
- **Exception:** trait events — shows "Trait revealed on page" instead of specific trait
- Highlighted/selected state: inset shadow + color tint
- Disabled state: an event cannot be clicked if selecting it would make the DT threshold unreachable (engine enforces this; UI greys it and shows tooltip "Cannot achieve 4 DT without this event"). This constraint must be evaluated live as other events are toggled.
- Tearing-disabled badge (lock icon) shown on events where tearing is not allowed

On `Ready` action: UI transitions to first event page.

---

### UI.6 — Right Page: Event Pages

Three sub-variants determined by `EventStyle`:

#### Announcement Style

```
┌──────────────────────────────────────┐
│  [EVENT NAME]       [+3 Threat ▲]   │  ← effect banner at top
│                                      │
│  [Typewriter lore text...           ]│
│  [slowly appearing character by     ]│
│  [character at configured speed     ]│
│                                      │
│                          [CONTINUE →]│
└──────────────────────────────────────┘
```

- Effect banner uses delta-style coloring (green = beneficial, red = harmful)
- Typewriter: skip-able by clicking anywhere on the text area (jumps to full text immediately)
- `Continue` dispatches `EventContinue`

#### Option Style

```
┌──────────────────────────────────────┐
│  [EVENT NAME]                        │
│  [Event description text]            │
│                                      │
│  [CrewCard A] [CrewCard B] [CrewCard C]  ← if crew selection needed
│                                      │
│  [Option 1] [Option 2] [Option 3]   │
│                                      │
│  [Lore text typewriters in after     │
│   selection is made]                 │
│                          [CONFIRM →] │ ← greyed until selection made
└──────────────────────────────────────┘
```

- Crew cards are rendered as smaller versions if shown here (name + ability + HP only)
- `EventOptionSelect` dispatches with `optionIndex`; lore text then types in
- `Confirm` dispatches `EventConfirm`

#### Shop Style

```
┌──────────────────────────────────────┐
│  [SHOP NAME]          Coin: 340 (−40)│
│                                      │
│  [Item 1] [Item 2] [Item 3]         │
│  [Item 4] [Item 5] [Item 6]         │
│                                      │
│                             [EXIT →] │
└──────────────────────────────────────┘
```

Each `ShopItemCard`:
- Item name, short description
- Cost with Cha discount applied: `120 → 110` (strikethrough base if discounted)
- `BUY` button: disabled if insufficient effective coin OR item already bought
- Greyed out if purchased (but remains visible with "PURCHASED" overlay)
- Hover: expanded card with full description
- Right-click: Inspector

Coin display shows running delta as items are purchased.

**Mercenary shop:** crew cards instead of item cards. Purchasing one triggers `DismissCrewMember` modal if over capacity.

**System Upgrade shop:** shows each upgradeable system as a card with current level, next level benefit, and cost. Clicking purchase is not reversible — a `ConfirmationDialog` appears: "Upgrade [System] to Level N for [cost] coin?"

---

### UI.7 — Right Page: Crew Creation UI

Used in prologue (×3) and any `CREW_GAIN` event. A multi-step flow, one attribute at a time:

```
Step 3 of 6: Choose Ability          [Crew card preview updates live]
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ Shield       │ │ Charge/Hack  │ │ Barrier      │
│ Common       │ │ Dual         │ │ Rare         │
│ Shield 8 at  │ │ Charge 4 /  │ │ Barrier 5 at │
│ current stats│ │ Hack 4       │ │ current stats│
└──────────────┘ └──────────────┘ └──────────────┘
```

Steps in order:
1. **Name** — 3 name options (race pool randomized); player picks one
2. **Race** — 3 race option cards; shows base HP, stat bonuses, passive description, race lore snippet
3. **Backstory** — 3 backstory options (flavor text); picking one has no mechanical effect but is shown on Inspector
4. **Ability** — 3 ability options (respecting 70/30 single/dual and 75/25 common/rare weights); shows computed output at current stats
5. **Stat block** — 3 stat distributions; shows total = Level; shows race bonuses applied on top; right-click on stat opens glossary
6. **Trait** — 3 trait options; shows trait name and description; right-click → Inspector

A crew card preview on the right side updates live as selections are made, so the player can see the full picture before committing each step. Steps cannot be un-done without tearing the page. `Back` button within the flow is not supported (each step dispatches a `PrologueCrewSelect` action that is committed).

---

### UI.8 — Right Page: Enemy Select

#### Standard (Non-Boss)

```
┌──────────────────────────────────────────────────┐
│ Enemy Designation: UNKNOWN        Threshold: 125  │
│                                                   │
│ [Weapon A Card]  [Weapon B Card]  [? ? ?]        │
│  locked           locked          choose from:    │
│                                   [opt 1][opt 2]  │
│                                   [opt 3]         │
│                                                   │
│ [Selected weapon description text appears here]  │
│                                                   │
│ Special Abilities (choose 1-3):                  │
│ [☐ Ability 1]  [☐ Ability 2]  [☐ Ability 3]    │
│                                                   │
│ Reward: S Coin + 3 formation options             │
│ ← reward preview updates live as abilities chosen│
│                                                [CONFIRM]│
└──────────────────────────────────────────────────┘
```

- Third weapon slot shows `?` until player selects from the 3 options
- Selecting a weapon option locks it in and shows its description
- Ability cards toggle on click; 1-3 can be selected
- Reward preview shows coin tier and formation count based on # abilities selected
- Hover any weapon → enlarged card + tooltip; right-click → Inspector
- Hover any ability → tooltip; right-click → Inspector
- `Confirm` dispatches `ConfirmEnemy`; requires: weapon selected + ≥1 ability selected

#### Boss Select

```
┌──────────────────────────────────────────────────┐
│                                                   │
│  [Boss Portrait A]       [Boss Portrait B]       │
│  [Boss Name A   ]       [Boss Name B   ]         │
│                                                   │
│  [Typewriter passage about selected boss]        │
│  [...types in character by character on click]   │
│                                                   │
│                                            [PROCEED]│
└──────────────────────────────────────────────────┘
```

- Selecting a boss triggers the typewriter passage
- `Proceed` dispatches `Proceed`; enabled after a boss is selected and the typewriter has finished (or been skipped)
- In Act IV (Chapter 10), only one boss option shown — same layout, second portrait empty

---

### UI.9 — Right Page: Combat Page (most complex)

The combat page is the most interactive and most complex component. It must handle live forecast updates, drag-and-drop, multiple overlays, and formation switching.

#### Overall Layout

```
┌────────────────────────────────────────────────────────┐
│ [Enemy Name]                                           │
│ Charge: ████████░░░░░░░░░░░░  87 / 125   (delta: +12)│
│ Super Shield: ██████░░░░░░  60 / 100  (Final Boss only)│
│                                                        │
│ ┌─────────────────────────────────────────────────┐   │
│ │  LEFT LANE   │  CENTER LANE  │  RIGHT LANE       │   │
│ │ [Weapon intent][Weapon intent][Weapon intent]    │   │
│ │ Lock-On: ●●○○○Lock-On: ●●●○○Lock-On: ●○○○○     │   │
│ │ Incoming: T12  Incoming: T8  Incoming: B3+T6     │   │
│ │ [Slot 1]      [Slot 1]       [Slot 1]            │   │
│ │ [Slot 2]      [Slot 2]       [Slot 2]            │   │
│ │ [Slot 3]                                         │   │
│ └─────────────────────────────────────────────────┘   │
│                                                        │
│ Active Mods / Passives: [list]                        │
│ Combat Modifiers: [+1 EL all crew] [-1 formation/page]│
│                                                        │
│ [Formation A] [Formation B] [Formation C]  ← toggle   │
│ Formation Passive: "Echo — 25% effects repeated"       │
│                                                    [READY]│
└────────────────────────────────────────────────────────┘
```

#### Enemy Intent Area

Three weapon cards, one per lane (Left, Center, Right). Each card shows:
- Weapon name
- Per-lane effect description: "M Threat", "+1 Lock-On", "Burn 3", etc.
- Lock-on indicator: 5 circles, filled = current lock-on level (with delta overlay if it changed this page from start-of-page effects)
- If enemy ability "Hidden intents" is active: cards show `???` until `ReadyUp` (revealed in execution)
- After a `Tear`, intent cards update to reflect new roll

#### Lane Display (`LaneDisplay` per lane)

Each lane column:
- **Header:** lane name, lock-on pips, incoming summary
- **Slot grid (`SlotGrid`):** vertical list of `SlotCell` components
- **Lane total forecast:** "→ Shield 0, Threat 12, Charge 0, Hack 0" (updates live during planning)

#### SlotCell (`SlotCell`)

A single draggable crew slot. States:
- **Empty:** dashed border, shows slot type icon (E/B/P/H/G/F/M/K) in center, shows lane connectivity badges
- **Occupied:** crew card thumbnail (name + ability effect at this slot context); shows fatigue delta if this would be their Nth use
- **Hover (drag target):** highlighted border
- **Kill slot (K):** red background; shows skull. Dropping a crew member here shows a confirmation: "This slot will kill [name]. Proceed?"
- **Pain slot:** red tint
- **Boost slot:** gold tint
- **Growth slot:** green tint

Slot type icon legend shown as a small key at the bottom of the slot grid.

#### Crew Roster (left page during combat)

During combat, the crew grid on the left page switches to **draggable mode**:
- Crew cards are draggable sources
- Dragging a crew card highlights valid drop targets (slots that are not blocked by panic/exhaust)
- Exhausted crew: greyed, no drag handle, `EXHAUSTED` badge
- Panicked crew: shaking animation, `PANICKED` badge, no drag handle
- Already slotted crew: a faint duplicate stays in the grid (crew can be slotted multiple times per formation if slots allow, limited by fatigue)

`SlotCrew` dispatched on drop: `{ crewId, slotIndex }`. Server returns updated `RenderState` with new deltas reflecting the slot choice.

`UnslotCrew` dispatched on drag-out or clicking an X on an occupied slot.

#### Formation Toggle Bar

Drawn formations shown as clickable cards at the bottom of the combat page. The number of formations equals the Computers system level (2-5).

Each formation toggle card shows:
- Formation name
- Miniature `FormationDiagram`
- Passive text (if any)
- Greyed = was the formation used on the immediately previous page

Clicking a different formation dispatches `SelectFormation`. Any crew slotted in the old formation's slots are unslotted first (engine handles this; UI re-renders from updated `RenderState`).

**Active formation highlight:** the selected card has a visible border/glow.

#### Live Forecast

During the planning phase, `RenderState.Deltas` contains forecasted values for the current slot assignments. These update on every `SlotCrew` / `UnslotCrew` / `SelectFormation` action response.

Per lane, the UI shows:
- Incoming Threat (from start-of-page effects)
- Forecasted Shield (from crew + mods)
- Net Threat after shield (red if > 0)
- Forecasted Charge contribution
- Forecasted Hack contribution
- Any reactive effects (Counterblaster: shows "Enemy Threat = X based on current forecast")

The forecast display must visually distinguish:
- **Settled:** start-of-page values already committed (grey / solid)
- **Forecasted:** values that depend on current planning choices (flashing / slightly transparent)

#### CombatModifiersPanel

Below the lane grid. Shows all active `CombatModifier` entries for this combat:
- Positive (green badge): "+1 EL all crew", "Empty slots Shield 1", etc.
- Negative (red badge): "+1 Lock-On at start", "Deployables disabled", etc.
- Enemy special abilities that are passive: listed here too ("Enemy: 25% critical Threat chance")

Multiple simultaneous modifiers are all shown — this is important since the spec explicitly requires it.

#### Ready Button

Always visible in the top-right or bottom-right of the combat page. Active as soon as the page loads (player can ready with zero crew slotted). Dispatches `ReadyUp`.

After readying, the UI enters an **execution animation phase** (optional, but improves readability):
1. Flash Shield values
2. Flash Threat damage (hull bar animates down)
3. Flash Hack → Coin up
4. Flash Charge accumulation → charge bar fills

Then auto-request next page state from server.

---

### UI.10 — Right Page: Combat Reward

```
┌──────────────────────────────────────┐
│  Combat Complete!   Coin earned: +85 │
│                                      │
│  Choose a Formation:                 │
│  [Formation A]  [Formation B]  [Formation C]  │
│      ↑ click to preview formation diagram below │
│                                      │
│  [FormationDiagram preview]          │
│                                      │
│                           [CONFIRM →]│ ← grey until selected
└──────────────────────────────────────┘
```

- Number of formation options = 3 by default; modified by number of special abilities chosen and Computers mods
- Clicking a card shows its diagram and passive text; right-click → Inspector
- `SelectReward` dispatched on card click; `ConfirmReward` on confirm

---

### UI.11 — Right Page: Chapter Conclusion

Untearable narrative page. Tear button in `PageViewerWidget` is disabled.

```
┌──────────────────────────────────────┐
│                                      │
│  [Lore text — typewriter in]         │
│  [describing Apogee's departure]     │
│  [from this system]                  │
│                                      │
│                          [CONTINUE →]│
└──────────────────────────────────────┘
```

---

### UI.12 — Right Page: Prologue Boon

Three selection steps (one per boon type), each with 3 options:

1. **Mod selection** — 3 mod cards; shows name, system, size, description
2. **Formation selection** — 3 formation cards with diagrams and passives
3. **Deployable selection** — 3 deployable cards; shows name and one-time effect

Each step dispatches part of `BoonSelect`. After all three selections, the prologue transitions to Chapter 1 Location Select.

---

### UI.13 — Right Page: Epilogue & High Score

**Epilogue screen:**
```
┌──────────────────────────────────────┐
│         RUN COMPLETE                 │
│                                      │
│  Total Charge Generated: 1,482       │
│  Total Hack Generated:     340       │
│  Total Shield Generated:   892       │
│  Pages Remaining:           47       │
│  Crew Survived:              3       │
│                                      │
│  [message of hope — lore text]       │
│                                      │
│               [SUBMIT SCORE →]       │
└──────────────────────────────────────┘
```

**High score submit:**
```
┌──────────────────────────────────────┐
│  Your score: 47 pages remaining      │
│                                      │
│  Enter your name:  [__________]      │
│                                      │
│                      [SUBMIT] [SKIP] │
└──────────────────────────────────────┘
```

`SubmitHighScore` dispatched with `playerName`. Then transitions to main menu.

---

### UI.14 — Overlays and Modals

#### Author Interjection Overlay

Triggered when `RenderState.PendingInterjections` is non-empty. Renders a speech bubble with the author's quote. Multiple queued interjections display sequentially (click to dismiss each).

Triggers (20+ defined in spec):
- Hull drops below 25% for the first time
- Hull drops to 1
- Pages remaining drops below 20
- Pages remaining drops to 0 ("Oh, dear.")
- First tear of the run
- First crew death
- First crew exhaust
- First combat win
- First morale event
- Chapter 1 start
- Chapter 10 start (Final Boss intro)
- Accumulating 5+ negative traits
- Three or more glitches equipped
- Full lock-on reached (Act I boss)
- Super Shield broken (Final Boss)
- Run won (success epilogue trigger)

Speech bubble floats center-screen, semi-transparent backdrop. The author has a name and a small portrait icon. Click anywhere to dismiss.

#### Inspector Overlay

Triggered by right-click on any inspectable game object.

```
┌──────────────────────────────────────┐
│  [Name]                         [←] [X]│
│  ─────────────────────────────────   │
│  [Full description, scrollable       │
│   if needed                          │
│                                      │
│   For crew: stat block, all traits,  │
│   current computed ability text      │
│                                      │
│   Glossary terms highlighted         │
│   and right-clickable]               │
└──────────────────────────────────────┘
```

- `[←]` back button only visible if reached via glossary chain; navigates to previous Inspector entry
- `[X]` closes overlay
- Inspectable objects: Crew cards, Mod cards, Formation cards, Weapon cards, Ability cards, Trait entries, Deployable cards, Location cards, Glossary terms
- Glossary term highlighting within Inspector text: any term in the static glossary map is underlined; right-clicking it pushes current entry onto the back stack and opens the term's entry

`useInspector` hook manages the navigation stack:
```typescript
const { open, back, close, currentEntry, canGoBack } = useInspector();
```

#### Crew Dismissal Modal

Triggered automatically when `RenderState.Right` has a dismissal required flag (set by engine when crew count exceeds cabin capacity). **Cannot be dismissed by clicking away** — player must make a choice.

```
┌──────────────────────────────────────┐
│  Crew Capacity Exceeded (4/4)        │
│                                      │
│  New Arrival:                        │
│  [Full CrewCard for new crew member] │
│                                      │
│  Drag a crew member here to dismiss: │
│  ┌────────────────────────────┐     │
│  │         DROP ZONE          │     │
│  └────────────────────────────┘     │
│                                      │
│  [CrewCard 1][CrewCard 2][CrewCard 3]│  ← draggable
│                                      │
│                       [CONFIRM] ← grey until drop made│
└──────────────────────────────────────┘
```

`DismissCrewMember` dispatched on confirm with the dropped crew's id.

#### Mod Storage Overlay

Accessible via `ModStoreButton` on left page.

```
┌──────────────────────────────────────┐
│  Mod Storage                    [X]  │
│                                      │
│  [Mod card]  [Mod card]  [Mod card]  │  ← draggable
│  [Mod card]  ...                     │
│                                      │
│  Drag mods to/from system slots      │
│  ↓ System slots shown below:         │
│  [Hull slots rail][Cabin slots rail] │
│  [Eng slots rail][Comp slots rail]   │
└──────────────────────────────────────┘
```

Drag-from-storage to a system slot: dispatches `MoveModFromStorage`.
Drag-from-system to storage zone: dispatches `MoveModToStorage`.
Validation: server rejects if insufficient slots; UI shows error toast.

#### Deployable Inventory Overlay

Opened from `DeployableButton`. Floats over the current page.

```
┌──────────────────────────────────────┐
│  Deployables                    [X]  │
│                                      │
│  ┌────────────────┐                 │
│  │ [Name]         │                 │
│  │ [Description]  │                 │
│  │          [USE] │ ← only on combat page, not used yet│
│  └────────────────┘                 │
│  ┌────────────────┐                 │
│  │ [Name]  USED ░░│ ← if used this page│
│  └────────────────┘                 │
│  ┌────────────────┐                 │
│  │ [empty slot]   │                 │
│  └────────────────┘                 │
└──────────────────────────────────────┘
```

`UseDeployable` dispatched on `USE` button click. Button disabled outside combat, if modifier "Deployables disabled" is active, or if already used this page.

---

### UI.15 — Page Viewer Widget

Persistent bottom bar, always visible.

```
[◄ PREV]  [37 of 200]  [NEXT ►]  [● CURRENT]  [✂ TEAR]
```

- `PREV`: navigate to `pageIndex - 1`; dispatches `viewPage(index-1)`. Greyed if on page 0.
- `NEXT`: navigate to `pageIndex + 1`; disabled if on latest page.
- `CURRENT`: jump to latest page; disabled if already on current.
- `TEAR`: dispatches `Tear` action. Enabled only if: on the current page AND `IsCurrentPage = true` AND current page type allows tearing. Visual state: always visible, but disabled state uses strikethrough or greyed scissors icon with tooltip "Cannot tear this page" when not allowed.
- Page counter: format `N of M` where N = current page index (1-based) and M = `PagesRemaining` at start of run (200).

When viewing a historical page:
- `PREV`/`NEXT` navigate within history
- `CURRENT` returns to live state
- `TEAR` disabled (cannot tear historical pages)
- A subtle banner at top of book spread: `"Viewing Page 24 — [RETURN TO CURRENT]"` with a button shortcut

---

### UI.16 — Main Menu & Meta Screens

#### Main Menu

```
┌──────────────────────────────────────────┐
│                                          │
│         TORN PAGES                       │
│         ─────────                        │
│                                          │
│     [NEW RUN]                            │
│     [CONTINUE RUN] ← greyed if no save  │
│     [HIGH SCORES]                        │
│     [ACHIEVEMENTS]                       │
│     [SETTINGS]                           │
│                                          │
│     Profile: [◄ Profile 1 ►]            │
└──────────────────────────────────────────┘
```

- **NEW RUN:** → Difficulty selection screen → Prologue crew creation
- **CONTINUE RUN:** resumes the saved active run for the current profile
- **HIGH SCORES:** per-difficulty leaderboard; profile-local
- **ACHIEVEMENTS:** 10 achievement cards with unlock condition and unlock state
- **SETTINGS:** opens `SettingsOverlay`
- **Profile selector:** arrow buttons cycle through profiles 1-3; long-press or click label to rename

#### Difficulty Selection Screen

Shown before new run. Lists difficulty levels 0-9. Only unlocked levels are clickable. Each shows:
- Level number and name (e.g. "Difficulty 3 — Hard Cover")
- Charge threshold modifier: "+30% thresholds"
- Lock icon if not yet unlocked

#### Settings Overlay

Tabs: Audio | Display | About

- **Audio:** Master volume slider, Music slider, SFX slider
- **Display:** Text speed slider (typewriter delay: Instant / Slow / Normal / Fast), (placeholder for future display options)
- **About:** version, credits placeholder

#### Run History Bookshelf

Visual list of `CompletedRunRecord` entries. Each shows:
- Run date
- Success / failure
- Chapters completed
- Pages remaining at end
- Crew names who survived

---

### UI.17 — Text & Icon System

#### Typewriter Hook

```typescript
function useTypewriter(fullText: string, speedMs: number): { displayed: string; isComplete: boolean; skip: () => void }
```

- `speedMs` read from `ProfileSettings.TextSpeed` (e.g. Instant=0, Fast=20ms/char, Normal=40ms, Slow=80ms)
- `skip()` sets `displayed = fullText` immediately
- Used in: `EventAnnouncementPage`, `EventOptionPage` (post-selection lore), `BossSelectPage` (boss passage)

#### Glossary Term Detection

```typescript
function detectGlossaryTerms(text: string): TextSpan[]
// Returns: array of { start, end, term, isGlossaryTerm }
```

The text is scanned for all keys in the static `GLOSSARY_MAP`. Overlapping matches: longest match wins. Returns spans which are rendered as either plain text or `<GlossarySpan>` components (underlined, right-clickable).

Applied to: all event descriptions, ability descriptions, mod descriptions, weapon descriptions, formation passive descriptions, tooltip text.

#### Icon Substitution

The engine can include icon tags in generated text: `[COIN]`, `[CHARGE]`, `[THREAT]`, `[SHIELD]`, `[HACK]`, `[BOOST]`, `[BURN]`, `[BARRIER]`, `[FUSION]`.

These are replaced with inline SVG icon components styled to match surrounding text size.

#### Ability Description Generation

`abilityText.ts` exports:

```typescript
function generateAbilityText(
  abilities: AbilityInstance[],
  crew: CrewMemberState,
  context: {
    phase: 'outside-combat' | 'combat-planning-unslotted' | 'combat-planning-slotted',
    slot?: SlotInstance,
    formation?: FormationInstance,
    combatState?: CombatState,
    activeMods?: ModInstance[],
  }
): string
```

This mirrors the C# `TextManager.GenerateAbilityDescription` method. The C# side generates this for `RenderState` population; the React side regenerates it live during combat planning as crew are slotted/unslotted (since `RenderState` is not updated until after a `SlotCrew` action round-trips to the server, but we want the card text to update immediately on drag). This is the **one allowed exception** to the "no client-side logic" rule — it is purely presentational and does not affect state.

---

### UI.18 — Audio

Audio is triggered entirely by the React layer. No audio signals from the engine.

#### Sound Effects (on-event triggers)

| Event | SFX |
|-------|-----|
| Button hover | soft click |
| Button click (mousedown) | click |
| Crew member slotted | placement thud |
| Crew member unslotted | lift sound |
| Damage taken | hull impact |
| Combat victory (escape) | warp/whoosh |
| Crew member death | low tone |
| Tearing a page | paper tear |
| Shop purchase | coin clink |

#### Background Music

| Context | Track |
|---------|-------|
| Main menu | title music |
| Act I chapters | act 1 ambient |
| Act II chapters | act 2 ambient |
| Act III chapters | act 3 ambient |
| Act IV (chapter 10) | act 4 ambient |
| Boss encounters (Ch 3, 6, 9) | boss music overlay |
| Final boss (Ch 10) | final boss track |
| Settings open | low-pass filter applied to all playing tracks |

Volume = `master × category`. Tracks cross-fade on transition (0.5s).

**Implementation note:** For Phase 1 prototype, all audio is placeholder or absent. Audio assets are a Phase 2 late milestone. Use `Howler.js` (`howler`) for audio management (loop, volume, fade support). All SFX and music tracks are defined as constants in `audio/tracks.ts` — the references exist from day one, the files themselves can be stubs.

---

### UI.19 — Build Order for UI Components

Build in this sequence to always have something runnable in the browser:

**Wave 1 — Shell (parallels Phase 2.1 engine work)**
1. `BookSpread` layout with placeholder left/right panels
2. `useGameState` hook + `GameContext` wired to Phase 1 stub API
3. `PageViewerWidget` (prev/next/current/tear buttons, page counter)
4. `HullBar`, `CoinDisplay` on left page (renders from stub `RenderState`)
5. `CrewCard` — static version (no dynamic ability text yet)
6. `DeltaOverlay` component — usable by all numeric displays

**Wave 2 — Pre-combat pages**
7. `LocationSelectPage`
8. `BuildUpPage` + `EventCard` + `DramaticTensionTracker`
9. `EventAnnouncementPage` + `useTypewriter`
10. `EventOptionPage`
11. `ShopPage` + `ShopItemCard`
12. `EnemySelectPage` (standard) + `WeaponCard` + `AbilityToggleCard`
13. `BossSelectPage`

**Wave 3 — Combat**
14. `CombatPage` shell + `EnemyPanel` + `ChargeBar`
15. `LaneDisplay` + `SlotGrid` + `SlotCell` (static, no drag)
16. `FormationToggleBar` + `FormationDiagram`
17. `CombatModifiersPanel`
18. Drag-and-drop: `@dnd-kit/core` integration, crew draggable, slot drop targets
19. Live forecast rendering (updates on each `SlotCrew` response)
20. Dynamic ability text in `CrewCard` (combat context)

**Wave 4 — Post-combat and overlays**
21. `CombatRewardPage`
22. `ChapterConclusionPage`
23. `PrologueBoonPage`
24. `CrewCreationUI` (multi-step)
25. `CrewDismissalModal` + drag-to-dismiss
26. `ModStorageOverlay` + mod drag-and-drop
27. `DeployableInventoryOverlay`

**Wave 5 — Inspector, glossary, interjections**
28. `InspectorOverlay` + `useInspector` hook + back-stack navigation
29. `GlossarySpan` + `glossary.ts` term detection
30. Icon substitution in text rendering
31. `AuthorInterjectionOverlay` + interjection trigger queue

**Wave 6 — Meta and polish**
32. `MainMenuScreen` + `ProfileSelector`
33. `DifficultySelectionScreen`
34. `SettingsOverlay` + text speed/volume controls
35. `EpilogueScreen` + `HighScoreSubmitScreen`
36. `RunHistoryBookshelf` + `AchievementsPanel`
37. Audio integration (`Howler.js`, SFX triggers, music cross-fade)
38. Delta flash animations + hull bar color transitions + panic animations
39. Historical page view mode (frozen, non-interactive, solid deltas, banner)

---

## Phase 3 — Testing Infrastructure

**Goal:** Automated confidence that the engine is correct. Two layers: unit tests (deterministic, fast) and the headless agent (full run simulation, fuzzy).

### 3.1 Unit Testing Framework

All tests in `TornPages.Engine.Tests` (xUnit). Group by module:

#### `EffectCalculatorTests`
Table-driven tests for every trait combination. Parameterize with `[Theory]` and `[InlineData]`. Cover:
- Base ability output (Shield 2×EL, Charge 2×EL, Rare 1×EL)
- Dual ability halving
- Every trait modifier (50+ traits, one test case each)
- Boost stacking (additive)
- Morale multiplier at 20%, 50%, 100%
- Edge cases: 0 Resolve, max lock-on, hull-1 trait

#### `CombatPageExecutorTests`
- Verify deterministic resolution order
- Burn-to-Threat conversion (page 2+)
- Barrier-to-Shield, Fusion-to-Charge (page 2+)
- Fatigue accumulation and Exhaust trigger
- Panic state: panicked crew cannot be slotted, clears next page
- Growth slot: +1 permanent EL
- Shield step absorbing Threat: excess goes to hull + crew
- Charge accumulation and threshold detection
- Per-lane net-negative Charge does not reduce accumulated total

#### `FormationDrawerTests`
- Guarantee: drawn set excludes last-used formation (when >1 exists)
- Guarantee: torn page produces different draw set
- Draw count matches Computers level (2-5)

#### `EventGeneratorTests`
- DT constraint: any single event can be selected and threshold still reachable
- "Gain Crew" always in pool, max once per chapter
- Shop events have correct item counts
- Max-per-chapter constraints respected

#### `RunFactoryTests`
- New run has correct starting values (Hull 80, Coin 100, Pages 200)
- 3 crew generated, each valid
- Stat totals sum to Level (8 for prologue)
- Formation archive has exactly 4 starting formations

#### `SerializationTests`
- Full `RunState` serializes to JSON and deserializes identically (round-trip)
- All `RightPageState` subtypes survive round-trip
- `CombatState` with boss state survives round-trip
- Deserialized state produces identical `RenderState` as original

#### `ActionValidatorTests`
- Illegal actions throw `EngineException` (not 500 errors)
- `SelectEvent` on a non-BuildUp page throws
- `ReadyBuildUp` with DT < 4 throws
- `UseDeployable` on a non-combat page throws
- `Tear` on untearable page type throws

### 3.2 Headless Agent

A separate console application (`TornPages.Agent`) that plays full runs autonomously:

```
TornPages.Agent/
  Agent.cs          # Main loop
  RandomStrategy.cs # Chooses random legal action
  BugReporter.cs    # Captures and formats failures
  RunValidator.cs   # Post-run consistency checks
```

#### Agent Design

```csharp
// Core loop
while (!run.IsTerminal) {
    var legalActions = ActionEnumerator.GetLegalActions(run);
    var chosen = strategy.Choose(legalActions, run);
    run = Engine.ApplyAction(run, chosen);
    RunValidator.AssertConsistency(run); // throws on invariant violation
}
```

**`RandomStrategy`:** Selects uniformly at random from legal actions. No heuristic. This is intentional — random exploration surfaces edge cases that smart play would avoid.

**`ActionEnumerator.GetLegalActions(RunState) → List<PlayerAction>`:**
- Returns every valid action for the current page and game state
- This is the specification of what is always valid — exhaustive enumeration
- Unit-tested independently

**`RunValidator.AssertConsistency(RunState)`:** After every action, check:
- Hull current ≤ Hull max; Hull current ≥ 0
- Coin ≥ 0
- Pages remaining ≥ 0
- Morale in [20, 100]
- Crew count ≤ Cabin capacity (unless dismissal modal is active)
- All equipped mods fit in system slots
- `ChargeAccumulated` ≥ 0
- Lock-on in [0, 5] per lane
- No dead crew in formation slots
- No exhausted crew in formation slots

**`BugReporter`:** On exception or invariant violation:
1. Record the full `RunState` as JSON
2. Record the sequence of `PlayerAction` that led to the failure
3. Write to `bugs/bug_{timestamp}.json`
4. Output summary to stdout

#### Agent Execution Modes

**CI mode:** Run N=100 full games, report any failures. CI fails if any bug is found.

```bash
dotnet run --project TornPages.Agent -- --runs 100 --seed 0 --ci
```

**Exploration mode:** Run N games with a specific seed range for reproducibility.

```bash
dotnet run --project TornPages.Agent -- --runs 1000 --seed 42 --output bugs/
```

**Replay mode:** Feed a saved `RunState` + action sequence, re-execute deterministically, confirm same outcome.

```bash
dotnet run --project TornPages.Agent -- --replay bugs/bug_20260414_153200.json
```

### 3.3 CI Integration

Add to `api-deploy.yml`:

```yaml
- name: Run unit tests
  run: dotnet test TornPages.Engine.Tests --logger "trx;LogFileName=test-results.trx"

- name: Run headless agent (100 games)
  run: dotnet run --project TornPages.Agent -- --runs 100 --seed 0 --ci

- name: Upload bug reports
  if: failure()
  uses: actions/upload-artifact@v4
  with:
    name: bug-reports
    path: bugs/
```

### 3.4 Coverage Targets

| Module | Target |
|--------|--------|
| EffectCalculator | 95% line coverage |
| CombatPageExecutor | 90% |
| ActionValidator | 100% (every illegal path tested) |
| EventGenerator | 85% |
| Serialization round-trip | 100% of all `RightPageState` subtypes |
| Agent: full run completion (no exceptions) | 100 of 100 games |

---

## Ordering Summary

```
Phase 1 (validate stack)
  1.1  Repo + project structure
  1.2  Stub API endpoints
  1.3  Latency measurement React app
  1.4  CORS + env config
  1.5  Hosting + CI/CD
  ✓ Exit: friend can access, copy latency results

Phase 2 (build the game)
  2.1.1  Data definitions (all *Definition records)
  2.1.2  State type hierarchy
  2.1.3  Run initialization + RNG
  2.1.4  Action dispatcher + validation
  2.1.5  RenderState generator
  2.2    Prologue
  2.3    Chapter structure (Location → BuildUp → Events → Enemy Select → Conclusion)
  2.4    Combat (layers 1→5, then reward)
  2.5    Full run loop (progression, loss/win, save/load)
  2.6    Metaprogression + UI shell
  2.7    Polish + author interjections

Phase 3 (testing infrastructure)
  3.1  Unit tests (per-module, table-driven)
  3.2  Headless agent (random play, invariant checks, bug reporter)
  3.3  CI integration
  3.4  Coverage enforcement
```

---

## Decisions Resolved Before Phase 2

1. **Hosting:** C# API runs locally, exposed via Cloudflare Tunnel at `api.torn-pages.com`. React client on Vercel at `torn-pages.vercel.app`. ✓

2. **Profile/save storage:** Server-side JSON files (`profiles/{profileId}.json`). Not `localStorage` — full server-side persistence means state survives browser changes and is accessible from any device hitting the tunnel. ✓

3. **Session/profile management:** Full profile-scoped API (see §1.2). Each player picks a profile at the main menu; all subsequent requests include that profile's GUID. The server maintains one `TornPagesEngine` instance per active profile in memory, backed by disk. Multiple players can use the server simultaneously with independent state. ✓

4. **Difficulty at launch:** Difficulty 0 only for Phase 2. ✓

---

## File Reference

| Path | Purpose |
|------|---------|
| `spec/torn_pages_design.md` | Full game design: mechanics, data, all numerical values |
| `spec/torn_pages_implementation_spec.md` | Technical spec: state types, API, patterns, serialization |
| `spec/torn_pages_lore_bible.md` | Lore, races, backstories, location names |
| `IMPLEMENTATION_PLAN.md` | This document |
