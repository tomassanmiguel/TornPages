namespace TornPages.Engine;

public static class RunFactory
{
    public static RunState CreateNewRun(string profileId, int seed, DifficultyLevel difficulty)
    {
        var rng = new DeterministicRng(seed, 0, 0);

        var startingLeft = new LeftPageState(
            HullCurrent: 80,
            HullMax: 80,
            Coin: 100,
            Morale: 100,
            Crew: [],
            FormationArchive: new FormationArchive(
                FormationData.StartingFour.Select(f => ToInstance(f)).ToList()),
            Systems: new ShipSystems(
                Engine: new SystemState(0, []),
                Cabin: new SystemState(0, []),
                Hull: new SystemState(0, []),
                Computers: new SystemState(0, [])),
            ModStorage: [],
            Deployables: [],
            PagesRemaining: 200,
            ChapterNumber: 0,
            ActNumber: 0,
            Stats: new RunStats(),
            CurrentLocationId: null,
            PendingCombatThreshold: null,
            PendingCombatModifiers: []);

        // Page 0: Generate prologue crew pages (3 crew)
        var crew0Page = GeneratePrologueCrewPage(startingLeft, 0, seed, 0, AbilityType.Shield);
        var crew1Page = GeneratePrologueCrewPage(startingLeft, 0, seed, 1, AbilityType.Charge);
        var crew2Page = GeneratePrologueCrewPage(startingLeft, 0, seed, 2, AbilityType.Hack);

        var runState = new RunState(
            Id: Guid.NewGuid().ToString(),
            Metadata: new RunMetadata(profileId, seed, difficulty, RunPhase.Prologue),
            Pages: [
                new PageState(startingLeft, crew0Page),
                new PageState(startingLeft, crew1Page),
                new PageState(startingLeft, crew2Page),
            ],
            CurrentPageIndex: 0);

        return runState;
    }

    public static PrologueCrewRightPage GeneratePrologueCrewPage(
        LeftPageState left, int pageIndex, int seed, int subSeed, AbilityType targetAbility)
    {
        var rng = new DeterministicRng(seed, pageIndex, subSeed + 100);
        var opts = GenerateCrewOptions(rng, left.ChapterNumber, targetAbility, null);
        return new PrologueCrewRightPage(subSeed, [opts], null);
    }

    public static CrewCreationOptions GenerateCrewOptions(
        DeterministicRng rng, int chapter, AbilityType? forcedAbility, Race? forcedRace)
    {
        // Names: 3 random first+last combos
        var names = Enumerable.Range(0, 3)
            .Select(_ => $"{rng.Pick(NamePool.FirstNames)} {rng.Pick(NamePool.LastNames)}")
            .Distinct().ToList();
        while (names.Count < 3)
            names.Add($"{rng.Pick(NamePool.FirstNames)} {rng.Pick(NamePool.LastNames)}");

        // Races: 3 options
        var races = forcedRace.HasValue
            ? Enumerable.Repeat(forcedRace.Value, 3).ToList()
            : rng.Sample((IReadOnlyList<Race>)Enum.GetValues<Race>(), 3)
                .Take(3).ToList();
        while (races.Count < 3) races.Add(rng.Pick((IReadOnlyList<Race>)Enum.GetValues<Race>()));

        // Backstories: 3 options matching race[0], [1], [2]
        var backstories = races.Select(r =>
            rng.Pick(NamePool.Backstories.GetValueOrDefault(r, ["A wandering spacer."]))).ToList();

        // Abilities: 3 options (respecting rarity weights)
        var abilities = Enumerable.Range(0, 3)
            .Select(_ => GenerateOneAbility(rng, forcedAbility))
            .ToList();

        // Stat blocks: level = 8 + chapter, distribute across Res/Int/Cha (min 1 each), Endurance = 1
        var level = 8 + chapter;
        var stats = Enumerable.Range(0, 3).Select(_ =>
        {
            // Reserve 1 point per stat so no stat is 0, then distribute the rest randomly
            var dist = rng.DistributePoints(level - 3, 3);
            return new StatBlock(dist[0] + 1, dist[1] + 1, dist[2] + 1, 1);
        }).ToList();

        // Traits: 3 options (1 = no trait option)
        var traitOpts = new List<TraitDefinition?> { null };
        var positiveTraits = rng.Sample(TraitData.PositiveTraits, 2);
        traitOpts.AddRange(positiveTraits);
        while (traitOpts.Count < 3) traitOpts.Add(null);

        return new CrewCreationOptions(names, races, backstories, abilities, stats, traitOpts);
    }

