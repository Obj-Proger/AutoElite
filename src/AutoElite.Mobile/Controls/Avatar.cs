using AutoElite.Mobile.Pages;
using Microsoft.Maui.Controls.Shapes;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// A person as a circle: their photo, or the initials of their name in its place; in gold for the user themselves and
/// grey for others.
/// </summary>
public sealed class Avatar : Border
{
    /// <summary>Identifies the <see cref="Name"/> property.</summary>
    public static readonly BindableProperty NameProperty = BindableProperty.Create(
        nameof(Name), typeof(string), typeof(Avatar), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Source"/> property.</summary>
    public static readonly BindableProperty SourceProperty = BindableProperty.Create(
        nameof(Source), typeof(ImageSource), typeof(Avatar), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Size"/> property.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(
        nameof(Size), typeof(double), typeof(Avatar), 40d, propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="IsAccent"/> property.</summary>
    public static readonly BindableProperty IsAccentProperty = BindableProperty.Create(
        nameof(IsAccent), typeof(bool), typeof(Avatar), false, propertyChanged: OnChanged);

    private readonly Label _initials = new()
    {
        FontFamily = "TextSemiBold",
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center
    };

    private readonly Image _photo = new() { Aspect = Aspect.AspectFill, IsVisible = false };

    /// <summary>Initializes a new instance of the <see cref="Avatar"/> class.</summary>
    public Avatar()
    {
        StrokeThickness = 0;
        StrokeShape = new Ellipse();
        Padding = 0;
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        Content = new Grid { _initials, _photo };
        Update();
    }

    /// <summary>Gets or sets the name of the person, whose initials stand in for a photo.</summary>
    public string? Name
    {
        get => (string?)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    /// <summary>Gets or sets the photo, if any.</summary>
    public ImageSource? Source
    {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>Gets or sets the width and the height of the circle.</summary>
    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether the circle is gold, as for the user themselves.</summary>
    public bool IsAccent
    {
        get => (bool)GetValue(IsAccentProperty);
        set => SetValue(IsAccentProperty, value);
    }

    private static void OnChanged(BindableObject bindable, object oldValue, object newValue) => ((Avatar)bindable).Update();

    private void Update()
    {
        WidthRequest = Size;
        HeightRequest = Size;
        _initials.FontSize = Math.Round(Size * 0.36);
        _initials.Text = PhoneCalls.InitialsOf(Name ?? string.Empty);
        _photo.Source = Source;
        _photo.IsVisible = Source is not null;
        _initials.IsVisible = Source is null;
        SetDynamicResource(BackgroundColorProperty, IsAccent ? "Accent" : "SurfaceRaised");
        _initials.SetDynamicResource(Label.TextColorProperty, IsAccent ? "OnAccent" : "Text");
        SemanticProperties.SetDescription(this, Name);
    }
}
