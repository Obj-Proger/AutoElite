using AutoElite.Mobile.Theming;
using Microsoft.Maui.Controls.Shapes;

namespace AutoElite.Mobile.Controls;

/// <summary>An icon in a rounded tile tinted by its tone, such as the kind of a notification.</summary>
public sealed class IconTile : Border
{
    /// <summary>Identifies the <see cref="Icon"/> property.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(IconTile), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Tone"/> property.</summary>
    public static readonly BindableProperty ToneProperty = BindableProperty.Create(
        nameof(Tone), typeof(StatusTone), typeof(IconTile), StatusTone.Neutral, propertyChanged: OnChanged);

    private readonly AppIcon _icon = new() { Size = 20 };

    /// <summary>Initializes a new instance of the <see cref="IconTile"/> class.</summary>
    public IconTile()
    {
        WidthRequest = 40;
        HeightRequest = 40;
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) };
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Start;
        Content = _icon;
        AppPalette.Follow(this, Paint);
    }

    /// <summary>Gets or sets the name of the icon.</summary>
    public string? Icon
    {
        get => (string?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Gets or sets what the icon tells, which gives its color and the tint of the tile.</summary>
    public StatusTone Tone
    {
        get => (StatusTone)GetValue(ToneProperty);
        set => SetValue(ToneProperty, value);
    }

    private static void OnChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tile = (IconTile)bindable;
        tile._icon.Name = tile.Icon;
        tile.Paint();
    }

    private void Paint()
    {
        BackgroundColor = ToneColors.BackgroundOf(Tone);
        _icon.SetDynamicResource(AppIcon.ColorProperty, ToneColors.ForegroundOf(Tone));
    }
}
