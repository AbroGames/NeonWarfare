# Сущности

[← README проекта](../../README.md)

`Character` (`RigidBody2D`) собирается из независимых подсистем, у которых есть **серверная и
клиентская версия**:

| Подсистема | Сервер | Клиент |
|---|---|---|
| Характеристики | `CharacterStats` | `CharacterStatsClient` |
| Статус-эффекты | `CharacterStatusEffects` | `CharacterStatusEffectsClient` |
| Управление | `CharacterController` (общий, источником данных может быть любая сторона) | — |

Выбор версии делается в `Character._Ready()` через `Net.DoServerClient(...)`.

Мостом между сторонами служит `CharacterSynchronizer` — нода, разбитая на `partial`-файлы по
подсистемам (`CharacterSynchronizer_Stats.cs`, `_Controller.cs`, `_StatusEffects.cs`), чтобы не
создавать много лишних нод.

> [!IMPORTANT]
> **В синхронизаторе не должно быть никакой логики, кроме сетевой.** Игровая логика живёт в
> подсистемах и в [сервисах мира](Services.md).

## Контроллеры

Реализации `IController`:

* `PlayerController` — локальный игрок: читает ввод, считает физику (`PhysicsCalculator`) и шлёт
  `MovementData` (позиция, поворот, скорость, монотонный `OrderId`);
* `RemoteController` — по умолчанию у всех чужих объектов: экстраполирует последний `MovementData`,
  игнорирует устаревшие пакеты по `OrderId`, при расхождении больше `DistanceForTeleport`
  телепортирует объект;
* `AiController` — наследник `PlayerController`, подменяющий источник «ввода» на `IAiControllerLogic`
  (`AiBattleControllerLogic`, `AiMoveControllerLogic`, `AiObserveControllerLogic`,
  `AiPatrolControllerLogic`).

Блокировки управления — `ControlBlocker`, набор преднастроенных констант (`MenuIsOpen`, `ChatIsOpen`,
`CharacterIsDead`, `CharacterIsStunned`, `CharacterIsRooted`, `CharacterIsSilenced`). Каждая блокирует
свою комбинацию из движения / поворота / скиллов; складываются в `ControlBlockerHandler`.

> [!IMPORTANT]
> Телепортироваться следует только через `CharacterController.Teleport()` — он корректно сбрасывает
> физику и интерполяцию.

## Характеристики

Перечисление `CharacterStat` — единственный источник истины по списку характеристик и их единицам
измерения, см. [CharacterStat.cs](../../Scenes/Entity/Characters/Stats/CharacterStat.cs). Оно
покрывает живучесть (`MaxHp`, `RegenHp`, `Armor`, `ArmorAbsorption`, `ReceivingHeal`), перемещение
(`MovementSpeed`, `RotationSpeed`, `Mass`) и скиллы (`SkillDamage`, `SkillHeal`, `SkillCooldown`,
`SkillCritChance`, …).

Значения складываются в `StatModifiersContainer<CharacterStat>` (класс из KludgeBox, в этом
репозитории его нет). Контейнер приватный и лежит внутри `CharacterStats` / `CharacterStatsClient`;
наружу торчат только прокси-методы `AddStatModifier` / `RemoveStatModifier` — они дополнительно
рассылают изменение по сети. Модификаторы бывают аддитивные и мультипликативные
(`StatModifier<CharacterStat>.CreateAdditive(...)`), а `CharacterStats` считает итоговое значение и
отдаёт наружу уже клампнутое. `Hp`, `DutyHp`, смерть и воскрешение живут там же.

## Статус-эффекты

`StatusEffect` с fluent-билдером (`Id`, `Tags`, `DisplayName`, `Description`, `IconName`, `Type`,
`IsVisual`, `Time`, `IsFinishCondition`) и политикой добавления `IAddingStatusEffectPolicy`
(`LimitByIdAddingPolicy`, `LimitByTagAddingPolicy`, `NoCheckAddingPolicy`,
`UpdateTimeAddingStatusEffectPolicy`).

Готовые реализации лежат в `StatusEffects/Impl/`: яд, лечение, изменение стата, блокировка управления,
воскрешение и т.д.
