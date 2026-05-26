# Сущности

[← README проекта](../../README.md)

`Character` (`RigidBody2D`) собирается из независимых подсистем, у которых есть **серверная и клиентская версия**:

| Подсистема | Сервер | Клиент |
|---|---|---|
| Характеристики | `CharacterStats` | `CharacterStatsClient` |
| Статус-эффекты | `CharacterStatusEffects` | `CharacterStatusEffectsClient` |
| Управление | `CharacterController` (общий, источником данных может быть любая сторона) | — |

Выбор версии делается в `Character._Ready()` через `Net.DoServerClient(...)`.
Мостом между сторонами служит `CharacterSynchronizer` — нода, разбитая на `partial`-файлы по подсистемам
(`CharacterSynchronizer_Stats.cs`, `_Controller.cs`, `_StatusEffects.cs`), чтобы не создавать много лишних нод.  
**В синхронизаторе не должно быть никакой логики, кроме сетевой.**

**Контроллеры** (`IController`):

* `PlayerController` — локальный игрок: читает ввод, считает физику (`PhysicsCalculator`) и шлёт
  `MovementData` (позиция, поворот, скорость, монотонный `OrderId`);
* `RemoteController` — по умолчанию у всех чужих объектов: экстраполирует последний `MovementData`,
  игнорирует устаревшие пакеты по `OrderId`, при расхождении больше `DistanceForTeleport` телепортирует объект;
* `AiController` — наследник `PlayerController`, подменяющий источник «ввода» на `IAiControllerLogic`
  (`AiBattleControllerLogic`, `AiMoveControllerLogic`, `AiObserveControllerLogic`, `AiPatrolControllerLogic`).

Блокировки управления — `ControlBlocker` (`MenuIsOpen`, `ChatIsOpen`, `CharacterIsDead`, `CharacterIsStunned`,
`CharacterIsRooted`, `CharacterIsSilenced`), каждая блокирует набор из движения / поворота / скиллов;
складываются в `ControlBlockerHandler`. Телепортироваться следует только через `CharacterController.Teleport()` —
он корректно сбрасывает физику и интерполяцию.

**Характеристики** — перечисление `CharacterStat` (`MaxHp`, `RegenHp`, `Armor`, `MovementSpeed`, `SkillDamage`, …)
хранится в  `StatModifiersContainer`. Там лежат аддитивные и мультипликативные модификаторы, а `CharacterStats`
считает итоговое значение и отдаёт наружу уже клампнутые значения. `Hp`, `DutyHp`, смерть и воскрешение живут там же.

**Статус-эффекты** — `StatusEffect` с fluent-билдером (`Id`, `Tags`, `DisplayName`, `Description`, `IconName`,
`Type`, `IsVisual`, `Time`, `IsFinishCondition`) и политикой добавления `IAddingStatusEffectPolicy`
(`LimitByIdAddingPolicy`, `LimitByTagAddingPolicy`, `NoCheckAddingPolicy`, `UpdateTimeAddingStatusEffectPolicy`).
Готовые реализации: яд, лечение, изменение стата, блокировка управления, воскрешение.
