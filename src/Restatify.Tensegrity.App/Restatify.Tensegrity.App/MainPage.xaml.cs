using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Restatify.Tensegrity.App;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        // Flyouts leben außerhalb des visuellen Baums und erben den DataContext nicht –
        // darum explizit setzen.
        PrivacyPanel.DataContext = DataContext;
        FeedbackPanel.DataContext = DataContext;
    }

    private async void OnSendClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is Core.MainViewModel viewModel)
        {
            await viewModel.SendAsync();
        }
    }

    private void OnPrivacyModeClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button button
            && System.Enum.TryParse<Core.ConfidentialityLevel>(button.Tag as string, out var level)
            && DataContext is Core.MainViewModel viewModel)
        {
            viewModel.SelectedLevel = level;
        }

        PrivacyFlyout.Hide();
    }

    private void OnThumbsUpClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is Core.MainViewModel viewModel)
        {
            viewModel.Feedback.Rating = 1;
        }
    }

    private void OnThumbsDownClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is Core.MainViewModel viewModel)
        {
            viewModel.Feedback.Rating = -1;
        }
    }

    private async void OnFeedbackSubmitClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is Core.MainViewModel viewModel)
        {
            await viewModel.Feedback.SubmitAsync();
        }
    }
}

/// <summary>Wählt die Blasen-Vorlage: Nutzer rechts (orange), NORA links (hell).</summary>
public sealed class ChatMessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate? UserTemplate { get; set; }

    public DataTemplate? AssistantTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return item is Core.ChatMessage { IsUser: true } ? UserTemplate! : AssistantTemplate!;
    }
}

/// <summary>Zeigt den Haken an der aktuell gewählten Vertraulichkeitsstufe im Privacy-Popup.</summary>
public sealed class LevelCheckConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is Core.ConfidentialityLevel level
            && parameter is string name
            && System.Enum.TryParse<Core.ConfidentialityLevel>(name, out var parsed)
            && level == parsed
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
