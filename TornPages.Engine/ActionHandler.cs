namespace TornPages.Engine;

public static class ActionHandler
{
    public static RunState Apply(RunState state, PlayerAction action)
    {
        var page = state.Pages[state.CurrentPageIndex];
        var left = page.Left;
        var right = page.Right;

        return action switch
        {
            TearAction => HandleTear(state),
            SelectPrologueCrewAttrAction a => HandlePrologueCrewAttr(state, a),
            ConfirmPrologueCrewAction => HandleConfirmPrologueCrew(state),
            BoonSelectAction a => HandleBoonSelect(state, a),
            ConfirmBoonAction => HandleConfirmBoon(state),
            SelectLocationAction a => HandleSelectLocation(state, a),
            ConfirmLocationAction => HandleConfirmLocation(state),
            SelectEventAction a => HandleSelectEvent(state, a),
            ReadyBuildUpAction => HandleReadyBuildUp(state),
            EventOptionSelectAction a => HandleEventOption(state, a),
            EventConfirmAction => HandleEventConfirm(state),
            EventContinueAction => HandleEventContinue(state),
            DismissCrewMemberAction a => HandleDismissCrew(state, a),
            ShopBuyAction a => HandleShopBuy(state, a),
            ShopExitAction => HandleShopExit(state),
            SelectWeaponAction a => HandleSelectWeapon(state, a),
            SelectEnemyAbilityAction a => HandleSelectEnemyAbility(state, a),
            ConfirmEnemyAction => HandleConfirmEnemy(state),
            SelectFormationAction a => HandleSelectFormation(state, a),
            SlotCrewAction a => HandleSlotCrew(state, a),
            UnslotCrewAction a => HandleUnslotCrew(state, a),
            ReadyUpAction => HandleReadyUp(state),
            UseDeployableAction a => HandleUseDeployable(state, a),
            SelectRewardAction a => HandleSelectReward(state, a),
            ConfirmRewardAction => HandleConfirmReward(state),
            ProceedAction => HandleProceed(state),
            MoveModToStorageAction a => HandleModToStorage(state, a),
            MoveModFromStorageAction a => HandleModFromStorage(state, a),
            _ => throw new EngineException($"Unknown action type: {action.GetType().Name}")
        };
    }

    // ─── Prologue Crew ────────────────────────────────────────────────────────

