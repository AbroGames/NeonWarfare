---
id: lan-trust-threat-model
title: "Threat model: доверенная LAN, админ по UID без аутентификации"
category: concept
status: active
tags: [security, networking, decisions]
created: "2026-08-06T22:03:31"
updated: "2026-08-06T22:04:40"
---

<!-- compiled_truth -->
## Суть

Модель доверия рассчитана на **trusted LAN co-op**, а не на публичный интернет:

- **Админ по UID без аутентификации:** кто знает/выставит UID с `--admin <uid>` (или существующий админ через `/admin add`) — получает полный контроль (kick/ban, switch surface, save-контроль).
- **ENet плейнтекст-UDP** — без транспортного шифрования (`Network.cs:24-25`).
- **Сохранения/настройки** — незашифрованные локальные файлы (`user://*.json`, `*.bin`) — by design для локальной игры.
- В scan/исходниках **нет** захардкоженных секретов/ключей. Идентичность игрока — локально случайный UID (`UidGenerator.cs:16-19`).

## Границы приемлемости

- LAN / доверенная среда — приемлемо.
- Публичный интернет — **небезопасно** без password/token-гейта и шифрования транспорта.

⚠️ **[ASK USER]** предполагается ли публичный интернет-хостинг? От этого зависит, нужны ли аутентификация и шифрование.

Связано: background, [[messagepack-for-net-and-saves]].


## Timeline

- time: 2026-08-06T22:03:31
  kind: decision
  summary: "Created this page: Threat model: доверенная LAN, админ по UID без аутентификации"
  source: "DedicatedServerArgs.cs; WorldFacadeService.cs:76-83; WorldCommandService.cs:82-100; docs/codebase/CONCERNS.md"
  affects: [lan-trust-threat-model]

- time: 2026-08-06T22:03:31
  kind: decision
  summary: "зафиксировано из CONCERNS.md: модель доверия и её границы"
  source: docs/codebase/CONCERNS.md
  affects: [lan-trust-threat-model]

- time: 2026-08-06T22:04:40
  kind: decision
  summary: "правка ссылок: корневой слаг без скобок (lint-links)"
  source: lint-links
  affects: [lan-trust-threat-model]
