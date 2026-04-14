namespace TornPages.Engine;

/// <summary>
/// Calculates a crew member's combat output given their context.
/// Shared between CombatResolver (execution) and the forecaster (planning UI).
/// </summary>
public static class EffectCalculator
{
    /// <summary>
    /// Compute base effect level (ignoring slot/lane modifiers).
    /// </summary>
    public static int EffectLevel(
        CrewMemberState crew,
        CombatState combat,
        LaneId? lane,
        LeftPageState left,
        FormationInstance? formation = null)
    {
        var el = 1 + GetBaseResolve(crew, combat, formation);
        el += crew.PermanentEffectLevelBonus;
        el += crew.CombatElBonus;
        el += GetTraitElModifiers(crew, combat, lane, left);
        el += GetModElBonus(left, combat);
        el = Math.Max(0, el);
        return el;
    }

    static int GetBaseResolve(CrewMemberState crew, CombatState combat, FormationInstance? formation)
    {
        // Formation overrides
        if (formation?.Definition.Passive?.Type == FormationPassiveType.BrainformOverride)
            return crew.Stats.Intelligence;
        if (formation?.Definition.Passive?.Type == FormationPassiveType.SmileformOverride)
            return crew.Stats.Charisma;
        // Crew trait override
        if (crew.Traits.Any(t => t.Definition.EffectType == TraitEffectType.ChaInsteadOfRes))
            return crew.Stats.Charisma;
        return crew.Stats.Resolve;
    }

    static int GetTraitElModifiers(CrewMemberState crew, CombatState combat, LaneId? lane, LeftPageState left)
    {
        var bonus = 0;
        foreach (var trait in crew.Traits)
        {
            if (trait.Definition.IsNegative)
            {
                bonus += GetNegativeTraitElMod(trait, combat, lane, left, crew);
            }
            else
            {
                bonus += GetPositiveTraitElMod(trait, combat, lane, left, crew);
            }
        }
        return bonus;
    }

    static int GetPositiveTraitElMod(TraitInstance trait, CombatState combat, LaneId? lane, LeftPageState left, CrewMemberState crew)
    {
        if (trait.Definition.EffectType == null) return 0;
        return trait.Definition.EffectType.Value switch
        {
            TraitEffectType.PlusElPerHundredCoin => left.Coin / 100,
            TraitEffectType.PlusElAfterEachPage => trait.AccumulatedBonus,
            TraitEffectType.ThreePlusElDecayEachPage => Math.Max(0, 3 - trait.AccumulatedBonus),
            TraitEffectType.DoubleElAtOneHull => left.HullCurrent == 1 ? EffectLevel(crew, combat, lane, left) : 0,
            TraitEffectType.PlusElEachTimeUsed => trait.AccumulatedBonus,
            TraitEffectType.PlusElPerLockOnInLane => lane.HasValue ? combat.LockOn[(int)lane] : 0,
            TraitEffectType.SevenElMinusPerOtherCrew => Math.Max(0, 7 - (left.Crew.Count(c => !c.IsDead) - 1)),
            TraitEffectType.PlusTwoElLeftLane => lane == LaneId.Left ? 2 : 0,
            TraitEffectType.PlusTwoElCenterLane => lane == LaneId.Center ? 2 : 0,
            TraitEffectType.PlusTwoElRightLane => lane == LaneId.Right ? 2 : 0,
            TraitEffectType.PlusElPerSameRaceCrew =>
                left.Crew.Count(c => !c.IsDead && c.Id != crew.Id && c.Race == crew.Race),
            TraitEffectType.PlusElPerTenIncomingThreat =>
                lane.HasValue ? combat.GeneratedIntents.Where(i => i.LaneIndex == (int)lane)
                    .Sum(i => i.ResolvedValue) / 10 : 0,
            TraitEffectType.PlusElWhenHullDamaged => trait.AccumulatedBonus,
            TraitEffectType.PlusElWhenSelfDamaged => trait.AccumulatedBonus,
            _ => 0
        };
    }

