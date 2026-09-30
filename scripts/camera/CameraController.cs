using Godot;

namespace AlienColony.Camera;

public partial class CameraController : Camera2D
{
    [Export] public float ZoomStep { get; set; } = 0.1f;
    [Export] public float MinZoom { get; set; } = 0.45f;
    [Export] public float MaxZoom { get; set; } = 2.0f;
    [Export] public float KeyboardSpeed { get; set; } = 650f;

    private bool _dragging;
    private Vector2 _lastMousePosition;

    public override void _Process(double delta)
    {
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        if (direction != Vector2.Zero)
            Position += direction * KeyboardSpeed * (float)delta / Zoom.X;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton button)
        {
            if (button.ButtonIndex == MouseButton.Middle)
            {
                _dragging = button.Pressed;
                _lastMousePosition = button.Position;
                GetViewport().SetInputAsHandled();
            }

            if (button.Pressed && button.ButtonIndex == MouseButton.WheelUp)
            {
                ChangeZoom(ZoomStep);
                GetViewport().SetInputAsHandled();
            }

            if (button.Pressed && button.ButtonIndex == MouseButton.WheelDown)
            {
                ChangeZoom(-ZoomStep);
                GetViewport().SetInputAsHandled();
            }
        }

        if (@event is InputEventMouseMotion motion && _dragging)
        {
            Vector2 delta = motion.Position - _lastMousePosition;
            Position -= delta / Zoom.X;
            _lastMousePosition = motion.Position;
            GetViewport().SetInputAsHandled();
        }
    }

    private void ChangeZoom(float amount)
    {
        float next = Mathf.Clamp(Zoom.X + amount, MinZoom, MaxZoom);
        Zoom = Vector2.One * next;
    }
}
