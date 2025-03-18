using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Riok.Mapperly.Common.Syntax;

namespace Riok.Mapperly.CodeFixes;

public abstract class AddAttributeCodeFixProvider(string title, params DiagnosticDescriptor[] fixableDiagnostics) : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = fixableDiagnostics.Select(x => x.Id).ToImmutableArray();

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var node = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent;
        if (node is not MethodDeclarationSyntax method)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: _ => AddAttribute(context.Document, root, method, diagnostic),
                equivalenceKey: diagnostic.Id
            ),
            diagnostic
        );
    }

    protected abstract AttributeListSyntax BuildAttribute(SyntaxFactoryHelper syntaxFactory, Diagnostic diagnostic);

    private Task<Document> AddAttribute(Document document, SyntaxNode root, MethodDeclarationSyntax method, Diagnostic diagnostic)
    {
        // TODO
        var factory = new SyntaxFactoryHelper();
        var attribute = BuildAttribute(factory, diagnostic).WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);
        var newMethod = method.WithAttributeLists(method.AttributeLists.Add(attribute));
        var newRoot = root.ReplaceNode(method, newMethod);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
