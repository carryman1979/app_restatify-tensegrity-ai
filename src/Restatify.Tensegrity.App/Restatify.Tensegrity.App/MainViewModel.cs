using System.ComponentModel;
using System.Runtime.CompilerServices;
using Restatify.Tensegrity.App.Core;

namespace Restatify.Tensegrity.App;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ICloudGateway _cloudGateway;
    private ConfidentialityLevel _selectedLevel = ConfidentialityLevel.Offen;
    private string _prompt = string.Empty;
    private string _statusText = "Bereit.";

    public MainViewModel()
        : this(new CloudGateway())
    {
    }

    public MainViewModel(ICloudGateway cloudGateway)
    {
        _cloudGateway = cloudGateway;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<ConfidentialityLevel> AvailableLevels { get; } = new[]
    {
        ConfidentialityLevel.Offen,
        ConfidentialityLevel.Vertraulich,
        ConfidentialityLevel.StrengGeheim,
        ConfidentialityLevel.NurLokal
    };

    public ConfidentialityLevel SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            if (_selectedLevel != value)
            {
                _selectedLevel = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CloudAllowed));
                OnPropertyChanged(nameof(WebBlocked));
                OnPropertyChanged(nameof(PolicyText));
                OnPropertyChanged(nameof(CanSend));
            }
        }
    }

    public string Prompt
    {
        get => _prompt;
        set
        {
            if (_prompt != value)
            {
                _prompt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSend));
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

    public bool CloudAllowed => ConfidentialityPolicy.IsCloudAllowed(SelectedLevel);

    public bool WebBlocked => ConfidentialityPolicy.IsBlockedInWeb(SelectedLevel);

    public bool CanSend => CloudAllowed && !string.IsNullOrWhiteSpace(Prompt);

    public string PolicyText => WebBlocked
        ? "Diese Stufe ist im Web/Cloud-Pfad gesperrt."
        : "Diese Stufe ist im Cloud-Pfad erlaubt.";

    public string TaintText => "Taint-Regel: Die strengste gewählte Stufe gewinnt und blockiert unsichere Cloud-Pfade.";

    public async Task SendAsync(CancellationToken cancellationToken = default)
    {
        var request = CloudRequestBuilder.TryBuild(Guid.NewGuid().ToString("N"), SelectedLevel, Prompt, null);
        if (request is null)
        {
            StatusText = "Blockiert: Diese Stufe darf nicht an die Cloud.";
            return;
        }

        StatusText = "Sende …";
        var result = await _cloudGateway.GenerateAsync(request, cancellationToken);
        StatusText = result.Allowed
            ? $"Antwort über Route '{result.Route}' erhalten."
            : "Die Cloud hat die Anfrage abgelehnt.";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

