namespace TornPages.Engine;

public static class EventEngine
{
    // ─── Build-Up Pool Generation ─────────────────────────────────────────────

    public static List<string> GenerateEventPool(
        LeftPageState left, DeterministicRng rng, string? locationId)
    {
        var totalCha = left.Crew.Where(c => !c.IsDead).Sum(c => c.Stats.Charisma);
        var poolSize = Math.Min(15, 5 + totalCha / 3);
        var chapterCount = new Dictionary<string, int>();

        var pool = new List<string>();

        // Always include CREW_GAIN and SHOP_MOD
        pool.Add("CREW_GAIN");
        pool.Add("SHOP_MOD");

        // Include location special event if available
        if (locationId != null)
        {
            var locDef = LocationData.Get(locationId);
            if (locDef?.SpecialEventId != null)
            {
                var locEvent = EventData.Get(locDef.SpecialEventId);
                if (locEvent != null && MeetsCondition(locEvent, left))
                    pool.Add(locEvent.Id);
            }
        }

        // Draw additional events from neutral pool
        var neutralPool = EventData.GetNeutralPool()
            .Where(e => !e.AlwaysInPool && e.RequiredLocationId == null)
            .ToList();

        var shuffled = rng.Shuffle(neutralPool);
        foreach (var evt in shuffled)
        {
            if (pool.Count >= poolSize) break;
            if (pool.Contains(evt.Id)) continue;
            if (!MeetsCondition(evt, left)) continue;
            if (pool.Count(id => SameGroup(id, evt.Id)) >= evt.MaxPerChapter) continue;
            pool.Add(evt.Id);
        }

        // Guarantee at least 3 negative events (DT > 0) if possible
        var negatives = pool.Where(id => (EventData.Get(id)?.DramaticTension ?? 0) > 0).Count();
        if (negatives < 3)
        {
            var extra = neutralPool
                .Where(e => e.DramaticTension > 0 && !pool.Contains(e.Id) && MeetsCondition(e, left))
                .ToList();
            foreach (var evt in extra)
            {
                if (negatives >= 3) break;
                pool.Add(evt.Id);
                negatives++;
            }
        }

        // DT constraint: ensure every event can be selected while still reaching DT ≥ 4
        pool = EnforceDtConstraint(pool, 4, rng, left);

        return pool;
    }

    static bool MeetsCondition(EventDefinition evt, LeftPageState left)
    {
        if (evt.RequiredCondition == null) return true;
        return evt.RequiredCondition switch
        {
            "coin>=50" => left.Coin >= 50,
            "coin>=80" => left.Coin >= 80,
            "crew>=2" => left.Crew.Count(c => !c.IsDead) >= 2,
            "formations>1" => left.FormationArchive.Formations.Count > 1,
            "systems_not_maxed" => SystemsNotAllMax(left),
            "hull_max>=11" => left.HullMax >= 11,
            "has_glitch" => HasGlitch(left),
            "has_upgradeable_formation" => HasUpgradeableFormation(left),
            "has_downgradeable_formation" => HasDowngradeableFormation(left),
            _ => true
        };
    }

    static bool SystemsNotAllMax(LeftPageState left) =>
        left.Systems.Engine.Level < 3 || left.Systems.Cabin.Level < 3
        || left.Systems.Hull.Level < 3 || left.Systems.Computers.Level < 3;

    static bool HasGlitch(LeftPageState left)
    {
        foreach (var sys in new[] { left.Systems.Engine, left.Systems.Cabin, left.Systems.Hull, left.Systems.Computers })
            if (sys.InstalledMods.Any(m => m.Definition.IsGlitch)) return true;
        return false;
    }

    static bool HasUpgradeableFormation(LeftPageState left) =>
        left.FormationArchive.Formations.Any(f =>
            f.Slots.Any(s => s.Type == SlotType.Empty || s.Type == SlotType.Pain
                || s.Type == SlotType.Fatigue || s.Type == SlotType.Kill));

