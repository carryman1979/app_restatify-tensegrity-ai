using System.Net.Http.Json;
using System.Text.Json;

namespace Restatify.Tensegrity.App.Core;

// HTTP-Transport zum Hive /feedback-Endpunkt. Nicht durch Unit-Tests abgedeckt –
// reiner Netzwerkpfad, der über die Smoke-Tests verifiziert wird.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class HiveFeedbackChannel : IFeedbackChannel
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://127.0.0.1:50051")
    };

    public async Task<FeedbackAck> SubmitAsync(FeedbackDraft draft, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            event_id = draft.EventId,
            app_instance_id = draft.AppInstanceId,
            model_id = draft.ModelId,
            rating = draft.Rating,
            comment = draft.Comment,
            level = ToHiveLevelName(draft.Level),
            evidence = (draft.Evidence ?? Array.Empty<EvidenceDraft>())
                .Select(entry => new { source_id = entry.SourceId, citation = entry.Citation })
                .ToArray()
        };

        using var response = await Http.PostAsJsonAsync("/feedback", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new FeedbackAck(false, $"Hive lehnte ab: {(int)response.StatusCode}");
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var accepted = document.RootElement.GetProperty("accepted").GetBoolean();
        return new FeedbackAck(accepted, accepted ? "accepted" : "rejected");
    }

    private static string ToHiveLevelName(ConfidentialityLevel level)
    {
        return level switch
        {
            ConfidentialityLevel.Vertraulich => "VERTRAULICH",
            ConfidentialityLevel.StrengGeheim => "STRENG_GEHEIM",
            ConfidentialityLevel.NurLokal => "NUR_LOKAL",
            _ => "OFFEN"
        };
    }
}
