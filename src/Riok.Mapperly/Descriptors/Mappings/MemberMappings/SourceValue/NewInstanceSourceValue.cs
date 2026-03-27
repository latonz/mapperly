using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riok.Mapperly.Descriptors.Constructors;

namespace Riok.Mapperly.Descriptors.Mappings.MemberMappings.SourceValue;

/// <summary>
/// A source value which creates a new instance of a type using a constructor.
/// Used to initialize init-only members with a default instance
/// when descendant members need to be mapped via path assignments.
/// </summary>
public class NewInstanceSourceValue(IInstanceConstructor constructor) : ISourceValue
{
    public ExpressionSyntax Build(TypeMappingBuildContext ctx) => constructor.CreateInstance(ctx);
}
