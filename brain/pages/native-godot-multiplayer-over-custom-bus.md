---
id: native-godot-multiplayer-over-custom-bus
title: "Собственная шина пакетов удалена в пользу штатного SceneMultiplayer"
category: decision
status: active
tags: [networking, architecture]
created: "2026-08-06T22:03:01"
updated: "2026-08-06T22:04:40"
---

<!-- compiled_truth -->
## Что решено

Используется **штатный высокоуровневый мультиплеер Godot** (`SceneMultiplayer` + `ENetMultiplayerPeer`). Собственная шина пакетов (`[GamePacket]`, `SC_`/`CS_`-классы, `[EventListener]`) **полностью удалена** (`README.md:186-189`).

## Альтернативы

Самописная пакетная шина поверх низкоуровневого API.

## Обоснование

Уйти от поддержки собственного сетевого слоя в пользу стандартного, хорошо протестированного движкового, с интеграцией в дерево сцен (RPC, `MultiplayerSpawner`).

## Радиус последствий

- Вся сетевая коммуникация — через `[Rpc]` с явными режимами (`AnyPeer`/`Authority`, `CallLocal`), MessagePack-`byte[]` payloads; JSON поверх сети запрещён. См. architecture, flow.
- Сетевой клей между server/client-половинами Character сосредоточен в `CharacterSynchronizer` (только сеть, без логики).


## Timeline

- time: 2026-08-06T22:03:01
  kind: decision
  summary: "Created this page: Собственная шина пакетов удалена в пользу штатного SceneMultiplayer"
  source: "README.md:186-189; git history"
  affects: [native-godot-multiplayer-over-custom-bus]

- time: 2026-08-06T22:03:01
  kind: decision
  summary: "зафиксировано из README: переход на штатный Godot мультиплеер"
  source: "README + git history"
  affects: [native-godot-multiplayer-over-custom-bus]

- time: 2026-08-06T22:04:40
  kind: decision
  summary: "правка ссылок: корневые слаги без скобок (lint-links)"
  source: lint-links
  affects: [native-godot-multiplayer-over-custom-bus]
