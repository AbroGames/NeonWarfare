---
id: godot-version-migration-cadence
title: "Регулярные миграции Godot (4.4.1 → 4.7.1) и KludgeBox (2.x → 3.3.3)"
category: decision
status: active
tags: [stack, maintenance, history]
created: "2026-08-06T22:03:32"
updated: "2026-08-06T22:03:32"
---

<!-- compiled_truth -->
## Что наблюдается (из git log)

Последовательные миграции движка: Godot **4.4.1 → 4.5.1 (w/ .NET 10) → 4.6 → 4.6.1 → 4.7 → 4.7.1**. Параллельно — миграции внутреннего `GodotTemplate` и библиотеки `KludgeBox` (**2.x → 3.0.2 → 3.2.2 → 3.3.3**). Текущий пин: `Godot.NET.Sdk 4.7.1`, `net10.0`.

## Сопутствующие рефакторинги при миграциях

- «Migrate to GodotTemplate», «Remove useless stuff from GodotTemplate» (#133) — вынос общего в шаблон.
- Перенос `AutoScale` из Root в KludgeBox; использование `type storage`, `StatModifier` из KludgeBox.
- 5 файлов `NeonWarfare.csproj.old*` в корне — ручные бэкапы вокруг миграций (долг, см. [[intent-vs-reality-version-drift]]).

## Обоснование

Поддержание актуальных версий движка/runtime; перенос переиспользуемой инфраструктуры во внутренний фреймворк/шаблон.


## Timeline

- time: 2026-08-06T22:03:32
  kind: decision
  summary: "Created this page: Регулярные миграции Godot (4.4.1 → 4.7.1) и KludgeBox (2.x → 3.3.3)"
  source: git log
  affects: [godot-version-migration-cadence]

- time: 2026-08-06T22:03:32
  kind: decision
  summary: "извлечено из git log: история миграций версий"
  source: git log
  affects: [godot-version-migration-cadence]
