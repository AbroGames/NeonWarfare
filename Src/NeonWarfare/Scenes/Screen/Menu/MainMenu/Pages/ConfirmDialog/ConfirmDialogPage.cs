using System;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Screen.Menu.MainMenu.Pages.ConfirmDialog;

public partial class ConfirmDialogPage : MainMenuPage
{
    [Child] public Label MessageLabel { get; private set; }
    [Child] public Button ResetButton { get; private set; }
    [Child] public Button BackButton { get; private set; }
    [Child] public Button ContinueButton { get; private set; }

    private string _message;
    private Action _onReset;
    private Action _onContinue;
    private Action _onBack;

    public override void _Ready()
    {
        Di.Process(this);

        MessageLabel.Text = _message;

        ResetButton.Pressed += OnResetPressed;
        BackButton.Pressed += OnBackPressed;
        ContinueButton.Pressed += OnContinuePressed;
    }

    /// <summary>Called by <see cref="PagesProvider.PrepareConfirmDialogPage"/> before the page is added to the tree.
    /// Stashes inputs — <see cref="MessageLabel"/> is applied in <see cref="_Ready"/>
    /// (the node does not exist yet).</summary>
    public void Setup(string message, Action onReset, Action onContinue, Action onBack)
    {
        _message = message;
        _onReset = onReset;
        _onContinue = onContinue;
        _onBack = onBack;
    }

    private void OnResetPressed()
    {
        // "Reset changes" — caller's onReset commits the draft (SetVisibleSettings + ApplyAndSaveSettings),
        // and the category page's own GoBack inside the closure pops this dialog (it is CurrentPage).
        _onReset?.Invoke();
        GoBack(); // pops the category page (now CurrentPage after the dialog was popped inside the closure)
    }

    private void OnContinuePressed()
    {
        // "Continue" (discard) — caller's onContinue re-applies the preserved snapshot, its GoBack pops the dialog.
        _onContinue?.Invoke();
        GoBack(); // pops the category page
    }

    private void OnBackPressed()
    {
        // "Back" — stay on the category page (just close the dialog). A null onBack is the common case.
        _onBack?.Invoke();
        GoBack(); // pops the dialog; the category page becomes CurrentPage and is re-shown
    }
}
