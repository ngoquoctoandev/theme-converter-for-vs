// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;

namespace ThemeConverter.ColorCompiler;

/// <summary>
///     Reads or writes a category of colors from a binary stream.  Each
///     category record contains the GUID identifier for the category
///     and the sequence of color names and values contained in the category.
/// </summary>
internal sealed class CategoryRecord
{
    private readonly Guid              _category;
    private readonly List<ColorRecord> _colors;

    public CategoryRecord(Guid category)
    {
        _category = category;
        _colors   = new List<ColorRecord>();
    }

    public IList<ColorRecord> Colors => _colors;

    public void Write(BinaryWriter writer)
    {
        WriteGuid(writer, _category);
        writer.Write(_colors.Count);
        foreach (var entry in _colors) entry.Write(writer);
    }

    public static void WriteGuid(BinaryWriter writer, Guid guid)
    {
        writer.Write(guid.ToByteArray());
    }
}