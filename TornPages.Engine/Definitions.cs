namespace TornPages.Engine;

// ── Formations ───────────────────────────────────────────────────────────────

public record FormationDefinition(
    string Id, string Name, FormationRarity Rarity,
    IReadOnlyList<SlotDefinition> Slots,
    FormationPassive? Passive = null);

public record SlotDefinition(
    SlotType Type,
    IReadOnlyList<LaneId> ConnectedLanes,
    bool IsDouble = false,
    string? ChainId = null);

public record FormationPassive(
    FormationPassiveType Type,
    int IntValue = 0,
    float FloatValue = 0f,
    AbilityType? AbilityTypeFilter = null,
    LaneId? Lane = null);

// ── Weapons ───────────────────────────────────────────────────────────────────

public record WeaponDefinition(
    string Id, string Name,
    WeaponEffect LeftEffect,
    WeaponEffect CenterEffect,
    WeaponEffect RightEffect,
    bool RandomiseLanes = false,
    string? SpecialNote = null);

public record WeaponEffect(
    WeaponEffectType Type,
    int IntValue = 0,
    bool IsReactive = false);   // for Counterblaster

// ── Enemy Abilities ───────────────────────────────────────────────────────────

public record EnemyAbilityDefinition(
    string Id, string Name, string Description,
    EnemyAbilityType AbilityType);

// ── Mods ─────────────────────────────────────────────────────────────────────

public record ModDefinition(
    string Id, string Name, string Description,
    SystemType System, ModSize Size, bool IsGlitch,
    ModEffectType EffectType,
    int IntValue = 0, float FloatValue = 0f);

// ── Traits ────────────────────────────────────────────────────────────────────

public record TraitDefinition(
    string Id, string Name, string Description,
    bool IsNegative,
    TraitEffectType? EffectType = null,
    NegativeTraitEffectType? NegativeEffectType = null);

// ── Abilities ─────────────────────────────────────────────────────────────────

public record AbilityDefinition(
    string Id,
    AbilityType Type,
    AbilityRarity Rarity);

// ── Races ─────────────────────────────────────────────────────────────────────

public record RaceDefinition(
    string Id, Race Race, string Name, string Description,
    int BaseHp, int BonusRes, int BonusCha, int BonusInt, int BonusEnd,
    RacePassiveType Passive = RacePassiveType.None);

public enum RacePassiveType { None, ReplicantRevive, SilentBoostRandomSlot }

// ── Deployables ───────────────────────────────────────────────────────────────

public record DeployableDefinition(
    string Id, string Name, string Description,
    DeployableEffectType EffectType,
    int IntValue = 0);

// ── Events ────────────────────────────────────────────────────────────────────

public record EventDefinition(
    string Id, string Name, EventStyle Style,
    int DramaticTension,
    bool TearingAllowed = true,
    bool AlwaysInPool = false,
    int MaxPerChapter = 99,
    string? RequiredLocationId = null,
    string? RequiredCondition = null);

// ── Locations ─────────────────────────────────────────────────────────────────

public record LocationDefinition(
    string Id, string Name, string LoreDescription,
    string? SpecialEventId = null);
