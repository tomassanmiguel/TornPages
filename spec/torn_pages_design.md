# Torn Pages — Game Design Document
*Compiled from handwritten design spec, April 2026*

---

## Table of Contents

1. [Concept](#concept)
2. [Gameplay Overview](#gameplay-overview)
3. [Tearing Pages](#tearing-pages)
4. [Chapter Flow](#chapter-flow)
5. [Crew Members](#crew-members)
6. [Crew Generation](#crew-generation)
7. [Crew Traits](#crew-traits)
8. [Build Up Phase](#build-up-phase)
9. [Events](#events)
10. [Locations & Special Events](#locations--special-events)
11. [Choosing Enemy](#choosing-enemy)
12. [Combat](#combat)
13. [Combat Concepts & Effects](#combat-concepts--effects)
14. [Combat Boosts & Penalties](#combat-boosts--penalties)
15. [Formations](#formations)
16. [Enemy Weapons](#enemy-weapons)
17. [Enemy Special Abilities](#enemy-special-abilities)
18. [Act Bosses](#act-bosses)
19. [Final Boss](#final-boss)
20. [Ship Systems](#ship-systems)
21. [Mods](#mods)
22. [Glitches](#glitches)
23. [Deployables](#deployables)
24. [Negative Traits](#negative-traits)
25. [Numerical Reference](#numerical-reference)
26. [UI Design](#ui-design)
27. [System Design & Architecture](#system-design--architecture)
28. [Demo Notes](#demo-notes)
29. [Metaprogression](#metaprogression)
30. [Miscellaneous Concepts](#miscellaneous-concepts)
31. [Achievements](#achievements)

---

## Concept

The player plays as a struggling author trying to produce a sequel to their hit first entry in the Diaspora Saga. It is a tactical, choose-your-own-adventure style roguelike. Each run entails producing a draft of this sequel novel. In each draft, the player will define the destiny of The Apogee and her crew as they embark on a quest to destroy the Hyperion Shield and free the galaxy from Macedonian tyranny.

---

## Gameplay Overview

Each draft is composed of ten chapters plus a prologue and an epilogue. The chapters are further subdivided into Act I (1–3), Act II (4–6), Act III (7–9), and Act IV (10). Each chapter has two parts: the Buildup and the Climax.

In the Buildup, players will choose plotlines and story elements to upgrade The Apogee and her crew. In the Climax, there will be a confrontation with a hostile ship — the Apogee must escape before being destroyed. In the last chapter of each act, the encounter is against an extra challenging boss enemy.

---

## Tearing Pages

The eponymous mechanic: the player is able to tear out the right-hand page of the book in order to rewrite it with more options. This comes with a cost, however. Each draft has a fixed number of pages from the start of the run. **Every phase of the chapter flow consumes a page** — location selection, build up, each event, enemy selection, each combat page, and the chapter conclusion all consume one page each. Tearing additionally consumes a page on top of the page being replaced. If the player runs out of pages in the book, the Author interjects ("Oh, dear.") and the run ends in failure with no epilogue.

If the Apogee's Hull reaches 0 at any point, the run also ends in failure.

---

## Chapter Flow

Each chapter proceeds in the following order:

1. **Choose Location** — a dedicated page presents the player with two location options, each accompanied by a lore description (TODO). The player selects one. Locations can appear in any act.
2. **Build Up page**
3. **Event pages**
4. **Enemy Selection page** — see Choosing Enemy
5. **Combat pages**
6. **Chapter Conclusion page** — a simple, untearable lore page describing the Apogee's exit from the system. Content is basic for now (TODO lore text). No gameplay decisions are made here.

---

## Crew Members

Crew Members are the core of any run of Torn Pages. Each crew member has the following properties:

- **Name**
- **Race**
- **Birthplace**
- **Backstory**
- **Traits** — these have myriad gameplay effects
- **Stats:**
  - **Resolve** — determines the Effect Level of this crew member in combat
  - **Intelligence** — determines contribution to PASSIVE CHARGE
  - **Charisma** — grants access to more options during the Buildup and reduced shop prices. Each total point of Charisma across the entire crew applies a 1% discount to all shop prices, applied consecutively (so prices asymptotically decrease but can never reach zero).
  - **Endurance** — determines the maximum fatigue this crew member can accrue during combat without exhausting
  - **Health** — determines the maximum amount of damage the crew member can sustain without dying
- **Ability** — what the crew member does when slotted in combat

### Effect Level & Boost

A crew member's **Effect Level** equals **1 + their Resolve stat** by default. Many traits, formations, and mods modify this further. Effect Level determines the base ability output:

- Common abilities (Shield, Hack, Charge) produce **2 × Effect Level** per slot
- Boost produces **10% × Effect Level** per slot (e.g. a crew member with Resolve 3 has Effect Level 4, producing 40% Boost)
- Rare abilities (Barrier, Heal, Fusion) produce **1 × Effect Level** per slot
- If a crew member has two abilities, the raw effect of both is halved (rounded down) before other modifiers apply

**Boost** is applied as a percentage increase to the **final output value**, not to Effect Level. All Boost sources in a lane are summed additively, then applied as a single multiplier to the output. For example: a crew member with Effect Level 4 using Charge (base output 8), in a lane with +30% boost and slotted into a +50% Boosted slot, produces 8 × 1.80 = 14.4 (rounded as appropriate).

**The `(2x)` slot notation** means the crew member's entire ability fires twice through that slot — the full formula (Effect Level → base output → Boost applied) runs twice. Boost is therefore double-dipped: it applies once per firing.

---

## Crew Generation

At various points we will author new characters into the story. Depending on the context, some elements of the crew member will be pre-set. In the prologue we create 3 crew members: one with shield ability, one with charge, and one with hack. Level of crew members equals 8 plus the current chapter. By default, crew members have base HP based on their race.

**Crew Creation UI:** A dedicated UI is used in the prologue and any event that grants a crew member. For each of the following attributes, the player is presented with a choice of three randomly generated options and selects one:

1. Name
2. Race
3. Backstory
4. Ability
5. Stat block — the total of the crew member's base Int, Cha, and Res stats equals their level (8 + current chapter). These points are distributed randomly across the three stats to generate each option. Race bonuses are applied on top of this base.
6. Trait

- **Name** — chosen randomly from a big pool
- **Race** — choose from 1 of 3 options
  - Human (+1 cha, +1 int, +1 res) [4 HP]
  - Symbiotes (+3 res) [5 HP]
  - Dwarves (+1 end) [5 HP]
  - Steelborn (+3 cha) [4 HP]
  - Replicants (when they would die, they revive with −1 res and full health) [3 HP]
  - Brittlebones (+4 int) [3 HP]
  - Silent (randomly Boost a random slot each page) [3 HP]
- **Ability** — 70% one ability, 30% two abilities; choose 1 of 3 randomly drawn options
  - 75% Common:
    - Shield (2×) — base amount is 2 × effect level
    - Hack (2×)
    - Charge (2×)
    - Boost (10% × X)
  - 25% Rare:
    - Barrier (X) — amount equals effect level (1:1)
    - Heal (X) — amount equals effect level (1:1)
    - Fusion (X) — amount equals effect level (1:1)
  - **Dual Abilities:** If a crew member has two abilities, the raw effect of both is halved (rounded down).
- **Trait** — choose 1 of 3 from randomly drawn options; modifies combat ability or provides other effect

---

## Crew Traits

The following traits may be assigned to crew members. Each modifies combat behavior or provides passive effects.

**Stat Modifiers:**
- Unaffected by Morale
- Won't Panic
- +2 int, −1 cha
- +2 cha, −1 int
- +2 res, −1 int
- +2 int, −1 res
- +2 cha, −1 res
- +2 res, −1 cha
- +1 int
- +1 cha
- +1 res
- +1 end, can't be boosted
- Affected 50% more by boost
- Gain +1 int, res, or cha after each combat
- +1 effect level for every 100 coin you have
- +1 effect level after every page in combat
- If the Apogee would be destroyed, this crew member sacrifices themselves to prevent it
- +3 effect level at start of combat; −1 after each page
- Double effect level while Apogee has exactly 1 hull left
- Regenerate 1 HP after combat
- +5 res, −1 end
- +1 effect level every time used in combat
- Shield 5 on exhaust
- +2 resolve; 25% chance to panic each page
- +1 end, −3 res
- +1 effect level for each other crew of the same species on the ship
- Apply 50% of effect on the next page too
- +1 effect level for each lock-on in the lane
- +7 effect level; minus one for each other crew member
- Intelligence on this crew member contributes double to passive charge
- Charisma instead of Resolve determines this crew member's effect level
- +2 res; randomize the effect of this crew member each page
- +1 effect level for every 10 incoming threat
- Hack 10 on exhaust
- +2 effect level in left lane
- +2 effect level in middle lane
- +2 effect level in right lane
- While exhausted, 25% each page to refresh
- Not damaged by threat overflow
- +1 effect level to connected crew members
- +2 res; any damage in combat is lethal
- This crew member has 6 max HP
- Whenever another crew member dies, gain 2 random stats
- Gain +1 effect level whenever the Apogee takes damage in combat
- Gain +1 effect level whenever this crew member takes damage
- Always connected to the Left lane, regardless of slot connectivity
- Always connected to the Center lane, regardless of slot connectivity
- Always connected to the Right lane, regardless of slot connectivity

---

## Build Up Phase

During the Build Up phase of a chapter, the player selects any number of EVENTS. Each event takes place on a subsequent page and will upgrade/downgrade the run. The Events offered are drawn from a pool of possible events. Events can be restricted to certain acts, conditions, or LOCATIONS.

Events offer a positive/negative value of DRAMATIC TENSION. The player must select a set of events that reach **at least 4** TENSION — going over 4 provides no additional benefit. Once sufficient DT is reached, they may ready and then the events will play out one by one, taking one page each. Tearing on the buildup page will reroll a new menu of event options, guaranteed to differ from the current set.

**DT pool generation constraint:** The pool must be generated such that there is sufficient positive DT available to theoretically allow any individual event to be selected. That is, even if a player selects the most negative-DT event in the pool, the remaining events must still offer enough positive DT to reach the threshold of 4. This applies to **all events including guaranteed ones** — for example, "Gain a Crew Member" (−1 DT) is always in the pool, so the remaining events must always provide at least +5 DT to ensure the threshold is reachable even if the player takes it.

All event choices and their effects are known to the player on the Build Up page before they ready up. For example, the player will know they are gaining M coin or upgrading engines before committing. The only exception is traits, which are revealed on the event page itself.

Another constraint: the system cannot generate an unclickable event (one that cannot be chosen while still allowing the DT threshold to be reached). The Ready button is greyed out while there is insufficient Dramatic Tension.

Number of events drawn scales with Charisma:
- Base: 5 events
- +1 event for every 3 Charisma total on team
- Max: 15 events

**Event Dramatic Tension Requirement: 4**

---

## Events

*(+X) numbers are Dramatic Tension values. Unless otherwise indicated, each category appears only once per chapter.*

### Neutral Events (always in pool)

Most events (e.g. Gain S coin) have several lore variations. Furthermore, neutral events may have a SPECIAL variant for some locations.

**Event UI Types:**
- **Announcement Style** — lore text rolls out in typewriter fashion, event effect announced at top, "Continue" button to proceed
- **Option Style** — description text with crew card if relevant, 3 option buttons, lore text revealed after selection, "Confirm" button to proceed
- **Shop Style** — items to buy with buy buttons; "Exit" button to leave shop; hovering shows details, right-click for full info; further lore revealed after selection is made

---

### Event List

#### Gain / Lose Coin
- Announcement Style; tearing reloads the coin amount
- Chosen randomly from: Gain XL (−2), Gain L (−1), Gain M (0), Gain XL (+1), Lose S (+2), Lose M (+3)
- Coin loss events (Lose S, Lose M) cannot be generated if the player does not have sufficient coin to cover the maximum loss threshold

#### Gain a Crew Member (−1)
- Uses crew creation UI (see Crew Generation)
- **Always guaranteed as an option in the Build Up pool**
- Max once per chapter

#### Crew Member Dies (+5)
- Options style — presents up to 3 choices from among current crew; if fewer than 3 crew members exist only the available crew are shown as options
- Requires 2+ crew members; suppressed from the pool entirely if only 1 crew member is present

> **General rule for option-style events:** Any event that presents crew member choices will show as many options as crew members are available (up to 3), rather than being suppressed. Events with explicit crew minimums are exceptions and will be suppressed if the requirement is not met.

#### Visit a Shop
- ALWAYS chosen for generation
- Chosen randomly from: Mod Shop, Deployable Shop, Healing Shop, Mercenary Shop, Sabotage Shop, **System Upgrade Shop**
- Uses shop UI; each shop type generates 6 options (no repeats) of the chosen type
- Mod Shop draws 6 mods from across all system flavors (not limited to one system)
- Healing shop sells the listed hull/morale options from Starting Values (4 options)
- Mercenary shop sells randomly generated crew members at the current chapter's level
- Sabotage shop sells randomly chosen combat boosts
- System Upgrade Shop offers one upgrade option per system (at the costs listed in Numerical Reference); does not appear if all four systems are already at max level
- It is possible to generate 2 shops in the same chapter — they appear as separate entries in the event pool, but must be different shop types

#### Combat Modifiers
- Announcement style; tearing changes the generated modifier
- Positive Modifier (0) or Negative Modifier (+2)
- Only one per chapter
- **Multiple combat modifiers can be active simultaneously** in a given combat — one from an event, one from a Sabotage Shop purchase, one from an enemy special ability, etc. All stack and apply independently.

#### Remove a Formation (−1)
- Option style, 3 formations chosen from deck
- Requires: >1 formation in deck

#### Gain a Deployable (−1)
- Announcement Style; tearing generates a new deployable

#### Upgrade a System (−2)
- Randomly chosen from Computers, Cabin, Engine, and Hull
- Announcement Style; tearing DISABLED — the specific system being upgraded is shown to the player on the Build Up page before they commit, so rerolling offers no additional information
- Can appear up to twice

#### Training
- Randomly chosen from: Int (+0), Cha (+0), Res (+0), End (−2)
- Options style, choose from 3 crew members; tearing rerolls the options
- Gives +1 to the given stat
- Can appear up to 3 times

#### Gain a Negative Trait (+3)
- Options style — the player chooses which crew member will receive the trait on the **event page itself**, not on the Build Up screen. The specific trait is revealed on that same page after the crew member is chosen.
- Tearing rerolls both the crew member options and the trait

#### Gain a Positive Trait (−1)
- Same flow as Gain a Negative Trait, but the trait offered is positive

#### Gain / Lose Morale
- Randomly chosen: restore all morale (−3), restore 25% (−1), lose 10% (+1), lose 25% (+3)
- Announcement Style (tearing disallowed)
- Only one per chapter

#### Gain Mod / Glitch (−2 / +4)
- Comes in Computer / Engine / Hull / Cabin flavors
- Announcement Style; tearing re-rolls the mod
- Can be 1 Mod and 1 Glitch per chapter

#### Upgrade / Downgrade a Formation (−1 / +1)
- Upgrade chosen from: Boost, No Fatigue, Heal, +Effect Level — replaces a randomly selected basic (Empty) or negative slot in the chosen formation
- Downgrade chosen from: +Fatigue, +Pain — replaces a randomly selected basic (Empty) or positive slot in the chosen formation
- Options Style — choose which formation will be modified; tearing chooses new options
- A formation is only eligible for upgrade if it has at least one basic or negative slot; only eligible for downgrade if it has at least one basic or positive slot
- The event itself is not offered if no eligible formations exist in the archive
- Max only 1 per chapter

#### Gain / Lose Max HP
- Announcement Style; tearing disabled
- Gain 10 max HP (+current HP by same amount) (+1 DT) or Lose 10 max HP (−2 DT)
- Lose variant cannot be generated if current max HP is less than 11
- Only one per chapter

#### Take Damage / Heal
- Chosen from S, M, L, XL: (+1/0, +2/−1, +3/−2, +4/−3)
- Announcement Style; tearing reloads the range
- Only one per page

---

### Location Special Events

#### The Drift
- Gain Brittlebones crew

#### Jensonia
- Copy one crew member's stats to all other crew members (option style)

#### Mycelia
- Gain Silent crew

#### Sigg
- Gain Steelborn crew

#### Merkaj
- Clear a random Glitch (announcement style)
- Only available if at least one Glitch is currently equipped

#### Enjukan
- Gain Replicant crew
- Convert all current crew members to Replicants — changes their race, replacing all racial stat bonuses and passives with Replicant equivalents

#### Voidwatch
- Randomize all crew member abilities (announcement style) — each crew member is assigned a new randomly generated ability respecting standard rarity weights (75% common, 25% rare), as if generating a new crew member

#### Khorum
- Gain Dwarf crew

#### Kraethos
- Gain Symbiote crew

*(Note: Merkaj, Odi, Jaguan, Melflux, Hermestris, Ludona, Dunson 4, Macedon, Kaimitrio, Luxion, Sol, Desolate Anchor 003 — special events TBD. Will implement many more special events later.)*

---

## Choosing Enemy

Before each combat, an enemy selection page is presented where the author concocts who you will fight. Enemies have 3 weapons which define what will populate in the 3 lanes. They also receive one or more special abilities which alter how the fight works.

2 weapons are chosen from the pool at random and are locked; the third is chosen by the player from a pool of 3 options. The Attenuation Threshold is static and determined by the current chapter.

The player is offered three special abilities and chooses 1–3. **The more abilities chosen, the more formation options are presented on the combat rewards page.** Additionally, defeating an enemy awards Coin based on how many special abilities were chosen:

| Special Abilities Chosen | Coin Reward |
|--------------------------|-------------|
| 1 | S Coin |
| 2 | M Coin |
| 3 | L Coin |

The coin gained is displayed on the combat rewards page.

For boss encounters, the enemy is pre-determined at game start. Instead of the standard selection, the player simply chooses one of two options from a pool of three. In Act IV, only one boss is available.

**Boss Select UI (Right Page):**
```
+--------------------------------------------------+
| Left Page    [Boss Portrait 1] [Boss Portrait 2] |
| Unchanged    [Nameplate      ] [Nameplate      ]  |
|              [~passage describing upcoming boss~] |
|              (text typewriters in on selection)   |
|                         [Proceed] (post-select)   |
+--------------------------------------------------+
```

**Standard Enemy Select UI (Right Page):**
```
+-------------------------------------------------------+
| Left Page   Enemy Name              Attn. Threshold   |
| Unchanged   [weapon 1][weapon 2][  ?  ]  <- locked    |
|             [  slot  ][  slot  ][slot  ]  weapons +   |
|             [Selected weapon description text  ]       |
|             [Bonus Abilities panel             ]       |
+-------------------------------------------------------+
```
- The third weapon slot starts hidden (shown as "?") and reveals its name after the player selects it
- Hovering a weapon card enlarges it and shows a tooltip; right-clicking opens full details
- Selected bonus abilities are displayed; hovering enlarges and shows a tooltip

---

## Combat

The goal of each combat is to generate enough CHARGE to attenuate to the nearby Anchor and escape to another system (starting a new chapter). Combat occurs in three Lanes where effects accumulate. The player will have a planning phase where they allocate resources to the lanes. Once they are satisfied, they "ready up" and anything in the lanes executes and adjusts the game state. After, if sufficient charge is reached, the combat ends.

**The general flow is:**

```
Enemy Intent Phase → Planning Phase → Execution Phase
```

In the **Enemy Intent Phase**, the enemy rolls an ability (tearing the page will re-roll a new intent) and populates each of the 3 lanes with negative incoming effects. The severity of these effects increases with the LOCK-ON level of each lane. Persistent stack contributions (Burn, Barrier, Fusion), Passive Charge, and start-of-page enemy abilities all resolve at this point — **before the player makes any decisions**. Tearing re-runs all of this with a new seed.

In the **Planning Phase**, the player toggles between Formations drawn from the ship's Formation Archive. The number of formations drawn each page equals the current Computers system level (2 at level 0, up to 5 at level 3). Tearing the page will draw a new set of formations, guaranteed to differ from the current set. The same formation will not be drawn on consecutive pages — if no other option exists in the archive, this constraint is relaxed and a formation may repeat. It is impossible to have zero formations in the archive. Formations contain Slots that have connectivity to one or more lanes. Crew members may be slotted into these slots, applying their ability once into **each connected lane independently** — a slot connected to two lanes fires the ability twice, making multi-lane connectivity a significant force multiplier.

Nothing is final during the Planning Phase until the player readys up. The UI shows a live forecast of the outcome, including continuous effects that respond to planning choices in real time (e.g. Counterblaster Threat scales with forecasted Charge). The player may freely toggle formations and unslot/reslot crew members until satisfied.

**Forecasting:** During the Planning Phase, the UI shows a live forecast of the outcome. Reactive abilities — such as enemies whose Threat scales with the player's Charge generation — update instantly as the player makes decisions. Passive Charge is also included in this forecast.

During the **Execution Phase**, effects in the lanes resolve in the following order, then we move to the next page:

1. **Persistent effect stacks** (Barrier, Fusion, Burn) — these add their per-page contributions at the start, before crew abilities fire
2. **Passive Charge** — added before crew abilities fire; contributes to reactive ability triggers
3. **Shield** — crew Shield abilities fire, mitigating incoming Threat
4. **Threat** — remaining unmitigated Threat deals damage to Hull and slotted crew
5. **Hack** — Hack generates Coin (1:1)
6. **Charge** — crew Charge abilities fire; Charge is added to the running total

After all effects resolve, a **combat end check** occurs: if accumulated Charge meets or exceeds the threshold, the combat ends successfully. If the Apogee's Hull reaches 0 at any point, the run ends in failure.

> **Tearing a combat page** re-runs all beginning-of-page logic (persistent stacks, passive charge, enemy intent) and redraws formations. The result is guaranteed to differ from the torn page.

---

## Combat Concepts & Effects

### Core Combat Effects

| Effect | Description |
|--------|-------------|
| **Threat** | Produced by enemies. If not fully mitigated by Shield, the **Hull takes damage equal to the unmitigated overflow** (Threat minus Shield). Additionally, whenever a lane has any unmitigated Threat, each crew member slotted there takes **1 damage per slot connected to that lane** — regardless of how large the overflow is. Is reset after each page normally. |
| **Charge** | Must cross threshold (determined by enemy) to successfully escape in combat. Persists page-to-page. Per-lane Charge can go negative, but only positive lane values contribute to the accumulated total. Effects that directly reduce accumulated Charge (e.g. Repulsor) are applied separately and can reduce the total. |
| **Shield** | Mitigates Threat. Does not persist between pages. |
| **Hack** | Used in combat to gain Coin. 1 Hack = 1 Coin. |
| **Boost** | Increases effect output by X% additively. Multiple Boost sources in the same lane stack additively (e.g. +30% lane boost + +50% slot boost = +80% total). Boost in lanes does not persist between pages. |
| **Burn** | Stacks of Burn apply 1 Threat per stack at the start of each page. Burn stacks persist in-lane. First triggers at the start of page 2 (not the page they are applied). |
| **Heal** | Repairs the Apogee's hull directly. This is distinct from formation H (Heal) slots, which restore HP to the slotted crew member. |
| **Barrier** | Stacks of Barrier apply 1 Shield per stack at the start of each page and persist in-lane. First triggers at the start of page 2. |
| **Fusion** | Stacks of Fusion apply 1 Charge per stack at the start of each page and persist in-lane. First triggers at the start of page 2. |

> **Note:** All persistent effects (Barrier, Fusion, Burn) add their stacks at the **start** of the page following the one on which they were applied. Applying Barrier on page 1 produces Shield starting page 2.

### Other Combat Mechanics

- **Passive CHARGE** — At the start of each page (before crew abilities fire), gain Passive Charge equal to 1 plus the total Intelligence stat of the crew. This contributes to reactive triggers (e.g. Counterblaster Threat).
- **Morale** — The crew has a Morale that starts at 100% and can be degraded to a minimum of 20% by events, enemy special abilities, weapons, or any other source — including mid-combat effects. Morale is applied as a final multiplier to all effect output — Shield, Threat, Hack, Charge, Heal, Barrier, Fusion, and Boost percentage — everything. It multiplies the computed final value after all other modifiers.
- **Charge** — Per-lane Charge can go negative (e.g. from Repulsor or enemy abilities). However, only positive lane results contribute to the running `ChargeAccumulated` total — a negative lane result simply contributes zero. The `ChargeAccumulated` total itself can only be reduced by effects that directly target it (e.g. Repulsor's −15/30/45 Charge applies directly). The "Negative charge disabled in lanes" Engine mod prevents any per-lane Charge reduction from applying.
- **Losing Crew Health** — Whenever unblocked damage is done in a lane, all crew slotted in that lane lose one health.
- **Fatigue** — The same crew member may be slotted many times into the same formation, including into slots with no lane connectivity; each use adds one Fatigue. Exceeding the fatigue limit (based on Endurance stat) causes the crew member to EXHAUST, making them unavailable for the rest of the encounter. 1 Fatigue is removed after each page from each crew member. There is no maximum number of times a crew member can be slotted per formation.
- **Crew Death** — When a crew member's HP reaches 0, they are permanently removed from the roster. Death-related traits and mods trigger normally on the page death occurs.
- **Panic** — Panic is applied at the **start** of a page. A panicked crew member cannot be slotted that page. Enemy abilities that cause panic take effect on the following page, not the page they are declared. Panic subsides at the start of the next page.
- **Modified Slots** — When a slot is modified, it replaces the existing slot type; a slot can only ever have one type. "Adding a slot" always means converting a neutral (Empty) or negative slot to the new type — positive slots cannot be targeted by upgrade-style modifications unless explicitly stated. Crew members are unslotted at the end of every page and re-slotted during the next Planning Phase. **Slot modifications made during combat are temporary** — they apply for the remainder of that combat but reset when the combat ends. Modifications made outside of combat (e.g. via the Upgrade/Downgrade Formation event) are permanent.
- **Lock-On** — Scales all effects placed by enemy weapons in that lane, including Threat, Burn stacks, and lock-on additions. All per-lane values from the enemy scale with the lock-on multiplier (see Numerical Reference).
- **Empty Slot Modifiers** — Effects that trigger on "empty slots" (e.g. "empty slots shield 1") apply to all unoccupied slots in the currently active formation that have lane connectivity. They fire once per empty connected slot into each of that slot's connected lanes.
- **Silent Racial Passive** — At the start of each page, the Silent crew member's passive temporarily modifies one random slot in the active formation to Boosted for that page only. This fires even if the Silent crew member is exhausted.
---

## Combat Boosts & Penalties

These are modifiers applied at the start of a combat encounter (e.g. from Sabotage Shop events or enemy special abilities).

### Positive
- Invulnerability on the first turn in combat
- Prevent the first instance of damage
- Crew members get +1 effect level
- Draw +1 formations per page
- Boost 1 random slot each page
- Hack 2 in all lanes each page
- Lower charge threshold by 10%
- Empty slots shield 1
- Empty slots charge 1
- Crew members get +1 end (temporarily)

### Negative
- +1 lock-on in all lanes to start
- +2 burn in all lanes to start
- Crew members get −1 effect level
- Draw −1 formations each page
- +1 pain slot each page
- Deployables cannot be used
- A random crew member panics each page
- Raise charge threshold by 10%
- Gain +1 lock-on in a random lane each page
- Empty slots produce Threat 2

---

## Formations

> *Note: We will use a prefab asset library for formation definition.*

Formations are stored in the **Formation Archive** (the player's deck). The archive starts with the four **Starting Formations** (Flat, Line, Flank, Center) and grows through combat rewards and events. There is no maximum deck size.

**Combat Reward Formations** are drawn from a weighted pool of 3 options:
- 70% Common
- 20% Uncommon
- 10% Rare

By default 3 options are presented. This count can be modified by Computers mods.

### Slot Type Legend

| Symbol | Type | Effect |
|--------|------|--------|
| `E` | Empty | No modifier — crew member's ability applies normally |
| `B` | Boosted | +50% effect to the crew member in this slot |
| `P` | Pain | Deals 1 damage to the crew member slotted here each page |
| `H` | Heal | Heals 1 HP to the crew member slotted here each page |
| `G` | Growth | Grants +1 permanent effect level to the crew member slotted here (persists to future pages) |
| `F` | Fatigue | Adds +1 fatigue to the crew member slotted here each page |
| `M` | Minus Fatigue | Removes 1 fatigue from the crew member slotted here (net −1 fatigue) |
| `K` | Kill | Immediately kills the crew member slotted here |

### Lane Connectivity Notation

Slots are connected to one or more lanes using `->`. A slot with no lane listed applies its effect to the crew member personally but does not fire their ability into any lane.

- `E -> L` — slot connected to Left lane
- `E -> C` — slot connected to Center lane
- `E -> R` — slot connected to Right lane
- `E -> L, C` — slot connected to both Left and Center lanes
- `E -> E -> L` — two chained slots both connected to Left lane. Chained slots are linked visually in the UI and share a chain ID in code. **Currently, chaining has no additional mechanical effect** — each slot in a chain is filled independently — but the system is designed to support future mechanics.
- `(2x)` — crew member's ability fires at double effectiveness in this slot


---

### Starting Formations

#### Flat
```
E -> L
E -> C
E -> R
```

#### Line
```
E -> E -> E -> C
```

#### Flank
```
E -> E -> L
E -> E -> R
```

#### Center
```
E -> L, C, R
```

---

### Common Formations (60%)

#### Encircle
```
E -> E -> L, R
M -> C
```

#### Column Center
```
B -> B -> E -> E -> C
```

#### Bide
```
G -> L
G -> G -> C
G -> R
```

#### Chandelier
```
E -> L, C, R
E -> L, C
E -> C, R
```

#### Leftron
```
E -> L  (2x)
E -> L
B -> L
E -> L
```

#### Rightron
```
E -> R  (2x)
B -> R
E -> R
E -> R
```

#### Bastion
```
B -> L, C
M -> C, R
```

#### Layers
```
E -> E -> L
E -> E -> C
E -> E -> R
```

#### Command
```
E -> L
E -> C
E -> R
G -> L, C, R
```

#### Focus
```
E -> E -> L, C, R
```

#### Pincer
```
E -> E -> E -> L
E -> E -> E -> R
```

#### Retreat
```
H
H
H
G
G
G
```
*All six slots have no lane connectivity — they heal or grow the slotted crew member directly.*

#### Pleasure & Pain
```
P -> H -> L, C
P -> H -> C, R
```

#### Middle Deflection
*Passive: Reduce lock-on by 1 in the Center lane.*
```
E -> L
E -> E -> E -> C
E -> R
```

#### Left Deflection
*Passive: Reduce lock-on by 1 in the Left lane.*
```
E -> E -> E -> L
E -> C
E -> R
```

#### Right Deflection
*Passive: Reduce lock-on by 1 in the Right lane.*
```
E -> L
E -> C
E -> E -> E -> R
```

#### Resolution
*Passive: Crew cannot panic this page.*
```
E -> E -> L
E -> C
E -> E -> R
```

#### Squeeze
```
E -> B -> L, R
```

#### Reprieve
```
M -> M -> L
M -> C
M -> M -> R
```

#### Column Left
```
B -> B -> E -> E -> L
```

#### Column Right
```
B -> B -> E -> E -> R
```

#### Echo
*Passive: 25% of all effects are repeated on the next page.*
```
E -> L
E -> C
E -> R
```

#### Pendant
```
E -> E -> L
F -> L, C, R
E -> E -> R
```

#### W
```
E -> L
E -> C
E -> R
E -> L, C
E -> C, R
```

---

### Uncommon Formations (30%)

#### Unify
*Passive: Gain +1 effect level for each slotted crew member with the same race.*
```
E -> L
E -> L, R
E -> R
B -> C
```

#### Champion
*Passive: Gain +1 effect level for each different race among slotted crew members.*
```
M
M
M
M
M
M
E -> L, C, R
```
*The six M slots have no lane connectivity — they reduce fatigue of slotted crew directly.*

#### All Out
```
F -> F -> F -> L, C, R
```

#### Left Bias
```
M -> E -> E -> L, C
```

#### Right Bias
```
M -> E -> E -> C, R
```

#### Full House
*Passive: Empty slots produce 5 Threat.*
```
E -> E -> E -> L
E -> E -> E -> C
E -> E -> E -> R
```

#### Exemplify
```
E -> L, C
E -> C, R
G
G
G
```
*The three G slots have no lane connectivity — they grow the slotted crew member directly.*

#### Triple Threat
```
B -> L
B -> C
B -> R
```

#### Arch
```
H -> M -> L
E -> C
G -> B -> R
```

#### Power Column
*Passive: Boost is 50% more effective.*
```
E -> E -> E -> E -> M -> C
```

#### Graveyard 1
*Passive: Exhausted crew members slotted here get +50% to their final effect output (applied after all other modifiers, including Morale).*
```
E -> L
E -> E -> E -> C
E -> E -> R
```

#### Graveyard 2
*Passive: Exhausted crew members slotted here get +50% to their final effect output.*
```
E -> L
E -> E -> E -> R
E -> E -> C
```

#### Graveyard 3
*Passive: Exhausted crew members slotted here get +50% to their final effect output.*
```
E -> R
E -> E -> E -> C
E -> E -> L
```

#### Graveyard 4
*Passive: Exhausted crew members slotted here get +50% to their final effect output.*
```
E -> R
E -> E -> E -> L
E -> E -> C
```

#### Shieldcore
*Passive: Shield effects get +50% effect.*
```
E -> L
E -> C
E -> R
```

#### Hackcore
*Passive: Hack effects get +50% effect.*
```
E -> L
E -> C
E -> R
```

#### Chargecore
*Passive: Charge effects get +50% effect.*
```
E -> L
E -> C
E -> R
```

#### Awakener
*Passive: After each page, replace a Fatigue slot on this formation with a Boosted slot (whether it was used or not). Once all Fatigue slots have been converted, the passive has no further effect. These conversions do not persist between combats — the formation resets to its original slot configuration at the start of each new combat.*
```
F -> F -> F -> L
F -> F -> F -> C
F -> F -> F -> R
```

#### Acrobat Left
```
E -> E -> L
E -> L
E -> E -> C, R
```

#### Acrobat Right
```
E -> E -> L, C
E -> E -> R
E -> R
```

#### Brainform
*Passive: Use Int instead of Res for determining effect level this page — applies to all crew members slotted in this formation.*
```
E -> E -> L
E -> P -> C
E -> E -> R
```

#### Smileform
*Passive: Use Cha instead of Res for determining effect level this page — applies to all crew members slotted in this formation.*
```
E -> E -> L
E -> P -> C
E -> E -> R
```

---

### Rare Formations (10%)

#### Superfocus
```
B -> C  (3x)
```

#### Medbay
*Passive: Heal slots are Boosted for crew members who are already at full health.*
```
H -> L
H -> C
H -> R
H
H
H
```
*The bottom three H slots have no lane connectivity.*

#### Sacrifice
*Passive: The K slot kills the crew member slotted into it before any abilities fire. Death-related traits and mods (e.g. "when dies, set morale to 30%") trigger as normal. The sacrificed crew member does not fire their ability on this page. All other slotted crew members gain +5 effect level this page.*
```
K
E -> E -> L
E -> L, C, R
E -> E -> R
```

#### Horseshoe
```
E -> L, R
E -> E -> E -> L
E -> E -> E -> R
```

#### Phalanx
*Passive: Lock-on cannot increase this page.*
```
E -> L
B -> C
E -> R
E -> L, C, R
```

#### Lavafield
```
P -> P -> P -> P -> L
P -> P -> P -> P -> C
P -> P -> P -> P -> R
```

#### Ascension
*Passive: The crew member in this slot gains +2 permanent stats distributed randomly across Int, Res, and Cha (could be +2 to one stat or +1 to two).*
```
E
```
*No lane connectivity — does not fire the crew member's ability.*

#### Phantom
*Passive: You can't take damage this page, but Threat does not dissipate. The combat end check still occurs normally — if sufficient Charge is generated while Phantom is active, the combat ends successfully and the leftover Threat is ignored.*
```
E -> L
E -> C
E -> R
```

#### Dancers
```
M -> L, C
M -> C, R
M -> L, R
```

#### Necropolis
*Passive: Each exhausted crew member slotted here Hacks 15 and Charges 3 this page, applied once per exhausted member into each lane their slot is connected to.*
```
F -> L, C
F -> C, R
F -> F -> L, C, R
```

---

## Enemy Weapons

*Damages are given as S/M/L for Act I/II/III. Each weapon shows effects per lane (Left, Middle, Right). Lanes are fixed unless noted as "Randomized."*

| Weapon | Left Lane | Middle Lane | Right Lane | Notes |
|--------|-----------|-------------|------------|-------|
| **Left Beam** | M/L/XL Threat | +1 lock-on | +1 lock-on | — |
| **Middle Beam** | +1 lock-on | M/L/XL Threat | +1 lock-on | — |
| **Right Beam** | +1 lock-on | +1 lock-on | M/L/XL Threat | — |
| **Broad Burn** | 3/4/5 Burn | 3/4/5 Burn | 3/4/5 Burn | — |
| **Lock On** | +1 lock-on | +1 lock-on | +1 lock-on | At max lock-on → L/XL/XXL Threat in all lanes |
| **Hack** | S/M/L Hack | +1 lock-on | S/M/L Threat | Randomized lanes (e.g. lock-on left, threat middle, hack right) |
| **Broad Beam** | S/M/L Threat | M/L/XL Threat | S/M/L Threat | Randomized lanes |
| **Panic Ray** | Crew panic | +1 lock-on | 3/4/5 Burn | Randomized lanes |
| **Repulsor** | +1 lock-on | −15/30/45 Charge | S/S/M Threat | Randomized lanes |
| **Shield Piercer** | S/S/M Piercing Threat | S/M/L Threat | +1 lock-on | Randomized lanes; Piercing Threat cannot be shielded |
| **Predictable Blast** | +1 lock-on | L/XL/XXL Threat | +1 lock-on | Shield is doubly effective |
| **Counterblaster** | Threat = charge generated this turn | Threat = charge generated this turn | +1 lock-on | Randomized lanes |
| **Long Term Threatener** | 3/4/5 Burn | +2 lock-on | Inflict −5% morale for each slotted crew in this lane | Randomized lanes |
| **Incinerator** | 6/8/10 Burn | +1 lock-on | +1 lock-on | Randomized lanes |
| **Fading Canon** | M/L/XL Threat | M/L/XL Threat | −1 lock-on | Randomized lanes; −3 threat for each other time it's been used |

---

## Enemy Special Abilities

The following are passive abilities that may be assigned to enemies:

- Invulnerable on even-numbered pages
- Lock-on abilities get +1
- Increase charge threshold by 50%
- All formations get +1 pain slot
- Increase threat by 30%
- Makes a crew member panic each page
- Morale drops 5% each page
- No passive charge generation
- All formations +1 fatigue slot
- 25% chance of critical threat (doubles threat)
- Apply 1 burn each page
- Deployables can't be used
- Only draw 1 formation each turn
- Generating charge in lanes also generates equal Threat in that lane — applies per lane independently
- +1 lock-on in all lanes whenever a crew member exhausts
- A random crew member is kidnapped and can't be used this combat
- Crew members take +1 damage from unmitigated threat
- Enemy intents in lane are hidden (until page confirmation)
- Enemy actions affected by player boost
- Immune during first two pages
- Whenever you charge in a lane, it gets +1 lock-on
- 25% of unmitigated Threat persists into the next page instead of fully clearing

---

## Act Bosses

### Act I Boss

- +1 lock-on in all lanes each page until full lock-on
- L Threat in all lanes once at full lock-on
- Before full lock-on: NO CHARGE, M Threat, PAIN in the three lanes (randomized)
  - PAIN makes all slots connected pain slots (overriding even other slot types)

---

### Act II Boss

**Phase 1** (>50% remaining charge threshold):
- Burn 7 in one lane, +1 lock-on in others
- M damage in two random lanes, +1 lock-on in last
- −12 Charge in all lanes

**Phase 2** (<50% remaining charge threshold):
- First page takes no action
- Then, if there is Burn, convert all Burn into 3× Threat. Any empty lanes instead get 7 Burn; then repeat this until we successfully threaten
- After, alternate between: XXL attacks in one lane, L in another, and Burn 7 in last and doing nothing

---

### Act III Boss

- Every other turn, starting on page 1, panic X crew members, where X is the number of times we've done this ability (1, 2, 3, ...)
- On panic turns: apply M, L, XL Threat in the lanes
- On non-panic turns: apply +1 lock-on and −10% PERMANENT boost reduction in all 3 lanes. This stacks indefinitely across pages with no floor — if the total reaches −100% and the player cannot raise boost higher, they will be unable to generate lane effects. This debuff adds with whatever boost the player can produce.
- When it reaches 50% charge threshold, go invulnerable on the next page

---

## Final Boss

- The Final Boss requires **400 Charge** to escape. Charge can only be generated while the Super Shield is down.
- Super Shield prevents charging — it has 100 points. Hack and Charge both reduce the Super Shield.
- On the page the Super Shield is down, actual Charge generates and the enemy simply does +1 lock-on in all lanes.
- Several possible attacks — choose one per lane, no repeats:
  - Death Laser: instantly kills a random crew member
  - XL Threat
  - Burn 12
  - Reduce morale by 10%
  - Permanently adds pain to all connected slots for the rest of the fight
  - S Threat (piercing)
  - Repair Super Shield 30
  - L Threat

---

## Ship Systems

The Apogee has several systems that can be upgraded in one of two ways. The first is a core upgrade which increases the power of the system and unlocks a Mod slot. Each system may be upgraded a maximum of 3 times. The second is to gain Mods — passive bonuses that help in a variety of ways. Each Mod takes up some number of slots.

**Mod Slots per System:** Each system starts with 3 mod slots at level 0 and gains 1 additional slot per upgrade, up to 5 slots at level 3.

**Glitch Equipping Rules:** Glitches must always be equipped in their matching system if a slot is available. If no slot is available, the Glitch goes into mod storage. If equipping a new Glitch would require displacing an existing mod (not another Glitch), it does so — the displaced mod moves to storage.

| System | Function |
|--------|----------|
| **Engine** | Determines starting Charge in combat |
| **Cabin** | Determines max Crew capacity |
| **Hull** | Determines max Hull HP. Upgrading the Hull raises both max and current HP by the difference (e.g. upgrading from level 0 to level 1 grants +20 max HP and +20 current HP, even if already at full health). |
| **Computers** | Determines number of formations drawn per turn |

### System Level Values

| System | Lvl 0 | Lvl 1 | Lvl 2 | Lvl 3 |
|--------|-------|-------|-------|-------|
| Engine (Starting Charge) | 0 | 15 | 40 | 75 |
| Cabin (Crew Capacity) | 4 | 5 | 6 | 7 |
| Computer (Formations/Turn) | 2 | 3 | 4 | 5 |
| Hull (Max HP) | 80 | 100 | 120 | 140 |

---

## Mods

Mods are passive bonuses installed in system slots. Each has a size designation — (S), (M), or (L) — which determines how many slots the mod occupies: S = 1 slot, M = 2 slots, L = 3 slots.

### Hull Mods

- Regenerate 1 Hull every page (S)
- Reduce all incoming Hull damage by 1 (S)
- Whenever the Hull takes damage, gain 5 coin (S)
- While you have <25% Hull, crew members get +1 effect level (S)
- Whenever the Hull takes damage, a random crew member gets +1 effect level this combat (S)
- Reduce all incoming damage by 33% (L)
- Empty formation slots heal 2 (S)
- When a crew member exhausts, heal 2 (S)
- While Hull integrity is 100%, all crew members get +1 effect level (M)
- Whenever you take damage, charge that much in all lanes (L)
- Whenever you shield, gain 25% of that on the next page (M)
- You are immune to damage on the first page of combat (L)
- Whenever you hack, get that amount of Shield (M)
- Boost affects shielding and healing 20% more (S)
- Leftover shields are retained (L)

### Engine Mods

- Lanes with max lock-on get +100% boost (S)
- Empty slots charge 2 (M)
- Increase passive charge generation by 50% (M)
- Whenever a crew member exhausts, charge 2 in connected lanes (S)
- All crew members gain an additional "Charge 1" (S)
- Charge thresholds are 10% lower in boss battles (M)
- Whenever you charge, also Shield for 10% of that value (M)
- Whenever you escape combat, heal the Hull 10% (S)
- Whenever you escape combat, gain a random deployable (M)
- If unmitigated Threat would deal damage but its value is less than 5, prevent all of that damage (S)
- Base engine charge increased by 50% (L)
- If you do not take any charge action on pages, passive charge gain is doubled (L)
- Whenever you escape combat, morale is increased by 10% (S)
- Negative charge disabled in lanes (S)
- Boost only loses 50% of its value each page (L)

### Computers Mods

- You can't be hacked (S)
- Empty slots shield 2 (M)
- Maximum lock-on is reduced by 1 (S)
- Boost 10% for every lock-on in lanes (S)
- Hack 2 in all lanes every page but start combat with +1 lock-on in all lanes (L)
- One slot each page that isn't boosted becomes boosted for this combat (S)
- One slot each page that isn't a −1 fatigue slot becomes a −1 fatigue slot this combat (S)
- Permanently boost 20% in the flank lanes (S)
- Permanently boost 40% in the middle lane (M)
- Whenever you hack, increase amount by 1 (S)
- Get +1 formation choice after combat (L)
- Get +1 formation choice each page (L)
- It is possible to redraw the same formation on subsequent pages (M)
- Boost all slots in starter formation (M)
- Boosted slots boost 25% more (M)
- −1 fatigue slots also boost 25% (S)
- Heal slots heal 1 more (S)
- Rare formations appear twice as often (S)

### Cabin Mods

- Whenever a crew member exhausts, Shield 2 in connected lanes (S)
- Crew members cannot panic (S)
- Whenever you escape combat, gain 10% interest on your current coin total (S)
- Crew members get +1 effect level for every 100 coin you have up to +5 (L)
- New crew members get +1 to a random stat (res, cha, int) (S)
- Whenever a crew member dies, all others gain +2 res (M)
- Exhausted crew members recover after 2 pages (L)
- Gain +1 maximum cabin capacity (M)
- Crew members do not take damage from overflow threat (L)
- If you have 5 different races among crew members, all get +1 effect level (S)
- If you have exactly one crew member, they cannot die and get +5 to int, cha, res, and +1 end (L)
- The first crew member used each page gains no fatigue (S)
- Use cha instead of res for determining effect level (M)
- Crew members with 2 or more fatigue get +2 effect level (S)
- Crew members heal to full health after combat (L)
- Crew get +1 effect level on the first page of combat (S)
- Crew lose +1 fatigue each page (L)

---

## Glitches

Glitches are negative mods that **must** be equipped if able.

### Hull Glitches

- Take +1 damage from all sources (S)
- Hull cannot be healed in combat — healing effects still work but don't apply until after combat (S)
- Take double damage from pain slots (M)
- Lose 1 coin after EVERY PAGE (M)

### Cabin Glitches

- Morale is capped at 80% (S)
- Exhausted crew members permanently gain −1 res (M)
- New crew members get −1 to a random stat (S)
- Crew members begin combat with 1 fatigue (S)

### Computers Glitches

- Add a +1 fatigue slot each page (S)
- Add a pain slot each page (S)
- Only get 1 option for formation removal (M)
- Get 1 fewer formation option from combat rewards (M)

### Engine Glitches

- Increase charge thresholds by 10% (S)
- Whenever you take damage, lose that much charge (M)
- All enemies get a random extra bonus ability (S)
- Start combat with 1 lock-on in each lane (S)

---

## Deployables

Up to 3 Deployables can be stocked — each grants a powerful one-time effect when expended. **Deployables are once-ever consumables: using one permanently removes it from the run.** The following are the available Deployables:

- Replace all empty slots with Shield 5
- Lose 10% morale; become invulnerable this page
- Heal 25% hull
- Boost 100% in all lanes
- Unexhaust all crew members and set fatigue to zero
- Crew members do not gain fatigue this page
- Immediately escape a non-boss combat; all crew members gain −1 res
- Redraw formations for this page
- Gain 20% morale
- Reduce lock-on in all lanes to zero
- Increase your current coin total by 30%
- Charge 30
- Cure all negative traits from crew members
- Destroy a random glitch

---

## Negative Traits

The following traits may be assigned to crew members as negative traits (e.g. via events):

- If this crew member panics, they exhaust
- −1 res
- −1 cha
- −1 int
- −1 end, +2 res
- Take double damage
- Gain +2 Threat to effect
- Panics whenever they take damage
- Can't be boosted
- −1 res after each page in combat
- −1 effect level for each lock-on in lane
- −1 effect level for each different race crew member on the ship
- Cannot heal
- When dies, set morale to 30%
- −1 effect level whenever hull is damaged
- −1 effect level in boss fights
- Intelligence contributes 50% less to passive charge
- Morale affects this crew member doubly (e.g. 60% morale → .6×.6 = .36)
- 2 max HP
- Gain −1 to a random stat after each combat

---

## Locations & Special Events

*(Note: Many more special events will be implemented later.)*

The following locations are available in the game. Some have defined special events; others are TBD.

| Location | Special Event |
|----------|---------------|
| Desolate Anchor 003 | TBD |
| The Drift | Gain Brittlebones crew |
| Jensonia | Copy one crew member's stats to all others (option style) |
| Mycelia | Gain Silent crew |
| Merkaj | Clear a random Glitch (announcement style; only available if a Glitch is equipped) |
| Odi | TBD |
| Jaguan | TBD |
| Melflux | TBD |
| Hermestris | TBD |
| Ludona | TBD |
| Sigg | Gain Steelborn crew |
| Enjukan | Gain Replicant crew; convert all current crew to Replicants (changes race, stat bonuses, and racial passive) |
| Voidwatch | Reassign all crew member abilities randomly, respecting standard rarity weights |
| Dunson 4 | TBD |
| Khorum | Gain Dwarf crew |
| Macedon | TBD |
| Kraethos | Gain Symbiote crew |
| Kaimitrio | TBD |
| Luxion | TBD |
| Sol | TBD |

---

## Numerical Reference

### Starting Values

| Resource | Value |
|----------|-------|
| Apogee Hull | 80 |
| Starting Coin | 100 |
| Pages per Run | 200 |

### Coin Drops

| Size | Range |
|------|-------|
| S | 30–50 |
| M | 50–80 |
| L | 80–110 |
| XL | 110–150 |

*(Values chosen randomly between the thresholds.)*

### Enemy Threat Values

| Size | Range |
|------|-------|
| S | 3–6 |
| M | 7–15 |
| L | 16–24 |
| XL | 25–36 |
| XXL | 37–55 |

### Taking Damage / Heal Amounts

| Size | Range |
|------|-------|
| S | 4–9 |
| M | 10–13 |
| L | 14–19 |
| XL | 20–25 |

### Charge Thresholds by Chapter

Chapters 3, 6, and 9 are act-ending boss encounters and have elevated thresholds reflecting larger enemy health pools. The non-monotonic progression is intentional.

| Chapter | Threshold | Note |
|---------|-----------|------|
| 1 | 50 | |
| 2 | 75 | |
| 3 | 200 | Act I Boss |
| 4 | 125 | |
| 5 | 150 | |
| 6 | 400 | Act II Boss |
| 7 | 225 | |
| 8 | 250 | |
| 9 | 650 | Act III Boss |
| 10 | N/A | Final Boss — requires 400 Charge while Super Shield is down |

### Lock-On Modifier

Lock-on adds to the base effect of enemy weapons in a lane. The values below are the **additional** multiplier on top of the base (100%) effect. Lock-on caps at **5** by default (some mods can reduce this cap).

| Level | 0 | 1 | 2 | 3 | 4 | 5 |
|-------|---|---|---|---|---|---|
| Additional Modifier | +0% | +20% | +40% | +60% | +80% | +100% |
| Total Multiplier | 100% | 120% | 140% | 160% | 180% | 200% |

### Shop Costs

| Item | Cost Range |
|------|-----------|
| Deployables | 45–75 |
| Mods (S) | 140–160 |
| Mods (M) | 160–200 |
| Mods (L) | 200–240 |
| Engine Upgrade | 225–275 |
| Cabin Upgrade | 180–220 |
| Computers Upgrade | 235–285 |
| Hull Upgrade | 160–230 |
| Heal 1 | 8 |
| Heal 5 | 35 |
| Heal 15 | 75 |
| Heal 30 | 140 |
| Heal Full | 250 |
| Morale 5% | 25 |
| Morale 20% | 75 |
| Morale 50% | 140 |
| Morale Full | 200 |
| Combat Boost | 65–85 |

---

## UI Design

### General Button Behavior

Buttons throughout should:
- Grow somewhat whilst hovered over
- Whilst mouse down on them reveal a grey overlay
- Play an SFX on mouse down

---

### Main Menu

```
+--[Profile]--------------------------------[Run History | Stats | Achievements]--+
|  [R][*]         TORN PAGES          [||||]                                     |
|  [P1][P2][P3]   [Diaspora II cover]  [Run History panel]                       |
|                 [Start]              [Achievements panel]                       |
|  Toggle/Delete/Create Profile        [Stats: Runs Played / Won / Lost]          |
|  (Profile 1 auto-created)                                                       |
|                 [Settings Menu (overlay)]                                       |
+---------------------------------------------------------------------------------+
```

- Title music plays; starting the game causes the book to flutter open and camera to zoom in
- While in settings menu, add a muted filter to music
- Profile button loads other profiles
- Profile 1 created automatically; up to 3 profiles per game download

**Settings Menu (Overlay):**
Tabs: `[Gameplay] [Graphical] [Audio] [Language] [Controls]`

- Clicking tabs switches to new options; hovering changes color to slightly brighter; SFX plays when clicking
- Cancel: undoes to settings in profile
- Save: retains options on this profile

**Settings Options:**
- *Gameplay:* Text speed (Slow / Normal / Fast), more TODO
- *Graphics:* Screen Resolution
- *Audio:* Master Volume, Music Volume, SFX Volume
- *Language:* English (more TODO)
- *Controls:* TODO

---

### Left Page UI

The left page shows the current game state status as well as the DELTA from whatever actions are being chosen on the right page. The left page is the same regardless of the page type on the right.

> All UI panels and interactive elements should include verbal descriptions — tooltips on hover and inspector entries on right-click — for every object rendered. This applies globally across all UI screens.

```
+------------------------------------------------------+
| Hull [WWWWW] 22/30  [Coin: 102] [|||||||] (Mod Slots)|
| Engine: 30          [||||||||]                        |
| Cabin 4/6                                             |
|  [~crew~]  [~crew~]                                   |
|  [~crew~]  [~crew~]                                   |
|  [Empty ]  [Empty ]                                   |
|  [~crew~]  [~crew~]                                   |
| Computers: 2  [Formation Deck: 1] [||] [Mod Store]    |
+------------------------------------------------------+
```

- Hull bar: color changes Green/Yellow/Red at 100%/50%/0%; hover for tooltip
- Delta shown on HP bar
- Engine: hover for tooltip; shows starting charge value
- Cabin: hover for tooltip explaining capacity
- Coin: delta represented with "+" symbol
- Mod Slots: hovering over a mod enlarges it somewhat
- Unavailable slots blocked out; empty slots labeled as such
- Deployable button: pulls up deployable inventory and gives option to use
- Mod Store button: shows Mod Storage overlay on top of right page

**Formation Viewer (bottom of left page):**
```
[FormationName] [FormationName] [v]  <- scroll if more formations than fit
[  slot  ][  slot  ][  slot  ]
[  slot  ][  slot  ]
     [Boosted 50%]
```

- Labeled with name and image; selected one gets a box around it
- Formation last used in combat will be greyed out
- Special slots colored differently; hovering brings up tooltip with slot description
- Bottom section shows the formation diagram

---

### Build Up UI (Right Page)

```
+-----------------------------------------------------+
|  [Left Page — same as usual, no Deltas needed]      |
|                    Dramatic Tension  0/9             |
|  [event][event][event]   Soundtrack for Act I/II/III/IV |
|  [event][event][event]                              |
|  [event][event][event]                              |
|  [event][event]                                     |
|                                      [Ready]        |
+-----------------------------------------------------+
```

- Events organized loosely into rows
- Selected events highlighted with a backdrop
- Event color coding: Green = purely positive, Yellow = mixed, Red = negative
- Each event shows its Dramatic Tension contribution
- Ready button greyed out while there is insufficient Dramatic Tension
- Events drawn randomly from available pool:
  - At least 3 negative events guaranteed
- Cannot generate an unclickable event (one that cannot be chosen and also reach DT threshold)
- Number of events drawn scales with Charisma

---

### Combat Pages UI (Right Page)

```
+-----------------------------------------------------------------+
|  Enemy Name  [Charge Progress Bar: Current | Delta (flashing)] |
|  20/100HP    [weapon][weapon][weapon]  <- current highlighted   |
|  Threat: 10  [lock-on indicator, delta flashing]               |
|  Shield: 3                                                      |
|   [Left Lane]    [Mid Lane]   [Right Lane]                      |
|   [slot][slot]  [slot][slot]  [slot][slot]  <- crew slots       |
|   [slot]        [slot]        [slot]        <- drag from left   |
|   O  [+][+]   [Ready]                                          |
|      ^bonus formation effect                                    |
|      Drawn formations — click button to toggle                  |
+-----------------------------------------------------------------+
```

- Combat soundtrack (1 for normal boss, 1 for final boss)
- Text field with verbal descriptor of enemy's actions; hover to see combat modifiers
- Enemy weapons: hover for tooltip, right-click for details; current weapon highlighted
- Lock-on indicator with flashing delta
- Lanes have text object explaining effects in that lane
- Crew slots: drag a crew member from the left page to slot them
- Enemy intents drawn from pool, randomly, but with no repeats (including tears)

---

### Crew Card UI

```
+------------------------------+
| NAME  SURNAME            [4] |
| Effect text (what they do    | [3]
| in combat currently)         | [2]
| [~~][~~][  ][  ][  ][  ] 3/3 |
+------------------------------+
```

- Top-right: Resolve, Intelligence, Charisma stats stacked
- Bottom row: always 6 rectangles representing current health; Green = healthy, Red = damaged, Black = unavailable (if max HP < 6)
- Fatigue/Endurance: numerator is Endurance − Fatigue; if exhausted replace with "X" (flashes if forecasted delta)
- Hovering enlarges the card; right-click to inspect
- SKULL overlay (flashing if delta) for dead in combat
- **Outside of combat**, the effect text displays the crew member's base ability description computed from their base Effect Level (1 + Res), with any relevant trait modifiers noted. This requires a code system that can convert ability + modifiers into a readable text description (e.g. "Shield 8 — boosted to Shield 12 by trait"). Endurance stat is also shown on the card outside combat.

---

### Inspector

RIGHT CLICKING on any object in the game will bring up the Inspector overlay. The overlay contains the name of the object and its description. All objects we draw should have this.

```
+------------------+
|   Name           |
|   All about the  |[scroll bar]
|   thing... lorem |
|   ipsum...       |
| [X]          [←] |
|  ^Dismiss    ^Returns to previous entry
|              if we accessed a glossary entry
+------------------+
```

For Crew and Mods, the inspector must be extended to include their effects, traits, stats, etc.

---

### Glossary

We maintain a mapping between terms and descriptions. Terms pertain to lore or mechanics. Whenever a term appears in descriptive text, we should highlight or underline the term. Right-clicking the term will pull up the inspector panel with the glossary entry for that term. We can chain glossary terms if glossary entries reference other entries — we also queue these and allow a button to return to previously viewed entries.

---

### Crew Dismissal

If crew are gained beyond cabin capacity, a special window will appear prompting dismissal of a crew member. Overlays right page. **Dismissal is mandatory** — the player cannot close this modal without dismissing a crew member. There is no cancel option.

```
+-------------------------------+
| [Cabin panel]  CREW DISMISSAL |
|                [extra crew card]
|                DISMISS        |
|                [drop target]  | <- drag crew member here for dismissal
|                [Confirm]      |
+-------------------------------+
```

*Note: Whenever we slot a crew member to a slot, we should play a "slotted" sound effect.*

---

### Mod Cards UI

Mod cards display as rectangles. Width based on number of slots needed. Icons for each mod — for now, use the first two letters of the name (e.g. `[Ik]` for Ikean Brainstone).

```
[O ]  [O  ]  [O    ]
 S      M       L
```

- Hovering over a mod card grows it and shows a tooltip with name and description of its effect
- Mods are system-specific (Hull, Engine, Computers, Cabin) and can only be placed in slots of the matching system. No neutral mods are defined for the initial implementation, though the system should support them for future additions.
- All mods can be dragged to/from the Mod overlay while it is open on the right page

**Mod Storage Overlay:**
```
+----------+
|  [Xx]  ^ |
|  [Xx]  | |  <- scroll bar if too many mods
|  [Xx]  v |
|      [X]  |  <- close button
+----------+
```

- Mods may be placed in storage (infinite capacity)
- This happens automatically if there are too many mods and no slots exist for a newly acquired one

---

### Combat Rewards Page

After combat, player chooses a reward formation. The Coin gained from the encounter (based on special abilities chosen — S, M, or L) is displayed prominently on this screen.

```
+----------------------------------------------+
|    Coin Gained: [amount]                     |
|          Choose your Reward                  |
| [+][+][+]  <- formations (click to select,   |
|              show below as preview)           |
|    [_]  [_]  <- formation previews           |
|                                               |
|              [Select]  <- unclickable if none |
+----------------------------------------------+
```

- Rewards drawn from weighted pool: 70% Common, 20% Uncommon, 10% Rare
- Number of options presented equals 3 by default (modified by Computers mods and number of special abilities chosen on the enemy selection screen)
- Selected formation highlighted
- After clicking, gain the chosen formation

---

### Events UI

**Announcement Style Event (Right Page):**
```
+-----------------------------+
| [image/icon]                |
| Lore text (typewriter)     ^|
|                             |
|              [Event effect announcement]
|             [Continue]      |
+-----------------------------+
```

**Option Style Event (Right Page):**
```
+--------------------------------------------+
| Description text ->                        |
| (crew card shown if crew member involved)  |
| [Option 1] [Option 2] [Option 3]           |
| Further lore revealed AFTER selection      |
|                          [Confirm]         |
+--------------------------------------------+
```

**Shop Style Event (Right Page):**
```
+---------------------------------------+
| Description text                      |
| [item][item][item]                    |
| [item][item][item]   <- buy buttons   |
| hover for details, right-click full   |
|                             [Exit]    |
+---------------------------------------+
```

---

### Page Viewer (Bottom Widget)

A widget on the bottom of the screen allows the player to browse previous pages. When viewing a previous page, deltas are solidified and everything is read-only — exactly as it was when we last viewed it.

The widget has buttons for:
- Previous Page
- Next Page
- Current Page (go to last viewed)
- TEAR — only available on current page

Invalid buttons are greyed out. We also show current and remaining pages like "37 of 100."

---

### Author Interjections

At many points in the game, the Author will interject with a speech bubble overlay to say things:

- When players try an impossible action (e.g. using an exhausted crew member): *"That won't work"*
- When the Apogee faces lethal damage: *"Uh oh"*
- When a crew member dies: *"This will be a gut punch"*
- ...and so on.

We will add lots more interjections in the future.

---

### High Scores

We should implement a simple high score system that tracks game completions by difficulty level. In demo, we can store high scores in a local file and access with C# engine.

The high score is the game completion with **MOST REMAINING PAGES**. High scores are tracked **separately per difficulty level** — a run completed on difficulty 3 only competes with other difficulty 3 completions. Players should enter their name and submit high score after a successful run.

---

## System Design & Architecture

### Architecture Overview

```
          Game Data
              |
   UI <--> Engine
    \       /
     \     /
      State
```

- **Game Data** — Permanent information that persists between runs (e.g. enemy intents, mod data, formation data)
- **UI** — Responsible for reading the game state and displaying to users; takes user input and passes it to the Engine
- **Engine** — Determines what happens and holds all game rules/logic
- **State** — The game state

**RIGID separation of responsibility.** We need to ensure all code is correctly stored in the right layers. No UI editing state, for example.

**Game Data should be defined in code!** This will be most expedient for LLM edits. **NO STORING GAME DATA IN SCRIPTABLE OBJECTS.**

Prefabs only used by the UI layer to display the game content.

Most objects should minimally have a name, short description, and long description for tooltip on hover and inspector on right click.

Exception: We can use Scriptable Objects for asset libraries.

> **NOTE:** We should have a class that manages all text — for iconography, glossary highlights, automatic sizing to bounding box, etc.

---

### Maintaining State

We should create an über class that contains the full run state. It contains a list of `<Page>` representing the pages we have gone through so far. It can also contain run-level metadata (e.g. flags, chapters removed from pool). It should **NOT** contain any player state data (e.g. Hull HP, Coin).

- **Page** consists of LeftPage and RightPage
- **LeftPage** is a concrete class that contains player state data
- **RightPage** is an abstract class that is implemented by all the different scenarios (Combat, BuildUp, etc.); RightPage contains info on any generated options and any chosen player decisions

Using LeftPage + RightPage we can render the whole game UI. LeftPage info is used for current permanent state. RightPage will mutate and is used to render the right side of the UI and deltas on top of the left. **Deltas flash in the UI when viewing the current page. When viewing a historical page, deltas are displayed as solid (non-flashing), since those values are set in stone.**

---

### Page Viewer

We will show a widget on the bottom of the screen that allows the player to browse to previous pages. When viewing a previous page, deltas are displayed as solid (non-flashing) — they are set in stone — and everything is read-only, exactly as it was when we last viewed that page. When on the current page, deltas flash as normal to indicate pending changes. The widget has buttons for NextPage, PreviousPage, CurrentPage, and TEAR. TEAR only available on current page. Invalid buttons are greyed out. We also show current and remaining pages like "37 of 100."

---

## Demo Notes

- Goal is an end-to-end playable demo
- We will include background music and sound effects for:
  - Hovering
  - Clicking interactables
  - Taking damage
  - Victory in combat
  - Slotting things
- No art yet — just text and simple image box sprites
- Most descriptive text left as TODOs
- We **WILL** implement metaprogression, saving and loading

**Technology:**
Although the end goal is to do this in Unity, in the immediate term we will build this in pure C# and React:
- Headless C# engine should be fully capable of running the game on its own. We should be able to completely re-use this in Unity later. Clear interfaces that are protected against illegal moves.
- React front end interfaces with the headless engine
- Host engine locally on dev CPU
- Host React publicly (after local testing) to enable playtesters

**Testing:**
- Build a nice testing suite to verify functionality
- Simple agent that can play the game (poorly) and interact with the headless agent

---

## Miscellaneous Concepts

- **Prologue** — Gives an introduction to the story. The player creates their three starting crew members using the Crew Creation UI, then proceeds to the **Prologue Boon Screen**: a special one-time selection where the player chooses one item from each of three categories:
  - **1 of 3 randomly drawn Mods** — drawn from the full mod pool across all system flavors and sizes
  - **1 of 3 randomly drawn Common Formations**
  - **1 of 3 randomly drawn Deployables**
- **Epilogue** — At the very end, if the Shield is destroyed, a stat screen is shown before the message of hope. The stat screen tracks: total Charge generated, total Hack generated, and total Shield generated during the run. Additional stats are TBD. If the run ends in failure (hull reaches 0 or pages depleted), the game returns directly to the main menu with no end screen.
- **Run History** — A shelf of books; you can access all the previous runs played on this account.
- **Saving/Loading** — Runs may be paused, saved, and reloaded. We serialize the Book data structure and can reload easily from there. We also save metaprogression with all previous books and unlocked achievements and completed difficulty modifiers. 100% completion means winning with all metaprogression challenges and achievements. We will allow 3 profiles per game download with their own save data.

---

## Metaprogression

Metaprogression works similarly to an ascension system. Each time the player completes a run with all currently active difficulty modifiers enabled, a new difficulty modifier is unlocked. Modifiers stack — each playthrough the player opts in to all previously unlocked modifiers simultaneously.

For the initial implementation, difficulty levels scale infinitely, with each level adding the following modifier on top of all prior ones:

- **Each level:** Increase all Charge Thresholds by 10% (cumulative per level)

For example, a player on difficulty level 3 would face Charge Thresholds increased by 30% above baseline. Additional and more varied modifiers (e.g. reduced pages, enemy ability bonuses) will be designed and added in future iterations.

---

## Achievements

> *Note: Achievements are a placeholder system. The list below is minimal for the initial implementation; more achievements will be added in future iterations.*

Achievements are tracked per profile and saved alongside metaprogression data. The following achievements are defined for the initial implementation — one for each of the first 10 ascension (difficulty) levels:

| Achievement | Condition |
|-------------|-----------|
| First Draft | Complete a run on difficulty level 0 |
| Revised Edition | Complete a run on difficulty level 1 |
| Second Printing | Complete a run on difficulty level 2 |
| Hard Cover | Complete a run on difficulty level 3 |
| Critically Acclaimed | Complete a run on difficulty level 4 |
| Award Nominated | Complete a run on difficulty level 5 |
| Bestseller | Complete a run on difficulty level 6 |
| International Release | Complete a run on difficulty level 7 |
| Collected Works | Complete a run on difficulty level 8 |
| Definitive Edition | Complete a run on difficulty level 9 |

Achievement names are placeholder — adjust as appropriate to fit the final tone and lore.
