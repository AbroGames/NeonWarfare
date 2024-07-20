using KludgeBox;
using Microsoft.CSharp.RuntimeBinder;

namespace NeonWarfare;

public partial class ServerRoot
{
	private static ServerRoot _instance;
	
	public new static ServerRoot Instance
	{
		get
		{
			if (_instance is null)
			{
				Log.Error("Try get Instance from ServerRoot singleton, but field is null (it is client, not server)");
			}
			return _instance;
		}
		private set
		{
			if (_instance is not null)
			{
				Log.Error("Try set Instance for ServerRoot singleton, but field is not null");
			}
			_instance = value;
		}
	}

	public override void _EnterTree()
	{
		Instance = this;
	}
}