    static AbilityDefinition GenerateOneAbility(DeterministicRng rng, AbilityType? forced)
    {
        if (forced.HasValue) return AbilityData.Get(forced.Value);
        // 30% chance of being dual (stored on instance, not here)
        var isRare = rng.Chance(0.25);
        if (isRare) return rng.Pick(AbilityData.Rare);
        return rng.Pick(AbilityData.Common);
    }

    public static FormationInstance ToInstance(FormationDefinition def)
    {
        var slots = def.Slots.Select(s => new SlotInstance(
            s.Type,
            s.ConnectedLanes.ToList(),
            s.IsDouble,
            s.ChainId)).ToList();
        return new FormationInstance(def.Id, slots, slots.Select(s => s with { }).ToList());
    }

    // After all 3 crew pages are confirmed, apply them and move to boon
    public static RunState ApplyAllCrewConfirmed(RunState state)
    {
        // Collect confirmed crew members from pages 0,1,2
        var crew = new List<CrewMemberState>();
        var left = state.Pages[0].Left;

        for (var i = 0; i < 3; i++)
        {
            if (state.Pages[i].Right is PrologueCrewRightPage cp && cp.Selections != null)
            {
                var opts = cp.GeneratedOptions[0];
                var sel = cp.Selections;
                var race = opts.RaceOptions[sel.RaceIndex];
                var raceDef = RaceData.Get(race);
                var stats = opts.StatOptions[sel.StatIndex];
                var stat = stats with
                {
                    Resolve = stats.Resolve + raceDef.BonusRes,
                    Intelligence = stats.Intelligence + raceDef.BonusInt,
                    Charisma = stats.Charisma + raceDef.BonusCha,
                    Endurance = stats.Endurance + raceDef.BonusEnd
                };
                var ability = opts.AbilityOptions[sel.AbilityIndex];
                // 30% dual: generate fresh for each crew member based on their index
                var rng = new DeterministicRng(state.Metadata.Seed, i, 200);
                var isDual = rng.Chance(0.30);

                TraitDefinition? trait = opts.TraitOptions[sel.TraitIndex];
                var traits = trait != null
                    ? new List<TraitInstance> { new(trait.Id) }
                    : new List<TraitInstance>();

                // Apply trait stat changes
                if (trait?.EffectType != null)
                    stat = ApplyTraitStats(stat, trait.EffectType.Value);
                // Apply negative trait stat changes
                if (trait?.NegativeEffectType != null)
                    stat = ApplyNegativeTraitStats(stat, trait.NegativeEffectType.Value);

                var maxHp = raceDef.BaseHp;
                if (trait?.EffectType == TraitEffectType.SixMaxHp) maxHp = 6;
                if (trait?.NegativeEffectType == NegativeTraitEffectType.TwoMaxHp) maxHp = 2;

                crew.Add(new CrewMemberState(
                    Id: Guid.NewGuid().ToString(),
                    Name: opts.NameOptions[sel.NameIndex],
                    Race: race,
                    Backstory: opts.BackstoryOptions[sel.BackstoryIndex],
                    Stats: stat,
                    HpCurrent: maxHp,
                    HpMax: maxHp,
                    Traits: traits,
                    Abilities: [new AbilityInstance(ability.Id, isDual)],
                    PermanentEffectLevelBonus: 0,
                    IsDead: false,
                    FatigueBaseline: 0));
            }
        }

        // Build boon page
        var boonRng = new DeterministicRng(state.Metadata.Seed, 3, 0);
        var modOptions = boonRng.Sample(
            ModData.NonGlitches.Where(m => m.Size == ModSize.S).ToList(), 3);
        var formOptions = boonRng.Sample(
            (IReadOnlyList<FormationDefinition>)FormationData.GetByRarity(FormationRarity.Common), 3);
        var deployOptions = boonRng.Sample(DeployableData.All, 3);

        var boonPage = new PrologueBoonRightPage(
            modOptions, formOptions, deployOptions, null, null, null);

        var updatedLeft = left with { Crew = crew };
        var boonPageState = new PageState(updatedLeft, boonPage);

        // Replace pages: keep indices 0-2 as committed, add boon page at index 3
        var pages = state.Pages.Take(3).ToList();
        pages.Add(boonPageState);

        return state with
        {
            Pages = pages,
            CurrentPageIndex = 3,
            Metadata = state.Metadata with { Phase = RunPhase.Prologue }
        };
    }

