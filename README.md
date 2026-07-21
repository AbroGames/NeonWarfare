# Neon Warfare

Neon Warfare — кооперативный top-down буллет-хелл на Godot и C#, где каждая сессия короткая, но
требует реальной слаженности между игроками: враги в бою каждый раз разные, и тактику нужно
подбирать на ходу. Игра — инди-проект с бесплатной версией, запускается на Windows/Linux/macOS.

---

## Документация

Индекс: тема → файл.  
Ключевые сущности в строках указаны намеренно,
чтобы файл находился поиском по имени класса или атрибута.

| Документ | Что внутри |
|---|---|
| [Дерево сцен](Docs/Arch/Scene-tree.md) | Иерархия «контейнер → содержимое», `NodeContainer`, правило «вызовы вниз, события вверх» |
| [Поток запуска](Docs/Arch/Startup-flow.md) | `RootStarter`, `GameStarter`, четыре режима игровой сессии |
| [Внедрение зависимостей](Docs/Arch/Dependency-injection.md) | `Di.Process(this)`, `[Child]`, `[Parent]`, `[SceneService]`, `[Logger]` |
| [Сеть](Docs/Arch/Networking.md) | RPC, спавн, рукопожатие клиента, `Net.IsServer()` / `IsClient()`, `DoServerClient` |
| [Данные и сохранения](Docs/Arch/Data-and-saves.md) | `Persistence` / `Temporary`, формат сейвов, MessagePack, пути на Windows и Linux |
| [Сущности](Docs/Arch/Entities.md) | `Character` и его подсистемы |
| [Сервисы](Docs/Arch/Services.md) | Глобальные сервисы и сервисы мира, `Services.*` |
| [Интерфейс](Docs/Arch/Ui.md) | Стек страниц меню, генерация экрана настроек |
| [Чат и команды](Docs/Arch/Chat-and-commands.md) | Чат, чат-команды |
| [Завершение работы](Docs/Arch/Shutdown.md) | Автосохранение при выходе, убийство дочерних процессов |
| [Соглашения по написанию кода](Docs/Code-style.md) | Пространства имён, пары RPC, сериализация, логирование, `double`/`float`, `.editorconfig` |
| [Структура репозитория](Docs/Repository-structure.md) | Что лежит в каждой папке |
| [Параметры командной строки](Docs/Cli-args.md) | Все флаги, `Scripts/Content/CmdArgs/` |
| [Локализация](Docs/Localization.md) | `Assets/Locales/*.po`, `messages.pot`, выбор локали |
| [Стек и зависимости](Docs/Stack.md) | Версии Godot и .NET, библиотеки |
| [Быстрый старт](Docs/Quick-start.md) | Настройка окружения, профили запуска Rider |