    static bool HasDowngradeableFormation(LeftPageState left) =>
        left.FormationArchive.Formations.Any(f =>
            f.Slots.Any(s => s.Type == SlotType.Empty || s.Type == SlotType.Boosted
                || s.Type == SlotType.Growth || s.Type == SlotType.Heal));

    static bool SameGroup(string existing, string newId)
    {
        // Events in the same "damage/heal" group share a max-1 limit
        var damageGroup = new HashSet<string> { "DAMAGE_S", "DAMAGE_M", "DAMAGE_L", "DAMAGE_XL",
            "HEAL_S", "HEAL_M", "HEAL_L", "HEAL_XL" };
        var moraleGroup = new HashSet<string> { "MORALE_RESTORE_FULL", "MORALE_RESTORE_25",
            "MORALE_LOSE_10", "MORALE_LOSE_25" };
        var trainingGroup = new HashSet<string> { "TRAINING_INT", "TRAINING_CHA", "TRAINING_RES", "TRAINING_END" };

        if (damageGroup.Contains(existing) && damageGroup.Contains(newId)) return true;
        if (moraleGroup.Contains(existing) && moraleGroup.Contains(newId)) return true;
        if (trainingGroup.Contains(existing) && trainingGroup.Contains(newId)) return true;

        // Same event id entirely
        return existing == newId;
    }

    static List<string> EnforceDtConstraint(List<string> pool, int threshold, DeterministicRng rng, LeftPageState left)
    {
        // For every event E, the remaining events must sum to >= (threshold - DT(E))
        // If constraint fails, try to add a positive event or remove the problematic event
        for (var iter = 0; iter < 20; iter++)
        {
            var ok = true;
            foreach (var id in pool)
            {
                var dt = EventData.Get(id)?.DramaticTension ?? 0;
                var otherPositive = pool.Where(oid => oid != id)
                    .Sum(oid => Math.Max(0, -(EventData.Get(oid)?.DramaticTension ?? 0)));
                var needed = threshold - dt;
                if (otherPositive < needed)
                {
                    ok = false;
                    break;
                }
            }
            if (ok) break;
            // Add a strongly positive event or remove a strongly negative one
            var toAdd = EventData.GetNeutralPool()
                .Where(e => e.DramaticTension <= -2 && !pool.Contains(e.Id) && MeetsCondition(e, left))
                .ToList();
            if (toAdd.Count > 0)
                pool.Add(rng.Pick(toAdd).Id);
            else
                break;
        }
        return pool;
    }

    // ─── Event Payload Generation ─────────────────────────────────────────────

