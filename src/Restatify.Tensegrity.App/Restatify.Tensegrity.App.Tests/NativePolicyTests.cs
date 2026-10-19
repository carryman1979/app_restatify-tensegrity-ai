using Restatify.Tensegrity.App.Core;
using Restatify.Tensegrity.App.Core.Native;

namespace Restatify.Tensegrity.App.Tests;

public class NativePolicyTests
{
    private sealed class FakeNative : ITensegrityNative
    {
        public List<byte> AllowsTransportCalls { get; } = new();
        public List<byte> BrowserBlockedCalls { get; } = new();
        public List<byte[]> TaintMaxCalls { get; } = new();
        public List<(ulong Bytes, byte Level)> FoldCalls { get; } = new();

        public bool AllowsTransportResult { get; set; }
        public bool BrowserBlockedResult { get; set; }
        public int TaintMaxResult { get; set; }
        public long FoldResult { get; set; }

        public bool AllowsTransport(byte levelIndex)
        {
            AllowsTransportCalls.Add(levelIndex);
            return AllowsTransportResult;
        }

        public bool BrowserBlocked(byte levelIndex)
        {
            BrowserBlockedCalls.Add(levelIndex);
            return BrowserBlockedResult;
        }

        public int TaintMax(byte[] levelIndices)
        {
            TaintMaxCalls.Add(levelIndices);
            return TaintMaxResult;
        }

        public long FoldPayloadBytes(ulong deltaBytes, byte levelIndex)
        {
            FoldCalls.Add((deltaBytes, levelIndex));
            return FoldResult;
        }
    }

    [Test]
    public void TryLoadFromCandidates_with_empty_list_fails()
    {
        var found = TensegrityNativeBinding.TryLoadFromCandidates(
            Array.Empty<string>(),
            out var binding,
            out var error);

        Assert.That(found, Is.False);
        Assert.That(binding, Is.Null);
        Assert.That(error, Is.EqualTo("no candidate path exists"));
    }

    [Test]
    public void TryLoadFromCandidates_with_nonexistent_path_fails()
    {
        var bogus = Path.Combine(Path.GetTempPath(), "definitely-missing", "tg_ffi.dll");

        var found = TensegrityNativeBinding.TryLoadFromCandidates(
            new[] { bogus },
            out var binding,
            out var error);

        Assert.That(found, Is.False);
        Assert.That(binding, Is.Null);
        Assert.That(error, Does.Contain("not loadable"));
    }

    [Test]
    public void EnumerateCandidatePaths_includes_explicit_path_when_file_exists()
    {
        var existing = Path.GetTempFileName();
        try
        {
            var candidates = TensegrityNativeBinding.EnumerateCandidatePaths(existing).ToArray();
            Assert.That(candidates, Does.Contain(existing));
        }
        finally
        {
            File.Delete(existing);
        }
    }

    [Test]
    public void Engine_without_native_uses_managed_backend()
    {
        var engine = new TensegrityPolicyEngine(null);

        Assert.That(engine.Backend, Is.EqualTo("Managed"));
        Assert.That(engine.AllowsTransport(ConfidentialityLevel.Offen), Is.True);
        Assert.That(engine.AllowsTransport(ConfidentialityLevel.StrengGeheim), Is.True);
        Assert.That(engine.AllowsTransport(ConfidentialityLevel.NurLokal), Is.False);
        Assert.That(engine.IsCloudAllowed(ConfidentialityLevel.Offen), Is.True);
        Assert.That(engine.IsCloudAllowed(ConfidentialityLevel.Vertraulich), Is.True);
        Assert.That(engine.IsCloudAllowed(ConfidentialityLevel.StrengGeheim), Is.False);
        Assert.That(engine.IsCloudAllowed(ConfidentialityLevel.NurLokal), Is.False);
        Assert.That(engine.IsBlockedInWeb(ConfidentialityLevel.StrengGeheim), Is.True);
        Assert.That(engine.IsBlockedInWeb(ConfidentialityLevel.Offen), Is.False);
        Assert.That(engine.IsLocalOnly(ConfidentialityLevel.NurLokal), Is.True);
        Assert.That(engine.IsLocalOnly(ConfidentialityLevel.Offen), Is.False);
    }

