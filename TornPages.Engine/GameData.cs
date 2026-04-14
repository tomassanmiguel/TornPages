namespace TornPages.Engine;

// ─────────────────────────────────────────────────────────────────────────────
//  Slot-builder helpers
// ─────────────────────────────────────────────────────────────────────────────
file static class S
{
    public static SlotDefinition E(params LaneId[] lanes) => new(SlotType.Empty, lanes);
    public static SlotDefinition B(params LaneId[] lanes) => new(SlotType.Boosted, lanes);
    public static SlotDefinition P(params LaneId[] lanes) => new(SlotType.Pain, lanes);
    public static SlotDefinition H(params LaneId[] lanes) => new(SlotType.Heal, lanes);
    public static SlotDefinition G(params LaneId[] lanes) => new(SlotType.Growth, lanes);
    public static SlotDefinition F(params LaneId[] lanes) => new(SlotType.Fatigue, lanes);
    public static SlotDefinition M(params LaneId[] lanes) => new(SlotType.MinusFatigue, lanes);
    public static SlotDefinition K(params LaneId[] lanes) => new(SlotType.Kill, lanes);
    public static SlotDefinition E2x(params LaneId[] lanes) => new(SlotType.Empty, lanes, IsDouble: true);
    public static SlotDefinition B3x(params LaneId[] lanes) => new SlotDefinition(SlotType.Boosted, lanes) with { IsDouble = true }; // Superfocus 3x handled via IsDouble
    public static readonly LaneId L = LaneId.Left, C = LaneId.Center, R = LaneId.Right;
}

// ─────────────────────────────────────────────────────────────────────────────
//  Formation Data
// ─────────────────────────────────────────────────────────────────────────────
public static class FormationData
{
    public static readonly IReadOnlyList<FormationDefinition> All;

