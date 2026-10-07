using System;
using System.Collections.Generic;

namespace ZeroUniverse.E2E.Tests.Oracles
{
    /// <summary>
    /// Authoritative reference oracle for Circular 105 Tax Code (MST), 12-digit CCCD, and Phone validation.
    /// Strictly mirrors the canonical specifications in TEST_INFRA.md § 3.2 & § 3.4.
    /// </summary>
    public static class MasterDataValidatorOracle
    {
        private static readonly int[] MstWeights = { 31, 29, 23, 19, 17, 13, 7, 5, 3 };

        public static readonly Dictionary<string, string> ProvinceCodes = new()
        {
            { "001", "Hà Nội" },
            { "002", "Hà Giang" },
            { "004", "Cao Bằng" },
            { "006", "Bắc Kạn" },
            { "008", "Tuyên Quang" },
            { "010", "Lào Cai" },
            { "011", "Điện Biên" },
            { "012", "Lai Châu" },
            { "014", "Sơn La" },
            { "015", "Yên Bái" },
            { "017", "Hòa Bình" },
            { "019", "Thái Nguyên" },
            { "020", "Lạng Sơn" },
            { "022", "Quảng Ninh" },
            { "024", "Bắc Giang" },
            { "025", "Phú Thọ" },
            { "026", "Vĩnh Phúc" },
            { "027", "Bắc Ninh" },
            { "030", "Hải Dương" },
            { "031", "Hải Phòng" },
            { "033", "Hưng Yên" },
            { "034", "Thái Bình" },
            { "035", "Hà Nam" },
            { "036", "Nam Định" },
            { "037", "Ninh Bình" },
            { "038", "Thanh Hóa" },
            { "040", "Nghệ An" },
            { "042", "Hà Tĩnh" },
            { "044", "Quảng Bình" },
            { "045", "Quảng Trị" },
            { "046", "Thừa Thiên Huế" },
            { "048", "Đà Nẵng" },
            { "049", "Quảng Nam" },
            { "051", "Quảng Ngãi" },
            { "052", "Bình Định" },
            { "054", "Phú Yên" },
            { "056", "Khánh Hòa" },
            { "058", "Ninh Thuận" },
            { "060", "Bình Thuận" },
            { "062", "Kon Tum" },
            { "064", "Gia Lai" },
            { "066", "Đắk Lắk" },
            { "067", "Đắk Nông" },
            { "068", "Lâm Đồng" },
            { "070", "Bình Phước" },
            { "072", "Tây Ninh" },
            { "074", "Bình Dương" },
            { "075", "Đồng Nai" },
            { "077", "Bà Rịa - Vũng Tàu" },
            { "079", "TP. Hồ Chí Minh" },
            { "080", "Long An" },
            { "082", "Tiền Giang" },
            { "083", "Bến Tre" },
            { "084", "Trà Vinh" },
            { "086", "Vĩnh Long" },
            { "087", "Đồng Tháp" },
            { "089", "An Giang" },
            { "091", "Kiên Giang" },
            { "092", "Cần Thơ" },
            { "093", "Hậu Giang" },
            { "094", "Sóc Trăng" },
            { "095", "Bạc Liêu" },
            { "096", "Cà Mau" }
        };

        public static bool IsValidMst(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            // Trim hyphens and spaces
            string clean = input.Trim().Replace(" ", "");

            if (clean.Length == 14 && clean[10] == '-')
            {
                // Format: 0100109106-001
                string branch = clean.Substring(11);
                if (branch == "000") return false;
                foreach (char c in branch)
                    if (!char.IsDigit(c)) return false;

                clean = clean.Substring(0, 10);
            }
            else if (clean.Length == 13)
            {
                // Format: 0100109106001
                string branch = clean.Substring(10);
                if (branch == "000") return false;
                foreach (char c in branch)
                    if (!char.IsDigit(c)) return false;

                clean = clean.Substring(0, 10);
            }
            else if (clean.Length != 10)
            {
                return false;
            }

            // Must be all digits
            for (int i = 0; i < 10; i++)
            {
                if (!char.IsDigit(clean[i])) return false;
            }

            int sum = 0;
            for (int i = 0; i < 9; i++)
            {
                sum += (clean[i] - '0') * MstWeights[i];
            }

            int remainder = sum % 11;
            int checkDigit = 10 - remainder;

            int actualCheck = clean[9] - '0';
            return actualCheck == checkDigit || (checkDigit == 10 && actualCheck == 0);
        }

        public static bool IsValidCccd(string? input, out int birthYear, out bool isMale, out string? provinceName)
        {
            birthYear = 0;
            isMale = false;
            provinceName = null;

            if (string.IsNullOrWhiteSpace(input)) return false;
            string clean = input.Trim();
            if (clean.Length != 12) return false;

            for (int i = 0; i < 12; i++)
            {
                if (!char.IsDigit(clean[i])) return false;
            }

            string pCode = clean.Substring(0, 3);
            if (!ProvinceCodes.TryGetValue(pCode, out provinceName))
            {
                return false;
            }

            char genderCenturyChar = clean[3];
            int centuryBase;

            switch (genderCenturyChar)
            {
                case '0': centuryBase = 1900; isMale = true; break;
                case '1': centuryBase = 1900; isMale = false; break;
                case '2': centuryBase = 2000; isMale = true; break;
                case '3': centuryBase = 2000; isMale = false; break;
                case '4': centuryBase = 2100; isMale = true; break;
                case '5': centuryBase = 2100; isMale = false; break;
                case '6': centuryBase = 2200; isMale = true; break;
                case '7': centuryBase = 2200; isMale = false; break;
                default: return false;
            }

            int yearSuffix = (clean[4] - '0') * 10 + (clean[5] - '0');
            birthYear = centuryBase + yearSuffix;

            return true;
        }

        public static bool IsValidPhone(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            string digits = StripPhoneFormatting(input);
            if (digits.StartsWith("84") && digits.Length == 11)
            {
                digits = "0" + digits.Substring(2);
            }

            if (digits.Length != 10 || digits[0] != '0') return false;

            // Valid mobile prefixes: 03x, 05x, 07x, 08x, 09x
            char second = digits[1];
            return second == '3' || second == '5' || second == '7' || second == '8' || second == '9';
        }

        public static string? NormalizePhone(string? input, bool international = false)
        {
            if (!IsValidPhone(input)) return null;

            string digits = StripPhoneFormatting(input!);
            if (digits.StartsWith("84") && digits.Length == 11)
            {
                digits = "0" + digits.Substring(2);
            }

            if (international)
            {
                return "+84" + digits.Substring(1);
            }
            return digits;
        }

        private static string StripPhoneFormatting(string input)
        {
            var sb = new System.Text.StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                if (char.IsDigit(input[i]))
                    sb.Append(input[i]);
            }
            return sb.ToString();
        }
    }
}
