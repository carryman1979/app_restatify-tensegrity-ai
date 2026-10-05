namespace Restatify.Tensegrity.App;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }

    private async void OnSendClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (DataContext is Core.MainViewModel viewModel)
        {
            await viewModel.SendAsync();
        }
    }

    private void OnThumbsUpClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (DataContext is Core.MainViewModel viewModel)
        {
            viewModel.Feedback.Rating = 1;
        }
    }

    private void OnThumbsDownClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (DataContext is Core.MainViewModel viewModel)
        {
            viewModel.Feedback.Rating = -1;
        }
    }

    private void OnFeedbackSubmitClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (DataContext is Core.MainViewModel viewModel)
        {
            viewModel.Feedback.Submit();
        }
    }
}
