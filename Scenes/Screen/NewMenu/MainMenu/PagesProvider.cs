using System;
using Godot;
using KludgeBox.DI.Requests.NotNullCheck;
using NeonWarfare.Scenes.KludgeBox;
using NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.Message;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu;

public partial class PagesProvider : CheckedAbstractStorage
{
	[Export] [NotNull] public PackedScene MainPageScene { get; private set; }
	[Export] [NotNull] public PackedScene SettingsPageScene { get; private set; }
	[Export] [NotNull] public PackedScene ConnectionPageScene { get; private set; }
	[Export] [NotNull] public PackedScene CreateServerPageScene { get; private set; }
	[Export] [NotNull] public PackedScene MessagePageScene { get; private set; }
	[Export] [NotNull] public PackedScene LanguageSelectionPageScene { get; private set; }
	[Export] [NotNull] public PackedScene SingleplayerPage { get; private set; }
	
	public MessagePage PrepareMessagePage(string message)
	{
		var page = MessagePageScene.Instantiate<MessagePage>().WithAvailablePages(this);
		page.MessageLabel.Text = message;
		return page;
	}

	public MainMenuPage PreparePage(PackedScene pageScene)
	{
		var instance = pageScene.Instantiate();
		if (instance is not MainMenuPage page)
		{
			throw new ArgumentException($"Attempt to prepare main menu page from non-page packed scene ({instance.GetType()})");
		}
		
		return page.WithAvailablePages(this);
	}
}