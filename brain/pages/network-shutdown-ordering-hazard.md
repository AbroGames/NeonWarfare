---
id: network-shutdown-ordering-hazard
title: "Hazard порядка shutdown: GetMultiplayer() = null после TreeExiting"
category: concept
status: active
tags: [networking, fragile, architecture]
created: "2026-08-06T22:03:32"
updated: "2026-08-06T22:04:40"
---

<!-- compiled_truth -->
## Суть

`SceneMultiplayer` перепривязывается к Game-узлу при каждом `Network._Ready` (чистый teardown), но multiplayer-идентичность привязана к lifetime Game. После `TreeExiting`: `GetMultiplayer()` = null и `Network` мог подменить peer на `OfflineMultiplayerPeer`.

## Обход (implemented)

Отдельный `WorldServerShutdowner` ловит `NotificationExitTree` и делает авто-сейв — именно потому, что в этот момент `GetMultiplayer()`/`Network` доверять нельзя.

## Правило для будущих правок

**Никогда** не доверять `GetMultiplayer()`/`Network` в `_exit_tree`-смежном коде; маршрутизировать через shutdowner. Game→MainMenu→Game-цикл полагается на корректное перестроение peer'а. См. flow §Shutdown, architecture §Known risks.


## Timeline

- time: 2026-08-06T22:03:32
  kind: decision
  summary: "Created this page: Hazard порядка shutdown: GetMultiplayer() = null после TreeExiting"
  source: "WorldServerShutdowner.cs:9-13; Network.cs:115-142; docs/codebase/CONCERNS.md"
  affects: [network-shutdown-ordering-hazard]

- time: 2026-08-06T22:03:32
  kind: decision
  summary: "зафиксировано из кода/CONCERNS.md: порядок shutdown и обход"
  source: "code + docs/codebase/CONCERNS.md"
  affects: [network-shutdown-ordering-hazard]

- time: 2026-08-06T22:04:40
  kind: decision
  summary: "правка ссылок: корневые слаги без скобок (lint-links)"
  source: lint-links
  affects: [network-shutdown-ordering-hazard]
