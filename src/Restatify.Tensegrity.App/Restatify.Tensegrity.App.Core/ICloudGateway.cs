namespace Restatify.Tensegrity.App.Core;

public interface ICloudGateway
{
    Task<GenerateResult> GenerateAsync(GenerateRequest request, CancellationToken cancellationToken = default);
}

public sealed record GenerateRequest(
    string RequestId,
    ConfidentialityLevel Level,
    string Prompt,
    IReadOnlyList<string>? ExpertSlotIds = null);

public sealed record GenerateResult(
    bool Allowed,
    string Route,
    IReadOnlyList<string> SelectedExpertSlotIds);
