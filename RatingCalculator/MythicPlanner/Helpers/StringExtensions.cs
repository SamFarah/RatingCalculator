namespace MythicPlanner.Helpers;

public static class StringExtensions
{
    public static string CapitalizeFirst(this string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return str;
        return char.ToUpper(str[0]) + str.Substring(1);
    }
}
