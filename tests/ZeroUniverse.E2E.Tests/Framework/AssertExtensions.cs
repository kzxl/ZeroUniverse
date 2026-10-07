using System;
using Xunit;

namespace ZeroUniverse.E2E.Tests.Framework
{
    public static class AssertExtensions
    {
        public static void AssertNotNullOrEmpty(string? value, string message)
        {
            Assert.True(!string.IsNullOrEmpty(value), message);
        }

        public static void AssertContainsIgnoreCase(string expectedSubstring, string actualText, string message)
        {
            Assert.True(actualText.IndexOf(expectedSubstring, StringComparison.OrdinalIgnoreCase) >= 0,
                $"{message}. Expected '{expectedSubstring}' to be in '{actualText}'");
        }
    }
}
