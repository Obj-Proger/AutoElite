using Microsoft.Maui.Controls.Shapes;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// A figure as a card: what it counts in capitals, the figure in large type with its unit, and, when it is part of a
/// whole, a bar of the color of its tone, such as hours driven out of the hours of the course.
/// </summary>
public sealed class StatTile : Border
{
    /// <summary>Identifies the <see cref="Title"/> property.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(StatTile), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Value"/> property.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(string), typeof(StatTile), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Unit"/> property.</summary>
    public static readonly BindableProperty UnitProperty = BindableProperty.Create(
        nameof(Unit), typeof(string), typeof(StatTile), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Progress"/> property.</summary>
    public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
        nameof(Progress), typeof(double), typeof(StatTile), -1d, propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Tone"/> property.</summary>
    public static readonly BindableProperty ToneProperty = BindableProperty.Create(
        nameof(Tone), typeof(StatusTone), typeof(StatTile), StatusTone.Accent, propertyChanged: OnChanged);

    private readonly Label _title = new()
    {
        FontFamily = "TextMedium",
        FontSize = 11,
        CharacterSpacing = 1.5,
        TextTransform = TextTransform.Uppercase,
        MaxLines = 1
    };

    private readonly Label _value = new() { FontFamily = "DisplaySemiBold", FontSize = 28 };
    private readonly Label _unit = new() { FontSize = 14, VerticalOptions = LayoutOptions.End, Margin = new Thickness(0, 0, 0, 5) };
    private readonly ProgressBar _bar = new();

    /// <summary>Initializes a new instance of the <see cref="StatTile"/> class.</summary>
    public StatTile()
    {
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) };
        Padding = 16;
        SetDynamicResource(BackgroundColorProperty, "Surface");
        _title.SetDynamicResource(Label.TextColorProperty, "TextMuted");
        _unit.SetDynamicResource(Label.TextColorProperty, "TextMuted");

        var figure = new HorizontalStackLayout { Spacing = 6 };
        figure.Add(_value);
        figure.Add(_unit);

        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Add(_title);
        stack.Add(figure);
        stack.Add(_bar);
        Content = stack;
        Update();
    }

    /// <summary>Gets or sets what the figure counts.</summary>
    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Gets or sets the figure.</summary>
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Gets or sets the unit after the figure, such as "of 56 h", if any.</summary>
    public string? Unit
    {
        get => (string?)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    /// <summary>Gets or sets the part of the whole the figure is, from 0 to 1; a negative value shows no bar.</summary>
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>Gets or sets the tone of the bar.</summary>
    public StatusTone Tone
    {
        get => (StatusTone)GetValue(ToneProperty);
        set => SetValue(ToneProperty, value);
    }

    private static void OnChanged(BindableObject bindable, object oldValue, object newValue) => ((StatTile)bindable).Update();

    private void Update()
    {
        _title.Text = Title;
        _value.Text = Value;
        _unit.Text = Unit;
        _unit.IsVisible = !string.IsNullOrWhiteSpace(Unit);
        _bar.IsVisible = Progress >= 0;
        _bar.Progress = Math.Clamp(Progress, 0, 1);
        _bar.SetDynamicResource(ProgressBar.ProgressColorProperty, ToneColors.FillOf(Tone));
        SemanticProperties.SetDescription(
            this, string.Join(" ", new[] { Title, Value, Unit }.Where(part => !string.IsNullOrWhiteSpace(part))));
    }
}
