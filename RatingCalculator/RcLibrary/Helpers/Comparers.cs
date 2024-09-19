
using RcLibrary.Models.RaiderIoModels;
using System.Diagnostics.CodeAnalysis;

namespace RcLibrary.Helpers;
public class KeyRunListComparer : IEqualityComparer<List<KeyRun>>
{
    public bool Equals(List<KeyRun>? x, List<KeyRun>? y)
    {
        if (x?.Count != y?.Count) return false;

        for (int i = 0; i < x?.Count; i++)
        {
            if (x?[i]?.KeyLevel != y?[i]?.KeyLevel || x?[i]?.ClearTimeMs != y?[i]?.ClearTimeMs || x?[i]?.DungeonShortName?.ToUpper() != x?[i]?.DungeonShortName?.ToUpper())
                return false;
        }
        return true;
    }

    public int GetHashCode([DisallowNull] List<KeyRun> obj)
    {
        string unique = string.Empty;
        obj?.ForEach(x => unique += $"{x?.KeyLevel}{x?.ClearTimeMs}{x?.DungeonShortName?.ToUpper()}");
        return unique.GetHashCode();
    }
}
