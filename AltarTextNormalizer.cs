using System.Text.RegularExpressions;

namespace AltarHelperV2
{
    /// <summary>
    /// Converts changing numeric values in altar text to stable configuration keys.
    /// Leading signs are significant: player penalties use -# while bonuses use +#.
    /// </summary>
    public static class AltarTextNormalizer
    {
        private static readonly Regex NumberPattern = new(
            @"(?<sign>[+-]?)\d+(?:[.,]\d+)?",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // In-game item references are wrapped in markup tags, e.g.
        // "[AbyssThread|Abyss Thread]" -> "Abyss Thread".
        private static readonly Regex ItemTagPattern = new(
            @"\[[^\[\]|]+\|(?<text>[^\[\]|]+)\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string StripItemTags(string value) =>
            ItemTagPattern.Replace(value ?? string.Empty, "${text}");

        public static string NormalizeNumbers(string value) =>
            NumberPattern.Replace(StripItemTags(value), "${sign}#");
    }
}
