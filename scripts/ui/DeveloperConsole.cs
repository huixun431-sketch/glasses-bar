using System;
using System.Globalization;
using Godot;

namespace GlassesBar;

public partial class DeveloperConsole : CanvasLayer
{
    public static bool IsOpen { get; private set; }

    private Control _panel = null!;
    private Label _output = null!;
    private LineEdit _input = null!;
    private MyopiaEffectController _myopia = null!;

    public override void _Ready()
    {
        _panel = GetNode<Control>("Panel");
        _output = GetNode<Label>("Panel/Margin/Stack/Output");
        _input = GetNode<LineEdit>("Panel/Margin/Stack/Input");
        _myopia = GetNode<MyopiaEffectController>("../MyopiaEffectController");
        _input.TextSubmitted += Execute;
        SetOpen(false);
    }

    public override void _Input(InputEvent @event)
    {
        if (!GameSession.Instance.GameStarted)
            return;
        if (@event is not InputEventKey key || !key.Pressed || key.Echo || key.PhysicalKeycode != Key.Quoteleft)
            return;

        SetOpen(!IsOpen);
        GetViewport().SetInputAsHandled();
    }

    private void SetOpen(bool open)
    {
        IsOpen = open;
        _panel.Visible = open;
        if (open)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _input.GrabFocus();
            _output.Text = $"开发控制台｜当前近视 {_myopia.MyopiaDegrees:0} 度\n输入 help 查看命令，按 ` 关闭";
        }
        else
        {
            _input.ReleaseFocus();
            if (!GetTree().Paused && GameSession.Instance.CanMove)
                Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    private void Execute(string rawCommand)
    {
        var parts = rawCommand.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        _input.Clear();
        if (parts.Length == 0)
            return;

        var command = parts[0].ToLowerInvariant();
        switch (command)
        {
            case "help":
                _output.Text = "命令：\nmyopia <0-1000>  设置现实世界近视度数\nmyopia             查看当前度数\nclear              清空输出\nclose              关闭控制台";
                break;
            case "myopia":
                if (parts.Length == 1)
                {
                    _output.Text = $"当前近视：{_myopia.MyopiaDegrees:0} 度｜Blur {_myopia.BlurRadius:0.00}";
                    break;
                }

                if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var degrees))
                {
                    _output.Text = "格式错误：myopia <0-1000>";
                    break;
                }

                _myopia.SetMyopiaDegrees(degrees);
                _output.Text = $"已设置近视：{_myopia.MyopiaDegrees:0} 度｜摘镜时生效";
                break;
            case "clear":
                _output.Text = string.Empty;
                break;
            case "close":
                SetOpen(false);
                break;
            default:
                _output.Text = $"未知命令：{parts[0]}。输入 help 查看命令。";
                break;
        }

        if (IsOpen)
            _input.GrabFocus();
    }
}
