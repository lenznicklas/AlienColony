using Godot;
using AlienColony.Systems;

namespace AlienColony.Camera;

public partial class CameraController : Camera2D
{
    [Export] public float ZoomStep { get; set; } = 0.1f;
    [Export] public float MinZoom { get; set; } = 0.45f;
    [Export] public float MaxZoom { get; set; } = 2.0f;
    [Export] public float KeyboardSpeed { get; set; } = 650f;
    [Export] public float DragThreshold { get; set; } = 4f;

    private PlacementManager _placement = null!;

    private bool _mouseHeld;
    private bool _dragging;
    private MouseButton _heldButton;

    private Vector2 _pressPosition;
    private Vector2 _lastMousePosition;

    public override void _Ready()
    {
        _placement =
            GetNode<PlacementManager>("../PlacementManager");

        _placement.PlacementModeChanged += OnPlacementModeChanged;

        SetProcessInput(true);
    }

    public override void _ExitTree()
    {
        if (_placement != null)
            _placement.PlacementModeChanged -= OnPlacementModeChanged;
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

    public override void _Input(InputEvent @event)
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

            bool supportedDragButton =
                button.ButtonIndex == MouseButton.Left ||
                button.ButtonIndex == MouseButton.Right ||
                button.ButtonIndex == MouseButton.Middle;

            if (!supportedDragButton)
                return;

            // Placement owns left/right click completely.
            if (_placement.IsPlacing &&
                (button.ButtonIndex == MouseButton.Left ||
                 button.ButtonIndex == MouseButton.Right))
            {
                ResetDrag();
                return;
            }

            if (button.Pressed)
            {
                _mouseHeld = true;
                _dragging = false;
                _heldButton = button.ButtonIndex;

                _pressPosition = button.Position;
                _lastMousePosition = button.Position;
            }
            else if (_mouseHeld &&
                     button.ButtonIndex == _heldButton)
            {
                ResetDrag();
            }

            return;
        }

        if (@event is not InputEventMouseMotion motion)
            return;

        if (!_mouseHeld)
            return;

        // If placement began after mouse down (e.g. clicking a UI button),
        // kill the pending drag immediately.
        if (_placement.IsPlacing &&
            (_heldButton == MouseButton.Left ||
             _heldButton == MouseButton.Right))
        {
            ResetDrag();
            return;
        }

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

        _lastMousePosition = motion.Position;

        GetViewport().SetInputAsHandled();
    }

    private void OnPlacementModeChanged(bool active)
    {
        if (active)
            ResetDrag();
    }

    private void ResetDrag()
    {
        _mouseHeld = false;
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
