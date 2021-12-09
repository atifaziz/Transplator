#region Copyright (c) 2021 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace Transplator;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using TokenTraits = PrivateTokenTraits;

[Generator]
public sealed class Generator : ISourceGenerator
{
    static readonly DiagnosticDescriptor SyntaxError =
        new(id: "TPR001",
            title: "Syntax error",
            messageFormat: "Syntax error: {0}",
            category: "Transplator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public void Initialize(GeneratorInitializationContext context) {}

    static class Metadata
    {
        const string Prefix = "build_metadata.AdditionalFiles.";
        public const string SourceItemType = Prefix + nameof(SourceItemType);
        public const string Name = Prefix + nameof(Name);
    }

    public void Execute(GeneratorExecutionContext context)
    {
        context.LaunchDebuggerIfFlagged(nameof(Transplator));

        var templates =
            from at in context.AdditionalFiles
            select context.AnalyzerConfigOptions.GetOptions(at) is {} options
                && options.TryGetValue(Metadata.SourceItemType, out var type)
                && "Transplate".Equals(type, StringComparison.OrdinalIgnoreCase)
                && at.GetText() is {} text
                 ? new
                   {
                       FilePath = at.Path,
                       Name = options.TryGetValue(Metadata.Name, out var name)
                              && !string.IsNullOrWhiteSpace(name)
                            ? name
                            : Path.GetFileNameWithoutExtension(at.Path),
                       Text = text,

                   }
                 : null
            into t
            where t is not null
            select t;

        foreach (var t in templates)
        {
            try
            {
                if (Generate(t.Name, t.Text) is { Length: > 0 } source)
                    context.AddSource(Path.GetFileNameWithoutExtension(t.FilePath) + ".cs", source);
            }
            catch (SyntaxErrorException e)
            {
                var args = Regex.Replace(e.Message, @"\r?\n", @"\n");
                context.ReportDiagnostic(Diagnostic.Create(SyntaxError, Location.None, args));
            }
        }
    }

    static readonly SourceText EmptySourceText = SourceText.From(string.Empty);
    static readonly Encoding Utf8BomlessEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static SourceText Generate(string name, SourceText text) =>
        Generate(name, text, null);

    public static SourceText Generate(string name, SourceText text, Encoding? outputEncoding)
    {
        if (text.Length == 0)
            return EmptySourceText;

        var source = text.ToString();

        var tokens =
            from tt in Parser.Parse(source).Trim(source)
            where tt.Token.Kind != TokenKind.Comment
            select tt;

        var tokenList = tokens.ToList();

        var sb = new StringBuilder();

        var isBare = tokenList[0].Token.Kind == TokenKind.Text;
        if (isBare)
        {
            sb.Append(@"using System;

partial class ").Append(name).AppendLine(@"Template
{
void RenderCore()
{");
        }

        foreach (var (token, start, end) in tokenList)
        {
            var traits = token.Traits;

            if ((traits & TokenTraits.Block) == TokenTraits.Block)
            {
                sb.Append(token.InnerText(source));
                if ((traits & TokenTraits.NeedsSemiColon) == TokenTraits.NeedsSemiColon)
                    sb.Append(';').AppendLine();
            }
            else
            {
                if (token.Kind == TokenKind.Text)
                {
                    if (end - start is { } length && length > 0)
                    {
                        sb.Append("WriteText(")
                          .Append("@\"")
                          .Append(source.Substring(start, length).Replace("\"", "\"\""))
                          .Append('\"')
                          .AppendLine(");");
                    }
                }
                else
                {
                    sb.Append("WriteValue(")
                      .Append(token.InnerText(source))
                      .AppendLine(");");
                }
            }
        }

        if (isBare)
            sb.AppendLine(@"    }").Append('}').AppendLine();

        return new StringBuilderSourceText(sb, outputEncoding ?? text.Encoding ?? Utf8BomlessEncoding);
    }
}
