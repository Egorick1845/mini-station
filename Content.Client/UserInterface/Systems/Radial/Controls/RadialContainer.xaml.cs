using System.Linq;
using System.Numerics;
using System.Text;
using Content.Client.Message;
using Robust.Client.Animations;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Animations;

namespace Content.Client.UserInterface.Systems.Radial.Controls;

[GenerateTypedNameReferences, Virtual]
public partial class RadialContainer : Control
{
    private EntityUid? _attachedEntity;

    private bool _isOpened = false;
    private const int MaxButtons = 8;

    private const string MoveAnimationKey = "move";
    private const string InSizeAnimationKey = "insize";
    private const string OutSizeAnimationKey = "outsize";

    /// <summary>
    /// Fired, when radial was died(closed)
    /// </summary>
    public Action<RadialContainer>? OnClose;

    public Action<RadialContainer>? OnAttached;
    public Action<RadialContainer>? OnDetached;

    /// <summary>
    /// Radial item size, when cursor was focused on button
    /// </summary>
    public float FocusSize { get; set; } = 64f;

    /// <summary>
    /// Normal radial item size, when cursor not focused
    /// </summary>
    public float NormalSize { get; set; } = 50f;

    /// <summary>
    /// Items moving animation time, when radial was opened
    /// </summary>
    public float MoveAnimationTime { get; set; } = 0.3f;

    /// <summary>
    /// Item focus sizing animation, when cursor focused and leaved
    /// </summary>
    public float FocusAnimationTime { get; set; } = 0.25f;

    /// <summary>
    /// Show or not item action text on radial menu
    /// </summary>
    public bool ActionVisible { get; set; } = true;

    /// <summary>
    /// Used for UIController! Radial menu position offset
    /// </summary>
    public float VerticalOffset { get; set; } = 0.0f;

    /// <summary>
    /// Used for UIController! Radius, where is container was available
    /// </summary>
    public float VisionRadius { get; set; } = 10.0f;

    /// <summary>
    /// Used for automatic set RadialItem.Controller texture
    /// </summary>
    public string ItemBackgroundTexturePath { get; set; }

    public EntityUid? AttachedEntity
    {
        get => _attachedEntity;
        set
        {
            if (value == EntityUid.Invalid || value == null)
                OnDetached?.Invoke(this);
            else
                OnAttached?.Invoke(this);

            _attachedEntity = value;
        }
    }

