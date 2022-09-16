using System.Collections.Immutable;
using System.Text;
using AniSort.Server.Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AniSort.Server.Generators;

[Generator]
public class HubGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource("HubAttribute.g.cs", SourceText.From(SourceGenerationHelper.HubAttribute, Encoding.UTF8)));

        var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider<ClassDeclarationSyntax>(
                static (s, _) => IsSyntaxTargetForGeneration(s),
#pragma warning disable CS8603
                static (ctx, _) => GetSemanticTargetForGeneration(ctx))
#pragma warning restore CS8603
            .Where(static m => m != null);

        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndEnums = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndEnums, static (ctx, source) => Execute(source.Item1, source.Item2, ctx));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };
    }

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax) context.Node;

        foreach (var attributeSyntax in classDeclarationSyntax.AttributeLists.SelectMany(attributeSyntaxList => attributeSyntaxList.Attributes))
        {
            if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
            {
                continue;
            }

            var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
            string fullName = attributeContainingTypeSymbol.ToDisplayString();

            if (fullName == "AniSort.Server.Generators.HubAttribute")
            {
                return classDeclarationSyntax;
            }
        }

        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            // nothing to do yet
            return;
        }

        // I'm not sure if this is actually necessary, but `[LoggerMessage]` does it, so seems like a good idea!
        var distinctEnums = classes.Distinct();

        // Convert each EnumDeclarationSyntax to an EnumToGenerate
        var hubServicesToGenerate = GetTypesToGenerate(compilation, distinctEnums, context.CancellationToken);

        // If there were errors in the EnumDeclarationSyntax, we won't create an
        // EnumToGenerate for it, so make sure we have something to generate
        if (hubServicesToGenerate.Count <= 0) return;

        context.AddSource(
            "HubServiceRegistration.g.cs",
            SourceText.From(SourceGenerationHelper.GenerateHubServiceRegistrationClass(hubServicesToGenerate), Encoding.UTF8));

        foreach (var hubServiceToGenerate in hubServicesToGenerate)
        {
            // generate the source code and add it to the output
            string result = SourceGenerationHelper.GenerateHubServiceClass(hubServiceToGenerate);
            context.AddSource($"{hubServiceToGenerate.Namespace}.Services.{hubServiceToGenerate.Name}Service.g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }

    private static List<HubServiceToGenerate> GetTypesToGenerate(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classes, CancellationToken cancellationToken)
    {
        // Create a list to hold our output
        var classesToGenerate = new List<HubServiceToGenerate>();

        // Get the semantic representation of our marker attribute 
        var attribute = compilation.GetTypeByMetadataName("AniSort.Server.Generators.HubAttribute");

        if (attribute == null)
        {
            // If this is null, the compilation couldn't find the marker attribute type
            // which suggests there's something very wrong! Bail out..
            return classesToGenerate;
        }

        foreach (var classDeclarationSyntax in classes)
        {
            // stop if we're asked to
            cancellationToken.ThrowIfCancellationRequested();

            // Get the semantic representation of the enum syntax
            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
            {
                // something went wrong, bail out
                continue;
            }

            var namespaceSymbol = classSymbol.ContainingNamespace;

            var invertedNamespaces = new List<string>();

            var ns = namespaceSymbol;

            while (ns != null)
            {
                invertedNamespaces.Add(ns.Name);
                ns = ns.ContainingNamespace;
            }

            invertedNamespaces.Reverse();

            string fullNamespace = string.Join(".", namespaceSymbol.ToMinimalDisplayString(semanticModel, 0));

            classesToGenerate.Add(new HubServiceToGenerate(classSymbol.MetadataName, fullNamespace, $"I{classSymbol.MetadataName}"));
        }

        return classesToGenerate;
    }
}
