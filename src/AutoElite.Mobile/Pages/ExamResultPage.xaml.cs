namespace AutoElite.Mobile.Pages;

/// <summary>A result of an exam to record or correct, opened over the exam.</summary>
public partial class ExamResultPage : ContentPage
{
    /// <summary>Initializes a new instance of the <see cref="ExamResultPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public ExamResultPage(ExamResultViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
