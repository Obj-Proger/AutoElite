using System.Windows.Input;
using AutoElite.Mobile.Localization;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// The top of the card of a person, such as a student: their avatar, name and contacts, then writing to them and, when
/// they have a phone, calling them.
/// </summary>
public sealed class PersonHeader : VerticalStackLayout
{
    /// <summary>Identifies the <see cref="Name"/> property.</summary>
    public static readonly BindableProperty NameProperty = BindableProperty.Create(
        nameof(Name), typeof(string), typeof(PersonHeader), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="Details"/> property.</summary>
    public static readonly BindableProperty DetailsProperty = BindableProperty.Create(
        nameof(Details), typeof(string), typeof(PersonHeader), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="WriteCommand"/> property.</summary>
    public static readonly BindableProperty WriteCommandProperty = BindableProperty.Create(
        nameof(WriteCommand), typeof(ICommand), typeof(PersonHeader), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="CallCommand"/> property.</summary>
    public static readonly BindableProperty CallCommandProperty = BindableProperty.Create(
        nameof(CallCommand), typeof(ICommand), typeof(PersonHeader), propertyChanged: OnChanged);

    /// <summary>Identifies the <see cref="CanCall"/> property.</summary>
    public static readonly BindableProperty CanCallProperty = BindableProperty.Create(
        nameof(CanCall), typeof(bool), typeof(PersonHeader), false, propertyChanged: OnChanged);

    private readonly Avatar _avatar = new() { Size = 64, IsAccent = true };
    private readonly Label _name = new() { FontFamily = "DisplaySemiBold", FontSize = 20 };
    private readonly Label _details = new() { FontSize = 14 };
    private readonly Button _write = new() { Text = AppTexts.Get("Student.Write") };
    private readonly Button _call = new() { Text = AppTexts.Get("Student.Call") };

    /// <summary>Initializes a new instance of the <see cref="PersonHeader"/> class.</summary>
    public PersonHeader()
    {
        Spacing = 16;
        _details.SetDynamicResource(Label.TextColorProperty, "TextSecondary");
        _call.SetDynamicResource(StyleProperty, "SecondaryButton");

        var texts = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
        texts.Add(_name);
        texts.Add(_details);
        var top = new Grid
        {
            ColumnSpacing = 16,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }
        };
        top.Add(_avatar, 0, 0);
        top.Add(texts, 1, 0);

        var buttons = new Grid
        {
            ColumnSpacing = 10,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }
        };
        buttons.Add(_write, 0, 0);
        buttons.Add(_call, 1, 0);
        Add(top);
        Add(buttons);
    }

    /// <summary>Gets or sets the full name of the person.</summary>
    public string? Name
    {
        get => (string?)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    /// <summary>Gets or sets how to reach the person, such as their phone and email address.</summary>
    public string? Details
    {
        get => (string?)GetValue(DetailsProperty);
        set => SetValue(DetailsProperty, value);
    }

    /// <summary>Gets or sets the command that opens a chat with the person.</summary>
    public ICommand? WriteCommand
    {
        get => (ICommand?)GetValue(WriteCommandProperty);
        set => SetValue(WriteCommandProperty, value);
    }

    /// <summary>Gets or sets the command that calls the person.</summary>
    public ICommand? CallCommand
    {
        get => (ICommand?)GetValue(CallCommandProperty);
        set => SetValue(CallCommandProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether the person has a phone to call.</summary>
    public bool CanCall
    {
        get => (bool)GetValue(CanCallProperty);
        set => SetValue(CanCallProperty, value);
    }

    private static void OnChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var header = (PersonHeader)bindable;
        header._avatar.Name = header.Name;
        header._name.Text = header.Name;
        header._details.Text = header.Details;
        header._details.IsVisible = !string.IsNullOrWhiteSpace(header.Details);
        header._write.Command = header.WriteCommand;
        header._call.Command = header.CallCommand;
        header._call.IsVisible = header.CanCall;
        Grid.SetColumnSpan(header._write, header.CanCall ? 1 : 2);
    }
}
