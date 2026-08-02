using Godot;

namespace GlassesBar.Tests;

public partial class Stage1AssetVisualCapture : Node
{
    private Node3D _main = null!;
    private PlayerController _player = null!;
    private DrinkWorkstation _workstation = null!;
    private InteractionContext _context = null!;
    private int _frame;

    public override void _Ready() => CallDeferred(MethodName.Prepare);

    public override void _Process(double delta)
    {
        if (_context is null)
            return;

        _frame++;
        switch (_frame)
        {
            case 26:
                GameSession.Instance.ToggleWorld();
                break;
            case 46:
                GameSession.Instance.ToggleWorld();
                SetView(-3.75f, -13f);
                break;
            case 66:
                SetView(0f, -5f);
                _main.GetNode<ToolInteractable>("NeutralGameplay/mortar").Interact(_context);
                _main.GetNode<ToolInteractable>("NeutralGameplay/pestle").Interact(_context);
                break;
            case 91:
                _workstation.ResetForNewDay();
                _player.ResetForNewDay();
                SetView(0f, -5f);
                _main.GetNode<ToolInteractable>("NeutralGameplay/highball_glass").Interact(_context);
                _main.GetNode<ToolInteractable>("NeutralGameplay/jigger_medium").Interact(_context);
                break;
            case 116:
                GameSession.Instance.ToggleWorld();
                break;
            case 141:
                GetTree().Quit(0);
                break;
        }
    }

    private void Prepare()
    {
        _main = GetNode<Node3D>("Main");
        _main.GetNode<Button>("OpeningMenu/Backdrop/MenuPanel/Margin/Stack/Start")
            .EmitSignal(Button.SignalName.Pressed);
        _player = _main.GetNode<PlayerController>("Player");
        _workstation = _main.GetNode<DrinkWorkstation>("NeutralGameplay/DrinkWorkstation");
        _context = new InteractionContext { Player = _player, Workstation = _workstation };
        SetView(2.7f, -13f);
    }

    private void SetView(float x, float pitchDegrees)
    {
        _player.Position = new Vector3(x, 1.045f, -1.2f);
        _player.RotationDegrees = new Vector3(0f, 180f, 0f);
        _player.GetNode<Node3D>("Head").RotationDegrees = new Vector3(pitchDegrees, 0f, 0f);
    }
}