    static StatBlock ApplyTraitStats(StatBlock s, TraitEffectType t) => t switch
    {
        TraitEffectType.PlusTwoIntMinusOneCha => s with { Intelligence = s.Intelligence + 2, Charisma = Math.Max(1, s.Charisma - 1) },
        TraitEffectType.PlusTwoChaMinusOneInt => s with { Charisma = s.Charisma + 2, Intelligence = Math.Max(1, s.Intelligence - 1) },
        TraitEffectType.PlusTwoResMinusOneInt => s with { Resolve = s.Resolve + 2, Intelligence = Math.Max(1, s.Intelligence - 1) },
        TraitEffectType.PlusTwoIntMinusOneRes => s with { Intelligence = s.Intelligence + 2, Resolve = Math.Max(1, s.Resolve - 1) },
        TraitEffectType.PlusTwoChaMinusOneRes => s with { Charisma = s.Charisma + 2, Resolve = Math.Max(1, s.Resolve - 1) },
        TraitEffectType.PlusTwoResMinusOneCha => s with { Resolve = s.Resolve + 2, Charisma = Math.Max(1, s.Charisma - 1) },
        TraitEffectType.PlusOneInt => s with { Intelligence = s.Intelligence + 1 },
        TraitEffectType.PlusOneCha => s with { Charisma = s.Charisma + 1 },
        TraitEffectType.PlusOneRes => s with { Resolve = s.Resolve + 1 },
        TraitEffectType.PlusOneEndNoBoosted => s with { Endurance = s.Endurance + 1 },
        TraitEffectType.PlusFiveResMinusOneEnd => s with { Resolve = s.Resolve + 5, Endurance = Math.Max(1, s.Endurance - 1) },
        TraitEffectType.PlusTwoResTwentyFivePanic => s with { Resolve = s.Resolve + 2 },
        TraitEffectType.PlusOneEndMinusThreeRes => s with { Endurance = s.Endurance + 1, Resolve = Math.Max(1, s.Resolve - 3) },
        TraitEffectType.PlusTwoResRandomizeEffect => s with { Resolve = s.Resolve + 2 },
        _ => s
    };

    static StatBlock ApplyNegativeTraitStats(StatBlock s, NegativeTraitEffectType t) => t switch
    {
        NegativeTraitEffectType.MinusOneRes => s with { Resolve = Math.Max(1, s.Resolve - 1) },
        NegativeTraitEffectType.MinusOneCha => s with { Charisma = Math.Max(1, s.Charisma - 1) },
        NegativeTraitEffectType.MinusOneInt => s with { Intelligence = Math.Max(1, s.Intelligence - 1) },
        NegativeTraitEffectType.MinusOneEndPlusTwoRes => s with { Endurance = Math.Max(1, s.Endurance - 1), Resolve = s.Resolve + 2 },
        _ => s
    };
}
