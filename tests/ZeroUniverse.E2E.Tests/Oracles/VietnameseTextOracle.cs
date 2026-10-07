using System;
using System.Text;

namespace ZeroUniverse.E2E.Tests.Oracles
{
    /// <summary>
    /// Authoritative reference oracle for Vietnamese text processing and diacritics removal.
    /// Strictly mirrors the canonical specifications in TEST_INFRA.md § 3.1.
    /// </summary>
    public static class VietnameseTextOracle
    {
        private static readonly string[] VietnameseSigns = new string[]
        {
            "aAeEoOuUiIdDyY",
            "áàạảãâấầậẩẫăắằặẳẵ",
            "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
            "éèẹẻẽêếềệểễ",
            "ÉÈẸẺẼÊẾỀỆỂỄ",
            "óòọỏõôốồộổỗơớờợởỡ",
            "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
            "úùụủũưứừựửữ",
            "ÚÙỤỦŨƯỨỪỰỬỮ",
            "íìịỉĩ",
            "ÍÌỊỈĨ",
            "đ",
            "Đ",
            "ýỳỵỷỹ",
            "ÝỲỴỶỸ"
        };

        public static char StripDiacritic(char c)
        {
            for (int i = 1; i < VietnameseSigns.Length; i++)
            {
                for (int j = 0; j < VietnameseSigns[i].Length; j++)
                {
                    if (VietnameseSigns[i][j] == c)
                    {
                        return VietnameseSigns[0][i - 1];
                    }
                }
            }
            return c;
        }

        public static string RemoveDiacritics(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            var sb = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                sb.Append(StripDiacritic(input[i]));
            }
            return sb.ToString();
        }

        public static string ToSearchKeyword(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var unaccented = RemoveDiacritics(input).ToLowerInvariant();
            var sb = new StringBuilder(unaccented.Length);
            bool lastWasSpace = false;

            for (int i = 0; i < unaccented.Length; i++)
            {
                char c = unaccented[i];
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                    lastWasSpace = false;
                }
                else
                {
                    if (!lastWasSpace && sb.Length > 0)
                    {
                        sb.Append(' ');
                        lastWasSpace = true;
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }

        public static string ToSlug(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string keyword = ToSearchKeyword(input);
            return keyword.Replace(' ', '-');
        }

        public static string UnSignedTransfer(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            string unaccented = RemoveDiacritics(input);
            var sb = new StringBuilder(unaccented.Length);

            for (int i = 0; i < unaccented.Length; i++)
            {
                char c = unaccented[i];
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else if (c == ' ')
                {
                    sb.Append('_');
                }
                else
                {
                    sb.Append('-');
                }
            }

            return sb.ToString();
        }
    }
}
