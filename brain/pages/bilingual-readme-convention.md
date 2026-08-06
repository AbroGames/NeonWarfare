---
id: bilingual-readme-convention
title: "Билингвальные README: README.md (RU, авторитетный) + README-EN.md (EN, зеркало)"
category: decision
status: active
tags: [docs, conventions]
created: "2026-08-06T22:17:49"
updated: "2026-08-06T22:17:58"
---

<!-- compiled_truth -->
## Что решено

Проект поддерживает **два README** в корне:
- `README.md` — русский, оригинал, **авторитетный источник истины** (объёмный дизайн-документ, ~530 строк).
- `README-EN.md` — английский перевод, создан 2026-08-06 как **зеркало** оригинала.

## Соотношение источников

- RU-README — первичный документ (задумка/архитектура пишутся здесь).
- EN-README — дословный faithful-перевод: та же структура, те же 12 разделов, те же admonitions, **идентификаторы verbatim** (имена классов, CLI-флаги, пути, ASCII-блоки, C#-сниппеты).

## Обоснование

README — основной архитектурный документ проекта (см. architecture, stack), на него ссылается мозг. Английское зеркало делает проект доступным не-русскоязычным контрибьюторам без потери точности технических деталей.

## Радиус последствий / правило для будущих правок

- **Любое изменение архитектурного содержания сначала в `README.md`, потом зеркалируется в `README-EN.md`** (или наоборот — но держать их синхронно).
- Не плодить расходящиеся версии: EN — зеркало, а не независимо эволюционирующий документ.
- Связано с расхождением версий «latest vs pinned» — см. [[intent-vs-reality-version-drift]] (актуально для обоих README).


## Timeline

- time: 2026-08-06T22:17:49
  kind: decision
  summary: "Created this page: Билингвальные README: README.md (RU, авторитетный) + README-EN.md (EN, зеркало)"
  source: "session 2026-08-06: создан README-EN.md как перевод README.md"
  affects: [bilingual-readme-convention]

- time: 2026-08-06T22:17:49
  kind: decision
  summary: "зафиксировано: проект ведёт два README, RU — источник истины"
  source: session 2026-08-06
  affects: [bilingual-readme-convention]

- time: 2026-08-06T22:17:58
  kind: decision
  summary: "правка ссылок: корневые слаги без скобок (lint-links)"
  source: lint-links
  affects: [bilingual-readme-convention]