    static FormationData()
    {
        var l = LaneId.Left; var c = LaneId.Center; var r = LaneId.Right;

        // ─ Starting ─
        var Flat = new FormationDefinition("Flat", "Flat", FormationRarity.Starting,
            [S.E(l), S.E(c), S.E(r)]);
        var Line = new FormationDefinition("Line", "Line", FormationRarity.Starting,
            [S.E(c), S.E(c), S.E(c)]);
        var Flank = new FormationDefinition("Flank", "Flank", FormationRarity.Starting,
            [S.E(l), S.E(l), S.E(r), S.E(r)]);
        var Center = new FormationDefinition("Center", "Center", FormationRarity.Starting,
            [S.E(l, c, r)]);

        // ─ Common ─
        var Encircle = new FormationDefinition("Encircle", "Encircle", FormationRarity.Common,
            [S.E(l, r), S.E(l, r), S.M(c)]);
        var ColumnCenter = new FormationDefinition("ColumnCenter", "Column Center", FormationRarity.Common,
            [S.B(c), S.B(c), S.E(c), S.E(c)]);
        var Bide = new FormationDefinition("Bide", "Bide", FormationRarity.Common,
            [S.G(l), S.G(c), S.G(c), S.G(r)]);
        var Chandelier = new FormationDefinition("Chandelier", "Chandelier", FormationRarity.Common,
            [S.E(l, c, r), S.E(l, c), S.E(c, r)]);
        var Leftron = new FormationDefinition("Leftron", "Leftron", FormationRarity.Common,
            [S.E2x(l), S.E(l), S.B(l), S.E(l)]);
        var Rightron = new FormationDefinition("Rightron", "Rightron", FormationRarity.Common,
            [S.E2x(r), S.B(r), S.E(r), S.E(r)]);
        var Bastion = new FormationDefinition("Bastion", "Bastion", FormationRarity.Common,
            [S.B(l, c), S.M(c, r)]);
        var Layers = new FormationDefinition("Layers", "Layers", FormationRarity.Common,
            [S.E(l), S.E(l), S.E(c), S.E(c), S.E(r), S.E(r)]);
        var Command = new FormationDefinition("Command", "Command", FormationRarity.Common,
            [S.E(l), S.E(c), S.E(r), S.G(l, c, r)]);
        var Focus = new FormationDefinition("Focus", "Focus", FormationRarity.Common,
            [S.E(l, c, r), S.E(l, c, r)]);
        var Pincer = new FormationDefinition("Pincer", "Pincer", FormationRarity.Common,
            [S.E(l), S.E(l), S.E(l), S.E(r), S.E(r), S.E(r)]);
        var Retreat = new FormationDefinition("Retreat", "Retreat", FormationRarity.Common,
            [S.H(), S.H(), S.H(), S.G(), S.G(), S.G()]);
        var PleasurePain = new FormationDefinition("PleasurePain", "Pleasure & Pain", FormationRarity.Common,
            [S.P(l, c), S.H(l, c), S.P(c, r), S.H(c, r)]);
        var MiddleDeflection = new FormationDefinition("MiddleDeflection", "Middle Deflection", FormationRarity.Common,
            [S.E(l), S.E(c), S.E(c), S.E(c), S.E(r)],
            new FormationPassive(FormationPassiveType.LockOnReduction, Lane: LaneId.Center, IntValue: 1));
        var LeftDeflection = new FormationDefinition("LeftDeflection", "Left Deflection", FormationRarity.Common,
            [S.E(l), S.E(l), S.E(l), S.E(c), S.E(r)],
            new FormationPassive(FormationPassiveType.LockOnReduction, Lane: LaneId.Left, IntValue: 1));
        var RightDeflection = new FormationDefinition("RightDeflection", "Right Deflection", FormationRarity.Common,
            [S.E(l), S.E(c), S.E(r), S.E(r), S.E(r)],
            new FormationPassive(FormationPassiveType.LockOnReduction, Lane: LaneId.Right, IntValue: 1));
        var Resolution = new FormationDefinition("Resolution", FormationRarity.Common == FormationRarity.Common ? "Resolution" : "Resolution", FormationRarity.Common,
            [S.E(l), S.E(l), S.E(c), S.E(r), S.E(r)],
            new FormationPassive(FormationPassiveType.NoPanic));
        var Squeeze = new FormationDefinition("Squeeze", "Squeeze", FormationRarity.Common,
            [S.E(l, r), S.B(l, r)]);
        var Reprieve = new FormationDefinition("Reprieve", "Reprieve", FormationRarity.Common,
            [S.M(l), S.M(l), S.M(c), S.M(r), S.M(r)]);
        var ColumnLeft = new FormationDefinition("ColumnLeft", "Column Left", FormationRarity.Common,
            [S.B(l), S.B(l), S.E(l), S.E(l)]);
        var ColumnRight = new FormationDefinition("ColumnRight", "Column Right", FormationRarity.Common,
            [S.B(r), S.B(r), S.E(r), S.E(r)]);
        var Echo = new FormationDefinition("Echo", "Echo", FormationRarity.Common,
            [S.E(l), S.E(c), S.E(r)],
            new FormationPassive(FormationPassiveType.EchoEffect, FloatValue: 0.25f));
        var Pendant = new FormationDefinition("Pendant", "Pendant", FormationRarity.Common,
            [S.E(l), S.E(l), S.F(l, c, r), S.E(r), S.E(r)]);
        var W = new FormationDefinition("W", "W", FormationRarity.Common,
            [S.E(l), S.E(c), S.E(r), S.E(l, c), S.E(c, r)]);

        // ─ Uncommon ─
        var Unify = new FormationDefinition("Unify", "Unify", FormationRarity.Uncommon,
            [S.E(l), S.E(l, r), S.E(r), S.B(c)],
            new FormationPassive(FormationPassiveType.UnifyBonus));
        var Champion = new FormationDefinition("Champion", "Champion", FormationRarity.Uncommon,
            [S.M(), S.M(), S.M(), S.M(), S.M(), S.M(), S.E(l, c, r)],
            new FormationPassive(FormationPassiveType.ChampionBonus));
        var AllOut = new FormationDefinition("AllOut", "All Out", FormationRarity.Uncommon,
            [S.F(l, c, r), S.F(l, c, r), S.F(l, c, r)]);
        var LeftBias = new FormationDefinition("LeftBias", "Left Bias", FormationRarity.Uncommon,
            [S.M(l, c), S.E(l, c), S.E(l, c)]);
        var RightBias = new FormationDefinition("RightBias", "Right Bias", FormationRarity.Uncommon,
            [S.M(c, r), S.E(c, r), S.E(c, r)]);
        var FullHouse = new FormationDefinition("FullHouse", "Full House", FormationRarity.Uncommon,
            [S.E(l), S.E(l), S.E(l), S.E(c), S.E(c), S.E(c), S.E(r), S.E(r), S.E(r)],
            new FormationPassive(FormationPassiveType.FullHousePassive, IntValue: 5));
        var Exemplify = new FormationDefinition("Exemplify", "Exemplify", FormationRarity.Uncommon,
            [S.E(l, c), S.E(c, r), S.G(), S.G(), S.G()]);
        var TripleThreat = new FormationDefinition("TripleThreat", "Triple Threat", FormationRarity.Uncommon,
            [S.B(l), S.B(c), S.B(r)]);
        var Arch = new FormationDefinition("Arch", "Arch", FormationRarity.Uncommon,
            [S.H(l), S.M(l), S.E(c), S.G(r), S.B(r)]);
        var PowerColumn = new FormationDefinition("PowerColumn", "Power Column", FormationRarity.Uncommon,
            [S.E(c), S.E(c), S.E(c), S.E(c), S.M(c)],
            new FormationPassive(FormationPassiveType.PowerColumnBoost, FloatValue: 1.5f));
        var Graveyard1 = new FormationDefinition("Graveyard1", "Graveyard I", FormationRarity.Uncommon,
            [S.E(l), S.E(c), S.E(c), S.E(c), S.E(r), S.E(r)],
            new FormationPassive(FormationPassiveType.GraveyardBonus, FloatValue: 0.5f));
        var Graveyard2 = new FormationDefinition("Graveyard2", "Graveyard II", FormationRarity.Uncommon,
            [S.E(l), S.E(r), S.E(r), S.E(r), S.E(c), S.E(c)],
            new FormationPassive(FormationPassiveType.GraveyardBonus, FloatValue: 0.5f));
        var Graveyard3 = new FormationDefinition("Graveyard3", "Graveyard III", FormationRarity.Uncommon,
            [S.E(r), S.E(c), S.E(c), S.E(c), S.E(l), S.E(l)],
            new FormationPassive(FormationPassiveType.GraveyardBonus, FloatValue: 0.5f));
        var Graveyard4 = new FormationDefinition("Graveyard4", "Graveyard IV", FormationRarity.Uncommon,
            [S.E(r), S.E(l), S.E(l), S.E(l), S.E(c), S.E(c)],
            new FormationPassive(FormationPassiveType.GraveyardBonus, FloatValue: 0.5f));
        var Shieldcore = new FormationDefinition("Shieldcore", "Shieldcore", FormationRarity.Uncommon,
            [S.E(l), S.E(c), S.E(r)],
            new FormationPassive(FormationPassiveType.CoreBonus, AbilityTypeFilter: AbilityType.Shield, FloatValue: 0.5f));
        var Hackcore = new FormationDefinition("Hackcore", "Hackcore", FormationRarity.Uncommon,
            [S.E(l), S.E(c), S.E(r)],
            new FormationPassive(FormationPassiveType.CoreBonus, AbilityTypeFilter: AbilityType.Hack, FloatValue: 0.5f));
        var Chargecore = new FormationDefinition("Chargecore", "Chargecore", FormationRarity.Uncommon,
            [S.E(l), S.E(c), S.E(r)],
            new FormationPassive(FormationPassiveType.CoreBonus, AbilityTypeFilter: AbilityType.Charge, FloatValue: 0.5f));
        var Awakener = new FormationDefinition("Awakener", "Awakener", FormationRarity.Uncommon,
            [S.F(l), S.F(l), S.F(l), S.F(c), S.F(c), S.F(c), S.F(r), S.F(r), S.F(r)],
            new FormationPassive(FormationPassiveType.AwakenPassive));
        var AcrobatLeft = new FormationDefinition("AcrobatLeft", "Acrobat Left", FormationRarity.Uncommon,
            [S.E(l), S.E(l), S.E(l), S.E(c, r), S.E(c, r)]);
        var AcrobatRight = new FormationDefinition("AcrobatRight", "Acrobat Right", FormationRarity.Uncommon,
            [S.E(l, c), S.E(l, c), S.E(r), S.E(r), S.E(r)]);
        var Brainform = new FormationDefinition("Brainform", "Brainform", FormationRarity.Uncommon,
            [S.E(l), S.E(l), S.E(c), S.P(c), S.E(r), S.E(r)],
            new FormationPassive(FormationPassiveType.BrainformOverride));
        var Smileform = new FormationDefinition("Smileform", "Smileform", FormationRarity.Uncommon,
            [S.E(l), S.E(l), S.E(c), S.P(c), S.E(r), S.E(r)],
            new FormationPassive(FormationPassiveType.SmileformOverride));

        // ─ Rare ─
        var Superfocus = new FormationDefinition("Superfocus", "Superfocus", FormationRarity.Rare,
            [new SlotDefinition(SlotType.Boosted, [LaneId.Center], IsDouble: true)]);
        var Medbay = new FormationDefinition("Medbay", "Medbay", FormationRarity.Rare,
            [S.H(l), S.H(c), S.H(r), S.H(), S.H(), S.H()],
            new FormationPassive(FormationPassiveType.MedbayPassive));
        var Sacrifice = new FormationDefinition("Sacrifice", "Sacrifice", FormationRarity.Rare,
            [S.K(), S.E(l), S.E(l), S.E(l, c, r), S.E(r), S.E(r)],
            new FormationPassive(FormationPassiveType.SacrificePassive));
        var Horseshoe = new FormationDefinition("Horseshoe", "Horseshoe", FormationRarity.Rare,
            [S.E(l, r), S.E(l), S.E(l), S.E(l), S.E(r), S.E(r), S.E(r)]);
        var Phalanx = new FormationDefinition("Phalanx", "Phalanx", FormationRarity.Rare,
            [S.E(l), S.B(c), S.E(r), S.E(l, c, r)],
            new FormationPassive(FormationPassiveType.PhalanxPassive));
        var Lavafield = new FormationDefinition("Lavafield", "Lavafield", FormationRarity.Rare,
            [S.P(l), S.P(l), S.P(l), S.P(l),
             S.P(c), S.P(c), S.P(c), S.P(c),
             S.P(r), S.P(r), S.P(r), S.P(r)]);
        var Ascension = new FormationDefinition("Ascension", "Ascension", FormationRarity.Rare,
            [S.E()],
            new FormationPassive(FormationPassiveType.AscensionPassive));
        var Phantom = new FormationDefinition("Phantom", "Phantom", FormationRarity.Rare,
            [S.E(l), S.E(c), S.E(r)],
            new FormationPassive(FormationPassiveType.PhantomPassive));
        var Dancers = new FormationDefinition("Dancers", "Dancers", FormationRarity.Rare,
            [S.M(l, c), S.M(c, r), S.M(l, r)]);
        var Necropolis = new FormationDefinition("Necropolis", "Necropolis", FormationRarity.Rare,
            [S.F(l, c), S.F(c, r), S.F(l, c, r), S.F(l, c, r)],
            new FormationPassive(FormationPassiveType.NecropassPassive));

        All = [
            Flat, Line, Flank, Center,
            Encircle, ColumnCenter, Bide, Chandelier, Leftron, Rightron, Bastion, Layers,
            Command, Focus, Pincer, Retreat, PleasurePain, MiddleDeflection, LeftDeflection,
            RightDeflection, Resolution, Squeeze, Reprieve, ColumnLeft, ColumnRight, Echo, Pendant, W,
            Unify, Champion, AllOut, LeftBias, RightBias, FullHouse, Exemplify, TripleThreat, Arch,
            PowerColumn, Graveyard1, Graveyard2, Graveyard3, Graveyard4,
            Shieldcore, Hackcore, Chargecore, Awakener, AcrobatLeft, AcrobatRight, Brainform, Smileform,
            Superfocus, Medbay, Sacrifice, Horseshoe, Phalanx, Lavafield, Ascension, Phantom, Dancers, Necropolis
        ];
    }

    public static FormationDefinition? Get(string id) => All.FirstOrDefault(f => f.Id == id);

    public static IReadOnlyList<FormationDefinition> GetByRarity(FormationRarity rarity) =>
        All.Where(f => f.Rarity == rarity).ToList();

    public static IReadOnlyList<FormationDefinition> StartingFour =>
        new[] { "Flat", "Line", "Flank", "Center" }.Select(id => All.First(f => f.Id == id)).ToList();
}

// ─────────────────────────────────────────────────────────────────────────────
//  Weapon Data
// ─────────────────────────────────────────────────────────────────────────────
public static class WeaponData
{
    static WeaponEffect T(int v) => new(WeaponEffectType.Threat, v);
    static WeaponEffect PT(int v) => new(WeaponEffectType.PiercingThreat, v);
    static WeaponEffect Burn(int v) => new(WeaponEffectType.BurnStacks, v);
    static WeaponEffect Lo(int v = 1) => new(WeaponEffectType.LockOnIncrease, v);
    static WeaponEffect LoDown(int v = 1) => new(WeaponEffectType.LockOnDecrease, v);
    static WeaponEffect ChrRed(int v) => new(WeaponEffectType.ChargeReduction, v);
    static WeaponEffect Mor(int v) => new(WeaponEffectType.MoraleReduction, v);
    static WeaponEffect Ctr() => new(WeaponEffectType.CounterblasterThreat, IsReactive: true);
    static WeaponEffect None() => new(WeaponEffectType.None);

