---
slug: stack
title: Tech stack
role: tech-stack choices
updated: "2026-08-06T22:01:45"
---

# Tech stack

## Среда исполнения

| Область | Значение | Источник |
|---------|----------|----------|
| Основной язык | C# (плюс `.tscn` сцены Godot) | `NeonWarfare.csproj:1`, `project.godot:19` |
| Runtime + версия | .NET `net10.0` (target framework) | `NeonWarfare.csproj:3` |
| Движок + версия | Godot 4.7.1, C# build, рендер Forward Plus | `project.godot:19`, `NeonWarfare.csproj:1` (`Godot.NET.Sdk/4.7.1`) |
| Пакетный менеджер | NuGet (`<PackageReference>`) | `NeonWarfare.csproj:7-11` |
| Сборка | MSBuild / `Godot.NET.Sdk`; assembly `NeonWarfare` | `NeonWarfare.csproj:1`, `project.godot:22-24` |
| Решение | VS 2012 format, один проект `NeonWarfare` | `NeonWarfare.sln` |

> **Расхождение Intent vs. Reality:** README пишет «последняя версия» и для Godot, и для .NET. Реально зафиксированы **Godot 4.7.1** и **.NET 10 (`net10.0`)**. См. [[intent-vs-reality-version-drift]].

## Production-зависимости

Только три production-пакета + внутренняя библиотека KludgeBox.

| Зависимость | Версия | Роль | Источник |
|-------------|--------|------|----------|
| `Godot.NET.Sdk` | 4.7.1 (SDK) | C#-биндинги Godot, интеграция сборки, базовые типы сцен/узлов | `NeonWarfare.csproj:1` |
| `KludgeBox` | 3.3.3 | Внутренний фреймворк: DI (`Di`, `[Child]`/`[Parent]`/`[SceneService]`/`[Logger]`), логирование (Serilog), глобальный `Services`, сетевые утилиты, расширения узлов Godot, `NodeContainer`, `AbstractMultiplayerSpawner`, `MpSync`, stat-модификаторы, `ProcessDeadChecker`/`ProcessShutdowner` | `NeonWarfare.csproj:8` |
| `CommunityToolkit.Mvvm` | 8.4.0 | `[ObservableProperty]` source generator для моделей данных мира (`PlayerData`, `GeneralData`) — генерирует `INotifyPropertyChanged`, на который подписываются стораджи для сетевой синхронизации | `NeonWarfare.csproj:9` |
| `MessagePack` | 3.1.4 | Бинарная сериализация состояния мира для сетевых payloads (`byte[]`) и on-disk сейвов; `[MessagePackObject]`, `[Key(N)]`, `[IgnoreMember]` | `NeonWarfare.csproj:10` |

Транзитивно (через KludgeBox): **Serilog** (бэкенд логирования), **Humanizer** (гуманизация строк, напр. `.Humanize().Titleize()` в `Setting.cs:22`).

## Дев-тулчейн

| Инструмент | Назначение |
|------------|------------|
| Godot .NET editor | редактирование сцен, запуск, export-пресеты, `.po` локали |
| Rider / VS | рекомендуемая C# IDE (`.sln`/`.csproj` + `.idea/`) |
| [GodotUpdaterUI](https://github.com/AbroGames/GodotUpdaterUI/releases) | настраивает ENV Godot-пути в `launchSettings.json` |
| `.editorconfig` | UTF-8, LF, max line 120 для `*.cs` |

CI/CD, контейнеры, сканеры безопасности и перф-тесты **отсутствуют**.

## Ключевые команды

```bash
dotnet build NeonWarfare.sln                       # restore NuGet + собрать assembly "NeonWarfare"
# Запуск: Godot editor F5 (старт Root.tscn)
dotnet run --project NeonWarfare.csproj -- --server --headless --port 25566   # dedicated-сервер
# Multiplayer fast-test: Rider run configs
#   Server          : --server
#   Autoconnect (1) : --auto-connect --uid TestPlayer1 --nick TestPlayer1
#   Autoconnect (2) : --auto-connect --uid TestPlayer2 --nick TestPlayer2
```

Автоматизированных тестов **нет** (`docs/codebase/TESTING.md`).

## Конфигурация и среда

- **Источники:** `project.godot` (движок), `.editorconfig` (стиль), `export_presets.cfg` (Windows/Linux), `user://` runtime-файлы (JSON: `game-settings.json`, `dedicated-server-settings.json`, `resume-game.json`; бинарные сейвы `user://saves/<name>.bin`).
- **CLI-аргументы** (парсятся только в RootStarter'ах, `Scripts/Content/CmdArgs/`): `--server`, `--headless`, `--port`, `--savefile`, `--admin <uid>`, `--parent-pid`, `--no-hud`, `--world-render`, `--godot-log-push`, `--auto-start`, `--auto-connect`, `--auto-connect-ip/port`, `--nick`, `--uid`.
- **Ключевые настройки движка:** физика **30 тиков/сек**, гравитация отключена, linear damp 0, `physics_interpolation = true`; локали `en`/`ru`; стартовая сцена `Scenes/Root/Root.tscn`.
- Поддерживаемые ОС: **Windows и Linux**.

Подробнее: `docs/codebase/STACK.md`.
