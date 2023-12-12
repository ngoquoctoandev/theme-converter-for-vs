// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace ThemeConverter.ColorCompiler;

internal class CategoryThemeKey
{
    public CategoryThemeKey(Guid category, Guid theme)
    {
        Category = category;
        ThemeId  = theme;
    }

    public Guid Category { get; }
    public Guid ThemeId  { get; }

    public override bool Equals(object obj)
    {
        var other = obj as CategoryThemeKey;

        if (other == null) return false;

        return Category == other.Category && ThemeId == other.ThemeId;
    }

    public override int GetHashCode() => Category.GetHashCode() ^ ThemeId.GetHashCode();
}