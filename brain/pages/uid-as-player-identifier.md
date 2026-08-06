---
id: uid-as-player-identifier
title: "UID как идентификатор игрока вместо ника"
category: decision
status: active
tags: [networking, identity, history]
created: "2026-08-06T22:03:43"
updated: "2026-08-06T22:03:43"
---

<!-- compiled_truth -->
## Что решено

Идентификатор игрока — локально случайный **UID** (`UidGenerator.cs:16-19`), а не ник. Ник остаётся отображаемым именем. Коммиты `ecf3bbd` «Use uid insted of nick as player identifier», `f72f2dc` «Change adminNickname to adminUid everywhere», `cd45e39` «Add --uid flag to client», `ef4e16e` «Render uid in server hud».

## Альтернативы

Ник как первичный идентификатор.

## Обоснование

Ники могут совпадать/меняться; стабильный UID надёжнее для авторизации (админ-права по UID, см. [[lan-trust-threat-model]]), сохранений и трекинга игроков. `adminUid` теперь повсеместно вместо `adminNickname`.

## Радиус последствий

- CLI: `--admin <uid>` (server), `--uid <uid>` (client); меню генерирует UID и хранит в настройках (`game-settings.json`).
- ⚠️ UID локально случаен и **без аутентификации** — кто знает UID админа, может его задать. См. границы в [[lan-trust-threat-model]].


## Timeline

- time: 2026-08-06T22:03:43
  kind: decision
  summary: "Created this page: UID как идентификатор игрока вместо ника"
  source: "git log (ecf3bbd, f72f2dc, cd45e39)"
  affects: [uid-as-player-identifier]

- time: 2026-08-06T22:03:43
  kind: decision
  summary: "извлечено из git log: переход с ника на UID"
  source: git log
  affects: [uid-as-player-identifier]
