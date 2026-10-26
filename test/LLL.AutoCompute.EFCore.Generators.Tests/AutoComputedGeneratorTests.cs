using System.Reflection;
using System.Runtime.Loader;
using LLL.AutoCompute.EFCore.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace LLL.AutoCompute.EFCore.Generators.Tests;

public class AutoComputedGeneratorTests
{
    private const string Attributes = """
        namespace LLL.AutoCompute.EFCore
        {
            [System.AttributeUsage(System.AttributeTargets.Property)]
            public sealed class AutoComputedAttribute : System.Attribute { }
        }
        namespace LLL.AutoCompute.EFCore.Infrastructure
        {
            public sealed class AutoComputedExpressionAttribute : System.Attribute
            {
                public AutoComputedExpressionAttribute(string property, string field) { }
            }
        }
        """;

    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator).Select(p => MetadataReference.CreateFromFile(p)).ToArray();

    private static (GeneratorDriverRunResult Result, Compilation Compilation) Generate(string source)
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12);
        var compilation = CSharpCompilation.Create("GeneratorTest_" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(Attributes, options), CSharpSyntaxTree.ParseText(source, options)],
            References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new AutoComputedGenerator().AsSourceGenerator()], parseOptions: options);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var generated, out _);
        return (driver.GetRunResult(), generated);
    }

    [Theory]
    [InlineData("public class Entity { public int Value { get; set; } [AutoComputed] public int Total => Value * 2; }", "AC0002")]
    [InlineData("public partial class Entity<T> { [AutoComputed] public int Total => 1; }", "AC0002")]
    [InlineData("public partial class Entity { [AutoComputed] public int Total { get; set; } }", "AC0001")]
    [InlineData("public partial class Entity { [AutoComputed] public static int Total => 1; }", "AC0001")]
    [InlineData("public partial class Entity { [AutoComputed] public int this[int i] => i; }", "AC0001")]
    [InlineData("public partial class Entity { [AutoComputed] public int Total { get { var n = 1; return n; } } }", "AC0001")]
    [InlineData("public partial class Entity { public string? Value { get; set; } [AutoComputed] public int Total => Value?.Length ?? 0; }", "AC0003")]
    [InlineData("public partial class Entity { public int Value { get; set; } [AutoComputed] public int Total => Value switch { 0 => 1, _ => 2 }; }", "AC0003")]
    [InlineData("public partial class Entity { private int __autoCompute_Total; [AutoComputed] public int Total => 1; }", "AC0001")]
    [InlineData("public class Container { public partial class Entity { [AutoComputed] public int Total => 1; } }", "AC0002")]
    [InlineData("public class Base { public int Total => 1; } public partial class Entity : Base { [AutoComputed] public new int Total => 2; }", "AC0001")]
    public void ReportsInvalidDeclarationsAndExpressions(string declaration, string diagnosticId)
    {
        var (result, _) = Generate("using LLL.AutoCompute.EFCore; " + declaration);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(diagnosticId, diagnostic.Id);
        Assert.Empty(result.GeneratedTrees);
        Assert.Null(result.Results.Single().Exception);
    }

    [Theory]
    [InlineData("Value * 2", 6)]
    [InlineData("this.Value + 4", 7)]
    [InlineData("Math.Max(Value, 8)", 8)]
    [InlineData("Max(Value, 9)", 9)]
    [InlineData("Pets.Count(p => p == PetType.Cat)", 2)]
    [InlineData("Pets.Where(p => p == PetType.Cat).Count()", 2)]
    [InlineData("Pets.Select(p => new { Kind = p }).Count(p => p.Kind == PetType.Cat)", 2)]
    [InlineData("Pets.Count(__autoComputeEntity => __autoComputeEntity == PetType.Cat)", 2)]
    [InlineData("Pets.Count(__autoComputeEntity => true)", 3)]
    [InlineData("Alias.Count(Pets)", 3)]
    [InlineData("Value + Offset", 5)]
    [InlineData("nameof(Value).Length", 5)]
    public void GeneratedExpressionMatchesGetter(string expression, int expected)
    {
        var (result, compilation) = Generate("""
            using System;
            using System.Linq;
            using static System.Math;
            using Alias = System.Linq.Enumerable;
            using LLL.AutoCompute.EFCore;
            namespace Demo;
            public enum PetType { Cat, Dog }
            public class BaseEntity { public int Value { get; set; } = 3; }
            public partial class Container
            {
                public partial class Entity : BaseEntity
                {
                    private const int Offset = 2;
                    public PetType[] Pets { get; set; } = [PetType.Cat, PetType.Dog, PetType.Cat];
                    [AutoComputed] public int Total => EXPRESSION;
                }
            }
            """.Replace("EXPRESSION", expression));
        Assert.Empty(result.Diagnostics);
        Assert.Single(result.GeneratedTrees);
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        using var stream = new MemoryStream();
        Assert.True(compilation.Emit(stream).Success);
        stream.Position = 0;
        var loadContext = new AssemblyLoadContext(compilation.AssemblyName, isCollectible: true);
        try
        {
            var assembly = loadContext.LoadFromStream(stream);
            var type = assembly.GetType("Demo.Container+Entity")!;
            var entity = Activator.CreateInstance(type)!;
            var getter = type.GetProperty("Total")!.GetValue(entity);
            var factory = type.GetMethod("__AutoComputeExpression_Total", BindingFlags.NonPublic | BindingFlags.Static)!;
            var lambda = (System.Linq.Expressions.LambdaExpression)factory.Invoke(null, null)!;
            Assert.Equal(expected, getter);
            Assert.Equal(getter, lambda.Compile().DynamicInvoke(entity));
            Assert.NotNull(type.GetField("__autoCompute_Total", BindingFlags.NonPublic | BindingFlags.Instance));
        }
        finally
        {
            loadContext.Unload();
        }
    }

    [Fact]
    public void SupportsGettersWithSingleReturnAndEscapedNames()
    {
        var (result, compilation) = Generate("""
            using LLL.AutoCompute.EFCore;
            public partial class @class
            {
                public int @int { get; set; }
                [AutoComputed] public int @event { get { return @int * 2; } }
                [AutoComputed] public int Other { get => this.@int + 1; }
            }
            """);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(2, result.GeneratedTrees.Length);
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void PreservesNullableReferenceTypesWithoutWarnings()
    {
        var (result, compilation) = Generate("""
            using LLL.AutoCompute.EFCore;
            public partial class Entity
            {
                public string? Value { get; set; }
                [AutoComputed] public string? Copy => Value;
            }
            """);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning));
    }
}
