namespace AutoElite.Mobile;

/// <summary>The first page, until the pages of the application are designed: it shows the address of the API.</summary>
public partial class MainPage : ContentPage
{
    /// <summary>Initializes a new instance of the <see cref="MainPage"/> class.</summary>
    public MainPage()
    {
        InitializeComponent();
        AddressLabel.Text = ApiAddress.Current.ToString();
    }
}
