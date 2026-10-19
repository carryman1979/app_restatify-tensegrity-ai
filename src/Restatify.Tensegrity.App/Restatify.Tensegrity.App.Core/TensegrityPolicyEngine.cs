using Restatify.Tensegrity.App.Core.Native;

namespace Restatify.Tensegrity.App.Core;

/// <summary>
/// Policy-Engine, die Entscheidungen bevorzugt über den nativen Rust-Core (tg-ffi)
/// trifft und bei fehlender Bibliothek auf die Managed-Implementierung zurückfällt
/// (<see cref="ConfidentialityPolicy"/>). Beide Wege müssen dieselben Ergebnisse
/// liefern – abgesichert über den Paritäts-Test.
/// </summary>
public sealed class TensegrityPolicyEngine
{
    private readonly ITensegrityNative? _native;

    public TensegrityPolicyEngine(ITensegrityNative? native)
    {
        _native = native;
    }

    /// <summary>"Native", wenn der Rust-Core geladen ist, sonst "Managed".</summary>
    public string Backend => _native is not null ? "Native" : "Managed";

    /// <summary>
    /// Transport-Gate: darf die Stufe das Gerät überhaupt verlassen?
    /// NUR_LOKAL blockt immer (Null-Byte-Transport). Delegiert an den nativen Kern,
    /// wenn verfügbar (tg_policy_allows_transport).
    /// </summary>
    public bool AllowsTransport(ConfidentialityLevel level)
    {
        if (_native is not null)
        {
            return _native.AllowsTransport(ToIndex(level));
        }

        return level is not ConfidentialityLevel.NurLokal;
    }

    /// <summary>
    /// Cloud-Zulassung – strenger als das Transport-Gate: nur Offen/Vertraulich werden
    /// zur Cloud-Inferenz geroutet (spiegelt den Cloud-PolicyGuard). STRENG_GEHEIM darf
    /// gefaltet transportiert, aber nicht cloud-verarbeitet werden. Diese Entscheidung
    /// liegt oberhalb der FFI-Oberfläche und bleibt bewusst managed.
    /// </summary>
    public bool IsCloudAllowed(ConfidentialityLevel level)
    {
        return ConfidentialityPolicy.IsCloudAllowed(level);
    }

    public bool IsLocalOnly(ConfidentialityLevel level)
    {
        return level is ConfidentialityLevel.NurLokal;
    }

    public bool IsBlockedInWeb(ConfidentialityLevel level)
    {
        if (_native is not null)
        {
            return _native.BrowserBlocked(ToIndex(level));
        }

        return ConfidentialityPolicy.IsBlockedInWeb(level);
    }

    public ConfidentialityLevel TaintMax(IEnumerable<ConfidentialityLevel> levels)
    {
        var indices = (levels ?? Enumerable.Empty<ConfidentialityLevel>())
            .Select(ToIndex)
            .ToArray();

        if (indices.Length == 0)
        {
            return ConfidentialityLevel.Offen;
        }

        if (_native is not null)
        {
            var result = _native.TaintMax(indices);
            return result >= 0 ? FromIndex(result) : ConfidentialityLevel.Offen;
        }

        return ConfidentialityPolicy.TaintMax(levels);
    }

    /// <summary>
    /// Gefaltete Nutzlastgröße in Bytes. Negative Werte signalisieren „kein Transport"
    /// (-1 = NUR_LOKAL, -2 = unbekannter Wert) und werden vom nativen Kern durchgereicht.
    /// </summary>
    public long FoldPayloadBytes(ulong deltaBytes, ConfidentialityLevel level)
    {
        if (_native is not null)
        {
            return _native.FoldPayloadBytes(deltaBytes, ToIndex(level));
        }

        return level switch
        {
            ConfidentialityLevel.Offen => (long)deltaBytes,
            ConfidentialityLevel.Vertraulich => (long)(deltaBytes / 2),
            ConfidentialityLevel.StrengGeheim => (long)(deltaBytes / 4),
            _ => -1,
        };
    }

    private static byte ToIndex(ConfidentialityLevel level) => (byte)level;

    private static ConfidentialityLevel FromIndex(int index)
    {
        return index is >= 0 and <= 3 ? (ConfidentialityLevel)index : ConfidentialityLevel.Offen;
    }
}
