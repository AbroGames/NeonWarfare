# Завершение работы

[← README проекта](../../README.md)

* **Клиент** — `Services.MainScene.Shutdown()` → отложенный `SceneTree.Quit()`.
* **Дочерний процесс сервера** — `ProcessShutdowner` (нода из KludgeBox,
  `KludgeBox.Godot.Nodes.Process`) вешается на `Game` в
  `HostDedicatedServerAndConnectGameStarter`. При уничтожении сцены `Game` он убивает процесс сервера
  по сохранённому PID.
* **Сервер, запущенный из клиента** — тому же процессу передан `--parent-pid`, и
  `HostMultiplayerGameStarter` вешает на `Game` `ProcessDeadChecker` (тоже нода из KludgeBox). Он
  периодически проверяет живость родителя и вызывает `MainScene.Shutdown()`, если клиент исчез из ОС.
* **Сохранение мира** — `WorldServerShutdowner` ловит `NotificationExitTree` и вызывает
  `TryAutoSave()`. Отдельная нода нужна потому, что после выхода из дерева `GetMultiplayer()` уже
  `null`, а `Network` к этому моменту мог подменить peer на `OfflineMultiplayerPeer` — доверять им
  нельзя.

Пара «клиент + вынесенный сервер» держится на двух нодах сразу и с двух сторон: `ProcessShutdowner`
убивает сервер при нормальном закрытии клиента, `ProcessDeadChecker` — страховка на случай, когда
клиент умер аварийно и убить никого не успел.

Автосохранение управляется настройкой `AutoSaveEnabled`: у клиента — в `GameSettings`, у выделенного
сервера — в `DedicatedServerSettings` (см. [Данные и сохранения](Data-and-saves.md)).