    // Act I = S, Act II = M, Act III = L — we store Act I values; resolution scales by chapter
    public static readonly IReadOnlyList<WeaponDefinition> All =
    [
        new("LeftBeam", "Left Beam", T(7), Lo(), Lo()),
        new("MiddleBeam", "Middle Beam", Lo(), T(7), Lo()),
        new("RightBeam", "Right Beam", Lo(), Lo(), T(7)),
        new("BroadBurn", "Broad Burn", Burn(3), Burn(3), Burn(3)),
        new("LockOn", "Lock On", Lo(), Lo(), Lo(), SpecialNote: "At max lock-on: L/XL/XXL Threat all lanes"),
        new("HackWeapon", "Hack", Lo(), T(7), Lo(), RandomiseLanes: true),
        new("BroadBeam", "Broad Beam", T(4), T(7), T(4), RandomiseLanes: true),
        new("PanicRay", "Panic Ray", new WeaponEffect(WeaponEffectType.None, 0), Lo(), Burn(3), RandomiseLanes: true,
            SpecialNote: "Left lane: crew panic"),
        new("Repulsor", "Repulsor", Lo(), ChrRed(15), T(4), RandomiseLanes: true),
        new("ShieldPiercer", "Shield Piercer", PT(4), T(7), Lo(), RandomiseLanes: true,
            SpecialNote: "Piercing Threat cannot be shielded"),
        new("PredictableBlast", "Predictable Blast", Lo(), T(16), Lo(),
            SpecialNote: "Shield doubly effective"),
        new("Counterblaster", "Counterblaster", Ctr(), Ctr(), Lo(), RandomiseLanes: true),
        new("LongTermThreatener", "Long Term Threatener", Burn(3), Lo(2), Mor(5), RandomiseLanes: true),
        new("Incinerator", "Incinerator", Burn(6), Lo(), Lo(), RandomiseLanes: true),
        new("FadingCanon", "Fading Canon", T(7), T(7), LoDown(), RandomiseLanes: true,
            SpecialNote: "-3 threat per prior use"),
    ];

    public static WeaponDefinition? Get(string id) => All.FirstOrDefault(w => w.Id == id);

    // Scale threat values by act (1=S,2=M,3=L)
    public static int ScaleThreat(int baseValue, int actNumber) => actNumber switch
    {
        1 => baseValue,           // S
        2 => baseValue + 8,       // M
        3 => baseValue + 16,      // L
        _ => baseValue
    };
}

// ─────────────────────────────────────────────────────────────────────────────
//  Enemy Ability Data
// ─────────────────────────────────────────────────────────────────────────────
public static class EnemyAbilityData
{
    public static readonly IReadOnlyList<EnemyAbilityDefinition> All =
    [
        new("InvulnEven", "Iron Schedule", "Invulnerable on even-numbered pages.", EnemyAbilityType.InvulnerableOnEvenPages),
        new("LockOnPlus1", "Tracking Systems", "Lock-on abilities deal +1 additional lock-on.", EnemyAbilityType.LockOnAbilitiesPlusOne),
        new("ChargeThreshPlus50", "Gravitational Anchor", "Charge threshold increased by 50%.", EnemyAbilityType.ChargeThresholdPlusFiftyPercent),
        new("FormPainSlot", "Corrosive Field", "All formations gain +1 Pain slot.", EnemyAbilityType.FormationsPlusOnePainSlot),
        new("ThreatPlus30", "Overcharged Cannons", "All Threat increased by 30%.", EnemyAbilityType.ThreatPlusThirtyPercent),
        new("PanicsEachPage", "Dread Signal", "A random crew member panics each page.", EnemyAbilityType.RandomCrewPanicsEachPage),
        new("MoraleDrops5", "Psychological Warfare", "Morale drops 5% each page.", EnemyAbilityType.MoraleDropsFivePercentEachPage),
        new("NoPassiveCharge", "Signal Jamming", "No passive charge generation.", EnemyAbilityType.NoPassiveCharge),
        new("FormFatigueSlot", "Gravitational Drag", "All formations gain +1 Fatigue slot.", EnemyAbilityType.FormationsPlusOneFatigueSlot),
        new("CritThreat25", "Volatile Payload", "25% chance of critical threat (doubles Threat).", EnemyAbilityType.TwentyFivePercentCriticalThreat),
        new("BurnEachPage", "Plasma Trail", "Apply 1 Burn each page.", EnemyAbilityType.OneBurnEachPage),
        new("DeployDisabled", "Interference Field", "Deployables can't be used.", EnemyAbilityType.DeployablesCantBeUsed),
        new("OneDraw", "Disruptor Wave", "Only draw 1 formation each turn.", EnemyAbilityType.OnlyDrawOneFormation),
        new("ChargeThreat", "Reactive Armor", "Generating charge also generates equal Threat in that lane.", EnemyAbilityType.ChargingAlsoGeneratesThreat),
        new("ExhaustLockOn", "Demoralization Protocol", "+1 lock-on in all lanes when a crew member exhausts.", EnemyAbilityType.PlusOneLockOnWhenCrewExhausts),
        new("Kidnapped", "Press Gang", "A random crew member is kidnapped and unavailable this combat.", EnemyAbilityType.CrewMemberKidnapped),
        new("PlusDamage", "Scatter Shot", "Crew members take +1 damage from unmitigated threat.", EnemyAbilityType.CrewMembersPlusOneDamageFromThreat),
        new("HiddenIntents", "Encrypted Orders", "Enemy intents are hidden until page confirmation.", EnemyAbilityType.EnemyIntentsHidden),
        new("PlayerBoostAffects", "Mirrored Defenses", "Enemy actions are affected by player boost.", EnemyAbilityType.EnemyAffectedByPlayerBoost),
        new("ImmuneFirstTwo", "Boot Sequence", "Immune during first two pages.", EnemyAbilityType.ImmuneDuringFirstTwoPages),
        new("ChargeLockOn", "Resonance Sensor", "Charging in a lane gives it +1 lock-on.", EnemyAbilityType.ChargingLaneGetsPlusOneLockOn),
        new("ThreatPersists25", "Feedback Loop", "25% of unmitigated Threat persists to next page.", EnemyAbilityType.TwentyFivePercentThreatPersists),
    ];

    public static EnemyAbilityDefinition? Get(string id) => All.FirstOrDefault(a => a.Id == id);
}

// ─────────────────────────────────────────────────────────────────────────────
//  Trait Data
// ─────────────────────────────────────────────────────────────────────────────
public static class TraitData
{
    public static readonly IReadOnlyList<TraitDefinition> PositiveTraits;
    public static readonly IReadOnlyList<TraitDefinition> NegativeTraits;
    public static readonly IReadOnlyList<TraitDefinition> All;

