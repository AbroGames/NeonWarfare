# Стек и зависимости

[← README проекта](../README.md)

* **Godot:** последняя версия, рендер `Forward+`.
* **.NET:** последняя версия.

Пакеты игрового проекта (`NeonWarfare.csproj`):

| Пакет | Зачем |
|---|---|
| `KludgeBox` | Внутренняя библиотека с общим переиспользуемым кодом: DI, логирование, утилитные ноды Godot, утилитные классы |
| `CommunityToolkit.Mvvm` | Аннотация `[ObservableProperty]` для моделей данных |
| `MessagePack` | Бинарная сериализация состояния мира для сохранений и для передачи по сети |

Пакеты тестового проекта (`Tests/NeonWarfare.Tests/NeonWarfare.Tests.csproj`), подробнее — в
[Тестировании](Testing.md):

| Пакет | Зачем |
|---|---|
| `xunit.v3` | Фреймворк тестов: `[Fact]`, `[Theory]`, `Assert` |
| `xunit.runner.visualstudio` | Адаптер VSTest — без него `dotnet test` и Rider не находят тесты |
| `Microsoft.NET.Test.Sdk` | Хост VSTest, включает таргет `dotnet test` |

**Про KludgeBox.** Исходников библиотеки в этом репозитории нет — она подключается NuGet-пакетом,
поэтому поиск по репозиторию не найдёт объявлений её типов (`NodeContainer`,
`AbstractMultiplayerSpawner`, `ProcessShutdowner`, `ProcessDeadChecker`,
`StatModifiersContainer<T>`, атрибут `[Sync]` и т.д.). Путь к исходному коду библиотеки сохранён в ENV
в `KLUDGEBOX_SRC` — читать их нужно там.

Через KludgeBox приходят транзитивно и используются напрямую в коде:

* **Serilog** — логирование (`[Logger] private ILogger _log`);
* **Humanizer** — подстановка в шаблоны строк (`FormatWith(...)`).

Из сборки подавлено предупреждение `CS0649` (`NoWarn` в `.csproj`): поля заполняет DI, а не
конструктор, и компилятор считает их неиспользуемыми.

В `NeonWarfare.csproj` есть `<Compile Remove="Tests/**" />`: каталог игрового проекта — это корень
репозитория, поэтому дефолтный глоб `Godot.NET.Sdk` (`**/*.cs`) иначе затянул бы файлы тестов в
игровую сборку, и та упала бы на типах xUnit. Тестовый проект собирается сам по себе; в
`ExportDebug` и `ExportRelease` он из сборки solution исключён, чтобы редактор Godot и экспорт игры
его не трогали.
