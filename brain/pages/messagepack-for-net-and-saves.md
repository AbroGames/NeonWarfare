---
id: messagepack-for-net-and-saves
title: "MessagePack для сети и сейвов; JSON только для on-disk настроек"
category: decision
status: active
tags: [serialization, networking, data]
created: "2026-08-06T22:03:01"
updated: "2026-08-06T22:03:01"
---

<!-- compiled_truth -->
## Что решено

| Канал | Формат | Аннотации |
|-------|--------|-----------|
| Сеть (RPC payloads) | **MessagePack** `byte[]` | `[MessagePackObject]`, `[Key(N)]`, `[IgnoreMember]` |
| Сохранения мира (`user://saves/*.bin`) | **MessagePack** | бинарный |
| On-disk настройки (`user://*.json`) | **JSON** (`System.Text.Json` + `ColorJsonConverter`) | человекочитаемость настроек |

## Альтернативы

JSON повсюду; или собственный формат.

## Обоснование

Компактный бинарный формат и для сети, и для сейвов (`IController.cs:11` `MovementData`, `ChatMessage.cs:6`, world-snapshot). JSON оставлен только там, где важна редактируемость/читаемость человеком — настройки.

## Радиус последствий / риски

- **Никакого JSON поверх сети** — только Godot-примитивы или MessagePack-`byte[]` (`README.md:464`).
- `MaxSyncPacketSize = 135000` ограничивает размер sync-пакета (`Network.cs:10`).
- ⚠️ **[TODO — подтвердить]** проверки размера/границ MessagePack-payloads от пиров (см. [[lan-trust-threat-model]]).


## Timeline

- time: 2026-08-06T22:03:01
  kind: decision
  summary: "Created this page: MessagePack для сети и сейвов; JSON только для on-disk настроек"
  source: "NeonWarfare.csproj:10; README.md:464-467"
  affects: [messagepack-for-net-and-saves]

- time: 2026-08-06T22:03:01
  kind: decision
  summary: "зафиксировано из csproj/README: единый бинарный формат"
  source: "csproj + README"
  affects: [messagepack-for-net-and-saves]
