using Godot;

namespace GlassesBar;

/// <summary>
/// Consumes hand-state notifications and updates only the first-person held-tool presentation.
/// Gameplay code does not depend on this implementation.
/// </summary>
internal sealed class HeldToolPresenter
{
    private readonly MeshInstance3D _leftHandVisual;
    private readonly MeshInstance3D _rightHandVisual;
    private readonly Label3D _leftHandLabel;
    private readonly Label3D _rightHandLabel;
    private DrinkWorkstation? _workstation;

    public HeldToolPresenter(
        MeshInstance3D leftHandVisual,
        MeshInstance3D rightHandVisual,
        Label3D leftHandLabel,
        Label3D rightHandLabel)
    {
        _leftHandVisual = leftHandVisual;
        _rightHandVisual = rightHandVisual;
        _leftHandLabel = leftHandLabel;
        _rightHandLabel = rightHandLabel;
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
        _leftHandVisual.Visible = !string.IsNullOrWhiteSpace(leftHand) && leftHand != "空";
        _rightHandVisual.Visible = !string.IsNullOrWhiteSpace(rightHand) && rightHand != "空";
        _leftHandLabel.Visible = false;
        _rightHandLabel.Visible = false;
        _leftHandLabel.Text = leftHand;
        _rightHandLabel.Text = rightHand;
    }

    private void UpdateMeshes(string leftToolId, string rightToolId)
    {
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
    }
}
