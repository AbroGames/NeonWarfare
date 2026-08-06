---
slug: mindmap
title: Feature mindmap
role: feature mindmap
updated: "2026-08-06T22:04:26"
---

# Feature mindmap

```mermaid
mindmap
  root((Neon Warfare))
    Точка входа
      Root.tscn
      RootStarterManager
        ClientRootStarter
        DedicatedServerRootStarter
    Игровая сессия Game
      Network ENet/SceneMultiplayer
      World
        Surfaces: SafeSurface / BattleSurface
        PersistenceData / TemporaryData
        World*Service
          StartStop
          Characters Player/Enemy
          DataSerializer
          Chat + Commands
          Performance Godot/Sharp/ENet/Ping
          MpSpawn
          Synchronizer
          DataSaveLoad
      GameStarters
        Singleplayer
        HostMultiplayer
        ConnectToMultiplayer
        HostDedicatedServerAndConnect
    Сущности Entity
      Characters
        CharacterController Player/Remote/AI
        CharacterStats server/client
        CharacterStatusEffects server/client
        CharacterSynchronizer только сетевой клей
        PhysicsCalculator аналитический решатель
      Walls scriptless StaticBody2D
    Глобальные сервисы Scripts/Service
      Settings GameSettings/DedicatedServerSettings
      SaveLoadService MessagePack
      MainSceneService
      NetworkService role-semantics
    UI Screen
      NewMenu page-stack PagesProvider
      Hud
      ServerHud
      LoadingScreen
    Системные подсистемы
      StatusEffect fluent-билдер + AddingPolicy
      Achievements система
      Локализация en/ru i18n
      AutoScale под разрешение
```

## Ветки признаков из макета

| Ветка | Назначение |
|-------|------------|
| Entry | выбор режима процесса (client/dedicated-server) |
| Game session | один матч: подъём сети, инстанцирование World, режим старта |
| World | мир-контейнер: поверхности, данные, мир-сервисы |
| Entity | игровые объекты; персонажи разделены на server/client пары |
| Global services | сквозные синглтоны через `Services` |
| UI | presentation-слой (без авторитарного состояния) |

> **Текущее состояние (см. background):** часть систем заложена, но не подключена — `NavigationService`/`Pathfinder` написаны, но не используются; `BattleSurface` пуст; `ClientPackedScenes` пуст; скиллы/атаки не реализованы (только заглушки ввода).
