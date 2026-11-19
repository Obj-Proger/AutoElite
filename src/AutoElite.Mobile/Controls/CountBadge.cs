using System.Globalization;
using Microsoft.Maui.Controls.Shapes;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// A count of new things in gold, such as the unread notifications on the bell or the messages of a chat: up to 99,
/// then 99+, and hidden at none.
/// </summary>
public sealed class CountBadge : Border
{
    /// <summary>Identifies the <see cref="Count"/> property.</summary>
    public static readonly BindableProperty CountProperty = BindableProperty.Create(
        nameof(Count), typeof(int), typeof(CountBadge), 0, propertyChanged: OnCountChanged);

    // The largest count written out in full.
    private const int Largest = 99;

    private readonly Label _label = new()
    {
        FontFamily = "TextSemiBold",
        FontSize = 11,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center
    };

    /// <summary>Initializes a new instance of the <see cref="CountBadge"/> class.</summary>
    public CountBadge()
    {
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) };
        HeightRequest = 20;
        MinimumWidthRequest = 20;
        Padding = new Thickness(5, 0);
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        IsVisible = false;
        InputTransparent = true;
        SetDynamicResource(BackgroundColorProperty, "Accent");
        _label.SetDynamicResource(Label.TextColorProperty, "OnAccent");
        Content = _label;
    }

    /// <summary>Gets or sets the count.</summary>
    public int Count
    {
        get => (int)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    /// <summary>Writes a count as a badge shows it: in full up to 99, then 99+.</summary>
    /// <param name="count">The count.</param>
    /// <returns>The text.</returns>
    public static string TextOf(int count) =>
        count > Largest ? string.Create(CultureInfo.CurrentCulture, $"{Largest}+") : count.ToString(CultureInfo.CurrentCulture);

    private static void OnCountChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var badge = (CountBadge)bindable;
        badge.IsVisible = badge.Count > 0;
        badge._label.Text = TextOf(badge.Count);
    }
}
