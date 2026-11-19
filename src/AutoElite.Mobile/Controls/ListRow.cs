using System.Windows.Input;
using AutoElite.Mobile.Theming;
using Microsoft.Maui.Controls.Shapes;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// One thing of a list as a card: an icon in a tile of the color of its tone, a title over a line of details, a short
/// value at the end, such as a time or a count, and an arrow when tapping it opens something. Tapping it runs
/// <see cref="Command"/>.
/// </summary>
public sealed class ListRow : Border
{
    /// <summary>Identifies the <see cref="Icon"/> property.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(ListRow), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Tone"/> property.</summary>
    public static readonly BindableProperty ToneProperty = BindableProperty.Create(
        nameof(Tone), typeof(StatusTone), typeof(ListRow), StatusTone.Neutral, propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Title"/> property.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(ListRow), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Subtitle"/> property.</summary>
    public static readonly BindableProperty SubtitleProperty = BindableProperty.Create(
        nameof(Subtitle), typeof(string), typeof(ListRow), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Trailing"/> property.</summary>
    public static readonly BindableProperty TrailingProperty = BindableProperty.Create(
        nameof(Trailing), typeof(string), typeof(ListRow), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="ShowsChevron"/> property.</summary>
    public static readonly BindableProperty ShowsChevronProperty = BindableProperty.Create(
        nameof(ShowsChevron), typeof(bool), typeof(ListRow), false, propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Command"/> property.</summary>
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(ListRow), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="CommandParameter"/> property.</summary>
    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(ListRow), propertyChanged: OnChanged);

    private readonly AppIcon _icon = new() { Size = 22 };
    private readonly Border _tile;
    private readonly Label _title = new() { FontFamily = "TextSemiBold", FontSize = 15, LineBreakMode = LineBreakMode.TailTruncation };
    private readonly Label _subtitle = new() { FontSize = 13, LineBreakMode = LineBreakMode.TailTruncation };
    private readonly Label _trailing = new() { FontFamily = "DisplaySemiBold", FontSize = 15, VerticalOptions = LayoutOptions.Center };
    private readonly AppIcon _chevron = new() { Name = "chevron-right", Size = 18 };
    private readonly TapGestureRecognizer _tap = new();

    /// <summary>Initializes a new instance of the <see cref="ListRow"/> class.</summary>
    public ListRow()
    {
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) };
        Padding = new Thickness(16, 14);
        SetDynamicResource(BackgroundColorProperty, "Surface");
        _subtitle.SetDynamicResource(Label.TextColorProperty, "TextMuted");
        _chevron.SetDynamicResource(AppIcon.ColorProperty, "TextMuted");

        _tile = new Border
        {
            WidthRequest = 44,
            HeightRequest = 44,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
            VerticalOptions = LayoutOptions.Center,
            Content = _icon
        };

        var texts = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        texts.Add(_title);
        texts.Add(_subtitle);

        var row = new Grid
        {
            ColumnSpacing = 14,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        row.Add(_tile, 0, 0);
        row.Add(texts, 1, 0);
        row.Add(_trailing, 2, 0);
        row.Add(_chevron, 3, 0);
        Content = row;

        GestureRecognizers.Add(_tap);
        AppPalette.Follow(this, Paint);
        Update();
    }

    /// <summary>Gets or sets the name of the icon of the tile; without one there is no tile.</summary>
    public string? Icon
    {
        get => (string?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Gets or sets what the row tells, which gives the color of its tile.</summary>
    public StatusTone Tone
    {
        get => (StatusTone)GetValue(ToneProperty);
        set => SetValue(ToneProperty, value);
    }

    /// <summary>Gets or sets the title.</summary>
    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Gets or sets the line of details under the title, if any.</summary>
    public string? Subtitle
    {
        get => (string?)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    /// <summary>Gets or sets the short value at the end, such as a time or a count, if any.</summary>
    public string? Trailing
    {
        get => (string?)GetValue(TrailingProperty);
        set => SetValue(TrailingProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether an arrow tells that tapping the row opens something.</summary>
    public bool ShowsChevron
    {
        get => (bool)GetValue(ShowsChevronProperty);
        set => SetValue(ShowsChevronProperty, value);
    }

    /// <summary>Gets or sets the command run when the row is tapped.</summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>Gets or sets the parameter of the command.</summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    private static void OnChanged(BindableObject bindable, object oldValue, object newValue) => ((ListRow)bindable).Update();

    private void Update()
    {
        _icon.Name = Icon;
        _tile.IsVisible = !string.IsNullOrEmpty(Icon);
        _title.Text = Title;
        _subtitle.Text = Subtitle;
        _subtitle.IsVisible = !string.IsNullOrWhiteSpace(Subtitle);
        _trailing.Text = Trailing;
        _trailing.IsVisible = !string.IsNullOrWhiteSpace(Trailing);
        _chevron.IsVisible = ShowsChevron;
        _tap.Command = Command;
        _tap.CommandParameter = CommandParameter;
        SemanticProperties.SetDescription(
            this, string.Join(", ", new[] { Title, Subtitle, Trailing }.Where(part => !string.IsNullOrWhiteSpace(part))));
        Paint();
    }

    private void Paint()
    {
        _tile.BackgroundColor = ToneColors.BackgroundOf(Tone);
        _icon.SetDynamicResource(AppIcon.ColorProperty, ToneColors.ForegroundOf(Tone));
    }
}
