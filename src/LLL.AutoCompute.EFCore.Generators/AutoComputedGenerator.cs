using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LLL.AutoCompute.EFCore.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class AutoComputedGenerator : IIncrementalGenerator
{
    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static readonly DiagnosticDescriptor InvalidDeclaration = new(
        "AC0001", "Unsupported AutoComputed property", "Property '{0}': {1}",
        "AutoCompute", DiagnosticSeverity.Error, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor PartialRequired = new(
        "AC0002", "AutoComputed requires partial classes",
        "Property '{0}' requires its declaring class and all containing classes to be partial and non-generic",
        "AutoCompute", DiagnosticSeverity.Error, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidExpression = new(
        "AC0003", "Unsupported AutoComputed expression",
        "Property '{0}' cannot be represented as an expression tree: {1}",
        "AutoCompute", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var properties = context.SyntaxProvider.ForAttributeWithMetadataName(
            "LLL.AutoCompute.EFCore.AutoComputedAttribute",
            static (_, _) => true,
            static (attributeContext, _) => attributeContext);

        context.RegisterSourceOutput(properties, static (output, property) => Generate(output, property));
    }

    private static void Generate(SourceProductionContext output, GeneratorAttributeSyntaxContext context)
    {
        var symbol = context.TargetSymbol as IPropertySymbol;
        var declaration = context.TargetNode as PropertyDeclarationSyntax;
        var location = context.TargetNode.GetLocation();
        var name = context.TargetSymbol.Name;
        if (symbol is null || declaration is null || symbol.IsStatic || symbol.IsIndexer
            || symbol.SetMethod is not null || symbol.IsAbstract || symbol.IsOverride
            || symbol.ReturnsByRef || symbol.ReturnsByRefReadonly
            || symbol.ExplicitInterfaceImplementations.Length != 0)
        {
            output.ReportDiagnostic(Diagnostic.Create(InvalidDeclaration, location, name,
                "only concrete, non-overridden, getter-only instance properties are supported"));
            return;
        }

        var containers = declaration.Ancestors().OfType<TypeDeclarationSyntax>().Reverse().ToArray();
        if (containers.Length == 0 || containers.Any(t => t is not ClassDeclarationSyntax
                || t.TypeParameterList is not null || !t.Modifiers.Any(SyntaxKind.PartialKeyword)))
        {
            output.ReportDiagnostic(Diagnostic.Create(PartialRequired, location, name));
            return;
        }

        var getter = declaration.AccessorList?.Accessors.SingleOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
        var expression = declaration.ExpressionBody?.Expression ?? getter?.ExpressionBody?.Expression;
        if (expression is null && getter?.Body?.Statements.Count == 1
            && getter.Body.Statements[0] is ReturnStatementSyntax returnStatement)
            expression = returnStatement.Expression;

        if (expression is null || expression.DescendantNodesAndSelf().OfType<BaseExpressionSyntax>().Any())
        {
            output.ReportDiagnostic(Diagnostic.Create(InvalidDeclaration, location, name,
                "use an expression-bodied getter or a single return statement; base accesses are not supported"));
            return;
        }

        for (var baseType = symbol.ContainingType.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.GetMembers(name).OfType<IPropertySymbol>().Any())
            {
                output.ReportDiagnostic(Diagnostic.Create(InvalidDeclaration, location, name,
                    "properties that hide inherited properties are not supported"));
                return;
            }
        }

        var fieldName = "__autoCompute_" + name;
        var factoryName = "__AutoComputeExpression_" + name;
        if (symbol.ContainingType.GetMembers(fieldName).Any() || symbol.ContainingType.GetMembers(factoryName).Any())
        {
            output.ReportDiagnostic(Diagnostic.Create(InvalidDeclaration, location, name,
                "the generated storage or expression factory name conflicts with an existing member"));
            return;
        }

        var parameterName = "__autoComputeEntity";
        var identifiers = expression.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken))
            .Select(t => t.ValueText).ToArray();
        while (identifiers.Contains(parameterName))
            parameterName += "_";

        var rewritten = new GetterRewriter(context.SemanticModel, symbol.ContainingType, parameterName)
            .Visit(expression)!.WithoutTrivia().NormalizeWhitespace().ToFullString();
        var source = new StringBuilder("// <auto-generated/>\n#nullable enable\n");
        if (!symbol.ContainingNamespace.IsGlobalNamespace)
            source.Append("namespace ").Append(symbol.ContainingNamespace.ToDisplayString()).Append(" {\n");
        foreach (var container in containers)
            source.Append(container.Modifiers.Any(SyntaxKind.StaticKeyword) ? "static partial class " : "partial class ")
                .Append(container.Identifier.Text).Append(" {\n");

        var propertyType = symbol.Type.ToDisplayString(TypeFormat);
        var entityType = symbol.ContainingType.ToDisplayString(TypeFormat);
        source.Append("#pragma warning disable CS0169, CS0414, CS0649\n")
            .Append("private ").Append(propertyType).Append(' ').Append(fieldName).Append(" = default!;\n")
            .Append("#pragma warning restore CS0169, CS0414, CS0649\n")
            .Append("[global::LLL.AutoCompute.EFCore.Infrastructure.AutoComputedExpressionAttribute(\"")
            .Append(name).Append("\", \"").Append(fieldName).Append("\")]\n")
            .Append("private static global::System.Linq.Expressions.Expression<global::System.Func<")
            .Append(entityType).Append(", ").Append(propertyType).Append(">> ")
            .Append(factoryName).Append("() => ").Append(parameterName).Append(" => ")
            .Append(rewritten).Append(";\n");

        foreach (var _ in containers)
            source.Append("}\n");
        if (!symbol.ContainingNamespace.IsGlobalNamespace)
            source.Append("}\n");

        var hintName = (symbol.ContainingNamespace.IsGlobalNamespace ? "" : symbol.ContainingNamespace.ToDisplayString() + ".")
            + string.Join(".", containers.Select(c => c.Identifier.ValueText).Concat(new[] { name })) + ".g.cs";
        var tree = CSharpSyntaxTree.ParseText(source.ToString(), (CSharpParseOptions)declaration.SyntaxTree.Options,
            path: hintName, encoding: Encoding.UTF8);
        var validation = context.SemanticModel.Compilation.AddSyntaxTrees(tree).GetSemanticModel(tree);
        var errors = validation.GetDiagnostics(cancellationToken: output.CancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length != 0)
        {
            output.ReportDiagnostic(Diagnostic.Create(InvalidExpression, location, name,
                string.Join("; ", errors.Select(e => e.GetMessage()).Distinct())));
            return;
        }

        output.AddSource(hintName, SourceText.From(source.ToString(), Encoding.UTF8));
    }

    private sealed class GetterRewriter(SemanticModel model, INamedTypeSymbol entityType, string parameterName)
        : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitThisExpression(ThisExpressionSyntax node)
            => SyntaxFactory.IdentifierName(parameterName).WithTriviaFrom(node);

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var method = model.GetSymbolInfo(node).Symbol as IMethodSymbol;
            if (method?.ReducedFrom is not null && node.Expression is MemberAccessExpressionSyntax access)
            {
                var arguments = node.ArgumentList.Arguments.Select(a => (ArgumentSyntax)Visit(a)!).ToList();
                arguments.Insert(0, SyntaxFactory.Argument((ExpressionSyntax)Visit(access.Expression)!));
                var methodName = method.TypeArguments.Length == 0 || method.TypeArguments.Any(t => t.IsAnonymousType)
                    ? Escape(method.Name)
                    : Escape(method.Name) + "<" + string.Join(", ", method.TypeArguments.Select(TypeName)) + ">";
                return SyntaxFactory.InvocationExpression(
                    SyntaxFactory.ParseExpression(TypeName(method.ReducedFrom.ContainingType) + "." + methodName),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))).WithTriviaFrom(node);
            }
            return base.VisitInvocationExpression(node);
        }

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (model.GetSymbolInfo(node).Symbol is INamedTypeSymbol type)
                return SyntaxFactory.ParseExpression(TypeName(type)).WithTriviaFrom(node);
            return base.VisitMemberAccessExpression(node);
        }

        public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
            => model.GetSymbolInfo(node).Symbol is INamedTypeSymbol type
                ? SyntaxFactory.ParseName(TypeName(type)).WithTriviaFrom(node)
                : base.VisitQualifiedName(node);

        public override SyntaxNode? VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
            => model.GetSymbolInfo(node).Symbol is INamedTypeSymbol type
                ? SyntaxFactory.ParseName(TypeName(type)).WithTriviaFrom(node)
                : base.VisitAliasQualifiedName(node);

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node) => VisitName(node);
        public override SyntaxNode? VisitGenericName(GenericNameSyntax node) => VisitName(node);

        private SyntaxNode? VisitName(SimpleNameSyntax node)
        {
            if (node.Parent is MemberAccessExpressionSyntax access && access.Name == node
                || node.Parent is MemberBindingExpressionSyntax
                || node.Parent is NameColonSyntax || node.Parent is NameEqualsSyntax
                || node.Parent is AssignmentExpressionSyntax assignment && assignment.Left == node
                    && assignment.Parent is InitializerExpressionSyntax)
                return node is GenericNameSyntax memberName
                    ? base.VisitGenericName(memberName) : base.VisitIdentifierName((IdentifierNameSyntax)node);

            var symbol = model.GetSymbolInfo(node).Symbol;
            if (symbol is ITypeSymbol type)
                return SyntaxFactory.ParseTypeName(TypeName(type)).WithTriviaFrom(node);

            if (symbol is IPropertySymbol || symbol is IFieldSymbol || symbol is IMethodSymbol)
            {
                var visited = node is GenericNameSyntax generic
                    ? (SimpleNameSyntax)base.VisitGenericName(generic)!
                    : node;
                if (symbol.IsStatic)
                    return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ParseExpression(TypeName(symbol.ContainingType)), visited.WithoutTrivia())
                        .WithTriviaFrom(node);

                for (var current = entityType; current is not null; current = current.BaseType)
                {
                    if (SymbolEqualityComparer.Default.Equals(current, symbol.ContainingType))
                        return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(parameterName), visited.WithoutTrivia()).WithTriviaFrom(node);
                }
            }
            return node is GenericNameSyntax genericName ? base.VisitGenericName(genericName) : node;
        }

        private static string TypeName(ITypeSymbol type) => type.ToDisplayString(TypeFormat);
        private static string Escape(string name) => "@" + name;
    }
}
