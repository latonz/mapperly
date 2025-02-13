using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Simplification;

namespace Riok.Mapperly.Tests;

public static class TestCodeFixHelper
{
    public static async Task<string> Fix<T>(string source)
        where T : CodeFixProvider, new()
    {
        var doc = CreateDocument(source);
        var compilation = await doc.Project.GetCompilationAsync();
        if (compilation == null)
            throw new InvalidOperationException();

        var generator = new MapperGenerator().AsSourceGenerator();
        var driver = CSharpGeneratorDriver.Create([generator], parseOptions: (CSharpParseOptions)doc.Project.ParseOptions!);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);

        var fixer = new T();

        var fixableDiagnostics = diagnostics.Where(x => fixer.FixableDiagnosticIds.Contains(x.Id)).ToList();
        if (fixableDiagnostics.Count == 0)
            return source;

        var root = await doc.GetSyntaxRootAsync() ?? throw new InvalidOperationException();
        foreach (var diagnostic in fixableDiagnostics)
        {
            var context = new CodeFixContext(
                doc,
                diagnostic,
                async (action, _) => root = await ApplyFix(root, action),
                CancellationToken.None
            );
            await fixer.RegisterCodeFixesAsync(context);
        }

        var fixedDocText = await doc.GetTextAsync();
        return fixedDocText.ToString();
    }

    private static async Task<SyntaxNode> ApplyFix(SyntaxNode root, CodeAction action)
    {
        var operations = await action.GetOperationsAsync(CancellationToken.None);
        var applyOperation = operations.OfType<ApplyChangesOperation>().FirstOrDefault();
        if (applyOperation == null)
            return root;

        var changedDoc = applyOperation.ChangedSolution.GetDocument(root.SyntaxTree);
        if (changedDoc == null)
            return root;

        return await changedDoc.GetSyntaxRootAsync(CancellationToken.None) ?? root;
    }

    private static async Task<Document> FixAll<T>(
        Document doc,
        T fixer,
        IReadOnlyCollection<Diagnostic> fixableDiagnostics,
        FixAllProvider fixAllProvider
    )
        where T : CodeFixProvider
    {
        // Mapperly fixers always use the diagnostic id
        // as equivalency key,
        // therefore we fix each diagnostic id one by one.
        var diagnosticId = fixableDiagnostics.Select(x => x.Id).Distinct().Single();
        var fixAllContext = new FixAllContext(
            doc,
            fixer,
            FixAllScope.Document,
            diagnosticId,
            [diagnosticId],
            new FakeDiagnosticProvider(fixableDiagnostics),
            CancellationToken.None
        );

        var fixAllAction = await fixAllProvider.GetFixAsync(fixAllContext);
        if (fixAllAction == null)
            return doc;

        var operations = await fixAllAction.GetOperationsAsync(CancellationToken.None);
        var solution = operations.OfType<ApplyChangesOperation>().First().ChangedSolution;
        var updatedDoc = solution.GetDocument(doc.Id) ?? doc;

        var updatedRoot = await updatedDoc.GetSyntaxRootAsync();
        var annotations = updatedRoot
            .DescendantNodesAndSelf()
            .SelectMany(n => n.GetAnnotations([Simplifier.Annotation.Kind!, Formatter.Annotation.Kind!]))
            .ToList();
        if (updatedRoot?.DescendantNodesAndSelf().Any(n => n.HasAnnotation(Simplifier.Annotation)) == true)
        {
            updatedDoc = await Simplifier.ReduceAsync(updatedDoc);
        }

        if (updatedRoot?.DescendantNodesAndSelf().Any(n => n.HasAnnotation(Formatter.Annotation)) == true)
        {
            updatedDoc = await Formatter.FormatAsync(updatedDoc);
        }

        return updatedDoc;
    }

    // TODO simplify?
    private static Document CreateDocument(string source)
    {
        var host = MefHostServices.Create(MefHostServices.DefaultAssemblies);
        var workspace = new AdhocWorkspace(host);
        var solution = workspace.CurrentSolution;
        var projectInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            "Tests",
            "Tests",
            LanguageNames.CSharp,
            compilationOptions: TestHelper.BuildCompilationOptions(NullableContextOptions.Enable),
            metadataReferences: TestHelper.BuildReferences()
        );
        solution = solution.AddProject(projectInfo);

        var docId = DocumentId.CreateNewId(projectInfo.Id);
        solution = solution.AddDocument(docId, "TestMapper.cs", source);
        workspace.TryApplyChanges(solution);
        return workspace.CurrentSolution.GetDocument(docId) ?? throw new InvalidOperationException("Could not create document");
    }

    private class FakeDiagnosticProvider(IEnumerable<Diagnostic> diagnostics) : FixAllContext.DiagnosticProvider
    {
        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken) =>
            Task.FromResult(diagnostics);

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            Task.FromResult(diagnostics);

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            Task.FromResult(diagnostics);
    }
}
