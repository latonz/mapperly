using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Riok.Mapperly.Descriptors.Mappings.UserMappings;

/// <summary>
/// An inlined version of a <see cref="UserImplementedMethodMapping"/>.
/// Does not support reference handling and has several other limitations,
/// <see cref="InlineExpressionRewriter"/>.
/// </summary>
/// <param name="userMapping">The original user mapping.</param>
/// <param name="sourceParameter">The source parameter of the user mapping. This will probably be rewritten when inlining.</param>
/// <param name="mappingInvocations">Mapping invocations to be inlined.</param>
/// <param name="mappingBody">The prepared user written mapping body code (rewritten by <see cref="InlineExpressionRewriter"/>.</param>
public class UserImplementedInlinedExpressionMapping(
    UserImplementedMethodMapping userMapping,
    ParameterSyntax sourceParameter,
    IReadOnlyDictionary<SyntaxAnnotation, INewInstanceMapping> mappingInvocations,
    ExpressionSyntax mappingBody
) : NewInstanceMapping(userMapping.SourceType, userMapping.TargetType), INewInstanceUserMapping
{
    public IMethodSymbol Method => userMapping.Method;
    public bool? Default => userMapping.Default;
    public bool IsExternal => userMapping.IsExternal;

    public override ExpressionSyntax Build(TypeMappingBuildContext ctx)
    {
        var body = InlineUserMappings(ctx, mappingBody);
        body = RenameLambdaParameters(ctx, body);
        return ReplaceSource(ctx, body);
    }

    private ExpressionSyntax InlineUserMappings(TypeMappingBuildContext ctx, ExpressionSyntax body)
    {
        var invocations = body.GetAnnotatedNodes(InlineExpressionRewriter.SyntaxAnnotationKindMapperInvocation)
            .OfType<InvocationExpressionSyntax>();
        return body.ReplaceNodes(invocations, (invocation, _) => InlineMapping(ctx, invocation));
    }

    private ExpressionSyntax InlineMapping(TypeMappingBuildContext ctx, InvocationExpressionSyntax invocation)
    {
        var annotation = invocation.GetAnnotations(InlineExpressionRewriter.SyntaxAnnotationKindMapperInvocation).FirstOrDefault();
        if (!mappingInvocations.TryGetValue(annotation, out var mapping))
            return invocation;

        return mapping.Build(ctx.WithSource(invocation.ArgumentList.Arguments[0].Expression));
    }

    private ExpressionSyntax ReplaceSource(TypeMappingBuildContext ctx, ExpressionSyntax body)
    {
        // include self since the method could just be TTarget MyMapping(TSource source) => source;
        // do not further descend if the source parameter is hidden
        var identifierNodes = body.DescendantNodesAndSelf(n => !IsSourceParameterHidden(n))
            .OfType<IdentifierNameSyntax>()
            .Where(x => x.Identifier.Text.Equals(sourceParameter.Identifier.Text, StringComparison.Ordinal));
        return body.ReplaceNodes(identifierNodes, (n, _) => ctx.Source.WithTriviaFrom(n));
    }

    private ExpressionSyntax RenameLambdaParameters(TypeMappingBuildContext ctx, ExpressionSyntax body)
    {
        // Find all lambda expressions and rename their parameters to ensure uniqueness
        var lambdas = body.DescendantNodesAndSelf().OfType<LambdaExpressionSyntax>().ToList();
        if (lambdas.Count == 0)
            return body;

        // Build a mapping of old parameter names to new unique names
        var parameterRenamings = new Dictionary<LambdaExpressionSyntax, Dictionary<string, string>>();
        
        foreach (var lambda in lambdas)
        {
            var renamings = new Dictionary<string, string>();
            
            if (lambda is SimpleLambdaExpressionSyntax simpleLambda)
            {
                var oldName = simpleLambda.Parameter.Identifier.Text;
                var newName = ctx.NameBuilder.New(oldName);
                renamings[oldName] = newName;
            }
            else if (lambda is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
            {
                foreach (var parameter in parenthesizedLambda.ParameterList.Parameters)
                {
                    var oldName = parameter.Identifier.Text;
                    var newName = ctx.NameBuilder.New(oldName);
                    renamings[oldName] = newName;
                }
            }
            
            if (renamings.Count > 0)
            {
                parameterRenamings[lambda] = renamings;
            }
        }

        // Replace lambda expressions with renamed parameters
        body = body.ReplaceNodes(lambdas, (originalLambda, _) =>
        {
            if (!parameterRenamings.TryGetValue(originalLambda, out var renamings))
                return originalLambda;

            LambdaExpressionSyntax renamedLambda;
            
            if (originalLambda is SimpleLambdaExpressionSyntax simpleLambda)
            {
                var oldName = simpleLambda.Parameter.Identifier.Text;
                var newName = renamings[oldName];
                var newParameter = simpleLambda.Parameter.WithIdentifier(Identifier(newName));
                var newBody = RenameIdentifiersInLambdaBody(simpleLambda.Body, renamings);
                renamedLambda = simpleLambda.WithParameter(newParameter).WithBody(newBody);
            }
            else if (originalLambda is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
            {
                var newParameters = parenthesizedLambda.ParameterList.Parameters
                    .Select(p => p.WithIdentifier(Identifier(renamings[p.Identifier.Text])))
                    .ToArray();
                var newParameterList = parenthesizedLambda.ParameterList.WithParameters(SeparatedList(newParameters));
                var newBody = RenameIdentifiersInLambdaBody(parenthesizedLambda.Body, renamings);
                renamedLambda = parenthesizedLambda.WithParameterList(newParameterList).WithBody(newBody);
            }
            else
            {
                return originalLambda;
            }

            return renamedLambda;
        });

        return body;
    }

    private static CSharpSyntaxNode RenameIdentifiersInLambdaBody(CSharpSyntaxNode body, Dictionary<string, string> renamings)
    {
        // Find all identifier references in the lambda body and rename them
        var identifiers = body.DescendantNodesAndSelf()
            .OfType<IdentifierNameSyntax>()
            .Where(id => renamings.ContainsKey(id.Identifier.Text))
            .ToList();

        if (identifiers.Count == 0)
            return body;

        return body.ReplaceNodes(identifiers, (original, _) =>
            original.WithIdentifier(Identifier(renamings[original.Identifier.Text])));
    }

    private bool IsSourceParameterHidden(SyntaxNode node)
    {
        return ExtractOverwrittenIdentifiers(node).Any(x => x.Text.Equals(sourceParameter.Identifier.Text, StringComparison.Ordinal));
    }

    private IEnumerable<SyntaxToken> ExtractOverwrittenIdentifiers(SyntaxNode node)
    {
        return node switch
        {
            SimpleLambdaExpressionSyntax simpleLambda => [simpleLambda.Parameter.Identifier],
            ParenthesizedLambdaExpressionSyntax lambda => lambda.ParameterList.Parameters.Select(p => p.Identifier),
            _ => [],
        };
    }
}
