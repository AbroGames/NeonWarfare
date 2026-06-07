# Сеть

[← README проекта](../../README.md)

Используется **штатный высокоуровневый мультиплеер Godot** (`SceneMultiplayer` + `ENetMultiplayerPeer`).

* **`Network`** ([Scenes/Game/Network/Network.cs](../../Scenes/Game/Network/Network.cs)) — обёртка над `MultiplayerApi`.
  Создаёт новый `SceneMultiplayer`, привязанный к ноде `Game`, чтобы при выходе в меню гарантированно отвалились
  все старые подписки. Отвечает за `ConnectToServer()`, `HostServer()`, `OpenServer()` и корректный `Shutdown()`.
* **`NetworkStateMachine`** — состояния `NotInitialized → Connecting/Hosting → Connected/Hosted → Disconnected`
  и производные флаги (`IsClient`, `IsServer`, `IsActiveGameState`).
* **RPC** — основной способ обмена. Пары «публичная обёртка + приватный `*Rpc`»:

  ```csharp
  public void Save(string saveFileName) => RpcId(ServerId, MethodName.SaveRpc, saveFileName);
  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
  private void SaveRpc(string saveFileName) { /* ... */ }
  ```
  Между функциями специально не добавляется пустая строка, т.к. публичный метод является просто удобным алиасом для приватного.  
  

* **Спавн объектов** — `WorldMultiplayerSpawnerService.AddSpawnerToNode(node)` вешает на ноду
  `WorldMultiplayerSpawner` (наследник `AbstractMultiplayerSpawner` из KludgeBox). Нода `WorldMultiplayerSpawner`
  отслеживает ноду, к которой она была прикреплена, и при создании подноды на отслеживаемой ноде, синхронизирует создание новой ноды по сети.
  Спавнер автоматически удаляется вместе с наблюдаемой нодой. Спавнить можно только сцены, перечисленные в `SyncedPackedScenes`.
* **Синхронизация полей** — атрибут `[Sync]` из `KludgeBox.Godot.Nodes.MpSync`
  (пример: `WorldTemporaryData.PlayerUidByPeerId`).
* **Каналы передачи** — `Consts.TransferChannel`: `Chat`, `StatsHp`, `StatsCache`. Указываются в
  `[Rpc(TransferChannel = (int) Consts.TransferChannel.X)]`, чтобы независимые потоки не ждали друг друга при потере сообщения.
* **Полезная нагрузка** — примитивы или `byte[]` от MessagePack. Максимальный размер синк-пакета —
  `Network.MaxSyncPacketSize`.  
**Подключение клиента** (`WorldSynchronizerService`) — одинаково для сетевой и одиночной игры:

```
клиент: StartSyncOnClient(uid, nick, color)
   → сервер: NewClientInitOnServerRpc — валидация (uid, длина ника и т.д.), регистрация PlayerData, выдача админки
             ├── отказ  → RejectSyncOnClientRpc(error) → клиент уходит в меню с сообщением
             └── успех  → EndSyncOnClientRpc(byte[]) — весь PersistenceData одним снимком MessagePack
   → клиент: десериализация мира → EndSyncOnServerRpc
   → сервер: WorldPlayerService.SpawnPlayer(peerId)
```
