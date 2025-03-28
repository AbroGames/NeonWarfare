using System;
using System.IO;
using System.Net;
using Godot;
using NeonWarfare.Scenes.Game.ClientGame.ClientSettings;
using NeonWarfare.Scenes.PackedScenesContainer.ClientPackedScenesContainer;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.KludgeBox.Godot.Services;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.CmdArgs;
using NeonWarfare.Scripts.Utils.GameSettings;
using NeonWarfare.Scripts.Utils.Profiling;

namespace NeonWarfare.Scenes.Root.ClientRoot;

public partial class ClientRoot : Node2D
{
	[Export] [NotNull] public ClientPackedScenesContainer PackedScenes { get; private set; }
	
	public ClientParams CmdParams { get; private set; }
	public Settings Settings { get; set; } = new Settings();

	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		Callable.From(() => { 
			Init(); 
			Start();
		}).CallDeferred();
	}

	public override void _Process(double delta)
	{
		UpdateStretchScale();
	}

	protected void Init()
	{
		RootService.CommonInit(ListenerSide.Client);
		CmdParams = ClientParams.GetFromCmd();

		Settings = SettingsService.Load();

		if (!CmdParams.IsHelper)
		{
			Settings.Changed += ApplySettings;
		}
		else
		{
			Settings.PlayerName = $"(HELPER) {Settings.PlayerName}";
			Settings.PlayerColor = Settings.PlayerColor.Inverted();
			GetTree().Root.Title = $"[HELPER] {GetTree().Root.Title}";
		}
		
		if (Settings.MaximizeOnStart)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
		}
		
		AchievementsList.Initialize();
		AchievementsState = AchievementsState.Load();
	}

	private void ApplySettings(Settings settings)
	{
		Audio2D.MasterVolume = settings.MasterVolume / 100;
		Audio2D.MusicVolume = settings.MusicVolume / 100;
		Audio2D.SoundsVolume = settings.SoundVolume / 100;
		
		settings.PlayerColor.ToHsv(out var h, out var s, out var _);
		MainMenu?.Background3D?.SetAccentColor(settings.PlayerColor);
	}
	
	protected void Start()
	{
		if (CmdParams.RunProfiler)
		{
			var profilingName = $"profiler_data_{DateTime.Now:yy-MM-dd} {DateTime.Now:h.mm.ss}.dat";
			var profilingPath = ProjectSettings.GlobalizePath($"res://{profilingName}");
			
			//ProfilingContainer.StartProfilingSession(profilingPath);
		}
		
		if (CmdParams.AutoConnectIp != null)
		{
			int autoConnectPort = CmdParams.AutoConnectPort.GetValueOrDefault(Network.DefaultPort);
			CreateClientGame(CmdParams.AutoConnectIp, autoConnectPort);
		}
		else
		{
			CreateMainMenu();
		}
		
		ApplySettings(Settings);
		UnlockAchievement(AchievementIds.MainMenuAchievement);
	}
	
	private static float _preserverVolume;
	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			SettingsService.Save(Settings);
			//ProfilingContainer.StopProfilingSession();
			Log.Info("Game exiting...");
		}
		
		if (what == NotificationWMWindowFocusOut)
		{
			_preserverVolume = Audio2D.MasterVolume;
			Audio2D.MasterVolume = 0;
		}

		if (what == NotificationWMWindowFocusIn)
		{
			Audio2D.MasterVolume = _preserverVolume;
		}
	}
	
	public void Shutdown()
	{
		GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest); // Уведомить все ноды, что игра вот-вот закроется
		GetTree().Quit();
	}
}
