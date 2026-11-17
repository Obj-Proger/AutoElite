namespace AutoElite.Mobile.Pages;

/// <summary>A payment that a manager records towards a contract, opened over the card of the student.</summary>
public partial class PaymentPage : ContentPage
{
    /// <summary>Initializes a new instance of the <see cref="PaymentPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public PaymentPage(PaymentViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
