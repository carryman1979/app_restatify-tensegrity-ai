using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Restatify.Tensegrity.App.Core.Native;

/// <summary>
/// Lädt tg_ffi.dll (Rust-Core, C-ABI) und stellt die exportierten Funktionen als
/// Delegates bereit. Entscheidung aus Spike S3: kein Bindings-Generator (uniffi/csbindgen),
/// weil die Oberfläche ausschließlich skalare Werte überträgt (ADR-0010).
///
/// Auflösungspfade (in dieser Reihenfolge):
/// 1. expliziter Pfad (Parameter oder Umgebungsvariable TENSEGRITY_NATIVE_PATH)
/// 2. tg_ffi.dll neben der App-Assembly
/// 3. tg_ffi.dll im Core-Repo (target/debug bzw. target/release) – für Entwicklung
/// </summary>
public sealed class TensegrityNativeBinding : ITensegrityNative, ITensegrityInference
{
    public const string PathEnvironmentVariable = "TENSEGRITY_NATIVE_PATH";
    public const string LibraryFileName = "tg_ffi.dll";

    private readonly IntPtr _library;
    private readonly AllowsTransportFn _allowsTransport;
    private readonly BrowserBlockedFn _browserBlocked;
    private readonly TaintMaxFn _taintMax;
    private readonly FoldPayloadBytesFn _foldPayloadBytes;
    private readonly ConfigureAutoFn? _configureAuto;
    private readonly GenerateLocalFn? _generateLocal;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    private delegate bool AllowsTransportFn(byte level);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    private delegate bool BrowserBlockedFn(byte level);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int TaintMaxFn([In] byte[] levels, UIntPtr len);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long FoldPayloadBytesFn(UIntPtr deltaBytes, byte level);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int ConfigureAutoFn();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long GenerateLocalFn([In] byte[] prompt, UIntPtr promptLen, [Out] byte[] output, UIntPtr outCap);

    private TensegrityNativeBinding(
        IntPtr library,
        AllowsTransportFn allowsTransport,
        BrowserBlockedFn browserBlocked,
        TaintMaxFn taintMax,
        FoldPayloadBytesFn foldPayloadBytes,
        ConfigureAutoFn? configureAuto,
        GenerateLocalFn? generateLocal)
    {
        _library = library;
        _allowsTransport = allowsTransport;
        _browserBlocked = browserBlocked;
        _taintMax = taintMax;
        _foldPayloadBytes = foldPayloadBytes;
        _configureAuto = configureAuto;
        _generateLocal = generateLocal;
        SupportsLocalInference = configureAuto is not null && generateLocal is not null;
    }

    /// <summary>True, wenn die geladene DLL die Inferenz-Exporte (ADR-0012) enthält.</summary>
    public bool SupportsLocalInference { get; }

    /// <summary>
    /// Versucht, die native Bibliothek zu laden. Liefert false, wenn kein Kandidatenpfad
    /// existiert, die Datei nicht ladbar ist oder ein Export fehlt.
    /// </summary>
    public static bool TryCreate(
        string? explicitPath,
        [NotNullWhen(true)] out TensegrityNativeBinding? binding,
        out string? error)
    {
        return TryLoadFromCandidates(EnumerateCandidatePaths(explicitPath), out binding, out error);
    }

    /// <summary>
    /// Lädt aus einer expliziten Kandidatenliste. Öffentlich für Diagnose und Tests;
    /// produktiv sollte <see cref="TryCreate"/> genutzt werden.
    /// </summary>
    public static bool TryLoadFromCandidates(
        IEnumerable<string> candidates,
        [NotNullWhen(true)] out TensegrityNativeBinding? binding,
        out string? error)
    {
        binding = null;
        error = null;

        var candidateArray = candidates.ToArray();
        if (candidateArray.Length == 0)
        {
            error = "no candidate path exists";
            return false;
        }

        foreach (var candidate in candidateArray)
        {
            if (!NativeLibrary.TryLoad(candidate, out var handle))
            {
                error = $"library not loadable: {candidate}";
                continue;
            }

            if (TryBindExports(handle, out var bindingInstance, out error))
            {
                binding = bindingInstance;
                error = null;
                return true;
            }

            NativeLibrary.Free(handle);
            error = $"missing export in {candidate}: {error}";
        }

        return false;
    }

