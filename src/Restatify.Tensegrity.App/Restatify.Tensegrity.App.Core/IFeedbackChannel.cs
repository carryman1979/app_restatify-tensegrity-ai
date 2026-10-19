namespace Restatify.Tensegrity.App.Core;

public interface IFeedbackChannel
{
    Task<FeedbackAck> SubmitAsync(FeedbackDraft draft, CancellationToken cancellationToken = default);
}

public sealed record FeedbackAck(bool Accepted, string Reason);
