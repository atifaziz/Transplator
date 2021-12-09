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
using System.Diagnostics;
using TokenTraits = PrivateTokenTraits;

public enum TokenKind { Text, Code, Comment }

/// <remarks>
/// This type is reserved for internal use.
/// </remarks>

[Flags]
public enum PrivateTokenTraits
{
    None,
    TrimLeft        = 1 << 2,
    TrimRight       = 1 << 3,
    TrimLeftGreedy  = 1 << 4,
    TrimRightGreedy = 1 << 5,
    LeftSpace       = 1 << 6,
    RightSpace      = 1 << 7,
    Expression      = 1 << 1,
    Block           = 1 << 8,
    Comment         = 1 << 9,
    NeedsSemiColon  = 1 << 10,
}

[DebuggerDisplay(nameof(DebuggerDisplayString) + "(),nq")]
public readonly struct Token : IEquatable<Token>
{
    internal Token(TokenTraits traits, int start, int length)
    {
        Traits = traits;
        Start = start;
        Length = length;
    }

    internal TokenTraits Traits { get; }

    public TokenKind Kind => Traits == TokenTraits.None ? TokenKind.Text
                           : HasTraits(TokenTraits.Comment) ? TokenKind.Comment
                           : TokenKind.Code;

    public int Start { get; }
    public int End => Start + Length;
    public int Length { get; }

    internal bool HasTraits(TokenTraits traits) => (Traits & traits) == traits;

    public override bool Equals(object? obj) => obj is Token other && Equals(other);
    public bool Equals(Token other) => Traits == other.Traits && Start == other.Start && Length == other.Length;

    public override int GetHashCode() => unchecked(((int)Traits * 397) ^ Start * 397 ^ Length);

    public static bool operator ==(Token left, Token right) => left.Equals(right);
    public static bool operator !=(Token left, Token right) => !left.Equals(right);

    public override string ToString() => $"{Kind} [{Start}..{End}) {{ {Traits} }}";

    string DebuggerDisplayString() => ToString();
}

public static class TokenExtensions
{
    public static string Substring(this Token token, string s)
    {
        if (s is null) throw new ArgumentNullException(nameof(s));
        return s.Substring(token.Start, token.Length);
    }

    public static string InnerText(this Token token, string s)
    {
        if (s is null) throw new ArgumentNullException(nameof(s));

        var (start, end) = token.TextExtents();
        return s.Substring(start, end - start);
    }

    public static (int Start, int End) TextExtents(this Token token)
    {
        if (token.Kind == TokenKind.Text)
            return (token.Start, token.End);

        var start = token.Start + 2;

        if (token.HasTraits(TokenTraits.TrimLeft) || token.HasTraits(TokenTraits.TrimLeftGreedy))
            start++;
        if (token.HasTraits(TokenTraits.LeftSpace))
            start++;
        if (token.HasTraits(TokenTraits.Comment))
            start++;

        var end = token.End - 2;

        if (token.HasTraits(TokenTraits.TrimRight) || token.HasTraits(TokenTraits.TrimRightGreedy))
            end--;
        if (token.HasTraits(TokenTraits.RightSpace))
            end--;

        return (start, end);
    }

    public static IEnumerable<(Token Token, int Start, int End)>
        Trim(this IEnumerable<Token> tokens, string source)
    {
        if (tokens is null) throw new ArgumentNullException(nameof(tokens));
        if (source is null) throw new ArgumentNullException(nameof(source));

        return _(); IEnumerable<(Token Token, int Index, int Length)> _()
        {
            using var token = tokens.GetEnumerator();
            var lt = TokenTraits.None;
            Token? textToken = null;

            while (token.MoveNext())
            {
                if (token.Current.Kind == TokenKind.Text)
                {
                    textToken = token.Current;
                }
                else if (textToken is {} tt)
                {
                    yield return Trim(tt, lt, token.Current.Traits);
                    textToken = null;

                    var (start, end) = token.Current.TextExtents();
                    yield return (token.Current, start, end);

                    lt = token.Current.Traits;
                }
                else
                {
                    var (start, end) = token.Current.TextExtents();
                    yield return (token.Current, start, end);
                    lt = token.Current.Traits;
                }
            }

            if (textToken is {} lastTextToken)
                yield return Trim(lastTextToken, lt, TokenTraits.None);
        }

        (Token, int, int) Trim(Token token, TokenTraits left, TokenTraits right)
        {
            var start = token.Start;
            var end = token.End;

            if ((left & TokenTraits.TrimRightGreedy) == TokenTraits.TrimRightGreedy)
            {
                while (start < end && char.IsWhiteSpace(source, start))
                    start++;
            }
            else if ((left & TokenTraits.TrimRight) == TokenTraits.TrimRight)
            {
                while (start < end && source[start] is ' ' or '\t')
                    start++;

                if (start < end && source[start] is '\r')
                    start++;

                if (start < end && source[start] is '\n')
                    start++;
            }

            if ((right & TokenTraits.TrimLeftGreedy) == TokenTraits.TrimLeftGreedy)
            {
                while (end > start && char.IsWhiteSpace(source, end - 1))
                    end--;
            }
            else if ((right & TokenTraits.TrimLeft) == TokenTraits.TrimLeft)
            {
                while (end > start && source[end - 1] is ' ' or '\t')
                    end--;
            }

            return (token, start, end);
        }
    }
}
