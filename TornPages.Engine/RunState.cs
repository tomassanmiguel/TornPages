using System.Text.Json.Serialization;

namespace TornPages.Engine;

// ─────────────────────────────────────────────────────────────────────────────
//  Top-level run state
// ─────────────────────────────────────────────────────────────────────────────

public record RunState(
    string Id,
    RunMetadata Metadata,
    List<PageState> Pages,
    int CurrentPageIndex);

public record RunMetadata(
    string ProfileId,
    int Seed,
    DifficultyLevel Difficulty,
    RunPhase Phase,
    int RngCounter = 0);

// ─────────────────────────────────────────────────────────────────────────────
//  Page
// ─────────────────────────────────────────────────────────────────────────────

public record PageState(LeftPageState Left, RightPageState Right);

// ─────────────────────────────────────────────────────────────────────────────
//  Persistent state snapshot (immutable once page is committed)
// ─────────────────────────────────────────────────────────────────────────────

public record LeftPageState(
    int HullCurrent,
    int HullMax,
    int Coin,
    int Morale,                          // 20–100
    List<CrewMemberState> Crew,
    FormationArchive FormationArchive,
    ShipSystems Systems,
    List<ModInstance> ModStorage,
    List<DeployableInstance> Deployables,
    int PagesRemaining,
    int ChapterNumber,
    int ActNumber,
    RunStats Stats,
    string? CurrentLocationId,
    int? PendingCombatThreshold,         // set when entering combat
    List<CombatModifier> PendingCombatModifiers, // from events
    bool IsRunOver = false,
    bool IsVictory = false);

// ─────────────────────────────────────────────────────────────────────────────
//  Crew
// ─────────────────────────────────────────────────────────────────────────────

public record CrewMemberState(
    string Id,
    string Name,
    Race Race,
    string Backstory,
    StatBlock Stats,
    int HpCurrent,
    int HpMax,
    List<TraitInstance> Traits,
    List<AbilityInstance> Abilities,
    int PermanentEffectLevelBonus,
    bool IsDead,
    int FatigueBaseline,         // fatigue at start of current page (after prior-page recovery)
    bool IsPanicked = false,     // clears at start of each new combat page
    int CombatElBonus = 0,       // resets between combats (for "battle-hardened" etc.)
    int TimesUsedThisCombat = 0  // resets between combats (for "flow state" etc.)
);

public record StatBlock(int Resolve, int Intelligence, int Charisma, int Endurance)
{
    public StatBlock AddRes(int v) => this with { Resolve = Math.Max(0, Resolve + v) };
    public StatBlock AddInt(int v) => this with { Intelligence = Math.Max(0, Intelligence + v) };
    public StatBlock AddCha(int v) => this with { Charisma = Math.Max(0, Charisma + v) };
    public StatBlock AddEnd(int v) => this with { Endurance = Math.Max(0, Endurance + v) };
    public int Total => Resolve + Intelligence + Charisma + Endurance;
}

public record TraitInstance(string TraitId, int AccumulatedBonus = 0)
{
    [JsonIgnore]
    public TraitDefinition Definition => TraitData.Get(TraitId)!;
}

public record AbilityInstance(string AbilityId, bool IsDual)
{
    [JsonIgnore]
    public AbilityDefinition Definition => AbilityData.Get(AbilityId)!;
    public float EffectMultiplier => IsDual ? 0.5f : 1f;
}

// ─────────────────────────────────────────────────────────────────────────────
//  Formation archive
// ─────────────────────────────────────────────────────────────────────────────

public record FormationArchive(
    List<FormationInstance> Formations,
    string? LastUsedFormationId = null);

public record FormationInstance(string FormationId, List<SlotInstance> Slots, List<SlotInstance> OriginalSlots)
{
    [JsonIgnore]
    public FormationDefinition Definition => FormationData.Get(FormationId)!;
}

public record SlotInstance(SlotType Type, List<LaneId> ConnectedLanes, bool IsDouble = false, string? ChainId = null);

// ─────────────────────────────────────────────────────────────────────────────
//  Ship systems
// ─────────────────────────────────────────────────────────────────────────────

