# Слои и дерево сцен

[← README проекта](../../README.md)

Проект построен как строгая иерархия "контейнер → содержимое". Каждый уровень знает только о своих потомках,
подмена содержимого выполняется через `NodeContainer`.

```
Root (Node2D)                                      Точка входа, живёт всю сессию приложения
├── MainSceneContainer                             Содержит MainMenu ИЛИ Game
│   └── MainMenu | Game
├── LoadingScreenContainer                         Экран загрузки поверх всего (CanvasLayer)
└── PackedScenes (RootPackedScenes)                Прототипы сцен, которые создаются в Root: Game, MainMenu, LoadingScreen

Game (Node2D)                                      Одна игровая сессия (одиночная или сетевая)
├── WorldContainer → World                         Контейнер содержит World
├── HudContainer                                   Содержит Hud ИЛИ ServerHud
│   └── Hud | ServerHud
├── PackedScenes (GamePackedScenes)                Прототипы сцен, которые создаются в Game: World, Hud, ServerHud
└── Network                                        Создаётся кодом, живёт вместе с Game

World (Node2D, IServiceProvider)                   Игровой мир и все его сервисы
├── Tree (WorldTree)                               Игровое дерево со всеми объектами
│   └── Surface (SafeSurface | BattleSurface)      Текущая локация
│       └── Character, Wall, ...                   Игровые объекты, синхронизируются MultiplayerSpawner
├── PersistenceData                                Данные, попадающие в сохранение
├── TemporaryData                                  Данные текущей сессии
├── Service                                        Сервисы живущие в рамках одной игровой сессии
├── SyncedPackedScenes                             Прототипы сцен, синхронизирующиеся при спавне с сервера на клиент
└── ClientPackedScenes                             Прототипы чисто клиентских (визуальных) сцен
```

> [!IMPORTANT]
> **Клиент и сервер используют одно и то же дерево сцен.**  
> Роль определяется в рантайме через `Net.IsServer()` / `Net.IsClient()` 
> и через хелперы `Net.DoClient(...)`, `Net.DoServerClient(...)`, `Net.DoServerNotServer(...)` и т.д.

Правило потоков управления: **вызовы идут вниз по дереву, события (`event` / сигналы) — вверх.**
Родитель знает о детях, ребёнок о родителе — нет (исключение — явный `[Parent]`-инжект в сервисах мира).
