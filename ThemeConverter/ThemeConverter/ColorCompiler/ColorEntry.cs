// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Drawing;

namespace ThemeConverter.ColorCompiler;

internal class ColorEntry
{
    public ColorEntry(ColorName name) => Name = name;

    public ColorName Name { get; set; }

    public ColorTheme Theme { get; set; }

    public bool IsEmpty { get; set; }

    public Color Background { get; set; }

    public uint BackgroundSource { get; set; }

    public __VSCOLORTYPE BackgroundType { get; set; }

    public Color Foreground { get; set; }

    public uint ForegroundSource { get; set; }

    public __VSCOLORTYPE ForegroundType { get; set; }

    public static Color FromRgba(uint rgba)
    {
        var alpha = (byte)(rgba >> 24);
        var blue  = (byte)(rgba >> 16);
        var green = (byte)(rgba >> 8);
        var red   = (byte)rgba;

        return Color.FromArgb(alpha, red, green, blue);
    }
}