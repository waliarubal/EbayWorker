using System.Text.RegularExpressions;

namespace EbayWorker.Helpers
{
    public static class Extensions
    {
        public static string SplitCamelCase(this string stringToSplit)
        {
            return Regex.Replace(
                Regex.Replace(
                    stringToSplit,
                    @"(\P{Ll})(\P{Ll}\p{Ll})",
                    "$1 $2"
                ),
                @"(\p{Ll})(\P{Ll})",
                "$1 $2"
            );
        }
    }
}
