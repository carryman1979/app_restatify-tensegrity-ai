namespace Restatify.Tensegrity.App.Core.Native;

/// <summary>
/// Lokale Inferenz über tg-ffi (ADR-0012): Konfiguration der lokalen Laufzeit
/// (llama-cli + GGUF-Modell) und Generierung ohne Netzwerk.
/// </summary>
public interface ITensegrityInference
{
    /// <summary>Konfiguriert die Laufzeit aus den Standardpfaden. False, wenn Binary/Modell fehlen.</summary>
    bool ConfigureLocal();

    /// <summary>Generiert eine Antwort lokal. Null bei Fehler (negativer FFI-Rückgabecode).</summary>
    string? GenerateLocal(string prompt, int maxOutputBytes = 4096);
}
