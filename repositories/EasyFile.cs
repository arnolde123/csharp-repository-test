using System;
using System.Globalization;
using System.Text;

namespace DocumentationBenchmark.Utilities
{
    /// <summary>
    /// Provides methods for sanitizing strings.
    /// </summary>
    public static class StringSanitizer
    {
        /// <summary>
        /// Normalizes whitespace in the given input string by replacing multiple whitespace characters with a single space.
        /// </summary>
        /// <param name="input">The input string to normalize.</param>
        /// <returns>A string with normalized whitespace.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the input string is null.</exception>
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

        /// <summary>
        /// Converts a phrase into a URL-friendly slug.
        /// </summary>
        /// <param name="phrase">The phrase to convert into a slug.</param>
        /// <param name="maxLength">The maximum length of the slug. Default is 50.</param>
        /// <returns>A URL-friendly slug derived from the phrase.</returns>
        /// <exception cref="ArgumentException">Thrown when the max length is less than 5 characters.</exception>
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

        /// <summary>
        /// Checks if the input contains unsafe characters.
        /// </summary>
        /// <param name="input">The input span of characters to check.</param>
        /// <returns><c>true</c> if unsafe characters are found; otherwise, <c>false</c>.</returns>
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

    /// <summary>
    /// Exception thrown when sanitization fails.
    /// </summary>
    public class SanitizationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SanitizationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public SanitizationException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SanitizationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public SanitizationException(string message, Exception inner) : base(message, inner) { }
    }
}