    static TraitData()
    {
        PositiveTraits =
        [
            new("UnaffectedMorale", "Iron Will", "Unaffected by Morale multiplier.", false, TraitEffectType.UnaffectedByMorale),
            new("WontPanic", "Steadfast", "This crew member cannot panic.", false, TraitEffectType.WontPanic),
            new("StatIntCha", "Analyst", "+2 Int, −1 Cha.", false, TraitEffectType.PlusTwoIntMinusOneCha),
            new("StatChaInt", "Diplomat", "+2 Cha, −1 Int.", false, TraitEffectType.PlusTwoChaMinusOneInt),
            new("StatResInt", "Resolute", "+2 Res, −1 Int.", false, TraitEffectType.PlusTwoResMinusOneInt),
            new("StatIntRes", "Quick Learner", "+2 Int, −1 Res.", false, TraitEffectType.PlusTwoIntMinusOneRes),
            new("StatChaRes", "Charismatic", "+2 Cha, −1 Res.", false, TraitEffectType.PlusTwoChaMinusOneRes),
            new("StatResCha", "Disciplined", "+2 Res, −1 Cha.", false, TraitEffectType.PlusTwoResMinusOneCha),
            new("PlusOneInt", "Bright", "+1 Int.", false, TraitEffectType.PlusOneInt),
            new("PlusOneCha", "Likeable", "+1 Cha.", false, TraitEffectType.PlusOneCha),
            new("PlusOneRes", "Determined", "+1 Res.", false, TraitEffectType.PlusOneRes),
            new("PlusOneEndNoBoosted", "Enduring", "+1 End; cannot be boosted.", false, TraitEffectType.PlusOneEndNoBoosted),
            new("FiftyMoreBoost", "Receptive", "Affected 50% more by Boost.", false, TraitEffectType.FiftyPercentMoreBoost),
            new("GainStatAfterCombat", "Veteran", "Gain +1 to a random stat (Int, Res, or Cha) after each combat.", false, TraitEffectType.GainStatAfterCombat),
            new("ElPerHundredCoin", "Motivated", "+1 Effect Level for every 100 coin you have.", false, TraitEffectType.PlusElPerHundredCoin),
            new("ElAfterEachPage", "Battle-Hardened", "+1 Effect Level after every page in combat.", false, TraitEffectType.PlusElAfterEachPage),
            new("SacrificePreventDeath", "Guardian", "If the Apogee would be destroyed, this crew member sacrifices themselves to prevent it.", false, TraitEffectType.SacrificeToPreventDeath),
            new("DecayEl", "Burning Bright", "+3 Effect Level at start of combat; −1 after each page.", false, TraitEffectType.ThreePlusElDecayEachPage),
            new("DoubleElOneHull", "Last Stand", "Double Effect Level while Apogee has exactly 1 Hull left.", false, TraitEffectType.DoubleElAtOneHull),
            new("RegenHpAfterCombat", "Regenerative", "Regenerate 1 HP after combat.", false, TraitEffectType.RegenHpAfterCombat),
            new("PlusFiveRes", "Stubborn", "+5 Res, −1 End.", false, TraitEffectType.PlusFiveResMinusOneEnd),
            new("ElEachTimeUsed", "Flow State", "+1 Effect Level every time used in combat.", false, TraitEffectType.PlusElEachTimeUsed),
            new("Shield5Exhaust", "Dying Breath", "Shield 5 on exhaust.", false, TraitEffectType.Shield5OnExhaust),
            new("TwoResPanic", "Reckless", "+2 Res; 25% chance to panic each page.", false, TraitEffectType.PlusTwoResTwentyFivePanic),
            new("PlusOneEndMinusThreeRes", "Patient", "+1 End, −3 Res.", false, TraitEffectType.PlusOneEndMinusThreeRes),
            new("ElPerSameRace", "Pack Mentality", "+1 Effect Level for each other crew member of the same race.", false, TraitEffectType.PlusElPerSameRaceCrew),
            new("Carryover", "Echo Chamber", "Apply 50% of effect on the next page too.", false, TraitEffectType.FiftyPercentCarryoverNextPage),
            new("ElPerLockOn", "Opportunist", "+1 Effect Level for each lock-on in the lane.", false, TraitEffectType.PlusElPerLockOnInLane),
            new("SevenElMinusCrew", "Lone Wolf", "+7 Effect Level; minus one for each other crew member.", false, TraitEffectType.SevenElMinusPerOtherCrew),
            new("IntDoublePassive", "Deep Thinker", "Intelligence contributes double to passive charge.", false, TraitEffectType.IntDoublePassiveCharge),
            new("ChaIsRes", "Silver Tongue", "Charisma instead of Resolve determines Effect Level.", false, TraitEffectType.ChaInsteadOfRes),
            new("RandomizeEffect", "Wild Card", "+2 Res; effect is randomized each page.", false, TraitEffectType.PlusTwoResRandomizeEffect),
            new("ElPerTenThreat", "Adrenaline", "+1 Effect Level for every 10 incoming Threat.", false, TraitEffectType.PlusElPerTenIncomingThreat),
            new("Hack10Exhaust", "Override", "Hack 10 on exhaust.", false, TraitEffectType.Hack10OnExhaust),
            new("PlusTwoElLeft", "Left-Brained", "+2 Effect Level in Left lane.", false, TraitEffectType.PlusTwoElLeftLane),
            new("PlusTwoElCenter", "Centered", "+2 Effect Level in Center lane.", false, TraitEffectType.PlusTwoElCenterLane),
            new("PlusTwoElRight", "Right-Brained", "+2 Effect Level in Right lane.", false, TraitEffectType.PlusTwoElRightLane),
            new("ExhaustRefresh", "Second Wind", "While exhausted, 25% chance each page to refresh.", false, TraitEffectType.ExhaustedTwentyFiveChanceRefresh),
            new("NoDamageThreat", "Shielded Mind", "Not damaged by Threat overflow.", false, TraitEffectType.NotDamagedByThreatOverflow),
            new("PlusElConnected", "Networked", "+1 Effect Level to connected crew members.", false, TraitEffectType.PlusElConnectedCrew),
            new("LethalDamage", "Glass Cannon", "+2 Res; any damage in combat is lethal.", false, TraitEffectType.PlusTwoResLethalDamage),
            new("SixMaxHp", "Resilient", "This crew member has 6 max HP.", false, TraitEffectType.SixMaxHp),
            new("GainStatsOnDeath", "Rising From Ashes", "When another crew member dies, gain 2 random stats.", false, TraitEffectType.GainStatsOnCrewmateDeath),
            new("ElWhenHullDamaged", "Protector", "Gain +1 Effect Level whenever the Apogee takes damage.", false, TraitEffectType.PlusElWhenHullDamaged),
            new("ElWhenSelfDamaged", "Wounded Pride", "Gain +1 Effect Level whenever this crew member takes damage.", false, TraitEffectType.PlusElWhenSelfDamaged),
            new("AlwaysLeft", "Left Anchor", "Always connected to the Left lane, regardless of slot.", false, TraitEffectType.AlwaysConnectedLeft),
            new("AlwaysCenter", "Center Anchor", "Always connected to the Center lane, regardless of slot.", false, TraitEffectType.AlwaysConnectedCenter),
            new("AlwaysRight", "Right Anchor", "Always connected to the Right lane, regardless of slot.", false, TraitEffectType.AlwaysConnectedRight),
        ];

        NegativeTraits =
        [
            new("NegPanicExhaust", "Brittle Nerves", "If this crew member panics, they exhaust.", true, NegativeEffectType: NegativeTraitEffectType.PanicExhaust),
            new("NegMinusRes", "Weak-Willed", "−1 Res.", true, NegativeEffectType: NegativeTraitEffectType.MinusOneRes),
            new("NegMinusCha", "Antisocial", "−1 Cha.", true, NegativeEffectType: NegativeTraitEffectType.MinusOneCha),
            new("NegMinusInt", "Slow Learner", "−1 Int.", true, NegativeEffectType: NegativeTraitEffectType.MinusOneInt),
            new("NegMinusEndPlusRes", "Fragile Stamina", "−1 End, +2 Res.", true, NegativeEffectType: NegativeTraitEffectType.MinusOneEndPlusTwoRes),
            new("NegDoubleDamage", "Thin Skin", "Takes double damage.", true, NegativeEffectType: NegativeTraitEffectType.DoubleDamage),
            new("NegPlusThreat", "Reckless Fighter", "+2 Threat to effect.", true, NegativeEffectType: NegativeTraitEffectType.PlusTwoThreatToEffect),
            new("NegPanicWhenDamaged", "Skittish", "Panics whenever they take damage.", true, NegativeEffectType: NegativeTraitEffectType.PanicWhenDamaged),
            new("NegCantBeBoosted", "Rigid", "Cannot be boosted.", true, NegativeEffectType: NegativeTraitEffectType.CantBeBoosted),
            new("NegMinusResEachPage", "Tired", "−1 Res after each page in combat.", true, NegativeEffectType: NegativeTraitEffectType.MinusOneResAfterEachPage),
            new("NegElLockOn", "Intimidated", "−1 Effect Level for each lock-on in lane.", true, NegativeEffectType: NegativeTraitEffectType.MinusElPerLockOnInLane),
            new("NegElDiffRace", "Xenophobe", "−1 Effect Level for each differently-raced crew member.", true, NegativeEffectType: NegativeTraitEffectType.MinusElPerDifferentRaceCrew),
            new("NegCantHeal", "Cursed", "Cannot heal.", true, NegativeEffectType: NegativeTraitEffectType.CannotHeal),
            new("NegMoraleOnDeath", "Martyr", "When this crew member dies, morale drops to 30%.", true, NegativeEffectType: NegativeTraitEffectType.MoraleToThirtyOnDeath),
            new("NegElHullDamaged", "Rattled", "−1 Effect Level whenever the hull takes damage.", true, NegativeEffectType: NegativeTraitEffectType.MinusElWhenHullDamaged),
            new("NegElBoss", "Boss Anxiety", "−1 Effect Level in boss fights.", true, NegativeEffectType: NegativeTraitEffectType.MinusElInBossFights),
            new("NegIntHalfPassive", "Distracted", "Intelligence contributes 50% less to passive charge.", true, NegativeEffectType: NegativeTraitEffectType.IntHalfPassiveCharge),
            new("NegMoraleDoubly", "Sensitive", "Morale affects this crew member doubly.", true, NegativeEffectType: NegativeTraitEffectType.MoraleAffectsDoubly),
            new("NegTwoMaxHp", "Frail", "2 max HP.", true, NegativeEffectType: NegativeTraitEffectType.TwoMaxHp),
            new("NegStatLoss", "Declining", "Gain −1 to a random stat after each combat.", true, NegativeEffectType: NegativeTraitEffectType.MinusStatAfterCombat),
        ];

        All = [.. PositiveTraits, .. NegativeTraits];
    }

    public static TraitDefinition? Get(string id) => All.FirstOrDefault(t => t.Id == id);
}

// ─────────────────────────────────────────────────────────────────────────────
//  Ability Data
// ─────────────────────────────────────────────────────────────────────────────
public static class AbilityData
{
    public static readonly IReadOnlyList<AbilityDefinition> All =
    [
        new("Shield", AbilityType.Shield, AbilityRarity.Common),
        new("Hack", AbilityType.Hack, AbilityRarity.Common),
        new("Charge", AbilityType.Charge, AbilityRarity.Common),
        new("Boost", AbilityType.Boost, AbilityRarity.Common),
        new("Barrier", AbilityType.Barrier, AbilityRarity.Rare),
        new("Heal", AbilityType.Heal, AbilityRarity.Rare),
        new("Fusion", AbilityType.Fusion, AbilityRarity.Rare),
    ];

