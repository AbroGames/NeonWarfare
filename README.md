# Neon Warfare

Neon Warfare — кооперативный top-down буллет-хелл на Godot и C#, где каждая сессия короткая, но
требует реальной слаженности между игроками: враги в бою каждый раз разные, и тактику нужно
подбирать на ходу. Игра — инди-проект с бесплатной версией, запускается на Windows/Linux/macOS.

---

## Документация

Перед нетривиальной задачей нужный файл читается **целиком**.  
Ключевые сущности в строках таблиц указаны намеренно,
чтобы файл находился поиском по имени класса или атрибута.

### Фундамент — читать при любой задаче

| Документ | Что внутри |
|---|---|
| [Дерево сцен](Docs/Arch/Scene-tree.md) | Иерархия «контейнер → содержимое», `NodeContainer`, правило «вызовы вниз, события вверх», именование нод |
| [Внедрение зависимостей](Docs/Arch/Dependency-injection.md) | `Di.Process(this)`, `[Child]`, `[Parent]`, `[SceneService]`, `[Logger]` |
| [Соглашения по написанию кода](Docs/Code-style.md) | Пространства имён, пары RPC, сериализация, логирование, `double`/`float`, `.editorconfig` |

### Устройство по областям — читать, когда задача трогает эту область

| Документ | Читать, когда трогаешь |
|---|---|
| [Сеть](Docs/Arch/Networking.md) | RPC, спавн, синхронизацию, рукопожатие клиента, `Net.IsServer()` / `IsClient()`, `DoServerClient` |
| [Данные и сохранения](Docs/Arch/Data-and-saves.md) | `Persistence` / `Temporary`, формат сейвов, MessagePack, пути на Windows и Linux |
| [Сервисы](Docs/Arch/Services.md) | Глобальные сервисы и сервисы мира, `Services.*` |
| [Поток запуска](Docs/Arch/Startup-flow.md) | `RootStarter`, `GameStarter`, четыре режима игровой сессии |
| [Завершение работы](Docs/Arch/Shutdown.md) | Автосохранение при выходе, убийство дочерних процессов |
| [Сущности](Docs/Arch/Entities.md) | `Character` и его подсистемы: контроллеры, характеристики, статус-эффекты |
| [Интерфейс](Docs/Arch/Ui.md) | Стек страниц меню, генерация экрана настроек, HUD, экран загрузки |
| [Чат и команды](Docs/Arch/Chat-and-commands.md) | Чат, перехватчики `IChatMessageInterceptor`, чат-команды `ICommandProcessor` |
| [Локализация](Docs/Localization.md) | Текст, видимый игроку: `Tr(KEY)`, `Assets/Locales/*.po`, `messages.pot`, выбор локали |

### Справочники и окружение — смотреть по мере надобности

| Документ | Что внутри |
|---|---|
| [Параметры командной строки](Docs/Cli-args.md) | Все флаги, `Scripts/Content/CmdArgs/` |
| [Структура репозитория](Docs/Repository-structure.md) | Что лежит в каждой папке |
| [Стек и зависимости](Docs/Stack.md) | Версии Godot и .NET, библиотеки, где искать исходники KludgeBox |
| [Быстрый старт](Docs/Quick-start.md) | Настройка окружения, профили запуска Rider |

---

## Что читать под задачу

