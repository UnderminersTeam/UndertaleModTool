using System.Text;

namespace UndertaleModTool_WinUI;

internal static class StartupArguments
{
    internal static string[] Parse(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            return [];

        List<string> result = [];
        StringBuilder current = new();
        bool inQuotes = false;

        foreach (char c in arguments)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
            result.Add(current.ToString());

        return result.ToArray();
    }

    internal static string? FindSupportedDataFilePath(IReadOnlyList<string> args) =>
        FindSupportedDataFilePath(args, 0, args.Count);

    internal static string? GetSupportedDataFileOptionValue(IReadOnlyList<string> args, string option)
    {
        int optionIndex = IndexOf(args, option);
        if (optionIndex < 0)
            return null;

        int valueStart = optionIndex + 1;
        int valueEnd = FindNextOptionIndex(args, valueStart);
        return FindSupportedDataFilePath(args, valueStart, valueEnd) ??
               JoinTokens(args, valueStart, valueEnd);
    }

    internal static string? GetOptionValue(IReadOnlyList<string> args, string option)
    {
        int optionIndex = IndexOf(args, option);
        if (optionIndex < 0)
            return null;

        int valueStart = optionIndex + 1;
        int valueEnd = FindNextOptionIndex(args, valueStart);
        return JoinTokens(args, valueStart, valueEnd);
    }

    private static string? FindSupportedDataFilePath(IReadOnlyList<string> args, int startIndex, int endIndex)
    {
        startIndex = Math.Clamp(startIndex, 0, args.Count);
        endIndex = Math.Clamp(endIndex, startIndex, args.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            string value = CleanToken(args[i]);
            if (MainPage.IsSupportedDataFilePath(value) && File.Exists(value))
                return value;
        }

        for (int end = startIndex; end < endIndex; end++)
        {
            if (!MainPage.IsSupportedDataFilePath(args[end]))
                continue;

            for (int start = startIndex; start <= end; start++)
            {
                if (IsOptionToken(args[start]))
                    continue;

                string? value = JoinTokens(args, start, end + 1);
                if (value is not null && MainPage.IsSupportedDataFilePath(value) && File.Exists(value))
                    return value;
            }
        }

        for (int i = startIndex; i < endIndex; i++)
        {
            string value = CleanToken(args[i]);
            if (MainPage.IsSupportedDataFilePath(value))
                return value;
        }

        return null;
    }

    private static int FindNextOptionIndex(IReadOnlyList<string> args, int startIndex)
    {
        for (int i = Math.Max(0, startIndex); i < args.Count; i++)
        {
            if (IsOptionToken(args[i]))
                return i;
        }

        return args.Count;
    }

    private static int IndexOf(IReadOnlyList<string> args, string value)
    {
        for (int i = 0; i < args.Count; i++)
        {
            if (string.Equals(args[i], value, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    private static string? JoinTokens(IReadOnlyList<string> args, int startIndex, int endIndex)
    {
        if (startIndex < 0 || startIndex >= endIndex || endIndex > args.Count)
            return null;

        return string.Join(' ', args.Skip(startIndex).Take(endIndex - startIndex).Select(CleanToken));
    }

    private static string CleanToken(string value) =>
        value.Trim().Trim('"');

    private static bool IsOptionToken(string value) =>
        value.StartsWith("--", StringComparison.Ordinal);
}
