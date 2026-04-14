namespace TornPages.Engine;

/// <summary>
/// Transforms a RunState (or a historical page) into a RenderState for the React UI.
/// Pure projection — no side effects.
/// </summary>
public static class RenderGenerator
{
    private static readonly string[] LaneNames = ["Left", "Center", "Right"];

    public static RenderState Generate(RunState run, int pageIndex)
    {
        var page = run.Pages[pageIndex];
        var left = page.Left;
        var right = page.Right;
        var isCurrentPage = pageIndex == run.CurrentPageIndex;

        // Compute deltas vs prior page left state
        var deltas = new Dictionary<string, object?>();
        if (pageIndex > 0)
        {
            var prev = run.Pages[pageIndex - 1].Left;
            if (left.HullCurrent != prev.HullCurrent)
                deltas["hullCurrent"] = left.HullCurrent - prev.HullCurrent;
            if (left.Coin != prev.Coin)
                deltas["coin"] = left.Coin - prev.Coin;
            if (left.Morale != prev.Morale)
                deltas["morale"] = left.Morale - prev.Morale;
            if (left.Crew.Count != prev.Crew.Count)
                deltas["crewCount"] = left.Crew.Count - prev.Crew.Count;
        }

        // Pending interjections: author flavor text for certain page types
        var interjections = new List<string>();

        return new RenderState(
            Left: BuildLeftRender(left, right),
            Right: BuildRightRender(left, right),
            Deltas: deltas,
            IsCurrentPage: isCurrentPage,
            PendingInterjections: interjections,
            CurrentPageIndex: run.CurrentPageIndex,
            TotalPages: run.Pages.Count,
            RunPhase: run.Metadata.Phase,
            IsVictory: left.IsVictory,
            IsRunOver: left.IsRunOver);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Left page
    // ─────────────────────────────────────────────────────────────────────────

    static LeftPageRender BuildLeftRender(LeftPageState left, RightPageState right)
    {
        // Grab combat state if on a combat page (for EL forecasts)
        CombatState? combat = right is CombatRightPage crp ? crp.Combat : null;
        FormationInstance? activeFormation = combat?.ActiveFormationId != null
            ? left.FormationArchive.Formations.FirstOrDefault(f => f.FormationId == combat.ActiveFormationId)
            : null;

        var crew = left.Crew.Select(c => BuildCrewRender(c, left, combat, activeFormation)).ToList();

        var formations = left.FormationArchive.Formations
            .Select(f => BuildFormationRender(f, combat)).ToList();

        var systems = BuildSystemsRender(left);
        var modStorage = left.ModStorage.Select(m => BuildModRender(m.InstanceId, m.Definition)).ToList();
        var deployables = left.Deployables.Select(d => BuildDeployableRender(d)).ToList();

        return new LeftPageRender(
            HullCurrent: left.HullCurrent,
            HullMax: left.HullMax,
            Coin: left.Coin,
            Morale: left.Morale,
            PagesRemaining: left.PagesRemaining,
            ChapterNumber: left.ChapterNumber,
            ActNumber: left.ActNumber,
            Crew: crew,
            Formations: formations,
            LastUsedFormationId: left.FormationArchive.LastUsedFormationId,
            Systems: systems,
            ModStorage: modStorage,
            Deployables: deployables,
            Stats: left.Stats);
    }

    static CrewRender BuildCrewRender(
        CrewMemberState crew, LeftPageState left,
        CombatState? combat, FormationInstance? formation)
    {
        var dummyCombat = combat ?? new CombatState();
        var el = EffectCalculator.EffectLevel(crew, dummyCombat, null, left, formation);
        var isExhausted = (crew.FatigueBaseline + (combat?.SlotUseCountThisPage.GetValueOrDefault(crew.Id, 0) ?? 0))
            > crew.Stats.Endurance;

        var abilities = crew.Abilities.Select(a => new AbilityRender(
            Id: a.AbilityId,
            TypeName: a.Definition.Type.ToString(),
            Type: a.Definition.Type,
            IsDual: a.IsDual,
            DisplayText: BuildAbilityDisplayText(a))).ToList();

        var traits = crew.Traits.Select(t => new TraitRender(
            Id: t.TraitId,
            Name: t.Definition.Name,
            Description: t.Definition.Description,
            IsNegative: t.Definition.IsNegative)).ToList();

        var raceDef = RaceData.Get(crew.Race);

        return new CrewRender(
            Id: crew.Id,
            Name: crew.Name,
            RaceName: crew.Race.ToString(),
            Race: crew.Race,
            Stats: crew.Stats,
            HpCurrent: crew.HpCurrent,
            HpMax: crew.HpMax,
            FatigueBaseline: crew.FatigueBaseline,
            Endurance: crew.Stats.Endurance,
            IsDead: crew.IsDead,
            IsPanicked: crew.IsPanicked,
            IsExhausted: isExhausted,
            Abilities: abilities,
            Traits: traits,
            Backstory: crew.Backstory,
            EffectiveEffectLevel: el,
            PermanentBonus: crew.PermanentEffectLevelBonus);
    }

    static string BuildAbilityDisplayText(AbilityInstance a)
    {
        var d = a.Definition;
        var dual = a.IsDual ? " (Dual)" : "";
        var rare = d.Rarity == AbilityRarity.Rare ? " [Rare]" : "";
        return $"{d.Type}{dual}{rare}";
    }

    static FormationRender BuildFormationRender(FormationInstance f, CombatState? combat)
    {
        var def = f.Definition;
        var slots = f.Slots.Select(s => new SlotRender(
            Type: s.Type,
            TypeName: s.Type.ToString(),
            ConnectedLanes: s.ConnectedLanes.Select(l => l.ToString()).ToList(),
            IsDouble: s.IsDouble,
            ChainId: s.ChainId,
            SlottedCrewId: null)).ToList();

        // If in combat, include slotted crew
        if (combat?.ActiveFormationId == f.FormationId)
        {
            var byLane = combat.SlottedByLane;
            for (var si = 0; si < slots.Count; si++)
            {
                var slot = slots[si];
                // Find a slotted crew in any connected lane at this slot index
                string? slottedId = null;
                foreach (var lane in slot.ConnectedLanes)
                {
                    if (Enum.TryParse<LaneId>(lane, out var lid))
                    {
                        var found = byLane[(int)lid].FirstOrDefault(sc => sc.SlotIndex == si);
                        if (found != null) { slottedId = found.CrewId; break; }
                    }
                }
                slots[si] = slot with { SlottedCrewId = slottedId };
            }
        }

        return new FormationRender(
            Id: f.FormationId,
            Name: def.Name,
            Rarity: def.Rarity,
            Slots: slots,
            PassiveDescription: def.Passive != null ? DescribeFormationPassive(def.Passive) : null,
            IsGreyedOut: false);
    }

    static SystemsRender BuildSystemsRender(LeftPageState left)
    {
        var totalCha = left.Crew.Where(c => !c.IsDead).Sum(c => c.Stats.Charisma);

        SystemRender Make(SystemState sys, SystemType type)
        {
            var name = type.ToString();
            var (label, value) = type switch
            {
                SystemType.Engine => ("Speed", sys.Level),
                SystemType.Cabin => ("Crew Cap", SystemValues.CrewCapacity(sys.Level)),
                SystemType.Hull => ("Max Hull Bonus", sys.Level * 5),
                SystemType.Computers => ("Formations/Draw", SystemValues.FormationsPerPage(sys.Level)),
                _ => ("Level", sys.Level)
            };
            var mods = sys.InstalledMods.Select(m => BuildModRender(m.InstanceId, m.Definition)).ToList();
            return new SystemRender(
                Name: name,
                Type: type,
                Level: sys.Level,
                ModSlots: sys.ModSlots,
                SlotsUsed: sys.SlotsUsed,
                PrimaryValueLabel: label,
                PrimaryValue: value,
                InstalledMods: mods);
        }

        return new SystemsRender(
            Engine: Make(left.Systems.Engine, SystemType.Engine),
            Cabin: Make(left.Systems.Cabin, SystemType.Cabin),
            Hull: Make(left.Systems.Hull, SystemType.Hull),
            Computers: Make(left.Systems.Computers, SystemType.Computers),
            TotalCharisma: totalCha);
    }

    static ModRender BuildModRender(string instanceId, ModDefinition def)
    {
        return new ModRender(
            InstanceId: instanceId,
            ModId: def.Id,
            Name: def.Name,
            Description: def.Description,
            Size: def.Size,
            IsGlitch: def.IsGlitch,
            System: def.System);
    }

    static DeployableRender BuildDeployableRender(DeployableInstance d)
    {
        return new DeployableRender(
            InstanceId: d.InstanceId,
            DeployableId: d.DeployableId,
            Name: d.Definition.Name,
            Description: d.Definition.Description);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Right page dispatch
    // ─────────────────────────────────────────────────────────────────────────

    static RightPageRender BuildRightRender(LeftPageState left, RightPageState right) => right switch
    {
        PrologueCrewRightPage r   => BuildPrologueCrew(r),
        PrologueBoonRightPage r   => BuildPrologueBoon(left, r),
        LocationSelectRightPage r => BuildLocationSelect(r),
        BuildUpRightPage r        => BuildBuildUp(left, r),
        EventRightPage r          => BuildEvent(left, r),
        EnemySelectRightPage r    => BuildEnemySelect(left, r),
        CombatRightPage r         => BuildCombat(left, r),
        CombatRewardRightPage r   => BuildCombatReward(left, r),
        ChapterConclusionRightPage r => BuildChapterConclusion(left, r),
        EpilogueRightPage r       => BuildEpilogue(left, r),
        _ => throw new EngineException($"Unknown right page type: {right.GetType().Name}")
    };

    // ─── Prologue crew ────────────────────────────────────────────────────────

    static PrologueCrewRightRender BuildPrologueCrew(PrologueCrewRightPage r)
    {
        var targetAbility = r.CrewIndex switch
        {
            0 => AbilityType.Shield,
            1 => AbilityType.Charge,
            2 => AbilityType.Hack,
            _ => AbilityType.Shield
        };
        return new PrologueCrewRightRender(
            CrewIndex: r.CrewIndex,
            TargetAbility: targetAbility,
            Options: r.GeneratedOptions[0],
            Selections: r.Selections,
            IsCommitted_: r.IsCommitted_);
    }

    // ─── Prologue boon ────────────────────────────────────────────────────────

    static PrologueBoonRightRender BuildPrologueBoon(LeftPageState left, PrologueBoonRightPage r)
    {
        var modOpts = r.ModOptions.Select(m => BuildModRender(m.Id, m)).ToList();
        var formOpts = r.FormationOptions.Select(f =>
        {
            var inst = RunFactory.ToInstance(f);
            return BuildFormationRender(inst, null);
        }).ToList();
        var depOpts = r.DeployableOptions.Select(d => new DeployableRender(
            InstanceId: d.Id,
            DeployableId: d.Id,
            Name: d.Name,
            Description: d.Description)).ToList();

        return new PrologueBoonRightRender(
            ModOptions: modOpts,
            FormationOptions: formOpts,
            DeployableOptions: depOpts,
            SelectedModId: r.SelectedModId,
            SelectedFormationId: r.SelectedFormationId,
            SelectedDeployableId: r.SelectedDeployableId,
            IsCommitted_: r.IsCommitted_);
    }

    // ─── Location select ──────────────────────────────────────────────────────

    static LocationSelectRightRender BuildLocationSelect(LocationSelectRightPage r)
    {
        var opts = r.LocationOptions.Select(id =>
        {
            var loc = LocationData.Get(id)!;
            return new LocationRender(
                Id: loc.Id,
                Name: loc.Name,
                LoreDescription: loc.LoreDescription,
                SpecialEventName: loc.SpecialEventId != null
                    ? EventData.Get(loc.SpecialEventId)?.Name : null);
        }).ToList();

        return new LocationSelectRightRender(
            Options: opts,
            SelectedLocationId: r.SelectedLocationId,
            IsCommitted_: r.IsCommitted_);
    }

    // ─── Build-up ─────────────────────────────────────────────────────────────

    static BuildUpRightRender BuildBuildUp(LeftPageState left, BuildUpRightPage r)
    {
        var dtThreshold = 4; // Minimum DT required to proceed
        var pool = r.EventPool.Select(id =>
        {
            var evt = EventData.Get(id)!;
            var summary = evt.Style switch
            {
                EventStyle.Shop => "Visit a merchant",
                EventStyle.Option => "A decision to make",
                EventStyle.CrewCreation => "New crew opportunity",
                _ => evt.Name
            };
            return new EventCardRender(
                Id: evt.Id,
                Name: evt.Name,
                DramaticTension: evt.DramaticTension,
                EffectSummary: summary,
                TearingAllowed: evt.TearingAllowed,
                Style: evt.Style);
        }).ToList();

        var totalDt = r.SelectedEventIds.Sum(id => EventData.Get(id)?.DramaticTension ?? 0);
        var canReady = totalDt >= dtThreshold;

        return new BuildUpRightRender(
            EventPool: pool,
            SelectedEventIds: r.SelectedEventIds,
            TotalDT: totalDt,
            DtThreshold: dtThreshold,
            CanReady: canReady,
            IsCommitted_: r.IsCommitted_);
    }

    // ─── Event ────────────────────────────────────────────────────────────────

    static EventRightRender BuildEvent(LeftPageState left, EventRightPage r)
    {
        var evt = EventData.Get(r.EventId)!;
        var payload = r.Payload;

        // Options labels (for Option-style events)
        List<string>? optionLabels = evt.Style == EventStyle.Option
            ? ["Option A", "Option B"]
            : null;

        // Check if shop is active
        ShopRightRender? shopData = null;
        if (evt.Style == EventStyle.Shop && payload.ShopItemIds != null && !r.IsCommitted_)
        {
            shopData = BuildShopRender(left, payload, false);
        }

        var selectedOption = r.PlayerChoice is int i ? i : (int?)null;
        // Crew dismissal: check if event requires dismissal based on payload (no formal field on evt)
        var needsDismissal = false;

        return new EventRightRender(
            EventId: r.EventId,
            EventName: evt.Name,
            Style: evt.Style,
            NarrativeText: BuildEventNarrativeText(evt, payload),
            EffectDescription: BuildEventEffectDescription(evt, payload),
            OptionLabels: optionLabels,
            SelectedOption: selectedOption,
            ShopData: shopData,
            NeedsCrewDismissal: needsDismissal,
            IsCommitted_: r.IsCommitted_,
            TearingAllowed_: evt.TearingAllowed);
    }

    static ShopRightRender BuildShopRender(LeftPageState left, EventPayload payload, bool exited)
    {
        var shopType = payload.ShopType ?? ShopType.Mod;
        var items = new List<ShopItemRender>();

        if (payload.ShopItemIds != null)
        {
            foreach (var id in payload.ShopItemIds)
            {
                var mod = ModData.Get(id);
                if (mod != null)
                {
                    var cost = mod.IntValue > 0 ? mod.IntValue : (int)mod.Size * 20 + 20;
                    items.Add(new ShopItemRender(
                        ItemId: id,
                        Name: mod.Name,
                        Description: mod.Description,
                        Cost: cost,
                        CanAfford: left.Coin >= cost,
                        Category: "Mod"));
                    continue;
                }
                var dep = DeployableData.Get(id);
                if (dep != null)
                {
                    var cost = dep.IntValue > 0 ? dep.IntValue : 30;
                    items.Add(new ShopItemRender(
                        ItemId: id,
                        Name: dep.Name,
                        Description: dep.Description,
                        Cost: cost,
                        CanAfford: left.Coin >= cost,
                        Category: "Deployable"));
                    continue;
                }
                var form = FormationData.Get(id);
                if (form != null)
                {
                    var cost = form.Rarity switch
                    {
                        FormationRarity.Common => 60,
                        FormationRarity.Uncommon => 100,
                        FormationRarity.Rare => 150,
                        _ => 60
                    };
                    items.Add(new ShopItemRender(
                        ItemId: id,
                        Name: form.Name,
                        Description: form.Passive != null ? DescribeFormationPassive(form.Passive) : "",
                        Cost: cost,
                        CanAfford: left.Coin >= cost,
                        Category: "Formation"));
                }
            }
        }

        return new ShopRightRender(
            ShopType: shopType,
            Items: items,
            CurrentCoin: left.Coin,
            IsExited: exited);
    }

    static string BuildEventNarrativeText(EventDefinition evt, EventPayload payload)
    {
        // Flavor text based on event style
        return evt.Style switch
        {
            EventStyle.Announcement => "Something happens aboard the ship...",
            EventStyle.Option => "The crew faces a decision.",
            EventStyle.Shop => "A merchant approaches with wares.",
            EventStyle.CrewCreation => "A new face joins the crew.",
            _ => "An event unfolds."
        };
    }

    static string BuildEventEffectDescription(EventDefinition evt, EventPayload payload)
    {
        // Build from available payload data
        var parts = new List<string>();
        if (payload.CoinAmount.HasValue)
            parts.Add(payload.CoinAmount.Value >= 0 ? $"+{payload.CoinAmount} coin" : $"{payload.CoinAmount} coin");
        if (payload.DamageAmount.HasValue)
            parts.Add($"-{payload.DamageAmount} hull");
        if (payload.HealAmount.HasValue)
            parts.Add($"+{payload.HealAmount} hull");
        if (payload.MoraleChange.HasValue)
            parts.Add(payload.MoraleChange.Value >= 0 ? $"+{payload.MoraleChange} morale" : $"{payload.MoraleChange} morale");
        if (payload.ModId != null)
            parts.Add($"Gain mod: {ModData.Get(payload.ModId)?.Name ?? payload.ModId}");
        if (payload.TraitId != null)
            parts.Add($"Gain trait: {TraitData.Get(payload.TraitId)?.Name ?? payload.TraitId}");
        if (payload.CombatModifier != null)
            parts.Add($"Combat modifier: {payload.CombatModifier.DisplayName}");
        if (parts.Count == 0) parts.Add(evt.Name);
        return string.Join(", ", parts);
    }

    // ─── Enemy select ─────────────────────────────────────────────────────────

    static EnemySelectRightRender BuildEnemySelect(LeftPageState left, EnemySelectRightPage r)
    {
        var ct = left.PendingCombatThreshold
            ?? ChargeThresholds.Get(left.ChapterNumber);

        WeaponRender ToWeaponRender(string id)
        {
            var w = WeaponData.Get(id)!;
            return new WeaponRender(
                Id: w.Id,
                Name: w.Name,
                LeftEffect: w.LeftEffect,
                CenterEffect: w.CenterEffect,
                RightEffect: w.RightEffect,
                RandomiseLanes: w.RandomiseLanes,
                Description: w.SpecialNote ?? "");
        }

        var locked1 = ToWeaponRender(r.LockedWeaponIds[0]);
        var locked2 = ToWeaponRender(r.LockedWeaponIds[1]);
        var choices = r.WeaponChoicePool.Select(ToWeaponRender).ToList();
        var abilities = r.AbilityPool.Select(id =>
        {
            var a = EnemyAbilityData.Get(id)!;
            return new EnemyAbilityRender(
                Id: a.Id,
                Name: a.Name,
                Description: a.Description,
                IsSelected: r.SelectedAbilityIds.Contains(id));
        }).ToList();

        // Boss info
        string? bossName = null, bossDesc = null;
        if (r.IsBoss && r.BossId != null)
        {
            bossName = r.BossId switch
            {
                "GATE_WARDEN" => "The Gate Warden",
                "PATIENCE_ENGINE" => "The Patience Engine",
                "THE_COLLECTOR" => "The Collector",
                "HYPERION_SHIELD" => "The Hyperion Shield",
                _ => r.BossId
            };
            bossDesc = r.BossId switch
            {
                "GATE_WARDEN" => "An ancient guardian that grows more aggressive as the battle progresses.",
                "PATIENCE_ENGINE" => "A mechanical juggernaut that escalates lock-on until you're overwhelmed.",
                "THE_COLLECTOR" => "A predator that kidnaps your crew and uses them against you.",
                "HYPERION_SHIELD" => "The final sentinel, protected by an impenetrable super-shield.",
                _ => ""
            };
        }

        return new EnemySelectRightRender(
            LockedWeapon1: locked1,
            LockedWeapon2: locked2,
            WeaponChoices: choices,
            SelectedWeaponId: r.SelectedWeaponId,
            AbilityPool: abilities,
            SelectedAbilityIds: r.SelectedAbilityIds,
            ChargeThreshold: ct,
            IsCommitted_: r.IsCommitted_,
            IsBoss: r.IsBoss,
            BossName: bossName,
            BossDescription: bossDesc);
    }

    // ─── Combat ───────────────────────────────────────────────────────────────

    static CombatRightRender BuildCombat(LeftPageState left, CombatRightPage r)
    {
        var combat = r.Combat;
        var formation = combat.ActiveFormationId != null
            ? left.FormationArchive.Formations.FirstOrDefault(f => f.FormationId == combat.ActiveFormationId)
            : null;

        var enemyRender = BuildEnemyCombatRender(combat);
        var lanes = BuildLaneRenders(left, combat, formation);
        var availableForms = BuildAvailableFormations(left, combat);
        var modifiers = combat.ActiveModifiers
            .Select(m => new CombatModifierRender(Name: m.DisplayName, IsPositive: m.IsPositive))
            .ToList();
        var availableDeps = left.Deployables
            .Where(d => !combat.DeployablesUsedThisPage.Contains(d.InstanceId))
            .Where(_ => !combat.ActiveModifiers.Any(m => m.Type == CombatModifierType.DeployablesDisabled))
            .Select(d => BuildDeployableRender(d))
            .ToList();

        var forecast = BuildForecast(left, combat, formation);

        return new CombatRightRender(
            Enemy: enemyRender,
            ChargeAccumulated: combat.ChargeAccumulated,
            ChargeThreshold: left.PendingCombatThreshold ?? ChargeThresholds.Get(left.ChapterNumber),
            LockOn: combat.LockOn,
            BurnStacks: combat.BurnStacks,
            BarrierStacks: combat.BarrierStacks,
            FusionStacks: combat.FusionStacks,
            Lanes: lanes,
            AvailableFormations: availableForms,
            ActiveFormationId: combat.ActiveFormationId,
            ActiveModifiers: modifiers,
            AwaitingReadyUp: combat.AwaitingReadyUp,
            IsComplete: combat.IsComplete,
            AvailableDeployables: availableDeps,
            Forecast: forecast,
            PanickedCrewIds: combat.PanickedCrewIdsThisPage,
            KidnappedCrewId: combat.KidnappedCrewId,
            IsPhantomPage: combat.IsPhantomPage,
            IsInvulnerablePage: combat.IsInvulnerablePage,
            IsCommitted_: r.IsCommitted_);
    }

    static EnemyCombatRender BuildEnemyCombatRender(CombatState combat)
    {
        var enemy = combat.Enemy;
        var intentsHidden = enemy.AbilityIds.Any(id =>
            EnemyAbilityData.Get(id)?.AbilityType == EnemyAbilityType.EnemyIntentsHidden);

        var weapons = enemy.WeaponIds.Select(id =>
        {
            var w = WeaponData.Get(id)!;
            return new WeaponRender(w.Id, w.Name, w.LeftEffect, w.CenterEffect, w.RightEffect, w.RandomiseLanes, w.SpecialNote ?? "");
        }).ToList();

        var abilities = enemy.AbilityIds.Select(id =>
        {
            var a = EnemyAbilityData.Get(id)!;
            return new EnemyAbilityRender(a.Id, a.Name, a.Description, true);
        }).ToList();

        var intents = intentsHidden ? [] : combat.GeneratedIntents.Select(i =>
        {
            var w = WeaponData.Get(i.WeaponId)!;
            return new GeneratedIntentRender(
                LaneIndex: i.LaneIndex,
                LaneName: LaneNames[i.LaneIndex],
                WeaponName: w.Name,
                EffectDescription: DescribeWeaponEffect(i.Effect),
                ResolvedValue: i.ResolvedValue);
        }).ToList();

        return new EnemyCombatRender(
            Name: enemy.Name,
            Weapons: weapons,
            Abilities: abilities,
            IsBoss: enemy.IsBoss,
            SuperShieldCurrent: combat.SuperShieldCurrent,
            Intents: intents,
            IntentsHidden: intentsHidden);
    }

    static string DescribeFormationPassive(FormationPassive p) => p.Type switch
    {
        FormationPassiveType.BrainformOverride => "Crew use Intelligence instead of Resolve",
        FormationPassiveType.SmileformOverride => "Crew use Charisma instead of Resolve",
        FormationPassiveType.CoreBonus => $"Boost {p.AbilityTypeFilter} abilities by {(int)(p.FloatValue * 100)}%",
        FormationPassiveType.PowerColumnBoost => $"Boost multiplier ×{p.FloatValue:F1}",
        FormationPassiveType.GraveyardBonus => $"Exhausted crew output ×{1f + p.FloatValue:F2}",
        _ => p.Type.ToString()
    };

    static string DescribeWeaponEffect(WeaponEffect e) => e.Type switch
    {
        WeaponEffectType.None => "—",
        WeaponEffectType.Threat => $"Threat {e.IntValue}",
        WeaponEffectType.PiercingThreat => $"Piercing Threat {e.IntValue}",
        WeaponEffectType.LockOnIncrease => $"Lock-On +{e.IntValue}",
        WeaponEffectType.LockOnDecrease => $"Lock-On −{e.IntValue}",
        WeaponEffectType.BurnStacks => $"Burn {e.IntValue}",
        WeaponEffectType.ChargeReduction => $"Charge −{e.IntValue}",
        WeaponEffectType.MoraleReduction => $"Morale −{e.IntValue}",
        WeaponEffectType.CounterblasterThreat => $"Counter Threat {e.IntValue}",
        WeaponEffectType.KillRandomCrew => "Kill random crew",
        _ => e.Type.ToString()
    };

    static List<LaneRender> BuildLaneRenders(LeftPageState left, CombatState combat, FormationInstance? formation)
    {
        var lanes = new List<LaneRender>();
        for (var li = 0; li < 3; li++)
        {
            var threat = combat.GeneratedIntents
                .Where(i => i.LaneIndex == li && i.Effect.Type == WeaponEffectType.Threat)
                .Sum(i => i.ResolvedValue);

            // Compute slot renders for this lane
            var slots = new List<SlotCombatRender>();
            if (formation != null)
            {
                var lane = (LaneId)li;
                var slotsInLane = formation.Slots
                    .Select((s, idx) => (s, idx))
                    .Where(t => t.s.ConnectedLanes.Contains(lane))
                    .ToList();

                foreach (var (s, idx) in slotsInLane)
                {
                    var slotted = combat.SlottedByLane[li].FirstOrDefault(sc => sc.SlotIndex == idx);
                    var crewId = slotted?.CrewId;
                    var crewName = crewId != null
                        ? left.Crew.FirstOrDefault(c => c.Id == crewId)?.Name : null;

                    var forecastText = BuildSlotForecastText(left, combat, formation, idx, lane);

                    slots.Add(new SlotCombatRender(
                        SlotIndex: idx,
                        Type: s.Type,
                        TypeName: s.Type.ToString(),
                        ConnectedLanes: s.ConnectedLanes.Select(l => l.ToString()).ToList(),
                        IsDouble: s.IsDouble,
                        SlottedCrewId: crewId,
                        SlottedCrewName: crewName,
                        ForecastText: forecastText));
                }
            }

            lanes.Add(new LaneRender(
                Name: LaneNames[li],
                LaneIndex: li,
                Threat: threat,
                Shield: 0,
                Hack: 0,
                Charge: 0,
                LockOn: combat.LockOn[li],
                BurnStacks: combat.BurnStacks[li],
                BarrierStacks: combat.BarrierStacks[li],
                FusionStacks: combat.FusionStacks[li],
                Slots: slots));
        }
        return lanes;
    }

    static string? BuildSlotForecastText(LeftPageState left, CombatState combat, FormationInstance formation, int slotIdx, LaneId lane)
    {
        var slot = formation.Slots[slotIdx];
        var slotted = combat.SlottedByLane[(int)lane].FirstOrDefault(sc => sc.SlotIndex == slotIdx);
        if (slotted == null) return null;
        var crew = left.Crew.FirstOrDefault(c => c.Id == slotted.CrewId);
        if (crew == null) return null;
        var ability = crew.Abilities.FirstOrDefault();
        if (ability == null) return null;
        var slotInst = new SlotInstance(slot.Type, slot.ConnectedLanes, slot.IsDouble, slot.ChainId);
        var output = EffectCalculator.Calculate(crew, ability, slotInst, lane, combat, left, formation);
        return FormatAbilityOutput(ability.Definition.Type, output);
    }

    static string FormatAbilityOutput(AbilityType type, AbilityOutput o) => type switch
    {
        AbilityType.Shield  => $"Shield {o.Shield}",
        AbilityType.Hack    => $"Hack {o.Hack}",
        AbilityType.Charge  => $"Charge {o.Charge}",
        AbilityType.Boost   => $"Boost +{o.BoostPct}%",
        AbilityType.Barrier => $"Barrier {o.Barrier}",
        AbilityType.Heal    => $"Heal {o.HullHeal}",
        AbilityType.Fusion  => $"Fusion {o.Fusion}",
        _ => ""
    };

    static List<FormationRender> BuildAvailableFormations(LeftPageState left, CombatState combat)
    {
        // Draw was determined in StartPage; use the drawn count from modifiers
        var count = CombatResolver.GetFormationDrawCount(left, combat);
        var avail = CombatResolver.DrawFormations(
            left.FormationArchive, count,
            new DeterministicRng(0, combat.PageNumber, 999),
            combat.LastFormationId,
            false, []);
        return avail.Select(f => BuildFormationRender(f, combat)).ToList();
    }

    static CombatForecast? BuildForecast(LeftPageState left, CombatState combat, FormationInstance? formation)
    {
        if (formation == null || !combat.AwaitingReadyUp) return null;

        var shieldByLane = new int[3];
        var threatByLane = new int[3];
        var hackByLane = new int[3];
        var chargeByLane = new int[3];
        var totalCharge = 0;
        var totalHullDamage = 0;
        var coinGain = 0;

        // Tally intents
        foreach (var intent in combat.GeneratedIntents)
        {
            if (intent.Effect.Type == WeaponEffectType.Threat)
                threatByLane[intent.LaneIndex] += intent.ResolvedValue;
        }

        // Tally crew outputs
        foreach (var crew in left.Crew.Where(c => !c.IsDead))
        {
            foreach (var slotted in combat.SlottedByLane.SelectMany(l => l).Where(sc => sc.CrewId == crew.Id))
            {
                var lane = slotted.EffectiveLanes.FirstOrDefault();
                var slot = formation.Slots[slotted.SlotIndex];
                var ability = crew.Abilities.FirstOrDefault();
                if (ability == null) continue;
                var slotInst = new SlotInstance(slot.Type, slot.ConnectedLanes, slot.IsDouble, slot.ChainId);

                foreach (var effectiveLane in slotted.EffectiveLanes)
                {
                    var output = EffectCalculator.Calculate(crew, ability, slotInst, effectiveLane, combat, left, formation);
                    shieldByLane[(int)effectiveLane] += output.Shield;
                    hackByLane[(int)effectiveLane] += output.Hack;
                    chargeByLane[(int)effectiveLane] += output.Charge;
                    totalCharge += output.Charge;
                }
            }
        }

        // Compute hull damage: threat - shield (per lane), min 0
        for (var i = 0; i < 3; i++)
        {
            var effective = Math.Max(0, threatByLane[i] - shieldByLane[i]);
            totalHullDamage += effective;
        }

        return new CombatForecast(
            ForecastShield: shieldByLane,
            ForecastThreat: threatByLane,
            ForecastHack: hackByLane,
            ForecastCharge: chargeByLane,
            ForecastChargeGain: totalCharge,
            ForecastHullDamage: totalHullDamage,
            ForecastCoinGain: coinGain);
    }

    // ─── Combat reward ────────────────────────────────────────────────────────

    static CombatRewardRightRender BuildCombatReward(LeftPageState left, CombatRewardRightPage r)
    {
        var formOpts = r.FormationOptions.Select(id =>
        {
            var def = FormationData.Get(id)!;
            var inst = RunFactory.ToInstance(def);
            return BuildFormationRender(inst, null);
        }).ToList();

        return new CombatRewardRightRender(
            FormationOptions: formOpts,
            SelectedFormationId: r.SelectedFormationId,
            CoinReward: r.CoinReward,
            IsCommitted_: r.IsCommitted_);
    }

    // ─── Chapter conclusion ───────────────────────────────────────────────────

    static ChapterConclusionRightRender BuildChapterConclusion(LeftPageState left, ChapterConclusionRightPage r)
    {
        var narrative = left.ChapterNumber switch
        {
            1 => "The first chapter closes. The crew breathes. But the void ahead stretches further still.",
            2 => "Two chapters survived. The ship groans, but holds. Onward.",
            3 => "The Gate Warden falls. The way opens. Act II awaits.",
            4 => "Deep in Act II now. Every page feels heavier than the last.",
            5 => "The middle of the journey. Half the story written in scars.",
            6 => "The Patience Engine is silent. Act III looms — the final pages.",
            7 => "Seven chapters. The crew is changed. Whatever waits ahead, they face it together.",
            8 => "The Collector defeated. One last obstacle remains.",
            9 => "Nine chapters. The final confrontation is at hand.",
            10 => "The Hyperion Shield shatters. The story is complete. You are free.",
            _ => $"Chapter {left.ChapterNumber} complete."
        };

        return new ChapterConclusionRightRender(
            NarrativeText: narrative,
            ChapterNumber: left.ChapterNumber,
            IsCommitted_: r.IsCommitted_);
    }

    // ─── Epilogue ─────────────────────────────────────────────────────────────

    static EpilogueRightRender BuildEpilogue(LeftPageState left, EpilogueRightPage r)
    {
        var final = r.FinalState;
        var survivors = final.Crew.Where(c => !c.IsDead).Select(c => c.Name).ToList();

        return new EpilogueRightRender(
            ChaptersCompleted: final.Stats.ChaptersCompleted,
            TotalDamageTaken: final.Stats.TotalDamageTaken,
            TotalShieldGenerated: final.Stats.TotalShieldGenerated,
            TotalChargeGenerated: final.Stats.TotalChargeGenerated,
            TotalHackGenerated: final.Stats.TotalHackGenerated,
            TotalCrewDeaths: final.Stats.TotalCrewDeaths,
            TotalTears: final.Stats.TotalTears,
            FinalHull: final.HullCurrent,
            FinalCoin: final.Coin,
            FinalMorale: final.Morale,
            SurvivorNames: survivors,
            IsCommitted_: r.IsCommitted_);
    }
}
