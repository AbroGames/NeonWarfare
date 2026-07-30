# Entities

[← Project README](../../README.md)

`Character` (`RigidBody2D`) is assembled from independent subsystems that have a **server-side and a
client-side version**:

| Subsystem | Server | Client |
|---|---|---|
| Stats | `CharacterStats` | `CharacterStatsClient` |
| Status effects | `CharacterStatusEffects` | `CharacterStatusEffectsClient` |
| Control | `CharacterController` (shared, the data source can be either side) | — |

The version is chosen in `Character._Ready()` through `Net.DoServerClient(...)`.

The bridge between the sides is `CharacterSynchronizer` — a node split into `partial` files by
subsystem (`CharacterSynchronizer_Stats.cs`, `_Controller.cs`, `_StatusEffects.cs`), so as not to
create a lot of extra nodes.

> [!IMPORTANT]
> **The synchronizer must contain no logic other than networking logic.** The game logic lives in the
> subsystems and in the [world services](Services.md).

## Controllers

The implementations of `IController`:

* `PlayerController` — the local player: reads the input, computes the physics
  (`PhysicsCalculator`) and sends `MovementData` (position, rotation, speed, a monotonic `OrderId`);
* `RemoteController` — the default for all foreign objects: extrapolates the last `MovementData`,
  ignores stale packets by `OrderId`, and when the divergence exceeds `DistanceForTeleport`
  teleports the object;
* `AiController` — a descendant of `PlayerController` that substitutes the source of "input" with
  `IAiControllerLogic` (`AiBattleControllerLogic`, `AiMoveControllerLogic`,
  `AiObserveControllerLogic`, `AiPatrolControllerLogic`).

Control blocks — `ControlBlocker`, a set of preconfigured constants (`MenuIsOpen`, `ChatIsOpen`,
`CharacterIsDead`, `CharacterIsStunned`, `CharacterIsRooted`, `CharacterIsSilenced`). Each one blocks
its own combination of movement / rotation / skills; they are combined in `ControlBlockerHandler`.

> [!IMPORTANT]
> Teleporting should only be done through `CharacterController.Teleport()` — it resets the physics and
> the interpolation correctly.

## Stats

The `CharacterStat` enum is the single source of truth for the list of stats and their units of
measurement, see [CharacterStat.cs](../../Scenes/Entity/Characters/Stats/CharacterStat.cs). It covers
survivability (`MaxHp`, `RegenHp`, `Armor`, `ArmorAbsorption`, `ReceivingHeal`), movement
(`MovementSpeed`, `RotationSpeed`, `Mass`) and skills (`SkillDamage`, `SkillHeal`, `SkillCooldown`,
`SkillCritChance`, …).

The values are accumulated in a `StatModifiersContainer<CharacterStat>` (a class from KludgeBox, it is
not in this repository). The container is private and lives inside `CharacterStats` /
`CharacterStatsClient`; only the `AddStatModifier` / `RemoveStatModifier` proxy methods stick out —
they additionally broadcast the change over the network. Modifiers come in additive and multiplicative
kinds (`StatModifier<CharacterStat>.CreateAdditive(...)`), and `CharacterStats` computes the final
value and hands it out already clamped. `Hp`, `DutyHp`, death and resurrection live there too.

## Status effects

`StatusEffect` with a fluent builder (`Id`, `Tags`, `DisplayName`, `Description`, `IconName`, `Type`,
`IsVisual`, `Time`, `IsFinishCondition`) and an adding policy, `IAddingStatusEffectPolicy`
(`LimitByIdAddingPolicy`, `LimitByTagAddingPolicy`, `NoCheckAddingPolicy`,
`UpdateTimeAddingStatusEffectPolicy`).

The ready-made implementations live in `StatusEffects/Impl/`: poison, healing, a stat change, a
control block, resurrection and so on.
