namespace AutoElite.Mobile.Pages;

/// <summary>The address of the school, which the user gives on the first start or when they change the school.</summary>
public partial class SchoolPage : ContentPage
{
    /// <summary>Initializes a new instance of the <see cref="SchoolPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public SchoolPage(SchoolViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
