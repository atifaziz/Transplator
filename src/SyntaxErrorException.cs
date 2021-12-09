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
using System.Runtime.Serialization;

[Serializable]
public class SyntaxErrorException : Exception
{
    public SyntaxErrorException() : this(null) {}
    public SyntaxErrorException(string? message) : this(message, null) {}
    public SyntaxErrorException(string? message, Exception? inner) : this(-1, message, inner) {}
    public SyntaxErrorException(int offset) : this(offset, null) {}
    public SyntaxErrorException(int offset, string? message) : this(offset, message, null) {}

    public SyntaxErrorException(int offset, string? message, Exception? inner) :
        base(FormatMessage(message, offset), inner)
    {
        Offset = Math.Max(offset, -1);
    }

    static string FormatMessage(string? message, int offset) =>
        message ?? (offset >= 0 ? "Syntax error." : $"Syntax error at offset {offset}.");

    protected SyntaxErrorException(SerializationInfo info,
                                   StreamingContext context) :
        base(info, context)
    {
        Offset = info.GetInt32(nameof(Offset));
    }

    public int Offset { get; }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(Offset), Offset);
    }
}