    public static AbilityDefinition Get(AbilityType type) => All.First(a => a.Type == type);
    public static AbilityDefinition? Get(string id) => All.FirstOrDefault(a => a.Id == id);

    public static readonly IReadOnlyList<AbilityDefinition> Common = All.Where(a => a.Rarity == AbilityRarity.Common).ToList();
    public static readonly IReadOnlyList<AbilityDefinition> Rare = All.Where(a => a.Rarity == AbilityRarity.Rare).ToList();
}

// ─────────────────────────────────────────────────────────────────────────────
//  Race Data
// ─────────────────────────────────────────────────────────────────────────────
public static class RaceData
{
    public static readonly IReadOnlyList<RaceDefinition> All =
    [
        new("Human", Race.Human, "Human", "Adaptable and versatile.", 4, 1, 1, 1, 0),
        new("Symbiote", Race.Symbiote, "Symbiote", "Bonded with a living organism.", 5, 3, 0, 0, 0),
        new("Dwarf", Race.Dwarf, "Dwarf", "Short, stout, and stubborn.", 5, 0, 0, 0, 1),
        new("Steelborn", Race.Steelborn, "Steelborn", "Forged from metal and determination.", 4, 0, 3, 0, 0),
        new("Replicant", Race.Replicant, "Replicant", "Synthetic, with a second chance at life.", 3, 0, 0, 0, 0, RacePassiveType.ReplicantRevive),
        new("Brittlebone", Race.Brittlebone, "Brittlebone", "Fragile frame, sharp mind.", 3, 0, 0, 4, 0),
        new("Silent", Race.Silent, "Silent", "Communicates without words.", 3, 0, 0, 0, 0, RacePassiveType.SilentBoostRandomSlot),
    ];

    public static RaceDefinition Get(Race race) => All.First(r => r.Race == race);
    public static RaceDefinition? Get(string id) => All.FirstOrDefault(r => r.Id == id);
}

// ─────────────────────────────────────────────────────────────────────────────
//  Deployable Data
// ─────────────────────────────────────────────────────────────────────────────
public static class DeployableData
{
    public static readonly IReadOnlyList<DeployableDefinition> All =
    [
        new("EmptySlotShield", "Emergency Shields", "Replace all empty slots with Shield 5.", DeployableEffectType.ReplaceEmptySlotsShieldFive),
        new("MoraleInvuln", "Kamikaze Drive", "Lose 10% morale; become invulnerable this page.", DeployableEffectType.LoseTenPercentMoraleInvulnerable),
        new("HealQuarter", "Hull Patches", "Heal 25% of max Hull.", DeployableEffectType.HealTwentyFivePercent),
        new("MegaBoost", "Boost Drive", "Boost 100% in all lanes this page.", DeployableEffectType.BoostHundredAllLanes),
        new("Unexhaust", "Stimulant Pack", "Unexhaust all crew members; set fatigue to zero.", DeployableEffectType.UnexhaustAllCrewZeroFatigue),
        new("NoFatigue", "Focus Field", "Crew members do not gain fatigue this page.", DeployableEffectType.NoFatigueThisPage),
        new("EscapeCombat", "Emergency Fold", "Immediately escape a non-boss combat; all crew gain −1 Res.", DeployableEffectType.EscapeNonBossCombat),
        new("Redraw", "Tactical Override", "Redraw all formations for this page.", DeployableEffectType.RedrawFormations),
        new("GainMorale", "Morale Broadcast", "Gain 20% morale.", DeployableEffectType.GainTwentyPercentMorale),
        new("ClearLockOn", "Signal Jammer", "Reduce lock-on in all lanes to zero.", DeployableEffectType.ReduceAllLockOnToZero),
        new("CoinBoost", "Supply Cache", "Increase your current coin total by 30%.", DeployableEffectType.IncreaseCoinThirtyPercent),
        new("ChargeBurst", "Capacitor Bank", "Charge 30.", DeployableEffectType.ChargeThirty),
        new("CureNegTraits", "Decontamination Pod", "Remove all negative traits from all crew members.", DeployableEffectType.CureNegativeTraits),
        new("DestroyGlitch", "Debug Protocol", "Destroy a random equipped Glitch.", DeployableEffectType.DestroyRandomGlitch),
    ];

    public static DeployableDefinition? Get(string id) => All.FirstOrDefault(d => d.Id == id);
}

// ─────────────────────────────────────────────────────────────────────────────
//  Mod Data
// ─────────────────────────────────────────────────────────────────────────────
public static class ModData
{
    public static readonly IReadOnlyList<ModDefinition> All;
    public static readonly IReadOnlyList<ModDefinition> Glitches;
    public static readonly IReadOnlyList<ModDefinition> NonGlitches;