    public RadialContainer()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        var background = Theme.Path.ToString();
        ItemBackgroundTexturePath = background += "SlotBackground";
    }

    /// <summary>
    /// Open container
    /// </summary>
    public void Open()
    {
        if (_isOpened)
            return;

        _isOpened = !_isOpened;

        UpdateButtons();
    }

    /// <summary>
    /// Open container with position
    /// </summary>
    /// <param name="position"></param>
    public void Open(Vector2 position)
    {
        if (_isOpened)
            return;

        _isOpened = !_isOpened;

        LayoutContainer.SetPosition(this, position);
        UpdateButtons();
    }

    /// <summary>
    /// Open container on center screen
    /// </summary>
    public void OpenCentered()
    {
        if (_isOpened)
            return;

        _isOpened = !_isOpened;

        if (Parent == null)
            return;

        LayoutContainer.SetPosition(this, (Parent.Size / 2) - (Size / 2));
        UpdateButtons();
    }

    public void OpenCenteredLeft() => OpenCenteredAt(new Vector2(0.25f, 0.5f));

    /// <summary>
    /// Open container at screen position
    /// </summary>
    /// <param name="position"></param>
    public void OpenCenteredAt(Vector2 position)
    {
        if (_isOpened)
            return;

        _isOpened = !_isOpened;

        if (Parent == null)
            return;

        LayoutContainer.SetPosition(this, (Parent.Size * position) - (Size / 2));
        UpdateButtons();
    }

    /// <summary>
    /// Open on attached entity in the world.
    /// </summary>
    public void OpenAttached(EntityUid uid)
    {
        if (_isOpened)
            return;

        _isOpened = !_isOpened;

        AttachedEntity = uid;
        UpdateButtons();
    }

    /// <summary>
    /// Insert a new radial item (button) on the container
    /// </summary>
    /// <param name="button">Control</param>
    /// <returns>Item control</returns>
    public RadialItem AddButton(RadialItem button)
    {
        Layout.AddChild(button);
        return button;
    }

    /// <summary>
    /// Insert a new radial item (button) on the container
    /// </summary>
    /// <param name="action">Item content text</param>
    /// <param name="texturePath">Path to item's icon texture</param>
    /// <returns>Item control</returns>
    public RadialItem AddButton(string action, string? texturePath = null)
    {
        var button = new RadialItem();
        button.Content = action;
        button.Controller.TexturePath = ItemBackgroundTexturePath;

        if (texturePath != null)
            button.Icon.TexturePath = texturePath;

        Layout.AddChild(button);

        return button;
    }

    /// <summary>
    /// Insert a new radial item (button) on the container
    /// </summary>
    /// <param name="action">Item content text</param>
    /// <param name="texture">Item's icon texture</param>
    /// <returns></returns>
    public RadialItem AddButton(string action, Texture? texture)
    {
        var button = new RadialItem();
        button.Content = action;
        button.Controller.TexturePath = ItemBackgroundTexturePath;

        if (texture != null)
            button.Icon.Texture = texture;

        Layout.AddChild(button);

        return button;
    }

    private void UpdateButtons()
    {
        Visible = true;

        var angleDegrees = 360 / Layout.ChildCount;
        var stepAngle = -angleDegrees + -90;
        var distance = GetDistance();

        foreach (var child in Layout.Children)
        {
            var button = (RadialItem) child;
            button.ButtonSize = new Vector2(NormalSize, NormalSize);
            stepAngle += angleDegrees;
            var pos = GetPointFromPolar(stepAngle, distance);
            PlayRadialAnimation(button, pos, MoveAnimationKey);

            button.Controller.OnMouseEntered += (_) =>
            {
                PlaySizeAnimation(button, FocusSize, OutSizeAnimationKey, InSizeAnimationKey);
                ActionLabel.SetMarkup(button.Content ?? string.Empty);
                ActionLabel.Visible = ActionVisible;
            };
            button.Controller.OnMouseExited += (_) =>
            {
                PlaySizeAnimation(button, NormalSize, InSizeAnimationKey, OutSizeAnimationKey);
                ActionLabel.Visible = false;
            };
        }

        CloseButton.ButtonSize = new Vector2(NormalSize, NormalSize);

        CloseButton.Controller.OnMouseEntered += (_) =>
        {
            PlaySizeAnimation(CloseButton, FocusSize, OutSizeAnimationKey, InSizeAnimationKey);
            ActionLabel.SetMarkup(CloseButton.Content ?? string.Empty);
            ActionLabel.Visible = true;
        };

        CloseButton.Controller.OnMouseExited += (_) =>
        {
            PlaySizeAnimation(CloseButton, NormalSize, InSizeAnimationKey, OutSizeAnimationKey);
            ActionLabel.Visible = false;
        };
        CloseButton.Controller.OnPressed += (_) =>
        {
            Dispose();
        };
    }

    private void PlayRadialAnimation(Control button, Vector2 pos, string playKey)
    {
        var anim = new Animation
        {
            Length = TimeSpan.FromMilliseconds(MoveAnimationTime * 1000),
            AnimationTracks =
            {
                new AnimationTrackControlProperty
                {
                    Property = nameof(RadialItem.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(0,0), 0f),
                        new AnimationTrackProperty.KeyFrame(pos, MoveAnimationTime)
                    }
                }
            }
        };
        if (!button.HasRunningAnimation(playKey))
            button.PlayAnimation(anim, playKey);
    }

    private void PlaySizeAnimation(Control button, float size, string playKey, string? stopKey)
    {
        var anim = new Animation
        {
            Length = TimeSpan.FromMilliseconds(FocusAnimationTime * 1000),
            AnimationTracks =
            {
                new AnimationTrackControlProperty
                {
                    Property = nameof(RadialItem.ButtonSize),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(button.Size, 0f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(size, size), FocusAnimationTime)
                    }
                }
            }
        };

        if (stopKey != null && button.HasRunningAnimation(stopKey))
            button.StopAnimation(stopKey);
        if (!button.HasRunningAnimation(playKey))
            button.PlayAnimation(anim, playKey);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        foreach (var child in Layout.Children)
        {
            var button = (RadialItem) child;
            LayoutContainer.SetPosition(child, button.Offset - (button.Size / 2));
        }

        // FIXME: We use item's offset like "local item position" for animation. Need make some better way to do it;
        LayoutContainer.SetPosition(CloseButton, CloseButton.Offset - (CloseButton.Size / 2));
        LayoutContainer.SetPosition(ActionLabel,
            new Vector2(0 - (GetTextWidth(ActionLabel) / 4), 18.5f + GetDistance(4.5f)));
    }

    private int GetTextWidth(RichTextLabel text)
    {
        var font = GetFont(text);
        var msg = text.GetMessage();
        if (msg == null)
            return 0;

        var width = 0;

        foreach (var t in msg)
        {
            var metrics = font.GetCharMetrics(new Rune(t), 1);

            if (metrics != null)
                width += metrics.Value.Width + metrics.Value.Advance;
        }

        return width;
    }

    protected override void Dispose(bool disposing)
    {
        OnClose?.Invoke(this);
        base.Dispose(disposing);
    }

    private Font GetFont(Control element)
    {
        if (element.TryGetStyleProperty<Font>("font", out var font))
        {
            return font;
        }

        return UserInterfaceManager.ThemeDefaults.DefaultFont;
    }

    private float GetDistance(float offset = 0.0f)
    {
        var distance = FocusSize * 1.2f;
        var children = Layout.Children.Count();

        if (children <= MaxButtons)
            return distance + offset;

        for (var i = 0; i < (children - MaxButtons); i++)
        {
            distance += (NormalSize / 3);
        }

        return distance + offset;
    }

    private static Vector2 GetPointFromPolar(double angleDegrees, double distance)
    {
        var angleRadians = angleDegrees * (Math.PI / 180.0);

        var x = distance * Math.Cos(angleRadians);
        var y = distance * Math.Sin(angleRadians);

        return new Vector2((float) Math.Round(x), (float) Math.Round(y));
    }
}
