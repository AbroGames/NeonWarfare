# Внедрение зависимостей (DI)

[← README проекта](../../README.md)

Используется DI из KludgeBox. Практически каждый класс первой строкой в `_Ready()` (или в конструкторе для
не-нод) вызывает `Di.Process(this)`, после чего заполняются помеченные поля:

| Атрибут | Что инжектит |
|---|---|
| `[Child]` | Дочернюю ноду по имени поля (или `[Child(By.Type)]` — по типу) |
| `[Parent]` | Родительскую ноду нужного типа |
| `[SceneService]` | Сервис из ближайшего `IServiceProvider` вверх по дереву (то есть из `World`) |
| `[Logger]` | `Serilog.ILogger`, настроенный на текущий класс |
| `[NotNull]` | Проверка, что `[Export]`-поле заполнено в редакторе (в `CheckedAbstractStorage`) |

`CheckedAbstractStorage` — база для всех хранилищ `PackedScene` (`RootPackedScenes`, `GamePackedScenes`,
`SyncedPackedScenes`, `ClientPackedScenes`, `PagesProvider`). Ссылки на прототипы сцен настраиваются в
редакторе Godot; получение любой сцены для инстанцирования начинается отсюда.
