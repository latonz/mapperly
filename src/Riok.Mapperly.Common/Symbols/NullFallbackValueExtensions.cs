using Microsoft.CodeAnalysis;

namespace Riok.Mapperly.Common.Symbols;

public static class NullFallbackValueExtensions
{
    public static bool IsNullable(this NullFallbackValue fallbackValue, ITypeSymbol targetType) =>
        fallbackValue == NullFallbackValue.Default && targetType.IsNullable();
}
