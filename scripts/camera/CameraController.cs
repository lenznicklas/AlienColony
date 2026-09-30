using Godot;
using AlienColony.Systems;

namespace AlienColony.Camera;

public partial class CameraController : Camera2D
{
    [Export] public float ZoomStep { get; set; } = 0.1f;
    [Export] public float MinZoom { get; set; } = 0.45f;
    [Export] public float MaxZoom { get; set; } = 2.0f;
    [Export] public float KeyboardSpeed { get; set; } = 650f;
    [Export] public float DragThreshold { get; set; } = 5f;

    private PlacementManager _placement = null!;

    private bool _leftPressed;
    private bool _rightPressed;
    private bool _middlePressed;
    private bool _dragging;

    private Vector2 _pressPosition;
    private Vector2 _lastMousePosition;

    public override void _Ready()
    {
        _placement =
            GetNode<PlacementManager>("../PlacementManager");
    }

    public override void _Process(double delta)
    {
        Vector2 direction = Input.GetVector(
            "ui_left",
            "ui_right",
            "ui_up",
            "ui_down"
        );

        if (direction != Vector2.Zero)
        {
            Position +=
                direction *
                KeyboardSpeed *
                (float)delta /
                Zoom.X;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton button)
        {
            if (button.Pressed &&
                button.ButtonIndex == MouseButton.WheelUp)
            {
                ChangeZoom(ZoomStep);
                GetViewport().SetInputAsHandled();
                return;
            }

            if (button.Pressed &&
                button.ButtonIndex == MouseButton.WheelDown)
            {
                ChangeZoom(-ZoomStep);
                GetViewport().SetInputAsHandled();
                return;
            }

            // During building placement, left click belongs entirely
            // to PlacementManager.
            if (button.ButtonIndex == MouseButton.Left)
            {
                if (_placement.IsPlacing)
                    return;

                _leftPressed = button.Pressed;

                if (button.Pressed)
                    BeginPotentialDrag(button.Position);
                else
                    EndDrag();

                return;
            }

            if (button.ButtonIndex == MouseButton.Right)
            {
                // Right drag pans as well when not placing.
                if (_placement.IsPlacing)
                    return;

                _rightPressed = button.Pressed;

                if (button.Pressed)
                    BeginPotentialDrag(button.Position);
                else
                    EndDrag();

                return;
            }

            if (button.ButtonIndex == MouseButton.Middle)
            {
                _middlePressed = button.Pressed;

                if (button.Pressed)
                    BeginPotentialDrag(button.Position);
                else
                    EndDrag();

                return;
            }
        }

        if (@event is InputEventMouseMotion motion)
        {
            bool held =
                _leftPressed ||
                _rightPressed ||
                _middlePressed;

            if (!held)
                return;

            if (!_dragging)
            {
                if (motion.Position.DistanceTo(_pressPosition) <
                    DragThreshold)
                    return;

                _dragging = true;
            }

            Vector2 delta =
                motion.Position - _lastMousePosition;

            Position -= delta / Zoom.X;

            _lastMousePosition =
                motion.Position;

            GetViewport().SetInputAsHandled();
        }
    }

    private void BeginPotentialDrag(Vector2 mousePosition)
    {
        _pressPosition = mousePosition;
        _lastMousePosition = mousePosition;
        _dragging = false;
    }

    private void EndDrag()
    {
        _leftPressed = false;
        _rightPressed = false;
        _middlePressed = false;
        _dragging = false;
    }

    private void ChangeZoom(float amount)
    {
        float next = Mathf.Clamp(
            Zoom.X + amount,
            MinZoom,
            MaxZoom
        );

        Zoom = Vector2.One * next;
    }
}
