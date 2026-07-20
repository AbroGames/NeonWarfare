# Соглашения по написанию кода

[← README проекта](../README.md)

## Пространство имён

Пространства имён повторяют путь до файла: `Scenes/World/Service/Chat/WorldChatService.cs` →
`NeonWarfare.Scenes.World.Service.Chat`.

Сцена и её обработчик лежат в одной папке под одним именем (`Hud.tscn` + `Hud.cs`). Файлы `.cs.uid`
генерирует Godot — руками их создавать нельзя.

## Инициализация

* Если требуется инъекция зависимостей, то в `_Ready()` (или конструктор у не-ноды) пишем
  `Di.Process(this)` **первой строкой**. Без него все `[Child]` / `[Parent]` / `[SceneService]` /
  `[Logger]` молча останутся `null` — см. [Внедрение зависимостей](Arch/Dependency-injection.md).
* Если ноде нужны данные **до** `_Ready()`, используется метод `InitPreReady(...)`, возвращающий
  `this`: `PackedScene.Instantiate<Hud>().InitPreReady(world)`. Для инициализации после готовности —
  `InitPostReady(...)`.
* Тяжёлая инициализация верхнего уровня разнесена на `Init()` и `Start()`.

## Клиент и сервер

* Проверять роль только через `Net.*`, а не через `GetMultiplayer().IsServer()` напрямую.
* Методы, допустимые лишь на одной стороне, начинаются с проверки:

  ```csharp
  if (!Net.IsServer()) throw new InvalidOperationException("Can only be executed on the server");
  ```

* Суффикс `OnServer` / `OnClient` в имени метода означает сторону выполнения
  (`InitOnServer`, `StartSyncOnClient`, `RejectSyncOnClient`).
* События именуются `<Что>Event` с указанием стороны: `SaveSuccessServerEvent`, `SyncEndedOnClientEvent`.

## RPC

* Публичный метод-обёртка + приватный метод с суффиксом `Rpc`, помеченный `[Rpc]`.
  Обёртка — единственная строка вида `=> RpcId(ServerId, MethodName.XxxRpc, ...)` или `=> Rpc(...)`.
  Пустая строка между ними **не ставится** — это намеренно.
* Всегда явно указывать режим: `[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]` для
  «клиент → сервер», `[Rpc(CallLocal = true)]` для «сервер → клиент».
* По возможности выносить RPC поток в отдельный `TransferChannel` из `Consts.TransferChannel`.
* Аргументы — примитивы Godot или `byte[]` от MessagePack. JSON в сети не используется.
* Стараться объединять данные в один вызов (например, координаты нескольких юнитов сразу),
  а не слать RPC на каждый объект.

Подробнее — в [Сети](Arch/Networking.md).

## Сериализация

* Сеть и сохранения — **MessagePack** (`[MessagePackObject]`, `[Key(N)]`, `[IgnoreMember]`).
* Файлы настроек на диске — **JSON** (`System.Text.Json`).

Путать эти два инструмента нельзя: см. [Данные и сохранения](Arch/Data-and-saves.md).

## Данные

* Классы данных содержат только состояние и его синхронизацию, без игровой логики.
* Модели — наследник `ObservableObject` + `[ObservableProperty]`; storage подписывается на
  `PropertyChanged` и рассылает изменения сам. Поэтому запись в свойство модели на сервере — это
  сетевой вызов, и такие свойства нельзя использовать как рабочие переменные в циклах.

## Логирование

Serilog через `[Logger] private ILogger _log`, параметры шаблона именованные:

```csharp
_log.Information("Connecting to the server at {host}:{port}", host, port);
```

## Точность: float vs double

Для некритичных вещей (визуал, вспомогательные расчёты) используем `float` и просто кастим `deltaTime`
из `double` во `float`. Для критичных (характеристики юнитов, урон, лечение, время) — `double`.

## Строки

* Сообщения об ошибках и шаблоны — `private const string` в начале класса, при обращении подстановка
  через `FormatWith(...)` из Humanizer.
* Всё, что видит игрок, должно переводиться — см. [Локализацию](Localization.md).

## Утечки памяти

Подписки на события `MultiplayerApi` и сервисов оформляются локальными функциями, чтобы их можно было
корректно отцепить (`GetMultiplayer().ConnectedToServer -= ConnectedToServerEvent`). Это частая
причина утечек при переходе Game → MainMenu, поэтому такие места в коде сопровождаются комментарием.

## Форматирование

`.editorconfig`: UTF-8, LF, максимальная длина строки — 120 символов.
