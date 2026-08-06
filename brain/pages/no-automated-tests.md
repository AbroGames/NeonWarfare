---
id: no-automated-tests
title: "Автоматизированные тесты отсутствуют"
category: decision
status: active
tags: [testing, debt]
created: "2026-08-06T22:03:31"
updated: "2026-08-06T22:03:31"
---

<!-- compiled_truth -->
## Что наблюдается

Тестового проекта/директории/атрибутов `[Test]`/`[Fact]` нет нигде. Покрытие не определено. Вместо тестов — ad-hoc in-game тестирование через `Test1/2/3` debug-кнопки в HUD (`Hud.cs:42-45`, `World.Test1/2/3()` RPC в `World.cs:98-144`, помечены `//TODO Remove after tests`).

## Обоснование (вероятное)

Ранняя стадия активной разработки; приоритет — фичи, а не инфраструктура тестирования.

## Радиус последствий / риски

- Самая хрупкая подсистема (физика/синхронизация, см. [[client-server-physics-sync-defects]]) валидируется только вручную → регрессии уходят незамеченными.
- `Test1/2/3` RPC в production-коде — debug-поверхность и шум.

## Действие (предложено)

Добавить `NeonWarfare.Tests`; unit-тестировать node-free логику (`PhysicsCalculator.CalculateAnalyticMotion`, `StatusEffect`-билдеры, `ControlBlockerHandler`, serializer round-trips, command parsing). См. `docs/codebase/TESTING.md`.


## Timeline

- time: 2026-08-06T22:03:31
  kind: decision
  summary: "Created this page: Автоматизированные тесты отсутствуют"
  source: "NeonWarfare.sln (no test project); docs/codebase/TESTING.md"
  affects: [no-automated-tests]

- time: 2026-08-06T22:03:31
  kind: decision
  summary: "зафиксировано из CONCERNS.md/TESTING.md: тестового проекта нет"
  source: docs/codebase
  affects: [no-automated-tests]
