using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Riok.Mapperly.Emit.Syntax.SyntaxFactoryHelper;

namespace Riok.Mapperly.Descriptors.Mappings;

/// <summary>
/// A projections queryable mapping
/// to map from one generic <see cref="IQueryable{T}"/> to another.
/// </summary>
public class QueryableProjectionMapping(
    ITypeSymbol sourceType,
    ITypeSymbol targetType,
    INewInstanceMapping delegateMapping,
    bool supportsNullableAttributes
) : NewInstanceMethodMapping(sourceType, targetType)
{
    private const string QueryableReceiverName = "global::System.Linq.Queryable";
    private const string SelectMethodName = nameof(Queryable.Select);

    public override IEnumerable<StatementSyntax> BuildBody(TypeMappingBuildContext ctx)
    {
        // disable nullable reference types for expressions, as for ORMs nullables usually don't apply
        // #nullable disable
        // return System.Linq.Enumerable.Select(source, x => ...);
        // #nullable enable
        
        // Build the delegate mapping with a temporary context to extract lambda parameter names
        // This is needed to avoid conflicts when generating the queryable projection lambda parameter
        var (tempCtx, tempSourceName) = ctx.WithNewScopedSource("__temp");
        var tempDelegateSyntax = delegateMapping.Build(tempCtx);
        var lambdaParameterNames = ExtractLambdaParameterNames(tempDelegateSyntax);
        foreach (var name in lambdaParameterNames)
        {
            ctx.NameBuilder.Reserve(name);
        }
        
        var (lambdaCtx, lambdaSourceName) = ctx.WithNewScopedSource();

        var delegateMappingSyntax = delegateMapping.Build(lambdaCtx);
        var projectionLambda = Lambda(lambdaSourceName, delegateMappingSyntax);
        var select = ctx.SyntaxFactory.StaticInvocation(QueryableReceiverName, SelectMethodName, ctx.Source, projectionLambda);
        var returnStatement = ctx.SyntaxFactory.Return(select);
        var leadingTrivia = returnStatement.GetLeadingTrivia().Insert(0, ElasticCarriageReturnLineFeed).Insert(1, Nullable(false));
        var trailingTrivia = returnStatement
            .GetTrailingTrivia()
            .Insert(0, ElasticCarriageReturnLineFeed)
            .Insert(1, Nullable(true, !supportsNullableAttributes));
        returnStatement = returnStatement.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia);
        return [returnStatement];
    }

    private static IEnumerable<string> ExtractLambdaParameterNames(ExpressionSyntax expression)
    {
        var lambdas = expression.DescendantNodesAndSelf().OfType<LambdaExpressionSyntax>();
        var names = new HashSet<string>();
        
        foreach (var lambda in lambdas)
        {
            if (lambda is SimpleLambdaExpressionSyntax simpleLambda)
            {
                names.Add(simpleLambda.Parameter.Identifier.Text);
            }
            else if (lambda is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
            {
                foreach (var parameter in parenthesizedLambda.ParameterList.Parameters)
                {
                    names.Add(parameter.Identifier.Text);
                }
            }
        }
        
        return names;
    }
}
