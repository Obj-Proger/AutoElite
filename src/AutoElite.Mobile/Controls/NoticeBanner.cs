using AutoElite.Mobile.Theming;
using Microsoft.Maui.Controls.Shapes;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// A notice at the top of a page, the same on every page: an icon of its tone and a sentence on a tint of its color,
/// such as that the page shows what was saved on the device, or why an action failed. It hides without text.
/// </summary>
public sealed class NoticeBanner : Border
{
    /// <summary>Identifies the <see cref="Text"/> property.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(NoticeBanner), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Tone"/> property.</summary>
    public static readonly BindableProperty ToneProperty = BindableProperty.Create(
        nameof(Tone), typeof(StatusTone), typeof(NoticeBanner), StatusTone.Neutral, propertyChanged: OnChanged);

    private readonly AppIcon _icon = new() { Size = 20, VerticalOptions = LayoutOptions.Start };
    private readonly Label _text = new() { FontSize = 14, VerticalOptions = LayoutOptions.Center };

    /// <summary>Initializes a new instance of the <see cref="NoticeBanner"/> class.</summary>
    public NoticeBanner()
    {
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) };
        Padding = new Thickness(14, 12);
        IsVisible = false;
        _text.SetDynamicResource(Label.TextColorProperty, "Text");

        var row = new Grid
        {
            ColumnSpacing = 12,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }
        };
        row.Add(_icon, 0, 0);
        row.Add(_text, 1, 0);
        Content = row;
        AppPalette.Follow(this, Paint);
    }

    /// <summary>Gets or sets the notice.</summary>
    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Gets or sets what the notice tells, which gives its icon and color.</summary>
    public StatusTone Tone
    {
        get => (StatusTone)GetValue(ToneProperty);
        set => SetValue(ToneProperty, value);
    }

    private static string IconOf(StatusTone tone) => tone switch
    {
        StatusTone.Success => "check",
        StatusTone.Warning => "triangle-alert",
        StatusTone.Danger => "circle-x",
        _ => "info"
    };

    private static void OnChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var banner = (NoticeBanner)bindable;
        banner._text.Text = banner.Text;
        banner._icon.Name = IconOf(banner.Tone);
        banner.IsVisible = !string.IsNullOrWhiteSpace(banner.Text);
        banner.Paint();
    }

    private void Paint()
    {
        BackgroundColor = ToneColors.BackgroundOf(Tone);
        _icon.SetDynamicResource(AppIcon.ColorProperty, ToneColors.ForegroundOf(Tone));
    }
}
