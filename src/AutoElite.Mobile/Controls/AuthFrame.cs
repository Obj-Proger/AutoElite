using AutoElite.Mobile.Localization;
using Microsoft.Maui.Controls.Shapes;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// The frame of the pages before signing in, the choice of the school and signing in: on a wide window the mark and
/// the name of the application with what it is on a panel at the left and the form in the middle of the right, as
/// sign-in pages of desktop applications are; on a phone the mark and the name above the form.
/// </summary>
[ContentProperty(nameof(Body))]
public sealed class AuthFrame : Grid
{
    /// <summary>Identifies the <see cref="Body"/> property.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(
        nameof(Body), typeof(View), typeof(AuthFrame), propertyChanged: OnBodyChanged);

    // The width from which the panel stands beside the form.
    private const double WideWidth = 900;

    private readonly ContentView _body = new();
    private readonly Border _panel;
    private readonly HorizontalStackLayout _compactLogo;

    /// <summary>Initializes a new instance of the <see cref="AuthFrame"/> class.</summary>
    public AuthFrame()
    {
        ColumnDefinitions.Add(new ColumnDefinition(new GridLength(0)));
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        var brand = new VerticalStackLayout { Spacing = 20, VerticalOptions = LayoutOptions.Center, MaximumWidthRequest = 440 };
        brand.Add(Mark(64, 30));
        var name = new Label { Text = "AutoElite", FontFamily = "DisplaySemiBold", FontSize = 40 };
        name.SetDynamicResource(Label.TextColorProperty, "Text");
        brand.Add(name);
        var tagline = new Label { Text = AppTexts.Get("Auth.Tagline"), FontSize = 18 };
        tagline.SetDynamicResource(Label.TextColorProperty, "TextSecondary");
        brand.Add(tagline);
        _panel = new Border { StrokeThickness = 0, Padding = new Thickness(56, 48), IsVisible = false, Content = brand };
        _panel.SetDynamicResource(BackgroundColorProperty, "Highlight");

        _compactLogo = new HorizontalStackLayout { Spacing = 12, Margin = new Thickness(0, 0, 0, 12) };
        _compactLogo.Add(Mark(44, 21));
        var compactName = new Label
        {
            Text = "AutoElite",
            FontFamily = "DisplaySemiBold",
            FontSize = 24,
            VerticalTextAlignment = TextAlignment.Center
        };
        compactName.SetDynamicResource(Label.TextColorProperty, "Text");
        _compactLogo.Add(compactName);

        var form = new VerticalStackLayout
        {
            Padding = new Thickness(24, 32),
            MaximumWidthRequest = 440,
            VerticalOptions = LayoutOptions.Center
        };
        form.Add(_compactLogo);
        form.Add(_body);

        this.Add(_panel, 0, 0);
        this.Add(new ScrollView { Content = form }, 1, 0);
        SizeChanged += (_, _) => Adapt(Width >= WideWidth);
    }

    /// <summary>Gets or sets the form.</summary>
    public View? Body
    {
        get => (View?)GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    // The mark of the application: its letter on a gold tile.
    private static Border Mark(double size, double fontSize)
    {
        var letter = new Label
        {
            Text = "A",
            FontFamily = "DisplaySemiBold",
            FontSize = fontSize,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        letter.SetDynamicResource(Label.TextColorProperty, "OnAccent");
        var mark = new Border
        {
            WidthRequest = size,
            HeightRequest = size,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(size * 0.28) },
            HorizontalOptions = LayoutOptions.Start,
            Content = letter
        };
        mark.SetDynamicResource(BackgroundColorProperty, "Accent");

        return mark;
    }

    private static void OnBodyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((AuthFrame)bindable)._body.Content = (View?)newValue;

    private void Adapt(bool isWide)
    {
        ColumnDefinitions[0].Width = isWide ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        _panel.IsVisible = isWide;
        _compactLogo.IsVisible = !isWide;
    }
}
