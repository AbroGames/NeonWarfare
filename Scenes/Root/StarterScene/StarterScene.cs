using System;
using System.Diagnostics;
using Godot;
using HarmonyLib;
using KludgeBox;
using NeonWarfare.Utils;

namespace NeonWarfare;

public partial class StarterScene : Node
{
	
	[Export] [NotNull] public PackedScene ClientRoot { get; private set; }
	[Export] [NotNull] public PackedScene ServerRoot { get; private set; }
	
	private static void PatchLogException()
	{
		var harmony = new Harmony("cc.abro.patch");
		var exceptionUtilsType = AccessTools.TypeByName("Godot.NativeInterop.ExceptionUtils");
		var originalMethod = AccessTools.Method(exceptionUtilsType, "LogException", [typeof(Exception)]);
		var patchMethod = AccessTools.Method(typeof(StarterScene), nameof(PatchPrefix));

		harmony.Patch(originalMethod, new HarmonyMethod(patchMethod), null);
		Log.Info($"Applied patch for {originalMethod.DeclaringType!.FullName}.{originalMethod.Name} to log exceptions");
	}

	public static bool PatchPrefix(ref Exception e)
	{
		Log.Error(e.ToString());
		return false;
	}
	
	public override void _Ready()
	{
		PatchLogException();
		NotNullChecker.CheckProperties(this);
        CallDeferred(nameof(StartGame));
	}

	private void StartGame()
	{
		PackedScene rootScene = CmdArgsService.ContainsInCmdArgs(ServerParams.ServerFlag) ? ServerRoot : ClientRoot;
		GetTree().ChangeSceneToPacked(rootScene);
	}
}