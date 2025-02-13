using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;

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

        var actions = new List<CodeAction>();
        var fixer = new T();

        var fixableDiagnostics = diagnostics.Where(x => fixer.FixableDiagnosticIds.Contains(x.Id)).ToList();
        await RegisterCodeFixes(fixer, fixableDiagnostics, doc, actions);

        var fixAllProvider = fixer.GetFixAllProvider();

        // for now we only support code fixers with fix all providders.
        if (fixAllProvider == null)
            return source;

        if (fixableDiagnostics.Count == 0)
            return source;

        foreach (var diagnosticGroup in fixableDiagnostics.GroupBy(x => x.Id))
        {
            doc = await FixAll(doc, fixer, diagnosticGroup.ToList(), fixAllProvider);
        }

        var fixedDocText = await doc.GetTextAsync();
        return fixedDocText.ToString();
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
        return solution.GetDocument(doc.Id) ?? doc;
    }

    private static async Task RegisterCodeFixes(
        CodeFixProvider fixer,
        IEnumerable<Diagnostic> diagnostics,
        Document document,
        List<CodeAction> actions
    )
    {
        foreach (var diagnostic in diagnostics)
        {
            var ctx = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
            await fixer.RegisterCodeFixesAsync(ctx);
        }
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