    [Test]
    public void Engine_without_native_matches_managed_fold_sizes()
    {
        var engine = new TensegrityPolicyEngine(null);

        Assert.That(engine.FoldPayloadBytes(1024, ConfidentialityLevel.Offen), Is.EqualTo(1024));
        Assert.That(engine.FoldPayloadBytes(1024, ConfidentialityLevel.Vertraulich), Is.EqualTo(512));
        Assert.That(engine.FoldPayloadBytes(1024, ConfidentialityLevel.StrengGeheim), Is.EqualTo(256));
        Assert.That(engine.FoldPayloadBytes(1024, ConfidentialityLevel.NurLokal), Is.EqualTo(-1));
    }

    [Test]
    public void Engine_without_native_taint_max_picks_strictest_level()
    {
        var engine = new TensegrityPolicyEngine(null);

        var result = engine.TaintMax(new[]
        {
            ConfidentialityLevel.Offen,
            ConfidentialityLevel.Vertraulich,
            ConfidentialityLevel.Offen,
        });

        Assert.That(result, Is.EqualTo(ConfidentialityLevel.Vertraulich));
    }

    [Test]
    public void Engine_with_native_delegates_to_native_backend()
    {
        var native = new FakeNative { AllowsTransportResult = true, BrowserBlockedResult = true };
        var engine = new TensegrityPolicyEngine(native);

        Assert.That(engine.Backend, Is.EqualTo("Native"));

        var transport = engine.AllowsTransport(ConfidentialityLevel.Vertraulich);
        var blocked = engine.IsBlockedInWeb(ConfidentialityLevel.StrengGeheim);
        // Die Cloud-Zulassung bleibt managed und darf den nativen Kern nicht aufrufen.
        var cloud = engine.IsCloudAllowed(ConfidentialityLevel.Vertraulich);

        Assert.That(transport, Is.True);
        Assert.That(blocked, Is.True);
        Assert.That(cloud, Is.True);
        Assert.That(native.AllowsTransportCalls, Is.EqualTo(new byte[] { 1 }));
        Assert.That(native.BrowserBlockedCalls, Is.EqualTo(new byte[] { 2 }));
    }

    [Test]
    public void Engine_with_native_delegates_taint_max_and_fold()
    {
        var native = new FakeNative { TaintMaxResult = 2, FoldResult = 256 };
        var engine = new TensegrityPolicyEngine(native);

        var max = engine.TaintMax(new[] { ConfidentialityLevel.Offen, ConfidentialityLevel.StrengGeheim });
        var folded = engine.FoldPayloadBytes(1024, ConfidentialityLevel.StrengGeheim);

        Assert.That(max, Is.EqualTo(ConfidentialityLevel.StrengGeheim));
        Assert.That(folded, Is.EqualTo(256));
        Assert.That(native.TaintMaxCalls.Single(), Is.EqualTo(new byte[] { 0, 2 }));
        Assert.That(native.FoldCalls.Single(), Is.EqualTo((1024UL, (byte)2)));
    }

    [Test]
    public void Engine_taint_max_with_empty_input_stays_local_and_never_calls_native()
    {
        var native = new FakeNative { TaintMaxResult = 3 };
        var engine = new TensegrityPolicyEngine(native);

        var result = engine.TaintMax(Array.Empty<ConfidentialityLevel>());

        Assert.That(result, Is.EqualTo(ConfidentialityLevel.Offen));
        Assert.That(native.TaintMaxCalls, Is.Empty);
    }

    [Test]
    public void Engine_taint_max_maps_native_error_code_to_offen()
    {
        var native = new FakeNative { TaintMaxResult = -2 };
        var engine = new TensegrityPolicyEngine(native);

        var result = engine.TaintMax(new[] { ConfidentialityLevel.Offen });

        Assert.That(result, Is.EqualTo(ConfidentialityLevel.Offen));
    }

