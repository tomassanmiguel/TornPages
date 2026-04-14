// TypeScript mirrors of C# RenderState types

export type RunPhase = 'Prologue' | 'Chapter' | 'Epilogue';
export type AbilityType = 'Shield' | 'Hack' | 'Charge' | 'Boost' | 'Barrier' | 'Heal' | 'Fusion';
export type SlotType = 'Empty' | 'Boosted' | 'Pain' | 'Heal' | 'Growth' | 'Fatigue' | 'MinusFatigue' | 'Kill';
export type FormationRarity = 0 | 1 | 2 | 3 | 4; // Starting=0, Common=1, Uncommon=2, Rare=3, Unique=4
export type ModSize = 0 | 1 | 2; // S=0, M=1, L=2
export type SystemType = 0 | 1 | 2 | 3; // Engine=0, Cabin=1, Hull=2, Computers=3
export type EventStyle = 'Announcement' | 'Option' | 'Shop' | 'CrewCreation';
export type ShopType = 'Mod' | 'Deployable' | 'Healing' | 'Mercenary' | 'Sabotage' | 'Upgrade';
export type Race = 0 | 1 | 2 | 3 | 4 | 5 | 6;

export interface StatBlock {
  resolve: number;
  intelligence: number;
  charisma: number;
  endurance: number;
}

export interface AbilityRender {
  id: string;
  typeName: string;
  type: AbilityType;
  isDual: boolean;
  displayText: string;
}

export interface TraitRender {
  id: string;
  name: string;
  description: string;
  isNegative: boolean;
}

export interface CrewRender {
  id: string;
  name: string;
  raceName: string;
  race: Race;
  stats: StatBlock;
  hpCurrent: number;
  hpMax: number;
  fatigueBaseline: number;
  endurance: number;
  isDead: boolean;
  isPanicked: boolean;
  isExhausted: boolean;
  abilities: AbilityRender[];
  traits: TraitRender[];
  backstory: string;
  effectiveEffectLevel: number;
  permanentBonus: number;
}

export interface SlotRender {
  type: number;
  typeName: string;
  connectedLanes: string[];
  isDouble: boolean;
  chainId: string | null;
  slottedCrewId: string | null;
}

export interface FormationRender {
  id: string;
  name: string;
  rarity: FormationRarity;
  slots: SlotRender[];
  passiveDescription: string | null;
  isGreyedOut: boolean;
}

export interface ModRender {
  instanceId: string;
  modId: string;
  name: string;
  description: string;
  size: ModSize;
  isGlitch: boolean;
  system: SystemType;
}

export interface DeployableRender {
  instanceId: string;
  deployableId: string;
  name: string;
  description: string;
}

export interface SystemRender {
  name: string;
  type: SystemType;
  level: number;
  modSlots: number;
  slotsUsed: number;
  primaryValueLabel: string;
  primaryValue: number;
  installedMods: ModRender[];
}

export interface SystemsRender {
  engine: SystemRender;
  cabin: SystemRender;
  hull: SystemRender;
  computers: SystemRender;
  totalCharisma: number;
}

export interface RunStats {
  totalChargeGenerated: number;
  totalHackGenerated: number;
  totalShieldGenerated: number;
  totalDamageTaken: number;
  totalCrewDeaths: number;
  totalTears: number;
  totalCombats: number;
  chaptersCompleted: number;
}

export interface LeftPageRender {
  hullCurrent: number;
  hullMax: number;
  coin: number;
  morale: number;
  pagesRemaining: number;
  chapterNumber: number;
  actNumber: number;
  crew: CrewRender[];
  formations: FormationRender[];
  lastUsedFormationId: string | null;
  systems: SystemsRender;
  modStorage: ModRender[];
  deployables: DeployableRender[];
  stats: RunStats;
}

// ─── Right page types ──────────────────────────────────────────────────────────

export interface CrewCreationOptions {
  nameOptions: string[];
  raceOptions: Race[];
  backstoryOptions: string[];
  abilityOptions: { id: string; type: AbilityType; rarity: number }[];
  statOptions: StatBlock[];
  traitOptions: ({ id: string; name: string; description: string; isNegative: boolean } | null)[];
}

export interface CrewCreationSelections {
  nameIndex: number;
  raceIndex: number;
  backstoryIndex: number;
  abilityIndex: number;
  statIndex: number;
  traitIndex: number;
}

export interface PrologueCrewRight {
  $type: 'PrologueCrew';
  crewIndex: number;
  targetAbility: AbilityType;
  options: CrewCreationOptions;
  selections: CrewCreationSelections | null;
  isCommitted_: boolean;
  tearingAllowed: boolean;
}

export interface PrologueBoonRight {
  $type: 'PrologueBoon';
  modOptions: ModRender[];
  formationOptions: FormationRender[];
  deployableOptions: DeployableRender[];
  selectedModId: string | null;
  selectedFormationId: string | null;
  selectedDeployableId: string | null;
  isCommitted_: boolean;
}

