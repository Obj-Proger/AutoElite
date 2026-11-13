namespace AutoElite.Mobile.Localization;

/// <summary>Gives a page the text of a key in the language of the device: <c>Text="{l:Text SignIn.Title}"</c>.</summary>
[ContentProperty(nameof(Key))]
public sealed class TextExtension : IMarkupExtension<string>
{
    /// <summary>Gets or sets the key of the text.</summary>
    public string Key { get; set; } = string.Empty;

    /// <inheritdoc />
    public string ProvideValue(IServiceProvider serviceProvider) => AppTexts.Get(Key);

    /// <inheritdoc />
    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
