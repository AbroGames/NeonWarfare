# Данные и сохранения

[← README проекта](../../README.md)

Данные мира разделены на два узла и **не содержат логики** — только состояние и его синхронизацию:

| Узел | Живёт | Синхронизация | Попадает в сохранение |
|---|---|---|---|
| `WorldPersistenceData` | до конца игры | RPC внутри storage-классов + снимок при коннекте | Да |
| `WorldTemporaryData` | до конца сессии | `[Sync]` | Нет |

`WorldPersistenceData` состоит из storage-нод (`GeneralDataStorage`, `PlayerDataStorage`), каждая из которых
реализует `ISerializableStorage` (`SerializeStorage` / `DeserializeStorage` / `SetAllPropertyListeners`).
`WorldDataSerializerService` рефлексией (`Services.MembersScanner`) обходит все `ISerializableStorage`
внутри `WorldPersistenceData` и собирает `Dictionary<string, byte[]>` → MessagePack. Тот же байтовый блоб
используется и для сохранения на диск, и для первичной синхронизации нового клиента.

Сами модели (`PlayerData`, `GeneralData`) — это `ObservableObject` с `[ObservableProperty]` и ключами
`[Key(N)]` MessagePack. Storage подписывается на `PropertyChanged` и автоматически рассылает изменение
клиентам — то есть **любая запись в свойство модели на сервере сама по себе сетевая**.

Сохранения: `user://saves/<name>.bin`, имя нового файла — `yyyy-MM-dd_HH-mm` (`SaveLoadService`).
Автосохранение выполняется в `WorldServerShutdowner` при выходе из дерева и управляется настройкой
`AutoSaveEnabled` (у клиента — `GameSettings`, у выделенного сервера — `DedicatedServerSettings`).

Прочие файлы в `user://` (JSON): `game-settings.json` — настройки клиента,
`dedicated-server-settings.json` — настройки выделенного сервера, `resume-game.json` — последняя
сессия для кнопки «Продолжить».