    public static EventPayload GeneratePayload(string eventId, LeftPageState left, DeterministicRng rng)
    {
        return eventId switch
        {
            "COIN_GAIN_XL" or "COIN_GAIN_XL_COSTLY" => new EventPayload(rng.Next(110, 151), null, null, null, null, null, null, null, null, null, null, null, null, null, null),
            "COIN_GAIN_L" => new EventPayload(rng.Next(80, 111), null, null, null, null, null, null, null, null, null, null, null, null, null, null),
            "COIN_GAIN_M" => new EventPayload(rng.Next(50, 81), null, null, null, null, null, null, null, null, null, null, null, null, null, null),
            "COIN_LOSE_S" => new EventPayload(-rng.Next(30, 51), null, null, null, null, null, null, null, null, null, null, null, null, null, null),
            "COIN_LOSE_M" => new EventPayload(-rng.Next(50, 81), null, null, null, null, null, null, null, null, null, null, null, null, null, null),
            "DAMAGE_S" => new EventPayload(null, rng.Next(4, 10), null, null, null, null, null, null, null, null, null, null, null, null, null),
            "DAMAGE_M" => new EventPayload(null, rng.Next(10, 14), null, null, null, null, null, null, null, null, null, null, null, null, null),
            "DAMAGE_L" => new EventPayload(null, rng.Next(14, 20), null, null, null, null, null, null, null, null, null, null, null, null, null),
            "DAMAGE_XL" => new EventPayload(null, rng.Next(20, 26), null, null, null, null, null, null, null, null, null, null, null, null, null),
            "HEAL_S" => new EventPayload(null, null, rng.Next(4, 10), null, null, null, null, null, null, null, null, null, null, null, null),
            "HEAL_M" => new EventPayload(null, null, rng.Next(10, 14), null, null, null, null, null, null, null, null, null, null, null, null),
            "HEAL_L" => new EventPayload(null, null, rng.Next(14, 20), null, null, null, null, null, null, null, null, null, null, null, null),
            "HEAL_XL" => new EventPayload(null, null, rng.Next(20, 26), null, null, null, null, null, null, null, null, null, null, null, null),
            "SHOP_MOD" => GenShopPayload(ShopType.Mod, left, rng),
            "SHOP_DEPLOYABLE" => GenShopPayload(ShopType.Deployable, left, rng),
            "SHOP_HEALING" => GenShopPayload(ShopType.Healing, left, rng),
            "SHOP_MERCENARY" => GenShopPayload(ShopType.Mercenary, left, rng),
            "SHOP_SABOTAGE" => GenShopPayload(ShopType.Sabotage, left, rng),
            "SHOP_UPGRADE" => GenShopPayload(ShopType.Upgrade, left, rng),
            "MOD_GAIN" => GenModGainPayload(left, rng, false),
            "GLITCH_GAIN" => GenModGainPayload(left, rng, true),
            "DEPLOYABLE_GAIN" => new EventPayload(null, null, null, null, null, null, rng.Pick(DeployableData.All).Id, null, null, null, null, null, null, null, null),
            "MORALE_RESTORE_FULL" => new EventPayload(null, null, null, null, null, null, null, null, null, null, null, null, 100 - left.Morale, null, null),
            "MORALE_RESTORE_25" => new EventPayload(null, null, null, null, null, null, null, null, null, null, null, null, Math.Min(100 - left.Morale, 25), null, null),
            "MORALE_LOSE_10" => new EventPayload(null, null, null, null, null, null, null, null, null, null, null, null, -10, null, null),
            "MORALE_LOSE_25" => new EventPayload(null, null, null, null, null, null, null, null, null, null, null, null, -25, null, null),
            "HULL_MAX_GAIN" => new EventPayload(null, null, null, null, null, null, null, null, null, null, null, null, null, 10, null),
            "HULL_MAX_LOSE" => new EventPayload(null, null, null, null, null, null, null, null, null, null, null, null, null, -10, null),
            "SYSTEM_UPGRADE_ENGINE" => new EventPayload(null, null, null, null, null, null, null, null, null, "Engine", null, null, null, null, null),
            "SYSTEM_UPGRADE_HULL" => new EventPayload(null, null, null, null, null, null, null, null, null, "Hull", null, null, null, null, null),
            "SYSTEM_UPGRADE_CABIN" => new EventPayload(null, null, null, null, null, null, null, null, null, "Cabin", null, null, null, null, null),
            "SYSTEM_UPGRADE_COMPUTERS" => new EventPayload(null, null, null, null, null, null, null, null, null, "Computers", null, null, null, null, null),
            "COMBAT_MOD_POS" => GenCombatModPayload(true, rng),
            "COMBAT_MOD_NEG" => GenCombatModPayload(false, rng),
            "TRAINING_INT" => new EventPayload(null, null, null, null, null, null, null, null, null, null, null, "Int", null, null, null),
            "TRAINING_CHA" => new EventPayload(null, null, null, null, null, null, null, null, null, null, null, "Cha", null, null, null),
            "TRAINING_RES" => new EventPayload(null, null, null, null, null, null, null, null, null, null, null, "Res", null, null, null),
            "TRAINING_END" => new EventPayload(null, null, null, null, null, null, null, null, null, null, null, "End", null, null, null),
            "TRAIT_POSITIVE" => new EventPayload(null, null, null, null, null, null, null, rng.Pick(TraitData.PositiveTraits).Id, null, null, null, null, null, null, null),
            "TRAIT_NEGATIVE" => new EventPayload(null, null, null, null, null, null, null, rng.Pick(TraitData.NegativeTraits).Id, null, null, null, null, null, null, null),
            "FORMATION_REMOVE" => GenFormationRemovePayload(left, rng),
            "FORMATION_UPGRADE" => GenFormationModPayload(left, rng, true),
            "FORMATION_DOWNGRADE" => GenFormationModPayload(left, rng, false),
            // Location events
            "LOC_DRIFT_BRITTLEBONE" => new EventPayload(null, null, null, null, null, null, null, null, null, null, null, null, null, null, "Brittlebone"),
            "LOC_MYCELIA_SILENT" => new EventPayload(null, null, null, null, null, null, null, null, null, null, null, null, null, null, "Silent"),
            "LOC_SIGG_STEELBORN" => new EventPayload(null, null, null, null, null, null, null, null, null, null, null, null, null, null, "Steelborn"),
            "LOC_ENJUKAN_REPLICANT" => new EventPayload(null, null, null, null, null, null, null, null, null, null, null, null, null, null, "Replicant"),
            "LOC_KHORUM_DWARF" => new EventPayload(null, null, null, null, null, null, null, null, null, null, null, null, null, null, "Dwarf"),
            "LOC_KRAETHOS_SYMBIOTE" => new EventPayload(null, null, null, null, null, null, null, null, null, null, null, null, null, null, "Symbiote"),
            _ => new EventPayload(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null)
        };
    }

