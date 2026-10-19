namespace Restatify.Tensegrity.App.Core;

/// <summary>Lokale Antwortgenerierung auf dem Gerät (ADR-0012), ohne Netzwerk.</summary>
public interface ILocalGateway
{
    /// <summary>True, wenn die lokale Laufzeit (llama-cli + Modell) eingerichtet ist.</summary>
    bool IsAvailable { get; }

    /// <summary>Generiert eine Antwort lokal. Wirft InvalidOperationException, wenn nicht verfügbar.</summary>
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
}
