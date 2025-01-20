using Microsoft.CodeAnalysis;
using Riok.Mapperly.Abstractions;

namespace Riok.Mapperly.Configuration;

// TODO
/// <summary>
/// Roslyn representation of <see cref="MapDerivedTypeAttribute"/>
/// (use <see cref="ITypeSymbol"/> instead of <see cref="Type"/>).
/// Keep in sync with <see cref="MapDerivedTypeAttribute"/>
/// </summary>
/// <param name="SourceType">The source type of the derived type mapping.</param>
/// <param name="TargetType">The target type of the derived type mapping.</param>
public record DerivedTypeMappingConfiguration(ITypeSymbol? SourceType, ITypeSymbol TargetType) : HasSyntaxReference
{
    public DerivedTypeMappingConfiguration(ITypeSymbol targetType, AttributeValue? discriminatorValue)
        : this(null, targetType)
    {
        DiscriminatorValue = discriminatorValue;
    }

    public DerivedTypeMappingConfiguration(ITypeSymbol targetType, AttributeValue? discriminatorValue, IMemberPathConfiguration member)
        : this(targetType, discriminatorValue)
    {
        Member = member;
    }

    public AttributeValue? DiscriminatorValue { get; private init; }

    public IMemberPathConfiguration? Member { get; private init; }
}
