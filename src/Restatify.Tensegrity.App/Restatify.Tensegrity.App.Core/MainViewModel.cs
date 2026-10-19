using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Restatify.Tensegrity.App.Core;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ICloudGateway _cloudGateway;
    private readonly TensegrityPolicyEngine _policy;
    private readonly ILocalGateway? _localGateway;
    private ConfidentialityLevel _selectedLevel = ConfidentialityLevel.Offen;
    private string _prompt = string.Empty;
    private string _statusText = "Bereit.";

    public MainViewModel()
        : this(new CloudGateway(), TensegrityPolicyEngine.CreateDefault(), NativeLocalGateway.CreateDefault())
    {
    }

    public MainViewModel(ICloudGateway cloudGateway)
        : this(cloudGateway, new TensegrityPolicyEngine(null), null)
    {
    }

    public MainViewModel(ICloudGateway cloudGateway, TensegrityPolicyEngine policy, ILocalGateway? localGateway = null)
    {
        _cloudGateway = cloudGateway;
        _policy = policy;
        _localGateway = localGateway;
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
            // Stufe darf nicht zur Cloud → lokaler Pfad (ADR-0012).
            await AnswerLocallyAsync(prompt, cancellationToken);
            return;
        }

        Messages.Add(ChatMessage.User(prompt));
        Prompt = string.Empty;
        StatusText = "Sende …";

        GenerateResult result;
        try
        {
            result = await _cloudGateway.GenerateAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Cloud nicht erreichbar → lokaler Fallback statt Fehlermeldung (ADR-0012).
            await AnswerLocallyAsync(prompt, cancellationToken);
            return;
        }

        StatusText = result.Allowed
            ? $"Antwort über Route '{result.Route}' erhalten."
            : "Die Cloud hat die Anfrage abgelehnt.";
        Messages.Add(ChatMessage.Assistant(
            result.Allowed
                ? $"Antwort über Route '{result.Route}' erhalten."
                : "Die Cloud hat die Anfrage abgelehnt."));
    }

    private async Task AnswerLocallyAsync(string prompt, CancellationToken cancellationToken)
    {
        if (_localGateway is { IsAvailable: true })
        {
            var last = Messages.LastOrDefault();
            if (last is null || !last.IsUser || last.Text != prompt)
            {
                Messages.Add(ChatMessage.User(prompt));
            }

            Prompt = string.Empty;
            StatusText = "Beantworte lokal …";
            var localAnswer = await _localGateway.GenerateAsync(prompt, cancellationToken);
            StatusText = "Lokal beantwortet (diese Anfrage hat das Gerät nicht verlassen).";
            Messages.Add(ChatMessage.Assistant($"Lokale Antwort:\n{localAnswer}"));
            return;
        }

        StatusText = "Blockiert: Diese Stufe bleibt auf dem Gerät, aber die lokale Laufzeit ist nicht eingerichtet.";
        Messages.Add(ChatMessage.Assistant(
            "Diese Stufe darf nicht an die Cloud – und die lokale Laufzeit fehlt noch. " +
            "Richte sie mit scripts/fetch-local-runtime.ps1 (Core-Repo) ein."));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

