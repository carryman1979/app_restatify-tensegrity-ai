using System.Net.Http.Json;
using System.Text.Json;

namespace Restatify.Tensegrity.App.Core;

// Runtime client that posts to the cloud gateway. Not covered by unit tests;
// it is pure HTTP transport and exercised via the cloud smoke/integration path.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class CloudGateway : ICloudGateway
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://127.0.0.1:8080")
    };

    public async Task<GenerateResult> GenerateAsync(GenerateRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            RequestId = request.RequestId,
            ConfidentialityLevel = CloudRequestBuilder.ToCloudLevelName(request.Level),
            Prompt = request.Prompt,
            ExpertSlotIds = request.ExpertSlotIds ?? Array.Empty<string>()
        };

        using var response = await Http.PostAsJsonAsync("/inference/generate", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        var allowed = root.GetProperty("allowed").GetBoolean();
        var route = root.GetProperty("route").GetString() ?? string.Empty;
        var slots = root.GetProperty("selectedExpertSlotIds").EnumerateArray()
            .Select(slot => slot.GetString()!)
            .ToArray();

        return new GenerateResult(allowed, route, slots);
    }
}
