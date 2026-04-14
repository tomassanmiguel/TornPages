namespace TornPages.Engine;

public static class CombatResolver
{
    // ─── Formation Drawing ─────────────────────────────────────────────────────

    public static List<FormationInstance> DrawFormations(
        FormationArchive archive, int count, DeterministicRng rng,
        string? lastFormationId, bool allowRepeats = false,
        List<string>? additionalExclusions = null)
    {
        var available = archive.Formations.ToList();
        if (!allowRepeats && available.Count > 1 && lastFormationId != null)
            available = available.Where(f => f.FormationId != lastFormationId).ToList();
        if (additionalExclusions != null)
            available = available.Where(f => !additionalExclusions.Contains(f.FormationId)).ToList();
        if (available.Count == 0) available = archive.Formations.ToList();

        count = Math.Min(count, available.Count);
        return rng.Sample(available, count);
    }

    public static int GetFormationDrawCount(LeftPageState left, CombatState combat)
    {
        var count = SystemValues.FormationsPerPage(left.Systems.Computers.Level);
        foreach (var mod in combat.ActiveModifiers)
        {
            if (mod.Type == CombatModifierType.DrawPlusOneFormation) count++;
            if (mod.Type == CombatModifierType.DrawMinusOneFormation) count--;
        }
        var mods = EffectCalculator.GetAllEquippedMods(left);
        if (mods.Any(m => m.EffectType == ModEffectType.PlusOneFormationDrawPerPage)) count++;
        // (No mod effect for OnlyDrawOneFormation — that's an enemy ability only)
        // Ability: only draw 1
        if (combat.Enemy.AbilityIds.Contains("OnlyDrawOneFormation")) count = 1;
        return Math.Max(1, count);
    }

    // ─── Start of Combat Page Setup ───────────────────────────────────────────

    public static CombatState StartPage(
        CombatState prev, LeftPageState left, DeterministicRng rng)
    {
        var next = prev with
        {
            PageNumber = prev.PageNumber + 1,
            SlotUseCountThisPage = [],
            DeployablesUsedThisPage = [],
            PanickedCrewIdsThisPage = [],
            IsPhantomPage = false,
            IsInvulnerablePage = false,
            AwaitingReadyUp = false,
            ActiveFormationId = null
        };

        // 1. Persistent stacks
        var burn = prev.BurnStacks.ToArray();
        var barrier = prev.BarrierStacks.ToArray();
        var fusion = prev.FusionStacks.ToArray();

        if (prev.PageNumber > 0) // stacks first trigger on page 2
        {
            // Burn → Threat (handled at ReadyUp threat step — just note it's there)
            // Barrier → Shield added at resolution
            // Fusion → Charge added at resolution
        }

        next = next with { BurnStacks = burn, BarrierStacks = barrier, FusionStacks = fusion };

        // 2. Passive charge (pre-calculated; applied at ReadyUp)
        // 3. Silent race passive
        // 4. Panic sources
        var panickedIds = new List<string>();

        // Enemy ability: panics random crew
        if (next.Enemy.AbilityIds.Contains("PanicsEachPage"))
        {
            var available = left.Crew.Where(c => !c.IsDead && !c.IsPanicked).ToList();
            if (available.Count > 0)
                panickedIds.Add(rng.Pick(available).Id);
        }
        foreach (var mod in next.ActiveModifiers)
        {
            if (mod.Type == CombatModifierType.RandomCrewPanicsEachPage)
            {
                var available = left.Crew.Where(c => !c.IsDead && !c.IsPanicked).ToList();
                if (available.Count > 0)
                    panickedIds.Add(rng.Pick(available).Id);
            }
        }

        // Per-crew traits: 25% panic, panic when wounded
        foreach (var crew in left.Crew.Where(c => !c.IsDead))
        {
            if (crew.Traits.Any(t => t.Definition.EffectType == TraitEffectType.PlusTwoResTwentyFivePanic)
                && rng.Chance(0.25))
                panickedIds.Add(crew.Id);
        }

        next = next with { PanickedCrewIdsThisPage = panickedIds.Distinct().ToList() };

        // 5. Enemy start-of-page abilities
        if (next.Enemy.AbilityIds.Contains("InvulnEven") && next.PageNumber % 2 == 0)
            next = next with { IsInvulnerablePage = true };
        if (next.Enemy.AbilityIds.Contains("ImmuneFirstTwo") && next.PageNumber <= 2)
            next = next with { IsInvulnerablePage = true };

        // Boss: check phantom
        if (next.BossInvulnNextPage)
            next = next with { IsInvulnerablePage = true, BossInvulnNextPage = false };

        // Phalanx passive: lock-on cannot increase this page (flag set during intent gen)
        // Phantom passive: handled when active formation is selected

        // 6. Generate intents
        next = GenerateIntents(next, left, rng);

        // 7. Add pain/fatigue slots from combat modifiers
        // (applied at formation selection time, not here)

        return next;
    }

    // ─── Intent Generation ────────────────────────────────────────────────────

