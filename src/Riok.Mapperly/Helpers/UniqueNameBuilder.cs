using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Riok.Mapperly.Helpers;

public class UniqueNameBuilder()
{
    private readonly HashSet<string> _usedNames = new(StringComparer.Ordinal);
    private readonly UniqueNameBuilder? _parentScope;

    private UniqueNameBuilder(UniqueNameBuilder parentScope)
        : this()
    {
        _parentScope = parentScope;
    }

    public void Reserve(string name) => _usedNames.Add(name);

    public UniqueNameBuilder NewScope() => new(this);

    public string New(string name)
    {
        var i = 0;
        var uniqueName = name;
        while (Contains(uniqueName))
        {
            i++;
            uniqueName = name + i;
        }

        _usedNames.Add(uniqueName);

        return uniqueName;
    }

    public string New(string name, IEnumerable<string> reservedNames)
    {
        var scope = NewScope();
        scope.Reserve(reservedNames);
        var uniqueName = scope.New(name);
        _usedNames.Add(uniqueName);
        return uniqueName;
    }

    public string NewForEnumeration(ExpressionSyntax expression)
    {
        var name = expression switch
        {
            IdentifierNameSyntax identifier => ToCamelCase(Pluralizer.ToSingular(identifier.Identifier.Text)),
            MemberAccessExpressionSyntax accessExpression => ToCamelCase(Pluralizer.ToSingular(accessExpression.Name.Identifier.Text)),
            _ => null,
        };

        return New(name ?? "item");
    }

    private void Reserve(IEnumerable<string> names) => _usedNames.AddRange(names);

    private bool Contains(string name)
    {
        if (_usedNames.Contains(name))
            return true;

        if (_parentScope != null)
            return _parentScope.Contains(name);

        return false;
    }

    [return: NotNullIfNotNull(nameof(name))]
    private string? ToCamelCase(string? name)
    {
        if (name == null)
            return null;

        return name.Length switch
        {
            1 => "" + char.ToLowerInvariant(name[0]),
            _ => char.ToLowerInvariant(name[0]) + name[1..],
        };
    }
}
