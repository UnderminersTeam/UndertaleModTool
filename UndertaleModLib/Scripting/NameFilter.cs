using System;
using System.Text.RegularExpressions;

namespace UndertaleModLib.Scripting;

public enum NameFilterMode
{
    Exact,
    Regex,
    Wildcard
}

public static class NameFilter
{
    public static bool IsMatch(string name, string pattern, NameFilterMode mode)
    {
        if (name is null || pattern is null)
            return false;

        if      (mode == NameFilterMode.Exact)      return name == pattern;
        else if (mode == NameFilterMode.Regex)      return Regex.IsMatch(name, pattern);
        else if (mode == NameFilterMode.Wildcard)   return WildcardMatch(name, pattern);
        else                                        return false;
    }

    private static bool WildcardMatch(string input, string pattern)
    {
        int i = 0, j = 0;
        int starIdx = -1, matchIdx = -1;

        while (i < input.Length)
        {
            if (j < pattern.Length && (pattern[j] == '?' ||
                char.ToUpperInvariant(pattern[j]) == char.ToUpperInvariant(input[i])))
            {
                i++;
                j++;
            }
            else if (j < pattern.Length && pattern[j] == '*')
            {
                starIdx = j;
                matchIdx = i;
                j++;
            }
            else if (starIdx != -1)
            {
                j = starIdx + 1;
                matchIdx++;
                i = matchIdx;
            }
            else
            {
                return false;
            }
        }

        while (j < pattern.Length && pattern[j] == '*')
            j++;

        return j == pattern.Length;
    }
}