    static ModData()
    {
        var hull = SystemType.Hull; var eng = SystemType.Engine;
        var comp = SystemType.Computers; var cab = SystemType.Cabin;
        var S = ModSize.S; var M = ModSize.M; var L = ModSize.L;

        All =
        [
            // ─ Hull Mods ─
            new("HullRegen", "Hull Regen", "Regenerate 1 Hull every page.", hull, S, false, ModEffectType.HullRegenOnePerPage),
            new("ReduceDmg1", "Ablative Plating", "Reduce all incoming Hull damage by 1.", hull, S, false, ModEffectType.ReduceAllDamageByOne),
            new("CoinOnHullDmg", "Salvage Protocol", "Whenever the Hull takes damage, gain 5 coin.", hull, S, false, ModEffectType.GainCoinOnHullDamage, 5),
            new("LowHullPlusEl", "Backup Systems", "While Hull < 25%, crew get +1 Effect Level.", hull, S, false, ModEffectType.CrewPlusElAtLowHull),
            new("DmgCrewPlusEl", "Battle Focus", "When Hull takes damage, a random crew member gets +1 EL this combat.", hull, S, false, ModEffectType.CrewPlusElWhenHullDamaged),
            new("ReduceDmg33", "Composite Hull", "Reduce all incoming damage by 33%.", hull, L, false, ModEffectType.ReduceAllDamageThirtyThreePercent),
            new("EmptySlotHeal2", "Passive Repair", "Empty formation slots heal 2.", hull, S, false, ModEffectType.EmptySlotHealTwo, 2),
            new("ExhaustHeal2", "Sympathy Circuit", "When a crew member exhausts, heal 2.", hull, S, false, ModEffectType.HealTwoWhenCrewExhausts, 2),
            new("FullHullPlusEl", "Pristine Systems", "While Hull at 100%, crew get +1 Effect Level.", hull, M, false, ModEffectType.CrewPlusElAtFullHull),
            new("ChargeWhenDmg", "Pain Drive", "When Hull takes damage, charge that much in all lanes.", hull, L, false, ModEffectType.ChargeWhenHullDamaged),
            new("RetainShield25", "Shield Buffer", "Retain 25% of Shield as next page's starting Shield.", hull, M, false, ModEffectType.RetainTwentyFiveShield),
            new("ImmuneFirstPage", "Armor Up", "Immune to damage on the first page of combat.", hull, L, false, ModEffectType.ImmuneDamageFirstPage),
            new("HackGrantsShield", "Dual Processor", "Whenever you Hack, gain that amount of Shield.", hull, M, false, ModEffectType.HackGrantsShield),
            new("BoostShieldHeal", "Amplified Repairs", "Boost affects shielding and healing 20% more.", hull, S, false, ModEffectType.BoostAffectsShieldHealMore),
            new("LeftoverShields", "Shield Overflow", "Leftover shields are retained.", hull, L, false, ModEffectType.LeftoverShieldsRetained),

            // ─ Engine Mods ─
            new("MaxLockOnBoost", "Focused Fire", "Lanes at max lock-on get +100% Boost.", eng, S, false, ModEffectType.MaxLockOnLaneBoost),
            new("EmptySlotCharge2", "Idle Power", "Empty slots charge 2.", eng, M, false, ModEffectType.EmptySlotChargeTwo, 2),
            new("PassiveCharge150", "Enhanced Generator", "Passive charge generation +50%.", eng, M, false, ModEffectType.PassiveChargePlusFiftyPercent),
            new("ExhaustCharge2", "Exhaust Surge", "When a crew member exhausts, charge 2 in connected lanes.", eng, S, false, ModEffectType.ExhaustCrewChargeTwoConnected, 2),
            new("AllCrewCharge1", "Distributed Power", "All crew gain +Charge 1 ability.", eng, S, false, ModEffectType.AllCrewGainChargeOne),
            new("ChargeThreshLowerBoss", "Boss Protocol", "Charge thresholds are 10% lower in boss battles.", eng, M, false, ModEffectType.ChargeThresholdLowerBoss),
            new("ChargingShields", "Feedback Shield", "Charging also Shields for 10% of that value.", eng, M, false, ModEffectType.ChargingAlsoShields),
            new("EscapeHeal10", "Victory Drive", "Escaping combat heals 10% Hull.", eng, S, false, ModEffectType.EscapeHealTen),
            new("EscapeGetDeployable", "Trophy Run", "Escaping combat grants a random deployable.", eng, M, false, ModEffectType.EscapeGainDeployable),
            new("SmallDamagePrevented", "Shock Absorber", "If Threat would deal < 5 damage, prevent it.", eng, S, false, ModEffectType.SmallDamagePrevented),
            new("BaseCharge150", "Overclocked Engine", "Base engine charge +50%.", eng, L, false, ModEffectType.BaseEngineChargePlusFifty),
            new("NoActionDoublePassive", "Patient Charger", "If no charge actions on a page, passive charge doubles.", eng, L, false, ModEffectType.NoActionDoublePassiveCharge),
            new("EscapeMorale", "Victory Broadcast", "Escaping combat raises morale by 10%.", eng, S, false, ModEffectType.EscapeMoraleUp),
            new("NegChargeDisabled", "Charge Stabilizer", "Negative charge disabled in lanes.", eng, S, false, ModEffectType.NegativeChargeDisabled),
            new("BoostPersists50", "Sustained Boost", "Boost only loses 50% of its value each page.", eng, L, false, ModEffectType.BoostPersistsFiftyPercent),

            // ─ Computers Mods ─
            new("CantBeHacked", "Firewall", "You can't be hacked.", comp, S, false, ModEffectType.CantBeHacked),
            new("EmptySlotShield2", "Defensive Grid", "Empty slots shield 2.", comp, M, false, ModEffectType.EmptySlotShieldTwo, 2),
            new("MaxLockOnMinus1", "Countermeasures", "Maximum lock-on reduced by 1.", comp, S, false, ModEffectType.MaxLockOnReducedByOne),
            new("BoostPerLockOn", "Targeting Matrix", "+10% Boost for every lock-on in lanes.", comp, S, false, ModEffectType.BoostPerLockOn),
            new("HackTwoStartLockOn", "Double Agent", "Hack 2 in all lanes every page; start with +1 lock-on all lanes.", comp, L, false, ModEffectType.HackTwoAllLanesStartWithLockOn),
            new("OneSlotBoosted", "Slot Optimizer", "One unboosted slot becomes Boosted each page.", comp, S, false, ModEffectType.OneSlotBoostedPerPage),
            new("OneSlotMinusFatigue", "Fatigue Manager", "One slot becomes MinusFatigue each page.", comp, S, false, ModEffectType.OneSlotMinusFatiguePerPage),
            new("BoostFlankLanes", "Flank Protocol", "+20% permanent Boost in Left and Right lanes.", comp, S, false, ModEffectType.PermanentBoostFlankLanes),
            new("BoostCenterLane", "Center Protocol", "+40% permanent Boost in Center lane.", comp, M, false, ModEffectType.PermanentBoostCenterLane),
            new("HackPlusOne", "Hack Amplifier", "+1 to every Hack.", comp, S, false, ModEffectType.HackPlusOne),
            new("PlusOneFormationReward", "Reward Scanner", "+1 formation choice after combat.", comp, L, false, ModEffectType.PlusOneFormationRewardChoice),
            new("PlusOneFormationDraw", "Tactical Options", "+1 formation drawn each page.", comp, L, false, ModEffectType.PlusOneFormationDrawPerPage),
            new("AllowRepeatForms", "Formation Cache", "Same formation can be drawn on consecutive pages.", comp, M, false, ModEffectType.AllowRepeatFormations),
            new("BoostStarter", "Starter Boost", "Boost all slots in starting formation.", comp, M, false, ModEffectType.BoostStarterFormation),
            new("BoostedSlotPlus25", "Power Boost", "Boosted slots boost 25% more.", comp, M, false, ModEffectType.BoostedSlotsPlusTwentyFiveBoost),
            new("MinusFatigueBoost25", "Comfort Drive", "−Fatigue slots also boost 25%.", comp, S, false, ModEffectType.MinusFatigueSlotsBoostedTwentyFive),
            new("HealSlotsPlusOne", "Medical Grid", "Heal slots heal 1 more.", comp, S, false, ModEffectType.HealSlotsPlusOne),
            new("RareForms2x", "Formation Archive", "Rare formations appear twice as often.", comp, S, false, ModEffectType.RareFormationsTwice),

            // ─ Cabin Mods ─
            new("ExhaustShield2", "Crew Shield Protocol", "When a crew member exhausts, Shield 2 in connected lanes.", cab, S, false, ModEffectType.ExhaustShieldTwoConnected, 2),
            new("CrewCantPanic", "Morale Officer", "Crew members cannot panic.", cab, S, false, ModEffectType.CrewCantPanic),
            new("EscapeInterest", "Interest Economy", "After escaping combat, gain 10% interest on coin.", cab, S, false, ModEffectType.EscapeInterestOnCoin),
            new("CrewElPerCoin", "Mercenary Mind", "Crew get +1 EL for every 100 coin (max +5).", cab, L, false, ModEffectType.CrewPlusElPerHundredCoin),
            new("NewCrewPlusStat", "Talent Agency", "New crew members get +1 to a random stat.", cab, S, false, ModEffectType.NewCrewPlusStat),
            new("DeathPlusTwoRes", "Sacrifice Pact", "When a crew member dies, all others gain +2 Res.", cab, M, false, ModEffectType.CrewDeathGrantsPlusTwoRes),
            new("ExhaustedRecover2", "Fast Recovery", "Exhausted crew recover after 2 pages instead of waiting.", cab, L, false, ModEffectType.ExhaustedRecoverAfterTwoPages),
            new("PlusCabinCap", "Expanded Cabin", "+1 max cabin capacity.", cab, M, false, ModEffectType.GainMaxCabinCapacity),
            new("NoDamageFromThreat", "Crew Shield", "Crew members do not take damage from Threat overflow.", cab, L, false, ModEffectType.CrewNoDamageFromThreat),
            new("FiveDiffRaces", "Diversity Bonus", "If 5 different races, all crew get +1 EL.", cab, S, false, ModEffectType.FiveDiffRacesPlusEl),
            new("OneCrew", "Solo Protocol", "If only one crew member, they can't die and get +5 Int/Cha/Res/+1 End.", cab, L, false, ModEffectType.OneCrewCantDie),
            new("FirstCrewNoFatigue", "First Responder", "The first crew member used each page gains no fatigue.", cab, S, false, ModEffectType.FirstCrewNoFatigue),
            new("CabinChaIsRes", "Social Drive", "Use Cha instead of Res for Effect Level.", cab, M, false, ModEffectType.CabinChaInsteadOfRes),
            new("TwoFatiguePlusTwoEl", "Under Pressure", "Crew with 2+ fatigue get +2 Effect Level.", cab, S, false, ModEffectType.TwoOrMoreFatiguePlusTwoEl),
            new("HealAfterCombat", "Medical Bay", "Crew heal to full HP after combat.", cab, L, false, ModEffectType.CrewHealFullAfterCombat),
            new("CrewPlusElFirstPage", "Ready Crew", "Crew get +1 Effect Level on the first page of combat.", cab, S, false, ModEffectType.CrewPlusElFirstPage),
            new("CrewLoseFatigue", "Rest Rotation", "Crew lose an extra 1 fatigue each page.", cab, L, false, ModEffectType.CrewLoseOneFatiguePerPage),

            // ─ Hull Glitches ─
            new("GlHullDmg", "Cracked Plating", "Take +1 damage from all sources.", hull, S, true, ModEffectType.GlitchPlusOneDamageAll),
            new("GlHullNoHeal", "Rusted Veins", "Hull cannot be healed in combat.", hull, S, true, ModEffectType.GlitchHullCantHealInCombat),
            new("GlDoublePain", "Pain Conduit", "Take double damage from Pain slots.", hull, M, true, ModEffectType.GlitchDoubleDamageFromPain),
            new("GlLoseCoin", "Fuel Leak", "Lose 1 coin after every page.", hull, M, true, ModEffectType.GlitchLoseCoinEveryPage),

            // ─ Cabin Glitches ─
            new("GlMoraleCap", "Dampened Spirits", "Morale capped at 80%.", cab, S, true, ModEffectType.GlitchMoraleCapEighty),
            new("GlExhaustMinusRes", "Burnout", "Exhausted crew permanently get −1 Res.", cab, M, true, ModEffectType.GlitchExhaustedMinusOneRes),
            new("GlNewCrewMinus", "Bad Recruiter", "New crew members get −1 to a random stat.", cab, S, true, ModEffectType.GlitchNewCrewMinusOneStat),
            new("GlStartFatigue", "Pre-Deployment Stress", "Crew begin combat with 1 fatigue.", cab, S, true, ModEffectType.GlitchCrewStartOneFatigue),

            // ─ Computers Glitches ─
            new("GlFatigueSlot", "Memory Leak", "+1 Fatigue slot added each page.", comp, S, true, ModEffectType.GlitchAddFatigueSlot),
            new("GlPainSlot", "Corrupted AI", "+1 Pain slot added each page.", comp, S, true, ModEffectType.GlitchAddPainSlot),
            new("GlOneFormRemoval", "Scanner Error", "Only 1 option for formation removal.", comp, M, true, ModEffectType.GlitchOnlyOneFormationRemoval),
            new("GlFewerFormRewards", "Reward Filter", "Get 1 fewer formation option from combat rewards.", comp, M, true, ModEffectType.GlitchFewerFormationRewards),

            // ─ Engine Glitches ─
            new("GlChargePlus10", "Dampened Drive", "Charge thresholds +10%.", eng, S, true, ModEffectType.GlitchChargeThresholdPlusTen),
            new("GlLoseChargeOnDmg", "Feedback Drain", "Take damage → lose that much charge.", eng, M, true, ModEffectType.GlitchLoseChargeWhenDamaged),
            new("GlEnemyExtraAbility", "Broadcast Glitch", "All enemies get an extra random ability.", eng, S, true, ModEffectType.GlitchAllEnemiesExtraAbility),
            new("GlStartLockOn", "Navigation Fault", "Start combat with 1 lock-on in each lane.", eng, S, true, ModEffectType.GlitchStartOneLockOnAll),
        ];

        Glitches = All.Where(m => m.IsGlitch).ToList();
        NonGlitches = All.Where(m => !m.IsGlitch).ToList();
    }

