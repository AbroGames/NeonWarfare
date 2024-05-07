namespace NeonWarfare;

public partial class Root
{
	public static Root Instance { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
	}
}