    static RunState HandlePrologueCrewAttr(RunState state, SelectPrologueCrewAttrAction action)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not PrologueCrewRightPage cp)
            throw new EngineException("Not on a prologue crew page.");

        var opts = cp.GeneratedOptions[0];
        var sel = cp.Selections ?? new CrewCreationSelections(-1, -1, -1, -1, -1, -1);
        sel = action.Attr switch
        {
            "name"      => sel with { NameIndex = action.OptionIndex },
            "race"      => sel with { RaceIndex = action.OptionIndex },
            "backstory" => sel with { BackstoryIndex = action.OptionIndex },
            "ability"   => sel with { AbilityIndex = action.OptionIndex },
            "stats"     => sel with { StatIndex = action.OptionIndex },
            "trait"     => sel with { TraitIndex = action.OptionIndex },
            _ => throw new EngineException($"Unknown attr: {action.Attr}")
        };

        return ReplaceCurrentPage(state, cp with { Selections = sel });
    }

    static RunState HandleConfirmPrologueCrew(RunState state)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not PrologueCrewRightPage cp)
            throw new EngineException("Not on a prologue crew page.");
        var sel = cp.Selections ?? throw new EngineException("No selections made.");
        if (sel.NameIndex < 0 || sel.RaceIndex < 0 || sel.BackstoryIndex < 0
            || sel.AbilityIndex < 0 || sel.StatIndex < 0 || sel.TraitIndex < 0)
            throw new EngineException("All crew attributes must be selected.");

        var committed = state with
        {
            Pages = ReplaceAt(state.Pages, idx, new PageState(state.Pages[idx].Left, cp with { IsCommitted_ = true }))
        };

        // Advance to next crew page or boon
        if (idx < 2)
            return committed with { CurrentPageIndex = idx + 1 };
        // All 3 crew confirmed — apply and advance to boon
        return RunFactory.ApplyAllCrewConfirmed(committed);
    }

    // ─── Prologue Boon ────────────────────────────────────────────────────────

    static RunState HandleBoonSelect(RunState state, BoonSelectAction action)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not PrologueBoonRightPage bp)
            throw new EngineException("Not on boon page.");
        return ReplaceCurrentPage(state, bp with
        {
            SelectedModId = action.ModId ?? bp.SelectedModId,
            SelectedFormationId = action.FormationId ?? bp.SelectedFormationId,
            SelectedDeployableId = action.DeployableId ?? bp.SelectedDeployableId,
        });
    }

    static RunState HandleConfirmBoon(RunState state)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not PrologueBoonRightPage bp)
            throw new EngineException("Not on boon page.");
        if (bp.SelectedModId == null || bp.SelectedFormationId == null || bp.SelectedDeployableId == null)
            throw new EngineException("All boons must be selected.");

        var left = state.Pages[idx].Left;

        // Apply mod
        var modDef = ModData.Get(bp.SelectedModId)!;
        var modInst = new ModInstance(Guid.NewGuid().ToString(), bp.SelectedModId);
        var sys = left.Systems.Get(modDef.System);
        var newSys = sys with { InstalledMods = [.. sys.InstalledMods, modInst] };
        left = left with { Systems = left.Systems.With(modDef.System, newSys) };

        // Apply formation
        var formDef = FormationData.Get(bp.SelectedFormationId)!;
        var formInst = RunFactory.ToInstance(formDef);
        var archive = left.FormationArchive with
        {
            Formations = [.. left.FormationArchive.Formations, formInst]
        };
        left = left with { FormationArchive = archive };

        // Apply deployable
        var depInst = new DeployableInstance(Guid.NewGuid().ToString(), bp.SelectedDeployableId);
        left = left with { Deployables = [.. left.Deployables, depInst] };

        left = left with { PagesRemaining = left.PagesRemaining - 1 };

        // Advance to Chapter 1 – LocationSelect
        return AdvanceToNextChapter(state, idx, left with { ChapterNumber = 1, ActNumber = 1 });
    }

    // ─── Location Select ──────────────────────────────────────────────────────

    static RunState HandleSelectLocation(RunState state, SelectLocationAction action)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not LocationSelectRightPage lp)
            throw new EngineException("Not on location select page.");
        if (!lp.LocationOptions.Contains(action.LocationId))
            throw new EngineException("Invalid location choice.");
        return ReplaceCurrentPage(state, lp with { SelectedLocationId = action.LocationId });
    }

    static RunState HandleConfirmLocation(RunState state)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not LocationSelectRightPage lp)
            throw new EngineException("Not on location select page.");
        if (lp.SelectedLocationId == null)
            throw new EngineException("No location selected.");

        var left = state.Pages[idx].Left with
        {
            CurrentLocationId = lp.SelectedLocationId,
            PagesRemaining = state.Pages[idx].Left.PagesRemaining - 1
        };

        // Advance to Build-Up
        return AdvanceToBuildUp(state, idx, left);
    }

    // ─── Build Up ─────────────────────────────────────────────────────────────

    static RunState AdvanceToBuildUp(RunState state, int fromIdx, LeftPageState left)
    {
        var rng = new DeterministicRng(state.Metadata.Seed, fromIdx + 1, 0);
        var pool = EventEngine.GenerateEventPool(left, rng, left.CurrentLocationId);
        var buPage = new BuildUpRightPage(pool, []);
        return AdvancePage(state, fromIdx, left, buPage);
    }

    static RunState HandleSelectEvent(RunState state, SelectEventAction action)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not BuildUpRightPage bu)
            throw new EngineException("Not on build-up page.");
        var selected = bu.SelectedEventIds.ToList();
        if (selected.Contains(action.EventId))
            selected.Remove(action.EventId);
        else
            selected.Add(action.EventId);
        return ReplaceCurrentPage(state, bu with { SelectedEventIds = selected });
    }

    static RunState HandleReadyBuildUp(RunState state)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not BuildUpRightPage bu)
            throw new EngineException("Not on build-up page.");
        if (bu.TotalDT < 4)
            throw new EngineException("Insufficient Dramatic Tension (need 4).");

        var left = state.Pages[idx].Left with { PagesRemaining = state.Pages[idx].Left.PagesRemaining - 1 };

        // Queue event pages
        var pages = state.Pages.ToList();
        // Commit build-up page
        pages[idx] = new PageState(state.Pages[idx].Left, bu with { IsCommitted_ = true });

        // Generate one EventRightPage for each selected event, in order
        var eventLeft = left;
        var nextIdx = idx + 1;
        foreach (var eventId in bu.SelectedEventIds)
        {
            var rng = new DeterministicRng(state.Metadata.Seed, nextIdx, 0);
            var payload = EventEngine.GeneratePayload(eventId, eventLeft, rng);
            var eventPage = new EventRightPage(eventId, payload, null, []);
            pages.Insert(nextIdx, new PageState(eventLeft, eventPage));
            nextIdx++;
        }

        // After events: EnemySelect page
        var rng2 = new DeterministicRng(state.Metadata.Seed, nextIdx, 0);
        var enemyPage = GenerateEnemySelectPage(eventLeft, state.Metadata.Seed, nextIdx, state.Pages[idx].Left.ChapterNumber);
        pages.Insert(nextIdx, new PageState(eventLeft, enemyPage));

        return state with
        {
            Pages = pages,
            CurrentPageIndex = idx + 1,
            Metadata = state.Metadata
        };
    }

    // ─── Events ───────────────────────────────────────────────────────────────

    static RunState HandleEventOption(RunState state, EventOptionSelectAction action)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not EventRightPage ep)
            throw new EngineException("Not on event page.");
        return ReplaceCurrentPage(state, ep with { PlayerChoice = action.OptionIndex });
    }

    static RunState HandleEventConfirm(RunState state)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not EventRightPage ep)
            throw new EngineException("Not on event page.");
        if (ep.PlayerChoice == null)
            throw new EngineException("No option selected.");

        var rng = new DeterministicRng(state.Metadata.Seed, idx, 1);
        var newLeft = EventEngine.ApplyEvent(ep.EventId, ep.Payload, ep.PlayerChoice, state.Pages[idx].Left, rng);
        newLeft = newLeft with { PagesRemaining = newLeft.PagesRemaining - 1 };

        // Check if crew gain event needs crew creation page insertion
        if (ep.EventId is "CREW_GAIN" or "LOC_DRIFT_BRITTLEBONE" or "LOC_MYCELIA_SILENT"
            or "LOC_SIGG_STEELBORN" or "LOC_ENJUKAN_REPLICANT" or "LOC_KHORUM_DWARF" or "LOC_KRAETHOS_SYMBIOTE")
        {
            // Crew creation is handled directly on this page
        }

        var committed = new PageState(state.Pages[idx].Left, ep with { IsCommitted_ = true });
        var pages = state.Pages.ToList();
        pages[idx] = committed;
        // Update left page of subsequent pages
        for (var i = idx + 1; i < pages.Count; i++)
            pages[i] = pages[i] with { Left = newLeft };

        return state with { Pages = pages, CurrentPageIndex = idx + 1 };
    }

    static RunState HandleEventContinue(RunState state)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not EventRightPage ep)
            throw new EngineException("Not on event page.");

        var rng = new DeterministicRng(state.Metadata.Seed, idx, 1);
        var newLeft = EventEngine.ApplyEvent(ep.EventId, ep.Payload, null, state.Pages[idx].Left, rng);
        newLeft = newLeft with { PagesRemaining = newLeft.PagesRemaining - 1 };

        var pages = state.Pages.ToList();
        pages[idx] = new PageState(state.Pages[idx].Left, ep with { IsCommitted_ = true });
        for (var i = idx + 1; i < pages.Count; i++)
            pages[i] = pages[i] with { Left = newLeft };

        return state with { Pages = pages, CurrentPageIndex = idx + 1 };
    }

    static RunState HandleDismissCrew(RunState state, DismissCrewMemberAction action)
    {
        var idx = state.CurrentPageIndex;
        var right = state.Pages[idx].Right;
        if (right is not EventRightPage ep)
            throw new EngineException("Crew dismissal only valid on event pages.");

        var crew = state.Pages[idx].Left.Crew.ToList();
        var target = crew.FirstOrDefault(c => c.Id == action.CrewId)
            ?? throw new EngineException("Crew member not found.");
        crew.Remove(target);
        var newLeft = state.Pages[idx].Left with { Crew = crew };
        var pages = ReplaceAt(state.Pages, idx, new PageState(newLeft, ep with { DismissedCrewIds = [.. ep.DismissedCrewIds, action.CrewId] }));
        return state with { Pages = pages };
    }

    // ─── Shop ─────────────────────────────────────────────────────────────────

    static RunState HandleShopBuy(RunState state, ShopBuyAction action)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not EventRightPage ep)
            throw new EngineException("Not on shop page.");
        var left = state.Pages[idx].Left;
        if (left.Coin < action.Cost)
            throw new EngineException("Insufficient coin.");

        var newLeft = left with { Coin = left.Coin - action.Cost };
        // Apply shop purchase effect
        var itemId = action.ItemId;
        if (ModData.Get(itemId) != null)
            newLeft = EventEngine.ApplyEvent("MOD_GAIN",
                new EventPayload(null, null, null, null, null, itemId, null, null, null, null, null, null, null, null, null),
                null, newLeft, new DeterministicRng(0));
        else if (DeployableData.Get(itemId) != null)
            newLeft = newLeft with { Deployables = [.. newLeft.Deployables, new DeployableInstance(Guid.NewGuid().ToString(), itemId)] };
        else if (itemId.StartsWith("heal_"))
        {
            var healAmt = int.Parse(itemId.Split('_')[1]);
            newLeft = newLeft with { HullCurrent = Math.Min(newLeft.HullMax, newLeft.HullCurrent + healAmt) };
        }
        else if (itemId.StartsWith("morale_"))
        {
            var pct = int.Parse(itemId.Split('_')[1]);
            newLeft = newLeft with { Morale = Math.Min(100, newLeft.Morale + pct) };
        }
        else if (itemId.StartsWith("upgrade_"))
        {
            var sysName = itemId.Split('_')[1];
            var sysType = Enum.Parse<SystemType>(sysName);
            newLeft = EventEngine.ApplyEvent($"SYSTEM_UPGRADE_{sysName.ToUpper()}",
                new EventPayload(null, null, null, null, null, null, null, null, null, sysName, null, null, null, null, null),
                null, newLeft, new DeterministicRng(0));
        }
        else if (itemId.StartsWith("sabotage_"))
        {
            var modTypeName = itemId.Replace("sabotage_", "");
            if (Enum.TryParse<CombatModifierType>(modTypeName, out var modType))
            {
                var mod = new CombatModifier(modType, null, true);
                newLeft = newLeft with { PendingCombatModifiers = [.. newLeft.PendingCombatModifiers, mod] };
            }
        }

        var pages = ReplaceAt(state.Pages, idx, new PageState(newLeft, ep));
        // Propagate left to future pages
        var pagesList = pages.ToList();
        for (var i = idx + 1; i < pagesList.Count; i++)
            pagesList[i] = pagesList[i] with { Left = newLeft };
        return state with { Pages = pagesList };
    }

    static RunState HandleShopExit(RunState state)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not EventRightPage ep)
            throw new EngineException("Not on shop page.");
        var newLeft = state.Pages[idx].Left with { PagesRemaining = state.Pages[idx].Left.PagesRemaining - 1 };
        var pages = state.Pages.ToList();
        pages[idx] = new PageState(state.Pages[idx].Left, ep with { IsCommitted_ = true });
        for (var i = idx + 1; i < pages.Count; i++)
            pages[i] = pages[i] with { Left = newLeft };
        return state with { Pages = pages, CurrentPageIndex = idx + 1 };
    }

    // ─── Enemy Select ─────────────────────────────────────────────────────────

    static EnemySelectRightPage GenerateEnemySelectPage(LeftPageState left, int seed, int pageIndex, int chapter)
    {
        var rng = new DeterministicRng(seed, pageIndex, 0);
        var isBoss = ChargeThresholds.IsBoss(chapter);
        var bossId = isBoss ? GetBossId(chapter) : null;

        if (isBoss)
        {
            // Boss: no weapon selection, no ability selection
            return new EnemySelectRightPage([], [], null, [], [], true, bossId);
        }

        // Normal: 2 locked + 3 choices for third weapon
        var allWeapons = WeaponData.All.ToList();
        var shuffled = rng.Shuffle(allWeapons);
        var locked = shuffled.Take(2).Select(w => w.Id).ToList();
        var rest = shuffled.Skip(2).Take(3).Select(w => w.Id).ToList();

        // 3 ability options
        var abilityPool = rng.Sample(EnemyAbilityData.All, 3).Select(a => a.Id).ToList();

        return new EnemySelectRightPage(locked, rest, null, abilityPool, [], false, null);
    }

    static string GetBossId(int chapter) => chapter switch
    {
        3 => "GateWarden", 6 => "PatienceEngine", 9 => "TheCollector", 10 => "HyperionShield", _ => "Unknown"
    };

    static RunState HandleSelectWeapon(RunState state, SelectWeaponAction action)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not EnemySelectRightPage ep)
            throw new EngineException("Not on enemy select page.");
        if (!ep.WeaponChoicePool.Contains(action.WeaponId))
            throw new EngineException("Invalid weapon choice.");
        return ReplaceCurrentPage(state, ep with { SelectedWeaponId = action.WeaponId });
    }

    static RunState HandleSelectEnemyAbility(RunState state, SelectEnemyAbilityAction action)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not EnemySelectRightPage ep)
            throw new EngineException("Not on enemy select page.");
        var selected = ep.SelectedAbilityIds.ToList();
        if (selected.Contains(action.AbilityId))
            selected.Remove(action.AbilityId);
        else if (selected.Count < 3)
            selected.Add(action.AbilityId);
        return ReplaceCurrentPage(state, ep with { SelectedAbilityIds = selected });
    }

    static RunState HandleConfirmEnemy(RunState state)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not EnemySelectRightPage ep)
            throw new EngineException("Not on enemy select page.");
        if (!ep.IsBoss && ep.SelectedWeaponId == null)
            throw new EngineException("Must select a weapon.");
        if (ep.SelectedAbilityIds.Count == 0 && !ep.IsBoss)
            throw new EngineException("Must select at least one ability.");

        var left = state.Pages[idx].Left;

        // Build weapon list
        List<string> weaponIds;
        if (ep.IsBoss)
            weaponIds = GetBossWeapons(ep.BossId ?? "GateWarden");
        else
            weaponIds = [.. ep.LockedWeaponIds, ep.SelectedWeaponId!];

        // Check glitch: all enemies get extra ability
        var allMods = EffectCalculator.GetAllEquippedMods(left).ToList();
        var abilityIds = ep.SelectedAbilityIds.ToList();
        if (allMods.Any(m => m.EffectType == ModEffectType.GlitchAllEnemiesExtraAbility))
        {
            var rng = new DeterministicRng(state.Metadata.Seed, idx, 5);
            var extra = rng.Pick(EnemyAbilityData.All.Where(a => !abilityIds.Contains(a.Id)).ToList());
            abilityIds.Add(extra.Id);
        }

        var enemyName = ep.IsBoss ? GetBossName(ep.BossId!) : "Enemy Vessel";
        var enemy = new EnemyState(enemyName, weaponIds, abilityIds, ep.IsBoss, ep.BossId);

        // Coin reward based on ability count
        var coinReward = ep.SelectedAbilityIds.Count switch { 1 => 40, 2 => 65, 3 => 95, _ => 40 };
        var rng2 = new DeterministicRng(state.Metadata.Seed, idx, 1);

        // Combat modifiers: glitch start with lock-on
        var startModifiers = left.PendingCombatModifiers.ToList();
        if (allMods.Any(m => m.EffectType == ModEffectType.GlitchStartOneLockOnAll))
            startModifiers.Add(new CombatModifier(CombatModifierType.PlusOneLockOnAllLanes));

        var initialLockOn = new int[3];
        foreach (var mod in startModifiers)
            if (mod.Type == CombatModifierType.PlusOneLockOnAllLanes)
                for (var i = 0; i < 3; i++) initialLockOn[i] = Math.Min(5, initialLockOn[i] + 1);

        // Initial burn from combat modifier
        var initialBurn = new int[3];
        foreach (var mod in startModifiers)
            if (mod.Type == CombatModifierType.PlusTwoBurnAllLanes)
                for (var i = 0; i < 3; i++) initialBurn[i] += 2;

        var initialCombat = new CombatState() with
        {
            Enemy = enemy,
            ActiveModifiers = startModifiers,
            LockOn = initialLockOn,
            BurnStacks = initialBurn,
            IntentsUsedThisCombat = []
        };

        // Generate first page intents
        var rng3 = new DeterministicRng(state.Metadata.Seed, idx + 1, 0);
        initialCombat = CombatResolver.StartPage(initialCombat, left, rng3);

        var chargeThreshold = ChargeThresholds.Get(left.ChapterNumber);
        var updatedLeft = left with
        {
            PagesRemaining = left.PagesRemaining - 1,
            PendingCombatThreshold = chargeThreshold,
            PendingCombatModifiers = []
        };

        var committed = ep with { IsCommitted_ = true };
        var pages = state.Pages.ToList();
        pages[idx] = new PageState(state.Pages[idx].Left, committed);

        // Insert combat page
        var combatPage = new CombatRightPage(initialCombat);
        pages.Insert(idx + 1, new PageState(updatedLeft, combatPage));
        for (var i = idx + 2; i < pages.Count; i++)
            pages[i] = pages[i] with { Left = updatedLeft };

        return state with { Pages = pages, CurrentPageIndex = idx + 1 };
    }

    static List<string> GetBossWeapons(string bossId) => bossId switch
    {
        "GateWarden" => ["LockOn", "BroadBeam", "PanicRay"],
        "PatienceEngine" => ["Incinerator", "Repulsor", "BroadBurn"],
        "TheCollector" => ["LongTermThreatener", "PredictableBlast", "Counterblaster"],
        "HyperionShield" => ["LeftBeam", "MiddleBeam", "RightBeam"],
        _ => ["BroadBeam", "LockOn", "BroadBurn"]
    };

    static string GetBossName(string bossId) => bossId switch
    {
        "GateWarden" => "The Gate Warden",
        "PatienceEngine" => "The Patience Engine",
        "TheCollector" => "The Collector",
        "HyperionShield" => "Hyperion Shield Core",
        _ => "Unknown Boss"
    };

    // ─── Combat ───────────────────────────────────────────────────────────────

    static RunState HandleSelectFormation(RunState state, SelectFormationAction action)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not CombatRightPage cp)
            throw new EngineException("Not on combat page.");
        var formation = state.Pages[idx].Left.FormationArchive.Formations
            .FirstOrDefault(f => f.FormationId == action.FormationId)
            ?? throw new EngineException("Formation not found.");
        return ReplaceCurrentPage(state, cp with
        {
            Combat = cp.Combat with
            {
                ActiveFormationId = action.FormationId,
                SlottedByLane = [[], [], []]  // unslot all when formation changes
            }
        });
    }

    static RunState HandleSlotCrew(RunState state, SlotCrewAction action)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not CombatRightPage cp)
            throw new EngineException("Not on combat page.");

        var left = state.Pages[idx].Left;
        var combat = cp.Combat;
        var crew = left.Crew.FirstOrDefault(c => c.Id == action.CrewId)
            ?? throw new EngineException("Crew not found.");

        if (crew.IsDead) throw new EngineException("Cannot slot dead crew.");
        if (combat.PanickedCrewIdsThisPage.Contains(crew.Id)) throw new EngineException("Crew is panicked.");
        if (combat.KidnappedCrewId == crew.Id) throw new EngineException("Crew is kidnapped.");

        if (combat.ActiveFormationId == null) throw new EngineException("No formation selected.");
        var formation = left.FormationArchive.Formations.First(f => f.FormationId == combat.ActiveFormationId);
        if (action.SlotIndex >= formation.Slots.Count) throw new EngineException("Invalid slot index.");

        // Fatigue check
        var useCount = combat.SlotUseCountThisPage.GetValueOrDefault(crew.Id, 0) + 1;
        var effectiveFatigue = crew.FatigueBaseline + useCount;
        if (effectiveFatigue > crew.Stats.Endurance)
            throw new EngineException("Crew member is exhausted.");

        var slot = formation.Slots[action.SlotIndex];
        var effectiveLanes = GetEffectiveLanes(crew, slot).ToList();

        // Build updated slotted structure
        var slottedByLane = combat.SlottedByLane.Select(l => l.ToList()).ToList();
        foreach (var lane in effectiveLanes)
        {
            var existing = slottedByLane[(int)lane].Find(sc => sc.SlotIndex == action.SlotIndex);
            if (existing != null) slottedByLane[(int)lane].Remove(existing);
            slottedByLane[(int)lane].Add(new SlottedCrew(crew.Id, action.SlotIndex, effectiveLanes));
        }

        var newUseCount = new Dictionary<string, int>(combat.SlotUseCountThisPage)
        {
            [crew.Id] = useCount
        };

        return ReplaceCurrentPage(state, cp with
        {
            Combat = combat with { SlottedByLane = slottedByLane, SlotUseCountThisPage = newUseCount }
        });
    }

    static RunState HandleUnslotCrew(RunState state, UnslotCrewAction action)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not CombatRightPage cp)
            throw new EngineException("Not on combat page.");

        var slottedByLane = cp.Combat.SlottedByLane.Select(l => l.ToList()).ToList();
        string? unslottedCrewId = null;
        for (var laneIdx = 0; laneIdx < 3; laneIdx++)
        {
            var entry = slottedByLane[laneIdx].Find(sc => sc.SlotIndex == action.SlotIndex);
            if (entry != null)
            {
                unslottedCrewId = entry.CrewId;
                slottedByLane[laneIdx].Remove(entry);
            }
        }

        if (unslottedCrewId == null) return state; // nothing to unslot

        // Decrement use count
        var newUseCount = new Dictionary<string, int>(cp.Combat.SlotUseCountThisPage);
        if (newUseCount.TryGetValue(unslottedCrewId, out var count) && count > 0)
            newUseCount[unslottedCrewId] = count - 1;

        return ReplaceCurrentPage(state, cp with
        {
            Combat = cp.Combat with { SlottedByLane = slottedByLane, SlotUseCountThisPage = newUseCount }
        });
    }

    static RunState HandleReadyUp(RunState state)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not CombatRightPage cp)
            throw new EngineException("Not on combat page.");

        var left = state.Pages[idx].Left;
        var rng = new DeterministicRng(state.Metadata.Seed, idx, 10);

        var result = CombatResolver.Resolve(cp.Combat, left, rng);

        var newLeft = result.Left with { PagesRemaining = result.Left.PagesRemaining - 1 };

        if (result.RunFailed)
        {
            // Run over
            var failLeft = newLeft with { IsRunOver = true };
            var pages2 = ReplaceAt(state.Pages, idx, new PageState(left, cp with { Combat = result.Combat with { IsComplete = true } }));
            return state with { Pages = pages2.ToList(), Metadata = state.Metadata with { Phase = RunPhase.Ended } };
        }

        if (result.CombatComplete)
        {
            // Advance to CombatReward
            var updatedCombat = result.Combat with { IsComplete = true };
            var pages2 = state.Pages.ToList();
            pages2[idx] = new PageState(left, cp with { Combat = updatedCombat, IsCommitted_ = true });

            // Generate reward page
            var rng2 = new DeterministicRng(state.Metadata.Seed, idx + 1, 0);
            var rewardForms = DrawRewardFormations(left.FormationArchive, rng2, 3, left);
            var abilityCount = cp.Combat.Enemy.AbilityIds.Count;
            var coinReward = abilityCount switch { 1 => rng2.Next(30, 51), 2 => rng2.Next(50, 81), _ => rng2.Next(80, 111) };
            var rewardPage = new CombatRewardRightPage(rewardForms.Select(f => f.FormationId).ToList(), null, coinReward, []);
            pages2.Insert(idx + 1, new PageState(newLeft, rewardPage));
            return state with { Pages = pages2, CurrentPageIndex = idx + 1 };
        }
        else
        {
            // Generate next combat page
            var pages2 = state.Pages.ToList();
            pages2[idx] = new PageState(left, cp with { Combat = result.Combat, IsCommitted_ = true });

            var rng2 = new DeterministicRng(state.Metadata.Seed, idx + 1, 0);
            var nextCombat = CombatResolver.StartPage(result.Combat, newLeft, rng2);
            var nextPage = new CombatRightPage(nextCombat);
            pages2.Insert(idx + 1, new PageState(newLeft, nextPage));
            return state with { Pages = pages2, CurrentPageIndex = idx + 1 };
        }
    }

    static List<FormationInstance> DrawRewardFormations(FormationArchive archive, DeterministicRng rng, int count, LeftPageState left)
    {
        var allNonStarting = FormationData.All.Where(f => f.Rarity != FormationRarity.Starting).ToList();
        var allMods = EffectCalculator.GetAllEquippedMods(left).ToList();
        var rareWeight = allMods.Any(m => m.EffectType == ModEffectType.RareFormationsTwice) ? 20 : 10;

        var weighted = allNonStarting.Select(f => (f, f.Rarity switch
        {
            FormationRarity.Common => 70,
            FormationRarity.Uncommon => 20,
            FormationRarity.Rare => rareWeight,
            _ => 0
        })).ToList();

        var results = new List<FormationInstance>();
        for (var i = 0; i < count; i++)
        {
            var chosen = rng.PickWeighted(weighted);
            results.Add(RunFactory.ToInstance(chosen));
        }
        return results;
    }

    static RunState HandleUseDeployable(RunState state, UseDeployableAction action)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not CombatRightPage cp)
            throw new EngineException("Not in combat.");

        // Check deployables disabled
        if (cp.Combat.ActiveModifiers.Any(m => m.Type == CombatModifierType.DeployablesDisabled)
            || cp.Combat.Enemy.AbilityIds.Contains("DeployDisabled"))
            throw new EngineException("Deployables are disabled this combat.");

        var left = state.Pages[idx].Left;
        var depInst = left.Deployables.FirstOrDefault(d => d.InstanceId == action.DeployableInstanceId)
            ?? throw new EngineException("Deployable not found.");

        if (cp.Combat.DeployablesUsedThisPage.Contains(action.DeployableInstanceId))
            throw new EngineException("Deployable already used.");

        // Apply deployable effect to combat state
        var (newCombat, newLeft) = ApplyDeployable(cp.Combat, left, depInst, state.Metadata.Seed, idx);

        // Mark as used
        newCombat = newCombat with
        {
            DeployablesUsedThisPage = [.. newCombat.DeployablesUsedThisPage, action.DeployableInstanceId]
        };
        newLeft = newLeft with
        {
            Deployables = newLeft.Deployables.Where(d => d.InstanceId != action.DeployableInstanceId).ToList()
        };

        var pages = ReplaceAt(state.Pages, idx, new PageState(newLeft, cp with { Combat = newCombat }));
        var pagesList = pages.ToList();
        for (var i = idx + 1; i < pagesList.Count; i++)
            pagesList[i] = pagesList[i] with { Left = newLeft };
        return state with { Pages = pagesList };
    }

    static (CombatState, LeftPageState) ApplyDeployable(
        CombatState combat, LeftPageState left, DeployableInstance dep, int seed, int pageIdx)
    {
        var rng = new DeterministicRng(seed, pageIdx, 99);
        switch (dep.Definition.EffectType)
        {
            case DeployableEffectType.LoseTenPercentMoraleInvulnerable:
                return (combat with { IsInvulnerablePage = true },
                    left with { Morale = Math.Max(20, left.Morale - 10) });
            case DeployableEffectType.HealTwentyFivePercent:
                return (combat, left with { HullCurrent = Math.Min(left.HullMax, left.HullCurrent + left.HullMax / 4) });
            case DeployableEffectType.BoostHundredAllLanes:
                // Add combat modifier: Boost 100% (simplified as large boost)
                return (combat with { ActiveModifiers = [.. combat.ActiveModifiers, new CombatModifier(CombatModifierType.BoostRandomSlotEachPage)] }, left);
            case DeployableEffectType.UnexhaustAllCrewZeroFatigue:
                {
                    var crew = left.Crew.Select(c => c with { FatigueBaseline = 0 }).ToList();
                    return (combat, left with { Crew = crew });
                }
            case DeployableEffectType.GainTwentyPercentMorale:
                return (combat, left with { Morale = Math.Min(100, left.Morale + 20) });
            case DeployableEffectType.ReduceAllLockOnToZero:
                return (combat with { LockOn = [0, 0, 0] }, left);
            case DeployableEffectType.IncreaseCoinThirtyPercent:
                return (combat, left with { Coin = (int)(left.Coin * 1.3f) });
            case DeployableEffectType.ChargeThirty:
                return (combat with { ChargeAccumulated = combat.ChargeAccumulated + 30 }, left);
            case DeployableEffectType.CureNegativeTraits:
                {
                    var crew = left.Crew.Select(c =>
                        c with { Traits = c.Traits.Where(t => !t.Definition.IsNegative).ToList() }).ToList();
                    return (combat, left with { Crew = crew });
                }
            case DeployableEffectType.DestroyRandomGlitch:
                return (combat, EventEngine.ApplyEvent("LOC_MERKAJ_GLITCH",
                    new EventPayload(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null),
                    null, left, rng));
            case DeployableEffectType.EscapeNonBossCombat:
                {
                    if (combat.Enemy.IsBoss) throw new EngineException("Cannot escape boss combat.");
                    var crew2 = left.Crew.Select(c => c with { Stats = c.Stats.AddRes(-1) }).ToList();
                    return (combat with { IsComplete = true }, left with { Crew = crew2 });
                }
            default:
                return (combat, left);
        }
    }

    // ─── Combat Reward ────────────────────────────────────────────────────────

    static RunState HandleSelectReward(RunState state, SelectRewardAction action)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not CombatRewardRightPage rp)
            throw new EngineException("Not on reward page.");
        return ReplaceCurrentPage(state, rp with { SelectedFormationId = action.FormationId });
    }

    static RunState HandleConfirmReward(RunState state)
    {
        var idx = state.CurrentPageIndex;
        if (state.Pages[idx].Right is not CombatRewardRightPage rp)
            throw new EngineException("Not on reward page.");
        if (rp.SelectedFormationId == null)
            throw new EngineException("No formation selected.");

        var left = state.Pages[idx].Left;

        // Add formation to archive
        var formDef = FormationData.Get(rp.SelectedFormationId)!;
        var formInst = RunFactory.ToInstance(formDef);
        var archive = left.FormationArchive with { Formations = [.. left.FormationArchive.Formations, formInst] };

        // Add coin reward
        var newLeft = left with
        {
            FormationArchive = archive,
            Coin = left.Coin + rp.CoinReward,
            PagesRemaining = left.PagesRemaining - 1,
            Stats = left.Stats with { TotalCombats = left.Stats.TotalCombats + 1 }
        };

        // Mod: escape heals hull
        var allMods = EffectCalculator.GetAllEquippedMods(left).ToList();
        if (allMods.Any(m => m.EffectType == ModEffectType.EscapeHealTen))
            newLeft = newLeft with { HullCurrent = Math.Min(newLeft.HullMax, (int)(newLeft.HullCurrent + newLeft.HullMax * 0.1f)) };
        if (allMods.Any(m => m.EffectType == ModEffectType.EscapeMoraleUp))
            newLeft = newLeft with { Morale = Math.Min(100, newLeft.Morale + 10) };
        if (allMods.Any(m => m.EffectType == ModEffectType.EscapeInterestOnCoin))
            newLeft = newLeft with { Coin = newLeft.Coin + (int)(newLeft.Coin * 0.1f) };
        if (allMods.Any(m => m.EffectType == ModEffectType.EscapeGainDeployable))
        {
            var rng2 = new DeterministicRng(state.Metadata.Seed, idx, 50);
            var newDep = new DeployableInstance(Guid.NewGuid().ToString(), rng2.Pick(DeployableData.All).Id);
            newLeft = newLeft with { Deployables = [.. newLeft.Deployables, newDep] };
        }
        if (allMods.Any(m => m.EffectType == ModEffectType.CrewHealFullAfterCombat))
        {
            var crew = newLeft.Crew.Select(c => c with { HpCurrent = c.HpMax }).ToList();
            newLeft = newLeft with { Crew = crew };
        }

        var pages = state.Pages.ToList();
        pages[idx] = new PageState(state.Pages[idx].Left, rp with { IsCommitted_ = true });

        // Insert chapter conclusion page
        var conclusionPage = new ChapterConclusionRightPage();
        pages.Insert(idx + 1, new PageState(newLeft, conclusionPage));
        for (var i = idx + 2; i < pages.Count; i++)
            pages[i] = pages[i] with { Left = newLeft };

        return state with { Pages = pages, CurrentPageIndex = idx + 1 };
    }

    // ─── Chapter Conclusion / Proceed ─────────────────────────────────────────

    static RunState HandleProceed(RunState state)
    {
        var idx = state.CurrentPageIndex;
        var right = state.Pages[idx].Right;

        if (right is ChapterConclusionRightPage)
        {
            var left = state.Pages[idx].Left;
            var newLeft = left with
            {
                PagesRemaining = left.PagesRemaining - 1,
                Stats = left.Stats with { ChaptersCompleted = left.Stats.ChaptersCompleted + 1 }
            };

            var pages = state.Pages.ToList();
            pages[idx] = new PageState(left, new ChapterConclusionRightPage(true));

            if (left.ChapterNumber >= 10)
            {
                // Victory!
                newLeft = newLeft with { IsVictory = true };
                var epiloguePage = new EpilogueRightPage(newLeft);
                pages.Insert(idx + 1, new PageState(newLeft, epiloguePage));
                return state with
                {
                    Pages = pages,
                    CurrentPageIndex = idx + 1,
                    Metadata = state.Metadata with { Phase = RunPhase.Ended }
                };
            }

            // Next chapter
            var nextChapter = left.ChapterNumber + 1;
            var nextAct = nextChapter switch { <= 3 => 1, <= 6 => 2, <= 9 => 3, _ => 4 };
            newLeft = newLeft with { ChapterNumber = nextChapter, ActNumber = nextAct };

            return AdvanceToNextChapter(state with { Pages = pages }, idx, newLeft);
        }

        throw new EngineException($"Proceed not valid on page type: {right?.GetType().Name}");
    }

    // ─── Tear ─────────────────────────────────────────────────────────────────

    static RunState HandleTear(RunState state)
    {
        var idx = state.CurrentPageIndex;
        var page = state.Pages[idx];
        if (!page.Right.TearingAllowed)
            throw new EngineException("Tearing is not allowed on this page.");

        var left = page.Left with { PagesRemaining = page.Left.PagesRemaining - 1 };
        if (left.PagesRemaining <= 0)
            throw new EngineException("No pages remaining.");

        // Regenerate the right page with a different seed
        var tearSeed = state.Metadata.Seed ^ (idx * 2 + 1);
        var rng = new DeterministicRng(tearSeed, idx, 0);
        RightPageState newRight = RegeneratePage(page.Right, left, tearSeed, idx, state);

        var pages = ReplaceAt(state.Pages, idx, new PageState(left, newRight));
        return state with
        {
            Pages = pages,
            Metadata = state.Metadata with { RngCounter = state.Metadata.RngCounter + 1 }
        };
    }

    static RightPageState RegeneratePage(RightPageState right, LeftPageState left, int seed, int idx, RunState state)
    {
        var rng = new DeterministicRng(seed, idx, 0);
        switch (right)
        {
            case BuildUpRightPage bu:
            {
                var newPool = EventEngine.GenerateEventPool(left, rng, left.CurrentLocationId);
                if (newPool.SequenceEqual(bu.EventPool) && newPool.Count > 3)
                    newPool = EventEngine.GenerateEventPool(left, new DeterministicRng(seed + 1, idx, 0), left.CurrentLocationId);
                return bu with { EventPool = newPool, SelectedEventIds = [] };
            }
            case EventRightPage ep:
            {
                var newPayload = EventEngine.GeneratePayload(ep.EventId, left, rng);
                return ep with { Payload = newPayload, PlayerChoice = null };
            }
            case EnemySelectRightPage:
                return GenerateEnemySelectPage(left, seed, idx, left.ChapterNumber);
            case CombatRightPage cp:
            {
                var rng2 = new DeterministicRng(seed, idx + 1, 0);
                var newCombat = CombatResolver.StartPage(cp.Combat with { SlottedByLane = [[], [], []], ActiveFormationId = null }, left, rng2);
                return cp with { Combat = newCombat };
            }
            case LocationSelectRightPage lp:
            {
                var newOpts = GenerateLocationOptions(left, rng, 2);
                return lp with { LocationOptions = newOpts, SelectedLocationId = null };
            }
            default:
                return right;
        }
    }

    // ─── Mod Storage ──────────────────────────────────────────────────────────

    static RunState HandleModToStorage(RunState state, MoveModToStorageAction action)
    {
        var idx = state.CurrentPageIndex;
        var left = state.Pages[idx].Left;
        ModInstance? found = null;
        SystemType? fromSystem = null;

        foreach (var sysType in new[] { SystemType.Engine, SystemType.Cabin, SystemType.Hull, SystemType.Computers })
        {
            var sys = left.Systems.Get(sysType);
            var mod = sys.InstalledMods.FirstOrDefault(m => m.InstanceId == action.ModInstanceId);
            if (mod != null) { found = mod; fromSystem = sysType; break; }
        }
        if (found == null) throw new EngineException("Mod not found.");

        var oldSys = left.Systems.Get(fromSystem!.Value);
        var newSys = oldSys with { InstalledMods = oldSys.InstalledMods.Where(m => m.InstanceId != action.ModInstanceId).ToList() };
        var newLeft = left with
        {
            Systems = left.Systems.With(fromSystem.Value, newSys),
            ModStorage = [.. left.ModStorage, found]
        };
        return UpdateLeftAtPage(state, idx, newLeft);
    }

    static RunState HandleModFromStorage(RunState state, MoveModFromStorageAction action)
    {
        var idx = state.CurrentPageIndex;
        var left = state.Pages[idx].Left;
        var mod = left.ModStorage.FirstOrDefault(m => m.InstanceId == action.ModInstanceId)
            ?? throw new EngineException("Mod not in storage.");
        var system = left.Systems.Get(action.System);
        if (system.SlotsAvailable < (int)mod.Definition.Size + 1)
            throw new EngineException("Insufficient mod slots.");

        var newStorage = left.ModStorage.Where(m => m.InstanceId != action.ModInstanceId).ToList();
        var newSys = system with { InstalledMods = [.. system.InstalledMods, mod] };
        var newLeft = left with
        {
            ModStorage = newStorage,
            Systems = left.Systems.With(action.System, newSys)
        };
        return UpdateLeftAtPage(state, idx, newLeft);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static RunState AdvanceToNextChapter(RunState state, int fromIdx, LeftPageState left)
    {
        var rng = new DeterministicRng(state.Metadata.Seed, fromIdx + 1, 0);
        var locOptions = GenerateLocationOptions(left, rng, 2);
        var locPage = new LocationSelectRightPage(locOptions, null);
        return AdvancePage(state, fromIdx, left, locPage);
    }

    static List<string> GenerateLocationOptions(LeftPageState left, DeterministicRng rng, int count)
    {
        var all = LocationData.All.Select(l => l.Id).ToList();
        return rng.Sample(all, count);
    }

    static RunState AdvancePage(RunState state, int fromIdx, LeftPageState newLeft, RightPageState newRight)
    {
        var pages = state.Pages.ToList();
        if (fromIdx + 1 < pages.Count)
        {
            pages[fromIdx + 1] = new PageState(newLeft, newRight);
            for (var i = fromIdx + 2; i < pages.Count; i++)
                pages[i] = pages[i] with { Left = newLeft };
        }
        else
            pages.Add(new PageState(newLeft, newRight));
        return state with { Pages = pages, CurrentPageIndex = fromIdx + 1 };
    }

    static RunState ReplaceCurrentPage(RunState state, RightPageState newRight)
    {
        var pages = ReplaceAt(state.Pages, state.CurrentPageIndex,
            new PageState(state.Pages[state.CurrentPageIndex].Left, newRight));
        return state with { Pages = pages };
    }

    static RunState UpdateLeftAtPage(RunState state, int idx, LeftPageState newLeft)
    {
        var pages = state.Pages.ToList();
        pages[idx] = new PageState(newLeft, pages[idx].Right);
        for (var i = idx + 1; i < pages.Count; i++)
            pages[i] = pages[i] with { Left = newLeft };
        return state with { Pages = pages };
    }

    static List<T> ReplaceAt<T>(IEnumerable<T> list, int index, T replacement)
    {
        var result = list.ToList();
        result[index] = replacement;
        return result;
    }

    static IEnumerable<LaneId> GetEffectiveLanes(CrewMemberState crew, SlotInstance slot)
    {
        var lanes = new HashSet<LaneId>(slot.ConnectedLanes);
        if (crew.Traits.Any(t => t.Definition.EffectType == TraitEffectType.AlwaysConnectedLeft)) lanes.Add(LaneId.Left);
        if (crew.Traits.Any(t => t.Definition.EffectType == TraitEffectType.AlwaysConnectedCenter)) lanes.Add(LaneId.Center);
        if (crew.Traits.Any(t => t.Definition.EffectType == TraitEffectType.AlwaysConnectedRight)) lanes.Add(LaneId.Right);
        return lanes;
    }
}