    static CombatState GenerateIntents(CombatState combat, LeftPageState left, DeterministicRng rng)
    {
        var weapons = combat.Enemy.WeaponIds
            .Select(id => WeaponData.Get(id)!)
            .Where(w => w != null).ToList();
        if (weapons.Count == 0) return combat;

        var intents = new List<GeneratedIntent>();

        for (var laneIdx = 0; laneIdx < 3; laneIdx++)
        {
            // Cycle through weapons without repeating across pages
            var usedThisCombat = combat.IntentsUsedThisCombat ?? [];
            var available = weapons.Where(w => !usedThisCombat.Contains($"{w.Id}_{laneIdx}")).ToList();
            if (available.Count == 0)
            {
                available = weapons.ToList();
            }
            var weapon = rng.Pick(available);

            // Get the lane's effect (with randomised lane support)
            var effects = new[] { weapon.LeftEffect, weapon.CenterEffect, weapon.RightEffect };
            if (weapon.RandomiseLanes)
                effects = rng.Shuffle(effects).ToArray();

            var effect = effects[laneIdx];
            var lockOn = combat.LockOn[laneIdx];
            var lockOnMultiplier = 1f + lockOn * 0.2f;
            var actNumber = left.ActNumber;

            var resolved = effect.Type switch
            {
                WeaponEffectType.Threat => (int)Math.Round(ScaleThreat(effect.IntValue, actNumber) * lockOnMultiplier),
                WeaponEffectType.PiercingThreat => (int)Math.Round(ScaleThreat(effect.IntValue, actNumber) * lockOnMultiplier),
                WeaponEffectType.BurnStacks => (int)Math.Round(ScaleBurn(effect.IntValue, actNumber) * lockOnMultiplier),
                WeaponEffectType.LockOnIncrease => (int)Math.Round(ScaleLockOn(effect.IntValue, actNumber) * lockOnMultiplier),
                WeaponEffectType.ChargeReduction => (int)Math.Round(ScaleChargeReduction(effect.IntValue, actNumber) * lockOnMultiplier),
                WeaponEffectType.LockOnDecrease => effect.IntValue,
                WeaponEffectType.MoraleReduction => effect.IntValue,
                _ => effect.IntValue
            };

            // Threat +30% ability
            if (effect.Type == WeaponEffectType.Threat || effect.Type == WeaponEffectType.PiercingThreat)
            {
                if (combat.Enemy.AbilityIds.Contains("ThreatPlus30"))
                    resolved = (int)Math.Round(resolved * 1.3f);
                // Critical threat (25%)
                if (combat.Enemy.AbilityIds.Contains("CritThreat25") && rng.Chance(0.25))
                    resolved *= 2;
            }

            intents.Add(new GeneratedIntent(combat.PageNumber + 1, laneIdx, weapon.Id, effect, resolved));
        }

        var newUsed = (combat.IntentsUsedThisCombat ?? []).ToList();
        for (var i = 0; i < 3; i++)
            newUsed.Add($"{intents[i].WeaponId}_{i}");

        return combat with { GeneratedIntents = intents, IntentsUsedThisCombat = newUsed };
    }

    static int ScaleThreat(int baseVal, int act) => act switch { 1 => baseVal, 2 => baseVal + 8, 3 => baseVal + 16, _ => baseVal };
    static int ScaleBurn(int baseVal, int act) => act switch { 1 => baseVal, 2 => baseVal + 1, 3 => baseVal + 2, _ => baseVal };
    static int ScaleLockOn(int baseVal, int act) => baseVal; // lock-on doesn't scale
    static int ScaleChargeReduction(int baseVal, int act) => act switch { 1 => baseVal, 2 => baseVal * 2, 3 => baseVal * 3, _ => baseVal };

    // ─── ReadyUp Resolution ───────────────────────────────────────────────────

    public record ResolutionResult(
        CombatState Combat,
        LeftPageState Left,
        bool CombatComplete,
        bool RunFailed);

