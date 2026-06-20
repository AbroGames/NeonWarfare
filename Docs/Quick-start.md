# Быстрый старт

[← README проекта](../README.md)

* Рекомендуется устанавливать и обновлять Godot через [GodotUpdaterUI](https://github.com/AbroGames/GodotUpdaterUI/releases): это автоматически настроит все ENV переменные, которые используются в проекте в `launchSettings.json`.  
* Чтобы в Godot настроить интеграцию с Rider, необходимо зайти в Editor → Editor Settings → Dotnet → Editor.
В списке External Editor выбрать JetBrains Rider и очистить значение Custom Exec Path Args.

Для быстрого тестирования мультиплеера в `Properties/launchSettings.json` уже заведены конфигурации запуска
(используют переменную окружения `GODOT_EXE` — путь к исполняемому файлу Godot, настраивается
[GodotUpdaterUI](https://github.com/AbroGames/GodotUpdaterUI/releases) автоматически):

* `Client` — обычный запуск клиента с главным меню.
* `Server` — выделенный сервер (`--server`).
* `Auto-start` — сразу начать одиночную игру с новым файлом сохранения, минуя меню (`--auto-start`).
* `Auto-start (test save)` — то же самое, но с сохранением `test` (`--auto-start-savefile test`):
  первый запуск создаёт его, последующие — загружают.
* `Autoconnect (1)` — клиент с автоподключением под `TestPlayer1`.
* `Autoconnect (2)` — клиент с автоподключением под `TestPlayer2`.

Rider подхватывает их автоматически как конфигурации запуска. Чтобы одновременно поднять сервер и одного
или двух клиентов в репозиторий добавлены `Multi-Launch`-конфигурации с нужным набором задач, например:

* Тип: `Multi-Launch`. Название: `Fast-test (1 client)`. Tasks: `Server, Autoconnect (1)`.
* Тип: `Multi-Launch`. Название: `Fast-test (2 clients)`. Tasks: `Server, Autoconnect (1), Autoconnect (2)`.