    public static ModDefinition? Get(string id) => All.FirstOrDefault(m => m.Id == id);
    public static IReadOnlyList<ModDefinition> GetBySystem(SystemType system) =>
        All.Where(m => m.System == system && !m.IsGlitch).ToList();
    public static IReadOnlyList<ModDefinition> GetGlitchesBySystem(SystemType system) =>
        Glitches.Where(m => m.System == system).ToList();
}

// ─────────────────────────────────────────────────────────────────────────────
//  Event Data
// ─────────────────────────────────────────────────────────────────────────────
public static class EventData
{
    public static readonly IReadOnlyList<EventDefinition> All =
    [
        // Coin events
        new("COIN_GAIN_XL", "Windfall", EventStyle.Announcement, -2),
        new("COIN_GAIN_L", "Salvage Find", EventStyle.Announcement, -1),
        new("COIN_GAIN_M", "Trade Route", EventStyle.Announcement, 0),
        new("COIN_GAIN_XL_COSTLY", "Risky Venture", EventStyle.Announcement, +1),
        new("COIN_LOSE_S", "Minor Loss", EventStyle.Announcement, +2, RequiredCondition: "coin>=50"),
        new("COIN_LOSE_M", "Significant Loss", EventStyle.Announcement, +3, RequiredCondition: "coin>=80"),
        // Crew
        new("CREW_GAIN", "Gain a Crew Member", EventStyle.CrewCreation, -1, AlwaysInPool: true, MaxPerChapter: 1),
        new("CREW_DIES", "Crew Member Dies", EventStyle.Option, +5, RequiredCondition: "crew>=2"),
        // Shops
        new("SHOP_MOD", "Mod Shop", EventStyle.Shop, 0, AlwaysInPool: true),
        new("SHOP_DEPLOYABLE", "Deployable Shop", EventStyle.Shop, 0),
        new("SHOP_HEALING", "Healing Shop", EventStyle.Shop, 0),
        new("SHOP_MERCENARY", "Mercenary Shop", EventStyle.Shop, 0),
        new("SHOP_SABOTAGE", "Sabotage Shop", EventStyle.Shop, 0),
        new("SHOP_UPGRADE", "System Upgrade Shop", EventStyle.Shop, 0, RequiredCondition: "systems_not_maxed"),
        // Combat modifiers
        new("COMBAT_MOD_POS", "Strategic Advantage", EventStyle.Announcement, 0, MaxPerChapter: 1),
        new("COMBAT_MOD_NEG", "Strategic Disadvantage", EventStyle.Announcement, +2, MaxPerChapter: 1),
        // Formations
        new("FORMATION_REMOVE", "Scrap a Formation", EventStyle.Option, -1, RequiredCondition: "formations>1"),
        new("DEPLOYABLE_GAIN", "Supply Drop", EventStyle.Announcement, -1),
        // System upgrades (tearing disabled)
        new("SYSTEM_UPGRADE_ENGINE", "Engine Overhaul", EventStyle.Announcement, -2, TearingAllowed: false, MaxPerChapter: 2),
        new("SYSTEM_UPGRADE_HULL", "Hull Reinforcement", EventStyle.Announcement, -2, TearingAllowed: false, MaxPerChapter: 2),
        new("SYSTEM_UPGRADE_CABIN", "Cabin Expansion", EventStyle.Announcement, -2, TearingAllowed: false, MaxPerChapter: 2),
        new("SYSTEM_UPGRADE_COMPUTERS", "Computer Upgrade", EventStyle.Announcement, -2, TearingAllowed: false, MaxPerChapter: 2),
        // Training
        new("TRAINING_INT", "Intelligence Training", EventStyle.Option, 0, MaxPerChapter: 3),
        new("TRAINING_CHA", "Charisma Training", EventStyle.Option, 0, MaxPerChapter: 3),
        new("TRAINING_RES", "Resolve Training", EventStyle.Option, 0, MaxPerChapter: 3),
        new("TRAINING_END", "Endurance Training", EventStyle.Option, -2, MaxPerChapter: 3),
        // Traits
        new("TRAIT_NEGATIVE", "Character Flaw", EventStyle.Option, +3),
        new("TRAIT_POSITIVE", "Hidden Strength", EventStyle.Option, -1),
        // Morale (tearing disabled)
        new("MORALE_RESTORE_FULL", "Rally the Crew", EventStyle.Announcement, -3, TearingAllowed: false, MaxPerChapter: 1),
        new("MORALE_RESTORE_25", "Pep Talk", EventStyle.Announcement, -1, TearingAllowed: false, MaxPerChapter: 1),
        new("MORALE_LOSE_10", "Bad News", EventStyle.Announcement, +1, TearingAllowed: false, MaxPerChapter: 1),
        new("MORALE_LOSE_25", "Morale Crisis", EventStyle.Announcement, +3, TearingAllowed: false, MaxPerChapter: 1),
        // Mods
        new("MOD_GAIN", "Salvaged Component", EventStyle.Announcement, -2, MaxPerChapter: 1),
        new("GLITCH_GAIN", "System Corruption", EventStyle.Announcement, +4, MaxPerChapter: 1),
        // Formation modification
        new("FORMATION_UPGRADE", "Formation Enhancement", EventStyle.Option, -1, MaxPerChapter: 1, RequiredCondition: "has_upgradeable_formation"),
        new("FORMATION_DOWNGRADE", "Wear and Tear", EventStyle.Option, +1, MaxPerChapter: 1, RequiredCondition: "has_downgradeable_formation"),
        // Hull max (tearing disabled)
        new("HULL_MAX_GAIN", "Hull Expansion", EventStyle.Announcement, +1, TearingAllowed: false, MaxPerChapter: 1),
        new("HULL_MAX_LOSE", "Hull Damage", EventStyle.Announcement, -2, TearingAllowed: false, MaxPerChapter: 1, RequiredCondition: "hull_max>=11"),
        // Damage / Heal
        new("DAMAGE_S", "Minor Skirmish", EventStyle.Announcement, +1, MaxPerChapter: 1),
        new("DAMAGE_M", "Skirmish", EventStyle.Announcement, +2, MaxPerChapter: 1),
        new("DAMAGE_L", "Ambush", EventStyle.Announcement, +3, MaxPerChapter: 1),
        new("DAMAGE_XL", "Heavy Attack", EventStyle.Announcement, +4, MaxPerChapter: 1),
        new("HEAL_S", "Minor Repairs", EventStyle.Announcement, 0, MaxPerChapter: 1),
        new("HEAL_M", "Repairs", EventStyle.Announcement, -1, MaxPerChapter: 1),
        new("HEAL_L", "Major Repairs", EventStyle.Announcement, -2, MaxPerChapter: 1),
        new("HEAL_XL", "Full Refit", EventStyle.Announcement, -3, MaxPerChapter: 1),
        // Location special events
        new("LOC_DRIFT_BRITTLEBONE", "Drift Colony", EventStyle.CrewCreation, -1, RequiredLocationId: "TheDrift"),
        new("LOC_JENSONIA_STATCOPY", "Jensonian Bonding", EventStyle.Option, 0, RequiredLocationId: "Jensonia"),
        new("LOC_MYCELIA_SILENT", "Mycelia Commune", EventStyle.CrewCreation, -1, RequiredLocationId: "Mycelia"),
        new("LOC_SIGG_STEELBORN", "Sigg Foundry", EventStyle.CrewCreation, -1, RequiredLocationId: "Sigg"),
        new("LOC_MERKAJ_GLITCH", "Merkaj Technicians", EventStyle.Announcement, -2, RequiredLocationId: "Merkaj", RequiredCondition: "has_glitch"),
        new("LOC_ENJUKAN_REPLICANT", "Enjukan Fabricators", EventStyle.CrewCreation, -1, RequiredLocationId: "Enjukan"),
        new("LOC_ENJUKAN_CONVERT", "Replicant Conversion", EventStyle.Announcement, 0, RequiredLocationId: "Enjukan"),
        new("LOC_VOIDWATCH_RANDO", "Voidwatch Protocols", EventStyle.Announcement, 0, RequiredLocationId: "Voidwatch"),
        new("LOC_KHORUM_DWARF", "Khorum Clan", EventStyle.CrewCreation, -1, RequiredLocationId: "Khorum"),
        new("LOC_KRAETHOS_SYMBIOTE", "Kraethos Nest", EventStyle.CrewCreation, -1, RequiredLocationId: "Kraethos"),
    ];

