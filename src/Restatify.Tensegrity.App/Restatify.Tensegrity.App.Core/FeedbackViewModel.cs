using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Restatify.Tensegrity.App.Core;

public sealed class FeedbackViewModel : INotifyPropertyChanged
{
    private readonly MainViewModel _main;
    private readonly IFeedbackChannel? _channel;
    private string _comment = string.Empty;
    private string _citation = string.Empty;
    private string _statusText = string.Empty;
    private int _rating;

    public FeedbackViewModel(MainViewModel main)
        : this(main, null)
    {
    }

    public FeedbackViewModel(MainViewModel main, IFeedbackChannel? channel)
    {
        _main = main;
        _channel = channel;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Rating
    {
        get => _rating;
        set
        {
            if (_rating != value)
            {
                _rating = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSubmit));
            }
        }
    }

    public string Comment
    {
        get => _comment;
        set
        {
            if (_comment != value)
            {
                _comment = value;
                OnPropertyChanged();
            }
        }
    }

    public string Citation
    {
        get => _citation;
        set
        {
            if (_citation != value)
            {
                _citation = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSubmit));
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText != value)
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }
    }

    public bool CanSubmit => Rating != 0 && !string.IsNullOrWhiteSpace(Citation);

    public async Task<bool> SubmitAsync(CancellationToken cancellationToken = default)
    {
        var draft = new FeedbackDraft(
            Guid.NewGuid().ToString("N"),
            "app-local",
            "model-local",
            Rating,
            Comment,
            _main.SelectedLevel,
            new[] { new EvidenceDraft("user-input", Citation, DateTimeOffset.UtcNow) });

        var (accepted, reason) = FeedbackValidator.Validate(draft);
        if (!accepted)
        {
            StatusText = $"Feedback verworfen: {reason}";
            return false;
        }

        if (_channel is null)
        {
            StatusText = "Feedback angenommen (lokal).";
            return true;
        }

        var ack = await _channel.SubmitAsync(draft, cancellationToken);
        StatusText = ack.Accepted ? "Feedback angenommen." : $"Feedback abgelehnt: {ack.Reason}";
        return ack.Accepted;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
