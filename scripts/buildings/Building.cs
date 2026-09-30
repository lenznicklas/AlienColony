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
                footprint.Grow(-3),
                new Color(0.20f, 0.90f, 1f, 0.16f),
                true
            );

            DrawRect(
                footprint.Grow(-3),
                new Color("#63e2ff"),
                false,
                3f
            );
        }
    }

    private void LoadBuildingTexture()
    {
        if (!BuildingCatalog.TryGet(
                BuildingId,
                out BuildingDefinition definition))
        {
            GD.PushWarning($"Unknown building id: {BuildingId}");
            return;
        }

        Texture2D? texture =
            GD.Load<Texture2D>(definition.TexturePath);

        if (texture == null)
        {
            GD.PushError(
                $"Could not load building texture: {definition.TexturePath}"
            );
            return;
        }

        Vector2 textureSize = texture.GetSize();

        if (textureSize.X <= 0 || textureSize.Y <= 0)
            return;

        _sprite = new Sprite2D
        {
            Name = "BuildingSprite",
            Texture = texture,
            Centered = true,
            ZIndex = 10
        };

        AddChild(_sprite);

        // IMPORTANT:
        // The complete PNG must stay inside the logical grid footprint.
        // Leave a little padding so it never touches/crosses the grid border.
        float maxWidth =
            GridSize.X * CellSize * 0.88f;

        float maxHeight =
            GridSize.Y * CellSize * 0.88f;

        float scale = Mathf.Min(
            maxWidth / textureSize.X,
            maxHeight / textureSize.Y
        );

        scale = Mathf.Clamp(scale, 0.001f, 4.0f);

        _sprite.Scale = Vector2.One * scale;
        _sprite.Position = Vector2.Zero;

        _textureLoaded = true;
        QueueRedraw();
    }
}
