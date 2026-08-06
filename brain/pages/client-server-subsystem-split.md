---
id: client-server-subsystem-split
title: "Server/client пары подсистем в одном дереве сцен"
category: decision
status: active
tags: [architecture, networking, entities]
created: "2026-08-06T22:03:01"
updated: "2026-08-06T22:03:01"
---

<!-- compiled_truth -->
## Что решено

Клиент и сервер **разделяют одно дерево сцен**; роль выбирается в рантайме. Поведение Character разбито на server/client пары подсистем: `CharacterStats`/`CharacterStatsClient`, `CharacterStatusEffects`/`...Client`, варианты контроллеров. Сетевой клей между половинами — `CharacterSynchronizer` (частичные классы `_*`), в котором **не должно быть логики, кроме сети** (`CharacterSynchronizer_Stats.cs:18`).

## Альтернативы

Полностью раздельные клиентские и серверные сцены/сборки.

## Обоснование

Одно дерево сцен упрощает синхронизацию и переисобщение кода; server authoritative, клиент зеркалирует. Связано с [[single-authoritative-logic-path]].

## Радиус последствий / риски

- Spawn/controller-init ordering тонкий (дефолтный `RemoteController`, teleport на spawn, интерполяция отключена 1 кадр) — хрупкая зона. См. [[client-server-physics-sync-defects]].
- Дублирование логики клампа между `CharacterStats` и `CharacterStatsClient` (долг).
- Открытый design-TODO: оставить `CharacterController` единым с флагами `syncToClient` или разбить на server/client + Facade.


## Timeline

- time: 2026-08-06T22:03:01
  kind: decision
  summary: "Created this page: Server/client пары подсистем в одном дереве сцен"
  source: "Character.cs:35-46; README.md:255-266"
  affects: [client-server-subsystem-split]

- time: 2026-08-06T22:03:01
  kind: decision
  summary: "зафиксировано из кода/README: разделение на server/client половины"
  source: "code + README"
  affects: [client-server-subsystem-split]
