// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace ThemeConverter.ColorCompiler;

internal class ColorName
{
    public ColorName(ColorCategory category, string name)
    {
        Category = category;
        Name     = name;
    }

    public ColorCategory Category { get; }

    public string Name { get; }

    public override bool Equals(object obj)
    {
        var other = obj as ColorName;

        if (other == null) return false;

        return Equals(Category, other.Category) && Equals(Name, other.Name);
    }

    public override int GetHashCode() => Name == null ? 0 : Name.GetHashCode();
}