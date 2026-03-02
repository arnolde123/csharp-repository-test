using System;
using System.Globalization;
using System.Text;

namespace DocumentationBenchmark.Utilities
{
    public static class StringSanitizer
    {
        public static string NormalizeWhitespace(string input)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input), "Input string cannot be null");

            if (input.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(input.Length);
            bool previousWasWhitespace = false;

            foreach (char c in input)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!previousWasWhitespace)
                    {
                        builder.Append(' ');
                        previousWasWhitespace = true;
                    }
                }
                else
                {
                    builder.Append(c);
                    previousWasWhitespace = false;
                }
            }

            return builder.ToString().Trim();
        }

        internal static string ToSlug(string phrase, int maxLength = 50)
        {
            if (string.IsNullOrWhiteSpace(phrase))
                return string.Empty;

            if (maxLength < 5)
                throw new ArgumentException("Max length must be at least 5 characters", nameof(maxLength));

            var normalized = phrase.Normalize(NormalizationForm.FormD);
            var slug = new StringBuilder(maxLength);
            int charCount = 0;

            foreach (var c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                
                if (unicodeCategory == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(c) && charCount < maxLength)
                {
                    slug.Append(char.ToLowerInvariant(c));
                    charCount++;
                }
                else if ((c == ' ' || c == '-') && charCount > 0 && slug[^1] != '-')
                {
                    slug.Append('-');
                    charCount++;
                }
            }

            return slug.ToString().TrimEnd('-');
        }

        private static bool ContainsUnsafeCharacters(ReadOnlySpan<char> input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] < 32 || input[i] > 126)
                    return true;
            }
            return false;
        }
    }

    public class SanitizationException : Exception
    {
        public SanitizationException(string message) : base(message) { }
        public SanitizationException(string message, Exception inner) : base(message, inner) { }
    }
}
