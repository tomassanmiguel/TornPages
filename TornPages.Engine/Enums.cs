namespace TornPages.Engine;

public enum Race { Human, Symbiote, Dwarf, Steelborn, Replicant, Brittlebone, Silent }
public enum AbilityType { Shield, Hack, Charge, Boost, Barrier, Heal, Fusion }
public enum AbilityRarity { Common, Rare }
public enum SlotType { Empty, Boosted, Pain, Heal, Growth, Fatigue, MinusFatigue, Kill }
public enum LaneId { Left, Center, Right }
public enum SystemType { Engine, Cabin, Hull, Computers }
public enum FormationRarity { Starting, Common, Uncommon, Rare }
public enum ModSize { S, M, L }
public enum DifficultyLevel { Normal = 0, Hard = 1 }
public enum RunPhase { Prologue, Chapter, Ended }
public enum EventStyle { Announcement, Option, Shop, CrewCreation }
public enum ShopType { Mod, Deployable, Healing, Mercenary, Sabotage, Upgrade }

public enum PageType
{
    PrologueCrew,
    PrologueBoon,
    LocationSelect,
    BuildUp,
    Event,
    EnemySelect,
    Combat,
    CombatReward,
    ChapterConclusion,
    Epilogue
}

public enum FormationPassiveType
{
    None, LockOnReduction, NoPanic, GraveyardBonus, CoreBonus, EchoEffect,
    FullHousePassive, PowerColumnBoost, BrainformOverride, SmileformOverride,
    UnifyBonus, ChampionBonus, AwakenPassive, PhalanxPassive, PhantomPassive,
    NecropassPassive, MedbayPassive, SacrificePassive, AscensionPassive
}

public enum WeaponEffectType
{
    None, Threat, PiercingThreat, BurnStacks, LockOnIncrease, LockOnDecrease,
    ChargeReduction, MoraleReduction, CounterblasterThreat, KillRandomCrew
}

public enum EnemyAbilityType
{
    InvulnerableOnEvenPages, LockOnAbilitiesPlusOne, ChargeThresholdPlusFiftyPercent,
    FormationsPlusOnePainSlot, ThreatPlusThirtyPercent, RandomCrewPanicsEachPage,
    MoraleDropsFivePercentEachPage, NoPassiveCharge, FormationsPlusOneFatigueSlot,
    TwentyFivePercentCriticalThreat, OneBurnEachPage, DeployablesCantBeUsed,
    OnlyDrawOneFormation, ChargingAlsoGeneratesThreat, PlusOneLockOnWhenCrewExhausts,
    CrewMemberKidnapped, CrewMembersPlusOneDamageFromThreat, EnemyIntentsHidden,
    EnemyAffectedByPlayerBoost, ImmuneDuringFirstTwoPages,
    ChargingLaneGetsPlusOneLockOn, TwentyFivePercentThreatPersists
}

public enum DeployableEffectType
{
    ReplaceEmptySlotsShieldFive, LoseTenPercentMoraleInvulnerable, HealTwentyFivePercent,
    BoostHundredAllLanes, UnexhaustAllCrewZeroFatigue, NoFatigueThisPage,
    EscapeNonBossCombat, RedrawFormations, GainTwentyPercentMorale,
    ReduceAllLockOnToZero, IncreaseCoinThirtyPercent, ChargeThirty,
    CureNegativeTraits, DestroyRandomGlitch
}

public enum CombatModifierType
{
    // Positive
    InvulnerabilityFirstPage, PreventFirstDamage, CrewPlusOneEl,
    DrawPlusOneFormation, BoostRandomSlotEachPage, HackTwoAllLanes,
    LowerChargeThresholdTenPercent, EmptySlotsShieldOne, EmptySlotsChargeOne, CrewPlusOneEnd,
    // Negative
    PlusOneLockOnAllLanes, PlusTwoBurnAllLanes, CrewMinusOneEl,
    DrawMinusOneFormation, PlusPainSlotEachPage, DeployablesDisabled,
    RandomCrewPanicsEachPage, RaiseChargeThresholdTenPercent,
    PlusOneLockOnRandomLaneEachPage, EmptySlotsThreattwo
}