    static EventPayload GenShopPayload(ShopType shopType, LeftPageState left, DeterministicRng rng)
    {
        var items = shopType switch
        {
            ShopType.Mod => rng.Sample(ModData.NonGlitches, 6).Select(m => m.Id).ToList(),
            ShopType.Deployable => rng.Sample(DeployableData.All, 6).Select(d => d.Id).ToList(),
            ShopType.Healing => new List<string> { "heal_1", "heal_5", "heal_15", "heal_30" },
            ShopType.Mercenary => rng.Sample(DeployableData.All, 6).Select(d => d.Id).ToList(), // placeholder
            ShopType.Sabotage => GenerateSabotageItems(rng),
            ShopType.Upgrade => GenerateUpgradeItems(left),
            _ => new List<string>()
        };
        return new EventPayload(null, null, null, shopType, items, null, null, null, null, null, null, null, null, null, null);
    }

    static List<string> GenerateSabotageItems(DeterministicRng rng)
    {
        var positives = Enum.GetValues<CombatModifierType>()
            .Where(t => (int)t < 10).ToList();
        return rng.Sample(positives, 6).Select(t => $"sabotage_{t}").ToList();
    }

    static List<string> GenerateUpgradeItems(LeftPageState left)
    {
        var items = new List<string>();
        if (left.Systems.Engine.Level < 3) items.Add("upgrade_Engine");
        if (left.Systems.Cabin.Level < 3) items.Add("upgrade_Cabin");
        if (left.Systems.Hull.Level < 3) items.Add("upgrade_Hull");
        if (left.Systems.Computers.Level < 3) items.Add("upgrade_Computers");
        return items;
    }

    static EventPayload GenModGainPayload(LeftPageState left, DeterministicRng rng, bool isGlitch)
    {
        var pool = isGlitch ? ModData.Glitches : ModData.NonGlitches;
        var mod = rng.Pick(pool);
        return new EventPayload(null, null, null, null, null, mod.Id, null, null, null, null, null, null, null, null, null);
    }

    static EventPayload GenCombatModPayload(bool positive, DeterministicRng rng)
    {
        var types = Enum.GetValues<CombatModifierType>()
            .Where(t => positive ? (int)t < 10 : (int)t >= 10).ToList();
        var chosen = rng.Pick(types);
        var mod = new CombatModifier(chosen, null, positive);
        return new EventPayload(null, null, null, null, null, null, null, null, null, null, mod, null, null, null, null);
    }

