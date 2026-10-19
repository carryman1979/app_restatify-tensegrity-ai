namespace Restatify.Tensegrity.App.Core.Native;

/// <summary>
/// Abstraktion der nativen tg-ffi-Oberfläche (Rust-Core, C-ABI).
/// Ermöglicht Tests ohne natives Binary und den Managed-Fallback.
/// </summary>
public interface ITensegrityNative
{
    /// <summary>Gibt true zurück, wenn der Level Netzwerk-Transport erlaubt.</summary>
    bool AllowsTransport(byte levelIndex);

    /// <summary>Gibt true zurück, wenn der Level im Browser/Web-Kontext blockiert ist.</summary>
    bool BrowserBlocked(byte levelIndex);

    /// <summary>
    /// Liefert den Index der strengsten Stufe. Fehlercodes: -1 (Null-Pointer), -2 (unbekannter Wert).
    /// Leere Eingaben werden von der aufrufenden Schicht behandelt und nicht hierher delegiert.
    /// </summary>
    int TaintMax(byte[] levelIndices);

    /// <summary>
    /// Liefert die gefaltete Nutzlastgröße in Bytes. Fehlercodes: -1 (NUR_LOKAL), -2 (unbekannter Wert).
    /// </summary>
    long FoldPayloadBytes(ulong deltaBytes, byte levelIndex);
}
