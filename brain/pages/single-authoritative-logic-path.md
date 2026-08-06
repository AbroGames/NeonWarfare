---
id: single-authoritative-logic-path
title: "Singleplayer, host и dedicated-server выполняют одну серверную логику"
category: decision
status: active
tags: [architecture, networking, core]
created: "2026-08-06T22:03:00"
updated: "2026-08-06T22:04:40"
---

<!-- compiled_truth -->
## Что решено

`Net.IsServer()` возвращает `true` не только для настоящего сервера, но и для **singleplayer и главного меню** (процесс — сам себе авторитет). `false` — **только** когда процесс подключён как клиент к чужому серверу. Хелперы `Net.DoClient(...)` / `DoServerClient(...)` / `DoServerNotServer(...)` маршрутизируют по стороне.

## Альтернативы

Раздельные пути логики для singleplayer vs multiplayer (традиционный подход многих движков).

## Обоснование

Писать серверную логику один раз и не дублировать её для одиночного режима (`README.md:184`). Singleplayer сводится к «серверу в том же процессе без пира».

## Радиус последствий / риски

- **`IsServer()` глобальна и side-aware, а не чистая проверка пира.** Любой код, полагающий, что «`IsServer()` ⇒ есть реальный сетевой пир», ошибочен. Смягчается документированной конвенцией.
- **Менять осторожно:** это нагрузко-несущая семантика, пронизывающая весь код ролей. См. architecture §Known risks, [[network-shutdown-ordering-hazard]].


## Timeline

- time: 2026-08-06T22:03:00
  kind: decision
  summary: "Created this page: Singleplayer, host и dedicated-server выполняют одну серверную логику"
  source: "code: NetworkService.cs:33-42; README.md:182-184"
  affects: [single-authoritative-logic-path]

- time: 2026-08-06T22:03:00
  kind: decision
  summary: "зафиксировано из кода/README: один авторитарный путь логики"
  source: "code + README"
  affects: [single-authoritative-logic-path]

- time: 2026-08-06T22:04:40
  kind: decision
  summary: "правка ссылок: корневые слаги без скобок (lint-links)"
  source: lint-links
  affects: [single-authoritative-logic-path]