    static EventPayload GenFormationRemovePayload(LeftPageState left, DeterministicRng rng)
    {
        var options = rng.Sample(
            left.FormationArchive.Formations.Select(f => f.FormationId).ToList(), 3);
        return new EventPayload(null, null, null, null, null, null, null, null, options, null, null, null, null, null, null);
    }

    static EventPayload GenFormationModPayload(LeftPageState left, DeterministicRng rng, bool upgrade)
    {
        var eligible = left.FormationArchive.Formations
            .Where(f => upgrade ? HasUpgradeSlot(f) : HasDowngradeSlot(f))
            .Select(f => f.FormationId).ToList();
        var options = rng.Sample(eligible, Math.Min(3, eligible.Count));
        return new EventPayload(null, null, null, null, null, null, null, null, options, null, null, null, null, null, null);
    }

    static bool HasUpgradeSlot(FormationInstance f) =>
        f.Slots.Any(s => s.Type == SlotType.Empty || s.Type == SlotType.Pain
            || s.Type == SlotType.Fatigue || s.Type == SlotType.Kill);

    static bool HasDowngradeSlot(FormationInstance f) =>
        f.Slots.Any(s => s.Type == SlotType.Empty || s.Type == SlotType.Boosted
            || s.Type == SlotType.Growth || s.Type == SlotType.Heal);

    // ─── Apply Event Effect to LeftPageState ──────────────────────────────────

    public static LeftPageState ApplyEvent(
        string eventId, EventPayload payload, object? choice,
        LeftPageState left, DeterministicRng rng)
    {
        switch (eventId)
        {
            case "COIN_GAIN_XL": case "COIN_GAIN_L": case "COIN_GAIN_M": case "COIN_GAIN_XL_COSTLY":
                return left with { Coin = left.Coin + (payload.CoinAmount ?? 0) };
            case "COIN_LOSE_S": case "COIN_LOSE_M":
                return left with { Coin = Math.Max(0, left.Coin + (payload.CoinAmount ?? 0)) };
            case "DAMAGE_S": case "DAMAGE_M": case "DAMAGE_L": case "DAMAGE_XL":
                return left with { HullCurrent = Math.Max(0, left.HullCurrent - (payload.DamageAmount ?? 0)) };
            case "HEAL_S": case "HEAL_M": case "HEAL_L": case "HEAL_XL":
                return left with { HullCurrent = Math.Min(left.HullMax, left.HullCurrent + (payload.HealAmount ?? 0)) };
            case "MORALE_RESTORE_FULL": case "MORALE_RESTORE_25": case "MORALE_LOSE_10": case "MORALE_LOSE_25":
                return left with { Morale = Math.Clamp(left.Morale + (payload.MoraleChange ?? 0), 20, 100) };
            case "HULL_MAX_GAIN":
            {
                var gain = payload.HullMaxChange ?? 10;
                return left with { HullMax = left.HullMax + gain, HullCurrent = left.HullCurrent + gain };
            }
            case "HULL_MAX_LOSE":
            {
                var lose = Math.Abs(payload.HullMaxChange ?? 10);
                return left with
                {
                    HullMax = Math.Max(1, left.HullMax - lose),
                    HullCurrent = Math.Min(left.HullCurrent, Math.Max(1, left.HullMax - lose))
                };
            }
            case "MOD_GAIN": return payload.ModId != null ? ApplyModGain(left, payload.ModId) : left;
            case "GLITCH_GAIN": return payload.ModId != null ? ApplyGlitchGain(left, payload.ModId) : left;
            case "DEPLOYABLE_GAIN":
                return payload.DeployableId != null
                    ? left with { Deployables = [.. left.Deployables, new DeployableInstance(Guid.NewGuid().ToString(), payload.DeployableId)] }
                    : left;
            case "SYSTEM_UPGRADE_ENGINE": return ApplySystemUpgrade(left, SystemType.Engine);
            case "SYSTEM_UPGRADE_HULL": return ApplySystemUpgrade(left, SystemType.Hull);
            case "SYSTEM_UPGRADE_CABIN": return ApplySystemUpgrade(left, SystemType.Cabin);
            case "SYSTEM_UPGRADE_COMPUTERS": return ApplySystemUpgrade(left, SystemType.Computers);
            case "COMBAT_MOD_POS": case "COMBAT_MOD_NEG":
                return payload.CombatModifier != null
                    ? left with { PendingCombatModifiers = [.. left.PendingCombatModifiers, payload.CombatModifier] }
                    : left;
            case "TRAINING_INT": return ApplyTraining(left, choice as int? ?? 0, "Int");
            case "TRAINING_CHA": return ApplyTraining(left, choice as int? ?? 0, "Cha");
            case "TRAINING_RES": return ApplyTraining(left, choice as int? ?? 0, "Res");
            case "TRAINING_END": return ApplyTraining(left, choice as int? ?? 0, "End");
            case "TRAIT_POSITIVE": case "TRAIT_NEGATIVE":
                return ApplyTrait(left, payload.TraitId!, choice as int? ?? 0);
            case "CREW_DIES": return ApplyCrewDies(left, choice as int? ?? 0);
            case "FORMATION_REMOVE": return ApplyFormationRemove(left, choice as int? ?? 0, payload);
            case "FORMATION_UPGRADE": return ApplyFormationUpgrade(left, choice as int? ?? 0, payload, rng);
            case "FORMATION_DOWNGRADE": return ApplyFormationDowngrade(left, choice as int? ?? 0, payload, rng);
            case "LOC_MERKAJ_GLITCH": return ApplyClearGlitch(left, rng);
            case "LOC_ENJUKAN_CONVERT": return ApplyConvertToReplicants(left, rng);
            case "LOC_VOIDWATCH_RANDO": return ApplyRandomizeAbilities(left, rng);
            case "LOC_JENSONIA_STATCOPY": return ApplyStatCopy(left, choice as int? ?? 0);
            default: return left;
        }
    }

