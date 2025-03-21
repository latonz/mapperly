using System.Diagnostics.CodeAnalysis;

namespace Riok.Mapperly.Helpers;

public static class Pluralizer
{
    private static readonly IReadOnlyList<Rule> _rules = new[]
    {
        new Rule("ies", "y"),
        new Rule("ves", "f"),
        new Rule("oes", "o"),
        new Rule("ses", "s"),
        new Rule("xes", "x"),
        new Rule("s", string.Empty),
    };

    public static string? ToSingular(string plural)
    {
        TryToSingular(plural, out var singular);
        return singular;
    }

    public static bool TryToSingular(string plural, [NotNullWhen(true)] out string? singular)
    {
        foreach (var rule in _rules)
        {
            if (rule.TrySingular(plural, out singular))
            {
                return true;
            }
        }

        singular = null;
        return false;
    }

    private record Rule(string PluralSuffix, string SingularSuffix)
    {
        public bool TrySingular(string plural, [NotNullWhen(true)] out string? singular)
        {
            var ok = plural.EndsWith(PluralSuffix, StringComparison.OrdinalIgnoreCase);
            if (ok)
            {
                singular = plural[..^PluralSuffix.Length] + SingularSuffix;
            }
            else
            {
                singular = null;
            }

            return ok;
        }
    }
}
