using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// What a list shows when it has nothing: an icon in a circle, a sentence on why and, when there is something to do
/// about it, a button, such as to book a lesson.
/// </summary>
public sealed class EmptyState : VerticalStackLayout
{
    /// <summary>Identifies the <see cref="Icon"/> property.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(EmptyState), "inbox", propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Text"/> property.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(EmptyState), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="ActionText"/> property.</summary>
    public static readonly BindableProperty ActionTextProperty = BindableProperty.Create(
        nameof(ActionText), typeof(string), typeof(EmptyState), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Command"/> property.</summary>
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(EmptyState), propertyChanged: OnChanged);

    private readonly AppIcon _icon = new() { Size = 26 };
    private readonly Label _text = new() { FontSize = 15, HorizontalTextAlignment = TextAlignment.Center };
    private readonly Button _action = new() { HorizontalOptions = LayoutOptions.Center };

    /// <summary>Initializes a new instance of the <see cref="EmptyState"/> class.</summary>
    public EmptyState()
    {
        Spacing = 12;
        Padding = new Thickness(24, 32);
        HorizontalOptions = LayoutOptions.Fill;
        _icon.SetDynamicResource(AppIcon.ColorProperty, "TextMuted");
        _text.SetDynamicResource(Label.TextColorProperty, "TextSecondary");
        _action.SetDynamicResource(StyleProperty, "SecondaryButton");

        var circle = new Border
        {
            WidthRequest = 56,
            HeightRequest = 56,
            StrokeThickness = 0,
            StrokeShape = new Ellipse(),
            HorizontalOptions = LayoutOptions.Center,
            Content = _icon
        };
        circle.SetDynamicResource(BackgroundColorProperty, "SurfaceRaised");

        Add(circle);
        Add(_text);
        Add(_action);
        Update();
    }

    /// <summary>Gets or sets the name of the icon.</summary>
    public string? Icon
    {
        get => (string?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Gets or sets why there is nothing.</summary>
    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Gets or sets the caption of the button, if there is something to do.</summary>
    public string? ActionText
    {
        get => (string?)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    /// <summary>Gets or sets the command of the button.</summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    private static void OnChanged(BindableObject bindable, object oldValue, object newValue) => ((EmptyState)bindable).Update();

    private void Update()
    {
        _icon.Name = Icon;
        _text.Text = Text;
        _action.Text = ActionText;
        _action.Command = Command;
        _action.IsVisible = Command is not null && !string.IsNullOrWhiteSpace(ActionText);
    }
}
