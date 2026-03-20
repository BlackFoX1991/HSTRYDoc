using System;
using System.Collections.Generic;

namespace HSTRYDoc
{
    internal readonly record struct TextSearchMatch(int Index, int Length);

    internal static class TextSearch
    {
        public static IReadOnlyList<TextSearchMatch> FindAll(string haystack, string needle, bool matchCase, bool wholeWord)
        {
            if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle))
                return Array.Empty<TextSearchMatch>();

            StringComparison comparison = matchCase
                ? StringComparison.CurrentCulture
                : StringComparison.CurrentCultureIgnoreCase;

            List<TextSearchMatch> matches = new();
            int searchStart = 0;

            while (searchStart <= haystack.Length - needle.Length)
            {
                int idx = haystack.IndexOf(needle, searchStart, comparison);
                if (idx < 0)
                    break;

                if (!wholeWord || IsWholeWordMatch(haystack, idx, needle.Length))
                    matches.Add(new TextSearchMatch(idx, needle.Length));

                searchStart = idx + 1;
            }

            return matches;
        }

        public static string BuildSnippet(string text, int index, int length, int context)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            int start = Math.Max(0, index - context);
            int end = Math.Min(text.Length, index + length + context);
            string snippet = text.Substring(start, end - start).Replace("\r", " ").Replace("\n", " ");

            if (start > 0)
                snippet = " " + snippet;

            if (end < text.Length)
                snippet += " ";

            return snippet;
        }

        private static bool IsWholeWordMatch(string haystack, int index, int length)
        {
            int left = index - 1;
            int right = index + length;

            bool leftBoundary = left < 0 || !IsWordChar(haystack[left]);
            bool rightBoundary = right >= haystack.Length || !IsWordChar(haystack[right]);

            return leftBoundary && rightBoundary;
        }

        private static bool IsWordChar(char c)
            => char.IsLetterOrDigit(c) || c == '_';
    }
}
