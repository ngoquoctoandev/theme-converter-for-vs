// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;

namespace ThemeConverter.ColorCompiler;

/// <summary>
///     Reads or writes the value of a color from a binary stream.  The color record
///     captures the string name of the color and its default background and foreground
///     values.  The ColorRecord must be scoped within a CategoryRecord to fully-identify
///     the color's name.
/// </summary>
internal sealed class ColorRecord
{
    private readonly string _name;

    public ColorRecord(string name) => _name = name;

    public ColorRecord(BinaryReader reader)
    {
        var nameLength = reader.ReadInt32();
        _name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));

        BackgroundType = (__VSCOLORTYPE)reader.ReadByte();
        if (IsValidColorType(BackgroundType))
        {
            Background = reader.ReadUInt32();
        }
        else
        {
            BackgroundType = (byte)__VSCOLORTYPE.CT_INVALID;
            Background     = 0;
        }

        ForegroundType = (__VSCOLORTYPE)reader.ReadByte();
        if (IsValidColorType(ForegroundType))
        {
            Foreground = reader.ReadUInt32();
        }
        else
        {
            ForegroundType = (byte)__VSCOLORTYPE.CT_INVALID;
            Foreground     = 0;
        }
    }

    public __VSCOLORTYPE BackgroundType { get; set; }

    public __VSCOLORTYPE ForegroundType { get; set; }

    public uint Background { get; set; }

    public uint Foreground { get; set; }

    public void Write(BinaryWriter writer)
    {
        var nameBytes = Encoding.UTF8.GetBytes(_name);
        writer.Write(nameBytes.Length);
        writer.Write(nameBytes);

        writer.Write((byte)BackgroundType);
        if (IsValidColorType(BackgroundType)) writer.Write(Background);
        writer.Write((byte)ForegroundType);
        if (IsValidColorType(ForegroundType)) writer.Write(Foreground);
    }

    private static bool IsValidColorType(__VSCOLORTYPE colorType) => colorType == __VSCOLORTYPE.CT_RAW || colorType == __VSCOLORTYPE.CT_SYSCOLOR || colorType == __VSCOLORTYPE.CT_AUTOMATIC || colorType == __VSCOLORTYPE.CT_COLORINDEX;
}