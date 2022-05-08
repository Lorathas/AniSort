using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mergealot;

[Generator]
public class MergeGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource("MergableAttribute.g.cs", SourceText.From(StaticClassHelpers.MarkerAttribute, Encoding.UTF8)));
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource("MergableKeyAttribute.g.cs", SourceText.From(StaticClassHelpers.KeyAttribute, Encoding.UTF8)));
    }

    private bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        if (node is RecordDeclarationSyntax { HasLeadingTrivia: true, AttributeLists.Count: > 0 } rNode)
        {
            foreach (var trivia in rNode.GetLeadingTrivia())
            {
                // trivia.Token.ValueText
            }
        }

        return false;
    }
}
