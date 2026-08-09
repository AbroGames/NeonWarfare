using System;
using Godot;
using KludgeBox.DI.Requests.NotNullCheck;
using NeonWarfare.Scenes.KludgeBox;
using NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.Message;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu;

public partial class PagesProvider : CheckedAbstractStorage
{
	[Export] [NotNull] public PackedScene MainPageScene { get; private set; }
	[Export] [NotNull] public PackedScene ServerListPageScene { get; private set; }
	[Export] [NotNull] public PackedScene MessagePageScene { get; private set; }
	[Export] [NotNull] public PackedScene LanguageSelectionPageScene { get; private set; }
	[Export] [NotNull] public PackedScene SingleplayerPage { get; private set; }
	[Export] [NotNull] public PackedScene MultiplayerPageScene { get; private set; }
	[Export] [NotNull] public PackedScene CreateNewServerPageScene { get; private set; }
	[Export] [NotNull] public PackedScene CreateSavedServerPageScene { get; private set; }
	[Export] [NotNull] public PackedScene PlayerSettingsPageScene { get; private set; }
	[Export] [NotNull] public PackedScene SettingsHubPageScene { get; private set; }
	[Export] [NotNull] public PackedScene SettingsCategoryPageScene { get; private set; }
	[Export] [NotNull] public PackedScene ConfirmDialogPageScene { get; private set; }
	
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

	public Pages.PlayerSettings.PlayerSettingsPage PreparePlayerSettingsPage(Action continuation)
	{
		var page = PlayerSettingsPageScene.Instantiate<Pages.PlayerSettings.PlayerSettingsPage>().WithAvailablePages(this);
		page.SetContinuation(continuation);
		return page;
	}

	public Pages.SettingsCategory.SettingsCategoryPage PrepareSettingsCategoryPage(string category, string titleKey)
	{
		var page = SettingsCategoryPageScene.Instantiate<Pages.SettingsCategory.SettingsCategoryPage>().WithAvailablePages(this);
		page.Configure(category, titleKey);
		return page;
	}

	public Pages.ConfirmDialog.ConfirmDialogPage PrepareConfirmDialogPage(string message, Action onReset = null, Action onContinue = null, Action onBack = null)
	{
		var page = ConfirmDialogPageScene.Instantiate<Pages.ConfirmDialog.ConfirmDialogPage>().WithAvailablePages(this);
		page.Setup(message, onReset, onContinue, onBack);
		return page;
	}
}