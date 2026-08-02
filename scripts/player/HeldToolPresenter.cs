using Godot;

namespace GlassesBar;

/// <summary>
/// Consumes hand-state notifications and updates only the first-person held-tool presentation.
/// Gameplay code does not depend on this implementation.
/// </summary>
internal sealed class HeldToolPresenter
{
    private readonly Node3D _leftHandAnchor;
    private readonly Node3D _rightHandAnchor;
    private readonly MeshInstance3D _leftHandVisual;
    private readonly MeshInstance3D _rightHandVisual;
    private readonly Label3D _leftHandLabel;
    private readonly Label3D _rightHandLabel;
    private DrinkWorkstation? _workstation;
    private Node3D? _leftAssetVisual;
    private Node3D? _rightAssetVisual;
    private string _leftToolId = string.Empty;
    private string _rightToolId = string.Empty;

    public HeldToolPresenter(
        Node3D leftHandAnchor,
        Node3D rightHandAnchor,
        MeshInstance3D leftHandVisual,
        MeshInstance3D rightHandVisual,
        Label3D leftHandLabel,
        Label3D rightHandLabel)
    {
        _leftHandAnchor = leftHandAnchor;
        _rightHandAnchor = rightHandAnchor;
        _leftHandVisual = leftHandVisual;
        _rightHandVisual = rightHandVisual;
        _leftHandLabel = leftHandLabel;
        _rightHandLabel = rightHandLabel;
        GameSession.Instance.WorldModeChanged += OnWorldModeChanged;
    }

    public void Bind(DrinkWorkstation workstation)
    {
        if (_workstation is not null)
        {
            _workstation.HandsChanged -= UpdateLabelsAndVisibility;
            _workstation.HandToolIdsChanged -= UpdateMeshes;
        }

        _workstation = workstation;
        _workstation.HandsChanged += UpdateLabelsAndVisibility;
        _workstation.HandToolIdsChanged += UpdateMeshes;
        UpdateLabelsAndVisibility(workstation.LeftHandDisplayName, workstation.RightHandDisplayName);
        UpdateMeshes(workstation.LeftHandToolId, workstation.RightHandToolId);
    }

    private void UpdateLabelsAndVisibility(string leftHand, string rightHand)
    {
        var leftOccupied = !string.IsNullOrWhiteSpace(leftHand) && leftHand != "空";
        var rightOccupied = !string.IsNullOrWhiteSpace(rightHand) && rightHand != "空";
        _leftHandVisual.Visible = leftOccupied && _leftAssetVisual is null;
        _rightHandVisual.Visible = rightOccupied && _rightAssetVisual is null;
        if (_leftAssetVisual is not null)
            _leftAssetVisual.Visible = leftOccupied && _leftAssetVisual.GetMeta("asset_id").AsString() == _leftToolId;
        if (_rightAssetVisual is not null)
            _rightAssetVisual.Visible = rightOccupied && _rightAssetVisual.GetMeta("asset_id").AsString() == _rightToolId;
        _leftHandLabel.Visible = false;
        _rightHandLabel.Visible = false;
        _leftHandLabel.Text = leftHand;
        _rightHandLabel.Text = rightHand;
    }

    private void UpdateMeshes(string leftToolId, string rightToolId)
    {
        _leftToolId = leftToolId;
        _rightToolId = rightToolId;
        _leftHandVisual.Mesh = leftToolId switch
        {
            "mortar" => new CylinderMesh { TopRadius = 0.135f, BottomRadius = 0.165f, Height = 0.16f },
            "traditional_filter" => new CylinderMesh { TopRadius = 0.12f, BottomRadius = 0.07f, Height = 0.22f },
            _ => new CylinderMesh { TopRadius = 0.075f, BottomRadius = 0.06f, Height = 0.22f }
        };
        _rightHandVisual.Mesh = rightToolId switch
        {
            "pestle" => new CylinderMesh { TopRadius = 0.034f, BottomRadius = 0.049f, Height = 0.3f },
            "jigger_small" => new CylinderMesh { TopRadius = 0.055f, BottomRadius = 0.055f, Height = 0.15f },
            "jigger_medium" => new CylinderMesh { TopRadius = 0.065f, BottomRadius = 0.065f, Height = 0.18f },
            "jigger_large" => new CylinderMesh { TopRadius = 0.075f, BottomRadius = 0.075f, Height = 0.21f },
            "ice_tongs" => new BoxMesh { Size = new Vector3(0.08f, 0.06f, 0.4f) },
            _ => new BoxMesh { Size = new Vector3(0.14f, 0.08f, 0.3f) }
        };
        var leftUsesAsset = UpdateAssetVisual(_leftHandAnchor, ref _leftAssetVisual, leftToolId);
        var rightUsesAsset = UpdateAssetVisual(_rightHandAnchor, ref _rightAssetVisual, rightToolId);
        _leftHandVisual.Visible = !string.IsNullOrWhiteSpace(leftToolId) && !leftUsesAsset;
        _rightHandVisual.Visible = !string.IsNullOrWhiteSpace(rightToolId) && !rightUsesAsset;
    }

    private static bool UpdateAssetVisual(Node3D handAnchor, ref Node3D? current, string toolId)
    {
        if (string.IsNullOrWhiteSpace(toolId))
        {
            if (current is not null)
                current.Visible = false;
            return false;
        }

        if (current is not null && current.GetMeta("asset_id").AsString() == toolId)
        {
            current.Visible = true;
            ToolVisualLibrary.ApplyWorldStyle(current, GameSession.Instance.WorldMode);
            return true;
        }

        if (current is not null)
        {
            handAnchor.RemoveChild(current);
            current.Free();
            current = null;
        }

        var visual = ToolVisualLibrary.Instantiate(toolId);
        if (visual is null)
            return false;

        visual.Name = "HeldAssetVisual";
        handAnchor.AddChild(visual);
        AlignGripToHand(visual);
        ToolVisualLibrary.ApplyWorldStyle(visual, GameSession.Instance.WorldMode);
        current = visual;
        return true;
    }

    private static void AlignGripToHand(Node3D visual)
    {
        var grip = ToolVisualLibrary.FindAnchor(visual, "Grip");
        if (grip is null)
            return;
        var gripInVisual = visual.GlobalTransform.AffineInverse() * grip.GlobalTransform;
        visual.Position = -(visual.Basis * gripInVisual.Origin);
    }

    private void OnWorldModeChanged(int mode)
    {
        if (_leftAssetVisual is not null)
            ToolVisualLibrary.ApplyWorldStyle(_leftAssetVisual, (WorldMode)mode);
        if (_rightAssetVisual is not null)
            ToolVisualLibrary.ApplyWorldStyle(_rightAssetVisual, (WorldMode)mode);
    }
}
