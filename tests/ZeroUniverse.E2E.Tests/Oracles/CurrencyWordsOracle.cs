using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroUniverse.E2E.Tests.Oracles
{
    public sealed class CurrencyWordsOptions
    {
        public static readonly CurrencyWordsOptions Northern = new() { UseSouthernZeroTens = false, UseSouthernThousands = false };
        public static readonly CurrencyWordsOptions Southern = new() { UseSouthernZeroTens = true, UseSouthernThousands = true };

        public bool UseSouthernZeroTens { get; set; } = false; // "lẻ" vs "linh"
        public bool UseSouthernThousands { get; set; } = false; // "ngàn" vs "nghìn"
        public bool CapitalizeFirstLetter { get; set; } = true;
    }

    /// <summary>
    /// Authoritative reference oracle for Circular 200/2014/TT-BTC currency to words spelling.
    /// Strictly mirrors the canonical specifications in TEST_INFRA.md § 3.3.
    /// </summary>
    public static class CurrencyWordsOracle
    {
        private static readonly string[] Digits = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };

        public static string ToVnCurrencyWords(
            decimal amount,
            string currencyUnit = "đồng",
            string subunitUnit = "xu",
            bool appendWholeNumberSuffix = true,
            CurrencyWordsOptions? options = null)
        {
            options ??= CurrencyWordsOptions.Northern;

            if (amount == 0m)
            {
                return appendWholeNumberSuffix ? $"Không {currencyUnit} chẵn" : $"Không {currencyUnit}";
            }

            bool isNegative = amount < 0m;
            amount = Math.Abs(amount);

            long integerPart = (long)Math.Truncate(amount);
            decimal fractionalPart = amount - integerPart;

            var sb = new StringBuilder();
            if (isNegative) sb.Append("Âm ");

            string intWords = ToVnWords(integerPart, options);
            sb.Append(intWords);
            sb.Append(' ');
            sb.Append(currencyUnit);

            int cents = (int)Math.Round(fractionalPart * 100m);
            if (cents > 0)
            {
                sb.Append(" và ");
                sb.Append(ToVnWords(cents, options));
                sb.Append(' ');
                sb.Append(subunitUnit);
            }
            else if (appendWholeNumberSuffix)
            {
                sb.Append(" chẵn");
            }

            string result = sb.ToString();
            if (options.CapitalizeFirstLetter && result.Length > 0)
            {
                result = char.ToUpperInvariant(result[0]) + result.Substring(1);
            }
            return result;
        }

        public static string ToVnWords(long number, CurrencyWordsOptions? options = null)
        {
            options ??= CurrencyWordsOptions.Northern;
            if (number == 0) return "không";

            string thousandsWord = options.UseSouthernThousands ? "ngàn" : "nghìn";
            string[] scaleWords = { "", thousandsWord, "triệu", "tỷ" };

            // Group into 3-digit triads
            var triads = new List<int>();
            long temp = Math.Abs(number);
            while (temp > 0)
            {
                triads.Add((int)(temp % 1000));
                temp /= 1000;
            }

            var parts = new List<string>();

            for (int i = triads.Count - 1; i >= 0; i--)
            {
                int triad = triads[i];
                if (triad == 0) continue;

                bool hasHigher = false;
                for (int h = triads.Count - 1; h > i; h--)
                {
                    if (triads[h] > 0) { hasHigher = true; break; }
                }

                string triadWords = ReadTriad(triad, hasHigher, options);
                string scale = GetScaleName(i, scaleWords);

                if (!string.IsNullOrEmpty(scale))
                {
                    parts.Add($"{triadWords} {scale}");
                }
                else
                {
                    parts.Add(triadWords);
                }
            }

            return string.Join(" ", parts);
        }

        private static string GetScaleName(int level, string[] scaleWords)
        {
            if (level == 0) return string.Empty;
            int billionPowers = level / 3;
            int remainder = level % 3;

            var sb = new StringBuilder();
            if (remainder > 0)
            {
                sb.Append(scaleWords[remainder]);
                sb.Append(' ');
            }

            for (int b = 0; b < billionPowers; b++)
            {
                sb.Append("tỷ ");
            }

            return sb.ToString().TrimEnd();
        }

        private static string ReadTriad(int value, bool hasHigher, CurrencyWordsOptions options)
        {
            int h = value / 100;
            int t = (value % 100) / 10;
            int u = value % 10;

            var words = new List<string>();
            string zeroTensWord = options.UseSouthernZeroTens ? "lẻ" : "linh";

            if (h > 0 || hasHigher)
            {
                words.Add($"{Digits[h]} trăm");
            }

            if (t > 1)
            {
                words.Add($"{Digits[t]} mươi");
                if (u == 1) words.Add("mốt");
                else if (u == 4) words.Add("tư");
                else if (u == 5) words.Add("lăm");
                else if (u > 0) words.Add(Digits[u]);
            }
            else if (t == 1)
            {
                words.Add("mười");
                if (u == 5) words.Add("lăm");
                else if (u > 0) words.Add(Digits[u]);
            }
            else if (t == 0 && u > 0)
            {
                if (h > 0 || hasHigher) words.Add(zeroTensWord);
                words.Add(Digits[u]);
            }

            return string.Join(" ", words);
        }
    }
}
