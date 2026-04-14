using System.Text.Json.Serialization;

namespace TornPages.Engine;

// ─────────────────────────────────────────────────────────────────────────────
//  Top-level render state (view model for React)
// ─────────────────────────────────────────────────────────────────────────────

public record RenderState(
    LeftPageRender Left,
    RightPageRender Right,
    Dictionary<string, object?> Deltas,
    bool IsCurrentPage,
    List<string> PendingInterjections,
    int CurrentPageIndex,
    int TotalPages,
    RunPhase RunPhase,
    bool IsVictory = false,
    bool IsRunOver = false);

// ─────────────────────────────────────────────────────────────────────────────
//  Left page render model
// ─────────────────────────────────────────────────────────────────────────────

public record LeftPageRender(
    int HullCurrent,
    int HullMax,
    int Coin,
    int Morale,
    int PagesRemaining,
    int ChapterNumber,
    int ActNumber,
    List<CrewRender> Crew,
    List<FormationRender> Formations,
    string? LastUsedFormationId,
    SystemsRender Systems,
    List<ModRender> ModStorage,
    List<DeployableRender> Deployables,
    RunStats Stats);

public record CrewRender(
    string Id,
    string Name,
    string RaceName,
    Race Race,
    StatBlock Stats,
    int HpCurrent,
    int HpMax,
    int FatigueBaseline,
    int Endurance,
    bool IsDead,
    bool IsPanicked,
    bool IsExhausted,
    List<AbilityRender> Abilities,
    List<TraitRender> Traits,
    string Backstory,
    int EffectiveEffectLevel,
    int PermanentBonus);

public record AbilityRender(string Id, string TypeName, AbilityType Type, bool IsDual, string DisplayText);
public record TraitRender(string Id, string Name, string Description, bool IsNegative);

public record FormationRender(
    string Id,
    string Name,
    FormationRarity Rarity,
    List<SlotRender> Slots,
    string? PassiveDescription,
    bool IsGreyedOut);

public record SlotRender(
    SlotType Type,
    string TypeName,
    List<string> ConnectedLanes,
    bool IsDouble,
    string? ChainId,
    string? SlottedCrewId);

public record SystemsRender(
    SystemRender Engine,
    SystemRender Cabin,
    SystemRender Hull,
    SystemRender Computers,
    int TotalCharisma);

public record SystemRender(
    string Name,
    SystemType Type,
    int Level,
    int ModSlots,
    int SlotsUsed,
    string PrimaryValueLabel,
    int PrimaryValue,
    List<ModRender> InstalledMods);

public record ModRender(string InstanceId, string ModId, string Name, string Description, ModSize Size, bool IsGlitch, SystemType System);
public record DeployableRender(string InstanceId, string DeployableId, string Name, string Description);

// ─────────────────────────────────────────────────────────────────────────────
//  Right page render models (polymorphic)
// ─────────────────────────────────────────────────────────────────────────────

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(PrologueCrewRightRender),      "PrologueCrew")]
[JsonDerivedType(typeof(PrologueBoonRightRender),      "PrologueBoon")]
[JsonDerivedType(typeof(LocationSelectRightRender),    "LocationSelect")]
[JsonDerivedType(typeof(BuildUpRightRender),           "BuildUp")]
[JsonDerivedType(typeof(EventRightRender),             "Event")]
[JsonDerivedType(typeof(ShopRightRender),              "Shop")]
[JsonDerivedType(typeof(EnemySelectRightRender),       "EnemySelect")]
[JsonDerivedType(typeof(CombatRightRender),            "Combat")]
[JsonDerivedType(typeof(CombatRewardRightRender),      "CombatReward")]
[JsonDerivedType(typeof(ChapterConclusionRightRender), "ChapterConclusion")]
[JsonDerivedType(typeof(EpilogueRightRender),          "Epilogue")]
public abstract record RightPageRender
{
    public abstract PageType PageType { get; }
    public virtual bool TearingAllowed => true;
    public virtual bool IsCommitted => false;
}

public record PrologueCrewRightRender(
    int CrewIndex,                         // 0=shield,1=charge,2=hack
    AbilityType TargetAbility,
    CrewCreationOptions Options,
    CrewCreationSelections? Selections,
    bool IsCommitted_) : RightPageRender
{
    public override PageType PageType => PageType.PrologueCrew;
    public override bool IsCommitted => IsCommitted_;
}

public record PrologueBoonRightRender(
    List<ModRender> ModOptions,
    List<FormationRender> FormationOptions,
    List<DeployableRender> DeployableOptions,
    string? SelectedModId,
    string? SelectedFormationId,
    string? SelectedDeployableId,
    bool IsCommitted_) : RightPageRender
{
    public override PageType PageType => PageType.PrologueBoon;
    public override bool IsCommitted => IsCommitted_;
}

public record LocationSelectRightRender(
    List<LocationRender> Options,
    string? SelectedLocationId,
    bool IsCommitted_) : RightPageRender
{
    public override PageType PageType => PageType.LocationSelect;
    public override bool IsCommitted => IsCommitted_;
}

public record LocationRender(string Id, string Name, string LoreDescription, string? SpecialEventName);

public record BuildUpRightRender(
    List<EventCardRender> EventPool,
    List<string> SelectedEventIds,
    int TotalDT,
    int DtThreshold,
    bool CanReady,
    bool IsCommitted_) : RightPageRender
{
    public override PageType PageType => PageType.BuildUp;
    public override bool IsCommitted => IsCommitted_;
}

