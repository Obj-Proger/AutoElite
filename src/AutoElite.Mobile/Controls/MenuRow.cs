using System.Windows.Input;
using AutoElite.Mobile.Theming;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// A row of a menu, such as one of the profile: an icon, a caption, its current value, such as the chosen language,
/// and an arrow; tapping it runs <see cref="Command"/>. Rows stand together on a card, with a line between them.
/// </summary>
public sealed class MenuRow : Grid
{
    /// <summary>Identifies the <see cref="Icon"/> property.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(MenuRow), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Title"/> property.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(MenuRow), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Value"/> property.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(string), typeof(MenuRow), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Tone"/> property.</summary>
    public static readonly BindableProperty ToneProperty = BindableProperty.Create(
        nameof(Tone), typeof(StatusTone), typeof(MenuRow), StatusTone.Neutral, propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Command"/> property.</summary>
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(MenuRow), propertyChanged: OnChanged);

    private readonly AppIcon _icon = new() { Size = 20 };
    private readonly Label _title = new() { FontSize = 15, VerticalOptions = LayoutOptions.Center };
    private readonly Label _value = new()
    {
        FontSize = 14,
        VerticalOptions = LayoutOptions.Center,
        LineBreakMode = LineBreakMode.TailTruncation,
        MaxLines = 1
    };

    private readonly TapGestureRecognizer _tap = new();

    /// <summary>Initializes a new instance of the <see cref="MenuRow"/> class.</summary>
    public MenuRow()
    {
        Padding = new Thickness(16, 0);
        MinimumHeightRequest = 52;
        ColumnSpacing = 14;
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _icon.SetDynamicResource(AppIcon.ColorProperty, "TextSecondary");
        _title.SetDynamicResource(Label.TextColorProperty, "Text");
        var chevron = new AppIcon { Name = "chevron-right", Size = 18 };
        chevron.SetDynamicResource(AppIcon.ColorProperty, "TextMuted");
        this.Add(_icon, 0, 0);
        this.Add(_title, 1, 0);
        this.Add(_value, 2, 0);
        this.Add(chevron, 3, 0);
        GestureRecognizers.Add(_tap);
        AppPalette.Follow(this, Paint);
    }

    /// <summary>Gets or sets the name of the icon.</summary>
    public string? Icon
    {
        get => (string?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Gets or sets the caption.</summary>
    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Gets or sets the current value, if any.</summary>
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Gets or sets what the value tells, which gives its color: plain, or a warning such as a debt.</summary>
    public StatusTone Tone
    {
        get => (StatusTone)GetValue(ToneProperty);
        set => SetValue(ToneProperty, value);
    }

    /// <summary>Gets or sets the command run when the row is tapped.</summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    private static void OnChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var row = (MenuRow)bindable;
        row._icon.Name = row.Icon;
        row._title.Text = row.Title;
        row._value.Text = row.Value;
        row._value.IsVisible = !string.IsNullOrWhiteSpace(row.Value);
        row._tap.Command = row.Command;
        SemanticProperties.SetDescription(
            row, string.Join(", ", new[] { row.Title, row.Value }.Where(part => !string.IsNullOrWhiteSpace(part))));
        row.Paint();
    }

    private void Paint() =>
        _value.SetDynamicResource(Label.TextColorProperty, Tone == StatusTone.Neutral ? "TextMuted" : ToneColors.ForegroundOf(Tone));
}
