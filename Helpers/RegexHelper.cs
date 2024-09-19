namespace Homiebot.Helpers;
using System.Text.RegularExpressions;
public static partial class RegexHelper
{
    [GeneratedRegex(@"(?:(\d+)\s*X\s*)?(\d*)D(\d*)((?:[+\/*-]\d+)|(?:[-][LH]))?", RegexOptions.IgnoreCase)]
    public static partial Regex DiceRoll();
    [GeneratedRegex(@"(acab includes\b)(.+)", RegexOptions.IgnoreCase)]
    public static partial Regex ACABRegex();
    [GeneratedRegex(@"(.+)\b(is a cia psyop|are a cia psyop\b)", RegexOptions.IgnoreCase)]
    public static partial Regex PsyopRegex();
    [GeneratedRegex(@"(.+)\b(found on wish.com\b)", RegexOptions.IgnoreCase)]
    public static partial Regex WishRegex();
    [GeneratedRegex(@"\b(why)|(how)\b", RegexOptions.IgnoreCase)]
    public static partial Regex ExcuseRegex();
    [GeneratedRegex(@"\b(thank you)|(thanks)\b", RegexOptions.IgnoreCase)]
    public static partial Regex ThanksRegex();
    [GeneratedRegex(@"\b(fuck you)|(screw you)|(up yours)\b", RegexOptions.IgnoreCase)]
    public static partial Regex RudeRegex();
    [GeneratedRegex(@"(@\([a-z,\.]+\))+", RegexOptions.IgnoreCase)]
    public static partial Regex JsonRegex();
}