public record ShipSystems(
    SystemState Engine,
    SystemState Cabin,
    SystemState Hull,
    SystemState Computers)
{
    public SystemState Get(SystemType type) => type switch
    {
        SystemType.Engine => Engine,
        SystemType.Cabin => Cabin,
        SystemType.Hull => Hull,
        SystemType.Computers => Computers,
        _ => throw new ArgumentOutOfRangeException()
    };

    public ShipSystems With(SystemType type, SystemState state) => type switch
    {
        SystemType.Engine => this with { Engine = state },
        SystemType.Cabin => this with { Cabin = state },
        SystemType.Hull => this with { Hull = state },
        SystemType.Computers => this with { Computers = state },
        _ => throw new ArgumentOutOfRangeException()
    };
}

public record SystemState(int Level, List<ModInstance> InstalledMods)
{
    public int ModSlots => SystemValues.ModSlots(Level);
    public int SlotsUsed => InstalledMods.Sum(m => (int)m.Definition.Size + 1);
    public int SlotsAvailable => ModSlots - SlotsUsed;
}

public record ModInstance(string InstanceId, string ModId)
{
    [JsonIgnore]
    public ModDefinition Definition => ModData.Get(ModId)!;
}

public record DeployableInstance(string InstanceId, string DeployableId)
{
    [JsonIgnore]
    public DeployableDefinition Definition => DeployableData.Get(DeployableId)!;
}

// ─────────────────────────────────────────────────────────────────────────────
//  Combat modifier (from events/shops/enemy abilities)
// ─────────────────────────────────────────────────────────────────────────────

public record CombatModifier(CombatModifierType Type, int? Value = null, bool IsPositive = true)
{
    [JsonIgnore]
    public string DisplayName => Type switch
    {
        CombatModifierType.InvulnerabilityFirstPage => "Invulnerable (first page)",
        CombatModifierType.PreventFirstDamage => "Prevent first damage",
        CombatModifierType.CrewPlusOneEl => "Crew +1 Effect Level",
        CombatModifierType.DrawPlusOneFormation => "Draw +1 Formation",
        CombatModifierType.BoostRandomSlotEachPage => "Boost random slot each page",
        CombatModifierType.HackTwoAllLanes => "Hack 2 all lanes each page",
        CombatModifierType.LowerChargeThresholdTenPercent => "Charge threshold −10%",
        CombatModifierType.EmptySlotsShieldOne => "Empty slots shield 1",
        CombatModifierType.EmptySlotsChargeOne => "Empty slots charge 1",
        CombatModifierType.CrewPlusOneEnd => "Crew +1 Endurance",
        CombatModifierType.PlusOneLockOnAllLanes => "+1 lock-on all lanes",
        CombatModifierType.PlusTwoBurnAllLanes => "+2 burn all lanes",
        CombatModifierType.CrewMinusOneEl => "Crew −1 Effect Level",
        CombatModifierType.DrawMinusOneFormation => "Draw −1 Formation",
        CombatModifierType.PlusPainSlotEachPage => "+1 pain slot each page",
        CombatModifierType.DeployablesDisabled => "Deployables disabled",
        CombatModifierType.RandomCrewPanicsEachPage => "Random crew panics each page",
        CombatModifierType.RaiseChargeThresholdTenPercent => "Charge threshold +10%",
        CombatModifierType.PlusOneLockOnRandomLaneEachPage => "+1 lock-on random lane each page",
        CombatModifierType.EmptySlotsThreattwo => "Empty slots: Threat 2",
        _ => Type.ToString()
    };
}

// ─────────────────────────────────────────────────────────────────────────────
//  Run statistics
// ─────────────────────────────────────────────────────────────────────────────

public record RunStats(
    int TotalChargeGenerated = 0,
    int TotalHackGenerated = 0,
    int TotalShieldGenerated = 0,
    int TotalDamageTaken = 0,
    int TotalCrewDeaths = 0,
    int TotalTears = 0,
    int TotalCombats = 0,
    int ChaptersCompleted = 0);

