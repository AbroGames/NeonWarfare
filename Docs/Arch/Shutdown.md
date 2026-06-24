# Завершение работы

[← README проекта](../../README.md)

* **Клиент** — `Services.MainScene.Shutdown()` → отложенный `SceneTree.Quit()`.
* **Дочерний процесс сервера** — при уничтожении сцены `Game` срабатывает `ProcessShutdowner`,
  который убивает процесс сервера по сохранённому PID.
* **Сервер с `--parent-pid`** — `ProcessDeadChecker` периодически проверяет живость родителя и
  завершает сервер, если клиент, который его запустил, исчез из ОС.
* **Сохранение мира** — `WorldServerShutdowner` ловит `NotificationExitTree` и вызывает `TryAutoSave()`.
  Отдельная нода нужна потому, что после выхода из дерева `GetMultiplayer()` уже `null`, а `Network`
  к этому моменту мог подменить peer на `OfflineMultiplayerPeer` — доверять им нельзя.
