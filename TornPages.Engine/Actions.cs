using System.Text.Json.Serialization;

namespace TornPages.Engine;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "actionType")]
[JsonDerivedType(typeof(SelectPrologueCrewAttrAction),  "SelectPrologueCrewAttr")]
[JsonDerivedType(typeof(ConfirmPrologueCrewAction),      "ConfirmPrologueCrew")]
[JsonDerivedType(typeof(BoonSelectAction),               "BoonSelect")]
[JsonDerivedType(typeof(ConfirmBoonAction),              "ConfirmBoon")]
[JsonDerivedType(typeof(SelectLocationAction),           "SelectLocation")]
[JsonDerivedType(typeof(ConfirmLocationAction),          "ConfirmLocation")]
[JsonDerivedType(typeof(SelectEventAction),              "SelectEvent")]
[JsonDerivedType(typeof(ReadyBuildUpAction),             "ReadyBuildUp")]
[JsonDerivedType(typeof(EventOptionSelectAction),        "EventOptionSelect")]
[JsonDerivedType(typeof(EventConfirmAction),             "EventConfirm")]
[JsonDerivedType(typeof(EventContinueAction),            "EventContinue")]
[JsonDerivedType(typeof(DismissCrewMemberAction),        "DismissCrewMember")]
[JsonDerivedType(typeof(ShopBuyAction),                  "ShopBuy")]
[JsonDerivedType(typeof(ShopExitAction),                 "ShopExit")]
[JsonDerivedType(typeof(SelectWeaponAction),             "SelectWeapon")]
[JsonDerivedType(typeof(SelectEnemyAbilityAction),       "SelectEnemyAbility")]
[JsonDerivedType(typeof(ConfirmEnemyAction),             "ConfirmEnemy")]
[JsonDerivedType(typeof(SelectFormationAction),          "SelectFormation")]
[JsonDerivedType(typeof(SlotCrewAction),                 "SlotCrew")]
[JsonDerivedType(typeof(UnslotCrewAction),               "UnslotCrew")]
[JsonDerivedType(typeof(ReadyUpAction),                  "ReadyUp")]
[JsonDerivedType(typeof(UseDeployableAction),            "UseDeployable")]
[JsonDerivedType(typeof(SelectRewardAction),             "SelectReward")]
[JsonDerivedType(typeof(ConfirmRewardAction),            "ConfirmReward")]
[JsonDerivedType(typeof(ProceedAction),                  "Proceed")]
[JsonDerivedType(typeof(TearAction),                     "Tear")]
[JsonDerivedType(typeof(MoveModToStorageAction),         "MoveModToStorage")]
[JsonDerivedType(typeof(MoveModFromStorageAction),       "MoveModFromStorage")]
public abstract record PlayerAction;

// Prologue – crew creation (one attribute at a time)
public record SelectPrologueCrewAttrAction(string Attr, int OptionIndex) : PlayerAction;
//  Attr: "name" | "race" | "backstory" | "ability" | "stats" | "trait"
public record ConfirmPrologueCrewAction : PlayerAction;

// Prologue – boon
public record BoonSelectAction(string? ModId, string? FormationId, string? DeployableId) : PlayerAction;
public record ConfirmBoonAction : PlayerAction;

// Location Select
public record SelectLocationAction(string LocationId) : PlayerAction;
public record ConfirmLocationAction : PlayerAction;

// Build Up
public record SelectEventAction(string EventId) : PlayerAction;   // toggle
public record ReadyBuildUpAction : PlayerAction;

// Events
public record EventOptionSelectAction(int OptionIndex) : PlayerAction;
public record EventConfirmAction : PlayerAction;
public record EventContinueAction : PlayerAction;
public record DismissCrewMemberAction(string CrewId) : PlayerAction;

// Shop
public record ShopBuyAction(string ItemId, int Cost) : PlayerAction;
public record ShopExitAction : PlayerAction;

// Enemy Select
public record SelectWeaponAction(string WeaponId) : PlayerAction;
public record SelectEnemyAbilityAction(string AbilityId) : PlayerAction;   // toggle
public record ConfirmEnemyAction : PlayerAction;

// Combat – planning
public record SelectFormationAction(string FormationId) : PlayerAction;
public record SlotCrewAction(string CrewId, int SlotIndex) : PlayerAction;
public record UnslotCrewAction(int SlotIndex) : PlayerAction;
public record ReadyUpAction : PlayerAction;
public record UseDeployableAction(string DeployableInstanceId) : PlayerAction;

// Combat reward
public record SelectRewardAction(string FormationId) : PlayerAction;
public record ConfirmRewardAction : PlayerAction;

// General
public record ProceedAction : PlayerAction;
public record TearAction : PlayerAction;
public record MoveModToStorageAction(string ModInstanceId) : PlayerAction;
public record MoveModFromStorageAction(string ModInstanceId, SystemType System, int SlotIndex) : PlayerAction;