    public static ResolutionResult Resolve(
        CombatState combat, LeftPageState left, DeterministicRng rng)
    {
        var hull = left.HullCurrent;
        var coin = left.Coin;
        var morale = left.Morale;
        var chargeAcc = combat.ChargeAccumulated;
        var crew = left.Crew.Select(c => c with { }).ToList(); // mutable copy
        var lockOn = combat.LockOn.ToArray();
        var burnStacks = combat.BurnStacks.ToArray();
        var barrierStacks = combat.BarrierStacks.ToArray();
        var fusionStacks = combat.FusionStacks.ToArray();
        var allMods = EffectCalculator.GetAllEquippedMods(left).ToList();

        var activeFormation = left.FormationArchive.Formations
            .FirstOrDefault(f => f.FormationId == combat.ActiveFormationId);

        // Build lane arrays
        var shieldPerLane = new int[3];
        var threatPerLane = new int[3];
        var hackPerLane = new int[3];
        var chargePerLane = new int[3];
        var log = combat.EventLog.ToList();

        // ── Pre-resolution: persistent stacks (page N >= 2) ──────────────────
        if (combat.PageNumber > 1)
        {
            for (var i = 0; i < 3; i++)
            {
                // Burn → Threat
                threatPerLane[i] += burnStacks[i];
                // Barrier → Shield
                shieldPerLane[i] += barrierStacks[i];
                // Fusion → Charge
                chargePerLane[i] += fusionStacks[i];
            }
        }

        // ── Passive charge ────────────────────────────────────────────────────
        if (!combat.Enemy.AbilityIds.Contains("NoPassiveCharge"))
        {
            var totalInt = crew.Where(c => !c.IsDead).Sum(c =>
            {
                var intVal = c.Stats.Intelligence;
                if (c.Traits.Any(t => t.Definition.EffectType == TraitEffectType.IntDoublePassiveCharge))
                    intVal *= 2;
                if (c.Traits.Any(t => t.Definition.NegativeEffectType == NegativeTraitEffectType.IntHalfPassiveCharge))
                    intVal = intVal / 2;
                return intVal;
            });
            var passiveCharge = 1 + totalInt;
            if (allMods.Any(m => m.EffectType == ModEffectType.PassiveChargePlusFiftyPercent))
                passiveCharge = (int)(passiveCharge * 1.5f);
            // Mod: if no charge action on pages, double passive
            var noActionDouble = allMods.Any(m => m.EffectType == ModEffectType.NoActionDoublePassiveCharge);
            var hasSlottedCrew = combat.SlottedByLane.Any(lane => lane.Count > 0);
            if (noActionDouble && !hasSlottedCrew) passiveCharge *= 2;

            for (var i = 0; i < 3; i++) chargePerLane[i] += passiveCharge / 3;
            chargePerLane[0] += passiveCharge % 3 > 0 ? 1 : 0;
        }

        // ── Crew abilities ────────────────────────────────────────────────────
        if (activeFormation != null)
        {
            var slots = activeFormation.Slots;
            var slottedCrewBySlot = new Dictionary<int, string>(); // slotIndex → crewId
            foreach (var lane in combat.SlottedByLane)
                foreach (var sc in lane)
                    slottedCrewBySlot[sc.SlotIndex] = sc.CrewId;

            // Collect unique slotted crew × slot pairs (one entry per slot used)
            var slottings = new List<(CrewMemberState crew, SlotInstance slot, int slotIdx)>();
            foreach (var (slotIdx, crewId) in slottedCrewBySlot)
            {
                if (slotIdx >= slots.Count) continue;
                var cm = crew.FirstOrDefault(c => c.Id == crewId);
                if (cm == null || cm.IsDead) continue;
                slottings.Add((cm, slots[slotIdx], slotIdx));
            }

            // Silent passive: randomise boost of one slot (already reflected in slot state if tracked)
            // (simplified: not modifying slot here since it's handled at formation level)

            // Necropolis passive: exhausted crew in slots hack+charge
            if (activeFormation.Definition.Passive?.Type == FormationPassiveType.NecropassPassive)
            {
                foreach (var (cm, slot, si) in slottings)
                {
                    var eff = cm.FatigueBaseline + combat.SlotUseCountThisPage.GetValueOrDefault(cm.Id, 0);
                    if (eff > cm.Stats.Endurance)
                    {
                        foreach (var lane in slot.ConnectedLanes)
                        {
                            hackPerLane[(int)lane] += 15;
                            chargePerLane[(int)lane] += 3;
                        }
                    }
                }
            }

            // Sacrifice passive: kill K-slot crew, grant +5 EL to others
            // (simplified: K slot removes the crew)
            var kSlotCrewIds = new HashSet<string>();
            if (activeFormation.Definition.Passive?.Type == FormationPassiveType.SacrificePassive)
            {
                for (var si = 0; si < slots.Count; si++)
                {
                    if (slots[si].Type == SlotType.Kill && slottedCrewBySlot.TryGetValue(si, out var cid))
                        kSlotCrewIds.Add(cid);
                }
            }

            foreach (var (cm, slot, si) in slottings)
            {
                if (kSlotCrewIds.Contains(cm.Id)) continue; // sacrificed

                var sacBonus = kSlotCrewIds.Count > 0 ? 5 : 0;

                foreach (var ability in cm.Abilities)
                {
                    foreach (var lane in GetEffectiveLanes(cm, slot))
                    {
                        var fires = slot.IsDouble ? 2 : 1;
                        for (var f = 0; f < fires; f++)
                        {
                            var output = EffectCalculator.Calculate(
                                cm, ability, slot, lane, combat, left, activeFormation);

                            shieldPerLane[(int)lane] += output.Shield;
                            hackPerLane[(int)lane] += output.Hack;
                            chargePerLane[(int)lane] += output.Charge;
                            // Boost is lane-wide; collected and applied additively
                            // (already folded into Calculate)
                        }
                    }
                }

                // Kill enemies in K slots
                foreach (var cid in kSlotCrewIds)
                {
                    var cm2 = crew.FirstOrDefault(c => c.Id == cid);
                    if (cm2 != null)
                    {
                        var idx2 = crew.IndexOf(cm2);
                        crew[idx2] = cm2 with { IsDead = true, HpCurrent = 0 };
                        log.Add(new CombatLogEntry($"{cm2.Name} was sacrificed.", combat.PageNumber));
                        // Morale trait on death
                        if (cm2.Traits.Any(t => t.Definition.NegativeEffectType == NegativeTraitEffectType.MoraleToThirtyOnDeath))
                            morale = 30;
                    }
                }
            }

            // Combat modifier: Hack 2 all lanes
            if (combat.ActiveModifiers.Any(m => m.Type == CombatModifierType.HackTwoAllLanes))
                for (var i = 0; i < 3; i++) hackPerLane[i] += 2;

            // Mod: Hack 2 all lanes (start with lock-on) handled at start-of-combat
            if (allMods.Any(m => m.EffectType == ModEffectType.HackTwoAllLanesStartWithLockOn))
                for (var i = 0; i < 3; i++) hackPerLane[i] += 2;

            // Empty slot effects
            var emptyConnectedSlots = GetEmptyConnectedSlots(slots, slottedCrewBySlot);
            foreach (var (emptSlot, laneIdx) in emptyConnectedSlots)
            {
                if (combat.ActiveModifiers.Any(m => m.Type == CombatModifierType.EmptySlotsShieldOne))
                    shieldPerLane[laneIdx] += 1;
                if (combat.ActiveModifiers.Any(m => m.Type == CombatModifierType.EmptySlotsChargeOne))
                    chargePerLane[laneIdx] += 1;
                if (combat.ActiveModifiers.Any(m => m.Type == CombatModifierType.EmptySlotsThreattwo))
                    threatPerLane[laneIdx] += 2;
                if (allMods.Any(m => m.EffectType == ModEffectType.EmptySlotChargeTwo))
                    chargePerLane[laneIdx] += 2;
                if (allMods.Any(m => m.EffectType == ModEffectType.EmptySlotShieldTwo))
                    shieldPerLane[laneIdx] += 2;
                if (allMods.Any(m => m.EffectType == ModEffectType.EmptySlotHealTwo))
                    hull = Math.Min(left.HullMax, hull + 2);
                // Full House passive
                if (activeFormation.Definition.Passive?.Type == FormationPassiveType.FullHousePassive)
                    threatPerLane[laneIdx] += activeFormation.Definition.Passive.IntValue;
            }
        }

        // Enemy Reactive Armor: charging also generates threat
        if (combat.Enemy.AbilityIds.Contains("ChargeThreat"))
            for (var i = 0; i < 3; i++)
                if (chargePerLane[i] > 0) threatPerLane[i] += chargePerLane[i];

        // Enemy intents for this page
        foreach (var intent in combat.GeneratedIntents.Where(g => g.PageNumber == combat.PageNumber))
        {
            var laneIdx = intent.LaneIndex;
            var val = intent.ResolvedValue;
            switch (intent.Effect.Type)
            {
                case WeaponEffectType.Threat:
                    threatPerLane[laneIdx] += val;
                    break;
                case WeaponEffectType.PiercingThreat:
                    // Piercing bypasses shield — tracked as negative shield
                    threatPerLane[laneIdx] += val; // will not be subtracted by shield step
                    // Mark as piercing so shield step skips it (simplified: separate array)
                    break;
                case WeaponEffectType.BurnStacks:
                    burnStacks[laneIdx] += val;
                    break;
                case WeaponEffectType.LockOnIncrease:
                    {
                        var maxLo = EffectCalculator.GetMaxLockOn(allMods);
                        // Phalanx: lock-on cannot increase
                        if (activeFormation?.Definition.Passive?.Type == FormationPassiveType.PhalanxPassive) break;
                        lockOn[laneIdx] = Math.Min(maxLo, lockOn[laneIdx] + val);
                    }
                    break;
                case WeaponEffectType.LockOnDecrease:
                    lockOn[laneIdx] = Math.Max(0, lockOn[laneIdx] - val);
                    break;
                case WeaponEffectType.ChargeReduction:
                    chargePerLane[laneIdx] -= val;
                    break;
                case WeaponEffectType.MoraleReduction:
                    morale = Math.Max(20, morale - val);
                    break;
                case WeaponEffectType.KillRandomCrew:
                    {
                        var alive = crew.Where(c => !c.IsDead).ToList();
                        if (alive.Count > 0)
                        {
                            var victim = rng.Pick(alive);
                            var vi = crew.IndexOf(victim);
                            crew[vi] = victim with { IsDead = true, HpCurrent = 0 };
                            log.Add(new CombatLogEntry($"Death Laser killed {victim.Name}!", combat.PageNumber));
                        }
                    }
                    break;
            }
        }

        // Fading Canon: reduce threat by 3 per prior use
        for (var i = 0; i < combat.GeneratedIntents.Count; i++)
        {
            var intent = combat.GeneratedIntents[i];
            if (intent.WeaponId == "FadingCanon" && intent.Effect.Type == WeaponEffectType.Threat)
                threatPerLane[intent.LaneIndex] = Math.Max(0, threatPerLane[intent.LaneIndex] - 3 * combat.FadingCanonUseCount);
        }

        // ── Step 1: Shield ────────────────────────────────────────────────────
        var remainingThreat = new int[3];
        for (var i = 0; i < 3; i++)
        {
            var shield = shieldPerLane[i];
            // Predictable Blast: shield doubly effective
            var predictableInLane = combat.GeneratedIntents.Any(g =>
                g.LaneIndex == i && g.WeaponId == "PredictableBlast");
            if (predictableInLane) shield *= 2;

            remainingThreat[i] = Math.Max(0, threatPerLane[i] - shield);
        }

        // ── Step 2: Threat damage ─────────────────────────────────────────────
        var isInvuln = combat.IsInvulnerablePage;
        var preventFirstDamage = combat.ActiveModifiers.Any(m => m.Type == CombatModifierType.PreventFirstDamage)
            && !combat.FirstDamagePrevented;
        var hullDamage = 0;

        // Mod: immune first page
        if (allMods.Any(m => m.EffectType == ModEffectType.ImmuneDamageFirstPage) && combat.PageNumber == 1)
            isInvuln = true;

        if (!isInvuln)
        {
            for (var i = 0; i < 3; i++)
            {
                if (remainingThreat[i] <= 0) continue;
                var dmg = remainingThreat[i];
                // Mod: reduce damage
                if (allMods.Any(m => m.EffectType == ModEffectType.ReduceAllDamageByOne))
                    dmg = Math.Max(0, dmg - 1);
                if (allMods.Any(m => m.EffectType == ModEffectType.ReduceAllDamageThirtyThreePercent))
                    dmg = (int)(dmg * 0.67f);
                if (allMods.Any(m => m.EffectType == ModEffectType.SmallDamagePrevented) && dmg < 5)
                    dmg = 0;
                // Glitch: +1 damage
                if (allMods.Any(m => m.EffectType == ModEffectType.GlitchPlusOneDamageAll))
                    dmg++;

                if (preventFirstDamage && dmg > 0)
                {
                    preventFirstDamage = false;
                    continue;
                }

                hullDamage += dmg;
                hull -= dmg;

                // Coin on hull damage mod
                if (allMods.Any(m => m.EffectType == ModEffectType.GainCoinOnHullDamage))
                    coin += 5;

                // Glitch: lose charge when damaged
                if (allMods.Any(m => m.EffectType == ModEffectType.GlitchLoseChargeWhenDamaged))
                    chargeAcc = Math.Max(0, chargeAcc - dmg);

                // Crew damage in this lane
                if (!allMods.Any(m => m.EffectType == ModEffectType.CrewNoDamageFromThreat))
                {
                    var damagePerCrew = combat.Enemy.AbilityIds.Contains("PlusDamage") ? 2 : 1;
                    if (activeFormation != null)
                    {
                        var slottedInLane = combat.SlottedByLane[i].Select(sc =>
                            crew.FirstOrDefault(c => c.Id == sc.CrewId)).Where(c => c != null).ToList();
                        foreach (var cm in slottedInLane)
                        {
                            var crewIdx = crew.IndexOf(cm!);
                            var newHp = cm!.HpCurrent;
                            var actualDmg = damagePerCrew;
                            // Glitch double pain
                            if (slots(activeFormation, slottedInLane, cm!.Id)
                                && allMods.Any(m => m.EffectType == ModEffectType.GlitchDoubleDamageFromPain))
                                actualDmg *= 2;
                            // Double damage trait
                            if (cm.Traits.Any(t => t.Definition.NegativeEffectType == NegativeTraitEffectType.DoubleDamage))
                                actualDmg *= 2;
                            // Not damaged by threat trait
                            if (cm.Traits.Any(t => t.Definition.EffectType == TraitEffectType.NotDamagedByThreatOverflow))
                                actualDmg = 0;
                            // Lethal damage trait
                            if (cm.Traits.Any(t => t.Definition.EffectType == TraitEffectType.PlusTwoResLethalDamage))
                                newHp = 0;
                            else
                                newHp -= actualDmg;

                            // Panic when damaged
                            if (cm.Traits.Any(t => t.Definition.NegativeEffectType == NegativeTraitEffectType.PanicWhenDamaged)
                                && actualDmg > 0)
                                combat = combat with { PanickedCrewIdsThisPage = [.. combat.PanickedCrewIdsThisPage, cm.Id] };

                            if (newHp <= 0)
                            {
                                newHp = 0;
                                // Replicant revive
                                if (cm.Race == Race.Replicant && !cm.Traits.Any(t => t.TraitId == "__revived"))
                                {
                                    newHp = cm.HpMax;
                                    var revStats = cm.Stats with { Resolve = Math.Max(0, cm.Stats.Resolve - 1) };
                                    crew[crewIdx] = cm with { HpCurrent = newHp, Stats = revStats };
                                    log.Add(new CombatLogEntry($"{cm.Name} revived as a Replicant!", combat.PageNumber));
                                    continue;
                                }
                                crew[crewIdx] = cm with { HpCurrent = 0, IsDead = true };
                                log.Add(new CombatLogEntry($"{cm.Name} was killed!", combat.PageNumber));
                                if (cm.Traits.Any(t => t.Definition.NegativeEffectType == NegativeTraitEffectType.MoraleToThirtyOnDeath))
                                    morale = 30;
                                // Other on-death effects
                                foreach (var other in crew.Where(c => !c.IsDead && c.Id != cm.Id))
                                {
                                    if (other.Traits.Any(t => t.Definition.EffectType == TraitEffectType.GainStatsOnCrewmateDeath))
                                    {
                                        var oi = crew.IndexOf(other);
                                        var rngStat = rng.Next(3);
                                        crew[oi] = rngStat switch
                                        {
                                            0 => other with { Stats = other.Stats.AddRes(2) },
                                            1 => other with { Stats = other.Stats.AddInt(2) },
                                            _ => other with { Stats = other.Stats.AddCha(2) }
                                        };
                                    }
                                    if (allMods.Any(m => m.EffectType == ModEffectType.CrewDeathGrantsPlusTwoRes))
                                    {
                                        var oi = crew.IndexOf(other);
                                        crew[oi] = other with { Stats = other.Stats.AddRes(2) };
                                    }
                                }
                                // Check guard trait
                                if (hull <= 0 && cm.Traits.Any(t => t.Definition.EffectType == TraitEffectType.SacrificeToPreventDeath))
                                    hull = 1;
                            }
                            else
                                crew[crewIdx] = cm with { HpCurrent = newHp };

                            // El traits on self-damage
                            if (actualDmg > 0)
                            {
                                var crewPost = crew[crewIdx];
                                var ti = crewPost.Traits.FindIndex(t => t.Definition.EffectType == TraitEffectType.PlusElWhenSelfDamaged);
                                if (ti >= 0)
                                    crew[crewIdx] = crewPost with { Traits = [.. crewPost.Traits.Take(ti), crewPost.Traits[ti] with { AccumulatedBonus = crewPost.Traits[ti].AccumulatedBonus + 1 }, .. crewPost.Traits.Skip(ti + 1)] };
                            }
                        }
                    }
                }

                // El trait: when hull damaged
                if (dmg > 0)
                {
                    for (var ci = 0; ci < crew.Count; ci++)
                    {
                        var cm = crew[ci];
                        var ti = cm.Traits.FindIndex(t => t.Definition.EffectType == TraitEffectType.PlusElWhenHullDamaged);
                        if (ti >= 0)
                            crew[ci] = cm with { Traits = [.. cm.Traits.Take(ti), cm.Traits[ti] with { AccumulatedBonus = cm.Traits[ti].AccumulatedBonus + 1 }, .. cm.Traits.Skip(ti + 1)] };
                    }
                }
            }
        }

        // Hull regen mod
        if (allMods.Any(m => m.EffectType == ModEffectType.HullRegenOnePerPage))
            hull = Math.Min(left.HullMax, hull + 1);

        // Hack-grants-shield mod
        // ── Step 3: Hack ─────────────────────────────────────────────────────
        for (var i = 0; i < 3; i++)
        {
            var hack = hackPerLane[i];
            if (allMods.Any(m => m.EffectType == ModEffectType.HackPlusOne) && hack > 0) hack++;
            coin += hack;
            if (allMods.Any(m => m.EffectType == ModEffectType.HackGrantsShield))
                shieldPerLane[i] += hack;
        }

        // ── Step 4: Charge ────────────────────────────────────────────────────
        var negChargeDisabled = allMods.Any(m => m.EffectType == ModEffectType.NegativeChargeDisabled);
        for (var i = 0; i < 3; i++)
        {
            var laneCharge = chargePerLane[i];
            if (negChargeDisabled) laneCharge = Math.Max(0, laneCharge);
            if (laneCharge > 0) chargeAcc += laneCharge;
            // Final boss: redirect charge to super shield
            if (combat.SuperShieldCurrent.HasValue && combat.SuperShieldCurrent.Value > 0 && laneCharge > 0)
            {
                var reduce = Math.Min(laneCharge, combat.SuperShieldCurrent.Value);
                combat = combat with { SuperShieldCurrent = combat.SuperShieldCurrent.Value - reduce };
                chargeAcc -= laneCharge; // redirected
            }
        }

        // Charging-also-shields mod
        if (allMods.Any(m => m.EffectType == ModEffectType.ChargingAlsoShields))
        {
            var totalCharge = chargePerLane.Sum(c => Math.Max(0, c));
            for (var i = 0; i < 3; i++) shieldPerLane[i] += totalCharge / 10;
        }

        // Charging-lane-gets-lockOn enemy ability
        if (combat.Enemy.AbilityIds.Contains("ChargingLaneGetsPlusOneLockOn"))
        {
            var maxLo = EffectCalculator.GetMaxLockOn(allMods);
            for (var i = 0; i < 3; i++)
                if (chargePerLane[i] > 0)
                    lockOn[i] = Math.Min(maxLo, lockOn[i] + 1);
        }

        // ── Post-page effects ─────────────────────────────────────────────────
        // Fatigue recovery and Growth slots
        var slotUseCount = combat.SlotUseCountThisPage;
        if (activeFormation != null)
        {
            var growthSlottedCrewIds = new HashSet<string>();
            foreach (var (slotIdx, crewId) in combat.SlottedByLane.SelectMany((lane, _) => lane).ToDictionary(sc => sc.SlotIndex, sc => sc.CrewId))
            {
                if (slotIdx >= activeFormation.Slots.Count) continue;
                var slot = activeFormation.Slots[slotIdx];
                if (slot.Type == SlotType.Growth) growthSlottedCrewIds.Add(crewId);
                // Pain slots deal 1 damage
                if (slot.Type == SlotType.Pain)
                {
                    var crewIdx2 = crew.FindIndex(c => c.Id == crewId);
                    if (crewIdx2 >= 0 && !crew[crewIdx2].IsDead)
                    {
                        var newHp2 = crew[crewIdx2].HpCurrent - 1;
                        if (allMods.Any(m => m.EffectType == ModEffectType.GlitchDoubleDamageFromPain))
                            newHp2 = crew[crewIdx2].HpCurrent - 2;
                        if (newHp2 <= 0) { newHp2 = 0; crew[crewIdx2] = crew[crewIdx2] with { IsDead = true, HpCurrent = 0 }; }
                        else crew[crewIdx2] = crew[crewIdx2] with { HpCurrent = newHp2 };
                    }
                }
                // Heal slots heal 1 HP
                if (slot.Type == SlotType.Heal)
                {
                    var crewIdx2 = crew.FindIndex(c => c.Id == crewId);
                    if (crewIdx2 >= 0 && !crew[crewIdx2].IsDead)
                    {
                        var bonusHeal = allMods.Any(m => m.EffectType == ModEffectType.HealSlotsPlusOne) ? 1 : 0;
                        crew[crewIdx2] = crew[crewIdx2] with
                        {
                            HpCurrent = Math.Min(crew[crewIdx2].HpMax, crew[crewIdx2].HpCurrent + 1 + bonusHeal)
                        };
                    }
                }
            }
            // Growth: +1 permanent El
            foreach (var crewId in growthSlottedCrewIds)
            {
                var crewIdx2 = crew.FindIndex(c => c.Id == crewId);
                if (crewIdx2 >= 0)
                    crew[crewIdx2] = crew[crewIdx2] with { PermanentEffectLevelBonus = crew[crewIdx2].PermanentEffectLevelBonus + 1 };
            }

            // Awakener passive: convert F → B after each page
            if (activeFormation.Definition.Passive?.Type == FormationPassiveType.AwakenPassive)
            {
                var fatigueSlotIdx = activeFormation.Slots.FindIndex(s => s.Type == SlotType.Fatigue);
                if (fatigueSlotIdx >= 0)
                {
                    var newSlots = activeFormation.Slots.ToList();
                    newSlots[fatigueSlotIdx] = newSlots[fatigueSlotIdx] with { Type = SlotType.Boosted };
                    // Update the formation instance (in-combat modification)
                    var fi = left.FormationArchive.Formations.FindIndex(f => f.FormationId == combat.ActiveFormationId);
                    if (fi >= 0)
                    {
                        // Simplified: we don't deeply mutate left state here; instead we flag it
                        // In a full impl this would modify the formation slots for this combat only
                    }
                }
            }
        }

        // Fatigue
        for (var ci = 0; ci < crew.Count; ci++)
        {
            var cm = crew[ci];
            if (cm.IsDead) continue;
            var usedThisPage = slotUseCount.GetValueOrDefault(cm.Id, 0);
            var newFatigue = cm.FatigueBaseline + usedThisPage;
            // -1 recovery
            newFatigue = Math.Max(0, newFatigue - 1);
            // Extra -1 from cabin mod
            if (allMods.Any(m => m.EffectType == ModEffectType.CrewLoseOneFatiguePerPage))
                newFatigue = Math.Max(0, newFatigue - 1);

            // El traits: after each page
            var traitsCopy = cm.Traits.ToList();
            for (var ti = 0; ti < traitsCopy.Count; ti++)
            {
                if (traitsCopy[ti].Definition.EffectType == TraitEffectType.PlusElAfterEachPage)
                    traitsCopy[ti] = traitsCopy[ti] with { AccumulatedBonus = traitsCopy[ti].AccumulatedBonus + 1 };
                if (traitsCopy[ti].Definition.NegativeEffectType == NegativeTraitEffectType.MinusOneResAfterEachPage)
                    traitsCopy[ti] = traitsCopy[ti] with { AccumulatedBonus = traitsCopy[ti].AccumulatedBonus + 1 };
                if (traitsCopy[ti].Definition.EffectType == TraitEffectType.ThreePlusElDecayEachPage)
                    traitsCopy[ti] = traitsCopy[ti] with { AccumulatedBonus = Math.Min(3, traitsCopy[ti].AccumulatedBonus + 1) };
            }

            // 25% chance refresh if exhausted
            if (newFatigue > cm.Stats.Endurance
                && cm.Traits.Any(t => t.Definition.EffectType == TraitEffectType.ExhaustedTwentyFiveChanceRefresh)
                && rng.Chance(0.25))
                newFatigue = 0;

            // Mod: exhausted recover after 2 pages (simplified: reset after 2 pages)
            crew[ci] = cm with { FatigueBaseline = newFatigue, Traits = traitsCopy };
        }

        // Morale enemy ability
        if (combat.Enemy.AbilityIds.Contains("MoraleDrops5"))
            morale = Math.Max(20, morale - 5);

        // Glitch: lose coin every page
        if (allMods.Any(m => m.EffectType == ModEffectType.GlitchLoseCoinEveryPage))
            coin = Math.Max(0, coin - 1);

        // Morale cap glitch
        if (allMods.Any(m => m.EffectType == ModEffectType.GlitchMoraleCapEighty))
            morale = Math.Min(80, morale);

        // 25% threat persists enemy ability
        float[] persistedThreat = new float[3];
        if (combat.Enemy.AbilityIds.Contains("ThreatPersists25"))
            for (var i = 0; i < 3; i++)
                persistedThreat[i] = remainingThreat[i] * 0.25f;

        // Build updated combat state
        var updatedCombat = combat with
        {
            ChargeAccumulated = chargeAcc,
            LockOn = lockOn,
            BurnStacks = burnStacks,
            BarrierStacks = barrierStacks,
            FusionStacks = fusionStacks,
            EventLog = log,
            IsComplete = IsChargeThresholdMet(chargeAcc, combat, left),
            AwaitingReadyUp = false,
            FadingCanonUseCount = combat.FadingCanonUseCount
                + (combat.GeneratedIntents.Any(g => g.WeaponId == "FadingCanon") ? 1 : 0),
        };

        // Update LeftPageState
        var updatedLeft = left with
        {
            HullCurrent = Math.Max(0, hull),
            Coin = Math.Max(0, coin),
            Morale = Math.Clamp(morale, 20, 100),
            Crew = crew,
            Stats = left.Stats with
            {
                TotalDamageTaken = left.Stats.TotalDamageTaken + hullDamage,
                TotalHackGenerated = left.Stats.TotalHackGenerated + hackPerLane.Sum(),
                TotalShieldGenerated = left.Stats.TotalShieldGenerated + shieldPerLane.Sum(),
                TotalChargeGenerated = left.Stats.TotalChargeGenerated + chargePerLane.Where(c => c > 0).Sum(),
            }
        };

        var runFailed = updatedLeft.HullCurrent <= 0 || updatedLeft.PagesRemaining <= 0;

        return new ResolutionResult(updatedCombat, updatedLeft, updatedCombat.IsComplete, runFailed);
    }