export interface LocationRender {
  id: string;
  name: string;
  loreDescription: string;
  specialEventName: string | null;
}

export interface LocationSelectRight {
  $type: 'LocationSelect';
  options: LocationRender[];
  selectedLocationId: string | null;
  isCommitted_: boolean;
}

export interface EventCardRender {
  id: string;
  name: string;
  dramaticTension: number;
  effectSummary: string;
  tearingAllowed: boolean;
  style: EventStyle;
}

export interface BuildUpRight {
  $type: 'BuildUp';
  eventPool: EventCardRender[];
  selectedEventIds: string[];
  totalDT: number;
  dtThreshold: number;
  canReady: boolean;
  isCommitted_: boolean;
}

export interface ShopItemRender {
  itemId: string;
  name: string;
  description: string;
  cost: number;
  canAfford: boolean;
  category: string;
}

export interface ShopRight {
  $type: 'Event';
  shopType: ShopType;
  items: ShopItemRender[];
  currentCoin: number;
  isExited: boolean;
}

export interface EventRight {
  $type: 'Event';
  eventId: string;
  eventName: string;
  style: EventStyle;
  narrativeText: string;
  effectDescription: string;
  optionLabels: string[] | null;
  selectedOption: number | null;
  shopData: ShopRight | null;
  needsCrewDismissal: boolean;
  isCommitted_: boolean;
  tearingAllowed_: boolean;
}

export interface WeaponEffect {
  type: number;
  intValue: number;
  isReactive: boolean;
}

export interface WeaponRender {
  id: string;
  name: string;
  leftEffect: WeaponEffect;
  centerEffect: WeaponEffect;
  rightEffect: WeaponEffect;
  randomiseLanes: boolean;
  description: string;
}

export interface EnemyAbilityRender {
  id: string;
  name: string;
  description: string;
  isSelected: boolean;
}

export interface EnemySelectRight {
  $type: 'EnemySelect';
  lockedWeapon1: WeaponRender;
  lockedWeapon2: WeaponRender;
  weaponChoices: WeaponRender[];
  selectedWeaponId: string | null;
  abilityPool: EnemyAbilityRender[];
  selectedAbilityIds: string[];
  chargeThreshold: number;
  isCommitted_: boolean;
  isBoss: boolean;
  bossName: string | null;
  bossDescription: string | null;
}

export interface GeneratedIntentRender {
  laneIndex: number;
  laneName: string;
  weaponName: string;
  effectDescription: string;
  resolvedValue: number;
}

export interface SlotCombatRender {
  slotIndex: number;
  type: number;
  typeName: string;
  connectedLanes: string[];
  isDouble: boolean;
  slottedCrewId: string | null;
  slottedCrewName: string | null;
  forecastText: string | null;
}

export interface LaneRender {
  name: string;
  laneIndex: number;
  threat: number;
  shield: number;
  hack: number;
  charge: number;
  lockOn: number;
  burnStacks: number;
  barrierStacks: number;
  fusionStacks: number;
  slots: SlotCombatRender[];
}

export interface EnemyCombatRender {
  name: string;
  weapons: WeaponRender[];
  abilities: EnemyAbilityRender[];
  isBoss: boolean;
  superShieldCurrent: number | null;
  intents: GeneratedIntentRender[];
  intentsHidden: boolean;
}

export interface CombatModifierRender {
  name: string;
  isPositive: boolean;
}

export interface CombatForecast {
  forecastShield: number[];
  forecastThreat: number[];
  forecastHack: number[];
  forecastCharge: number[];
  forecastChargeGain: number;
  forecastHullDamage: number;
  forecastCoinGain: number;
}

export interface CombatRight {
  $type: 'Combat';
  enemy: EnemyCombatRender;
  chargeAccumulated: number;
  chargeThreshold: number;
  lockOn: number[];
  burnStacks: number[];
  barrierStacks: number[];
  fusionStacks: number[];
  lanes: LaneRender[];
  availableFormations: FormationRender[];
  activeFormationId: string | null;
  activeModifiers: CombatModifierRender[];
  awaitingReadyUp: boolean;
  isComplete: boolean;
  availableDeployables: DeployableRender[];
  forecast: CombatForecast | null;
  panickedCrewIds: string[];
  kidnappedCrewId: string | null;
  isPhantomPage: boolean;
  isInvulnerablePage: boolean;
  isCommitted_: boolean;
}

export interface CombatRewardRight {
  $type: 'CombatReward';
  formationOptions: FormationRender[];
  selectedFormationId: string | null;
  coinReward: number;
  isCommitted_: boolean;
}

export interface ChapterConclusionRight {
  $type: 'ChapterConclusion';
  narrativeText: string;
  chapterNumber: number;
  isCommitted_: boolean;
}

