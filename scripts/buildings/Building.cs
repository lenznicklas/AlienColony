using Godot;
using System;

namespace AlienColony.Buildings;

public partial class Building : Node2D
{
    public event Action<Building>? Selected;

    [Export] public string BuildingId { get; set; } = "building";
    [Export] public string DisplayName { get; set; } = "Building";
    [Export] public Vector2I GridSize { get; set; } = Vector2I.One;
    [Export] public int CellSize { get; set; } = 64;

    public bool InputEnabled { get; set; } = true;

    public Vector2I GridOrigin { get; set; }
    public bool IsSelected { get; private set; }

    private Sprite2D? _sprite;
    private bool _textureLoaded;

    public override void _Ready()
    {
        LoadBuildingTexture();
        QueueRedraw();
    }

    // Selection stays on unhandled input on purpose:
    // placement/camera get first chance via _Input().
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!InputEnabled)
            return;

        if (@event is not InputEventMouseButton mouse ||
            !mouse.Pressed ||
            mouse.ButtonIndex != MouseButton.Left)
            return;

        Vector2 local = ToLocal(GetGlobalMousePosition());
        Vector2 pixelSize =
            new(GridSize.X * CellSize, GridSize.Y * CellSize);

        Rect2 bounds =
            new(-pixelSize * 0.5f, pixelSize);

        if (!bounds.HasPoint(local))
            return;

        Selected?.Invoke(this);
        GetViewport().SetInputAsHandled();
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 pixelSize =
            new(GridSize.X * CellSize, GridSize.Y * CellSize);

        Rect2 footprint =
            new(-pixelSize * 0.5f, pixelSize);

        if (!_textureLoaded)
        {
            DrawRect(
                footprint.Grow(-5),
                new Color("#798c84"),
                true
            );

            DrawRect(
                footprint.Grow(-5),
                Colors.White,
                false,
                3f
            );
        }

        if (IsSelected)
        {
            DrawRect(
                footprint.Grow(6),
                new Color(0.25f, 0.9f, 1f, 0.18f),
                true
            );

            DrawRect(
                footprint.Grow(6),
                new Color("#63e2ff"),
                false,
                4f
            );
        }
    }

    private void LoadBuildingTexture()
    {
        if (!BuildingCatalog.TryGet(
                BuildingId,
                out BuildingDefinition definition))
        {
            GD.PushWarning(
                $"Unknown building id: {BuildingId}"
            );
            return;
        }

        Texture2D? texture =
            GD.Load<Texture2D>(definition.TexturePath);

        if (texture == null)
        {
            GD.PushError(
                $"Could not load building texture: " +
                $"{definition.TexturePath}"
            );
            return;
        }

        Vector2 textureSize = texture.GetSize();

        if (textureSize.X <= 0 ||
            textureSize.Y <= 0)
            return;

        _sprite = new Sprite2D
        {
            Name = "BuildingSprite",
            Texture = texture,
            Centered = true,
            ZIndex = 10
        };

        AddChild(_sprite);

        float targetWidth =
            GridSize.X * CellSize * 1.25f;

        float targetHeight =
            GridSize.Y * CellSize * 1.70f;

        float scale = Mathf.Max(
            targetWidth / textureSize.X,
            targetHeight / textureSize.Y
        );

        scale = Mathf.Clamp(
            scale,
            0.02f,
            4.0f
        );

        _sprite.Scale =
            Vector2.One * scale;

        _sprite.Position =
            new Vector2(
                0,
                -GridSize.Y * CellSize * 0.20f
            );

        _textureLoaded = true;
        QueueRedraw();
    }
}
