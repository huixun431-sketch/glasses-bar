using Godot;

namespace GlassesBar;

public partial class OpeningMenuController : CanvasLayer
{
    [Signal] public delegate void StartRequestedEventHandler();
    [Signal] public delegate void QuitRequestedEventHandler();

    private Button _start = null!;
    private Button _quit = null!;

    public override void _Ready()
    {
        _start = GetNode<Button>("Backdrop/MenuPanel/Margin/Stack/Start");
        _quit = GetNode<Button>("Backdrop/MenuPanel/Margin/Stack/Quit");
        _start.Pressed += () => EmitSignal(SignalName.StartRequested);
        _quit.Pressed += () => EmitSignal(SignalName.QuitRequested);
        GameSession.Instance.GameStartedChanged += OnGameStartedChanged;
        OnGameStartedChanged(GameSession.Instance.GameStarted);
    }

    private void OnGameStartedChanged(bool started)
    {
        Visible = !started;
        if (started)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            return;
        }

        Input.MouseMode = Input.MouseModeEnum.Visible;
        _start.GrabFocus();
    }
}