export interface EpilogueRight {
  $type: 'Epilogue';
  chaptersCompleted: number;
  totalDamageTaken: number;
  totalShieldGenerated: number;
  totalChargeGenerated: number;
  totalHackGenerated: number;
  totalCrewDeaths: number;
  totalTears: number;
  finalHull: number;
  finalCoin: number;
  finalMorale: number;
  survivorNames: string[];
  isCommitted_: boolean;
}

export type RightPageRender =
  | PrologueCrewRight
  | PrologueBoonRight
  | LocationSelectRight
  | BuildUpRight
  | EventRight
  | EnemySelectRight
  | CombatRight
  | CombatRewardRight
  | ChapterConclusionRight
  | EpilogueRight;

export interface RenderState {
  left: LeftPageRender;
  right: RightPageRender;
  deltas: Record<string, unknown>;
  isCurrentPage: boolean;
  pendingInterjections: string[];
  currentPageIndex: number;
  totalPages: number;
  runPhase: RunPhase;
  isVictory: boolean;
  isRunOver: boolean;
}

export interface ProfileSummary {
  profileId: string;
  name: string;
  hasActiveRun: boolean;
  chapterNumber: number | null;
}

// ─── Action types ─────────────────────────────────────────────────────────────
// Note: C# polymorphic discriminator is "actionType" (not "$type")

export interface PlayerAction {
  actionType: string;
  [key: string]: unknown;
}

// Prologue
export const PrologueCrewAttrAction = (attr: string, optionIndex: number): PlayerAction =>
  ({ actionType: 'SelectPrologueCrewAttr', attr, optionIndex });
export const ConfirmPrologueCrewAction = (): PlayerAction =>
  ({ actionType: 'ConfirmPrologueCrew' });
export const BoonSelectAction = (modId: string | null, formationId: string | null, deployableId: string | null): PlayerAction =>
  ({ actionType: 'BoonSelect', modId, formationId, deployableId });
export const ConfirmBoonAction = (): PlayerAction =>
  ({ actionType: 'ConfirmBoon' });

// Location / build-up
export const SelectLocationAction = (locationId: string): PlayerAction =>
  ({ actionType: 'SelectLocation', locationId });
export const ConfirmLocationAction = (): PlayerAction =>
  ({ actionType: 'ConfirmLocation' });
export const SelectEventAction = (eventId: string): PlayerAction =>
  ({ actionType: 'SelectEvent', eventId });
export const ReadyBuildUpAction = (): PlayerAction =>
  ({ actionType: 'ReadyBuildUp' });

// Events
export const EventOptionSelectAction = (optionIndex: number): PlayerAction =>
  ({ actionType: 'EventOptionSelect', optionIndex });
export const EventConfirmAction = (): PlayerAction =>
  ({ actionType: 'EventConfirm' });
export const EventContinueAction = (): PlayerAction =>
  ({ actionType: 'EventContinue' });
export const DismissCrewMemberAction = (crewId: string): PlayerAction =>
  ({ actionType: 'DismissCrewMember', crewId });
export const ShopBuyAction = (itemId: string, cost: number): PlayerAction =>
  ({ actionType: 'ShopBuy', itemId, cost });
export const ShopExitAction = (): PlayerAction =>
  ({ actionType: 'ShopExit' });

// Enemy select
export const SelectWeaponAction = (weaponId: string): PlayerAction =>
  ({ actionType: 'SelectWeapon', weaponId });
export const SelectEnemyAbilityAction = (abilityId: string): PlayerAction =>
  ({ actionType: 'SelectEnemyAbility', abilityId });
export const ConfirmEnemyAction = (): PlayerAction =>
  ({ actionType: 'ConfirmEnemy' });

// Combat
export const SelectFormationAction = (formationId: string): PlayerAction =>
  ({ actionType: 'SelectFormation', formationId });
export const SlotCrewAction = (crewId: string, slotIndex: number): PlayerAction =>
  ({ actionType: 'SlotCrew', crewId, slotIndex });
export const UnslotCrewAction = (slotIndex: number): PlayerAction =>
  ({ actionType: 'UnslotCrew', slotIndex });
export const ReadyUpAction = (): PlayerAction =>
  ({ actionType: 'ReadyUp' });
export const UseDeployableAction = (instanceId: string): PlayerAction =>
  ({ actionType: 'UseDeployable', instanceId });

// Rewards
export const SelectRewardAction = (formationId: string): PlayerAction =>
  ({ actionType: 'SelectReward', formationId });
export const ConfirmRewardAction = (): PlayerAction =>
  ({ actionType: 'ConfirmReward' });

// Navigation
export const ProceedAction = (): PlayerAction =>
  ({ actionType: 'Proceed' });
export const TearAction = (): PlayerAction =>
  ({ actionType: 'Tear' });
export const MoveModToStorageAction = (modInstanceId: string): PlayerAction =>
  ({ actionType: 'MoveModToStorage', modInstanceId });
export const MoveModFromStorageAction = (modInstanceId: string, system: number, slotIndex: number): PlayerAction =>
  ({ actionType: 'MoveModFromStorage', modInstanceId, system, slotIndex });
