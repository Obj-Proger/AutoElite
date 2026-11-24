using Microsoft.Maui.Controls.Shapes;
using ShapePath = Microsoft.Maui.Controls.Shapes.Path;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// An icon of <see cref="AppIcons"/> by its name, as an outline in the color of the text unless its color is set, such
/// as to a color of the theme: <c>Color="{DynamicResource AccentText}"</c>. It is 24 wide by default.
/// </summary>
public sealed class AppIcon : ContentView
{
    /// <summary>Identifies the <see cref="Name"/> property.</summary>
    public static readonly BindableProperty NameProperty = BindableProperty.Create(
        nameof(Name), typeof(string), typeof(AppIcon), propertyChanged: OnNameChanged);

    /// <summary>Identifies the <see cref="Size"/> property.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(
        nameof(Size), typeof(double), typeof(AppIcon), 24d, propertyChanged: OnSizeChanged);

    /// <summary>Identifies the <see cref="Color"/> property.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(
        nameof(Color), typeof(Color), typeof(AppIcon), Colors.Gray, propertyChanged: OnColorChanged);

    // The grid the outlines are drawn on.
    private const double GridSize = 24;

    private readonly ShapePath _outline = new()
    {
        StrokeThickness = 1.8,
        StrokeLineCap = PenLineCap.Round,
        StrokeLineJoin = PenLineJoin.Round,
        Stroke = Colors.Gray
    };

    /// <summary>Initializes a new instance of the <see cref="AppIcon"/> class.</summary>
    public AppIcon()
    {
        HorizontalOptions = LayoutOptions.Center;
        VerticalOptions = LayoutOptions.Center;
        InputTransparent = true;
        // The outline keeps the whole grid, placed exactly, and is scaled to the size from its corner, so that no part of
        // it is cut off however small or large the icon.
        var canvas = new AbsoluteLayout();
        AbsoluteLayout.SetLayoutBounds(_outline, new Rect(0, 0, GridSize, GridSize));
        canvas.Add(_outline);
        Content = canvas;
        SetDynamicResource(ColorProperty, "Text");
        Resize();
    }

    /// <summary>Gets or sets the name of the icon, such as calendar.</summary>
    public string? Name
    {
        get => (string?)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    /// <summary>Gets or sets the width and the height of the icon.</summary>
    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Gets or sets the color of the outline.</summary>
    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    private static void OnNameChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((AppIcon)bindable)._outline.Data = AppIcons.GeometryOf(newValue as string);

    private static void OnSizeChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((AppIcon)bindable).Resize();

    private static void OnColorChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((AppIcon)bindable)._outline.Stroke = newValue as Color ?? Colors.Gray;

    // The outline scaled from its grid to the size.
    private void Resize()
    {
        WidthRequest = Size;
        HeightRequest = Size;
        _outline.AnchorX = 0;
        _outline.AnchorY = 0;
        _outline.Scale = Size / GridSize;
    }
}
