namespace Restatify.Tensegrity.App.Core;

public static class CloudRequestBuilder
{
    public static GenerateRequest? TryBuild(string requestId, ConfidentialityLevel level, string prompt, IReadOnlyList<string>? expertSlotIds)
    {
        if (!ConfidentialityPolicy.IsCloudAllowed(level))
        {
            return null;
        }

        var slots = expertSlotIds?
            .Where(slotId => !string.IsNullOrWhiteSpace(slotId))
            .Select(slotId => slotId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new GenerateRequest(requestId, level, prompt, slots);
    }

    public static string ToCloudLevelName(ConfidentialityLevel level)
    {
        return level switch
        {
            ConfidentialityLevel.Offen => "Offen",
            ConfidentialityLevel.Vertraulich => "Vertraulich",
            _ => "Offen"
        };
    }
}
