using System;

namespace dm.TanTipBot.Common
{
    public static class Extensions
    {
        public static string Format(this int source)
        {
            //return string.Format("{0:#,##0.00####}", source);
            return string.Format("{0:#,##0}", source);
        }

        public static string ToDate(this DateTime source)
        {
            return source.ToString("r");
        }

        public static string ToString(this Currency currency, bool shortName = false)
        {
            string s = string.Empty;
            switch (currency)
            {
                case Currency.Tangram:
                    s = (shortName) ? "TGM" : "Tangram";
                    break;
            }
            return s;
        }

        public static string TrimEnd(this string source, string suffixToRemove, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {

            if (source != null && suffixToRemove != null && source.EndsWith(suffixToRemove, comparisonType))
            {
                return source.Substring(0, source.Length - suffixToRemove.Length);
            }
            else
            {
                return source;
            }
        }
    }
}