    static LeftPageState ApplyModGain(LeftPageState left, string modId)
    {
        var mod = ModData.Get(modId)!;
        var instance = new ModInstance(Guid.NewGuid().ToString(), modId);
        var system = left.Systems.Get(mod.System);
        if (system.SlotsAvailable >= (int)mod.Size + 1)
        {
            var newSystem = system with { InstalledMods = [.. system.InstalledMods, instance] };
            return left with { Systems = left.Systems.With(mod.System, newSystem) };
        }
        return left with { ModStorage = [.. left.ModStorage, instance] };
    }

    static LeftPageState ApplyGlitchGain(LeftPageState left, string modId)
    {
        var mod = ModData.Get(modId)!;
        var instance = new ModInstance(Guid.NewGuid().ToString(), modId);
        var system = left.Systems.Get(mod.System);
        if (system.SlotsAvailable >= (int)mod.Size + 1)
        {
            var newSystem = system with { InstalledMods = [.. system.InstalledMods, instance] };
            return left with { Systems = left.Systems.With(mod.System, newSystem) };
        }
        // Displace a non-glitch mod if needed
        var displaced = system.InstalledMods.FirstOrDefault(m => !m.Definition.IsGlitch);
        if (displaced != null)
        {
            var remaining = system.InstalledMods.Where(m => m.InstanceId != displaced.InstanceId).ToList();
            remaining.Add(instance);
            var newSystem = system with { InstalledMods = remaining };
            return left with
            {
                Systems = left.Systems.With(mod.System, newSystem),
                ModStorage = [.. left.ModStorage, displaced]
            };
        }
        return left with { ModStorage = [.. left.ModStorage, instance] };
    }

