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
using System.Collections.Generic;
using TokenTraits = PrivateTokenTraits;

public static class Parser
{
    public static IEnumerable<Token> Parse(string source)
    {
        return Parse(source, (i, length) => new Token(TokenTraits.None, i, length),
                     (traits, i, length) => new Token(traits, i, length));
    }

    static IEnumerable<T> Parse<T>(string source,
                                   Func<int, int, T> textSelector,
                                   Func<TokenTraits, int, int, T> codeSelector)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (textSelector is null) throw new ArgumentNullException(nameof(textSelector));
        if (codeSelector is null) throw new ArgumentNullException(nameof(codeSelector));

        return _(); IEnumerable<T> _()
        {
            var si = 0;
            while (true)
            {
                var psi = source.IndexOf("{%", si, StringComparison.Ordinal);
                if (psi < 0)
                    break;

                var textLength = psi - si;
                if (textLength > 0)
                    yield return textSelector(si, textLength);

                var pei = source.IndexOf("%}", psi, StringComparison.Ordinal) is {} i and >= 0
                        ? i + 2
                        : throw new SyntaxErrorException($@"Invalid template syntax. Token starting at offset {psi} is not closed.");

                yield return codeSelector(GetCodeTraits(source, psi, pei - psi), psi, pei - psi);
                si = pei;
            }

            if (si < source.Length)
                yield return textSelector(si, source.Length - si);
        }
    }

    static TokenTraits GetCodeTraits(string s, int index, int length)
    {
        var traits = TokenTraits.None;

        SyntaxErrorException SyntaxError() =>
            new(index, $"Syntax error in token starting at offset {index}.");

        if (length < 6) // at least: "{% " + something + " %}"
            throw SyntaxError();

        var si = index + 2;
        var ei = index + length - 2;

        var ltt = s[si] switch
        {
            '-' => TokenTraits.TrimLeftGreedy,
            '~' => TokenTraits.TrimLeft,
            _   => TokenTraits.None
        };

        si += ltt != TokenTraits.None ? 1 : 0;
        if (si == ei)
            throw SyntaxError();
        traits |= ltt;

        var rtt = s[ei - 1] switch
        {
            '-' => TokenTraits.TrimRightGreedy,
            '~' => TokenTraits.TrimRight,
            _   => TokenTraits.None
        };

        ei -= rtt != TokenTraits.None ? 1 : 0;
        if (si == ei)
            throw SyntaxError();
        traits |= rtt;

        var comment = false;
        if (s[si] == '#')
        {
            si++;
            comment = true;
            traits |= TokenTraits.Comment;
        }

        if (si == ei || s[si] is not ' ' and not '\n' and not '\r'
                     || s[ei - 1] is not ' ' and not '\n' and not '\r')
            throw SyntaxError();

        if (s[si] == ' ')
            traits |= TokenTraits.LeftSpace;

        if (s[ei - 1] == ' ')
            traits |= TokenTraits.RightSpace;

        while (si < ei && char.IsWhiteSpace(s, si))
            si++;

        while (ei > si && char.IsWhiteSpace(s, ei - 1))
            ei--;

        if (si == ei)
            throw SyntaxError();

        if (comment)
            return traits;

        var fch = s[si];
        var lch = s[ei - 1];
        if (lch is ';' or '}' or '{' || fch == '{')
        {
            traits |= TokenTraits.Block;
        }
        else if (fch is '@' or '"' or '\'' or '(' or '+' or '-' or '*' or > '0' and < '9')
        {
            traits |= TokenTraits.Expression;
        }
        else
        {
            var i = si;
            while (i < ei && s[i] is >= 'a' and <= 'z')
                i++;
            var len = i - si;

            bool IsWord(string word, out string? match)
            {
                if (!Equal(word, s, si, len))
                {
                    match = null;
                    return false;
                }

                match = word;
                return true;
            }

            if (i < ei
                && s[i] != '_'
                && !char.IsLetterOrDigit(s, i)
                && (IsWord("if"     , out var word) ||
                    IsWord("for"    , out word) ||
                    IsWord("foreach", out word) ||
                    IsWord("while"  , out word) ||
                    IsWord("do"     , out word) ||
                    IsWord("using"  , out word) ||
                    IsWord("try"    , out word) ||
                    IsWord("goto"   , out word) ||
                    IsWord("lock"   , out word) ||
                    IsWord("return" , out word) ||
                    IsWord("throw"  , out word) ||
                    IsWord("var"    , out word)))
            {
                traits |= TokenTraits.Block;
                traits |= (word, lch) switch
                {
                    ("goto" or "return" or "lock" or "throw" or "var", not ';') =>
                        TokenTraits.NeedsSemiColon,
                    ("if" or "while", not '}') =>
                        TokenTraits.NeedsSemiColon,
                    _ =>
                        TokenTraits.None
                };
            }
            else
            {
                traits |= TokenTraits.Expression;
            }
        }

        return traits;
    }

    static bool Equal(string a, string b, int bi, int length) =>
        length == a.Length && 0 == string.CompareOrdinal(a, 0, b, bi, length);
}