    public static EventDefinition? Get(string id) => All.FirstOrDefault(e => e.Id == id);
    public static IReadOnlyList<EventDefinition> GetNeutralPool() =>
        All.Where(e => e.RequiredLocationId == null).ToList();
}

// ─────────────────────────────────────────────────────────────────────────────
//  Location Data
// ─────────────────────────────────────────────────────────────────────────────
public static class LocationData
{
    public static readonly IReadOnlyList<LocationDefinition> All =
    [
        new("DesolateAnchor003", "Desolate Anchor 003", "An abandoned jump anchor, silent and cold."),
        new("TheDrift", "The Drift", "A lawless sector of drifting hulks and Brittlebone settlers.", "LOC_DRIFT_BRITTLEBONE"),
        new("Jensonia", "Jensonia", "A world renowned for its communal bonds between crew.", "LOC_JENSONIA_STATCOPY"),
        new("Mycelia", "Mycelia", "A spore-world where the Silent evolved their unique bond.", "LOC_MYCELIA_SILENT"),
        new("Merkaj", "Merkaj", "A tech scavenger hub with excellent de-glitching services.", "LOC_MERKAJ_GLITCH"),
        new("Odi", "Odi", "A distant listening post at the edge of mapped space."),
        new("Jaguan", "Jaguan", "A volatile frontier planet, resource-rich and lawless."),
        new("Melflux", "Melflux", "A flux-tide research station orbiting a binary pulsar."),
        new("Hermestris", "Hermestris", "A mercantile hub for the outer systems."),
        new("Ludona", "Ludona", "A cooling world, once tropical, now icebound."),
        new("Sigg", "Sigg", "A forge world run by Steelborn clans.", "LOC_SIGG_STEELBORN"),
        new("Enjukan", "Enjukan", "The Replicant homeworld — a city of steel and memory.", "LOC_ENJUKAN_REPLICANT"),
        new("Voidwatch", "Voidwatch", "A military intelligence station in deep space.", "LOC_VOIDWATCH_RANDO"),
        new("Dunson4", "Dunson 4", "A mining colony on the fourth moon of a gas giant."),
        new("Khorum", "Khorum", "A mountain citadel world and ancestral home of the Dwarves.", "LOC_KHORUM_DWARF"),
        new("Macedon", "Macedon", "The imperial capital — fortress world of the Macedonian regime."),
        new("Kraethos", "Kraethos", "A dense jungle world, home of the Symbiote colonies.", "LOC_KRAETHOS_SYMBIOTE"),
        new("Kaimitrio", "Kaimitrio", "A neutral trading station at the crossroads of four sectors."),
        new("Luxion", "Luxion", "A wealthy pleasure world for the Macedonian elite."),
        new("Sol", "Sol", "The ancient birthworld, now under tight Macedonian control."),
    ];

    public static LocationDefinition? Get(string id) => All.FirstOrDefault(l => l.Id == id);
}

// ─────────────────────────────────────────────────────────────────────────────
//  Crew Name Pools
// ─────────────────────────────────────────────────────────────────────────────
public static class NamePool
{
    public static readonly IReadOnlyList<string> FirstNames =
    [
        "Avery", "Blake", "Cameron", "Daria", "Echo", "Farrow", "Galen", "Hira",
        "Iko", "Juno", "Kael", "Lyra", "Milo", "Nara", "Oryn", "Petra",
        "Quinn", "Riven", "Sable", "Tali", "Uris", "Vex", "Wren", "Xyla",
        "Yael", "Zael", "Ash", "Brix", "Cove", "Dune", "Ember", "Flint",
        "Gram", "Haven", "Iris", "Jade", "Knox", "Lark", "Maeve", "Nox",
        "Orin", "Pell", "Rook", "Sage", "Thane", "Umber", "Vale", "Wisp",
    ];

    public static readonly IReadOnlyList<string> LastNames =
    [
        "Voss", "Strand", "Calder", "Merrow", "Taine", "Holst", "Frey", "Ardent",
        "Corvin", "Dusk", "Elara", "Frost", "Grim", "Hale", "Irons", "Jax",
        "Krest", "Layne", "Mast", "Nord", "Onyx", "Pale", "Quill", "Rune",
        "Stone", "Thorn", "Ulan", "Vane", "Wick", "Xeris", "Yore", "Zane",
    ];

    public static readonly Dictionary<Race, IReadOnlyList<string>> Backstories = new()
    {
        [Race.Human] =
        [
            "A merchant pilot who lost their ship to Macedonian tariffs.",
            "A former colonial administrator fleeing a scandal.",
            "An idealistic rebel who joined the resistance early.",
            "A wandering engineer with no fixed port.",
            "Born on a generation ship, never known a fixed star.",
        ],
        [Race.Symbiote] =
        [
            "Bonded to their organism at birth; severed it to travel freely.",
            "A xenobiologist who chose to undergo the bonding ritual.",
            "Exiled from their colony for studying forbidden synthesis.",
            "Their bond-organism died in the Macedonian purge; seeking purpose.",
        ],
        [Race.Dwarf] =
        [
            "A forge-master from Khorum's deepest workshops.",
            "Disgraced in a duel; seeking redemption far from home.",
            "A clan historian carrying the last copies of their lineage records.",
            "Ran the blockade to escape Macedonian conscription.",
        ],
        [Race.Steelborn] =
        [
            "A Sigg foundry worker who discovered the forges made weapons for Macedon.",
            "A silver-tongued trader who amassed a fortune before losing it all.",
            "A negotiator whose last treaty failed catastrophically.",
            "Born into the ruling class, rejected it entirely.",
        ],
        [Race.Replicant] =
        [
            "A third iteration; the first two lives were lost to Macedon.",
            "A prototype model who escaped the factory floor.",
            "Spent years trying to find their original self's memories.",
            "Runs on degraded firmware — a blessing in some ways.",
        ],
        [Race.Brittlebone] =
        [
            "A Drift scholar whose archive was destroyed in a raid.",
            "An astronomer who calculated the Hyperion Shield's structure.",
            "A reclusive cryptographer recruited from their isolated lab.",
            "Fragile in body, but few think faster in the whole galaxy.",
        ],
        [Race.Silent] =
        [
            "Grew up in a Mycelia commune; speaks in colors and sensation.",
            "Separated from their colony by a jump-drive malfunction.",
            "A mediator who bridges worlds without a common tongue.",
            "The last of their bonded group; silence their constant companion.",
        ],
    };
}

// ─────────────────────────────────────────────────────────────────────────────
//  Charge Thresholds by Chapter
// ─────────────────────────────────────────────────────────────────────────────
public static class ChargeThresholds
{
    private static readonly int[] Values = [0, 50, 75, 200, 125, 150, 400, 225, 250, 650, 400];
    public static int Get(int chapter) => chapter is >= 1 and <= 10 ? Values[chapter] : 50;
    public static bool IsBoss(int chapter) => chapter is 3 or 6 or 9 or 10;
}

// ─────────────────────────────────────────────────────────────────────────────
//  Loot / Coin tier ranges
// ─────────────────────────────────────────────────────────────────────────────
public static class CoinRanges
{
    public static (int min, int max) S => (30, 50);
    public static (int min, int max) M => (50, 80);
    public static (int min, int max) L => (80, 110);
    public static (int min, int max) XL => (110, 150);

    public static (int min, int max) Damage(string tier) => tier switch
    {
        "S" => (4, 9), "M" => (10, 13), "L" => (14, 19), "XL" => (20, 25), _ => (4, 9)
    };
    public static (int min, int max) Threat(string tier) => tier switch
    {
        "S" => (3, 6), "M" => (7, 15), "L" => (16, 24), "XL" => (25, 36), "XXL" => (37, 55), _ => (3, 6)
    };
}

public static class SystemValues
{
    public static int StartingCharge(int level) => level switch { 0 => 0, 1 => 15, 2 => 40, 3 => 75, _ => 0 };
    public static int CrewCapacity(int level) => level switch { 0 => 4, 1 => 5, 2 => 6, 3 => 7, _ => 4 };
    public static int FormationsPerPage(int level) => level switch { 0 => 2, 1 => 3, 2 => 4, 3 => 5, _ => 2 };
    public static int MaxHull(int level) => level switch { 0 => 80, 1 => 100, 2 => 120, 3 => 140, _ => 80 };
    public static int ModSlots(int level) => 3 + level;
}
