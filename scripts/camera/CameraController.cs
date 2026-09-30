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

    private bool _mouseHeld;
    private bool _mouseDragging;
    private Vector2 _mousePressPosition;
    private Vector2 _lastMousePosition;

    public override void _Ready()
    {
        _placement =
            GetNode<PlacementManager>(
                "../PlacementManager"
            );

        SetProcessInput(true);
    }

    public override void _Process(double delta)
    {
        Vector2 direction =
            Input.GetVector(
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

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventScreenDrag touchDrag)
        {
            if (_placement.IsPlacing)
                return;

            Position -=
                touchDrag.Relative /
                Zoom.X;

            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventMagnifyGesture magnify)
        {
            float next =
                Mathf.Clamp(
                    Zoom.X * magnify.Factor,
                    MinZoom,
                    MaxZoom
                );

            Zoom =
                Vector2.One * next;

            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is not InputEventMouseButton button)
        {
            if (@event is InputEventMouseMotion motion &&
                _mouseHeld &&
                !_placement.IsPlacing)
            {
                if (!_mouseDragging)
                {
                    if (motion.Position.DistanceTo(
                            _mousePressPosition) <
                        DragThreshold)
                        return;

                    _mouseDragging = true;
                }

                Vector2 delta =
                    motion.Position -
                    _lastMousePosition;

                Position -=
                    delta /
                    Zoom.X;

                _lastMousePosition =
                    motion.Position;

                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (button.Pressed &&
            button.ButtonIndex ==
            MouseButton.WheelUp)
        {
            ChangeZoom(ZoomStep);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (button.Pressed &&
            button.ButtonIndex ==
            MouseButton.WheelDown)
        {
            ChangeZoom(-ZoomStep);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (button.ButtonIndex !=
            MouseButton.Left &&
            button.ButtonIndex !=
            MouseButton.Middle &&
            button.ButtonIndex !=
            MouseButton.Right)
            return;

        if (_placement.IsPlacing)
        {
            _mouseHeld = false;
            _mouseDragging = false;
            return;
        }

        if (button.Pressed)
        {
            _mouseHeld = true;
            _mouseDragging = false;
            _mousePressPosition =
                button.Position;

            _lastMousePosition =
                button.Position;
        }
        else
        {
            _mouseHeld = false;
            _mouseDragging = false;
        }
    }

    private void ChangeZoom(float amount)
    {
        float next =
            Mathf.Clamp(
                Zoom.X + amount,
                MinZoom,
                MaxZoom
            );

        Zoom =
            Vector2.One * next;
    }
}
