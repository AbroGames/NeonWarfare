# Поток запуска

[← README проекта](../../README.md)

```
Root._Ready()
  └── RootStarterManager.ChooseStarter()      Есть ли "--server" в OS.GetCmdlineArgs()?
        ├── ClientRootStarter                 Нет  → клиент
        └── DedicatedServerRootStarter        Да   → выделенный сервер

Далее в обоих случаях:
ClientRootStarter / DedicatedServerRootStarter
├── Init()       Общий BaseRootStarter.Init(): обработчик исключений, кэши, LoadingScreenService, I18N.
│                Затем специфичное в конкретном *RootStarter.Init(): Net.Init(), настройки, локаль, автомасштаб UI
│                
└── Start()      Запуск нужного сценария через Services.MainScene.*
```

Далее `MainSceneService` создаёт сцену `Game` и передаёт ей **стартер игры** — объект,
который знает, как именно поднять эту сессию:

```
Game.Init(BaseGameStarter starter) → starter.Init(game)
  ├── game.AddNetwork()         Создаёт Network (ENet + SceneMultiplayer)
  ├── game.AddWorld()           Создаёт World
  ├── game.AddHud() / game.AddServerHud()
  ├── ServerStartWorld()        На сервере: StartNewGame() или LoadGame()
  └── ClientStartWorld()        На клиенте: StartSyncWithServer()
```
