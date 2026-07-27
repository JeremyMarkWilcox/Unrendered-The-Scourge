using Godot;
using Unrendered.Core;
using Unrendered.Data;
using Unrendered.Managers;

namespace Unrendered.World;

/// <summary>
/// Draws one entity as it appears in one realm. Two of these exist per placed entity — a
/// Waking one and a Dream one — and each simply asks
/// <see cref="EntityStateManager.Resolve"/> what it should look like. Neither ever branches
/// on state itself, so adding a fourth condition later means editing the table and nothing
/// else.
/// </summary>
public partial class EntityOverlay : Node2D
{
    private EntityPlacement _placement;
    private GameRealm _realm;
    private EntityPresentation _presentation;

    public EntityPresentation Presentation => _presentation;
    public string InstanceId => _placement?.InstanceId ?? "";

    public void Setup(EntityPlacement placement, GameRealm realm)
    {
        _placement = placement;
        _realm = realm;

        Position = realm == GameRealm.Dream ? placement.DreamPosition : placement.WakingPosition;
        var s = realm == GameRealm.Dream ? placement.DreamScale : placement.WakingScale;
        Scale = new Vector2(s, s);

        Refresh();
    }

    public override void _Ready()
    {
        var bus = EventBus.Instance;
        if (bus == null) return;

        bus.EntityStateChanged += OnEntityStateChanged;
    }

    public override void _ExitTree()
    {
        var bus = EventBus.Instance;
        if (bus == null) return;

        bus.EntityStateChanged -= OnEntityStateChanged;
    }

    private void OnEntityStateChanged(string entityId, int newState)
    {
        if (entityId != InstanceId) return;
        Refresh();
    }

    public void Refresh()
    {
        if (_placement?.Entity == null) return;

        _presentation = EntityStateManager.Instance?.Resolve(_placement.Entity, _placement.InstanceId, _realm);
        Visible = _presentation?.Visible ?? false;
        QueueRedraw();
    }

    /// <summary>Screen-space bounds of the drawn figure, in this node's parent space.</summary>
    public Rect2 Bounds()
    {
        var size = _presentation?.PlaceholderSize ?? new Vector2(64, 64);
        if (_presentation?.Texture != null) size = _presentation.Texture.GetSize();

        size *= Scale;

        // Figures are anchored at their feet.
        return new Rect2(Position - new Vector2(size.X * 0.5f, size.Y), size);
    }

    public override void _Draw()
    {
        if (_presentation is not { Visible: true }) return;

        if (_presentation.Texture != null)
        {
            var size = _presentation.Texture.GetSize();
            DrawTextureRect(
                _presentation.Texture,
                new Rect2(-size.X * 0.5f, -size.Y, size.X, size.Y),
                tile: false,
                modulate: _presentation.Modulate);
            return;
        }

        DrawPlaceholderSilhouette();
    }

    /// <summary>
    /// Stand-in figure. Nightmares get a ragged, asymmetric hulk; porcelain nobles get a
    /// clean vertical column with a head. Crude, but you can tell the three forms apart
    /// at a glance, which is the whole point of having it.
    /// </summary>
    private void DrawPlaceholderSilhouette()
    {
        var size = _presentation.PlaceholderSize;
        var baseColour = _presentation.PlaceholderColor;
        var tint = new Color(baseColour.R, baseColour.G, baseColour.B, _presentation.Modulate.A);
        var w = size.X;
        var h = size.Y;

        // Contact shadow. Squashed and faint — a full circle at full strength reads as a
        // hole punched in the ground rather than something standing on it.
        DrawSetTransform(Vector2.Zero, 0f, new Vector2(1f, 0.22f));
        DrawCircle(Vector2.Zero, w * 0.44f, new Color(0, 0, 0, 0.20f * tint.A));
        DrawSetTransform(Vector2.Zero, 0f, Vector2.One);

        if (_presentation.PlaceholderJagged)
        {
            // Hunched, wrong number of limbs, low to the ground.
            var body = new[]
            {
                new Vector2(-w * 0.50f, 0f),
                new Vector2(-w * 0.42f, -h * 0.52f),
                new Vector2(-w * 0.18f, -h * 0.40f),
                new Vector2(-w * 0.05f, -h * 0.86f),
                new Vector2(w * 0.14f, -h * 0.46f),
                new Vector2(w * 0.34f, -h * 0.62f),
                new Vector2(w * 0.46f, -h * 0.20f),
                new Vector2(w * 0.50f, 0f),
            };
            DrawColoredPolygon(body, tint);
            DrawCircle(new Vector2(w * 0.02f, -h * 0.82f), h * 0.10f, tint.Darkened(0.45f));

            // Two small lights where eyes would be.
            var eye = new Color(1f, 0.86f, 0.45f, tint.A);
            DrawCircle(new Vector2(-w * 0.03f, -h * 0.84f), h * 0.022f, eye);
            DrawCircle(new Vector2(w * 0.07f, -h * 0.84f), h * 0.022f, eye);
        }
        else
        {
            // Upright, symmetrical, unhurried.
            var body = new[]
            {
                new Vector2(-w * 0.34f, 0f),
                new Vector2(-w * 0.20f, -h * 0.62f),
                new Vector2(-w * 0.13f, -h * 0.76f),
                new Vector2(w * 0.13f, -h * 0.76f),
                new Vector2(w * 0.20f, -h * 0.62f),
                new Vector2(w * 0.34f, 0f),
            };
            DrawColoredPolygon(body, tint);
            DrawCircle(new Vector2(0, -h * 0.83f), h * 0.11f, tint.Lightened(0.25f));
        }

        // Dark royalty, allegedly. Nothing mundane wears one.
        if (_presentation.PlaceholderCrowned)
            DrawRect(new Rect2(-w * 0.17f, -h * 1.02f, w * 0.34f, h * 0.06f), tint.Darkened(0.35f));
    }
}
