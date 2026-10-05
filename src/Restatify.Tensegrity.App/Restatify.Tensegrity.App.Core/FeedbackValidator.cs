namespace Restatify.Tensegrity.App.Core;

public static class FeedbackValidator
{
    public static (bool Accepted, string Reason) Validate(FeedbackDraft draft)
    {
        if (draft is null)
        {
            return (false, "Feedback fehlt.");
        }

        if (string.IsNullOrWhiteSpace(draft.EventId) || string.IsNullOrWhiteSpace(draft.AppInstanceId))
        {
            return (false, "Event- oder App-Kennung fehlt.");
        }

        if (draft.Rating is < -1 or > 1)
        {
            return (false, "Bewertung muss -1, 0 oder 1 sein.");
        }

        if (draft.Level == ConfidentialityLevel.NurLokal)
        {
            return (false, "NUR_LOKAL-Feedback verbleibt auf dem Gerät.");
        }

        var evidence = draft.Evidence ?? Array.Empty<EvidenceDraft>();
        if (draft.Rating != 0)
        {
            if (evidence.Count == 0)
            {
                return (false, "Bewertetes Feedback benötigt mindestens einen Beleg.");
            }

            foreach (var entry in evidence)
            {
                if (string.IsNullOrWhiteSpace(entry.SourceId) || string.IsNullOrWhiteSpace(entry.Citation))
                {
                    return (false, "Belege benötigen Quelle und Zitat.");
                }
            }
        }

        return (true, "accepted");
    }
}

public sealed record FeedbackDraft(
    string EventId,
    string AppInstanceId,
    string ModelId,
    int Rating,
    string Comment,
    ConfidentialityLevel Level,
    IReadOnlyList<EvidenceDraft>? Evidence = null);

public sealed record EvidenceDraft(string SourceId, string Citation, DateTimeOffset ObservedAt);
