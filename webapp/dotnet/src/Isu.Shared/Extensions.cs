using System;
using System.Collections.Generic;
using System.Text;

namespace Isu.Shared
{
    public static class Extensions
    {
        public static string ToTitleCase(this string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            if (str.Length < 2)
                return str;

            return str.Substring(0, 1).ToUpper() + str.Substring(1);
        }

        public static string ToHexString(this IEnumerable<byte> bytes)
        {
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                var hex = b.ToString("x2");
                sb.Append(hex);
            }
            return sb.ToString();
        }
    }
}
