using KludgeBox;

namespace NeonWarfare;

public partial class ClientRoot
{
	private static ClientRoot _instance;
	
	public new static ClientRoot Instance
	{
		get
		{
			if (_instance is null)
			{
				Log.Error("Try get Instance from ClientRoot singleton, but field is null (it is server, not client)");
			}
			return _instance;
		}
		private set
		{
			if (_instance is not null)
			{
				Log.Error("Try set Instance for ClientRoot singleton, but field is not null");
			}
			_instance = value;
		}
	}

	public override void _EnterTree()
	{
		Instance = this;
	}
}