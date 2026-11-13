namespace AutoElite.Mobile.Pages;

/// <summary>Completing a driving lesson, opened from the home page of a driving instructor.</summary>
public partial class CompletionPage : ContentPage
{
    /// <summary>Initializes a new instance of the <see cref="CompletionPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public CompletionPage(CompletionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