| Задача | Что читать |
|---|---|
| Добавить или изменить RPC | [Сеть](Docs/Arch/Networking.md) → [Соглашения](Docs/Code-style.md#rpc) |
| Добавить поле, попадающее в сохранение | [Данные и сохранения](Docs/Arch/Data-and-saves.md) → [Сеть](Docs/Arch/Networking.md) (первичный снимок мира) |
| Добавить сервис мира | [Сервисы](Docs/Arch/Services.md) → [Дерево сцен](Docs/Arch/Scene-tree.md) (именование нод без префикса `World`) → [DI](Docs/Arch/Dependency-injection.md) |
| Добавить режим игровой сессии | [Поток запуска](Docs/Arch/Startup-flow.md) → [Сеть](Docs/Arch/Networking.md) (роли процесса) |
| Добавить флаг командной строки | [Параметры командной строки](Docs/Cli-args.md) → [Поток запуска](Docs/Arch/Startup-flow.md) |
| Добавить чат-команду | [Чат и команды](Docs/Arch/Chat-and-commands.md) |
| Добавить страницу меню или настройку | [Интерфейс](Docs/Arch/Ui.md) → [Локализация](Docs/Localization.md) |
| Добавить стат, статус-эффект, подсистему персонажа | [Сущности](Docs/Arch/Entities.md) → [Сеть](Docs/Arch/Networking.md) |
| Добавить видимый игроку текст | [Локализация](Docs/Localization.md) |
| `[Child]` / `[SceneService]` пришли `null` | [DI](Docs/Arch/Dependency-injection.md) → [Дерево сцен](Docs/Arch/Scene-tree.md) |
| Код ведёт себя по-разному в одиночке и в сети | [Сеть](Docs/Arch/Networking.md) (`IsServer` / `IsClient`) → [Поток запуска](Docs/Arch/Startup-flow.md) |
| Не находится объявление типа (`NodeContainer`, `[Sync]`, `StatModifiersContainer<T>`) | [Стек](Docs/Stack.md) — исходников KludgeBox в репозитории нет, путь к ним в ENV `KLUDGEBOX_SRC` |
| Поднять сервер и клиента для проверки | [Быстрый старт](Docs/Quick-start.md) → [Параметры командной строки](Docs/Cli-args.md) |

---

## Точки входа в код

| Путь | Что это |
|---|---|
| [Scenes/Root/Root.cs](Scenes/Root/Root.cs) | Точка входа процесса, живёт всю сессию приложения |
| [Scenes/Game/Game.cs](Scenes/Game/Game.cs) | Одна игровая сессия: `Network`, `World`, `Hud` / `ServerHud`; создаётся заново на каждый вход в игру |
| [Scenes/Game/Starters/](Scenes/Game/Starters/) | Четыре стартера игровой сессии |
| [Scenes/World/World.cs](Scenes/World/World.cs) | Корень мира, `IServiceProvider`, реестр сервисов мира |
| [Scenes/World/Service/](Scenes/World/Service/) | Сервисы мира |
| [Scenes/World/Data/PersistenceData/WorldPersistenceData.cs](Scenes/World/Data/PersistenceData/WorldPersistenceData.cs) | Данные, попадающие в сохранение: `GeneralDataStorage`, `PlayerDataStorage` |
| [Scenes/World/Data/TemporaryData/WorldTemporaryData.cs](Scenes/World/Data/TemporaryData/WorldTemporaryData.cs) | Данные текущей сессии, в сохранение не идут, синхронизируются через `[Sync]` |
| [Scenes/World/Tree/WorldTree.cs](Scenes/World/Tree/WorldTree.cs) | Игровое дерево, переключение локаций: `SetSafeSurface()` / `SetBattleSurface()` |
| [Scenes/World/Tree/Surfaces/Safe/SafeSurface.cs](Scenes/World/Tree/Surfaces/Safe/SafeSurface.cs) | Мирный хаб — первая из двух главных локаций |
| [Scenes/World/Tree/Surfaces/Battle/BattleSurface.cs](Scenes/World/Tree/Surfaces/Battle/BattleSurface.cs) | Боевая зона — вторая из двух главных локаций |
| [Scenes/Entity/Characters/Character.cs](Scenes/Entity/Characters/Character.cs) | Персонаж (`RigidBody2D`) и все его подсистемы |
| [Scripts/Services.cs](Scripts/Services.cs) | Реестр глобальных сервисов |
| [Scripts/Consts.cs](Scripts/Consts.cs) | Глобальные константы, `Consts.TransferChannel` |
| [Scripts/Content/CmdArgs/](Scripts/Content/CmdArgs/) | `CommonArgs`, `ClientArgs`, `DedicatedServerArgs` |
| [Properties/launchSettings.json](Properties/launchSettings.json), [.run/](.run/) | Профили запуска Rider и Multi-Launch |
 