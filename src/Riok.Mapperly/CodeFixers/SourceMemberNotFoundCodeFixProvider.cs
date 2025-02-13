using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riok.Mapperly.Diagnostics;
using Riok.Mapperly.Emit.Syntax;

namespace Riok.Mapperly.CodeFixers;

// TODO this probably needs to be moved in another assembly
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SourceMemberNotFoundCodeFixProvider))]
[Shared]
public class SourceMemberNotFoundCodeFixProvider() : AddAttributeCodeFixProvider("Ignore", DiagnosticDescriptors.SourceMemberNotFound)
{
    protected override AttributeListSyntax BuildAttribute(SyntaxFactoryHelper syntaxFactory, Diagnostic diagnostic)
    {
        // TODO
        return syntaxFactory.MapperIgnoreSourceAttribute(diagnostic.Properties["Member"] ?? "<unknown>");
    }
}
