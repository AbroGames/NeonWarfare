---
id: intent-vs-reality-version-drift
title: "README пишет «последняя версия»; код пинит Godot 4.7.1 / net10.0"
category: decision
status: active
tags: [stack, docs, debt]
created: "2026-08-06T22:03:31"
updated: "2026-08-06T22:03:31"
---

<!-- compiled_truth -->
## Что наблюдается

- README §"Стек и зависимости" пишет «Последняя версия» и для Godot, и для .NET.
- Реально в сборке зафиксированы: `Godot.NET.Sdk 4.7.1` (`NeonWarfare.csproj:1`), `net10.0` (`NeonWarfare.csproj:3`), `project.godot:19` `PackedStringArray("4.7", "C#", "Forward Plus")`.

## Обоснование (вероятное)

README намеренно говорит «последняя», чтобы не отставать от релизов движка; реальные пины обновляются при миграциях.

## Радиус последствий / риски

- Контрибьюторы, идущие по README, могут поставить другую версию движка/runtime и получить непонятные ошибки.
- **Действие:** обновить README до явных пинов `Godot 4.7.1 (.NET)` + `net10.0` (предложено в `docs/codebase/CONCERNS.md` §1).

⚠️ **[ASK USER]** какой источник истины для документации — «последняя» или пин?


## Timeline

- time: 2026-08-06T22:03:31
  kind: decision
  summary: "Created this page: README пишет «последняя версия»; код пинит Godot 4.7.1 / net10.0"
  source: "README.md:33-46 vs project.godot:19, NeonWarfare.csproj:1,3"
  affects: [intent-vs-reality-version-drift]

- time: 2026-08-06T22:03:31
  kind: decision
  summary: "зафиксировано из CONCERNS.md: расхождение документации и пинов"
  source: docs/codebase/CONCERNS.md
  affects: [intent-vs-reality-version-drift]