    static int GetNegativeTraitElMod(TraitInstance trait, CombatState combat, LaneId? lane, LeftPageState left, CrewMemberState crew)
    {
        if (trait.Definition.NegativeEffectType == null) return 0;
        return trait.Definition.NegativeEffectType.Value switch
        {
            NegativeTraitEffectType.MinusElPerLockOnInLane => lane.HasValue ? -combat.LockOn[(int)lane] : 0,
            NegativeTraitEffectType.MinusElPerDifferentRaceCrew =>
                -left.Crew.Count(c => !c.IsDead && c.Id != crew.Id && c.Race != crew.Race),
            NegativeTraitEffectType.MinusElWhenHullDamaged => -trait.AccumulatedBonus,
            NegativeTraitEffectType.MinusElInBossFights => combat.Enemy.IsBoss ? -1 : 0,
            NegativeTraitEffectType.MinusOneResAfterEachPage => -trait.AccumulatedBonus,
            _ => 0
        };
    }

    static int GetModElBonus(LeftPageState left, CombatState combat)
    {
        var bonus = 0;
        var allMods = GetAllEquippedMods(left);

        if (allMods.Any(m => m.EffectType == ModEffectType.CrewPlusElAtLowHull) && left.HullCurrent < left.HullMax / 4)
            bonus += 1;
        if (allMods.Any(m => m.EffectType == ModEffectType.CrewPlusElAtFullHull) && left.HullCurrent == left.HullMax)
            bonus += 1;
        if (allMods.Any(m => m.EffectType == ModEffectType.CrewPlusElFirstPage) && combat.PageNumber == 1)
            bonus += 1;
        foreach (var m in allMods.Where(m => m.EffectType == ModEffectType.CrewPlusElPerHundredCoin))
            bonus += Math.Min(5, left.Coin / 100);

        foreach (var mod in combat.ActiveModifiers)
        {
            if (mod.Type == CombatModifierType.CrewPlusOneEl) bonus++;
            if (mod.Type == CombatModifierType.CrewMinusOneEl) bonus--;
        }

        return bonus;
    }