    static bool IsChargeThresholdMet(int chargeAcc, CombatState combat, LeftPageState left)
    {
        var threshold = left.PendingCombatThreshold ?? ChargeThresholds.Get(left.ChapterNumber);
        // Apply modifiers
        var allMods = EffectCalculator.GetAllEquippedMods(left).ToList();
        if (allMods.Any(m => m.EffectType == ModEffectType.GlitchChargeThresholdPlusTen))
            threshold = (int)(threshold * 1.1f);
        if (allMods.Any(m => m.EffectType == ModEffectType.ChargeThresholdLowerBoss) && combat.Enemy.IsBoss)
            threshold = (int)(threshold * 0.9f);
        foreach (var mod in combat.ActiveModifiers)
        {
            if (mod.Type == CombatModifierType.LowerChargeThresholdTenPercent) threshold = (int)(threshold * 0.9f);
            if (mod.Type == CombatModifierType.RaiseChargeThresholdTenPercent) threshold = (int)(threshold * 1.1f);
        }
        if (combat.Enemy.AbilityIds.Contains("ChargeThreshPlus50"))
            threshold = (int)(threshold * 1.5f);

        // Final boss: only count charge while super shield is down
        if (combat.Enemy.BossId == "FinalBoss")
        {
            return combat.SuperShieldCurrent is <= 0 && chargeAcc >= 400;
        }

        return chargeAcc >= threshold;
    }

