# Сеть

[← README проекта](../../README.md)

Используется **штатный высокоуровневый мультиплеер Godot** (`SceneMultiplayer` + `ENetMultiplayerPeer`).

## Основные узлы

* **`Network`** ([Scenes/Game/Network/Network.cs](../../Scenes/Game/Network/Network.cs)) — обёртка над
  `MultiplayerApi`. Создаёт новый `SceneMultiplayer`, привязанный к ноде `Game`, чтобы при выходе в
  меню гарантированно отвалились все старые подписки. Отвечает за `ConnectToServer()`,
  `HostServer()`, `OpenServer()` и корректный `Shutdown()`.
* **`NetworkStateMachine`** — состояния `NotInitialized → Connecting/Hosting → Connected/Hosted →
  Disconnected` и производные флаги (`IsClient`, `IsServer`, `IsActiveGameState`).

## Роли процесса: `IsServer()` / `IsClient()`

| Режим | `Net.IsServer()` | `Net.IsClient()` |
|---|---|---|
| Главное меню | `true` | `true` |
| Одиночная игра | `true` | `true` |
| Хост «изнутри клиента» | `true` | `true` |
| Подключение к чужому серверу | `false` | `true` |
| Выделенный сервер (`--server`) | `true` | `false` |

В [Потоке запуска](Startup-flow.md) описано как создается каждый режим.

## RPC

Основной способ обмена. Пара «публичная обёртка + приватный `*Rpc`»:

```csharp
public void Save(string saveFileName) => RpcId(ServerId, MethodName.SaveRpc, saveFileName);
[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
private void SaveRpc(string saveFileName) { /* ... */ }
```

Между функциями специально не добавляется пустая строка, т.к. публичный метод является просто удобным
алиасом для приватного.

Режим указывается всегда явно. Полная памятка по написанию RPC — в
[Соглашениях по написанию кода](../Code-style.md).

## Спавн объектов

`WorldMultiplayerSpawnerService.AddSpawnerToNode(node)` вешает на ноду `WorldMultiplayerSpawner`
(наследник `AbstractMultiplayerSpawner` из KludgeBox). Спавнер отслеживает ноду, к которой он был
прикреплён, и при создании подноды на отслеживаемой ноде синхронизирует создание новой ноды по сети.
Имя спавнеру даётся по шаблону `<имя ноды>-MultiplayerSpawner`, и он автоматически удаляется вместе с
наблюдаемой нодой.  
Исключение — спавнер для `Tree`, который лежит прямо в `World.tscn`, а не создается из кода.

Спавнить можно только сцены, перечисленные в `SyncedPackedScenes`.

## Синхронизация полей

Атрибут `[Sync]` из `KludgeBox.Godot.Nodes.MpSync` (пример: `WorldTemporaryData.PlayerUidByPeerId`).
Что синкать через `[Sync]`, а что складывать в снимок сохранения — в
[Данных и сохранениях](Data-and-saves.md).

## Каналы передачи

`Consts.TransferChannel`: `Chat`, `StatsHp`, `StatsCache`. Указываются как
`[Rpc(TransferChannel = (int) Consts.TransferChannel.X)]`, чтобы независимые потоки не ждали друг
друга при потере сообщения.

## Полезная нагрузка

Примитивы Godot или `byte[]` от MessagePack; JSON по сети не ходит. Максимальный размер синк-пакета —
`Network.MaxSyncPacketSize` = `1350 * 100` (около 135 КБ: сотня ENet-пакетов по MTU). В него должен
влезать в том числе первичный снимок мира, который уходит клиенту одним вызовом.

## Подключение клиента

`WorldSynchronizerService`, одинаково для сетевой и одиночной игры: клиент вызывает
`StartSyncOnClient(uid, nick, color)`, что уходит на сервер как `NewClientInitOnServerRpc` —
валидация (uid, длина ника и т.д.), регистрация `PlayerData`, выдача админки. При отказе сервер
шлёт `RejectSyncOnClientRpc(error)`, и клиент уходит в меню с сообщением. При успехе —
`EndSyncOnClientRpc(byte[])` с всем `PersistenceData` одним снимком MessagePack. Клиент
десериализует мир и отвечает `EndSyncOnServerRpc`, после чего сервер вызывает
`WorldPlayerService.SpawnPlayer(peerId)`.
