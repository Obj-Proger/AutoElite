using AutoElite.Mobile.Theming;
using Microsoft.Maui.Controls.Shapes;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// A short status as a rounded label in the color of its tone, such as Paid or Cancelled, so that a state reads at a
/// glance instead of as one more line of text. It hides without text.
/// </summary>
public sealed class StatusPill : Border
{
    /// <summary>Identifies the <see cref="Text"/> property.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(StatusPill), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Tone"/> property.</summary>
    public static readonly BindableProperty ToneProperty = BindableProperty.Create(
        nameof(Tone), typeof(StatusTone), typeof(StatusPill), StatusTone.Neutral, propertyChanged: OnChanged);

    private readonly Label _label = new() { FontFamily = "TextSemiBold", FontSize = 12, MaxLines = 1 };

    /// <summary>Initializes a new instance of the <see cref="StatusPill"/> class.</summary>
    public StatusPill()
    {
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) };
        Padding = new Thickness(10, 4);
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        IsVisible = false;
        Content = _label;
        AppPalette.Follow(this, Paint);
    }

    /// <summary>Gets or sets the status.</summary>
    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Gets or sets what the status tells, which gives its color.</summary>
    public StatusTone Tone
    {
        get => (StatusTone)GetValue(ToneProperty);
        set => SetValue(ToneProperty, value);
    }

    private static void OnChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var pill = (StatusPill)bindable;
        pill._label.Text = pill.Text;
        pill.IsVisible = !string.IsNullOrWhiteSpace(pill.Text);
        pill.Paint();
    }

    private void Paint()
    {
        BackgroundColor = ToneColors.BackgroundOf(Tone);
        _label.SetDynamicResource(Label.TextColorProperty, ToneColors.ForegroundOf(Tone));
    }
}
