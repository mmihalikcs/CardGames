using Godot;

namespace CardGames.Godot.Scripts;

/// <summary>
/// Pause overlay shown on top of GameSessionPanel while a game is in progress. Uses
/// SceneTree.Paused (this node's ProcessMode is Always, set in the .tscn) so the playfield under it -
/// which stays at the default Inherit/Pausable ProcessMode - simply stops receiving input while
/// paused, rather than hand-rolling per-widget mouse_filter/focus-release logic. Extensibility: adding
/// a future third menu option is just one more Button node in the .tscn's MenuOptions VBoxContainer
/// plus one more event + Pressed wire-up here, mirroring GameOverPanel's single-BackButton precedent -
/// no data-driven menu-item framework needed.
/// </summary>
public partial class PauseMenuPanel : Control
{
    private Button _QuitGameButton = null!;
    private Button _ExitToDesktopButton = null!;

    public event Action? QuitGameRequested;
    public event Action? ExitToDesktopRequested;

    public override void _Ready()
    {
        _QuitGameButton = GetNode<Button>("MenuCenter/MenuOptions/QuitGameButton");
        _ExitToDesktopButton = GetNode<Button>("MenuCenter/MenuOptions/ExitToDesktopButton");

        _QuitGameButton.Pressed += () => QuitGameRequested?.Invoke();
        _ExitToDesktopButton.Pressed += () => ExitToDesktopRequested?.Invoke();
    }

    public void Open()
    {
        Visible = true;
        GetTree().Paused = true;
    }

    public void Close()
    {
        Visible = false;
        GetTree().Paused = false;
    }

    // Fires while paused because this node's ProcessMode is Always. GameSessionPanel._UnhandledInput
    // handles the open half of the Escape toggle, but that node is Pausable so it stops receiving
    // input once paused - this handler is what makes the close half work.
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible)
            return;

        if (@event.IsActionPressed("ui_cancel"))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }
}
