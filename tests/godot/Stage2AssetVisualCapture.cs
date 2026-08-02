using Godot;

namespace GlassesBar.Tests;

public partial class Stage2AssetVisualCapture : Node
{
    private Node3D _main = null!;
    private PlayerController _player = null!;
    private DrinkWorkstation _workstation = null!;
    private InteractionContext _context = null!;
    private CabinetInteractable _iceDrawer = null!;
    private int _frame;

    public override void _Ready() => CallDeferred(MethodName.Prepare);

    public override void _Process(double delta)
    {
        if (_context is null)
            return;

        _frame++;
        switch (_frame)
        {
            case 24:
                GameSession.Instance.ToggleWorld();
                break;
            case 44:
                GameSession.Instance.ToggleWorld();
                SetView(0f, -5f);
                Hold("traditional_filter", "bean_scoop");
                break;
            case 68:
                SetView(-2f, -30f, -2.3f);
                _iceDrawer.SetOpen(true, false);
                break;
            case 72:
                ResetHands();
                SetView(-2f, -30f, -2.3f);
                Hold("traditional_filter", "ice_tongs");
                break;
            case 86:
                SetView(3.45f, -13f);
                break;
            case 100:
                ResetHands();
                SetView(0f, -5f);
                Hold("highball_glass", "jigger_small");
                break;
            case 128:
                ResetHands();
                Hold("highball_glass", "jigger_large");
                break;
            case 156:
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
        _iceDrawer = _main.GetNode<CabinetInteractable>("NeutralGameplay/front_drawer_2_upper");
        _context = new InteractionContext { Player = _player, Workstation = _workstation };
        SetView(-2.7f, -13f);
    }

    private void Hold(string leftId, string rightId)
    {
        _main.GetNode<ToolInteractable>($"NeutralGameplay/{leftId}").Interact(_context);
        _main.GetNode<ToolInteractable>($"NeutralGameplay/{rightId}").Interact(_context);
    }

    private void ResetHands()
    {
        _workstation.ResetForNewDay();
        _player.ResetForNewDay();
    }

    private void SetView(float x, float pitchDegrees, float z = -1.2f)
    {
        _player.Position = new Vector3(x, 1.045f, z);
        _player.RotationDegrees = new Vector3(0f, 180f, 0f);
        _player.GetNode<Node3D>("Head").RotationDegrees = new Vector3(pitchDegrees, 0f, 0f);
    }
}
