# Сервисы

[← README проекта](../../README.md)

## Глобальные сервисы

Статический класс `Services` ([Scripts/Services.cs](../../Scripts/Services.cs)), доступен из любой
точки. Часть приходит из KludgeBox (`Di`, `Rand`, `Math`, `NodeTree`, `I18N`, `AutoScaling`,
`AssemblyCache`, `TypesMapping`, `ExceptionHandler`, `StringCompression`, `MembersScanner` — последний
проксирует `Di.MembersScanner`), часть — игровые:

| Сервис | Класс | Назначение |
|---|---|---|
| `Services.Net` | `NetworkService` | Роль процесса (`IsClient`/`IsServer`), хелперы `DoClient`, `DoServerClient` |
| `Services.MainScene` | `MainSceneService` | Переключение MainMenu ↔ Game, точки входа во все режимы, `Shutdown()` |
| `Services.LoadingScreen` | `LoadingScreenService` | Показ / скрытие экрана загрузки |
| `Services.GameSettings` | `GameSettingsService` | Настройки клиента + временные `--nick` / `--uid` |
| `Services.DedicatedServerSettings` | `DedicatedServerSettingsService` | Настройки выделенного сервера |
| `Services.MenuGameSettings` | `MenuGameSettingsService` | Мост между `GameSettings` и моделью экрана настроек |
| `Services.SaveLoad` | `SaveLoadService` | Файлы сохранений, `SaveException` / `LoadException` |
| `Services.LastGame` | `ResumableGameService` | Последняя сессия для кнопки «Продолжить» (`ResumableGame`) |
| `Services.Process` | `ProcessService` | Запуск дочернего процесса выделенного сервера |
| `Services.IconsStorage` | `IconsStorageService` | Идентификаторы иконок |

Имя поля в `Services` намеренно короче имени класса (`Services.SaveLoad` → `SaveLoadService`): суффикс
`Service` в точке вызова был бы шумом.

`Services.Di` и `Services.Net` дополнительно вынесены в `Services.Global` и подключены через
`global using static` — поэтому в коде пишется просто `Di.Process(this)` и `Net.IsServer()`.
Там же глобально доступны `Consts.Global` (`ServerId`, `BroadcastId`) и расширения Godot из KludgeBox
(вектора, цвета, камера, ноды — отсюда, например, `Vec2(x, y)`) — см.
[Scripts/GlobalUsings.cs](../../Scripts/GlobalUsings.cs). Новые глобальные импорты добавляются только
туда.

## Сервисы мира

Ноды-дети `World`. Сам `World` реализует `IServiceProvider` и в `_EnterTree()` регистрирует их в
словаре по типу — до того, как у детей отработает `_Ready()`. Это и делает возможным инжект
`[SceneService]` вверх по дереву.

| Сервис | Назначение |
|---|---|
| `WorldServerStartStopService` | Серверный старт: новая игра / загрузка, инициализация синхронизатора и команд |
| `WorldClientStartStopService` | Клиентский старт: синхронизация с сервером, экран загрузки, пинг |
| `WorldSynchronizerService` | Handshake клиента, валидация игрока, первичная передача мира |
| `WorldMultiplayerSpawnerService` | Навешивание `MultiplayerSpawner` на ноды |
| `WorldDataSaveLoadService` | Сохранение / загрузка, права на сохранение, автосейв |
| `WorldDataSerializerService` | (Де)сериализация `WorldPersistenceData` |
| `WorldChatService` | Чат, история чата, перехватчики |
| `WorldCommandService` | Чат-команды, автоподбор всех `ICommandProcessor` из сборки |
| `WorldPlayerService` / `WorldEnemyService` | Спавн игроков и ботов (общая база `WorldCharacterService`) |
| `WorldPerformanceService` | Godot / .NET / ENet / ping-метрики |
| `WorldFacadeService` | Фасад для частых сводных запросов (данные игрока, онлайн/офлайн, `IsAdmin`) |

> [!NOTE]
> `World` — хранилище сервисов. Каждый сервис может ссылаться на другие сервисы и является точкой
> взаимодействия с системой: вызывая его метод, ты должен получить консистентное состояние всей
> системы, а не только этого сервиса.

Имена **нод** этих сервисов в `World.tscn` идут без префикса `World` (`ChatService`, а не
`WorldChatService`) — см. [Дерево сцен](Scene-tree.md).
