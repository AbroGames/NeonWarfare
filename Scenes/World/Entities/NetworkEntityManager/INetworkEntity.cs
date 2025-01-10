using Godot;
using KludgeBox.Events;

namespace NeonWarfare;

public interface INetworkEntity //TODO переделать в NetworkEntityComponent? Тогда можно будет в нем же ловить пакеты по уничтожению объекта, и вызывать Action соответсвующий, туда же обработку Move пакета (можно без инерции, обычный move).
{
    public long Nid { get; set; }
}