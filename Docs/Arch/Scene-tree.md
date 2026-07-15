# Дерево сцен

[← README проекта](../../README.md)

Проект построен как строгая иерархия «контейнер → содержимое». Каждый уровень знает только о своих
потомках, подмена содержимого выполняется через `NodeContainer`.

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


**Клиент и сервер используют одно и то же дерево сцен.**  
Роль определяется в рантайме через `Net.IsServer()` / `Net.IsClient()`
и через хелперы `Net.DoClient(...)`, `Net.DoServerClient(...)`, `Net.DoServerNotServer(...)` и т.д.

Правило потоков управления: **вызовы идут вниз по дереву, события (`event` / сигналы) — вверх.**
Родитель знает о детях, ребёнок о родителе — нет (исключение — явный `[Parent]`-инжект в сервисах
мира).

## Именование нод

Ноды сервисов мира в `World.tscn` называются **без префикса `World`**: `ChatService`, `PlayerService`,
`SynchronizerService` — при том что классы называются `WorldChatService`, `WorldPlayerService`,
`WorldSynchronizerService`.

*Осознанное отклонение от правила «сцена и обработчик называются одинаково».* `[Child]` инжектит
**по имени поля**, поэтому имена нод обязаны совпадать с именами свойств в `World.cs`, а не с именами
классов. Префикс `World` в классе нужен, чтобы имя было однозначным в рамках всей сборки; внутри
`World.tscn` он был бы шумом (`World/WorldChatService`).

Практическое следствие: **переименование свойства в `World.cs` ломает инжект**, пока не переименована
нода в `World.tscn`, и наоборот. Компилятор это не ловит.

## Спавнер у `Tree`

Спавнеры для поверхностей навешиваются кодом (`WorldTree.SetSafeSurface()` /
`SetBattleSurface()` → `WorldMultiplayerSpawnerService.AddSpawnerToNode(...)`), и имя им даётся по
шаблону `<имя ноды>-MultiplayerSpawner`.

Сама нода `Tree` — исключение: она существует в `World.tscn` с самого начала, и момента, в который
сервис мог бы навесить на неё спавнер, просто нет. Поэтому её спавнер (`Tree-MultiplayerSpawner`,
`spawn_path = "../Tree"`) положен прямо в сцену, руками, по тому же шаблону имени.