    static LeftPageState ApplySystemUpgrade(LeftPageState left, SystemType type)
    {
        var system = left.Systems.Get(type);
        if (system.Level >= 3) return left;
        var newSystem = system with { Level = system.Level + 1 };
        var updated = left with { Systems = left.Systems.With(type, newSystem) };
        // Hull upgrade also increases current HP
        if (type == SystemType.Hull)
        {
            var hpGain = SystemValues.MaxHull(newSystem.Level) - SystemValues.MaxHull(system.Level);
            updated = updated with
            {
                HullMax = SystemValues.MaxHull(newSystem.Level),
                HullCurrent = updated.HullCurrent + hpGain
            };
        }
        return updated;
    }

    static LeftPageState ApplyTraining(LeftPageState left, int crewIndex, string stat)
    {
        if (crewIndex < 0 || crewIndex >= left.Crew.Count) return left;
        var crew = left.Crew.ToList();
        crew[crewIndex] = stat switch
        {
            "Int" => crew[crewIndex] with { Stats = crew[crewIndex].Stats.AddInt(1) },
            "Cha" => crew[crewIndex] with { Stats = crew[crewIndex].Stats.AddCha(1) },
            "Res" => crew[crewIndex] with { Stats = crew[crewIndex].Stats.AddRes(1) },
            "End" => crew[crewIndex] with { Stats = crew[crewIndex].Stats.AddEnd(1) },
            _ => crew[crewIndex]
        };
        return left with { Crew = crew };
    }

    static LeftPageState ApplyTrait(LeftPageState left, string traitId, int crewIndex)
    {
        if (crewIndex < 0 || crewIndex >= left.Crew.Count) return left;
        var crew = left.Crew.ToList();
        crew[crewIndex] = crew[crewIndex] with
        {
            Traits = [.. crew[crewIndex].Traits, new TraitInstance(traitId)]
        };
        return left with { Crew = crew };
    }

    static LeftPageState ApplyCrewDies(LeftPageState left, int crewIndex)
    {
        var alive = left.Crew.Where(c => !c.IsDead).ToList();
        if (crewIndex < 0 || crewIndex >= alive.Count) return left;
        var target = alive[crewIndex];
        var crew = left.Crew.ToList();
        var idx = crew.IndexOf(target);
        crew[idx] = crew[idx] with { IsDead = true, HpCurrent = 0 };
        return left with { Crew = crew };
    }

    static LeftPageState ApplyFormationRemove(LeftPageState left, int choiceIndex, EventPayload payload)
    {
        if (payload.FormationOptions == null || choiceIndex >= payload.FormationOptions.Count) return left;
        var formId = payload.FormationOptions[choiceIndex];
        var formations = left.FormationArchive.Formations.Where(f => f.FormationId != formId).ToList();
        return left with { FormationArchive = left.FormationArchive with { Formations = formations } };
    }

    static LeftPageState ApplyFormationUpgrade(LeftPageState left, int choiceIndex, EventPayload payload, DeterministicRng rng)
    {
        if (payload.FormationOptions == null || choiceIndex >= payload.FormationOptions.Count) return left;
        var formId = payload.FormationOptions[choiceIndex];
        var fi = left.FormationArchive.Formations.FindIndex(f => f.FormationId == formId);
        if (fi < 0) return left;
        var form = left.FormationArchive.Formations[fi];
        var upgradeTypes = new[] { SlotType.Boosted, SlotType.Heal, SlotType.MinusFatigue, SlotType.Growth };
        var upgradeable = form.Slots.FindIndex(s => s.Type == SlotType.Empty || s.Type == SlotType.Pain
            || s.Type == SlotType.Fatigue || s.Type == SlotType.Kill);
        if (upgradeable < 0) return left;
        var newType = rng.Pick(upgradeTypes);
        var newSlots = form.Slots.ToList();
        newSlots[upgradeable] = newSlots[upgradeable] with { Type = newType };
        var updatedForm = form with { Slots = newSlots, OriginalSlots = newSlots };
        var formations = left.FormationArchive.Formations.ToList();
        formations[fi] = updatedForm;
        return left with { FormationArchive = left.FormationArchive with { Formations = formations } };
    }

