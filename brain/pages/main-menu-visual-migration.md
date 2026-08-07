---
id: main-menu-visual-migration
title: "Перенос оформления MainMenu из before-migration поверх Page-архитектуры"
category: decision
status: archived
tags: [main-menu, ui, migration, visual]
created: "2026-08-07T11:17:14"
updated: "2026-08-07T12:07:45"
---

<!-- compiled_truth -->
Перенос визуала MainMenu из `../NeonWarfare-before-migration` завершён. Неон-cyan визуал наложен поверх Page-архитектуры (`PagesProvider`/`PageContainer`/`IPage`), Page-система не трогалась. Задача GitHub #12 (epic *Creation of MainMenu*, milestone v0.2.0).

## Что реализовано

**4 файла СОЗДАНО:**
- `Assets/Materials/MainMenuWallMaterial.tres` — StandardMaterial3D teal, `albedo_texture`+`emission_texture` на `MainMenuBackgroundGrid.png`
- `Scenes/Screen/NewMenu/MainMenu/Background/CorridorSegment3D.tscn` — копия legacy (6 CSGBox3D), material перепривязан на `MainMenuWallMaterial.tres`
- `Scenes/Screen/NewMenu/MainMenu/Background/Background3D.cs` — namespace `NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Background`, `[Export][NotNull] _light`+`_material` + `Di.Process(this)` (НЕ legacy `NotNullChecker.CheckProperties` — тот отсутствует в проекте), `SetAccentColor` вызывается в `_Ready` с `Services.GameSettings.GetSettings().PlayerColor`
- `Scenes/Screen/NewMenu/MainMenu/Background/CorridorContainer.cs` — тот же namespace, константы legacy сохранены (`_coridorSpeed=0.05`, `_segmentLength=7`, `_corridorSegmentsCount=5`)

**2 файла ИЗМЕНЕНО:**
- `MainMenu.tscn` — `TextureRect` (синий gradient) заменён на `SubViewportContainer`→`SubViewport`→`Background3D` (OmniLight3D+Camera3D ortho fov85+CorridorContainer+WorldEnvironment fog). 3 seed-сегмента коридора.
- `MainPage.tscn` — заголовок: ProstoOne+Glow.gdshader+TextureRect → Play-Bold outline-only (font_size 68, fill alpha 0, outline 3 teal). Кнопки: зелёно-коричневые StyleBoxFlat → StyleBoxEmpty normal + cyan GradientTexture1D hover, Play-Regular font_size 20. Glow.gdshader удалён из MainPage полностью.

## Accent — решён (по умолчанию плана)

UI chrome (outline заголовка, hover кнопок) — **фиксированный teal** `Color(0,0.647059,0.647059,1)`. Accent из `GameSettings.PlayerColor` применяется **только к 3D-фону** через `Background3D.SetAccentColor` (AlbedoColor+Emission). Точное соответствие legacy. Полная пропагация accent на UI — опциональный follow-up.

## Конвенция DI (отличие от плана)

План в §4.3/§4.4 предполагал `[Export][NotNull]` + `NotNullChecker.CheckProperties(this)`. В текущем проекте `NotNullChecker` **отсутствует** — проверка `[NotNull]` делается через `Di.Process(this)` (KludgeBox DI). Использован паттерн `[Export][NotNull]` + `Di.Process(this)` + `using KludgeBox.DI.Requests.NotNullCheck`. Build: 0 CS-ошибок.

## Верификация

- C# build: 0 ошибок компиляции (единственная MSB3374 — FS permission на `.godot/mono/temp/obj`, owned by root, не связана с кодом)
- Сцены: `Glow.gdshader` удалён из MainPage (0 упоминаний), `TextureRect` удалён из MainMenu (0 упоминаний)
- Все ассеты-UID совпадают с планом (grid `uid://bjvq28tv5sfy2`, Play-Bold `uid://bedcgaalmrgab`, Play-Regular `uid://cir5jadbcsm8f`)
- **Требует ручной проверки в Godot-редакторе:** открыть сцены, дать реимпорт, проверить визуал коридора + навигацию кнопок

## Связанные страницы

- Архитектура клиент/сервер — [[client-server-subsystem-split]]
- Миграции Godot/KludgeBox — [[godot-version-migration-cadence]]


## Timeline

- time: 2026-08-07T11:17:14
  kind: decision
  summary: "Created this page: Перенос оформления MainMenu из before-migration поверх Page-архитектуры"
  source: "Изучение ../NeonWarfare-before-migration и обсуждение с пользователем; задача GitHub #12 (epic Creation of MainMenu)"
  affects: [main-menu-visual-migration]

- time: 2026-08-07T11:17:43
  kind: decision
  summary: "Создан план переноса неон-cyan оформления MainMenu из before-migration поверх существующей Page-архитектуры NewMenu; accent color настраиваемый из GameSettings.PlayerColor"
  source: brain update-truth
  affects: [main-menu-visual-migration]

- time: 2026-08-07T11:22:48
  kind: decision
  summary: "Полный самодостаточный план реализации записан в MainMenu-Visual-Migration-Plan.md для передачи другому агенту"
  source: brain update-truth
  affects: [main-menu-visual-migration]

- time: 2026-08-07T11:31:56
  kind: decision
  summary: "Реализация завершена: 3D-коридор, teal визуал, accent на 3D фон; UI chrome фиксирован teal"
  source: "Реализация по MainMenu-Visual-Migration-Plan.md"
  affects: [main-menu-visual-migration]

- time: 2026-08-07T11:32:01
  kind: decision
  summary: "Реализован перенос визуала MainMenu: 4 файла создано, 2 изменено; accent только на 3D-фон, UI chrome фиксирован teal"
  source: "Реализация по MainMenu-Visual-Migration-Plan.md; build чистый (0 CS-ошибок)"
  affects: [main-menu-visual-migration]

- time: 2026-08-07T12:07:45
  kind: reversal
  summary: "Реализация завершена; план удалён, таска закрыта. Финальное состояние зафиксировано в compiled_truth до архивации."
  source: brain archive-page
  affects: [main-menu-visual-migration]
