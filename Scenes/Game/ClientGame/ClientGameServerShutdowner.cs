using Godot;
using KludgeBox;
using NeonWarfare;
using NeonWarfare.NetOld;

public partial class ClientGame
{
	
	//Дублируем обработку нотификаций дополнительно к ClientRoot, чтобы можно было вырубать процесс сервера при закрытии игры и выходе в главное меню 
	public override void _Notification(int id)
	{
		ClientRoot.Instance.ServerShutdowner._Notification(id);
	}
}