public enum TraitEffectType
{
    UnaffectedByMorale, WontPanic,
    PlusTwoIntMinusOneCha, PlusTwoChaMinusOneInt, PlusTwoResMinusOneInt, PlusTwoIntMinusOneRes,
    PlusTwoChaMinusOneRes, PlusTwoResMinusOneCha,
    PlusOneInt, PlusOneCha, PlusOneRes,
    PlusOneEndNoBoosted, FiftyPercentMoreBoost, GainStatAfterCombat,
    PlusElPerHundredCoin, PlusElAfterEachPage, SacrificeToPreventDeath,
    ThreePlusElDecayEachPage, DoubleElAtOneHull, RegenHpAfterCombat,
    PlusFiveResMinusOneEnd, PlusElEachTimeUsed, Shield5OnExhaust,
    PlusTwoResTwentyFivePanic, PlusOneEndMinusThreeRes,
    PlusElPerSameRaceCrew, FiftyPercentCarryoverNextPage,
    PlusElPerLockOnInLane, SevenElMinusPerOtherCrew,
    IntDoublePassiveCharge, ChaInsteadOfRes, PlusTwoResRandomizeEffect,
    PlusElPerTenIncomingThreat, Hack10OnExhaust,
    PlusTwoElLeftLane, PlusTwoElCenterLane, PlusTwoElRightLane,
    ExhaustedTwentyFiveChanceRefresh, NotDamagedByThreatOverflow,
    PlusElConnectedCrew, PlusTwoResLethalDamage,
    SixMaxHp, GainStatsOnCrewmateDeath, PlusElWhenHullDamaged, PlusElWhenSelfDamaged,
    AlwaysConnectedLeft, AlwaysConnectedCenter, AlwaysConnectedRight
}

public enum NegativeTraitEffectType
{
    PanicExhaust, MinusOneRes, MinusOneCha, MinusOneInt, MinusOneEndPlusTwoRes,
    DoubleDamage, PlusTwoThreatToEffect, PanicWhenDamaged, CantBeBoosted,
    MinusOneResAfterEachPage, MinusElPerLockOnInLane, MinusElPerDifferentRaceCrew,
    CannotHeal, MoraleToThirtyOnDeath, MinusElWhenHullDamaged, MinusElInBossFights,
    IntHalfPassiveCharge, MoraleAffectsDoubly, TwoMaxHp, MinusStatAfterCombat
}

public enum ModEffectType
{
    // Hull
    HullRegenOnePerPage, ReduceAllDamageByOne, GainCoinOnHullDamage,
    CrewPlusElAtLowHull, CrewPlusElWhenHullDamaged, ReduceAllDamageThirtyThreePercent,
    EmptySlotHealTwo, HealTwoWhenCrewExhausts, CrewPlusElAtFullHull,
    ChargeWhenHullDamaged, RetainTwentyFiveShield, ImmuneDamageFirstPage,
    HackGrantsShield, BoostAffectsShieldHealMore, LeftoverShieldsRetained,
    // Engine
    MaxLockOnLaneBoost, EmptySlotChargeTwo, PassiveChargePlusFiftyPercent,
    ExhaustCrewChargeTwoConnected, AllCrewGainChargeOne, ChargeThresholdLowerBoss,
    ChargingAlsoShields, EscapeHealTen, EscapeGainDeployable,
    SmallDamagePrevented, BaseEngineChargePlusFifty, NoActionDoublePassiveCharge,
    EscapeMoraleUp, NegativeChargeDisabled, BoostPersistsFiftyPercent,
    // Computers
    CantBeHacked, EmptySlotShieldTwo, MaxLockOnReducedByOne,
    BoostPerLockOn, HackTwoAllLanesStartWithLockOn, OneSlotBoostedPerPage,
    OneSlotMinusFatiguePerPage, PermanentBoostFlankLanes, PermanentBoostCenterLane,
    HackPlusOne, PlusOneFormationRewardChoice, PlusOneFormationDrawPerPage,
    AllowRepeatFormations, BoostStarterFormation, BoostedSlotsPlusTwentyFiveBoost,
    MinusFatigueSlotsBoostedTwentyFive, HealSlotsPlusOne, RareFormationsTwice,
    // Cabin
    ExhaustShieldTwoConnected, CrewCantPanic, EscapeInterestOnCoin,
    CrewPlusElPerHundredCoin, NewCrewPlusStat, CrewDeathGrantsPlusTwoRes,
    ExhaustedRecoverAfterTwoPages, GainMaxCabinCapacity, CrewNoDamageFromThreat,
    FiveDiffRacesPlusEl, OneCrewCantDie, FirstCrewNoFatigue,
    CabinChaInsteadOfRes, TwoOrMoreFatiguePlusTwoEl, CrewHealFullAfterCombat,
    CrewPlusElFirstPage, CrewLoseOneFatiguePerPage,
    // Glitches – Hull
    GlitchPlusOneDamageAll, GlitchHullCantHealInCombat, GlitchDoubleDamageFromPain, GlitchLoseCoinEveryPage,
    // Glitches – Cabin
    GlitchMoraleCapEighty, GlitchExhaustedMinusOneRes, GlitchNewCrewMinusOneStat, GlitchCrewStartOneFatigue,
    // Glitches – Computers
    GlitchAddFatigueSlot, GlitchAddPainSlot, GlitchOnlyOneFormationRemoval, GlitchFewerFormationRewards,
    // Glitches – Engine
    GlitchChargeThresholdPlusTen, GlitchLoseChargeWhenDamaged, GlitchAllEnemiesExtraAbility, GlitchStartOneLockOnAll
}
