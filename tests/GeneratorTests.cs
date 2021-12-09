namespace Transplator.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;
using RsAnalyzerConfigOptions = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;

public class GeneratorTests
{
    [Test]
    public void Generate()
    {
        const string source = @"foo = {% bar %}";

        var output = Generator.Generate("Test", SourceText.From(source));

        var lines =
            from line in output.Lines
            select line.ToString();

        Assert.That(lines, Is.EqualTo(new[]
        {
            "using System;",
            string.Empty,
            "partial class TestTemplate",
            "{",
            "    void RenderCore()",
            "    {",
            "WriteText(@\"foo = \");",
            "WriteValue(bar);",
            "    }",
            "}",
            string.Empty
        }));
    }

    [Test]
    public void GenerateViaDriver()
    {
        const string source = @"foo = {% bar %}";

        var output = SourceText.From(GetGeneratedOutput(source));

        var lines =
            from line in output.Lines
            select line.ToString();

        Assert.That(lines, Is.EqualTo(new[]
        {
            "using System;",
            string.Empty,
            "partial class TestTemplate",
            "{",
            "    void RenderCore()",
            "    {",
            "WriteText(@\"foo = \");",
            "WriteValue(bar);",
            "    }",
            "}",
            string.Empty
        }));
    }

    static string GetGeneratedOutput(string source)
    {
        var references =
            from assembly in AppDomain.CurrentDomain.GetAssemblies()
            where !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location)
            select MetadataReference.CreateFromFile(assembly.Location);

        var compilation =
            CSharpCompilation.Create("generated.dll",
                                     syntaxTrees: null,
                                     references,
                                     new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ISourceGenerator generator = new Generator();

        AdditionalText additionalText = new AdditionalTextString("Test.txt", source);

        RsAnalyzerConfigOptions options =
            new AnalyzerConfigOptions(KeyValuePair.Create("build_metadata.AdditionalFiles.SourceItemType", "Transplate"));

        var driver =
            CSharpGeneratorDriver.Create(
                new[] { generator },
                new[] { additionalText },
                parseOptions: null,
                new AnalyzerConfigOptionsProvider(KeyValuePair.Create(additionalText, options)));

        driver.RunGeneratorsAndUpdateCompilation(compilation,
                                                 out var outputCompilation,
                                                 out var generateDiagnostics);

        Assert.False(generateDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error),
                     "Failed: " + generateDiagnostics.FirstOrDefault()?.GetMessage());

        return outputCompilation.SyntaxTrees.Last().ToString();
    }
}