// ─────────────────────────────────────────────────────────────────────────────
//  Right-page state hierarchy (polymorphic, JSON-serialisable)
// ─────────────────────────────────────────────────────────────────────────────

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(PrologueCrewRightPage),      "PrologueCrew")]
[JsonDerivedType(typeof(PrologueBoonRightPage),      "PrologueBoon")]
[JsonDerivedType(typeof(LocationSelectRightPage),    "LocationSelect")]
[JsonDerivedType(typeof(BuildUpRightPage),           "BuildUp")]
[JsonDerivedType(typeof(EventRightPage),             "Event")]
[JsonDerivedType(typeof(EnemySelectRightPage),       "EnemySelect")]
[JsonDerivedType(typeof(CombatRightPage),            "Combat")]
[JsonDerivedType(typeof(CombatRewardRightPage),      "CombatReward")]
[JsonDerivedType(typeof(ChapterConclusionRightPage), "ChapterConclusion")]
[JsonDerivedType(typeof(EpilogueRightPage),          "Epilogue")]
public abstract record RightPageState
{
    public abstract bool IsCommitted { get; }
    public abstract PageType PageType { get; }
    public virtual bool TearingAllowed => true;
}

// ─── Prologue ─────────────────────────────────────────────────────────────────

public record PrologueCrewRightPage(
    int CrewIndex,                   // 0=shield, 1=charge, 2=hack
    List<CrewCreationOptions> GeneratedOptions,
    CrewCreationSelections? Selections,
    bool IsCommitted_ = false) : RightPageState
{
    public override bool IsCommitted => IsCommitted_;
    public override PageType PageType => PageType.PrologueCrew;
}

public record CrewCreationOptions(
    List<string> NameOptions,
    List<Race> RaceOptions,
    List<string> BackstoryOptions,
    List<AbilityDefinition> AbilityOptions,
    List<StatBlock> StatOptions,
    List<TraitDefinition?> TraitOptions   // null = no trait
);

public record CrewCreationSelections(
    int NameIndex,
    int RaceIndex,
    int BackstoryIndex,
    int AbilityIndex,
    int StatIndex,
    int TraitIndex);

public record PrologueBoonRightPage(
    List<ModDefinition> ModOptions,
    List<FormationDefinition> FormationOptions,
    List<DeployableDefinition> DeployableOptions,
    string? SelectedModId,
    string? SelectedFormationId,
    string? SelectedDeployableId,
    bool IsCommitted_ = false) : RightPageState
{
    public override bool IsCommitted => IsCommitted_;
    public override PageType PageType => PageType.PrologueBoon;
}

// ─── Chapter ──────────────────────────────────────────────────────────────────

public record LocationSelectRightPage(
    List<string> LocationOptions,    // IDs
    string? SelectedLocationId,
    bool IsCommitted_ = false) : RightPageState
{
    public override bool IsCommitted => IsCommitted_;
    public override PageType PageType => PageType.LocationSelect;
}

public record BuildUpRightPage(
    List<string> EventPool,           // event IDs
    List<string> SelectedEventIds,
    bool IsCommitted_ = false) : RightPageState
{
    public override bool IsCommitted => IsCommitted_;
    public override PageType PageType => PageType.BuildUp;
    public int TotalDT => SelectedEventIds
        .Sum(id => EventData.Get(id)?.DramaticTension ?? 0);
}

public record EventRightPage(
    string EventId,
    EventPayload Payload,
    object? PlayerChoice,        // typed per event
    List<string> DismissedCrewIds,
    bool IsCommitted_ = false) : RightPageState
{
    public override bool IsCommitted => IsCommitted_;
    public override PageType PageType => PageType.Event;
    public override bool TearingAllowed => EventData.Get(EventId)?.TearingAllowed ?? true;
}

// Holds generated sub-options for an event
public record EventPayload(
    int? CoinAmount,
    int? DamageAmount,
    int? HealAmount,
    ShopType? ShopType,
    List<string>? ShopItemIds,   // mod/deployable IDs
    string? ModId,
    string? DeployableId,
    string? TraitId,
    List<string>? FormationOptions,
    string? SystemUpgradeTarget,
    CombatModifier? CombatModifier,
    string? StatTrainingStat,
    int? MoraleChange,
    int? HullMaxChange,
    string? FixedRace          // for location crew events
);

