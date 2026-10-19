using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Restatify.Tensegrity.App.Core;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ICloudGateway _cloudGateway;
    private readonly TensegrityPolicyEngine _policy;
    private ConfidentialityLevel _selectedLevel = ConfidentialityLevel.Offen;
    private string _prompt = string.Empty;
    private string _statusText = "Bereit.";

    public MainViewModel()
        : this(new CloudGateway(), TensegrityPolicyEngine.CreateDefault())
    {
    }

    public MainViewModel(ICloudGateway cloudGateway)
        : this(cloudGateway, new TensegrityPolicyEngine(null))
    {
    }

    public MainViewModel(ICloudGateway cloudGateway, TensegrityPolicyEngine policy)
    {
        _cloudGateway = cloudGateway;
        _policy = policy;
        Feedback = new FeedbackViewModel(this);
        Messages.Add(ChatMessage.Assistant(
            "Hallo! Ich bin NORA. Wähle unten links den Privacy-Mode und stelle deine Frage."));
    }

    public FeedbackViewModel Feedback { get; } = null!;

    /// <summary>Chat-Verlauf für die Blasen-Darstellung (NORA links, Nutzer rechts).</summary>
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

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
                OnPropertyChanged(nameof(CanSend));
                OnPropertyChanged(nameof(PrivacyModeText));
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

    public bool CloudAllowed => _policy.IsCloudAllowed(SelectedLevel);

    public bool WebBlocked => _policy.IsBlockedInWeb(SelectedLevel);

    public bool CanSend => CloudAllowed && !string.IsNullOrWhiteSpace(Prompt);

    /// <summary>Aktive Policy-Engine: "Native" (Rust-Core via FFI) oder "Managed" (Fallback).</summary>
    public string PolicyBackend => _policy.Backend;

    public string PolicyBackendText => _policy.Backend == "Native"
        ? "Policy-Engine: Rust-Core (nativ)"
        : "Policy-Engine: Managed (Fallback)";

    /// <summary>Anzeigename der gewählten Stufe im Privacy-Mode-Popup (Mockup-Sprache).</summary>
    public string PrivacyModeText => SelectedLevel switch
    {
        ConfidentialityLevel.Offen => "Open",
        ConfidentialityLevel.Vertraulich => "Confidential",
        ConfidentialityLevel.StrengGeheim => "Strictly confidential",
        _ => "Nur lokal",
    };

    public async Task SendAsync(CancellationToken cancellationToken = default)
    {
        var prompt = Prompt;
        var request = CloudRequestBuilder.TryBuild(Guid.NewGuid().ToString("N"), SelectedLevel, prompt, null);
        if (request is null)
        {
            StatusText = "Blockiert: Diese Stufe darf nicht an die Cloud.";
            Messages.Add(ChatMessage.Assistant(
                "Diese Stufe ist nur lokal erlaubt – ich sende nichts an die Cloud."));
            return;
        }

        Messages.Add(ChatMessage.User(prompt));
        Prompt = string.Empty;
        StatusText = "Sende …";
        var result = await _cloudGateway.GenerateAsync(request, cancellationToken);
        StatusText = result.Allowed
            ? $"Antwort über Route '{result.Route}' erhalten."
            : "Die Cloud hat die Anfrage abgelehnt.";
        Messages.Add(ChatMessage.Assistant(
            result.Allowed
                ? $"Antwort über Route '{result.Route}' erhalten."
                : "Die Cloud hat die Anfrage abgelehnt."));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