    static LeftPageState ApplyFormationDowngrade(LeftPageState left, int choiceIndex, EventPayload payload, DeterministicRng rng)
    {
        if (payload.FormationOptions == null || choiceIndex >= payload.FormationOptions.Count) return left;
        var formId = payload.FormationOptions[choiceIndex];
        var fi = left.FormationArchive.Formations.FindIndex(f => f.FormationId == formId);
        if (fi < 0) return left;
        var form = left.FormationArchive.Formations[fi];
        var downgradeTypes = new[] { SlotType.Pain, SlotType.Fatigue };
        var downgradeable = form.Slots.FindIndex(s => s.Type == SlotType.Empty || s.Type == SlotType.Boosted
            || s.Type == SlotType.Growth || s.Type == SlotType.Heal);
        if (downgradeable < 0) return left;
        var newType = rng.Pick(downgradeTypes);
        var newSlots = form.Slots.ToList();
        newSlots[downgradeable] = newSlots[downgradeable] with { Type = newType };
        var updatedForm = form with { Slots = newSlots, OriginalSlots = newSlots };
        var formations = left.FormationArchive.Formations.ToList();
        formations[fi] = updatedForm;
        return left with { FormationArchive = left.FormationArchive with { Formations = formations } };
    }

    static LeftPageState ApplyClearGlitch(LeftPageState left, DeterministicRng rng)
    {
        foreach (var sysType in new[] { SystemType.Engine, SystemType.Cabin, SystemType.Hull, SystemType.Computers })
        {
            var sys = left.Systems.Get(sysType);
            var glitch = sys.InstalledMods.FirstOrDefault(m => m.Definition.IsGlitch);
            if (glitch != null)
            {
                var newMods = sys.InstalledMods.Where(m => m.InstanceId != glitch.InstanceId).ToList();
                return left with { Systems = left.Systems.With(sysType, sys with { InstalledMods = newMods }) };
            }
        }
        return left;
    }

    static LeftPageState ApplyConvertToReplicants(LeftPageState left, DeterministicRng rng)
    {
        var replicantDef = RaceData.Get(Race.Replicant);
        var crew = left.Crew.Select(c =>
        {
            if (c.IsDead) return c;
            var oldDef = RaceData.Get(c.Race);
            var stats = c.Stats
                .AddRes(-oldDef.BonusRes).AddInt(-oldDef.BonusInt)
                .AddCha(-oldDef.BonusCha).AddEnd(-oldDef.BonusEnd)
                .AddRes(replicantDef.BonusRes).AddInt(replicantDef.BonusInt)
                .AddCha(replicantDef.BonusCha).AddEnd(replicantDef.BonusEnd);
            return c with { Race = Race.Replicant, Stats = stats, HpMax = replicantDef.BaseHp, HpCurrent = replicantDef.BaseHp };
        }).ToList();
        return left with { Crew = crew };
    }

    static LeftPageState ApplyRandomizeAbilities(LeftPageState left, DeterministicRng rng)
    {
        var crew = left.Crew.Select(c =>
        {
            if (c.IsDead) return c;
            var isRare = rng.Chance(0.25);
            var ability = isRare ? rng.Pick(AbilityData.Rare) : rng.Pick(AbilityData.Common);
            var isDual = rng.Chance(0.30);
            return c with { Abilities = [new AbilityInstance(ability.Id, isDual)] };
        }).ToList();
        return left with { Crew = crew };
    }

    static LeftPageState ApplyStatCopy(LeftPageState left, int sourceCrewIndex)
    {
        var alive = left.Crew.Where(c => !c.IsDead).ToList();
        if (sourceCrewIndex < 0 || sourceCrewIndex >= alive.Count) return left;
        var source = alive[sourceCrewIndex];
        var crew = left.Crew.Select(c =>
        {
            if (c.IsDead || c.Id == source.Id) return c;
            return c with { Stats = source.Stats };
        }).ToList();
        return left with { Crew = crew };
    }
}