public record EnemySelectRightPage(
    List<string> LockedWeaponIds,        // 2 locked weapons
    List<string> WeaponChoicePool,       // 3 options for third weapon
    string? SelectedWeaponId,
    List<string> AbilityPool,            // 3 options
    List<string> SelectedAbilityIds,
    bool IsBoss,
    string? BossId,
    bool IsCommitted_ = false) : RightPageState
{
    public override bool IsCommitted => IsCommitted_;
    public override PageType PageType => PageType.EnemySelect;
}

public record ChapterConclusionRightPage(bool IsCommitted_ = false) : RightPageState
{
    public override bool IsCommitted => IsCommitted_;
    public override PageType PageType => PageType.ChapterConclusion;
    public override bool TearingAllowed => false;
}

public record EpilogueRightPage(LeftPageState FinalState, bool IsCommitted_ = false) : RightPageState
{
    public override bool IsCommitted => IsCommitted_;
    public override PageType PageType => PageType.Epilogue;
    public override bool TearingAllowed => false;
}

// ─────────────────────────────────────────────────────────────────────────────
//  Combat right page
// ─────────────────────────────────────────────────────────────────────────────

public record CombatRightPage(
    CombatState Combat,
    bool IsCommitted_ = false) : RightPageState
{
    public override bool IsCommitted => IsCommitted_;
    public override PageType PageType => PageType.Combat;
}

public record CombatRewardRightPage(
    List<string> FormationOptions,   // IDs
    string? SelectedFormationId,
    int CoinReward,
    List<ModStorageMove> ModMoves,
    bool IsCommitted_ = false) : RightPageState
{
    public override bool IsCommitted => IsCommitted_;
    public override PageType PageType => PageType.CombatReward;
}

public record ModStorageMove(string ModInstanceId, SystemType? TargetSystem, int? TargetSlotIndex, bool ToStorage);

// ─────────────────────────────────────────────────────────────────────────────
//  Combat State
// ─────────────────────────────────────────────────────────────────────────────

public record CombatState(
    int PageNumber,
    int ChargeAccumulated,
    int[] LockOn,                  // [Left, Center, Right]
    int[] BurnStacks,
    int[] BarrierStacks,
    int[] FusionStacks,
    float[] PersistentBoostModifier,  // Act III boss debuff
    EnemyState Enemy,
    string? ActiveFormationId,
    string? LastFormationId,
    List<List<SlottedCrew>> SlottedByLane,  // [Left][Center][Right]
    List<CombatModifier> ActiveModifiers,
    bool IsPhantomPage,
    bool IsInvulnerablePage,
    List<GeneratedIntent> GeneratedIntents,
    Dictionary<string, int> SlotUseCountThisPage,
    List<string> DeployablesUsedThisPage,
    List<string> PanickedCrewIdsThisPage,
    string? KidnappedCrewId,
    bool IsComplete,
    bool AwaitingReadyUp,
    List<CombatLogEntry> EventLog,
    // Boss-specific tracking
    bool BossPhase2 = false,
    int BossPanicCount = 0,
    bool BossInvulnNextPage = false,
    bool BossFullLockOnReached = false,
    int? SuperShieldCurrent = null,
    int FadingCanonUseCount = 0,
    bool FirstDamagePrevented = false,
    List<string> IntentsUsedThisCombat = null!  // for no-repeat tracking
)
{
    public CombatState() : this(0, 0, [0, 0, 0], [0, 0, 0], [0, 0, 0], [0, 0, 0],
        [0f, 0f, 0f], null!, null, null,
        [[], [], []], [], false, false, [], [], [], [], null, false, false, [], false, 0, false, false, null, 0, false, []) { }
}

public record EnemyState(
    string Name,
    List<string> WeaponIds,          // always 3
    List<string> AbilityIds,
    bool IsBoss,
    string? BossId = null);

public record SlottedCrew(string CrewId, int SlotIndex, List<LaneId> EffectiveLanes)
{
    [JsonIgnore]
    public bool IsValid => CrewId != null;
}

public record GeneratedIntent(
    int PageNumber,
    int LaneIndex,           // 0=Left,1=Center,2=Right
    string WeaponId,
    WeaponEffect Effect,
    int ResolvedValue);       // computed with lock-on scaling

public record CombatLogEntry(string Message, int PageNumber);
