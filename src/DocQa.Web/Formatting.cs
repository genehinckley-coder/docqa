using System.Globalization;

namespace DocQa.Web;

/// <summary>How the UI writes amounts and counts.</summary>
internal static class Formatting
{
    /// <summary>A US-dollar amount to the cent, as the design shows it: <c>$2.16</c>.</summary>
    public static string Money(decimal amount) => "$" + amount.ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>A count and its noun: "1 turn", "2 turns".</summary>
    public static string Count(int count, string noun) =>
        count.ToString(CultureInfo.InvariantCulture) + " " + (count == 1 ? noun : noun + "s");
}