public record EventCardRender(string Id, string Name, int DramaticTension, string EffectSummary, bool TearingAllowed, EventStyle Style);

public record EventRightRender(
    string EventId,
    string EventName,
    EventStyle Style,
    string NarrativeText,
    string EffectDescription,
    List<string>? OptionLabels,
    int? SelectedOption,
    ShopRightRender? ShopData,
    bool NeedsCrewDismissal,
    bool IsCommitted_,
    bool TearingAllowed_) : RightPageRender
{
    public override PageType PageType => PageType.Event;
    public override bool IsCommitted => IsCommitted_;
    public override bool TearingAllowed => TearingAllowed_;
}

public record ShopRightRender(
    ShopType ShopType,
    List<ShopItemRender> Items,
    int CurrentCoin,
    bool IsExited) : RightPageRender
{
    public override PageType PageType => PageType.Event;
}

public record ShopItemRender(string ItemId, string Name, string Description, int Cost, bool CanAfford, string Category);

public record EnemySelectRightRender(
    WeaponRender LockedWeapon1,
    WeaponRender LockedWeapon2,
    List<WeaponRender> WeaponChoices,
    string? SelectedWeaponId,
    List<EnemyAbilityRender> AbilityPool,
    List<string> SelectedAbilityIds,
    int ChargeThreshold,
    bool IsCommitted_,
    bool IsBoss = false,
    string? BossName = null,
    string? BossDescription = null) : RightPageRender
{
    public override PageType PageType => PageType.EnemySelect;
    public override bool IsCommitted => IsCommitted_;
}

public record WeaponRender(string Id, string Name, WeaponEffect LeftEffect, WeaponEffect CenterEffect, WeaponEffect RightEffect, bool RandomiseLanes, string Description);
public record EnemyAbilityRender(string Id, string Name, string Description, bool IsSelected);

public record CombatRightRender(
    EnemyCombatRender Enemy,
    int ChargeAccumulated,
    int ChargeThreshold,
    int[] LockOn,
    int[] BurnStacks,
    int[] BarrierStacks,
    int[] FusionStacks,
    List<LaneRender> Lanes,
    List<FormationRender> AvailableFormations,
    string? ActiveFormationId,
    List<CombatModifierRender> ActiveModifiers,
    bool AwaitingReadyUp,
    bool IsComplete,
    List<DeployableRender> AvailableDeployables,
    CombatForecast? Forecast,
    List<string> PanickedCrewIds,
    string? KidnappedCrewId,
    bool IsPhantomPage,
    bool IsInvulnerablePage,
    bool IsCommitted_) : RightPageRender
{
    public override PageType PageType => PageType.Combat;
    public override bool IsCommitted => IsCommitted_;
}

public record EnemyCombatRender(
    string Name,
    List<WeaponRender> Weapons,
    List<EnemyAbilityRender> Abilities,
    bool IsBoss,
    int? SuperShieldCurrent,
    List<GeneratedIntentRender> Intents,
    bool IntentsHidden);

public record GeneratedIntentRender(int LaneIndex, string LaneName, string WeaponName, string EffectDescription, int ResolvedValue);

public record LaneRender(
    string Name,
    int LaneIndex,
    int Threat,
    int Shield,
    int Hack,
    int Charge,
    int LockOn,
    int BurnStacks,
    int BarrierStacks,
    int FusionStacks,
    List<SlotCombatRender> Slots);

public record SlotCombatRender(int SlotIndex, SlotType Type, string TypeName, List<string> ConnectedLanes, bool IsDouble, string? SlottedCrewId, string? SlottedCrewName, string? ForecastText);

public record CombatModifierRender(string Name, bool IsPositive);

public record CombatForecast(
    int[] ForecastShield,
    int[] ForecastThreat,
    int[] ForecastHack,
    int[] ForecastCharge,
    int ForecastChargeGain,
    int ForecastHullDamage,
    int ForecastCoinGain);

public record CombatRewardRightRender(
    List<FormationRender> FormationOptions,
    string? SelectedFormationId,
    int CoinReward,
    bool IsCommitted_) : RightPageRender
{
    public override PageType PageType => PageType.CombatReward;
    public override bool IsCommitted => IsCommitted_;
}

public record ChapterConclusionRightRender(
    string NarrativeText,
    int ChapterNumber,
    bool IsCommitted_) : RightPageRender
{
    public override PageType PageType => PageType.ChapterConclusion;
    public override bool IsCommitted => IsCommitted_;
    public override bool TearingAllowed => false;
}

public record EpilogueRightRender(
    int ChaptersCompleted,
    int TotalDamageTaken,
    int TotalShieldGenerated,
    int TotalChargeGenerated,
    int TotalHackGenerated,
    int TotalCrewDeaths,
    int TotalTears,
    int FinalHull,
    int FinalCoin,
    int FinalMorale,
    List<string> SurvivorNames,
    bool IsCommitted_) : RightPageRender
{
    public override PageType PageType => PageType.Epilogue;
    public override bool IsCommitted => IsCommitted_;
    public override bool TearingAllowed => false;
}

// Profile models for API
public record ProfileSummary(string ProfileId, string Name, bool HasActiveRun, int? ChapterNumber);
public record ProfileState(string ProfileId, string Name, RunState? ActiveRun);
