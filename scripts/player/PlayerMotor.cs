using Godot;
using GlassesBar.Domain;

namespace GlassesBar;

/// <summary>
/// Owns locomotion, view rotation and player-pose persistence. It does not issue gameplay actions.
/// </summary>
internal sealed class PlayerMotor
{
    private readonly CharacterBody3D _body;
    private readonly Node3D _head;
    private readonly Transform3D _dayStartTransform;
    private readonly Vector3 _dayStartHeadRotation;

    public PlayerMotor(CharacterBody3D body, Node3D head)
    {
        _body = body;
        _head = head;
        _dayStartTransform = body.Transform;
        _dayStartHeadRotation = head.Rotation;
    }

    public void ApplyLook(Vector2 relativeMotion, float sensitivity)
    {
        _body.RotateY(-relativeMotion.X * sensitivity);
        _head.RotateX(-relativeMotion.Y * sensitivity);
        var rotation = _head.Rotation;
        rotation.X = Mathf.Clamp(rotation.X, -1.45f, 1.45f);
        _head.Rotation = rotation;
    }

    public void Move(double delta, float moveSpeed, float gravity, bool acceptsMovementInput)
    {
        var velocity = _body.Velocity;
        if (!_body.IsOnFloor())
            velocity.Y -= gravity * (float)delta;
        else if (velocity.Y < 0f)
            velocity.Y = 0f;

        var input = acceptsMovementInput
            ? Input.GetVector("move_left", "move_right", "move_forward", "move_back")
            : Vector2.Zero;
        var direction = (_body.Transform.Basis * new Vector3(input.X, 0f, input.Y)).Normalized();
        velocity.X = direction.X * moveSpeed;
        velocity.Z = direction.Z * moveSpeed;
        _body.Velocity = velocity;
        _body.MoveAndSlide();
    }

    public void ResetForNewDay()
    {
        _body.Transform = _dayStartTransform;
        _head.Rotation = _dayStartHeadRotation;
        _body.Velocity = Vector3.Zero;
    }

    public PlayerSnapshot CaptureState() => new()
    {
        Position = ToSpatialPosition(_body.Position),
        BodyRotation = ToSpatialPosition(_body.Rotation),
        HeadRotation = ToSpatialPosition(_head.Rotation)
    };

    public void RestoreState(PlayerSnapshot snapshot)
    {
        _body.Position = ToVector3(snapshot.Position);
        _body.Rotation = ToVector3(snapshot.BodyRotation);
        _head.Rotation = ToVector3(snapshot.HeadRotation);
        _body.Velocity = Vector3.Zero;
    }

    private static SpatialPosition ToSpatialPosition(Vector3 value) => new(value.X, value.Y, value.Z);

    private static Vector3 ToVector3(SpatialPosition value) =>
        new((float)value.X, (float)value.Y, (float)value.Z);
}
