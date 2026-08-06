---
id: achievements-system
title: "Система достижений (achievements)"
category: project
status: active
tags: [feature, ui]
created: "2026-08-06T22:03:43"
updated: "2026-08-06T22:04:40"
---

<!-- compiled_truth -->
## Что это

Система достижений, добавленная большим epic-коммитом `e6208e8` («Major update - Epic: add achievements system»). Включает базовые достижения, broadcast достижений в чат, тултипы, звуковое уведомление.

## Сопутствующие фичи того же эпика

- UI: инфо о текущей волне в BattleWorld HUD; фикс чат-истории; нейм-теги над игроками; переключение FPS/TPS по F3; connection/death-нотисы в чат; перевод настроек на английский.
- Core: реорганизация сцен; события для настроек; отключение профайлинга.

## Связи

Достижения транслируются через чат (`WorldChatService`), см. architecture. Иконки достижений в `Assets/Textures/UI/Icons/Achievements/` (включая крупные `.psd` — долг, см. roadmap).


## Timeline

- time: 2026-08-06T22:03:43
  kind: decision
  summary: "Created this page: Система достижений (achievements)"
  source: "git log (e6208e8)"
  affects: [achievements-system]

- time: 2026-08-06T22:03:43
  kind: decision
  summary: "извлечено из git log: epic-коммит внедрения достижений"
  source: git log
  affects: [achievements-system]

- time: 2026-08-06T22:04:40
  kind: decision
  summary: "правка ссылок: корневые слаги без скобок (lint-links)"
  source: lint-links
  affects: [achievements-system]
