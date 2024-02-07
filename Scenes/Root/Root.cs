using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Godot;

public partial class Root : Node2D
{
	
	[Export] [NotNull] public PackedScenesContainer PackedScenes { get; private set; }
	[Export] [NotNull] public AbroDraft.Game Game { get; private set; }
	
	public EventBus EventBus { get; private set; } = new();
	public List<Object> Services { get; private set; } = new();
	
	public static Root Instance { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		ServicesInit();
	}

	//Todo вынести в другое место (автоматически через аннотации, например. Хранить сервисы тоже в другом месте, наверн)
	public void ServicesInit()
	{
		Services.Add(new PlayerRotateService());
		Services.Add(new PlayerMovementService());
	}
}
