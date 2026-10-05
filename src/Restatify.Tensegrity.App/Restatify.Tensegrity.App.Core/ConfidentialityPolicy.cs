namespace Restatify.Tensegrity.App.Core;

public static class ConfidentialityPolicy
{
    public static bool IsCloudAllowed(ConfidentialityLevel level)
    {
        return level is ConfidentialityLevel.Offen or ConfidentialityLevel.Vertraulich;
    }

    public static bool IsLocalOnly(ConfidentialityLevel level)
    {
        return level is ConfidentialityLevel.NurLokal;
    }

    public static bool IsBlockedInWeb(ConfidentialityLevel level)
    {
        return level is ConfidentialityLevel.StrengGeheim or ConfidentialityLevel.NurLokal;
    }

    public static ConfidentialityLevel TaintMax(IEnumerable<ConfidentialityLevel> levels)
    {
        if (levels is null)
        {
            return ConfidentialityLevel.Offen;
        }

        return levels.DefaultIfEmpty(ConfidentialityLevel.Offen).Max();
    }
}
