namespace Transplator.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using TokenTraits = PrivateTokenTraits;

    public class ParserTests
    {
        public record Token(TokenKind Kind, string Text);

        static readonly IEnumerable<TestCaseData> ParseTestData = new[]
        {
            new TestCaseData("", Array.Empty<Token>()),
            new TestCaseData("foo", new Token[]
            {
                new(TokenKind.Text, "foo"),
            }),
            new TestCaseData("{% foo %}", new Token[]
            {
                new(TokenKind.Code, "{% foo %}"),
            }),
            new TestCaseData("foo {% bar %}", new Token[]
            {
                new(TokenKind.Text, "foo "),
                new(TokenKind.Code, "{% bar %}"),
            }),
            new TestCaseData("{% foo %} bar", new Token[]
            {
                new(TokenKind.Code, "{% foo %}"),
                new(TokenKind.Text, " bar"),
            }),
            new TestCaseData("foo {% bar %} baz", new Token[]
            {
                new(TokenKind.Text, "foo "),
                new(TokenKind.Code, "{% bar %}"),
                new(TokenKind.Text, " baz"),
            }),
            new TestCaseData("%}", new Token[]
            {
                new(TokenKind.Text, "%}"),
            }),
            new TestCaseData("{", new Token[]
            {
                new(TokenKind.Text, "{"),
            }),
        };

        [TestCaseSource(nameof(ParseTestData))]
        public void Parse(string source, Token[] expectations)
        {
            var tokens =
                from t in Parser.Parse(source)
                select new Token(t.Kind, t.Substring(source));

            Assert.That(tokens, Is.EqualTo(expectations));
        }

        [TestCase(@"{% s %}", TokenTraits.Expression)]
        [TestCase(@"{%- s %}", TokenTraits.Expression | TokenTraits.TrimLeftGreedy)]
        [TestCase(@"{%~ s %}", TokenTraits.Expression | TokenTraits.TrimLeft)]
        [TestCase(@"{% s -%}", TokenTraits.Expression | TokenTraits.TrimRightGreedy)]
        [TestCase(@"{% s ~%}", TokenTraits.Expression | TokenTraits.TrimRight)]
        [TestCase(@"{%- s -%}", TokenTraits.Expression | TokenTraits.TrimLeftGreedy | TokenTraits.TrimRightGreedy)]
        [TestCase(@"{%~ s ~%}", TokenTraits.Expression | TokenTraits.TrimLeft | TokenTraits.TrimRight)]
        [TestCase(@"{%# comment %}", TokenTraits.Comment)]
        [TestCase(@"{%-# comment %}", TokenTraits.Comment | TokenTraits.TrimLeftGreedy)]
        [TestCase(@"{%~# comment %}", TokenTraits.Comment | TokenTraits.TrimLeft)]
        [TestCase(@"{%# comment -%}", TokenTraits.Comment | TokenTraits.TrimRightGreedy)]
        [TestCase(@"{%# comment ~%}", TokenTraits.Comment | TokenTraits.TrimRight)]
        [TestCase(@"{%-# comment -%}", TokenTraits.Comment | TokenTraits.TrimLeftGreedy | TokenTraits.TrimRightGreedy)]
        [TestCase(@"{%~# comment ~%}", TokenTraits.Comment | TokenTraits.TrimLeft | TokenTraits.TrimRight)]
        [TestCase(@"{% 123 %}", TokenTraits.Expression)]
        [TestCase(@"{% checked(x + y) %}", TokenTraits.Expression)]
        [TestCase(@"{% foo %}", TokenTraits.Expression)]
        [TestCase(@"{% 'foo' %}", TokenTraits.Expression)]
        [TestCase(@"{% ""foo"" %}", TokenTraits.Expression)]
        [TestCase(@"{% a + b %}", TokenTraits.Expression)]
        [TestCase(@"{% var a = 1; %}", TokenTraits.Block)]
        [TestCase(@"{% var a = 1 %}", TokenTraits.Block | PrivateTokenTraits.NeedsSemiColon)]
        [TestCase(@"{% {} %}", TokenTraits.Block)]
        [TestCase(@"{% { %}", TokenTraits.Block)]
        [TestCase(@"{% } %}", TokenTraits.Block)]
        [TestCase(@"{% if %}", TokenTraits.Expression)]
        [TestCase(@"{% throw new Exception() %}", TokenTraits.Block | TokenTraits.NeedsSemiColon)]
        [TestCase(@"{% goto foo %}", TokenTraits.Block | TokenTraits.NeedsSemiColon)]
        [TestCase(@"{% return 42 %}", TokenTraits.Block | TokenTraits.NeedsSemiColon)]
        [TestCase(@"{% lock (obj) obj.Foo() %}", TokenTraits.Block | TokenTraits.NeedsSemiColon)]
        [TestCase(@"{% if (true) Foo() %}", TokenTraits.Block | TokenTraits.NeedsSemiColon)]
        [TestCase(@"{% while (true) Foo() %}", TokenTraits.Block | TokenTraits.NeedsSemiColon)]
        [TestCase(@"{% if (true) { %}", TokenTraits.Block)]
        [TestCase(@"{% % %}", TokenTraits.Expression)]
        public void Traits(string source, TokenTraits expectedTraits)
        {
            Assert.That(Parser.Parse(source).Single().Traits,
                        Is.EqualTo(expectedTraits | TokenTraits.LeftSpace
                                                  | TokenTraits.RightSpace));

            foreach (var r in new[] { "\r", "\n" })
            {
                var s = Regex.Replace(source, @"(?<=^{%[-~]?#?) ", r);

                Assert.That(Parser.Parse(s).Single().Traits,
                            Is.EqualTo(expectedTraits | TokenTraits.RightSpace));

                s = Regex.Replace(source, @" (?=[-~]?%}$)", r);

                Assert.That(Parser.Parse(s).Single().Traits,
                            Is.EqualTo(expectedTraits | TokenTraits.LeftSpace));
            }
        }

        [TestCase("{%")]
        [TestCase("{%%}")]
        [TestCase("{% %}")]
        [TestCase("{%  %}")]
        [TestCase("{%.x.%}")]
        public void SyntaxError(string source)
        {
            void Act() => _ = Parser.Parse(source).ToList();
            Assert.Throws<SyntaxErrorException>(Act);
        }

        [TestCase("foo", "foo")]
        [TestCase("{% foo %}", "foo")]
        [TestCase("{%- foo %}", "foo")]
        [TestCase("{% foo -%}", "foo")]
        [TestCase("{%- foo -%}", "foo")]
        [TestCase("{%~ foo %}", "foo")]
        [TestCase("{% foo ~%}", "foo")]
        [TestCase("{%~ foo ~%}", "foo")]
        [TestCase("{%# foo %}", "foo")]
        [TestCase("{%-# foo %}", "foo")]
        [TestCase("{%# foo -%}", "foo")]
        [TestCase("{%-# foo -%}", "foo")]
        [TestCase("{%~# foo %}", "foo")]
        [TestCase("{%# foo ~%}", "foo")]
        [TestCase("{%~# foo ~%}", "foo")]
        [TestCase("{%\nfoo %}", "\nfoo")]
        [TestCase("{% foo\n%}", "foo\n")]
        [TestCase("{%\nfoo\n%}", "\nfoo\n")]
        public void InnerText(string source, string innerText)
        {
            var token = Parser.Parse(source).Single();
            Assert.That(token.InnerText(source), Is.EqualTo(innerText));
        }

        [TestCase("{% foo %}", "{% foo %}")]
        [TestCase("bar", "bar")]
        [TestCase("{% foo %} \t bar \t {% baz %}",
                  "{% foo %} \t bar \t {% baz %}")]
        [TestCase("{% foo -%}{% foo %} \t bar \t {% baz %}{%- baz %}",
                  "{% foo -%}{% foo %} \t bar \t {% baz %}{%- baz %}")]
        [TestCase("{% foo -%} \t bar \t {% baz %}",
                  "{% foo -%}bar \t {% baz %}")]
        [TestCase("{% foo %} \t bar \t {%- baz %}",
                  "{% foo %} \t bar{%- baz %}")]
        [TestCase("{% foo -%} \t bar \t {%- baz %}",
                  "{% foo -%}bar{%- baz %}")]
        [TestCase("{% foo ~%} \t \n bar \n \t {%~ baz %}",
                  "{% foo ~%} bar \n{%~ baz %}")]
        [TestCase("{% foo ~%} \t \r\n bar \r\n \t {%~ baz %}",
                  "{% foo ~%} bar \r\n{%~ baz %}")]
        [TestCase("{% foo ~%} \t \r bar \r \t {%~ baz %}",
                  "{% foo ~%} bar \r{%~ baz %}")]
        [TestCase("\r\n \t{%- foo -%}\r\n \t", "{%- foo -%}")]
        [TestCase("\t \r\n{%~ foo ~%}\r\n \t", "\t \r\n{%~ foo ~%}")]
        public void Trimming(string source, string expected)
        {
            var parts =
                from e in Parser.Parse(source).Trim(source)
                select e.Token.Kind == TokenKind.Text
                     ? source[e.Start..e.End]
                     : e.Token.Substring(source);
            Assert.That(string.Join(string.Empty, parts), Is.EqualTo(expected));
        }
    }
}
