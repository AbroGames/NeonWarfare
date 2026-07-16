# Быстрый старт

[← README проекта](../README.md)

## Окружение

* Рекомендуется устанавливать и обновлять Godot через
  [GodotUpdaterUI](https://github.com/AbroGames/GodotUpdaterUI/releases): это автоматически настроит
  все ENV-переменные, которые используются в проекте в `launchSettings.json` — в первую очередь
  `GODOT_EXE` (путь к исполняемому файлу Godot).
* Чтобы в Godot настроить интеграцию с Rider, необходимо зайти в Editor → Editor Settings → Dotnet →
  Editor. В списке External Editor выбрать JetBrains Rider и очистить значение Custom Exec Path Args.

## Профили запуска

Для быстрого тестирования в `Properties/launchSettings.json` уже заведены конфигурации запуска. Rider
подхватывает их автоматически.

| Профиль | Аргументы | Что делает |
|---|---|---|
| `Client` | — | Обычный запуск клиента с главным меню |
| `Auto-start (new game)` | `--auto-start` | Сразу одиночная игра с новым файлом сохранения, минуя меню |
| `Auto-start (saved game)` | `--auto-start --auto-start-savefile test` | То же, но с сохранением `test`: первый запуск создаёт его, последующие — загружают |
| `Server` | `--server --world-render` | Выделенный сервер с отрисовкой мира |
| `Autoconnect (1)` | `--auto-connect --uid TestPlayer1 --nick TestPlayer1` | Клиент с автоподключением |
| `Autoconnect (2)` | `--auto-connect --uid TestPlayer2 --nick TestPlayer2` | Второй клиент с автоподключением |

Все флаги описаны в [Параметрах командной строки](Cli-args.md).

## Multi-Launch: сервер и клиенты одной кнопкой

Чтобы одновременно поднять сервер и одного или двух клиентов, в репозиторий добавлены
`Multi-Launch`-конфигурации Rider — файлы `.run/*.run.xml`:

* Тип: `Multi-Launch`. Название: `Fast-test (1 client)`. Tasks: `Server, Autoconnect (1)`.
* Тип: `Multi-Launch`. Название: `Fast-test (2 clients)`. Tasks: `Server, Autoconnect (1), Autoconnect (2)`.

При правке `Properties/launchSettings.json` или `.run/` нужно синхронно править этот раздел.