    static IEnumerable<LaneId> GetEffectiveLanes(CrewMemberState crew, SlotInstance slot)
    {
        var lanes = new HashSet<LaneId>(slot.ConnectedLanes);
        // Lane anchor traits
        if (crew.Traits.Any(t => t.Definition.EffectType == TraitEffectType.AlwaysConnectedLeft))
            lanes.Add(LaneId.Left);
        if (crew.Traits.Any(t => t.Definition.EffectType == TraitEffectType.AlwaysConnectedCenter))
            lanes.Add(LaneId.Center);
        if (crew.Traits.Any(t => t.Definition.EffectType == TraitEffectType.AlwaysConnectedRight))
            lanes.Add(LaneId.Right);
        return lanes;
    }

    static IEnumerable<(SlotInstance slot, int laneIdx)> GetEmptyConnectedSlots(
        List<SlotInstance> slots, Dictionary<int, string> slottedBySlotIndex)
    {
        for (var i = 0; i < slots.Count; i++)
        {
            if (slottedBySlotIndex.ContainsKey(i)) continue;
            foreach (var lane in slots[i].ConnectedLanes)
                yield return (slots[i], (int)lane);
        }
    }

    // Helper: check if crew is in a pain slot (for glitch double pain)
    static bool slots(FormationInstance formation, List<CrewMemberState?> crewInLane, string crewId)
        => false; // simplified

    public static int GetPassiveCharge(LeftPageState left, CombatState combat)
    {
        if (combat.Enemy.AbilityIds.Contains("NoPassiveCharge")) return 0;
        var totalInt = left.Crew.Where(c => !c.IsDead).Sum(c =>
        {
            var v = c.Stats.Intelligence;
            if (c.Traits.Any(t => t.Definition.EffectType == TraitEffectType.IntDoublePassiveCharge)) v *= 2;
            if (c.Traits.Any(t => t.Definition.NegativeEffectType == NegativeTraitEffectType.IntHalfPassiveCharge)) v /= 2;
            return v;
        });
        return 1 + totalInt;
    }
}