    /// <summary>Alle Kandidatenpfade, die tatsächlich existieren (für Diagnose und Tests).</summary>
    public static IEnumerable<string> EnumerateCandidatePaths(string? explicitPath)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in RawCandidatePaths(explicitPath))
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate) && seen.Add(candidate))
            {
                yield return candidate;
            }
        }
    }

    private static IEnumerable<string> RawCandidatePaths(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            yield return explicitPath;
        }

        var fromEnvironment = Environment.GetEnvironmentVariable(PathEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            yield return fromEnvironment;
        }

        yield return Path.Combine(AppContext.BaseDirectory, LibraryFileName);

        // Entwicklungs-Fallback: Core-Repo neben dem App-Repo im selben Workspace.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var coreRepo = Path.Combine(directory.FullName, "core_restatify-tensegrity-ai-slm");
            if (Directory.Exists(coreRepo))
            {
                yield return Path.Combine(coreRepo, "target", "debug", LibraryFileName);
                yield return Path.Combine(coreRepo, "target", "release", LibraryFileName);
                yield break;
            }

            directory = directory.Parent;
        }
    }

    private static bool TryBindExports(
        IntPtr handle,
        [NotNullWhen(true)] out TensegrityNativeBinding? binding,
        out string? error)
    {
        binding = null;

        if (!TryGetExport(handle, "tg_policy_allows_transport", out AllowsTransportFn? allowsTransport)
            || !TryGetExport(handle, "tg_policy_browser_blocked", out BrowserBlockedFn? browserBlocked)
            || !TryGetExport(handle, "tg_taint_max", out TaintMaxFn? taintMax)
            || !TryGetExport(handle, "tg_fold_payload_bytes", out FoldPayloadBytesFn? foldPayloadBytes))
        {
            error = "one or more exports missing";
            return false;
        }

        // Inferenz-Exporte sind optional: ältere DLLs ohne ADR-0012 bleiben ladbar,
        // die lokale Generierung ist dann einfach nicht verfügbar.
        var hasConfigure = TryGetExport(handle, "tg_infer_configure_auto", out ConfigureAutoFn? configureAuto);
        var hasGenerate = TryGetExport(handle, "tg_generate_local", out GenerateLocalFn? generateLocal);

        binding = new TensegrityNativeBinding(
            handle,
            allowsTransport,
            browserBlocked,
            taintMax,
            foldPayloadBytes,
            hasConfigure ? configureAuto : null,
            hasGenerate ? generateLocal : null);
        error = null;
        return true;
    }

    private static bool TryGetExport<TDelegate>(IntPtr handle, string name, [NotNullWhen(true)] out TDelegate? result)
        where TDelegate : Delegate
    {
        if (NativeLibrary.TryGetExport(handle, name, out var address))
        {
            result = Marshal.GetDelegateForFunctionPointer<TDelegate>(address);
            return true;
        }

        result = null;
        return false;
    }

    // Die folgenden Methoden sind die rohe ABI-Grenze zum Rust-Core. Sie werden über den
    // Paritäts-Test verifiziert, wenn die DLL vorhanden ist; ohne natives Binary sind sie
    // nicht überdeckbar.
    [ExcludeFromCodeCoverage(Justification = "Native ABI-Grenze: nur mit vorhandener tg_ffi.dll ausführbar (Paritäts-Test).")]
    public bool AllowsTransport(byte levelIndex) => _allowsTransport(levelIndex);

    [ExcludeFromCodeCoverage(Justification = "Native ABI-Grenze: nur mit vorhandener tg_ffi.dll ausführbar (Paritäts-Test).")]
    public bool BrowserBlocked(byte levelIndex) => _browserBlocked(levelIndex);

    [ExcludeFromCodeCoverage(Justification = "Native ABI-Grenze: nur mit vorhandener tg_ffi.dll ausführbar (Paritäts-Test).")]
    public int TaintMax(byte[] levelIndices) => _taintMax(levelIndices, (UIntPtr)levelIndices.Length);

    [ExcludeFromCodeCoverage(Justification = "Native ABI-Grenze: nur mit vorhandener tg_ffi.dll ausführbar (Paritäts-Test).")]
    public long FoldPayloadBytes(ulong deltaBytes, byte levelIndex) => _foldPayloadBytes((UIntPtr)deltaBytes, levelIndex);

    [ExcludeFromCodeCoverage(Justification = "Native ABI-Grenze: benötigt tg_ffi.dll und eingerichtete lokale Laufzeit.")]
    public bool ConfigureLocal() => _configureAuto?.Invoke() == 0;

    [ExcludeFromCodeCoverage(Justification = "Native ABI-Grenze: benötigt tg_ffi.dll und eingerichtete lokale Laufzeit.")]
    public string? GenerateLocal(string prompt, int maxOutputBytes = 4096)
    {
        if (_generateLocal is null)
        {
            return null;
        }

        var promptBytes = Encoding.UTF8.GetBytes(prompt);
        var output = new byte[maxOutputBytes];
        var written = _generateLocal(promptBytes, (UIntPtr)promptBytes.Length, output, (UIntPtr)output.Length);
        return written >= 0 ? Encoding.UTF8.GetString(output, 0, (int)written) : null;
    }
}