    /// <summary>
    /// Calculate the full output for one ability through one slot into one lane.
    /// Returns: (shieldOut, hackOut, chargeOut, boostPct, barrierOut, healOut, fusionOut)
    /// </summary>
    public static AbilityOutput Calculate(
        CrewMemberState crew,
        AbilityInstance ability,
        SlotInstance slot,
        LaneId lane,
        CombatState combat,
        LeftPageState left,
        FormationInstance formation,
        bool isSilentBoost = false)
    {
        var el = EffectLevel(crew, combat, lane, left, formation);

        // Clamp EL to 0 minimum
        el = Math.Max(0, el);

        int baseOutput;
        if (ability.Definition.Rarity == AbilityRarity.Common)
            baseOutput = ability.Definition.Type == AbilityType.Boost ? (int)(10 * el) : 2 * el;
        else
            baseOutput = el; // Rare: 1×

        // Dual halving
        if (ability.IsDual) baseOutput = baseOutput / 2;

        // Is double slot (fires twice — handled at call site, not here)
        // Lane boost accumulation
        var laneBoost = GetLaneBoost(lane, combat, left);
        var slotBoost = slot.Type == SlotType.Boosted ? 50 : 0;
        if (isSilentBoost) slotBoost += 50;
        if (slot.Type == SlotType.MinusFatigue
            && GetAllEquippedMods(left).Any(m => m.EffectType == ModEffectType.MinusFatigueSlotsBoostedTwentyFive))
            slotBoost += 25;

        // NoBoosted trait
        if (crew.Traits.Any(t => t.Definition.EffectType == TraitEffectType.PlusOneEndNoBoosted) ||
            crew.Traits.Any(t => t.Definition.NegativeEffectType == NegativeTraitEffectType.CantBeBoosted))
        {
            slotBoost = 0;
            laneBoost = 0;
        }

        // FiftyPercentMoreBoost trait
        var boostMultiplier = 1f;
        if (crew.Traits.Any(t => t.Definition.EffectType == TraitEffectType.FiftyPercentMoreBoost))
            boostMultiplier = 1.5f;

        var totalBoostPct = (laneBoost + slotBoost) * boostMultiplier;

        // Persistent boost debuff from Act III boss
        totalBoostPct += combat.PersistentBoostModifier[(int)lane] * 100f; // stored as -0.1, -0.2, etc.

        // Formation CoreBonus passive
        var formPassive = formation.Definition.Passive;
        if (formPassive?.Type == FormationPassiveType.CoreBonus &&
            formPassive.AbilityTypeFilter == ability.Definition.Type)
            totalBoostPct += formPassive.FloatValue * 100;

        // PowerColumn passive: boost 50% more effective
        if (formPassive?.Type == FormationPassiveType.PowerColumnBoost)
            totalBoostPct *= formPassive.FloatValue;

        float finalOutput = baseOutput * (1f + totalBoostPct / 100f);

        // Morale multiplier
        float moraleMultiplier = left.Morale / 100f;
        if (!crew.Traits.Any(t => t.Definition.EffectType == TraitEffectType.UnaffectedByMorale))
        {
            if (crew.Traits.Any(t => t.Definition.NegativeEffectType == NegativeTraitEffectType.MoraleAffectsDoubly))
                moraleMultiplier = moraleMultiplier * moraleMultiplier;
            finalOutput *= moraleMultiplier;
        }

        // Graveyard bonus for exhausted crew
        if (formPassive?.Type == FormationPassiveType.GraveyardBonus)
        {
            var effectiveFatigue = crew.FatigueBaseline + combat.SlotUseCountThisPage.GetValueOrDefault(crew.Id, 0);
            if (effectiveFatigue > crew.Stats.Endurance)
                finalOutput *= (1f + formPassive.FloatValue);
        }

        var rounded = (int)Math.Round(finalOutput);

        return ability.Definition.Type switch
        {
            AbilityType.Shield  => new AbilityOutput(rounded, 0, 0, 0, 0, 0, 0),
            AbilityType.Hack    => new AbilityOutput(0, rounded, 0, 0, 0, 0, 0),
            AbilityType.Charge  => new AbilityOutput(0, 0, rounded, 0, 0, 0, 0),
            AbilityType.Boost   => new AbilityOutput(0, 0, 0, rounded, 0, 0, 0),
            AbilityType.Barrier => new AbilityOutput(0, 0, 0, 0, rounded, 0, 0),
            AbilityType.Heal    => new AbilityOutput(0, 0, 0, 0, 0, rounded, 0),
            AbilityType.Fusion  => new AbilityOutput(0, 0, 0, 0, 0, 0, rounded),
            _ => new AbilityOutput(0, 0, 0, 0, 0, 0, 0)
        };
    }

    static int GetLaneBoost(LaneId lane, CombatState combat, LeftPageState left)
    {
        var boost = 0;
        // From mods
        var allMods = GetAllEquippedMods(left);
        if (allMods.Any(m => m.EffectType == ModEffectType.PermanentBoostFlankLanes) && lane != LaneId.Center)
            boost += 20;
        if (allMods.Any(m => m.EffectType == ModEffectType.PermanentBoostCenterLane) && lane == LaneId.Center)
            boost += 40;
        foreach (var m in allMods.Where(m => m.EffectType == ModEffectType.BoostPerLockOn))
            boost += combat.LockOn[(int)lane] * 10;
        if (allMods.Any(m => m.EffectType == ModEffectType.MaxLockOnLaneBoost)
            && combat.LockOn[(int)lane] >= GetMaxLockOn(allMods))
            boost += 100;
        return boost;
    }

    public static int GetMaxLockOn(IEnumerable<ModDefinition> mods)
    {
        var cap = 5;
        if (mods.Any(m => m.EffectType == ModEffectType.MaxLockOnReducedByOne)) cap--;
        return Math.Max(0, cap);
    }

    public static IEnumerable<ModDefinition> GetAllEquippedMods(LeftPageState left)
    {
        foreach (var s in new[] { left.Systems.Engine, left.Systems.Cabin, left.Systems.Hull, left.Systems.Computers })
            foreach (var m in s.InstalledMods)
                yield return m.Definition;
    }
}

public record AbilityOutput(int Shield, int Hack, int Charge, int BoostPct, int Barrier, int HullHeal, int Fusion);
