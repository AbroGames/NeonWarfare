# Данные и сохранения

[← README проекта](../../README.md)

Данные мира разделены на два узла и **не содержат логики** — только состояние и его синхронизацию:

| Узел | Живёт | Синхронизация | Попадает в сохранение |
|---|---|---|---|
| `WorldPersistenceData` | до конца игры | RPC внутри storage-классов + снимок при коннекте | Да |
| `WorldTemporaryData` | до конца сессии | `[Sync]` | Нет |

Это разные ноды, и выбор между ними делается осознанно: всё, что должно пережить перезапуск, идёт в
`PersistenceData`, всё остальное — в `TemporaryData`.

## Сериализация мира

`WorldPersistenceData` состоит из storage-нод (`GeneralDataStorage`, `PlayerDataStorage`), каждая из
которых реализует `ISerializableStorage` (`SerializeStorage` / `DeserializeStorage` /
`SetAllPropertyListeners`).

`WorldDataSerializerService` рефлексией (`Services.MembersScanner`) обходит все `ISerializableStorage`
внутри `WorldPersistenceData` и собирает `Dictionary<string, byte[]>` → MessagePack. Тот же байтовый
блоб используется и для сохранения на диск, и для первичной синхронизации нового клиента
(см. [Сеть](Networking.md)).

## Модели

Сами модели (`PlayerData`, `GeneralData`) — это `ObservableObject` с `[ObservableProperty]` и ключами
`[Key(N)]` MessagePack. Storage подписывается на `PropertyChanged` и автоматически рассылает изменение
клиентам.

> [!IMPORTANT]
> **Любая запись в свойство модели на сервере сама по себе сетевая.** Эти свойства нельзя использовать
> как рабочие переменные в циклах: каждое присваивание порождает трафик.

## Файлы

Все пользовательские файлы лежат в каталоге, на который Godot маппит `user://`. Каталог зависит от
ОС и от названия проекта (`Neon Warfare`):

| ОС | Путь |
|---|---|
| Windows | `%APPDATA%\Godot\app_userdata\Neon Warfare\` |
| Linux | `~/.local/share/godot/app_userdata/Neon Warfare/` |

Сохранения: `user://saves/<name>.bin`, имя нового файла — `yyyy-MM-dd_HH-mm` (`SaveLoadService`).
Автосохранение выполняется в `WorldServerShutdowner` при выходе из дерева и управляется настройкой
`AutoSaveEnabled` (у клиента — `GameSettings`, у выделенного сервера — `DedicatedServerSettings`).
Подробнее — в [Завершении работы](Shutdown.md).

Прочие файлы в `user://` (JSON, `System.Text.Json`):

| Файл | Что хранит |
|---|---|
| `game-settings.json` | Настройки клиента |
| `dedicated-server-settings.json` | Настройки выделенного сервера |
| `resume-game.json` | Последняя сессия для кнопки «Продолжить» |
