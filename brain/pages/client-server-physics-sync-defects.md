---
id: client-server-physics-sync-defects
title: "Клиент/сервер-синхронизация физики — активная нестабильная зона"
category: concept
status: active
tags: [physics, networking, debt, fragile]
created: "2026-08-06T22:03:31"
updated: "2026-08-06T22:03:31"
---

<!-- compiled_truth -->
## Суть

Самая хрупкая подсистема. Физика решается аналитически (`PhysicsCalculator`), и клиент с сервером должны согласовываться точно. Валидируется **только вручную** (тестов нет, см. [[no-automated-tests]]).

## Задокументированные дефекты

- Игрок проходит сквозь врагов на высокой скорости.
- `Mass` больше не влияет на таран (ramming).
- Server TPS-нагрузка растёт нелинейно: 300 static units → сервер 33% (пики 70%) vs клиент 14%; 10 units ≈ 4% с обеих сторон.
- Spawn-телепорт вызывает однокадровую вспышку в (0;0) (`Character.cs:44-46`, `WorldCharacterService.cs:32-38`).

## Тюнинг захардкожен

`_force=5000`, `GroundFriction=2000`, `AirFriction=0.04`, `MaxSpeed=1000` как readonly; `MaxSpeed`-кламп закомментирован (`PhysicsCalculator.cs:18-21,56-58`). Балансировка требует перекомпиляции.

## RemoteController

Экстраполяция, `DistanceForTeleport = 50`, `InertiaTime = 0.2` — видимое «прилипание» под лагом/потерями. Автор отмечает необходимость перехода экстраполяция → интерполяция (`RemoteController.cs:42`).

## Действие (предложено)

- Добавить `NeonWarfare.Tests` + unit-тесты на node-free логику (`CalculateAnalyticMotion`, билдеры `StatusEffect`, сериализацию).
- Решить split клиент/сервер-физики; фиксить spawn-position flow (передавать координаты через RPC или `MpSpawner.SpawnFunction`).
- Профилировать `_IntegrateForces` per unit с обеих сторон.

Связано: [[client-server-subsystem-split]], [[no-automated-tests]].


## Timeline

- time: 2026-08-06T22:03:31
  kind: decision
  summary: "Created this page: Клиент/сервер-синхронизация физики — активная нестабильная зона"
  source: "PhysicsCalculator.cs:9-35,62-77; RemoteController.cs:42-44; docs/codebase/CONCERNS.md"
  affects: [client-server-physics-sync-defects]

- time: 2026-08-06T22:03:31
  kind: decision
  summary: "зафиксировано из CONCERNS.md + TODO-плотности: известные дефекты физики"
  source: "code + docs/codebase/CONCERNS.md"
  affects: [client-server-physics-sync-defects]