    [Test]
    public void Parity_native_binding_matches_managed_policy_for_all_levels()
    {
        if (!TensegrityNativeBinding.TryCreate(null, out var binding, out var error))
        {
            Assert.Ignore($"tg_ffi.dll nicht verfügbar ({error}) – Paritäts-Test übersprungen.");
            return;
        }

        var engine = new TensegrityPolicyEngine(binding);
        var managed = new TensegrityPolicyEngine(null);
        Assert.That(engine.Backend, Is.EqualTo("Native"));

        foreach (var level in Enum.GetValues<ConfidentialityLevel>())
        {
            // Transport-Gate: NUR_LOKAL verlässt das Gerät nie, alles darunter darf.
            Assert.That(
                engine.AllowsTransport(level),
                Is.EqualTo(level is not ConfidentialityLevel.NurLokal),
                $"AllowsTransport weicht für {level} ab");

            Assert.That(
                engine.IsBlockedInWeb(level),
                Is.EqualTo(ConfidentialityPolicy.IsBlockedInWeb(level)),
                $"IsBlockedInWeb weicht für {level} ab");

            Assert.That(
                engine.FoldPayloadBytes(1024, level),
                Is.EqualTo(managed.FoldPayloadBytes(1024, level)),
                $"FoldPayloadBytes weicht für {level} ab");
        }

        var taint = engine.TaintMax(new[]
        {
            ConfidentialityLevel.Offen,
            ConfidentialityLevel.Vertraulich,
            ConfidentialityLevel.Offen,
        });
        Assert.That(taint, Is.EqualTo(ConfidentialityLevel.Vertraulich));
    }

    [Test]
    public void MainViewModel_uses_native_engine_when_injected()
    {
        var native = new FakeNative { BrowserBlockedResult = true };
        var viewModel = new MainViewModel(new FakeGateway(), new TensegrityPolicyEngine(native));

        Assert.That(viewModel.PolicyBackend, Is.EqualTo("Native"));
        Assert.That(viewModel.PolicyBackendText, Does.Contain("nativ"));

        viewModel.SelectedLevel = ConfidentialityLevel.StrengGeheim;

        Assert.That(viewModel.WebBlocked, Is.True);
        Assert.That(native.BrowserBlockedCalls, Is.EqualTo(new byte[] { 2 }));
    }

    [Test]
    public void MainViewModel_without_engine_falls_back_to_managed()
    {
        var viewModel = new MainViewModel(new FakeGateway());

        Assert.That(viewModel.PolicyBackend, Is.EqualTo("Managed"));
        Assert.That(viewModel.PolicyBackendText, Does.Contain("Fallback"));
        Assert.That(viewModel.CloudAllowed, Is.True);

        viewModel.SelectedLevel = ConfidentialityLevel.StrengGeheim;
        Assert.That(viewModel.CloudAllowed, Is.False);
    }

    [Test]
    public void CreateDefault_matches_binding_availability()
    {
        var engine = TensegrityPolicyEngine.CreateDefault();

        var nativeExpected = TensegrityNativeBinding.TryCreate(null, out _, out _);
        Assert.That(engine.Backend, Is.EqualTo(nativeExpected ? "Native" : "Managed"));
    }

    [Test]
    public void NativeLocalGateway_reflects_runtime_availability()
    {
        var gateway = NativeLocalGateway.CreateDefault();

        Assert.That(gateway, Is.Not.Null);
        if (!gateway.IsAvailable)
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await gateway.GenerateAsync("x"));
        }
    }

    [Test]
    public async Task LocalGateway_EndToEnd_With_Real_Model()
    {
        if (Environment.GetEnvironmentVariable("TENSEGRITY_E2E_LOCAL") != "1")
        {
            Assert.Ignore("E2E deaktiviert (TENSEGRITY_E2E_LOCAL=1 zum Aktivieren).");
            return;
        }

        var gateway = NativeLocalGateway.CreateDefault();
        Assert.That(gateway.IsAvailable, Is.True, "Lokale Laufzeit nicht eingerichtet (fetch-local-runtime.ps1).");

        var answer = await gateway.GenerateAsync("Sag kurz Hallo auf Deutsch.");

        Assert.That(answer, Is.Not.Empty);
        TestContext.Out.WriteLine($"Lokale Antwort: {answer}");
    }

    private sealed class FakeGateway : ICloudGateway
    {
        public Task<GenerateResult> GenerateAsync(GenerateRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GenerateResult(true, "generalist", Array.Empty<string>()));
        }
    }
}
