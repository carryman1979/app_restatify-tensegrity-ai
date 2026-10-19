using Restatify.Tensegrity.App.Core.Native;

namespace Restatify.Tensegrity.App.Core;

/// <summary>
/// ILocalGateway über den nativen Rust-Core (tg-ffi). Verfügbar, wenn die DLL die
/// Inferenz-Exporte enthält und llama-cli + GGUF-Modell gefunden wurden (Standardpfade
/// oder Umgebungsvariablen, siehe scripts/fetch-local-runtime.ps1 im Core-Repo).
/// </summary>
public sealed class NativeLocalGateway : ILocalGateway
{
    private readonly ITensegrityInference? _inference;
    private readonly bool _configured;

    private NativeLocalGateway(ITensegrityInference? inference, bool configured)
    {
        _inference = inference;
        _configured = configured;
    }

    public bool IsAvailable => _configured;

    public static NativeLocalGateway CreateDefault()
    {
        if (TensegrityNativeBinding.TryCreate(null, out var binding, out _)
            && binding.SupportsLocalInference
            && binding.ConfigureLocal())
        {
            return new NativeLocalGateway(binding, true);
        }

        return new NativeLocalGateway(null, false);
    }

    public Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (!_configured || _inference is null)
        {
            throw new InvalidOperationException("Lokale Laufzeit ist nicht konfiguriert.");
        }

        return Task.Run(
            () => _inference.GenerateLocal(prompt)
                ?? throw new InvalidOperationException("Lokale Generierung fehlgeschlagen."),
            cancellationToken);
    }
}
